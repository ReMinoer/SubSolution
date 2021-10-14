using System.Collections.Generic;
using System.Collections.ObjectModel;
using SubSolution.Base;
using SubSolution.FileSystems;

namespace SubSolution
{
    public class ManualSolution : SolutionBase<ManualSolution, ManualSolution.Folder>
    {
        public override sealed Folder Root { get; }

        public List<ConfigurationPlatform> ConfigurationPlatforms { get; }
        protected override sealed IReadOnlyList<ISolutionConfigurationPlatform> ProtectedConfigurationPlatforms { get; }

        public ManualSolution(string outputDirectoryPath, IFileSystem? fileSystem = null)
            : base(outputDirectoryPath, fileSystem)
        {
            Root = new Folder(this, _fileSystem, _knownPaths);

            ConfigurationPlatforms = new List<ConfigurationPlatform>();
            ProtectedConfigurationPlatforms = ConfigurationPlatforms.AsReadOnly();
        }

        public ManualSolution(IMovableSolution solution, IFileSystem? fileSystem = null)
            : this(solution.OutputDirectoryPath, fileSystem)
        {
            Root.AddFolderContent(solution.Root);

            foreach (ISolutionConfigurationPlatform configurationPlatform in solution.ConfigurationPlatforms)
            {
                ConfigurationPlatform copy = new ConfigurationPlatform(_fileSystem, configurationPlatform.ConfigurationName, configurationPlatform.PlatformName);

                foreach ((string projectPath, SolutionProjectContext projectContext) in configurationPlatform.ProjectContexts)
                    copy.ProjectContexts.Add(projectPath, new SolutionProjectContext(projectContext));

                ConfigurationPlatforms.Add(copy);
            }
        }

        public class ConfigurationPlatform : ISolutionConfigurationPlatform
        {
            public string ConfigurationName { get; }
            public string PlatformName { get; }
            public Dictionary<string, SolutionProjectContext> ProjectContexts { get; }

            private readonly IReadOnlyDictionary<string, SolutionProjectContext> _readOnlyProjectContexts;
            IReadOnlyDictionary<string, SolutionProjectContext> ISolutionConfigurationPlatform.ProjectContexts => _readOnlyProjectContexts;

            public ConfigurationPlatform(IFileSystem fileSystem, string configurationName, string platformName)
            {
                ConfigurationName = configurationName;
                PlatformName = platformName;

                ProjectContexts = new Dictionary<string, SolutionProjectContext>(fileSystem.PathComparer);
                _readOnlyProjectContexts = new ReadOnlyDictionary<string, SolutionProjectContext>(ProjectContexts);
            }
        }

        public class Folder : SolutionFolderBase<ManualSolution, Folder>
        {
            public Folder(ManualSolution solution, IFileSystem fileSystem, Dictionary<string, Folder> knownPaths)
                : base(solution, fileSystem, knownPaths, () => new Folder(solution, fileSystem, knownPaths)) {}
        }
    }
}