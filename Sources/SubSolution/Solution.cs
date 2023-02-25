using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using SubSolution.Base;
using SubSolution.FileSystems;

namespace SubSolution
{
    public class Solution : SolutionBase<Solution, Solution.Folder>
    {
        public override sealed Folder Root { get; }

        private readonly List<ConfigurationPlatform> _configurationPlatforms;
        protected override sealed IReadOnlyList<ISolutionConfigurationPlatform> ProtectedConfigurationPlatforms { get; }

        private readonly Dictionary<string, Regex[]> _configurationMatches;
        private readonly Dictionary<string, Regex[]> _platformMatches;

        public Solution(string outputDirectoryPath, IFileSystem? fileSystem = null)
            : base(outputDirectoryPath, fileSystem)
        {
            Root = new Folder(this, _fileSystem, _knownPaths);

            _configurationPlatforms = new List<ConfigurationPlatform>();
            ProtectedConfigurationPlatforms = _configurationPlatforms.AsReadOnly();

            _configurationMatches = new Dictionary<string, Regex[]>();
            _platformMatches = new Dictionary<string, Regex[]>();
        }

        public void AddConfiguration(string configurationName, Regex[]? matchingProjectConfigurations = null)
        {
            matchingProjectConfigurations ??= new[] { new Regex(configurationName, RegexOptions.IgnoreCase) };

            _configurationMatches.Add(configurationName, matchingProjectConfigurations);

            foreach ((string platformName, Regex[] matchingProjectPlatforms) in _platformMatches)
            {
                AddConfigurationPlatform(configurationName, platformName, matchingProjectConfigurations, matchingProjectPlatforms);
            }
        }

        public void AddPlatform(string platformName, Regex[]? matchingProjectPlatforms = null)
        {
            matchingProjectPlatforms ??= new[] { new Regex(platformName, RegexOptions.IgnoreCase) };

            _platformMatches.Add(platformName, matchingProjectPlatforms);

            foreach ((string configurationName, Regex[] matchingProjectConfigurations) in _configurationMatches)
            {
                AddConfigurationPlatform(configurationName, platformName, matchingProjectConfigurations, matchingProjectPlatforms);
            }
        }

        private void AddConfigurationPlatform(string configurationName, string platformName, Regex[] matchingProjectConfigurations, Regex[] matchingProjectPlatforms)
        {
            var configurationPlatform = new ConfigurationPlatform(_fileSystem, configurationName, platformName);
            configurationPlatform.MatchingProjectConfigurationNames.AddRange(matchingProjectConfigurations);
            configurationPlatform.MatchingProjectPlatformNames.AddRange(matchingProjectPlatforms);

            FillConfigurationPlatformWithProjectContexts(configurationPlatform, Root);
            _configurationPlatforms.Add(configurationPlatform);
        }

        private void FillConfigurationPlatformWithProjectContexts(ConfigurationPlatform configurationPlatform, Folder folder)
        {
            foreach ((string projectPath, ISolutionProject project) in folder.Projects)
                if (!configurationPlatform.ProjectContexts.ContainsKey(projectPath))
                    configurationPlatform.AddProjectContext(projectPath, project);

            foreach (Folder subFolder in folder.SubFolders.Values)
                FillConfigurationPlatformWithProjectContexts(configurationPlatform, subFolder);
        }

        public void RemoveConfiguration(string configurationName)
        {
            _configurationMatches.Remove(configurationName);
            _configurationPlatforms.RemoveAll(x => x.ConfigurationName == configurationName);
        }

        public void RemovePlatform(string platformName)
        {
            _platformMatches.Remove(platformName);
            _configurationPlatforms.RemoveAll(x => x.PlatformName == platformName);
        }

        private class ConfigurationPlatform : ISolutionConfigurationPlatform
        {
            public string ConfigurationName { get; }
            public string PlatformName { get; }
            public string FullName => ConfigurationName + '|' + PlatformName;
            public List<Regex> MatchingProjectConfigurationNames { get; }
            public List<Regex> MatchingProjectPlatformNames { get; }

            private readonly Dictionary<string, SolutionProjectContext> _projectContexts;
            public IReadOnlyDictionary<string, SolutionProjectContext> ProjectContexts { get; }

            public ConfigurationPlatform(IFileSystem fileSystem, string configurationName, string platformName)
            {
                ConfigurationName = configurationName;
                PlatformName = platformName;

                MatchingProjectConfigurationNames = new List<Regex>();
                MatchingProjectPlatformNames = new List<Regex>();

                _projectContexts = new Dictionary<string, SolutionProjectContext>(fileSystem.PathComparer);
                ProjectContexts = new ReadOnlyDictionary<string, SolutionProjectContext>(_projectContexts);
            }

            public void AddProjectContext(string projectPath, ISolutionProject project)
            {
                SolutionProjectContext solutionProjectContext;
                bool isCompleteMatch;

                if (project.NoPlatform)
                {
                    if (project.Configurations.Count == 0 )
                        return;

                    string? matchingProjectConfiguration = MatchNames(project.Configurations, MatchingProjectConfigurationNames);
                    isCompleteMatch = matchingProjectConfiguration != null;

                    string? resolvedProjectConfiguration = matchingProjectConfiguration ?? project.Configurations[0];
                    solutionProjectContext = new SolutionProjectContext(resolvedProjectConfiguration);
                }
                else
                {
                    if (project.Configurations.Count == 0 || project.Platforms.Count == 0)
                        return;

                    string? matchingProjectConfiguration = MatchNames(project.Configurations, MatchingProjectConfigurationNames);
                    string? matchingProjectPlatform = MatchNames(project.Platforms, MatchingProjectPlatformNames);
                    isCompleteMatch = matchingProjectConfiguration != null && matchingProjectPlatform != null;

                    string? resolvedProjectConfiguration = matchingProjectConfiguration ?? project.Configurations[0];
                    string? resolvedProjectPlatform = matchingProjectPlatform ?? project.Platforms[0];
                    solutionProjectContext = new SolutionProjectContext(resolvedProjectConfiguration, resolvedProjectPlatform);
                }

                solutionProjectContext.Build = project.CanBuild && isCompleteMatch;
                solutionProjectContext.Deploy = project.AlwaysDeploy || (project.CanDeploy && isCompleteMatch);
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

            static private string? MatchNames(IReadOnlyList<string> names, IReadOnlyList<Regex> matches)
            {
                foreach (Regex match in matches)
                    foreach (string name in names)
                        if (match.IsMatch(name))
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
        }
    }
}