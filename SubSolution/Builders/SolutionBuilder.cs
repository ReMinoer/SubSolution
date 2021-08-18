using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SubSolution.Configuration;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution.Builders
{
    public class SolutionBuilder : ISolutionBuilder, ISubSolutionConfigurationVisitor
    {
        private const string LogTokenNone = "*none*";
        private const string LogTokenRoot = "*root*";

        private readonly string _workspaceDirectoryPath;
        
        private readonly SolutionOutput _solutionOutput;
        private readonly Stack<SolutionOutput.Folder> _currentFolderStack;
        private readonly Stack<string> _currentFolderPathStack;

        private SolutionOutput.Folder CurrentFolder => _currentFolderStack.Peek();
        private string CurrentFolderPath => _currentFolderPathStack.Count > 0 ? string.Join('/', _currentFolderPathStack.Reverse()) : LogTokenRoot;

        private readonly ISubSolutionFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        private readonly ISet<string> _knownConfigurationFilePaths;

        public SolutionBuilder(SubSolutionContext context)
        {
            _workspaceDirectoryPath = context.WorkspaceDirectoryPath;

            _solutionOutput = new SolutionOutput(context.SolutionPath, context.FileSystem);
            _currentFolderStack = new Stack<SolutionOutput.Folder>();
            _currentFolderStack.Push(_solutionOutput.Root);
            _currentFolderPathStack = new Stack<string>();

            _knownConfigurationFilePaths = new HashSet<string>();
            if (context.ConfigurationFilePath != null)
                _knownConfigurationFilePaths.Add(context.ConfigurationFilePath);

            _fileSystem = context.FileSystem ?? StandardFileSystem.Instance;
            _logger = context.Logger ?? NullLogger.Instance;
            _logLevel = context.LogLevel;
        }

        public ISolutionOutput Build(SubSolutionConfiguration configuration)
        {
            Log("Start building solution");
            Log($"Configuration file: {_knownConfigurationFilePaths.FirstOrDefault() ?? LogTokenNone}");
            Log($"Solution output file: {_solutionOutput.OutputPath}");
            Log($"Initial workspace directory: {_workspaceDirectoryPath}");

            VisitConfigurationBindings(configuration.ConfigurationBindings);

            if (configuration.Root != null)
                VisitRoot(configuration.Root);

            return _solutionOutput;
        }

        private void VisitConfigurationBindings(SolutionConfigurationBindingList? bindingList)
        {
            if (bindingList == null)
            {
                AddDefaultConfigurationBindings();
                return;
            }

            if (bindingList.UseDefaultBindings == true)
                AddDefaultConfigurationBindings();

            foreach (Binding binding in bindingList.Binding)
                binding.Accept(this);
        }

        private void AddDefaultConfigurationBindings()
        {
            _solutionOutput.ConfigurationBindings.Add(new SolutionOutput.ConfigurationBinding("Debug", "Debug"));
            _solutionOutput.ConfigurationBindings.Add(new SolutionOutput.ConfigurationBinding("Release", "Release"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("Any CPU", "Any CPU"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("x86", "x86"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("x64", "x64"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("Any CPU", "x86"));
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding("Any CPU", "x64"));
        }

        public void Visit(Configuration.Configuration configuration)
        {
            _solutionOutput.ConfigurationBindings.Add(new SolutionOutput.ConfigurationBinding(configuration.Project, configuration.Solution));
        }

        public void Visit(Platform platform)
        {
            _solutionOutput.PlatformBindings.Add(new SolutionOutput.ConfigurationBinding(platform.Project, platform.Solution));
        }

        private void VisitRoot(SolutionRootConfiguration root)
        {
            foreach (SolutionItems items in root.SolutionItems)
                items.Accept(this);
        }

        public void Visit(Folder folder)
        {
            using (MoveCurrentFolder(folder.Name))
                VisitRoot(folder.Content);
        }

        public void Visit(Files files)
        {
            foreach (string relativeFilePath in GetMatchingFilePaths(files.Path, defaultFileExtension: "*"))
                AddFoldersAndFileToSolution(relativeFilePath, (folder, file, overwrite) => folder.AddFile(file, overwrite), files.CreateFolders == true, files.Overwrite == true);
        }

        public void Visit(Projects projects)
        {
            foreach (string relativeFilePath in GetMatchingFilePaths(projects.Path, defaultFileExtension: "csproj"))
                AddFoldersAndFileToSolution(relativeFilePath, (folder, file, overwrite) => folder.AddProject(file, overwrite), projects.CreateFolders == true, projects.Overwrite == true);
        }

        public void Visit(SubSolutions subSolutions)
        {
            IEnumerable<string> matchingFilePaths = GetMatchingFilePaths(subSolutions.Path, defaultFileExtension: "subsln");
            if (subSolutions.ReverseOrder == true)
            {
                matchingFilePaths = matchingFilePaths.Reverse();
            }

            foreach (string relativeFilePath in matchingFilePaths)
            {
                string filePath = _fileSystem.Combine(_workspaceDirectoryPath, relativeFilePath);
                if (!_knownConfigurationFilePaths.Add(filePath) && subSolutions.Overwrite != true)
                    continue;

                SubSolutionContext subContext = SubSolutionContext.FromConfigurationFile(filePath, _fileSystem);
                subContext.Logger = _logger;
                subContext.LogLevel = _logLevel;

                SolutionBuilder solutionBuilder = new SolutionBuilder(subContext);
                ISolutionOutput subSolution = solutionBuilder.Build(subContext.Configuration);

                string outputDirectory = _fileSystem.GetParentDirectoryPath(_solutionOutput.OutputPath)!;
                subSolution.SetOutputDirectory(outputDirectory);

                if (subSolutions.CreateRootFolder == true)
                {
                    using (MoveCurrentFolder(subContext.SolutionName))
                        CurrentFolder.AddFolderContent(subSolution.Root, subSolutions.Overwrite == true);
                }
                else
                {
                    CurrentFolder.AddFolderContent(subSolution.Root, subSolutions.Overwrite == true);
                }
            }
        }

        private IEnumerable<string> GetMatchingFilePaths(string? globPattern, string defaultFileExtension)
        {
            if (string.IsNullOrEmpty(globPattern))
                globPattern = "**/*." + defaultFileExtension;
            else if (globPattern.EndsWith("/") || globPattern.EndsWith("\\"))
                globPattern += "*." + defaultFileExtension;
            else if (globPattern.EndsWith("**"))
                globPattern += "/*." + defaultFileExtension;

            Log($"Search for files matching pattern: {globPattern}");

            return _fileSystem.GetFilesMatchingGlobPattern(_workspaceDirectoryPath, globPattern);
        }

        private void AddFoldersAndFileToSolution(string relativeFilePath, Action<SolutionOutput.Folder, string, bool> addEntry, bool createFolders, bool overwrite)
        {
            string filePath = _fileSystem.Combine(_workspaceDirectoryPath, relativeFilePath);
            relativeFilePath = _fileSystem.MakeRelativePath(_workspaceDirectoryPath, filePath);

            if (createFolders)
            {
                string relativeDirectoryPath = _fileSystem.GetParentDirectoryPath(relativeFilePath) ?? string.Empty;
                string[] solutionFolderPath = _fileSystem.SplitPath(relativeDirectoryPath);

                using (MoveCurrentFolder(solutionFolderPath))
                {
                    Log($"Add: {relativeFilePath}");
                    addEntry(CurrentFolder, relativeFilePath, overwrite);
                }
            }
            else
            {
                Log($"Add: {relativeFilePath}");
                addEntry(CurrentFolder, relativeFilePath, overwrite);
            }
        }

        private IDisposable MoveCurrentFolder(params string[] relativeFolderPath)
        {
            foreach (string folderName in relativeFolderPath)
                _currentFolderPathStack.Push(folderName);

            Log($"Set current solution folder to: {CurrentFolderPath}");

            _currentFolderStack.Push(CurrentFolder.GetOrCreateSubFolder(relativeFolderPath));

            return new Disposable(() =>
            {
                for (int i = 0; i < relativeFolderPath.Length; i++)
                    _currentFolderPathStack.Pop();

                Log($"Set current solution folder back to: {CurrentFolderPath}");

                _currentFolderStack.Pop();

                RemoveEmptySubFolders(CurrentFolder);
            });
        }

        static private void RemoveEmptySubFolders(SolutionOutput.Folder folder)
        {
            foreach (SolutionOutput.Folder subFolder in folder.SubFolders.Values)
                RemoveEmptySubFolders(subFolder);

            IEnumerable<string> emptySubFolderNames = folder.SubFolders.Where(x => x.Value.IsEmpty).Select(x => x.Key);
            foreach (string emptySubFolderName in emptySubFolderNames)
                folder.RemoveSubFolder(emptySubFolderName);
        }

        private void Log(string message) => _logger.Log(_logLevel, message);
    }
}