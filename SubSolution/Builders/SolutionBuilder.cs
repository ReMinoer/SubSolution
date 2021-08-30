using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SubSolution.Configuration;
using SubSolution.FileSystems;
using SubSolution.Generators;
using SubSolution.ProjectReaders;
using SubSolution.Raw;
using SubSolution.Utils;

namespace SubSolution.Builders
{
    public class SolutionBuilder : ISolutionBuilder, ISubSolutionConfigurationVisitor
    {
        private const string LogTokenNone = "*none*";
        private const string LogTokenRoot = "*root*";

        private readonly string _workspaceDirectoryPath;
        
        private readonly Solution _solution;
        private readonly Stack<Solution.Folder> _currentFolderStack;
        private readonly Stack<string> _currentFolderPathStack;

        private Solution.Folder CurrentFolder => _currentFolderStack.Peek();
        private string CurrentFolderPath => _currentFolderPathStack.Count > 0 ? string.Join('/', _currentFolderPathStack.Reverse()) : LogTokenRoot;

        private readonly ISubSolutionFileSystem _fileSystem;
        private readonly CacheSolutionProjectReader _projectReader;

        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        private readonly ISet<string> _ignoredSolutionPaths;
        private readonly ISet<string> _projectConfigurations;
        private readonly ISet<string> _projectPlatforms;

        public SolutionBuilder(SubSolutionContext context)
        {
            _workspaceDirectoryPath = context.WorkspaceDirectoryPath;

            _solution = new Solution(context.SolutionPath, context.FileSystem);
            _currentFolderStack = new Stack<Solution.Folder>();
            _currentFolderStack.Push(_solution.Root);
            _currentFolderPathStack = new Stack<string>();

            _fileSystem = context.FileSystem ?? StandardFileSystem.Instance;
            _projectReader = new CacheSolutionProjectReader(_fileSystem, context.ProjectReader);

            _ignoredSolutionPaths = new HashSet<string>(_fileSystem.PathComparer);
            if (context.ConfigurationFilePath != null)
                _ignoredSolutionPaths.Add(context.ConfigurationFilePath);
            _ignoredSolutionPaths.Add(context.SolutionPath);

            _projectConfigurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _projectPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            _logger = context.Logger ?? NullLogger.Instance;
            _logLevel = context.LogLevel;
        }
        
        public async Task<ISolution> BuildAsync(SubSolutionConfiguration configuration)
        {
            Log("Start building solution");
            Log($"Configuration file: {_ignoredSolutionPaths.FirstOrDefault() ?? LogTokenNone}");
            Log($"Solution output file: {_solution.OutputPath}");
            Log($"Initial workspace directory: {_workspaceDirectoryPath}");

            bool hasFullConfigurationPlatforms = (configuration.Configurations?.Configuration.Count ?? 0) > 0 && (configuration.Platforms?.Platform.Count ?? 0) > 0;
            if (hasFullConfigurationPlatforms)
                VisitConfigurationsAndPlatforms(configuration.Configurations, configuration.Platforms);

            if (configuration.Root != null)
                await VisitRootAsync(configuration.Root);

            if (!hasFullConfigurationPlatforms)
                FillMissingConfigurationsPlatformsFromProjects(configuration.Configurations, configuration.Platforms);

            return _solution;
        }

        private void VisitConfigurationsAndPlatforms(SolutionConfigurationList configurations, SolutionPlatformList platforms)
        {
            foreach (SolutionConfiguration configuration in configurations.Configuration)
                foreach (SolutionPlatform platform in platforms.Platform)
                {
                    var configurationPlatform = new Solution.ConfigurationPlatform(_fileSystem, configuration.Name, platform.Name);
                    if (configuration.ProjectConfiguration.Count == 0)
                        configurationPlatform.MatchingProjectConfigurationNames.Add(configuration.Name);
                    else
                        configurationPlatform.MatchingProjectConfigurationNames.AddRange(configuration.ProjectConfiguration.Select(x => x.Match));

                    if (platform.ProjectPlatform.Count == 0)
                        configurationPlatform.MatchingProjectPlatformNames.Add(platform.Name);
                    else
                        configurationPlatform.MatchingProjectPlatformNames.AddRange(platform.ProjectPlatform.Select(x => x.Match));

                    _solution.AddConfigurationPlatform(configurationPlatform);
                }
        }

        private void FillMissingConfigurationsPlatformsFromProjects(SolutionConfigurationList? configurations, SolutionPlatformList? platforms)
        {
            if (configurations is null)
            {
                configurations = new SolutionConfigurationList();

                foreach (string projectConfigurationName in _projectConfigurations)
                {
                    configurations.Configuration.Add(new SolutionConfiguration
                    {
                        Name = projectConfigurationName,
                        ProjectConfiguration = new List<ProjectConfigurationMatch>
                        {
                            new ProjectConfigurationMatch
                            {
                                Match = projectConfigurationName
                            }
                        }
                    });
                }
            }

            if (platforms is null)
            {
                platforms = new SolutionPlatformList();

                foreach (string projectPlatformName in _projectPlatforms)
                {
                    platforms.Platform.Add(new SolutionPlatform
                    {
                        Name = projectPlatformName,
                        ProjectPlatform = new List<ProjectPlatformMatch>
                        {
                            new ProjectPlatformMatch
                            {
                                Match = projectPlatformName
                            }
                        }
                    });
                }
            }

            VisitConfigurationsAndPlatforms(configurations, platforms);
        }

        private async Task VisitRootAsync(SolutionRootConfiguration root)
        {
            foreach (SolutionItems items in root.SolutionItems)
                await items.AcceptAsync(this);
        }

        public async Task VisitAsync(Folder folder)
        {
            using (MoveCurrentFolder(folder.Name))
                await VisitRootAsync(folder.Content);
        }

        public async Task VisitAsync(Files files)
        {
            foreach (string relativeFilePath in GetMatchingFilePaths(files.Path, defaultFileExtension: "*"))
                await AddFoldersAndFileToSolution(relativeFilePath, AddFile, files.CreateFolders == true, files.Overwrite == true);

            static Task AddFile(Solution.Folder folder, string filePath, bool overwrite)
            {
                folder.AddFile(filePath, overwrite);
                return Task.CompletedTask;
            }
        }
        
        public async Task VisitAsync(Projects projects)
        {
            IEnumerable<string> matchingFilePaths = GetMatchingFilePaths(projects.Path, defaultFileExtension: "csproj");
            Dictionary<string, Task<ISolutionProject>> matchingProjectByPath = matchingFilePaths.ToDictionary(x => x, x => _projectReader.ReadAsync(_fileSystem.Combine(_solution.OutputDirectory, x)));

            await Task.WhenAll(matchingProjectByPath.Values);

            foreach (string relativeFilePath in matchingProjectByPath.Keys)
                await AddFoldersAndFileToSolution(relativeFilePath, AddProject, projects.CreateFolders == true, projects.Overwrite == true);

            async Task AddProject(Solution.Folder folder, string projectPath, bool overwrite)
            {
                ISolutionProject project = await matchingProjectByPath[projectPath];
                if (folder.AddProject(projectPath, project, overwrite))
                {
                    foreach (string projectConfiguration in project.Configurations)
                        _projectConfigurations.Add(projectConfiguration);
                    foreach (string projectPlatform in project.Platforms)
                        _projectPlatforms.Add(projectPlatform);
                }
            }
        }

        public Task VisitAsync(Solutions solutions)
        {
            return VisitAsyncBase(solutions, "sln", async filePath =>
            {
                string solutionName = _fileSystem.GetFileNameWithoutExtension(filePath);

                await using Stream fileStream = _fileSystem.OpenStream(filePath);
                RawSolution rawSolution = await RawSolution.ReadAsync(fileStream);

                RawSolutionToSolutionGenerator solutionGenerator = new RawSolutionToSolutionGenerator(_fileSystem, _projectReader);
                (ISolution solution, _) = await solutionGenerator.GenerateAsync(rawSolution, filePath);

                return (solution, solutionName);
            });
        }

        public Task VisitAsync(SubSolutions subSolutions)
        {
            return VisitAsyncBase(subSolutions, "subsln", async filePath =>
            {
                SubSolutionContext subContext = await SubSolutionContext.FromConfigurationFileAsync(filePath, _projectReader, _fileSystem);
                subContext.Logger = _logger;
                subContext.LogLevel = _logLevel;

                SolutionBuilder solutionBuilder = new SolutionBuilder(subContext);
                return (await solutionBuilder.BuildAsync(subContext.Configuration), subContext.SolutionName);
            });
        }

        private async Task VisitAsyncBase(SolutionContentFiles solutionContentFiles, string defaultFileExtension, Func<string, Task<(ISolution, string)>> solutionLoader)
        {
            IEnumerable<string> matchingFilePaths = GetMatchingFilePaths(solutionContentFiles.Path, defaultFileExtension);
            if (solutionContentFiles.ReverseOrder == true)
            {
                matchingFilePaths = matchingFilePaths.Reverse();
            }

            foreach (string relativeFilePath in matchingFilePaths)
            {
                string filePath = _fileSystem.Combine(_workspaceDirectoryPath, relativeFilePath);
                if (!_ignoredSolutionPaths.Add(filePath) && solutionContentFiles.Overwrite != true)
                    continue;

                (ISolution solution, string solutionName) = await solutionLoader(filePath);
                solution.SetOutputDirectory(_solution.OutputDirectory);

                if (solutionContentFiles.CreateRootFolder == true)
                {
                    using (MoveCurrentFolder(solutionName))
                        CurrentFolder.AddFolderContent(solution.Root, solutionContentFiles.Overwrite == true);
                }
                else
                {
                    CurrentFolder.AddFolderContent(solution.Root, solutionContentFiles.Overwrite == true);
                }
            }
        }

        private IEnumerable<string> GetMatchingFilePaths(string? globPattern, string defaultFileExtension)
        {
            if (string.IsNullOrEmpty(globPattern))
                globPattern = "**/*." + defaultFileExtension;
            else if (globPattern.EndsWith("/") || globPattern.EndsWith("\\"))
                globPattern += "*." + defaultFileExtension;
            else if (globPattern.EndsWith("**"))
                globPattern += "/*." + defaultFileExtension;

            Log($"Search for files matching pattern: {globPattern}");

            return _fileSystem.GetFilesMatchingGlobPattern(_workspaceDirectoryPath, globPattern);
        }

        private async Task AddFoldersAndFileToSolution(string relativeFilePath, Func<Solution.Folder, string, bool, Task> addEntry, bool createFolders, bool overwrite)
        {
            //string absoluteFilePath = _fileSystem.Combine(_workspaceDirectoryPath, workspaceRelativeFilePath);
            //string relativeFilePath = _fileSystem.MakeRelativePath(_solution.OutputDirectory, absoluteFilePath);

            if (createFolders)
            {
                string relativeDirectoryPath = _fileSystem.GetParentDirectoryPath(relativeFilePath) ?? string.Empty;
                string[] solutionFolderPath = _fileSystem.SplitPath(relativeDirectoryPath);

                using (MoveCurrentFolder(solutionFolderPath))
                {
                    Log($"Add: {relativeFilePath}");
                    await addEntry(CurrentFolder, relativeFilePath, overwrite);
                }
            }
            else
            {
                Log($"Add: {relativeFilePath}");
                await addEntry(CurrentFolder, relativeFilePath, overwrite);
            }
        }

        private IDisposable MoveCurrentFolder(params string[] relativeFolderPath)
        {
            foreach (string folderName in relativeFolderPath)
                _currentFolderPathStack.Push(folderName);

            Log($"Set current solution folder to: {CurrentFolderPath}");

            _currentFolderStack.Push(CurrentFolder.GetOrAddSubFolder(relativeFolderPath));

            return new Disposable(() =>
            {
                for (int i = 0; i < relativeFolderPath.Length; i++)
                    _currentFolderPathStack.Pop();

                Log($"Set current solution folder back to: {CurrentFolderPath}");

                _currentFolderStack.Pop();

                RemoveEmptySubFolders(CurrentFolder);
            });
        }

        static private void RemoveEmptySubFolders(Solution.Folder folder)
        {
            foreach (Solution.Folder subFolder in folder.SubFolders.Values)
                RemoveEmptySubFolders(subFolder);

            IEnumerable<string> emptySubFolderNames = folder.SubFolders.Where(x => x.Value.IsEmpty).Select(x => x.Key);
            foreach (string emptySubFolderName in emptySubFolderNames)
                folder.RemoveSubFolder(emptySubFolderName);
        }

        private void Log(string message) => _logger.Log(_logLevel, message);
    }
}