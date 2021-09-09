using System;
using System.Collections.Generic;
using System.Linq;
using SubSolution.FileSystems;
using SubSolution.Raw;

namespace SubSolution.Converters
{
    public class SolutionConverter
    {
        private readonly IFileSystem _fileSystem;

        public SolutionConverter(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public RawSolution Convert(ISolution solution)
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
                RawSolution.Project project = GetOrCreateProject(rawSolution, folderProject, RawProjectTypeGuid.CSharp, _fileSystem.GetFileNameWithoutExtension(projectPath), projectPath, NewGuid());
                projectGuids.Add(projectPath, project.Arguments[2]);
            }

            foreach ((string name, ISolutionFolder subFolder) in solutionFolder.SubFolders.Select(x => (x.Key, x.Value)))
            {
                RawSolution.Project subFolderProject = GetOrCreateProject(rawSolution, folderProject, RawProjectTypeGuid.Folder, name, name, NewGuid());
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

            project = new RawSolution.Project(RawProjectTypeGuid.Folder, RawKeyword.DefaultRootFileFolderName, RawKeyword.DefaultRootFileFolderName, $"{{{Guid.NewGuid().ToString().ToUpper()}}}");
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