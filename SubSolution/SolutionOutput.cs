using System;
using System.Collections.Generic;
using System.Linq;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution
{
    public class SolutionOutput : ISolutionOutput
    {
        private readonly ISubSolutionFileSystem _fileSystem;

        private readonly Dictionary<string, Folder> _knownPaths = new Dictionary<string, Folder>();

        public string OutputPath { get; private set; }

        public Folder Root { get; }
        ISolutionFolder ISolution.Root => Root;

        public SolutionOutput(string outputPath, ISubSolutionFileSystem? fileSystem = null)
        {
            Root = new Folder(this);
            OutputPath = outputPath;

            _fileSystem = fileSystem ?? StandardFileSystem.Instance;
        }

        public void SetOutputDirectory(string outputDirectory)
        {
            string currentOutputDirectory = _fileSystem.GetParentDirectoryPath(OutputPath)!;

            _knownPaths.Clear();

            ChangeOutputDirectory(outputDirectory, currentOutputDirectory, Root);
            OutputPath = outputDirectory;
        }

        private void ChangeOutputDirectory(string outputDirectory, string previousOutputDirectory, Folder folder)
        {
            string[] previousFilePaths = folder.FilePaths.ToArray();
            string[] previousProjectPaths = folder.ProjectPaths.ToArray();
            folder.ClearEntries();

            foreach (string previousFilePath in previousFilePaths)
                folder.AddFile(_fileSystem.MoveRelativePathRoot(previousFilePath, previousOutputDirectory, outputDirectory));
            foreach (string previousProjectPath in previousProjectPaths)
                folder.AddProject(_fileSystem.MoveRelativePathRoot(previousProjectPath, previousOutputDirectory, outputDirectory));

            foreach (Folder subFolder in folder.SubFolders.Values)
                ChangeOutputDirectory(outputDirectory, previousOutputDirectory, subFolder);
        }

        public class Folder : ISolutionFolder
        {
            private readonly SolutionOutput _solution;

            private readonly HashSet<string> _filePaths;
            private readonly HashSet<string> _projectPaths;
            private readonly Dictionary<string, Folder> _subFolders;

            public ReadOnlyCollection<string> FilePaths { get; }
            public ReadOnlyCollection<string> ProjectPaths { get; }
            public CovariantReadOnlyDictionary<string, Folder> SubFolders { get; }

            IReadOnlyCollection<string> ISolutionFolder.FilePaths => FilePaths;
            IReadOnlyCollection<string> ISolutionFolder.ProjectPaths => ProjectPaths;
            ICovariantReadOnlyDictionary<string, ISolutionFolder> ISolutionFolder.SubFolders => SubFolders;

            public bool IsEmpty => _filePaths.Count == 0 && _projectPaths.Count == 0 && _subFolders.Count == 0;

            public Folder(SolutionOutput solution)
            {
                _solution = solution;

                _filePaths = new HashSet<string>();
                FilePaths = new ReadOnlyCollection<string>(_filePaths);

                _projectPaths = new HashSet<string>();
                ProjectPaths = new ReadOnlyCollection<string>(_projectPaths);

                _subFolders = new Dictionary<string, Folder>();
                SubFolders = new CovariantReadOnlyDictionary<string, Folder>(_subFolders);
            }

            public void AddFile(string filePath, bool overwrite = false)
                => AddEntry(filePath, x => x._filePaths, overwrite);

            public void AddProject(string projectPath, bool overwrite = false)
                => AddEntry(projectPath, x => x._projectPaths, overwrite);

            private void AddEntry(string filePath, Func<Folder, HashSet<string>> getFolderContent, bool overwrite)
            {
                if (overwrite && _solution._knownPaths.Remove(filePath, out Folder folder))
                    getFolderContent(folder).Remove(filePath);

                if (!overwrite && _solution._knownPaths.ContainsKey(filePath))
                    return;
                
                getFolderContent(this).Add(filePath);
                _solution._knownPaths.Add(filePath, this);
            }

            public void AddFolderContent(ISolutionFolder folder, bool overwrite = false)
            {
                foreach (string filePath in folder.FilePaths)
                    AddFile(filePath, overwrite);
                foreach (string projectPath in folder.ProjectPaths)
                    AddProject(projectPath, overwrite);

                foreach ((string subFolderName, ISolutionFolder subFolder) in folder.SubFolders.Select(x => (x.Key, x.Value)))
                    GetOrCreateSubFolder(subFolderName).AddFolderContent(subFolder, overwrite);
            }

            public bool RemoveFile(string filePath)
                => RemoveEntry(filePath, _solution._knownPaths, _filePaths);

            public bool RemoveProject(string projectPath)
                => RemoveEntry(projectPath, _solution._knownPaths, _projectPaths);

            private bool RemoveEntry(string filePath, Dictionary<string, Folder> dictionary, HashSet<string> folderContent)
            {
                if (!folderContent.Remove(filePath))
                    return false;

                dictionary.Remove(filePath);
                return true;
            }

            public bool RemoveSubFolder(string subFolderName)
            {
                if (!_subFolders.TryGetValue(subFolderName, out Folder subFolder))
                    return false;

                subFolder.ClearEntries();
                _subFolders.Remove(subFolderName);
                return true;
            }

            public void Clear()
            {
                ClearEntries();
                _subFolders.Clear();
            }

            public void ClearEntries()
            {
                foreach (string filePath in _filePaths)
                    _solution._knownPaths.Remove(filePath);

                foreach (string projectPath in _projectPaths)
                    _solution._knownPaths.Remove(projectPath);

                _filePaths.Clear();
                _projectPaths.Clear();
            }

            public Folder GetOrCreateSubFolder(string folderName)
            {
                if (!_subFolders.TryGetValue(folderName, out Folder subFolder))
                    _subFolders[folderName] = subFolder = new Folder(_solution);

                return subFolder;
            }

            public Folder GetOrCreateSubFolder(IEnumerable<string> folderPath)
            {
                return folderPath.Aggregate(this, (currentFolder, folderName) => currentFolder.GetOrCreateSubFolder(folderName));
            }
        }
    }
}