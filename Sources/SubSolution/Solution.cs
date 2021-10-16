using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SubSolution.Base;
using SubSolution.FileSystems;

namespace SubSolution
{
    public class Solution : SolutionBase<Solution, Solution.Folder>
    {
        public override sealed Folder Root { get; }

        private readonly List<ConfigurationPlatform> _configurationPlatforms;
        protected override sealed IReadOnlyList<ISolutionConfigurationPlatform> ProtectedConfigurationPlatforms { get; }

        public Solution(string outputDirectoryPath, IFileSystem? fileSystem = null)
            : base(outputDirectoryPath, fileSystem)
        {
            Root = new Folder(this, _fileSystem, _knownPaths);

            _configurationPlatforms = new List<ConfigurationPlatform>();
            ProtectedConfigurationPlatforms = _configurationPlatforms.AsReadOnly();
        }

        public void AddConfigurationPlatform(ConfigurationPlatform configurationPlatform)
        {
            // TODO: Check configurationPlatform have no unexpected projects contexts

            Root.FillConfigurationPlatformWithProjectContexts(configurationPlatform);
            _configurationPlatforms.Add(configurationPlatform);
        }

        public class ConfigurationPlatform : ISolutionConfigurationPlatform
        {
            public string ConfigurationName { get; }
            public string PlatformName { get; }
            public List<string> MatchingProjectConfigurationNames { get; }
            public List<string> MatchingProjectPlatformNames { get; }

            private readonly Dictionary<string, SolutionProjectContext> _projectContexts;
            public IReadOnlyDictionary<string, SolutionProjectContext> ProjectContexts { get; }

            public ConfigurationPlatform(IFileSystem fileSystem, string configurationName, string platformName)
            {
                ConfigurationName = configurationName;
                PlatformName = platformName;

                MatchingProjectConfigurationNames = new List<string>();
                MatchingProjectPlatformNames = new List<string>();

                _projectContexts = new Dictionary<string, SolutionProjectContext>(fileSystem.PathComparer);
                ProjectContexts = new ReadOnlyDictionary<string, SolutionProjectContext>(_projectContexts);
            }

            public void AddProjectContext(string projectPath, ISolutionProject project)
            {
                string? matchingProjectConfiguration = MatchNames(project.Configurations, MatchingProjectConfigurationNames);
                string? matchingProjectPlatform = MatchNames(project.Platforms, MatchingProjectPlatformNames);
                bool isCompleteMatch = matchingProjectConfiguration != null && matchingProjectPlatform != null;

                string? resolvedProjectConfiguration = matchingProjectConfiguration ?? project.Configurations[0];
                string? resolvedProjectPlatform = matchingProjectPlatform ?? project.Platforms[0];

                var solutionProjectContext = new SolutionProjectContext(resolvedProjectConfiguration, resolvedProjectPlatform)
                {
                    Build = project.CanBuild && isCompleteMatch,
                    Deploy = project.AlwaysDeploy || (project.CanDeploy && isCompleteMatch)
                };

                _projectContexts.Add(projectPath, solutionProjectContext);
            }

            public void RemoveProjectContext(string projectPath)
            {
                _projectContexts.Remove(projectPath);
            }

            public void RenameProjectContext(string previousProjectPath, string newProjectPath)
            {
                _projectContexts.Remove(previousProjectPath, out SolutionProjectContext projectContext);
                _projectContexts.Add(newProjectPath, projectContext);
            }

            static private string? MatchNames(IReadOnlyList<string> names, IReadOnlyList<string> matches)
            {
                foreach (string match in matches)
                    foreach (string name in names)
                        if (name.Contains(match, StringComparison.OrdinalIgnoreCase))
                            return name;

                return null;
            }
        }

        public class Folder : SolutionFolderBase<Solution, Folder>
        {
            public Folder(Solution solution, IFileSystem fileSystem, Dictionary<string, Folder> knownPaths)
                : base(solution, fileSystem, knownPaths, () => new Folder(solution, fileSystem, knownPaths))
            {
            }

            protected override void AutoAddProjectContexts(string projectPath, ISolutionProject project)
            {
                foreach (ConfigurationPlatform configurationPlatform in _solution._configurationPlatforms)
                    configurationPlatform.AddProjectContext(projectPath, project);
            }

            protected override void AutoRemoveProjectContexts(string projectPath)
            {
                foreach (ConfigurationPlatform configurationPlatform in _solution._configurationPlatforms)
                    configurationPlatform.RemoveProjectContext(projectPath);
            }

            protected override void AutoRenameProjectContexts(string previousProjectPath, string newProjectPath)
            {
                foreach (ConfigurationPlatform configurationPlatform in _solution._configurationPlatforms)
                    configurationPlatform.RenameProjectContext(previousProjectPath, newProjectPath);
            }

            public void FillConfigurationPlatformWithProjectContexts(ConfigurationPlatform configurationPlatform)
            {
                foreach ((string projectPath, ISolutionProject project) in Projects)
                    if (!configurationPlatform.ProjectContexts.ContainsKey(projectPath))
                        configurationPlatform.AddProjectContext(projectPath, project);

                foreach (Folder subFolder in SubFolders.Values)
                    subFolder.FillConfigurationPlatformWithProjectContexts(configurationPlatform);
            }
        }
    }
}