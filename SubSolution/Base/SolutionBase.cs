using System.Collections.Generic;
using SubSolution.FileSystems;

namespace SubSolution.Base
{
    public abstract class SolutionBase<TSolution, TFolder> : IMergeableSolution
        where TSolution : SolutionBase<TSolution, TFolder>
        where TFolder : SolutionFolderBase<TSolution, TFolder>
    {
        protected readonly IFileSystem _fileSystem;
        protected readonly Dictionary<string, TFolder> _knownPaths;
        
        public string OutputDirectoryPath { get; private set; }

        public abstract TFolder Root { get; }
        ISolutionFolder ISolution.Root => Root;
        IFilterableSolutionFolder IFilterableSolution.Root => Root;

        protected abstract IReadOnlyList<ISolutionConfigurationPlatform> ProtectedConfigurationPlatforms { get; }
        IReadOnlyList<ISolutionConfigurationPlatform> ISolution.ConfigurationPlatforms => ProtectedConfigurationPlatforms;

        public SolutionBase(string outputDirectoryPath, IFileSystem? fileSystem)
        {
            _fileSystem = fileSystem ?? StandardFileSystem.Instance;
            _knownPaths = new Dictionary<string, TFolder>(_fileSystem.PathComparer);
            
            OutputDirectoryPath = outputDirectoryPath;
        }

        public void SetOutputDirectory(string outputDirectoryPath)
        {
            _knownPaths.Clear();

            Root.ChangeItemsRootDirectory(outputDirectoryPath);
            OutputDirectoryPath = outputDirectoryPath;
        }
    }
}