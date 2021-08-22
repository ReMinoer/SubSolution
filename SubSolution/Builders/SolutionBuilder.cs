using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SubSolution.Configuration;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;
using SubSolution.Utils;

namespace SubSolution.Builders
{
    public class SolutionBuilder : ISolutionBuilder, ISubSolutionConfigurationVisitor
    {
        private const string LogTokenNone = "*none*";
        private const string LogTokenRoot = "*root*";

        private readonly string _workspaceDirectoryPath;
        
        private readonly SolutionOutput _solutionOutput;
        private readonly Stack<SolutionOutput.Folder> _currentFolderStack;
        private readonly Stack<string> _currentFolderPathStack;

        private SolutionOutput.Folder CurrentFolder => _currentFolderStack.Peek();
        private string CurrentFolderPath => _currentFolderPathStack.Count > 0 ? string.Join('/', _currentFolderPathStack.Reverse()) : LogTokenRoot;

        private readonly ISubSolutionFileSystem _fileSystem;
        private readonly ISolutionProjectReader _projectReader;

        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        private readonly ISet<string> _knownConfigurationFilePaths;

        public SolutionBuilder(SubSolutionContext context)
        {
            _workspaceDirectoryPath = context.WorkspaceDirectoryPath;

            _solutionOutput = new SolutionOutput(context.SolutionPath, context.FileSystem);
            _currentFolderStack = new Stack<SolutionOutput.Folder>();
            _currentFolderStack.Push(_solutionOutput.Root);
            _currentFolderPathStack = new Stack<string>();

            _fileSystem = context.FileSystem ?? StandardFileSystem.Instance;
            _projectReader = context.ProjectReader;

            _knownConfigurationFilePaths = new HashSet<string>(_fileSystem.PathComparer);
            if (context.ConfigurationFilePath != null)
                _knownConfigurationFilePaths.Add(context.ConfigurationFilePath);

            _logger = context.Logger ?? NullLogger.Instance;
            _logLevel = context.LogLevel;
        }

        public async Task<ISolutionOutput> BuildAsync(SubSolutionConfiguration configuration)
        {
            Log("Start building solution");
            Log($"Configuration file: {_knownConfigurationFilePaths.FirstOrDefault() ?? LogTokenNone}");
            Log($"Solution output file: {_solutionOutput.OutputPath}");
            Log($"Initial workspace directory: {_workspaceDirectoryPath}");

            VisitConfigurationBindings(configuration.ConfigurationBindings);

            if (configuration.Root != null)
                await VisitRootAsync(configuration.Root);

            return _solutionOutput;
        }

        private void VisitConfigurationBindings(SolutionConfigurationBindingList? bindingList)
        {
            if (bindingList == null)
            {
                AddDefaultConfigurationBindings();
                return;
            }

            if (bindingList.UseDefaultBindings == true)
                AddDefaultConfigurationBindings();

            foreach (Binding binding in bindingList.Binding)
                binding.Accept(this);
        }

        private void AddDefaultConfigurationBindings()
        {
            _solutionOutput.ConfigurationBindings.Add(new SolutionOutput.ConfigurationBinding("Debug", "Debug"));
            _solutionOutput.ConfigurationBindings.Add(new SolutionOutput.ConfigurationBinding("Release", "Release"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("Any CPU", "Any CPU"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("x86", "x86"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("x64", "x64"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("Any CPU", "x86"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("Any CPU", "x64"));
        }

        public void Visit(Configuration.Configuration configuration)
        {
            _solutionOutput.ConfigurationBindings.Add(new SolutionOutput.ConfigurationBinding(configuration.Project, configuration.Solution));
        }

        public void Visit(Platform platform)
        {
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding(platform.Project, platform.Solution));
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

            static Task AddFile(SolutionOutput.Folder folder, string filePath, bool overwrite)
            {
                folder.AddFile(filePath, overwrite);
                return Task.CompletedTask;
            }
        }
        
        public async Task VisitAsync(Projects projects)
        {
            string outputDirectory = _fileSystem.GetParentDirectoryPath(_solutionOutput.OutputPath)!;

            IEnumerable<string> matchingFilePaths = GetMatchingFilePaths(projects.Path, defaultFileExtension: "csproj");
            Dictionary<string, Task<ISolutionProject>> matchingProjectByPath = matchingFilePaths.ToDictionary(x => x, x => _projectReader.ReadAsync(x, outputDirectory));

            await Task.WhenAll(matchingProjectByPath.Values);

            foreach (string relativeFilePath in matchingProjectByPath.Keys)
                await AddFoldersAndFileToSolution(relativeFilePath, AddProject, projects.CreateFolders == true, projects.Overwrite == true);

            async Task AddProject(SolutionOutput.Folder folder, string projectPath, bool overwrite)
            {
                ISolutionProject project = await matchingProjectByPath[projectPath];
                folder.AddProject(project, overwrite);
            }
        }

        public async Task VisitAsync(SubSolutions subSolutions)
        {
            IEnumerable<string> matchingFilePaths = GetMatchingFilePaths(subSolutions.Path, defaultFileExtension: "subsln");
            if (subSolutions.ReverseOrder == true)
            {
                matchingFilePaths = matchingFilePaths.Reverse();
            }

            foreach (string relativeFilePath in matchingFilePaths)
            {
                string filePath = _fileSystem.Combine(_workspaceDirectoryPath, relativeFilePath);
                if (!_knownConfigurationFilePaths.Add(filePath) && subSolutions.Overwrite != true)
                    continue;

                SubSolutionContext subContext = await SubSolutionContext.FromConfigurationFileAsync(filePath, _projectReader, _fileSystem);
                subContext.Logger = _logger;
                subContext.LogLevel = _logLevel;

                SolutionBuilder solutionBuilder = new SolutionBuilder(subContext);
                ISolutionOutput subSolution = await solutionBuilder.BuildAsync(subContext.Configuration);

                string outputDirectory = _fileSystem.GetParentDirectoryPath(_solutionOutput.OutputPath)!;
                subSolution.SetOutputDirectory(outputDirectory);

                if (subSolutions.CreateRootFolder == true)
                {
                    using (MoveCurrentFolder(subContext.SolutionName))
                        await CurrentFolder.AddFolderContent(subSolution.Root, _projectReader, subSolutions.Overwrite == true);
                }
                else
                {
                    await CurrentFolder.AddFolderContent(subSolution.Root, _projectReader, subSolutions.Overwrite == true);
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

        private async Task AddFoldersAndFileToSolution(string relativeFilePath, Func<SolutionOutput.Folder, string, bool, Task> addEntry, bool createFolders, bool overwrite)
        {
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

            _currentFolderStack.Push(CurrentFolder.GetOrCreateSubFolder(relativeFolderPath));

            return new Disposable(() =>
            {
                for (int i = 0; i < relativeFolderPath.Length; i++)
                    _currentFolderPathStack.Pop();

                Log($"Set current solution folder back to: {CurrentFolderPath}");

                _currentFolderStack.Pop();

                RemoveEmptySubFolders(CurrentFolder);
            });
        }

        static private void RemoveEmptySubFolders(SolutionOutput.Folder folder)
        {
            foreach (SolutionOutput.Folder subFolder in folder.SubFolders.Values)
                RemoveEmptySubFolders(subFolder);

            IEnumerable<string> emptySubFolderNames = folder.SubFolders.Where(x => x.Value.IsEmpty).Select(x => x.Key);
            foreach (string emptySubFolderName in emptySubFolderNames)
                folder.RemoveSubFolder(emptySubFolderName);
        }

        private void Log(string message) => _logger.Log(_logLevel, message);
    }
}