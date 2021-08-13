using System;
using System.Collections.Generic;
using System.Linq;
using SubSolution.FileSystems;
using SubSolution.Raw;

namespace SubSolution.Generators
{
    public class RawSolutionGenerator : ISolutionGenerator<RawSolution>
    {
        static private readonly Guid FolderGuid = Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8");
        static private readonly Guid CSharpProjectGuid = Guid.Parse("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");

        private const string PreSolutionKeyword = "preSolution";
        private const string PostSolutionKeyword = "postSolution";

        private readonly ISubSolutionFileSystem _fileSystem;

        public RawSolutionGenerator(ISubSolutionFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public RawSolution Generate(ISolutionOutput solutionOutput)
        {
            RawSolution rawSolution = new RawSolution
            {
                SlnFormatVersion = new Version(12, 0),
                MajorVisualStudioVersion = 16,
                VisualStudioVersion = new Version(16, 0, 31424, 327),
                MinimumVisualStudioVersion = new Version(10, 0, 40219, 1)
            };

            Dictionary<string, string> projectGuids = GenerateFolderContent(rawSolution, solutionOutput.Root);
            GenerateConfigurations(rawSolution, solutionOutput, projectGuids);
            return rawSolution;
        }

        private Dictionary<string, string> GenerateFolderContent(RawSolution rawSolution, ISolutionFolder solutionFolder, RawSolution.Project? folderProject = null)
        {
            Dictionary<string, string> projectGuids = new Dictionary<string, string>();

            foreach (string projectPath in solutionFolder.ProjectPaths)
            {
                RawSolution.Project project = GetOrCreateProject(rawSolution, folderProject, CSharpProjectGuid, _fileSystem.GetFileNameWithoutExtension(projectPath), projectPath, NewGuid());
                projectGuids.Add(projectPath, project.Arguments[2]);
            }

            foreach ((string name, ISolutionFolder subFolder) in solutionFolder.SubFolders.Select(x => (x.Key, x.Value)))
            {
                RawSolution.Project subFolderProject = GetOrCreateProject(rawSolution, folderProject, FolderGuid, name, name, NewGuid());
                GenerateFolderContent(rawSolution, subFolder, subFolderProject);
            }

            if (solutionFolder.FilePaths.Count > 0)
            {
                RawSolution.Section solutionItemsSection = GetOrAddSolutionItemsSection(folderProject ?? GetOrAddRootFileFolderProject(rawSolution));

                foreach (string filePath in solutionFolder.FilePaths)
                    solutionItemsSection.SetOrAddValue(filePath, filePath);
            }

            return projectGuids;
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
                GetOrAddGlobalSection(rawSolution, "NestedProjects", PreSolutionKeyword).SetOrAddValue(project.Arguments[2], folderProject.Arguments[2]);

            return project;
        }

        static private void GenerateConfigurations(RawSolution rawSolution, ISolutionOutput solutionOutput, Dictionary<string, string> projectGuids)
        {
            if (solutionOutput.Configurations.Count == 0)
                return;

            RawSolution.Section solutionConfigsSection = GetOrAddGlobalSection(rawSolution, "SolutionConfigurationPlatforms", PreSolutionKeyword);
            RawSolution.Section projectConfigsSection = GetOrAddGlobalSection(rawSolution, "ProjectConfigurationPlatforms", PostSolutionKeyword);

            foreach (ISolutionConfiguration configuration in solutionOutput.Configurations)
            {
                string fullConfigName = configuration.Configuration + '|' + configuration.Platform;

                solutionConfigsSection.SetOrAddValue(fullConfigName, fullConfigName);

                foreach (ISolutionProjectContext projectContext in configuration.ProjectContexts)
                {
                    string prefix = $"{projectGuids[projectContext.Project.Path]}.{projectContext.Configuration}|{projectContext.Platform}.";

                    projectConfigsSection.SetOrAddValue(prefix + "ActiveCfg", fullConfigName);
                    if (projectContext.Build)
                        projectConfigsSection.SetOrAddValue(prefix + "Build.0", fullConfigName);
                    if (projectContext.Deploy)
                        projectConfigsSection.SetOrAddValue(prefix + "Deploy.0", fullConfigName);
                }
            }
        }

        static private RawSolution.Project GetOrAddRootFileFolderProject(RawSolution rawSolution)
        {
            RawSolution.Project? project = rawSolution.Projects.FirstOrDefault(x => x.Arguments[0] == "Solution Items");
            if (project != null)
                return project;

            project = new RawSolution.Project(FolderGuid, "Solution Items", "Solution Items", $"{{{Guid.NewGuid().ToString().ToUpper()}}}");
            rawSolution.Projects.Add(project);
            return project;
        }

        static private RawSolution.Section GetOrAddGlobalSection(RawSolution rawSolution, string name, params string[] arguments)
        {
            RawSolution.Section? section = rawSolution.GlobalSections.FirstOrDefault(x => x.Parameter == name);
            if (section != null)
                return section;

            section = new RawSolution.Section("GlobalSection", name, arguments);
            rawSolution.GlobalSections.Add(section);
            return section;
        }

        static private RawSolution.Section GetOrAddSolutionItemsSection(RawSolution.Project project)
        {
            RawSolution.Section? section = project.Sections.FirstOrDefault(x => x.Parameter == "SolutionItems");
            if (section != null)
                return section;

            section = new RawSolution.Section("ProjectSection", "SolutionItems", "preProject");
            project.Sections.Add(section);
            return section;
        }
    }
}