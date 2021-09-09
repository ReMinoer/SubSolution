using System.Collections.Generic;
using SubSolution.FileSystems;

namespace SubSolution.Base
{
    public abstract class SolutionBase<TSolution, TFolder> : ISolution
        where TSolution : SolutionBase<TSolution, TFolder>
        where TFolder : SolutionFolderBase<TSolution, TFolder>
    {
        protected readonly IFileSystem _fileSystem;
        protected readonly Dictionary<string, TFolder> _knownPaths;

        public string SolutionName { get; set; }
        public string OutputDirectory { get; private set; }
        public string OutputPath => _fileSystem.Combine(OutputDirectory, SolutionName + ".sln");

        public abstract TFolder Root { get; }
        ISolutionFolder ISolution.Root => Root;

        protected abstract IReadOnlyList<ISolutionConfigurationPlatform> ProtectedConfigurationPlatforms { get; }
        IReadOnlyList<ISolutionConfigurationPlatform> ISolution.ConfigurationPlatforms => ProtectedConfigurationPlatforms;

        public SolutionBase(string outputPath, IFileSystem? fileSystem)
        {
            _fileSystem = fileSystem ?? StandardFileSystem.Instance;
            _knownPaths = new Dictionary<string, TFolder>(_fileSystem.PathComparer);

            SolutionName = _fileSystem.GetFileNameWithoutExtension(outputPath);
            OutputDirectory = _fileSystem.GetParentDirectoryPath(outputPath)!;
        }

        public void SetOutputDirectory(string outputDirectory)
        {
            _knownPaths.Clear();

            Root.ChangeItemsRootDirectory(outputDirectory);
            OutputDirectory = outputDirectory;
        }
    }
}