using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;
using SubSolution.Raw;

namespace SubSolution.Converters
{
    public class RawSolutionConverter
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProjectReader _projectReader;

        public bool SkipConfigurationPlatforms { get; set; }

        public RawSolutionConverter(IFileSystem fileSystem, IProjectReader projectReader)
        {
            _fileSystem = fileSystem;
            _projectReader = projectReader;
        }

        public async Task<(ManualSolution, List<Issue>)> ConvertAsync(IRawSolution rawSolution, string solutionDirectoryPath, bool skipProjectLoading = false)
        {
            List<Issue> issues = new List<Issue>();

            Dictionary<Guid, IRawSolutionProject> projectsByGuid = rawSolution.Projects.ToDictionary(x => x.ProjectGuid, x => x);
            Dictionary<Guid, string> projectPathsByGuid = rawSolution.Projects.ToDictionary(x => x.ProjectGuid, x => x.Path);

            var childrenGraph = new Dictionary<Guid, List<Guid>>();
            var parentGraph = new Dictionary<Guid, Guid>();
            FillProjectParentingGraphs(rawSolution, issues, childrenGraph, parentGraph);

            ManualSolution solution = new ManualSolution(solutionDirectoryPath, _fileSystem);
            await FillHierarchyAsync(solution, projectsByGuid, childrenGraph, parentGraph, skipProjectLoading);

            if (!SkipConfigurationPlatforms)
                FillConfigurationPlatforms(rawSolution, solution, projectPathsByGuid, issues);

            return (solution, issues);
        }

        private void FillProjectParentingGraphs(IRawSolution rawSolution, List<Issue> issues, Dictionary<Guid, List<Guid>> childrenGraph, Dictionary<Guid, Guid> parentGraph)
        {
            IRawSolutionSection? nestedProjectSection = GetGlobalSection(rawSolution, RawKeyword.NestedProjects);
            if (nestedProjectSection == null)
                return;

            foreach ((string childGuidText, string parentGuidText) in nestedProjectSection.ValuesByKey)
            {
                if (!RawGuid.TryParse(childGuidText, out Guid childGuid))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to parse GUID {childGuidText} in {RawKeyword.NestedProjects} section."));
                    continue;
                }

                if (!RawGuid.TryParse(parentGuidText, out Guid parentGuid))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to parse GUID {parentGuidText} in {RawKeyword.NestedProjects} section."));
                    continue;
                }

                parentGraph.Add(childGuid, parentGuid);

                if (!childrenGraph.TryGetValue(parentGuid, out List<Guid> childrenGuids))
                {
                    childrenGuids = new List<Guid>();
                    childrenGraph.Add(parentGuid, childrenGuids);
                }

                childrenGuids.Add(childGuid);
            }
        }

        private async Task FillHierarchyAsync(ManualSolution solution, Dictionary<Guid, IRawSolutionProject> projectsByGuid,
            Dictionary<Guid, List<Guid>> childrenGraph, Dictionary<Guid, Guid> parentGraph, bool skipProjectLoading = false)
        {
            IRawSolutionProject? rootFilesFolderProject = projectsByGuid
                .Where(x => x.Value.TypeGuid == ProjectTypes.FolderGuid
                    && x.Value.Name == RawKeyword.DefaultRootFileFolderName
                    && !parentGraph.TryGetValue(x.Key, out _))
                .Select(x => x.Value)
                .FirstOrDefault();

            if (rootFilesFolderProject != null)
                FillFolderFiles(solution.Root, rootFilesFolderProject);

            IEnumerable<Guid> rootProjectGuids = projectsByGuid.Where(x => x.Value != rootFilesFolderProject).Select(x => x.Key).Except(parentGraph.Keys);
            await FillSubFolderAsync(solution, solution.Root, rootProjectGuids, projectsByGuid, childrenGraph, skipProjectLoading);
        }

        private async Task FillSubFolderAsync(ManualSolution solution, ManualSolution.Folder folder, IEnumerable<Guid> childrenGuids,
            Dictionary<Guid, IRawSolutionProject> projectsByGuid, Dictionary<Guid, List<Guid>> childrenGraph, bool skipProjectLoading = false)
        {
            foreach (Guid childGuid in childrenGuids)
            {
                IRawSolutionProject childProject = projectsByGuid[childGuid];
                if (childProject.TypeGuid == ProjectTypes.FolderGuid)
                {
                    ManualSolution.Folder subFolder = folder.GetOrAddSubFolder(childProject.Name);
                    FillFolderFiles(subFolder, childProject);

                    if (!childrenGraph.TryGetValue(childGuid, out List<Guid> subFolderChildrenGuid))
                        continue;

                    await FillSubFolderAsync(solution, subFolder, subFolderChildrenGuid, projectsByGuid, childrenGraph, skipProjectLoading);
                }
                else
                {
                    string relativeProjectPath = childProject.Path;
                    string absoluteProjectPath = _fileSystem.MakeAbsolutePath(solution.OutputDirectoryPath, relativeProjectPath);

                    ISolutionProject solutionProject = skipProjectLoading ? new SolutionProject(childProject.TypeGuid) : await _projectReader.ReadAsync(absoluteProjectPath);
                    folder.AddProject(relativeProjectPath, solutionProject);
                }
            }
        }

        private void FillFolderFiles(ManualSolution.Folder folder, IRawSolutionProject rawFolderProject)
        {
            IRawSolutionSection? solutionItemsSection = rawFolderProject.Sections.FirstOrDefault(x => x.Parameter == RawKeyword.SolutionItems);
            if (solutionItemsSection is null)
                return;

            foreach (string filePath in solutionItemsSection.ValuesByKey.Keys)
                folder.AddFile(filePath);
        }

        private void FillConfigurationPlatforms(IRawSolution rawSolution, ManualSolution solution, Dictionary<Guid, string> projectPathsByGuid, List<Issue> issues)
        {
            IRawSolutionSection? solutionConfigurationPlatformsSection = GetGlobalSection(rawSolution, RawKeyword.SolutionConfigurationPlatforms);
            if (solutionConfigurationPlatformsSection is null)
                return;

            IRawSolutionSection? projectConfigurationPlatformsSection = GetGlobalSection(rawSolution, RawKeyword.ProjectConfigurationPlatforms);
            if (projectConfigurationPlatformsSection is null)
                return;

            Dictionary<string, ManualSolution.ConfigurationPlatform> configurationPlatformByFullName = new Dictionary<string, ManualSolution.ConfigurationPlatform>();

            foreach (string solutionConfigurationPlatformFullName in solutionConfigurationPlatformsSection.ValuesByKey.Keys)
            {
                string[] splitNames = solutionConfigurationPlatformFullName.Split('|');
                string solutionConfigurationName = splitNames[0];
                string solutionPlatformName = splitNames[1];

                var configurationPlatform = new ManualSolution.ConfigurationPlatform(_fileSystem, solutionConfigurationName, solutionPlatformName);

                solution.ConfigurationPlatforms.Add(configurationPlatform);
                configurationPlatformByFullName.Add(solutionConfigurationPlatformFullName, configurationPlatform);
            }

            Dictionary<(Guid, string), SolutionProjectContext> projectContextsByGuidAndSolutionConfigurationPlatforms = new Dictionary<(Guid, string), SolutionProjectContext>();

            foreach ((string key, string projectConfigurationPlatformFullName) in projectConfigurationPlatformsSection.ValuesByKey.Where(x => x.Key.EndsWith(RawKeyword.ActiveCfg)))
            {
                string[] splitKey = key.Split('.');

                if (!RawGuid.TryParse(splitKey[0], out Guid projectGuid))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to parse GUID in {key} key."));
                    continue;
                }
                
                string solutionConfigurationPlatformFullName = splitKey[1];

                string[] splitNames = projectConfigurationPlatformFullName.Split('|');
                string projectConfigurationName = splitNames[0];
                string projectPlatformName = splitNames[1];

                if (!projectPathsByGuid.TryGetValue(projectGuid, out string projectPath))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to get project associated to GUID {projectGuid} found in {RawKeyword.ActiveCfg} key."));
                    continue;
                }

                var solutionProjectContext = new SolutionProjectContext(projectConfigurationName, projectPlatformName);

                configurationPlatformByFullName[solutionConfigurationPlatformFullName].ProjectContexts.Add(projectPath, solutionProjectContext);
                projectContextsByGuidAndSolutionConfigurationPlatforms.Add((projectGuid, solutionConfigurationPlatformFullName), solutionProjectContext);
            }

            foreach (string key in projectConfigurationPlatformsSection.ValuesByKey.Keys.Where(x => !x.EndsWith(RawKeyword.ActiveCfg)))
            {
                string[] splitKey = key.Split('.');

                if (!RawGuid.TryParse(splitKey[0], out Guid projectGuid))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to parse GUID in {key} key."));
                    continue;
                }

                string solutionConfigurationPlatformFullName = splitKey[1];
                string type = string.Join('.', splitKey.Skip(2));

                if (!projectContextsByGuidAndSolutionConfigurationPlatforms.TryGetValue((projectGuid, solutionConfigurationPlatformFullName), out SolutionProjectContext projectContext))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to found {RawKeyword.ActiveCfg} associated to key {key}."));
                    continue;
                }

                switch (type)
                {
                    case RawKeyword.Build0:
                        projectContext.Build = true;
                        break;
                    case RawKeyword.Deploy0:
                        projectContext.Deploy = true;
                        break;
                    default:
                        issues.Add(new Issue(IssueLevel.Error, $"Unknwon type {type} in key {key}."));
                        break;
                }
            }
        }

        private IRawSolutionSection? GetGlobalSection(IRawSolution rawSolution, string parameter) => rawSolution.GlobalSections.FirstOrDefault(x => x.Parameter == parameter);
    }
}