﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SubSolution.Builders.Configuration;
using SubSolution.Builders.Filters;
using SubSolution.Builders.GlobPatterns;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;
using SubSolution.Raw;
using SubSolution.Utils;

namespace SubSolution.Builders
{
    public class SolutionBuilder : ISolutionItemSourcesVisitor
    {
        private const string LogTokenNone = "*none*";
        private const string LogTokenRoot = "*root*";

        private readonly string _currentDirectoryPath;
        private readonly string _workspaceDirectoryPath;
        
        private readonly Solution _solution;
        private readonly Stack<Solution.Folder> _currentFolderStack;
        private readonly Stack<string> _currentFolderPathStack;

        private Solution.Folder CurrentFolder => _currentFolderStack.Peek();
        private string CurrentFolderPath => _currentFolderPathStack.Count > 0 ? _currentFolderPathStack.Reverse().Join('/') : LogTokenRoot;

        private readonly IGlobPatternFileSystem _fileSystem;
        private readonly CacheProjectReader _projectReader;
        private readonly ProjectGraph _projectGraph;

        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        private readonly ISet<string> _ignoredSolutionPaths;
        private readonly ISet<string> _projectConfigurations;
        private readonly ISet<string> _projectPlatforms;
        private readonly bool _ignoreConfigurationsAndPlatforms;

        private readonly ISet<string> _allAddedProjects;
        private readonly Dictionary<string, ISet<string>> _solutionSetsById;
        private ISet<string>? _projectsInDefaultScope;
        private bool _virtualizing;

        public List<Issue> Issues { get; }

        public SolutionBuilder(SolutionBuilderContext context)
        {
            _currentDirectoryPath = context.CurrentDirectoryPath;
            _workspaceDirectoryPath = context.WorkspaceDirectoryPath;

            _solution = new Solution(context.SolutionDirectoryPath, context.FileSystem);
            _currentFolderStack = new Stack<Solution.Folder>();
            _currentFolderStack.Push(_solution.Root);
            _currentFolderPathStack = new Stack<string>();

            _fileSystem = context.FileSystem ?? StandardGlobPatternFileSystem.Instance;
            _projectReader = new CacheProjectReader(_fileSystem, context.ProjectReader);
            _projectGraph = new ProjectGraph(_fileSystem, _projectReader);

            _logger = context.Logger ?? NullLogger.Instance;
            _logLevel = context.LogLevel;

            _ignoredSolutionPaths = new HashSet<string>(_fileSystem.PathComparer);
            if (context.ConfigurationFilePath != null)
                _ignoredSolutionPaths.Add(context.ConfigurationFilePath);
            _ignoredSolutionPaths.Add(context.SolutionPath);

            _projectConfigurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _projectPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _ignoreConfigurationsAndPlatforms = context.IgnoreConfigurationsAndPlatforms;

            _solutionSetsById = new Dictionary<string, ISet<string>>();
            _allAddedProjects = new HashSet<string>(_fileSystem.PathComparer);

            Issues = new List<Issue>();
        }
        
        public async Task<Solution> BuildAsync(Subsln configuration)
        {
            Issues.Clear();

            Log("Start building solution");
            Log($"Configuration file: {_ignoredSolutionPaths.FirstOrDefault() ?? LogTokenNone}");
            Log($"Solution output directory: {_solution.OutputDirectoryPath}");
            Log($"Initial workspace directory: {_workspaceDirectoryPath}");

            if (_ignoreConfigurationsAndPlatforms)
            {
                if (configuration.Root != null)
                    await VisitRootAsync(configuration.Root);

                return _solution;
            }

            bool hasFullConfigurationPlatforms = (configuration.Configurations?.Configuration.Count ?? 0) > 0 && (configuration.Platforms?.Platform.Count ?? 0) > 0;
            if (hasFullConfigurationPlatforms)
                VisitConfigurationsAndPlatforms(configuration.Configurations!, configuration.Platforms!);

            if (configuration.Virtual != null)
                await VisitVirtualAsync(configuration.Virtual);

            if (configuration.Root != null)
                await VisitRootAsync(configuration.Root);
            
            if (!hasFullConfigurationPlatforms)
                FillMissingConfigurationsPlatformsFromProjects(configuration.Configurations, configuration.Platforms);

            return _solution;
        }

        private void VisitConfigurationsAndPlatforms(SolutionConfigurationList configurations, SolutionPlatformList platforms)
        {
            foreach (SolutionConfiguration configuration in configurations.Configuration)
            {
                Regex[]? matchingProjectConfigurations = configuration.ProjectConfiguration.Count > 0
                    ? configuration.ProjectConfiguration.Select(x => new Regex(x.Match, RegexOptions.IgnoreCase)).ToArray()
                    : null;

                _solution.AddConfiguration(configuration.Name, matchingProjectConfigurations);
            }

            foreach (SolutionPlatform platform in platforms.Platform)
            {
                Regex[]? matchingProjectPlatforms = platform.ProjectPlatform.Count > 0
                    ? platform.ProjectPlatform.Select(x => new Regex(x.Match, RegexOptions.IgnoreCase)).ToArray()
                    : null;

                _solution.AddPlatform(platform.Name, matchingProjectPlatforms);
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

        private IDisposable Virtualize()
        {
            bool previousValue = _virtualizing;
            _virtualizing = true;
            return new Disposable(() => _virtualizing = previousValue);
        }

        private async Task VisitVirtualAsync(VirtualProjectsSets virtualProjectsSets)
        {
            using (Virtualize())
                foreach (SolutionProjects solutionProjects in virtualProjectsSets.SolutionProjects)
                    await solutionProjects.AcceptAsync(this);
        }

        private async Task VisitRootAsync(SolutionFolderBase root)
        {
            foreach (SolutionItems items in root.SolutionItems)
                await items.AcceptAsync(this);

            if (root.CollapseFoldersWithUniqueSubFolder == true)
                CollapseFoldersWithUniqueSubFolder(CurrentFolder);

            if (root.CollapseFoldersWithUniqueItem == true)
                CollapseFoldersWithUniqueItem(CurrentFolder);
        }

        public async Task VisitAsync(Folder folder)
        {
            using (MoveCurrentFolder(folder.Name))
                await VisitRootAsync(folder.Content);
        }

        private void CollapseFoldersWithUniqueSubFolder(Solution.Folder folder)
        {
            foreach (Solution.Folder subFolder in folder.SubFolders.Values)
                CollapseFoldersWithUniqueSubFolder(subFolder);

            CollapseSubFoldersIf(folder, x => x.Projects.Count == 0 && x.FilePaths.Count == 0 && x.SubFolders.Count == 1);
        }

        private void CollapseFoldersWithUniqueItem(Solution.Folder folder)
        {
            CollapseSubFoldersIf(folder, x => x.Projects.Count + x.FilePaths.Count == 1 && x.SubFolders.Count == 0);

            foreach (Solution.Folder subFolder in folder.SubFolders.Values)
                CollapseFoldersWithUniqueItem(subFolder);
        }

        private void CollapseSubFoldersIf(Solution.Folder folder, Predicate<ISolutionFolder> folderPredicate)
        {
            var subFoldersToCollapse = new List<string>();

            foreach ((string subFolderName, Solution.Folder subFolder) in folder.SubFolders.Select(x => (x.Key, x.Value)))
                if (folderPredicate(subFolder))
                    subFoldersToCollapse.Add(subFolderName);

            foreach (string subFolderName in subFoldersToCollapse)
                folder.CollapseSubFolder(subFolderName);
        }

        public async Task VisitAsync(Files files)
        {
            IEnumerable<string> matchingFilePaths = GetMatchingFilePaths(files.Path, defaultFileExtension: "*");

            IFilter<string>? filter = await BuildFilterAsync(files.Where);
            if (filter != null)
            {
                Log("Filter files: " + filter.TextFormat);
                matchingFilePaths = matchingFilePaths.Where(filter.Match);
            }

            foreach (string relativeFilePath in matchingFilePaths)
                AddFoldersAndFileToSolution(relativeFilePath, AddFile, files.CreateFolders == true, files.Overwrite == true);

            static void AddFile(Solution.Folder folder, string workspaceRelativePath, string outputRelativePath, bool overwrite)
            {
                folder.AddFile(outputRelativePath, overwrite);
            }
        }
        
        public async Task VisitAsync(Projects projects)
        {
            string[] matchingFilePaths = GetMatchingProjectPaths(projects.Path).ToArray();
            
            Task<ISolutionProject>[] readProjectTasks = matchingFilePaths
                .Select(x => _projectReader.ReadAsync(_fileSystem.MakeAbsolutePath(_workspaceDirectoryPath, x)))
                .ToArray();

            try
            {
                await Task.WhenAll(readProjectTasks);
            }
            catch (Exception ex)
            {
                string faultedFilePath = matchingFilePaths[Array.FindIndex(readProjectTasks, x => x.IsFaulted)];
                Issues.Add(new Issue(IssueLevel.Error, $"Failed to read project \"{faultedFilePath}\".", ex));
                return;
            }

            Dictionary<string, ISolutionProject> matchingProjectByPath = matchingFilePaths
                .Zip(readProjectTasks.Select(x => x.Result), (k, v) => (k, v))
                .ToDictionary(x => x.k, x => x.v);
            
            await FilterProjectsAsync(projects, matchingProjectByPath);
            AddProjects(projects, matchingProjectByPath);
        }

        public async Task VisitAsync(Dependencies dependencies)
        {
            ISet<string> targetPaths;
            if (dependencies.Target is null)
            {
                targetPaths = GetDefaultTarget();
            }
            else if (!_solutionSetsById.TryGetValue(dependencies.Target, out targetPaths))
            {
                Issues.Add(new Issue(IssueLevel.Warning, $"Unknown target \"{dependencies.Target}\". {nameof(Dependencies)} element ignored."));
                return;
            }

            var matchingDependencies = new Dictionary<string, ISolutionProject>(_fileSystem.PathComparer);
            foreach (string targetPath in targetPaths)
            {
                string absoluteTargetPath = _fileSystem.MakeAbsolutePath(_workspaceDirectoryPath, targetPath);
                try
                {
                    foreach (string absoluteDependencyPath in await _projectGraph.GetDependenciesAsync(absoluteTargetPath))
                    {
                        string dependencyPath = _fileSystem.MakeRelativePath(_workspaceDirectoryPath, absoluteDependencyPath);
                        if (matchingDependencies.ContainsKey(dependencyPath))
                            continue;

                        ISolutionProject dependencyProject;
                        try
                        {
                            dependencyProject = await _projectReader.ReadAsync(absoluteDependencyPath);
                        }
                        catch (Exception ex)
                        {
                            Issues.Add(new Issue(IssueLevel.Error, $"Failed to read dependency project \"{absoluteDependencyPath}\".", ex));
                            continue;
                        }

                        matchingDependencies.Add(dependencyPath, dependencyProject);
                    }
                }
                catch (Exception ex)
                {
                    Issues.Add(new Issue(IssueLevel.Error, $"Failed to get dependencies of project \"{absoluteTargetPath}\".", ex));
                }
            }
            
            await FilterProjectsAsync(dependencies, matchingDependencies);
            AddProjects(dependencies, matchingDependencies);
        }

        public async Task VisitAsync(Dependents dependents)
        {
            ISet<string> targetPaths;
            if (dependents.Target is null)
            {
                targetPaths = GetDefaultTarget();
            }
            else if (!_solutionSetsById.TryGetValue(dependents.Target, out targetPaths))
            {
                Issues.Add(new Issue(IssueLevel.Warning, $"Unknown target \"{dependents.Target}\". {nameof(Dependents)} element ignored."));
                return;
            }

            ISet<string> scopePaths;
            if (dependents.Scope is null)
            {
                scopePaths = GetDefaultScope();
            }
            else if (!_solutionSetsById.TryGetValue(dependents.Scope, out scopePaths))
            {
                Issues.Add(new Issue(IssueLevel.Warning, $"Unknown scope \"{dependents.Scope}\". {nameof(Dependents)} element ignored."));
                return;
            }

            try
            {
                await Task.WhenAll(scopePaths
                    .Select(x => _fileSystem.MakeAbsolutePath(_workspaceDirectoryPath, x))
                    .Select(_projectGraph.GetDependenciesAsync));
            }
            catch (Exception ex)
            {
                Issues.Add(new Issue(IssueLevel.Error, "Failed to get scope dependencies.", ex));
                return;
            }

            var matchingDependents = new Dictionary<string, ISolutionProject>(_fileSystem.PathComparer);
            foreach (string targetPath in targetPaths)
            {
                string absoluteTargetPath = _fileSystem.MakeAbsolutePath(_workspaceDirectoryPath, targetPath);
                bool directOnly = dependents.KeepOnlyDirect == true;
                try
                {
                    foreach (string absoluteDependentPath in await _projectGraph.GetDependentsAsync(absoluteTargetPath, directOnly))
                    {
                        string dependentPath = _fileSystem.MakeRelativePath(_workspaceDirectoryPath, absoluteDependentPath);
                        if (!scopePaths.Contains(dependentPath))
                            continue;
                        if (matchingDependents.ContainsKey(dependentPath))
                            continue;

                        ISolutionProject dependentProject;
                        try
                        {
                            dependentProject = await _projectReader.ReadAsync(absoluteDependentPath);
                        }
                        catch (Exception ex)
                        {
                            Issues.Add(new Issue(IssueLevel.Error, $"Failed to read dependent project \"{absoluteDependentPath}\".", ex));
                            continue;
                        }

                        matchingDependents.Add(dependentPath, dependentProject);
                    }
                }
                catch (Exception ex)
                {
                    Issues.Add(new Issue(IssueLevel.Error, $"Failed to get dependents of project \"{absoluteTargetPath}\".", ex));
                }
            }

            if (dependents.KeepOnlySatisfiedBeforeFilter == true)
                await KeepOnlySatisfiedAsync(matchingDependents);

            await FilterProjectsAsync(dependents, matchingDependents);

            if (dependents.KeepOnlySatisfied == true)
                await KeepOnlySatisfiedAsync(matchingDependents);

            AddProjects(dependents, matchingDependents);
        }

        private ISet<string> GetDefaultTarget() => _allAddedProjects.ToHashSet(_fileSystem.PathComparer);
        private ISet<string> GetDefaultScope() => _projectsInDefaultScope ??= GetMatchingProjectPaths("**").ToHashSet(_fileSystem.PathComparer);

        private async Task KeepOnlySatisfiedAsync(Dictionary<string, ISolutionProject> matchingDependents)
        {
            List<string> remainingDependents = new List<string>(matchingDependents.Keys);
            HashSet<string> addedProjects = new HashSet<string>(_allAddedProjects, _fileSystem.PathComparer);

            bool anySatisfied;
            do
            {
                anySatisfied = false;
                for (int i = 0; i < remainingDependents.Count; i++)
                {
                    string dependentPath = remainingDependents[i];
                    string absoluteDependentPath = _fileSystem.MakeAbsolutePath(_workspaceDirectoryPath, dependentPath);

                    IReadOnlyCollection<string> absoluteDependentDependenciesPath;
                    try
                    {
                        absoluteDependentDependenciesPath = await _projectGraph.GetDependenciesAsync(absoluteDependentPath);
                    }
                    catch (Exception ex)
                    {
                        Issues.Add(new Issue(IssueLevel.Error, $"Failed to get direct dependencies of project \"{absoluteDependentPath}\".", ex));
                        return;
                    }

                    IEnumerable<string> dependentDependencyPaths = absoluteDependentDependenciesPath.Select(x => _fileSystem.MakeRelativePath(_workspaceDirectoryPath, x));

                    if (!addedProjects.IsSupersetOf(dependentDependencyPaths))
                        continue;

                    addedProjects.Add(dependentPath);
                    remainingDependents.RemoveAt(i);
                    i--;
                    anySatisfied = true;
                }
            }
            while (anySatisfied);

            foreach (string remainingDependent in remainingDependents)
                matchingDependents.Remove(remainingDependent);
        }

        private async Task FilterProjectsAsync(ProjectsBase projects, Dictionary<string, ISolutionProject> matchingProjectByPath)
        {
            IFilter<(string, ISolutionProject)>? filter = await BuildFilterAsync(projects.Where);
            if (filter != null)
            {
                Log("Filter projects: " + filter.TextFormat);

                string[] ignoredProjectPaths = matchingProjectByPath.Where(x => !filter.Match((x.Key, x.Value))).Select(x => x.Key).ToArray();
                foreach (string ignoredProjectPath in ignoredProjectPaths)
                    matchingProjectByPath.Remove(ignoredProjectPath);
            }
        }

        private void AddProjects(ProjectsBase projects, Dictionary<string, ISolutionProject> matchingProjectByPath)
        {
            if (projects.Id != null)
                _solutionSetsById.Add(projects.Id, matchingProjectByPath.Keys.ToHashSet(_fileSystem.PathComparer));

            if (_virtualizing)
                return;

            foreach (string relativeFilePath in matchingProjectByPath.Keys)
                AddFoldersAndFileToSolution(relativeFilePath, AddProject, projects.CreateFolders == true, projects.Overwrite == true);

            void AddProject(Solution.Folder folder, string workspaceRelativePath, string outputRelativePath, bool overwrite)
            {
                ISolutionProject project = matchingProjectByPath[workspaceRelativePath];
                if (folder.AddProject(outputRelativePath, project, overwrite) && !_ignoreConfigurationsAndPlatforms)
                {
                    _allAddedProjects.Add(outputRelativePath);

                    foreach (string projectConfiguration in project.Configurations)
                        _projectConfigurations.Add(projectConfiguration);
                    foreach (string projectPlatform in project.Platforms)
                        _projectPlatforms.Add(projectPlatform);
                }
            }
        }

        public Task VisitAsync(Solutions solutions)
        {
            return VisitSolutionsBaseAsync(solutions, "sln", async filePath =>
            {
                await using IAsyncDisposable _ = _fileSystem.OpenStream(filePath)
                    .AsAsyncDisposable(out Stream fileStream);

                RawSolution rawSolution = await RawSolution.ReadAsync(fileStream);

                RawSolutionConverter solutionConverter = new RawSolutionConverter(_fileSystem, _projectReader)
                {
                    SkipConfigurationPlatforms = true
                };

                string solutionName = _fileSystem.GetFileNameWithoutExtension(filePath);
                string solutionDirectoryPath = _fileSystem.GetParentDirectoryPath(filePath)!;

                IMergeableSolution solution = await solutionConverter.ConvertAsync(rawSolution, solutionDirectoryPath);
                Issues.AddRange(solutionConverter.Issues);

                return (solution, solutionName);
            });
        }

        public Task VisitAsync(SubSolutions subSolutions)
        {
            return VisitSolutionsBaseAsync(subSolutions, "subsln", async filePath =>
            {
                SolutionBuilderContext subContext = await SolutionBuilderContext.FromConfigurationFileAsync(filePath, _projectReader, _fileSystem);
                subContext.Logger = _logger;
                subContext.LogLevel = _logLevel;
                subContext.IgnoreConfigurationsAndPlatforms = true;

                SolutionBuilder solutionBuilder = new SolutionBuilder(subContext);
                IMergeableSolution solution = await solutionBuilder.BuildAsync(subContext.Configuration);

                return (solution, subContext.SolutionName);
            });
        }

        private async Task VisitSolutionsBaseAsync(SolutionContentFiles solutionContentFiles, string defaultFileExtension, Func<string, Task<(IMergeableSolution, string)>> solutionLoader)
        {
            IEnumerable<string> matchingFilePaths = GetMatchingFilePaths(solutionContentFiles.Path, defaultFileExtension);
            if (solutionContentFiles.ReverseOrder == true)
            {
                matchingFilePaths = matchingFilePaths.Reverse();
            }

            IFilter<string>? filter = await BuildFilterAsync(solutionContentFiles.Where);
            if (filter != null)
            {
                Log("Filter solutions: " + filter.TextFormat);
                matchingFilePaths = matchingFilePaths.Where(filter.Match);
            }

            HashSet<string> allSolutionsProjects = new HashSet<string>(_fileSystem.PathComparer);

            foreach (string relativeFilePath in matchingFilePaths)
            {
                string filePath = _fileSystem.MakeAbsolutePath(_workspaceDirectoryPath, relativeFilePath);
                if (!_ignoredSolutionPaths.Add(filePath) && solutionContentFiles.Overwrite != true)
                    continue;

                (IMergeableSolution solution, string solutionName) = await solutionLoader(filePath);
                solution.ChangeOutputDirectory(_solution.OutputDirectoryPath);

                if (solutionContentFiles.KeepOnly != null)
                {
                    Subsln scopedConfiguration = new Subsln
                    {
                        Root = new SolutionRoot()
                    };

                    foreach (SolutionFiles keptSolutionFiles in solutionContentFiles.KeepOnly.SolutionFiles)
                        scopedConfiguration.Root.SolutionItems.Add(keptSolutionFiles);

                    SolutionBuilderContext scopedContext = SolutionBuilderContext.FromConfiguration(
                        _currentDirectoryPath, scopedConfiguration, _projectReader, _solution.OutputDirectoryPath, _workspaceDirectoryPath, _fileSystem);

                    scopedContext.Logger = _logger;
                    scopedContext.LogLevel = _logLevel;
                    scopedContext.IgnoreConfigurationsAndPlatforms = true;

                    SolutionBuilder scopedSolutionBuilder = new SolutionBuilder(scopedContext);
                    IMergeableSolution scopedSolution = await scopedSolutionBuilder.BuildAsync(scopedContext.Configuration);

                    IReadOnlyCollection<string> scopedProjectPaths = scopedSolution.Root.GetAllProjectPaths();
                    solution.Root.FilterProjects((path, _) => scopedProjectPaths.Contains(path));

                    IReadOnlyCollection<string> scopedFilePaths = scopedSolution.Root.GetAllFilePaths();
                    solution.Root.FilterFiles(scopedFilePaths.Contains);
                }

                if (solutionContentFiles.WhereProjects?.IgnoreAll == true)
                {
                    solution.Root.FilterProjects((_, __) => false);
                }
                else
                {
                    IFilter<(string, ISolutionProject)>? projectFilter = await BuildFilterAsync(solutionContentFiles.WhereProjects);
                    if (projectFilter != null)
                    {
                        Log($"Filter \"{solutionName}\" solution projects: " + projectFilter.TextFormat);
                        solution.Root.FilterProjects((path, project) => projectFilter.Match((path, project)));
                    }
                }

                if (solutionContentFiles.WhereFiles?.IgnoreAll == true)
                {
                    solution.Root.FilterFiles(_ => false);
                }
                else
                {
                    IFilter<string>? fileFilter = await BuildFilterAsync(solutionContentFiles.WhereFiles);
                    if (fileFilter != null)
                    {
                        Log($"Filter \"{solutionName}\" solution files: " + fileFilter.TextFormat);
                        solution.Root.FilterFiles(fileFilter.Match);
                    }
                }
                
                foreach (string addedProject in solution.Root.GetAllProjectPaths())
                    allSolutionsProjects.Add(addedProject);

                if (_virtualizing)
                    continue;

                if (solutionContentFiles.CreateRootFolder == true)
                {
                    using (MoveCurrentFolder(solutionName))
                        CurrentFolder.AddFolderContent(solution.Root, solutionContentFiles.Overwrite == true);
                }
                else
                {
                    CurrentFolder.AddFolderContent(solution.Root, solutionContentFiles.Overwrite == true);
                }

                if (!_ignoreConfigurationsAndPlatforms)
                    FillProjectConfigurationPlatformList(solution.Root);

                void FillProjectConfigurationPlatformList(ISolutionFolder folder)
                {
                    foreach (ISolutionProject project in folder.Projects.Values)
                    {
                        foreach (string projectConfiguration in project.Configurations)
                            _projectConfigurations.Add(projectConfiguration);
                        foreach (string projectPlatform in project.Platforms)
                            _projectPlatforms.Add(projectPlatform);
                    }

                    foreach (ISolutionFolder subFolder in folder.SubFolders.Values)
                        FillProjectConfigurationPlatformList(subFolder);
                }

                foreach (string solutionProject in allSolutionsProjects)
                    _allAddedProjects.Add(solutionProject);
            }

            if (solutionContentFiles.Id != null)
                _solutionSetsById.Add(solutionContentFiles.Id, allSolutionsProjects);
        }

        private IEnumerable<string> GetMatchingFilePaths(string? globPattern, string defaultFileExtension)
        {
            if (globPattern != null && _fileSystem.IsAbsolutePath(globPattern))
            {
                if (_fileSystem.FileExists(globPattern))
                    return new[] { _fileSystem.MakeRelativePathIfPossible(_workspaceDirectoryPath, globPattern) };
                
                return Enumerable.Empty<string>();
            }

            globPattern = GlobPatternUtils.Expand(globPattern, defaultFileExtension);

            Log($"Search for files matching pattern: {globPattern}");
            return _fileSystem.GetFilesMatchingGlobPattern(_workspaceDirectoryPath, globPattern);
        }

        private IEnumerable<string> GetMatchingProjectPaths(string? globPattern)
        {
            if (globPattern != null && _fileSystem.IsAbsolutePath(globPattern))
            {
                if (_fileSystem.FileExists(globPattern))
                    return new[] { _fileSystem.MakeRelativePathIfPossible(_workspaceDirectoryPath, globPattern) };

                return Enumerable.Empty<string>();
            }

            var expandedGlobPatterns = new HashSet<string>();
            foreach (string projectExtensionPattern in ProjectFileExtensions.ExtensionPatterns)
                expandedGlobPatterns.Add(GlobPatternUtils.Expand(globPattern, projectExtensionPattern));
            
            Log($"Search for files matching pattern: {string.Join("|", expandedGlobPatterns)}");

            var allMatchingProjects = new HashSet<string>(_fileSystem.PathComparer);
            foreach (string fullGlobPattern in expandedGlobPatterns)
            {
                IEnumerable<string> matchingProjects = _fileSystem.GetFilesMatchingGlobPattern(_workspaceDirectoryPath, fullGlobPattern);

                // If globPattern was updated to include project file extension patterns, keep only known extensions.
                if (fullGlobPattern != globPattern)
                    matchingProjects = matchingProjects.Where(ProjectFileExtensions.MatchAny);

                foreach (string matchingProject in matchingProjects)
                    allMatchingProjects.Add(matchingProject);
            }
            
            return allMatchingProjects;
        }

        private void AddFoldersAndFileToSolution(string workspaceRelativeFilePath, Action<Solution.Folder, string, string, bool> addEntry, bool createFolders, bool overwrite)
        {
            string absoluteFilePath = _fileSystem.MakeAbsolutePath(_workspaceDirectoryPath, workspaceRelativeFilePath);
            string outputRelativeFilePath = _fileSystem.MakeRelativePathIfPossible(_solution.OutputDirectoryPath, absoluteFilePath);

            if (createFolders)
            {
                string relativeDirectoryPath = _fileSystem.GetParentDirectoryPath(workspaceRelativeFilePath) ?? string.Empty;
                string[] solutionFolderPath = _fileSystem.SplitPath(relativeDirectoryPath);

                using (MoveCurrentFolder(solutionFolderPath))
                {
                    Log($"Add: {workspaceRelativeFilePath}");
                    addEntry(CurrentFolder, workspaceRelativeFilePath, outputRelativeFilePath, overwrite);
                }
            }
            else
            {
                Log($"Add: {workspaceRelativeFilePath}");
                addEntry(CurrentFolder, workspaceRelativeFilePath, outputRelativeFilePath, overwrite);
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

            string[] emptySubFolderNames = folder.SubFolders.Where(x => x.Value.IsEmpty).Select(x => x.Key).ToArray();
            foreach (string emptySubFolderName in emptySubFolderNames)
                folder.RemoveSubFolder(emptySubFolderName);
        }

        private async Task<IFilter<(string, ISolutionProject)>?> BuildFilterAsync(ProjectFilterRoot? filterRoot)
        {
            if (filterRoot is null)
                return null;

            var filter = new AllFilter<(string, ISolutionProject)>();
            var filterBuilder = new ProjectFilterBuilder(_fileSystem);

            foreach (ProjectFilters filterNode in filterRoot.ProjectFilters)
            {
                await filterNode.AcceptAsync(filterBuilder);
                filter.Filters.Add(filterBuilder.BuiltFilter);
            }

            await filter.PrepareAsync();
            return filter;
        }

        private async Task<IFilter<string>?> BuildFilterAsync(FileFilterRoot? filterRoot)
        {
            if (filterRoot is null)
                return null;

            var filter = new AllFilter<string>();
            var filterBuilder = new FileFilterBuilder(_fileSystem);

            foreach (FileFilters filterNode in filterRoot.FileFilters)
            {
                await filterNode.AcceptAsync(filterBuilder);
                filter.Filters.Add(filterBuilder.BuiltFilter);
            }

            await filter.PrepareAsync();
            return filter;
        }

        private void Log(string message) => _logger.Log(_logLevel, message);
    }
}