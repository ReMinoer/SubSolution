using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubSolution.Converters.Changes;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;
using SubSolution.Raw;

namespace SubSolution.Converters
{
    public class SolutionConverter
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProjectReader? _projectReader;
        private readonly IProjectGraph? _projectGraph;
        private readonly string? _solutionDirectoryPath;
        
        private readonly List<SolutionChange> _changes;
        public IReadOnlyCollection<SolutionChange> Changes { get; }

        public ILogger? Logger { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public SolutionConverter(IFileSystem fileSystem, IProjectReader? projectReader, string? solutionDirectoryPath)
        {
            _fileSystem = fileSystem;
            _solutionDirectoryPath = solutionDirectoryPath;
            if (projectReader != null)
            {
                _projectReader = new CacheProjectReader(_fileSystem, projectReader);
                _projectGraph = new ProjectGraph(_fileSystem, projectReader);
            }

            _changes = new List<SolutionChange>();
            Changes = _changes.AsReadOnly();
        }

        public async Task<RawSolution> ConvertAsync(ISolution solution)
        {
            _changes.Clear();

            Dictionary<string, Guid> projectGuids = new Dictionary<string, Guid>();

            RawSolution rawSolution = new RawSolution
            {
                SlnFormatVersion = new Version(12, 0),
                MajorVisualStudioVersion = 16,
                VisualStudioVersion = new Version(16, 0, 31424, 327),
                MinimumVisualStudioVersion = new Version(10, 0, 40219, 1)
            };

            await ComputeDependenciesIfNecessaryAsync(solution);
            await AddFolderContentAsync(rawSolution, solution.Root, projectGuids);
            AddConfigurationPlatforms(rawSolution, solution, projectGuids);
            return rawSolution;
        }

        public async Task UpdateAsync(RawSolution rawSolution, ISolution solution)
        {
            _changes.Clear();

            var existingFolders = new Dictionary<ISolutionFolder, RawSolution.Project>();
            var rawFolderPaths = new Dictionary<RawSolution.Project, string>();
            var missingFilePaths = new Dictionary<string, int>(_fileSystem.PathComparer);
            var projectGuidByPath = new Dictionary<string, Guid>(_fileSystem.PathComparer);
            var projectPathByGuid = new Dictionary<Guid, string>();

            Task computeDependenciesTask = ComputeDependenciesIfNecessaryAsync(solution);

            UpdateExistingFileAndFolders(rawSolution, solution, existingFolders, rawFolderPaths, missingFilePaths);
            CleanExistingProjects(rawSolution, solution, existingFolders, rawFolderPaths, projectGuidByPath, projectPathByGuid);

            HashSet<string> removedSolutionConfigs = new HashSet<string>();
            CleanExistingSolutionConfigurationPlatforms(rawSolution, solution, removedSolutionConfigs);
            CleanExistingProjectConfigurationPlatforms(rawSolution, solution, removedSolutionConfigs, projectPathByGuid);
            CleanExistingSharedProjectDependencies(rawSolution, solution, projectPathByGuid);

            await computeDependenciesTask;
            await AddFolderContentAsync(rawSolution, solution.Root, projectGuidByPath, existingFolders, missingFilePaths);
            AddConfigurationPlatforms(rawSolution, solution, projectGuidByPath, checkExisting: true);
            
            foreach ((string filePath, int count) in missingFilePaths)
                for (int i = 0; i < count; i++)
                    Log(SolutionChangeType.Remove, SolutionObjectType.File, filePath, null);

            rawSolution.GlobalSections.RemoveAll(x => x.OrderedValuePairs.Count == 0);
        }

        private async Task ComputeDependenciesIfNecessaryAsync(ISolution solution)
        {
            if (_projectGraph is null || _solutionDirectoryPath is null)
                return;
            
            await Task.WhenAll(solution.Root.GetAllProjectPaths()
                .Select(x => _fileSystem.MakeAbsolutePath(_solutionDirectoryPath, x))
                .Select(_projectGraph.GetDependenciesAsync));
        }

        private Exception SharedProjectsUnsupportedException()
        {
            return new InvalidOperationException("Shared projects are not supported by this converter. " +
                "You must provide the solution path and a project reader when creating your converter.");
        }

        private async Task AddFolderContentAsync(RawSolution rawSolution, ISolutionFolder solutionFolder, Dictionary<string, Guid> projectGuids,
            Dictionary<ISolutionFolder, RawSolution.Project>? existingFolders = null, Dictionary<string, int>? missingFilePaths = null,
            string? folderPath = null, RawSolution.Project? folderProject = null)
        {
            bool checkExisting = existingFolders != null;

            foreach ((string projectPath, ISolutionProject project) in solutionFolder.Projects)
            {
                if (!checkExisting || !projectGuids.ContainsKey(projectPath))
                {
                    Log(SolutionChangeType.Add, SolutionObjectType.Project, projectPath, folderPath);

                    RawSolution.Project rawProject = CreateProject(rawSolution, project.TypeGuid, projectPath, folderProject?.ProjectGuid);
                    projectGuids.Add(rawProject.Path, rawProject.ProjectGuid);
                }

                if (project.Type == ProjectType.Shared || project.Type == ProjectType.SharedItems)
                {
                    if (_projectReader is null || _projectGraph is null || _solutionDirectoryPath is null)
                        throw SharedProjectsUnsupportedException();

                    string sharedItemsFilePath = projectPath;
                    if (project.Type == ProjectType.Shared)
                        sharedItemsFilePath = _fileSystem.ChangeFileExtension(projectPath, ProjectFileExtensions.Extensions[ProjectFileExtension.Projitems]);
                    
                    string key = $"{sharedItemsFilePath}*{projectGuids[projectPath].ToRawFormat().ToLower()}*{RawKeyword.SharedMSBuildProjectFilesSharedItemsImports}";
                    string value;
                    switch (project.Type)
                    {
                        case ProjectType.Shared:
                            value = RawKeyword.SharedItemsImportsShprojValue;
                            break;
                        case ProjectType.SharedItems:
                            value = RawKeyword.SharedItemsImportsVcxitemsValue;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }

                    RawSolution.Section sharedProjectSection = GetOrAddGlobalSection(rawSolution, RawKeyword.SharedMSBuildProjectFiles);
                    if (!sharedProjectSection.ValuesByKey.TryGetValue(key, out string existingValue) || value != existingValue)
                        sharedProjectSection.SetOrAddValue(key, value);

                    string absoluteProjectPath = _fileSystem.MakeAbsolutePath(_solutionDirectoryPath, projectPath);
                    foreach (string dependentProjectAbsolutePath in (await _projectGraph.GetDependentsAsync(absoluteProjectPath, directOnly: true)))
                    {
                        string dependentProjectRelativePath = _fileSystem.MakeRelativePath(_solutionDirectoryPath, dependentProjectAbsolutePath);
                        key = $"{sharedItemsFilePath}*{projectGuids[dependentProjectRelativePath].ToRawFormat().ToLower()}*{RawKeyword.SharedMSBuildProjectFilesSharedItemsImports}";

                        ISolutionProject dependentProject = await _projectReader.ReadAsync(dependentProjectAbsolutePath);
                        switch (dependentProject.Type)
                        {
                            case ProjectType.Shared:
                                value = RawKeyword.SharedItemsImportsShprojValue;
                                break;
                            case ProjectType.SharedItems:
                                value = RawKeyword.SharedItemsImportsVcxitemsValue;
                                break;
                            case ProjectType.CSharpDotNetSdk:
                            case ProjectType.VisualBasicDotNetSdk:
                                value = RawKeyword.SharedItemsImportsDotNetSdkValue;
                                break;
                            default:
                                value = RawKeyword.SharedItemsImportsDefaultValue;
                                break;
                        }
                        
                        if (!sharedProjectSection.ValuesByKey.TryGetValue(key, out existingValue) || (existingValue != null && value != existingValue))
                        {
                            Log(SolutionChangeType.Add, SolutionObjectType.SharedProject, projectPath, dependentProjectRelativePath);
                            sharedProjectSection.SetOrAddValue(key, value);
                        }
                    }
                }
            }

            foreach ((string subFolderName, ISolutionFolder subFolder) in solutionFolder.SubFolders.Select(x => (x.Key, x.Value)))
            {
                RawSolution.Project subFolderProject;
                if (existingFolders is null)
                {
                    Log(SolutionChangeType.Add, SolutionObjectType.Folder, subFolderName, folderPath);

                    subFolderProject = CreateProject(rawSolution, ProjectTypes.FolderGuid, subFolderName, folderProject?.ProjectGuid);
                }
                else
                    subFolderProject = existingFolders[subFolder];

                await AddFolderContentAsync(rawSolution, subFolder, projectGuids, existingFolders, missingFilePaths, AppendFolderPath(folderPath, subFolderName), subFolderProject);
            }

            if (solutionFolder.FilePaths.Count > 0)
            {
                RawSolution.Section solutionItemsSection = GetOrAddSolutionItemsSection(folderProject ?? GetOrAddRootFileFolderProject(rawSolution));

                foreach (string filePath in solutionFolder.FilePaths)
                {
                    var changeType = SolutionChangeType.Add;

                    if (missingFilePaths != null)
                    {
                        if (solutionItemsSection.ValuesByKey.ContainsKey(filePath))
                            continue;

                        if (missingFilePaths.TryGetValue(filePath, out int filePathCounter))
                        {
                            changeType = SolutionChangeType.Move;

                            filePathCounter--;
                            if (filePathCounter <= 0)
                                missingFilePaths.Remove(filePath);
                        }
                        else
                        {
                            changeType = SolutionChangeType.Add;
                        }
                    }

                    Log(changeType, SolutionObjectType.File, filePath, folderPath);
                    solutionItemsSection.AddValue(filePath, filePath);
                }
            }
        }

        private RawSolution.Project CreateProject(RawSolution rawSolution, Guid typeGuid, string path, Guid? parentGuid = null)
        {
            string name = typeGuid == ProjectTypes.FolderGuid ? path : _fileSystem.GetFileNameWithoutExtension(path);
            var projectGuid = Guid.NewGuid();

            RawSolution.Project project = new RawSolution.Project(typeGuid, name, path, projectGuid);
            rawSolution.Projects.Add(project);

            if (parentGuid != null)
            {
                RawSolution.Section nestedProjectsSection = GetOrAddGlobalSection(rawSolution, RawKeyword.NestedProjects);
                nestedProjectsSection.AddValue(projectGuid.ToRawFormat(), parentGuid.Value.ToRawFormat());
            }

            return project;
        }

        private void AddConfigurationPlatforms(RawSolution rawSolution, ISolution solution,
            Dictionary<string, Guid> projectGuidbyPath,
            bool checkExisting = false)
        {
            if (solution.ConfigurationPlatforms.Count == 0)
                return;

            RawSolution.Section solutionConfigsSection = GetOrAddGlobalSection(rawSolution, RawKeyword.SolutionConfigurationPlatforms);
            RawSolution.Section projectConfigsSection = GetOrAddGlobalSection(rawSolution, RawKeyword.ProjectConfigurationPlatforms);

            Func<RawSolution.Section, string, string, bool> addOrMove;
            if (checkExisting)
                addOrMove = MoveOrAddValue;
            else
                addOrMove = AddValue;

            foreach (ISolutionConfigurationPlatform configurationPlatform in solution.ConfigurationPlatforms)
            {
                string solutionConfigurationFullName = configurationPlatform.ConfigurationName + '|' + configurationPlatform.PlatformName;

                if (addOrMove(solutionConfigsSection, solutionConfigurationFullName, solutionConfigurationFullName))
                    Log(SolutionChangeType.Add, SolutionObjectType.ConfigurationPlatform, solutionConfigurationFullName, null);

                foreach ((string projectPath, SolutionProjectContext projectContext) in configurationPlatform.ProjectContexts)
                {
                    string prefix = $"{projectGuidbyPath[projectPath].ToRawFormat()}.{solutionConfigurationFullName}.";
                    string projectConfigurationFullName = projectContext.ConfigurationName + '|' + projectContext.PlatformName;

                    if (addOrMove(projectConfigsSection, prefix + RawKeyword.ActiveCfg, projectConfigurationFullName))
                        Log(SolutionChangeType.Add, SolutionObjectType.ProjectContext, projectPath, solutionConfigurationFullName);

                    if (projectContext.Build)
                        addOrMove(projectConfigsSection, prefix + RawKeyword.Build0, projectConfigurationFullName);
                    if (projectContext.Deploy)
                        addOrMove(projectConfigsSection, prefix + RawKeyword.Deploy0, projectConfigurationFullName);
                }
            }

            bool AddValue(RawSolution.Section section, string key, string value)
            {
                section.AddValue(key, value);
                return true;
            }

            bool MoveOrAddValue(RawSolution.Section x, string k, string v)
            {
                if (x.TryReplaceValue(k, v))
                    return false;

                AddValue(x, k, v);
                return true;
            }
        }

        private void UpdateExistingFileAndFolders(RawSolution rawSolution, ISolution solution,
            Dictionary<ISolutionFolder, RawSolution.Project> existingFolders,
            Dictionary<RawSolution.Project, string> rawFolderPaths,
            Dictionary<string, int> missingFilePaths)
        {
            RawSolution.Section? nestedProjectsSection = GetGlobalSection(rawSolution, RawKeyword.NestedProjects);

            var rootSubFolderProjects = new List<RawSolution.Project>();
            var folderProjectChildren = new Dictionary<Guid, List<RawSolution.Project>>();

            RawSolution.Project? rootFileFolderProject = GetRootFileFolderProject(rawSolution);
            bool hasValidRootFileFolderProject = true;

            // Get current folder hierarchy
            foreach (RawSolution.Project rawProject in rawSolution.Projects)
            {
                Guid? parentGuid = GetParentGuid(rawProject, nestedProjectsSection);
                if (parentGuid.HasValue)
                {
                    // If parent is default file folder, we will not be used as such since it contains more than files.
                    if (rootFileFolderProject != null && parentGuid == rootFileFolderProject.ProjectGuid)
                        hasValidRootFileFolderProject = false;
                }

                if (rawProject.TypeGuid != ProjectTypes.FolderGuid)
                    continue;

                if (parentGuid.HasValue)
                {
                    if (!folderProjectChildren.TryGetValue(parentGuid.Value, out List<RawSolution.Project> children))
                        folderProjectChildren[parentGuid.Value] = children = new List<RawSolution.Project>();
                    children.Add(rawProject);
                }
                else
                {
                    rootSubFolderProjects.Add(rawProject);
                }
            }

            // Clean root file folder project (if valid). Remove it if it has no sections after clean.
            if (rootFileFolderProject != null && hasValidRootFileFolderProject)
            {
                CleanFiles(rootFileFolderProject, solution.Root);
                if (rootFileFolderProject.Sections.Count == 0)
                    rawSolution.Projects.Remove(rootFileFolderProject);

                rootSubFolderProjects.Remove(rootFileFolderProject);
            }

            // Explore hierarchy
            UpdateExistingSubFolders(solution.Root, null, rootSubFolderProjects);

            void UpdateExistingSubFolders(ISolutionFolder folder, Guid? folderGuid, List<RawSolution.Project> subFolderProjects, string? folderPath = null)
            {
                foreach (RawSolution.Project subFolderProject in subFolderProjects)
                {
                    // If sub-folder still exist, find missing files and do recursive update.
                    if (folder.SubFolders.ContainsKey(subFolderProject.Name))
                    {
                        ISolutionFolder subFolder = folder.SubFolders[subFolderProject.Name];
                        existingFolders.Add(subFolder, subFolderProject);

                        string subFolderPath = AppendFolderPath(folderPath, subFolderProject.Name);
                        rawFolderPaths.Add(subFolderProject, subFolderPath);

                        CleanFiles(subFolderProject, subFolder);

                        if (!folderProjectChildren.TryGetValue(subFolderProject.ProjectGuid, out List<RawSolution.Project> childFolderProjects))
                            childFolderProjects = new List<RawSolution.Project>();

                        UpdateExistingSubFolders(subFolder, subFolderProject.ProjectGuid, childFolderProjects, subFolderPath);
                        continue;
                    }
                    
                    // Else remove it.
                    RemoveFolder(subFolderProject, folderPath);
                }

                foreach ((string subFolderName, ISolutionFolder subFolder) in folder.SubFolders.Select(x => (x.Key, x.Value)))
                {
                    // If sub-folder already existed, skip.
                    if (subFolderProjects.Any(x => x.Name == subFolderName))
                        continue;

                    // Else add it.
                    AddSubFolder(subFolderName, subFolder, folderGuid, folderPath);
                }
            }

            void AddSubFolder(string folderName, ISolutionFolder folder, Guid? parentGuid, string? folderPath)
            {
                // Add folder
                Log(SolutionChangeType.Add, SolutionObjectType.Folder, folderName, folderPath);

                RawSolution.Project folderProject = CreateProject(rawSolution, ProjectTypes.FolderGuid, folderName, parentGuid);
                existingFolders.Add(folder, folderProject);

                string newFolderPath = AppendFolderPath(folderPath, folderName);
                rawFolderPaths.Add(folderProject, newFolderPath);

                // Add sub-folders
                foreach ((string subFolderName, ISolutionFolder subFolder) in folder.SubFolders.Select(x => (x.Key, x.Value)))
                {
                    AddSubFolder(subFolderName, subFolder, folderProject.ProjectGuid, newFolderPath);
                }
            }

            void RemoveFolder(RawSolution.Project folderProject, string? folderPath)
            {
                // Remove sub-folders
                if (folderProjectChildren.TryGetValue(folderProject.ProjectGuid, out List<RawSolution.Project> childFolderProjects))
                {
                    foreach (RawSolution.Project childFolderProject in childFolderProjects)
                    {
                        RemoveFolder(childFolderProject, AppendFolderPath(folderPath, childFolderProject.Name));
                    }
                }

                // Add all folder files to missing files
                RawSolution.Section? solutionItemsSection = GetSolutionItemsSection(folderProject);
                if (solutionItemsSection != null)
                {
                    foreach (string removedFilePath in solutionItemsSection.ValuesByKey.Keys)
                        AddMissingFile(removedFilePath);
                }

                // Remove folder
                Log(SolutionChangeType.Remove, SolutionObjectType.Folder, folderProject.Name, folderPath);

                rawSolution.Projects.Remove(folderProject);
                nestedProjectsSection?.RemoveValue(folderProject.ProjectGuid.ToRawFormat());
            }

            void CleanFiles(RawSolution.Project folderProject, ISolutionFolder folder)
            {
                RawSolution.Section? solutionItemsSection = GetSolutionItemsSection(folderProject);
                if (solutionItemsSection == null)
                    return;

                // Get removed files
                List<string> removedFilePaths = new List<string>();
                foreach (string existingFilePath in solutionItemsSection.ValuesByKey.Keys)
                {
                    if (!folder.FilePaths.Contains(existingFilePath))
                        removedFilePaths.Add(existingFilePath);
                }

                // If all files are removed, remove section.
                if (solutionItemsSection.ValuesByKey.Count == removedFilePaths.Count)
                {
                    foreach (string removedFilePath in removedFilePaths)
                        AddMissingFile(removedFilePath);
                    folderProject.Sections.Remove(solutionItemsSection);

                    return;
                }

                // Else, only clean file entries.
                foreach (string removedFilePath in removedFilePaths)
                {
                    AddMissingFile(removedFilePath);
                    solutionItemsSection.RemoveValue(removedFilePath);
                }
            }

            void AddMissingFile(string missingFilePath)
            {
                if (missingFilePaths.ContainsKey(missingFilePath))
                    missingFilePaths[missingFilePath]++;
                else
                    missingFilePaths.Add(missingFilePath, 1);
            }
        }

        private void CleanExistingProjects(RawSolution rawSolution, ISolution solution,
            Dictionary<ISolutionFolder, RawSolution.Project> existingFolders,
            Dictionary<RawSolution.Project, string> rawFolderPaths,
            Dictionary<string, Guid> projectGuidByPath,
            Dictionary<Guid, string> projectPathByGuid)
        {
            RawSolution.Section? nestedProjectsSection = GetGlobalSection(rawSolution, RawKeyword.NestedProjects);

            var projects = new Dictionary<string, ISolutionProject>(_fileSystem.PathComparer);
            var projectFolders = new Dictionary<ISolutionProject, ISolutionFolder>();

            GetProjectHierarchy(solution.Root);

            void GetProjectHierarchy(ISolutionFolder folder)
            {
                foreach ((string projectPath, ISolutionProject project) in folder.Projects)
                {
                    projects.Add(projectPath, project);
                    projectFolders.Add(project, folder);
                }

                foreach (ISolutionFolder subFolder in folder.SubFolders.Values)
                    GetProjectHierarchy(subFolder);
            }

            List<(RawSolution.Project, ProjectType?)> removedProjects = new List<(RawSolution.Project, ProjectType?)>();
            foreach (RawSolution.Project rawProject in rawSolution.Projects)
            {
                if (rawProject.TypeGuid == ProjectTypes.FolderGuid)
                    continue;

                if (projects.TryGetValue(rawProject.Path, out ISolutionProject project))
                {
                    projectGuidByPath.Add(rawProject.Path, rawProject.ProjectGuid);
                    projectPathByGuid.Add(rawProject.ProjectGuid, rawProject.Path);
                    
                    ISolutionFolder newFolder = projectFolders[project];
                    RawSolution.Project? newFolderProject = newFolder != solution.Root ? existingFolders[newFolder] : null;

                    // If project kept the same parent, skip it.
                    Guid? parentGuid = GetParentGuid(rawProject, nestedProjectsSection);
                    if (parentGuid == newFolderProject?.ProjectGuid)
                        continue;

                    // Else, set new parent GUID or remove if none.
                    string? folderPath = null;
                    if (newFolderProject != null)
                    {
                        folderPath = rawFolderPaths[newFolderProject];

                        nestedProjectsSection ??= GetOrAddGlobalSection(rawSolution, RawKeyword.NestedProjects);
                        nestedProjectsSection.SetOrAddValue(rawProject.ProjectGuid.ToRawFormat(), newFolderProject.ProjectGuid.ToRawFormat());
                    }
                    else
                    {
                        nestedProjectsSection?.RemoveValue(rawProject.ProjectGuid.ToRawFormat());
                    }

                    Log(SolutionChangeType.Move, rawProject.Path, folderPath, project.Type);

                    continue;
                }

                ProjectType? projectType = ProjectTypes.FromGuidAndExtension(rawProject.TypeGuid, _fileSystem.GetProjectExtension(rawProject.Path));
                removedProjects.Add((rawProject, projectType));
            }

            foreach ((RawSolution.Project removedProject, ProjectType? projectType) in removedProjects)
            {
                Log(SolutionChangeType.Remove, removedProject.Path, null, projectType);

                rawSolution.Projects.Remove(removedProject);
                nestedProjectsSection?.RemoveValue(removedProject.ProjectGuid.ToRawFormat());
            }
        }

        private void CleanExistingSolutionConfigurationPlatforms(RawSolution rawSolution, ISolution solution,
            HashSet<string> removedSolutionConfigs)
        {
            RawSolution.Section? solutionConfigsSection = GetGlobalSection(rawSolution, RawKeyword.SolutionConfigurationPlatforms);
            if (solutionConfigsSection is null)
                return;

            foreach (string solutionConfigurationFullName in solutionConfigsSection.ValuesByKey.Keys)
            {
                if (solution.ConfigurationPlatforms.Any(x => x.FullName == solutionConfigurationFullName))
                    continue;

                Log(SolutionChangeType.Remove, SolutionObjectType.ConfigurationPlatform, solutionConfigurationFullName, null);
                removedSolutionConfigs.Add(solutionConfigurationFullName);
            }

            foreach (string removedConfigFullName in removedSolutionConfigs)
                solutionConfigsSection.RemoveValue(removedConfigFullName);
        }

        private void CleanExistingProjectConfigurationPlatforms(RawSolution rawSolution, ISolution solution,
            HashSet<string> removedSolutionConfigs,
            Dictionary<Guid, string> projectPathByGuids)
        {
            RawSolution.Section? projectConfigsSection = GetGlobalSection(rawSolution, RawKeyword.ProjectConfigurationPlatforms);
            if (projectConfigsSection is null)
                return;

            List<string> removedConfigKeys = new List<string>();
            foreach ((string configKey, string projectConfigurationPlatformFullName) in projectConfigsSection.ValuesByKey)
            {
                string[] splitKey = configKey.Split('.');

                if (!RawGuid.TryParse(splitKey[0], out Guid projectGuid))
                    throw new FormatException($"Failed to parse GUID in key \"{splitKey}\".");

                string solutionConfigurationPlatformFullName = splitKey[1];

                // Is the matching solution configuration-platform has been removed, remove directly.
                if (removedSolutionConfigs.Contains(solutionConfigurationPlatformFullName))
                {
                    removedConfigKeys.Add(configKey);
                    continue;
                }

                // If solution configuration-platform not exist yet, nothing to clean.
                ISolutionConfigurationPlatform? solutionConfigurationPlatform = solution.ConfigurationPlatforms.FirstOrDefault(x => x.FullName == solutionConfigurationPlatformFullName);
                if (solutionConfigurationPlatform is null)
                    continue;

                // If project and project context still exist...
                if (projectPathByGuids.TryGetValue(projectGuid, out string projectPath))
                {
                    if (solutionConfigurationPlatform.ProjectContexts.TryGetValue(projectPath, out SolutionProjectContext projectContext))
                    {
                        // If project context configuration-platform don't match anymore, replace value.
                        string[] splitNames = projectConfigurationPlatformFullName.Split('|');
                        string projectConfigurationName = splitNames[0];
                        string projectPlatformName = splitNames[1];

                        bool reconfigured = false;
                        if (projectConfigurationName != projectContext.ConfigurationName && projectPlatformName != projectContext.PlatformName)
                        {
                            projectConfigsSection.ReplaceValue(configKey, projectContext.ConfigurationName + '|' + projectContext.PlatformName);
                            reconfigured = true;
                        }

                        string type = splitKey.Skip(2).Join('.');

                        // If the key still match its properties, nothing to clean.
                        switch (type)
                        {
                            case RawKeyword.ActiveCfg:
                                if (reconfigured)
                                    Log(SolutionChangeType.Edit, SolutionObjectType.ProjectContext, projectPath, solutionConfigurationPlatformFullName);
                                continue;
                            case RawKeyword.Build0:
                                if (projectContext.Build)
                                    continue;
                                break;
                            case RawKeyword.Deploy0:
                                if (projectContext.Deploy)
                                    continue;
                                break;
                            default:
                                continue;
                        }
                    }

                    // Else remove it.
                    Log(SolutionChangeType.Remove, SolutionObjectType.ProjectContext, projectPath, solutionConfigurationPlatformFullName);
                }
                else
                {
                    Log(SolutionChangeType.Remove, SolutionObjectType.ProjectContext, projectGuid.ToString(), solutionConfigurationPlatformFullName);
                }

                removedConfigKeys.Add(configKey);
            }

            foreach (string removedConfigKey in removedConfigKeys)
                projectConfigsSection.RemoveValue(removedConfigKey);
        }

        private void CleanExistingSharedProjectDependencies(RawSolution rawSolution, ISolution solution,
            Dictionary<Guid, string> projectPathByGuids)
        {
            RawSolution.Section? sharedProjectsSection = GetGlobalSection(rawSolution, RawKeyword.SharedMSBuildProjectFiles);
            if (sharedProjectsSection is null)
                return;

            IReadOnlyDictionary<string, ISolutionProject>? allProjectsByPath = null;
            List<string> removedSharedProjectItems = new List<string>();
            foreach ((string itemKey, string itemValue) in sharedProjectsSection.ValuesByKey)
            {
                string[] splitKey = itemKey.Split('*');
                if (splitKey.Length != 3)
                    continue;

                string itemKeyword = splitKey[2];
                if (!itemKeyword.Equals(RawKeyword.SharedMSBuildProjectFilesSharedItemsImports, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!RawGuid.TryParse(splitKey[1], out Guid projectGuid))
                    throw new FormatException($"Failed to parse GUID in key \"{splitKey}\".");

                string sharedProjectPath;
                string sharedItemsFilePath = splitKey[0];
                string? sharedItemsExtension = _fileSystem.GetExtension(sharedItemsFilePath);

                if (sharedItemsExtension == ProjectFileExtensions.Extensions[ProjectFileExtension.Projitems])
                {
                    sharedProjectPath = _fileSystem.ChangeFileExtension(sharedItemsFilePath, ProjectFileExtensions.Extensions[ProjectFileExtension.Shproj]);
                }
                else if (sharedItemsExtension == ProjectFileExtensions.Extensions[ProjectFileExtension.Vcxitems])
                {
                    sharedProjectPath = sharedItemsFilePath;
                }
                else
                {
                    throw new NotSupportedException($"Shared items files using extension {sharedItemsExtension} are unknown.");
                }
                
                // If project is not existing anymore, remove the item.
                if (!projectPathByGuids.TryGetValue(projectGuid, out string projectPath))
                {
                    removedSharedProjectItems.Add(itemKey);
                    continue;
                }

                allProjectsByPath ??= solution.Root.GetAllProjects();
                if (!allProjectsByPath.TryGetValue(projectPath, out ISolutionProject solutionProject))
                {
                    removedSharedProjectItems.Add(itemKey);
                    continue;
                }

                if (itemValue == RawKeyword.SharedItemsImportsDefaultValue || itemValue == RawKeyword.SharedItemsImportsDotNetSdkValue)
                {
                    if (_solutionDirectoryPath is null)
                        throw SharedProjectsUnsupportedException();

                    string projectDirectoryPath = _fileSystem.GetParentDirectoryPath(projectPath)!;
                    string projectAbsolutePath = _fileSystem.MakeAbsolutePath(_solutionDirectoryPath, projectDirectoryPath);
                    string sharedProjectDependencyPath = _fileSystem.MoveRelativePathRoot(sharedProjectPath, _solutionDirectoryPath, projectAbsolutePath);

                    // If project is not dependent of the shared project anymore, remove the item.
                    if (!solutionProject.ProjectDependencies.Contains(sharedProjectDependencyPath, _fileSystem.PathComparer))
                    {
                        Log(SolutionChangeType.Remove, SolutionObjectType.SharedProject, sharedProjectPath, projectPath);
                        removedSharedProjectItems.Add(itemKey);
                    }
                }
                else if (itemValue == RawKeyword.SharedItemsImportsShprojValue || itemValue == RawKeyword.SharedItemsImportsVcxitemsValue)
                {
                    // If project is not a shared project anymore, remove the item.
                    if (solutionProject.Type != ProjectType.Shared && solutionProject.Type != ProjectType.SharedItems)
                    {
                        Log(SolutionChangeType.Remove, SolutionObjectType.SharedProject, sharedProjectPath, projectPath);
                        removedSharedProjectItems.Add(itemKey);
                    }
                }
                else
                {
                    // On any unsupported value, remove the item.
                    Log(SolutionChangeType.Remove, SolutionObjectType.SharedProject, sharedProjectPath, projectPath);
                    removedSharedProjectItems.Add(itemKey);
                }
            }

            foreach (string removedSharedProjectItem in removedSharedProjectItems)
                sharedProjectsSection.RemoveValue(removedSharedProjectItem);
        }

        static private RawSolution.Project? GetRootFileFolderProject(RawSolution rawSolution)
            => rawSolution.Projects.FirstOrDefault(x => x.Name == RawKeyword.DefaultRootFileFolderName);

        static private RawSolution.Project GetOrAddRootFileFolderProject(RawSolution rawSolution)
        {
            RawSolution.Project? project = GetRootFileFolderProject(rawSolution);
            if (project != null)
                return project;

            project = new RawSolution.Project(ProjectTypes.FolderGuid, RawKeyword.DefaultRootFileFolderName, RawKeyword.DefaultRootFileFolderName, Guid.NewGuid());
            rawSolution.Projects.Add(project);
            return project;
        }

        static private RawSolution.Section? GetGlobalSection(RawSolution rawSolution, string parameter)
            => rawSolution.GlobalSections.FirstOrDefault(x => x.Parameter == parameter);

        static private RawSolution.Section GetOrAddGlobalSection(RawSolution rawSolution, string parameter)
        {
            RawSolution.Section? section = GetGlobalSection(rawSolution, parameter);
            if (section != null)
                return section;

            section = new RawSolution.Section(RawKeyword.GlobalSection, parameter, RawKeyword.GetGlobalSectionArgument(parameter));
            rawSolution.GlobalSections.Add(section);
            return section;
        }

        static private RawSolution.Section? GetSolutionItemsSection(RawSolution.Project project)
            => project.Sections.FirstOrDefault(x => x.Parameter == RawKeyword.SolutionItems);

        static private RawSolution.Section GetOrAddSolutionItemsSection(RawSolution.Project project)
        {
            RawSolution.Section? section = GetSolutionItemsSection(project);
            if (section != null)
                return section;

            section = new RawSolution.Section(RawKeyword.ProjectSection, RawKeyword.SolutionItems, RawKeyword.PreProject);
            project.Sections.Add(section);
            return section;
        }

        static private Guid? GetParentGuid(RawSolution.Project rawProject, RawSolution.Section? nestedProjectsSection)
        {
            if (nestedProjectsSection != null && nestedProjectsSection.ValuesByKey.TryGetValue(rawProject.ProjectGuid.ToRawFormat(), out string parentRawGuid))
            {
                if (!RawGuid.TryParse(parentRawGuid, out Guid parentGuid))
                    throw new FormatException($"Failed to parse GUID {{{parentRawGuid}}} of project \"{rawProject.Path}\" parent.");

                return parentGuid;
            }

            return null;
        }

        private void Log(SolutionChangeType changeType, SolutionObjectType objectType, string objectName, string? targetName)
            => Log(changeType, objectType, objectName, targetName, null);
        private void Log(SolutionChangeType changeType, string projectPath, string? solutionFolderPath, ProjectType? projectType)
            => Log(changeType, SolutionObjectType.Project, projectPath, solutionFolderPath, projectType);

        private void Log(SolutionChangeType changeType, SolutionObjectType objectType, string objectName, string? targetName, object? objectTag)
        {
            SolutionChange change = new SolutionChange(changeType, objectType, objectName, targetName)
            {
                ObjectTag = objectTag
            };

            if (objectType == SolutionObjectType.Folder)
            {
                Logger?.Log(LogLevel.Trace, change.GetMessage(startWithBullet: true, _fileSystem));
            }
            else
            {
                _changes.Add(change);
                Logger?.Log(LogLevel, change.GetMessage(startWithBullet: true, _fileSystem));
            }
        }

        private string AppendFolderPath(string? folderPath, string subFolderName) => folderPath is null ? subFolderName : folderPath + '/' + subFolderName;
    }
}