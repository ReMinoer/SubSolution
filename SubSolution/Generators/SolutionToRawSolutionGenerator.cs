using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;
using SubSolution.Raw;

namespace SubSolution.Generators
{
    public enum IssueLevel
    {
        Error,
        Warning
    }

    public class Issue
    {
        public IssueLevel Level { get; }
        public string Message { get; }

        public Issue(IssueLevel level, string message)
        {
            Level = level;
            Message = message;
        }
    }

    public class RawSolutionToSolutionGenerator
    {
        private readonly ISubSolutionFileSystem _fileSystem;
        private readonly ISolutionProjectReader _projectReader;

        public RawSolutionToSolutionGenerator(ISubSolutionFileSystem fileSystem, ISolutionProjectReader projectReader)
        {
            _fileSystem = fileSystem;
            _projectReader = projectReader;
        }

        public async Task<(ISolution, List<Issue>)> GenerateAsync(IRawSolution rawSolution, string solutionPath)
        {
            List<Issue> issues = new List<Issue>();

            Dictionary<Guid, IRawSolutionProject> projectsByGuid = rawSolution.Projects.ToDictionary(x => Guid.Parse(x.Arguments[2]), x => x);

            var childrenGraph = new Dictionary<Guid, List<Guid>>();
            var parentGraph = new Dictionary<Guid, Guid>();
            GetProjectParentingGraph(rawSolution, issues, childrenGraph, parentGraph);

            Solution solution = new Solution(solutionPath, _fileSystem);
            await FillHierarchy(solution, projectsByGuid, childrenGraph, parentGraph);

            return (solution, issues);
        }

        private void GetProjectParentingGraph(IRawSolution rawSolution, List<Issue> issues, Dictionary<Guid, List<Guid>> childrenGraph, Dictionary<Guid, Guid> parentGraph)
        {
            IRawSolutionSection? nestedProjectSection = rawSolution.GlobalSections.FirstOrDefault(x => x.Parameter == RawKeyword.NestedProjects);
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

        private async Task FillHierarchy(Solution solution, Dictionary<Guid, IRawSolutionProject> projectsByGuid,
            Dictionary<Guid, List<Guid>> childrenGraph, Dictionary<Guid, Guid> parentGraph)
        {
            IRawSolutionProject? rootFilesFolderProject = projectsByGuid
                .Where(x => x.Value.TypeGuid == ProjectTypeGuid.Folder
                    && x.Value.Arguments[0] == RawKeyword.DefaultRootFileFolderName
                    && !parentGraph.TryGetValue(x.Key, out _))
                .Select(x => x.Value)
                .FirstOrDefault();

            if (rootFilesFolderProject != null)
                FillFolderFiles(solution.Root, rootFilesFolderProject);

            IEnumerable<Guid> rootProjectGuids = projectsByGuid.Keys.Except(parentGraph.Keys);
            await FillSubFolder(solution, solution.Root, rootProjectGuids, projectsByGuid, childrenGraph);
        }

        private async Task FillSubFolder(Solution solution, Solution.Folder folder, IEnumerable<Guid> childrenGuids,
            Dictionary<Guid, IRawSolutionProject> projectsByGuid, Dictionary<Guid, List<Guid>> childrenGraph)
        {
            foreach (Guid childGuid in childrenGuids)
            {
                IRawSolutionProject childProject = projectsByGuid[childGuid];
                if (childProject.TypeGuid == ProjectTypeGuid.Folder)
                {
                    Solution.Folder subFolder = folder.GetOrAddSubFolder(childProject.Arguments[0]);
                    FillFolderFiles(subFolder, childProject);

                    if (!childrenGraph.TryGetValue(childGuid, out List<Guid> subFolderChildrenGuid))
                        continue;

                    await FillSubFolder(solution, subFolder, subFolderChildrenGuid, projectsByGuid, childrenGraph);
                }
                else
                {
                    string relativeProjectPath = childProject.Arguments[1];
                    string absoluteProjectPath = _fileSystem.Combine(solution.OutputDirectory, relativeProjectPath);

                    ISolutionProject solutionProject = await _projectReader.ReadAsync(absoluteProjectPath);
                    folder.AddProject(relativeProjectPath, solutionProject);
                }
            }
        }

        private void FillFolderFiles(Solution.Folder folder, IRawSolutionProject rawFolderProject)
        {
            IRawSolutionSection? solutionItemsSection = rawFolderProject.Sections.FirstOrDefault(x => x.Parameter == RawKeyword.SolutionItems);
            if (solutionItemsSection is null)
                return;

            foreach (string filePath in solutionItemsSection.ValuesByKey.Keys)
                folder.AddFile(filePath);
        }
    }

    public class SolutionToRawSolutionGenerator
    {
        private readonly ISubSolutionFileSystem _fileSystem;

        public SolutionToRawSolutionGenerator(ISubSolutionFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public RawSolution Generate(ISolution solution)
        {
            RawSolution rawSolution = new RawSolution
            {
                SlnFormatVersion = new Version(12, 0),
                MajorVisualStudioVersion = 16,
                VisualStudioVersion = new Version(16, 0, 31424, 327),
                MinimumVisualStudioVersion = new Version(10, 0, 40219, 1)
            };

            Dictionary<string, string> projectGuids = new Dictionary<string, string>();

            GenerateFolderContent(rawSolution, projectGuids, solution.Root);
            GenerateConfigurations(rawSolution, solution, projectGuids);
            return rawSolution;
        }

        private void GenerateFolderContent(RawSolution rawSolution, Dictionary<string, string> projectGuids, ISolutionFolder solutionFolder, RawSolution.Project? folderProject = null)
        {
            foreach (string projectPath in solutionFolder.Projects.Keys)
            {
                RawSolution.Project project = GetOrCreateProject(rawSolution, folderProject, ProjectTypeGuid.CSharp, _fileSystem.GetFileNameWithoutExtension(projectPath), projectPath, NewGuid());
                projectGuids.Add(projectPath, project.Arguments[2]);
            }

            foreach ((string name, ISolutionFolder subFolder) in solutionFolder.SubFolders.Select(x => (x.Key, x.Value)))
            {
                RawSolution.Project subFolderProject = GetOrCreateProject(rawSolution, folderProject, ProjectTypeGuid.Folder, name, name, NewGuid());
                GenerateFolderContent(rawSolution, projectGuids, subFolder, subFolderProject);
            }

            if (solutionFolder.FilePaths.Count > 0)
            {
                RawSolution.Section solutionItemsSection = GetOrAddSolutionItemsSection(folderProject ?? GetOrAddRootFileFolderProject(rawSolution));

                foreach (string filePath in solutionFolder.FilePaths)
                    solutionItemsSection.SetOrAddValue(filePath, filePath);
            }
        }

        static private string NewGuid() => $"{{{Guid.NewGuid().ToString().ToUpper()}}}";

        static private RawSolution.Project GetOrCreateProject(RawSolution rawSolution, RawSolution.Project? folderProject, Guid typeGuid, params string[] arguments)
        {
            RawSolution.Project? project = rawSolution.Projects.FirstOrDefault(x => x.TypeGuid == typeGuid && x.Arguments.SequenceEqual(arguments));
            if (project is null)
            {
                project = new RawSolution.Project(typeGuid, arguments);
                rawSolution.Projects.Add(project);
            }
            
            if (folderProject != null)
                GetOrAddGlobalSection(rawSolution, RawKeyword.NestedProjects, RawKeyword.PreSolution).SetOrAddValue(project.Arguments[2], folderProject.Arguments[2]);

            return project;
        }

        static private void GenerateConfigurations(RawSolution rawSolution, ISolution solution, Dictionary<string, string> projectGuids)
        {
            if (solution.ConfigurationPlatforms.Count == 0)
                return;

            RawSolution.Section solutionConfigsSection = GetOrAddGlobalSection(rawSolution, RawKeyword.SolutionConfigurationPlatforms, RawKeyword.PreSolution);
            RawSolution.Section projectConfigsSection = GetOrAddGlobalSection(rawSolution, RawKeyword.ProjectConfigurationPlatforms, RawKeyword.PostSolution);

            foreach (ISolutionConfigurationPlatform configurationPlatform in solution.ConfigurationPlatforms)
            {
                string solutionConfigurationFullName = configurationPlatform.ConfigurationName + '|' + configurationPlatform.PlatformName;

                solutionConfigsSection.SetOrAddValue(solutionConfigurationFullName, solutionConfigurationFullName);

                foreach ((string projectPath, SolutionProjectContext projectContext) in configurationPlatform.ProjectContexts)
                {
                    string prefix = $"{projectGuids[projectPath]}.{solutionConfigurationFullName}.";
                    string projectConfigurationFullName = projectContext.ConfigurationName + '|' + projectContext.PlatformName;

                    projectConfigsSection.SetOrAddValue(prefix + RawKeyword.ActiveCfg, projectConfigurationFullName);
                    if (projectContext.Build)
                        projectConfigsSection.SetOrAddValue(prefix + RawKeyword.Build0, projectConfigurationFullName);
                    if (projectContext.Deploy)
                        projectConfigsSection.SetOrAddValue(prefix + RawKeyword.Deploy0, projectConfigurationFullName);
                }
            }
        }

        static private RawSolution.Project GetOrAddRootFileFolderProject(RawSolution rawSolution)
        {
            RawSolution.Project? project = rawSolution.Projects.FirstOrDefault(x => x.Arguments[0] == RawKeyword.DefaultRootFileFolderName);
            if (project != null)
                return project;

            project = new RawSolution.Project(ProjectTypeGuid.Folder, RawKeyword.DefaultRootFileFolderName, RawKeyword.DefaultRootFileFolderName, $"{{{Guid.NewGuid().ToString().ToUpper()}}}");
            rawSolution.Projects.Add(project);
            return project;
        }

        static private RawSolution.Section GetOrAddGlobalSection(RawSolution rawSolution, string parameter, params string[] arguments)
        {
            RawSolution.Section? section = rawSolution.GlobalSections.FirstOrDefault(x => x.Parameter == parameter);
            if (section != null)
                return section;

            section = new RawSolution.Section(RawKeyword.GlobalSection, parameter, arguments);
            rawSolution.GlobalSections.Add(section);
            return section;
        }

        static private RawSolution.Section GetOrAddSolutionItemsSection(RawSolution.Project project)
        {
            RawSolution.Section? section = project.Sections.FirstOrDefault(x => x.Parameter == RawKeyword.SolutionItems);
            if (section != null)
                return section;

            section = new RawSolution.Section(RawKeyword.ProjectSection, RawKeyword.SolutionItems, RawKeyword.PreProject);
            project.Sections.Add(section);
            return section;
        }
    }
}