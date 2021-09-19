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

        public async Task<(ManualSolution, List<Issue>)> ConvertAsync(IRawSolution rawSolution, string solutionPath)
        {
            List<Issue> issues = new List<Issue>();

            Dictionary<Guid, IRawSolutionProject> projectsByGuid = rawSolution.Projects.ToDictionary(x => Guid.Parse(x.Arguments[2]), x => x);
            Dictionary<Guid, string> projectPathsByGuid = rawSolution.Projects.ToDictionary(x => Guid.Parse(x.Arguments[2]), x => x.Arguments[1]);

            var childrenGraph = new Dictionary<Guid, List<Guid>>();
            var parentGraph = new Dictionary<Guid, Guid>();
            FillProjectParentingGraphs(rawSolution, issues, childrenGraph, parentGraph);

            ManualSolution solution = new ManualSolution(solutionPath, _fileSystem);
            await FillHierarchy(solution, projectsByGuid, childrenGraph, parentGraph);

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
                if (!Guid.TryParse(childGuidText[1..^1], out Guid childGuid))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to parse GUID {childGuidText} in {RawKeyword.NestedProjects} section."));
                    continue;
                }

                if (!Guid.TryParse(parentGuidText[1..^1], out Guid parentGuid))
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

        private async Task FillHierarchy(ManualSolution solution, Dictionary<Guid, IRawSolutionProject> projectsByGuid,
            Dictionary<Guid, List<Guid>> childrenGraph, Dictionary<Guid, Guid> parentGraph)
        {
            IRawSolutionProject? rootFilesFolderProject = projectsByGuid
                .Where(x => x.Value.TypeGuid == RawProjectTypeGuid.Folder
                    && x.Value.Arguments[0] == RawKeyword.DefaultRootFileFolderName
                    && !parentGraph.TryGetValue(x.Key, out _))
                .Select(x => x.Value)
                .FirstOrDefault();

            if (rootFilesFolderProject != null)
                FillFolderFiles(solution.Root, rootFilesFolderProject);

            IEnumerable<Guid> rootProjectGuids = projectsByGuid.Where(x => x.Value != rootFilesFolderProject).Select(x => x.Key).Except(parentGraph.Keys);
            await FillSubFolder(solution, solution.Root, rootProjectGuids, projectsByGuid, childrenGraph);
        }

        private async Task FillSubFolder(ManualSolution solution, ManualSolution.Folder folder, IEnumerable<Guid> childrenGuids,
            Dictionary<Guid, IRawSolutionProject> projectsByGuid, Dictionary<Guid, List<Guid>> childrenGraph)
        {
            foreach (Guid childGuid in childrenGuids)
            {
                IRawSolutionProject childProject = projectsByGuid[childGuid];
                if (childProject.TypeGuid == RawProjectTypeGuid.Folder)
                {
                    ManualSolution.Folder subFolder = folder.GetOrAddSubFolder(childProject.Arguments[0]);
                    FillFolderFiles(subFolder, childProject);

                    if (!childrenGraph.TryGetValue(childGuid, out List<Guid> subFolderChildrenGuid))
                        continue;

                    await FillSubFolder(solution, subFolder, subFolderChildrenGuid, projectsByGuid, childrenGraph);
                }
                else
                {
                    string relativeProjectPath = childProject.Arguments[1];
                    string absoluteProjectPath = _fileSystem.MakeAbsolutePath(solution.OutputDirectory, relativeProjectPath);

                    ISolutionProject solutionProject = await _projectReader.ReadAsync(absoluteProjectPath);
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
                string projectGuidText = splitKey[0][1..^1];
                string solutionConfigurationPlatformFullName = splitKey[1];

                string[] splitNames = projectConfigurationPlatformFullName.Split('|');
                string projectConfigurationName = splitNames[0];
                string projectPlatformName = splitNames[1];

                if (!Guid.TryParse(projectGuidText, out Guid projectGuid))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to parse GUID {projectGuidText} in {RawKeyword.ActiveCfg} key."));
                    continue;
                }

                if (!projectPathsByGuid.TryGetValue(projectGuid, out string projectPath))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to get project associated to GUID {projectGuidText} found in {RawKeyword.ActiveCfg} key."));
                    continue;
                }

                var solutionProjectContext = new SolutionProjectContext(projectConfigurationName, projectPlatformName);

                configurationPlatformByFullName[solutionConfigurationPlatformFullName].ProjectContexts.Add(projectPath, solutionProjectContext);
                projectContextsByGuidAndSolutionConfigurationPlatforms.Add((projectGuid, solutionConfigurationPlatformFullName), solutionProjectContext);
            }

            foreach (string key in projectConfigurationPlatformsSection.ValuesByKey.Keys.Where(x => !x.EndsWith(RawKeyword.ActiveCfg)))
            {
                string[] splitKey = key.Split('.');
                string projectGuidText = splitKey[0][1..^1];
                string solutionConfigurationPlatformFullName = splitKey[1];
                string type = string.Join('.', splitKey.Skip(2));

                if (!Guid.TryParse(projectGuidText, out Guid projectGuid))
                {
                    issues.Add(new Issue(IssueLevel.Error, $"Failed to parse GUID {projectGuidText} in {RawKeyword.ActiveCfg} key."));
                    continue;
                }

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