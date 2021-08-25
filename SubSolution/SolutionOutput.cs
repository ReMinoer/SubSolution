using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution
{
    public class SolutionOutput : ISolutionOutput
    {
        private readonly ISubSolutionFileSystem _fileSystem;
        private readonly Dictionary<string, Folder> _knownPaths;

        public string OutputPath { get; private set; }

        public Folder Root { get; }
        ISolutionFolder ISolution.Root => Root;

        private readonly List<ConfigurationPlatform> _configurationPlatforms;
        public IReadOnlyList<ISolutionConfigurationPlatform> ConfigurationPlatforms { get; }

        public SolutionOutput(string outputPath, ISubSolutionFileSystem? fileSystem = null)
        {
            _fileSystem = fileSystem ?? StandardFileSystem.Instance;
            _knownPaths = new Dictionary<string, Folder>(_fileSystem.PathComparer);

            OutputPath = outputPath;
            Root = new Folder(this);

            _configurationPlatforms = new List<ConfigurationPlatform>();
            ConfigurationPlatforms = _configurationPlatforms.AsReadOnly();
        }

        public void SetOutputDirectory(string outputDirectory)
        {
            _knownPaths.Clear();

            Root.ChangeItemsRootDirectory(outputDirectory);
            OutputPath = outputDirectory;
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
            public Dictionary<string, SolutionProjectContext> ProjectContexts { get; }

            private readonly IReadOnlyList<string> _readOnlyMatchingProjectConfigurationNames;
            IReadOnlyList<string> ISolutionConfigurationPlatform.MatchingProjectConfigurationNames => _readOnlyMatchingProjectConfigurationNames;

            private readonly IReadOnlyList<string> _readOnlyMatchingProjectPlatformNames;
            IReadOnlyList<string> ISolutionConfigurationPlatform.MatchingProjectPlatformNames => _readOnlyMatchingProjectPlatformNames;

            private readonly IReadOnlyDictionary<string, SolutionProjectContext> _readOnlyProjectContexts;
            IReadOnlyDictionary<string, SolutionProjectContext> ISolutionConfigurationPlatform.ProjectContexts => _readOnlyProjectContexts;

            public ConfigurationPlatform(ISubSolutionFileSystem fileSystem, string configurationName, string platformName)
            {
                ConfigurationName = configurationName;
                PlatformName = platformName;

                MatchingProjectConfigurationNames = new List<string>();
                _readOnlyMatchingProjectConfigurationNames = MatchingProjectConfigurationNames.AsReadOnly();

                MatchingProjectPlatformNames = new List<string>();
                _readOnlyMatchingProjectPlatformNames = MatchingProjectPlatformNames.AsReadOnly();

                ProjectContexts = new Dictionary<string, SolutionProjectContext>(fileSystem.PathComparer);
                _readOnlyProjectContexts = new ReadOnlyDictionary<string, SolutionProjectContext>(ProjectContexts);
            }
        }

        public class Folder : ISolutionFolder
        {
            private readonly SolutionOutput _solution;

            private readonly HashSet<string> _filePaths;
            private readonly Dictionary<string, ISolutionProject> _projects;
            private readonly Dictionary<string, Folder> _subFolders;

            public Utils.ReadOnlyCollection<string> FilePaths { get; }
            public ReadOnlyDictionary<string, ISolutionProject> Projects { get; }
            public CovariantReadOnlyDictionary<string, Folder> SubFolders { get; }

            IReadOnlyCollection<string> ISolutionFolder.FilePaths => FilePaths;
            IReadOnlyDictionary<string, ISolutionProject> ISolutionFolder.Projects => Projects;
            ICovariantReadOnlyDictionary<string, ISolutionFolder> ISolutionFolder.SubFolders => SubFolders;

            public bool IsEmpty => _filePaths.Count == 0 && _projects.Count == 0 && _subFolders.Count == 0;

            public Folder(SolutionOutput solution)
            {
                _solution = solution;

                _filePaths = new HashSet<string>(_solution._fileSystem.PathComparer);
                FilePaths = new Utils.ReadOnlyCollection<string>(_filePaths);
                
                _projects = new Dictionary<string, ISolutionProject>(_solution._fileSystem.PathComparer);
                Projects = new ReadOnlyDictionary<string, ISolutionProject>(_projects);

                _subFolders = new Dictionary<string, Folder>();
                SubFolders = new CovariantReadOnlyDictionary<string, Folder>(_subFolders);
            }

            public bool AddFile(string filePath, bool overwrite = false)
                => AddEntry(filePath, x => x._filePaths.Add(filePath), x => x._filePaths.Remove(filePath), overwrite);

            public bool AddProject(string projectPath, ISolutionProject project, bool overwrite = false)
            {
                if (!AddEntry(projectPath, x => x._projects.Add(projectPath, project), x => x._projects.Remove(projectPath), overwrite))
                    return false;

                foreach (ConfigurationPlatform configurationPlatform in _solution._configurationPlatforms)
                    AddProjectContext(projectPath, project, configurationPlatform);

                return true;
            }

            private bool AddEntry(string filePath, Action<Folder> addPath, Action<Folder> removePath, bool overwrite)
            {
                if (overwrite && _solution._knownPaths.Remove(filePath, out Folder folder))
                    removePath(folder);

                if (!overwrite && _solution._knownPaths.ContainsKey(filePath))
                    return false;

                addPath(this);
                _solution._knownPaths.Add(filePath, this);
                return true;
            }

            public async Task AddFolderContent(ISolutionFolder folder, bool overwrite = false)
            {
                foreach (string filePath in folder.FilePaths)
                    AddFile(filePath, overwrite);

                foreach ((string projectPaths, ISolutionProject project) in folder.Projects)
                    AddProject(projectPaths, project, overwrite);

                foreach ((string subFolderName, ISolutionFolder subFolder) in folder.SubFolders.Select(x => (x.Key, x.Value)))
                    await GetOrCreateSubFolder(subFolderName).AddFolderContent(subFolder, overwrite);
            }

            public void FillConfigurationPlatformWithProjectContexts(ConfigurationPlatform configurationPlatform)
            {
                foreach ((string projectPath, ISolutionProject project) in Projects)
                    if (!configurationPlatform.ProjectContexts.ContainsKey(projectPath))
                        AddProjectContext(projectPath, project, configurationPlatform);

                foreach (Folder subFolder in SubFolders.Values)
                    subFolder.FillConfigurationPlatformWithProjectContexts(configurationPlatform);
            }

            private void AddProjectContext(string projectPath, ISolutionProject project, ConfigurationPlatform configurationPlatform)
            {
                string? matchingProjectConfiguration = MatchNames(project.Configurations, configurationPlatform.MatchingProjectConfigurationNames);
                string? matchingProjectPlatform = MatchNames(project.Platforms, configurationPlatform.MatchingProjectPlatformNames);
                bool isCompleteMatch = matchingProjectConfiguration != null && matchingProjectPlatform != null;

                string? resolvedProjectConfiguration = matchingProjectConfiguration ?? project.Configurations[0];
                string? resolvedProjectPlatform = matchingProjectPlatform ?? project.Platforms[0];

                var solutionProjectContext = new SolutionProjectContext(resolvedProjectConfiguration, resolvedProjectPlatform)
                {
                    Build = project.CanBuild && isCompleteMatch,
                    Deploy = project.CanDeploy && isCompleteMatch
                };

                configurationPlatform.ProjectContexts.Add(projectPath, solutionProjectContext);
            }

            static private string? MatchNames(IReadOnlyList<string> names, IReadOnlyList<string> matches)
            {
                foreach (string match in matches)
                    foreach (string name in names)
                        if (name.Contains(match, StringComparison.OrdinalIgnoreCase))
                            return name;

                return null;
            }

            public bool RemoveFile(string filePath)
                => RemoveEntry(filePath, () => _filePaths.Remove(filePath));
            public bool RemoveProject(string projectPath)
                => RemoveEntry(projectPath, () => _projects.Remove(projectPath));

            private bool RemoveEntry(string filePath, Func<bool> removeFilePath)
            {
                if (!removeFilePath())
                    return false;

                _solution._knownPaths.Remove(filePath);
                return true;
            }

            public bool RemoveSubFolder(string subFolderName)
            {
                if (!_subFolders.TryGetValue(subFolderName, out Folder subFolder))
                    return false;

                subFolder.Clear();
                _subFolders.Remove(subFolderName);
                return true;
            }

            public void Clear()
            {
                foreach (string filePath in _filePaths)
                    _solution._knownPaths.Remove(filePath);

                foreach (string projectPath in _projects.Keys)
                    _solution._knownPaths.Remove(projectPath);

                _filePaths.Clear();
                _projects.Clear();
                _subFolders.Clear();
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

            public void ChangeItemsRootDirectory(string outputDirectory)
            {
                string previousOutputDirectory = _solution._fileSystem.GetParentDirectoryPath(_solution.OutputPath)!;
                ChangeItemsRootDirectory(outputDirectory, previousOutputDirectory);
            }

            private void ChangeItemsRootDirectory(string outputDirectory, string previousOutputDirectory)
            {
                string[] previousFilePaths = _filePaths.ToArray();
                Dictionary<string, ISolutionProject> previousProjectPaths = _projects.ToDictionary(x => x.Key, x => x.Value);

                foreach (string filePath in _filePaths)
                    _solution._knownPaths.Remove(filePath);
                foreach (string projectPath in _projects.Keys)
                    _solution._knownPaths.Remove(projectPath);

                _filePaths.Clear();
                _projects.Clear();

                foreach (Folder subFolder in SubFolders.Values)
                    subFolder.ChangeItemsRootDirectory(outputDirectory, previousOutputDirectory);

                foreach (string previousFilePath in previousFilePaths)
                {
                    string newFilePath = _solution._fileSystem.MoveRelativePathRoot(previousFilePath, previousOutputDirectory, outputDirectory);

                    _filePaths.Add(newFilePath);
                    _solution._knownPaths.Add(newFilePath, this);
                }

                foreach ((string previousProjectPath, ISolutionProject project) in previousProjectPaths)
                {
                    string newFilePath = _solution._fileSystem.MoveRelativePathRoot(previousProjectPath, previousOutputDirectory, outputDirectory);

                    _projects.Add(newFilePath, project);
                    _solution._knownPaths.Add(newFilePath, this);
                }
            }
        }
    }
}