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

        public List<SolutionConfiguration> Configurations { get; }
        private readonly IReadOnlyCollection<SolutionConfiguration> _readOnlyConfigurations;
        IReadOnlyCollection<ISolutionConfiguration> ISolution.Configurations => _readOnlyConfigurations;

        public List<ConfigurationBinding> ConfigurationBindings { get; }
        public List<ConfigurationBinding> PlatformBindings { get; }

        public SolutionOutput(string outputPath, ISubSolutionFileSystem? fileSystem = null)
        {
            _fileSystem = fileSystem ?? StandardFileSystem.Instance;

            OutputPath = outputPath;
            Root = new Folder(this);

            Configurations = new List<SolutionConfiguration>();
            _readOnlyConfigurations = Configurations.AsReadOnly();

            ConfigurationBindings = new List<ConfigurationBinding>();
            PlatformBindings = new List<ConfigurationBinding>();
        }

        public void SetOutputDirectory(string outputDirectory)
        {
            _knownPaths.Clear();

            Root.ChangeItemsRootDirectory(outputDirectory);
            OutputPath = outputDirectory;
        }

        public class SolutionConfiguration : ISolutionConfiguration
        {
            public string Configuration { get; }
            public string Platform { get; }
            public List<ISolutionProjectContext> ProjectContexts { get; }

            private readonly IReadOnlyCollection<ISolutionProjectContext> _readOnlyProjectContexts;
            IReadOnlyCollection<ISolutionProjectContext> ISolutionConfiguration.ProjectContexts => _readOnlyProjectContexts;

            public SolutionConfiguration(string configuration, string platform)
            {
                Configuration = configuration;
                Platform = platform;

                ProjectContexts = new List<ISolutionProjectContext>();
                _readOnlyProjectContexts = ProjectContexts.AsReadOnly();
            }
        }

        public class ProjectContext : ISolutionProjectContext
        {
            public string ProjectPath { get; set; }
            public string Configuration { get; set; }
            public string Platform { get; set; }
            public bool Build { get; set; } = true;
            public bool Deploy { get; set; }

            public ProjectContext(string projectPath, string configuration, string platform)
            {
                ProjectPath = projectPath;
                Configuration = configuration;
                Platform = platform;
            }
        }

        public struct ConfigurationBinding
        {
            public string? ProjectsValue { get; set; }
            public string? SolutionValue { get; set; }

            public ConfigurationBinding(string projectsValue, string solutionValue)
            {
                ProjectsValue = projectsValue;
                SolutionValue = solutionValue;
            }
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
            {
                if (!AddEntry(projectPath, x => x._projectPaths, overwrite))
                    return;

                (string[] projectConfigurations, string[] projectPlatforms) = GetProjectConfigurationsAndPlatform();

                Dictionary<string, string?> resolvedSolutionPlatforms = projectPlatforms.ToDictionary(x => x, ResolveSolutionPlatform);

                foreach (string projectConfiguration in projectConfigurations)
                {
                    string? resolvedSolutionConfiguration = ResolveSolutionConfiguration(projectConfiguration);
                    if (resolvedSolutionConfiguration is null)
                        continue;

                    foreach (string projectPlatform in projectPlatforms)
                    {
                        string? resolvedSolutionPlatform = resolvedSolutionPlatforms[projectPlatform];
                        if (resolvedSolutionPlatform is null)
                            continue;

                        SolutionConfiguration? solutionConfiguration = _solution.Configurations.FirstOrDefault(x => x.Configuration == resolvedSolutionConfiguration && x.Platform == resolvedSolutionPlatform);
                        if (solutionConfiguration == null)
                        {
                            solutionConfiguration = new SolutionConfiguration(resolvedSolutionConfiguration, resolvedSolutionPlatform);
                            _solution.Configurations.Add(solutionConfiguration);
                        }

                        solutionConfiguration.ProjectContexts.Add(new ProjectContext(projectPath, projectConfiguration, projectConfiguration));
                    }
                }
            }

            private string? ResolveSolutionConfiguration(string projectConfiguration)
            {
                return _solution.ConfigurationBindings.FirstOrDefault(b => b.ProjectsValue == projectConfiguration).SolutionValue;
            }

            private string? ResolveSolutionPlatform(string projectPlatform)
            {
                return _solution.PlatformBindings.FirstOrDefault(b => b.ProjectsValue == projectPlatform).SolutionValue;
            }

            private (string[], string[]) GetProjectConfigurationsAndPlatform()
            {
                return (new[] { "Debug", "Release" }, new[] { "Any CPU" });
            }

            private bool AddEntry(string filePath, Func<Folder, HashSet<string>> getFolderContent, bool overwrite)
            {
                if (overwrite && _solution._knownPaths.Remove(filePath, out Folder folder))
                    getFolderContent(folder).Remove(filePath);

                if (!overwrite && _solution._knownPaths.ContainsKey(filePath))
                    return false;
                
                getFolderContent(this).Add(filePath);
                _solution._knownPaths.Add(filePath, this);
                return true;
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

                subFolder.Clear();
                _subFolders.Remove(subFolderName);
                return true;
            }

            public void Clear()
            {
                foreach (string filePath in _filePaths)
                    _solution._knownPaths.Remove(filePath);

                foreach (string projectPath in _projectPaths)
                    _solution._knownPaths.Remove(projectPath);

                _filePaths.Clear();
                _projectPaths.Clear();
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
                ReplacePaths(_filePaths);
                ReplacePaths(_projectPaths);

                foreach (Folder subFolder in SubFolders.Values)
                    subFolder.ChangeItemsRootDirectory(outputDirectory, previousOutputDirectory);

                void ReplacePaths(HashSet<string> collection)
                {
                    string[] previousFilePaths = collection.ToArray();

                    collection.Clear();
                    foreach (string previousFilePath in previousFilePaths)
                        _solution._knownPaths.Remove(previousFilePath);

                    foreach (string previousFilePath in previousFilePaths)
                    {
                        string newFilePath = _solution._fileSystem.MoveRelativePathRoot(previousFilePath, previousOutputDirectory, outputDirectory);

                        collection.Add(newFilePath);
                        _solution._knownPaths.Add(newFilePath, this);
                    }
                }
            }
        }
    }
}