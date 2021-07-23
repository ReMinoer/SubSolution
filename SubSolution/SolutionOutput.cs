using System.Collections.Generic;
using SubSolution.Utils;

namespace SubSolution
{
    public class SolutionOutput : ISolutionOutput
    {
        private readonly HashSet<string> _filePaths = new HashSet<string>();
        private readonly HashSet<string> _projectPaths = new HashSet<string>();
        
        public string OutputPath { get; }

        public Folder Root { get; } = new Folder();
        ISolutionFolder ISolution.Root => Root;

        public SolutionOutput(string outputPath)
        {
            OutputPath = outputPath;
        }

        public void AddFile(string filePath, IEnumerable<string> solutionFolderPath)
        {
            if (_filePaths.Add(filePath))
                GetSolutionFolder(solutionFolderPath).FilePaths.Add(filePath);
        }

        public void AddProject(string projectPath, IEnumerable<string> solutionFolderPath)
        {
            if (_projectPaths.Add(projectPath))
                GetSolutionFolder(solutionFolderPath).ProjectPaths.Add(projectPath);
        }

        private Folder GetSolutionFolder(IEnumerable<string> solutionFolderPath)
        {
            Folder currentFolder = Root;
            foreach (string solutionFolderName in solutionFolderPath)
            {
                if (!currentFolder.SubFolders.TryGetValue(solutionFolderName, out Folder subFolder))
                    currentFolder.SubFolders[solutionFolderName] = subFolder = new Folder();

                currentFolder = subFolder;
            }

            return currentFolder;
        }

        public class Folder : ISolutionFolder
        {
            public HashSet<string> FilePaths { get; }
            public HashSet<string> ProjectPaths { get; }
            public Dictionary<string, Folder> SubFolders { get; }

            private readonly ReadOnlyCollection<string> _readOnlyFilePaths;
            private readonly ReadOnlyCollection<string> _readOnlyProjectPaths;
            private readonly CovariantReadOnlyDictionary<string, Folder> _readOnlySubFolders;

            IReadOnlyCollection<string> ISolutionFolder.FilePaths => _readOnlyFilePaths;
            IReadOnlyCollection<string> ISolutionFolder.ProjectPaths => _readOnlyProjectPaths;
            ICovariantReadOnlyDictionary<string, ISolutionFolder> ISolutionFolder.SubFolders => _readOnlySubFolders;

            public Folder()
            {
                FilePaths = new HashSet<string>();
                _readOnlyFilePaths = new ReadOnlyCollection<string>(FilePaths);

                ProjectPaths = new HashSet<string>();
                _readOnlyProjectPaths = new ReadOnlyCollection<string>(ProjectPaths);

                SubFolders = new Dictionary<string, Folder>();
                _readOnlySubFolders = new CovariantReadOnlyDictionary<string, Folder>(SubFolders);
            }
        }
    }
}