using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution.Base
{
    public abstract class SolutionFolderBase<TSolution, TFolder> : IFilterableSolutionFolder
        where TSolution : SolutionBase<TSolution, TFolder>
        where TFolder : SolutionFolderBase<TSolution, TFolder>
    {
        protected readonly TFolder _owner;
        protected readonly TSolution _solution;
        protected readonly IFileSystem _fileSystem;
        protected readonly Dictionary<string, TFolder> _knownPaths;
        protected readonly Func<TFolder> _folderCreator;

        private readonly HashSet<string> _filePaths;
        private readonly Dictionary<string, ISolutionProject> _projects;
        private readonly Dictionary<string, TFolder> _subFolders;

        public Utils.ReadOnlyCollection<string> FilePaths { get; }
        public ReadOnlyDictionary<string, ISolutionProject> Projects { get; }
        public CovariantReadOnlyDictionary<string, TFolder> SubFolders { get; }

        IReadOnlyCollection<string> ISolutionFolder.FilePaths => FilePaths;
        IReadOnlyDictionary<string, ISolutionProject> ISolutionFolder.Projects => Projects;
        ICovariantReadOnlyDictionary<string, ISolutionFolder> ISolutionFolder.SubFolders => SubFolders;

        public bool IsEmpty => _filePaths.Count == 0 && _projects.Count == 0 && _subFolders.Count == 0;

        public SolutionFolderBase(TSolution solution, IFileSystem fileSystem, Dictionary<string, TFolder> knownPaths, Func<TFolder> folderCreator)
        {
            _owner = (TFolder)this;
            _solution = solution;
            _fileSystem = fileSystem;
            _knownPaths = knownPaths;
            _folderCreator = folderCreator;

            _filePaths = new HashSet<string>(_fileSystem.PathComparer);
            FilePaths = new Utils.ReadOnlyCollection<string>(_filePaths);

            _projects = new Dictionary<string, ISolutionProject>(_fileSystem.PathComparer);
            Projects = new ReadOnlyDictionary<string, ISolutionProject>(_projects);

            _subFolders = new Dictionary<string, TFolder>();
            SubFolders = new CovariantReadOnlyDictionary<string, TFolder>(_subFolders);
        }

        public bool AddFile(string filePath, bool overwrite = false)
            => AddEntry(filePath, x => x._filePaths.Add(filePath), x => x._filePaths.Remove(filePath), overwrite);
        public virtual bool AddProject(string projectPath, ISolutionProject project, bool overwrite = false)
            => AddEntry(projectPath, x => x._projects.Add(projectPath, project), x => x._projects.Remove(projectPath), overwrite);

        private bool AddEntry(string filePath, Action<TFolder> addPath, Action<TFolder> removePath, bool overwrite)
        {
            if (overwrite && _knownPaths.Remove(filePath, out TFolder folder))
                removePath(folder);

            if (!overwrite && _knownPaths.ContainsKey(filePath))
                return false;

            addPath(_owner);
            _knownPaths.Add(filePath, _owner);
            return true;
        }

        public void AddFolderContent(ISolutionFolder folder, bool overwrite = false)
        {
            foreach (string filePath in folder.FilePaths)
                AddFile(filePath, overwrite);

            foreach ((string projectPaths, ISolutionProject project) in folder.Projects)
                AddProject(projectPaths, project, overwrite);

            foreach ((string subFolderName, ISolutionFolder subFolder) in folder.SubFolders.Select(x => (x.Key, x.Value)))
                GetOrAddSubFolder(subFolderName).AddFolderContent(subFolder, overwrite);
        }

        public bool RemoveFile(string filePath)
            => RemoveEntry(filePath, () => _filePaths.Remove(filePath));
        public bool RemoveProject(string projectPath)
            => RemoveEntry(projectPath, () => _projects.Remove(projectPath));

        private bool RemoveEntry(string filePath, Func<bool> removeFilePath)
        {
            if (!removeFilePath())
                return false;

            _knownPaths.Remove(filePath);
            return true;
        }

        public bool RemoveSubFolder(string subFolderName)
        {
            if (!_subFolders.TryGetValue(subFolderName, out TFolder subFolder))
                return false;

            subFolder.Clear();
            _subFolders.Remove(subFolderName);
            return true;
        }

        public void Clear()
        {
            foreach (TFolder subFolder in _subFolders.Values)
                subFolder.Clear();

            foreach (string filePath in _filePaths)
                _knownPaths.Remove(filePath);

            foreach (string projectPath in _projects.Keys)
                _knownPaths.Remove(projectPath);

            _filePaths.Clear();
            _projects.Clear();
            _subFolders.Clear();
        }

        public bool CollapseSubFolder(string subFolderName)
        {
            if (!_subFolders.TryGetValue(subFolderName, out TFolder subFolder))
                return false;

            PrepareFolderMove();
            _subFolders.Remove(subFolderName);

            AddFolderContent(subFolder);
            return true;
        }

        private void PrepareFolderMove()
        {
            foreach (string filePath in _filePaths)
                _knownPaths.Remove(filePath);
            foreach (string projectPath in _projects.Keys)
                _knownPaths.Remove(projectPath);

            foreach (TFolder subFolder in _subFolders.Values)
                subFolder.PrepareFolderMove();
        }

        public TFolder GetOrAddSubFolder(string folderName)
        {
            if (!_subFolders.TryGetValue(folderName, out TFolder subFolder))
                _subFolders[folderName] = subFolder = _folderCreator();

            return subFolder;
        }

        public TFolder GetOrAddSubFolder(IEnumerable<string> folderPath)
        {
            return folderPath.Aggregate(_owner, (currentFolder, folderName) => currentFolder.GetOrAddSubFolder(folderName));
        }

        public void ChangeItemsRootDirectory(string outputDirectory)
        {
            ChangeItemsRootDirectory(outputDirectory, _solution.OutputDirectory);
        }

        private void ChangeItemsRootDirectory(string outputDirectory, string previousOutputDirectory)
        {
            string[] previousFilePaths = _filePaths.ToArray();
            Dictionary<string, ISolutionProject> previousProjectPaths = _projects.ToDictionary(x => x.Key, x => x.Value);

            foreach (string filePath in _filePaths)
                _knownPaths.Remove(filePath);
            foreach (string projectPath in _projects.Keys)
                _knownPaths.Remove(projectPath);

            _filePaths.Clear();
            _projects.Clear();

            foreach (TFolder subFolder in SubFolders.Values)
                subFolder.ChangeItemsRootDirectory(outputDirectory, previousOutputDirectory);

            foreach (string previousFilePath in previousFilePaths)
            {
                string newFilePath = _fileSystem.MoveRelativePathRoot(previousFilePath, previousOutputDirectory, outputDirectory);

                _filePaths.Add(newFilePath);
                _knownPaths.Add(newFilePath, _owner);
            }

            foreach ((string previousProjectPath, ISolutionProject project) in previousProjectPaths)
            {
                string newFilePath = _fileSystem.MoveRelativePathRoot(previousProjectPath, previousOutputDirectory, outputDirectory);

                _projects.Add(newFilePath, project);
                _knownPaths.Add(newFilePath, _owner);
            }
        }

        public void FilterProjects(Func<string, ISolutionProject, bool> predicate)
        {
            string[] projectPathsToRemove = _projects.Where(x => !predicate(x.Key, x.Value)).Select(x => x.Key).ToArray();

            foreach (string removedPath in projectPathsToRemove)
                RemoveProject(removedPath);

            foreach (TFolder subFolder in _subFolders.Values)
                subFolder.FilterProjects(predicate);

            string[] emptySubFolderNames = _subFolders.Where(x => x.Value.IsEmpty).Select(x => x.Key).ToArray();
            foreach (string emptySubFolderName in emptySubFolderNames)
                _subFolders.Remove(emptySubFolderName);
        }

        public void FilterFiles(Func<string, bool> predicate)
        {
            string[] filePathsToRemove = _filePaths.Where(x => !predicate(x)).ToArray();

            foreach (string removedPath in filePathsToRemove)
                RemoveFile(removedPath);

            foreach (TFolder subFolder in _subFolders.Values)
                subFolder.FilterFiles(predicate);

            string[] emptySubFolderNames = _subFolders.Where(x => x.Value.IsEmpty).Select(x => x.Key).ToArray();
            foreach (string emptySubFolderName in emptySubFolderNames)
                _subFolders.Remove(emptySubFolderName);
        }
    }
}