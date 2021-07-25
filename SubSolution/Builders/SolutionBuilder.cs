using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly SolutionOutput _solutionOutput;

        private readonly string _initialWorkspaceDirectoryPath;

        private readonly ISubSolutionFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;

        private readonly ISet<string> _knownConfigurationFilePaths;
        private readonly Stack<string> _currentWorkspacePathStack;
        private readonly Stack<string> _currentFolderPathStack;

        private string CurrentWorkspaceDirectoryPath => _currentWorkspacePathStack.Peek();
        private IEnumerable<string> CurrentFolderPath => _currentFolderPathStack.Reverse();

        public SolutionBuilder(SubSolutionContext context)
        {
            _solutionOutput = new SolutionOutput(context.SolutionPath);
            _initialWorkspaceDirectoryPath = context.WorkspaceDirectoryPath;

            _knownConfigurationFilePaths = new HashSet<string>();
            if (context.ConfigurationFilePath != null)
                _knownConfigurationFilePaths.Add(context.ConfigurationFilePath);

            _currentWorkspacePathStack = new Stack<string>();
            _currentWorkspacePathStack.Push(_initialWorkspaceDirectoryPath);

            _currentFolderPathStack = new Stack<string>();

            _fileSystem = context.FileSystem ?? StandardFileSystem.Instance;
            _logger = context.Logger ?? NullLogger.Instance;
            _logLevel = context.LogLevel;
        }

        private void Log(string message) => _logger.Log(_logLevel, message);

        private IDisposable AppendSubFolderPath(params string[] relativeFolderPath)
        {
            foreach (string folderName in relativeFolderPath)
                _currentFolderPathStack.Push(folderName);

            string currentFolderPath = _currentFolderPathStack.Count > 0 ? string.Join('/', CurrentFolderPath) : LogTokenRoot;
            Log($"Set current solution folder to: {currentFolderPath}");

            return new Disposable(() =>
            {
                for (int i = 0; i < relativeFolderPath.Length; i++)
                    _currentFolderPathStack.Pop();

                currentFolderPath = _currentFolderPathStack.Count > 0 ? string.Join('/', CurrentFolderPath) : LogTokenRoot;
                Log($"Set current solution folder back to: {currentFolderPath}");
            });
        }

        private IDisposable NewWorkspaceDirectory(string workspaceDirectoryPath)
        {
            _currentWorkspacePathStack.Push(workspaceDirectoryPath);
            Log($"Set current workspace directory to: {CurrentWorkspaceDirectoryPath}");

            return new Disposable(() =>
            {
                _currentWorkspacePathStack.Pop();
                Log($"Set current workspace directory back to: {CurrentWorkspaceDirectoryPath}");
            });
        }

        public ISolutionOutput Build(SubSolutionConfiguration configuration)
        {
            Log("Start building solution");
            Log($"Configuration file: {_knownConfigurationFilePaths.FirstOrDefault() ?? LogTokenNone}");
            Log($"Solution output file: {_solutionOutput.OutputPath}");
            Log($"Initial workspace directory: {_initialWorkspaceDirectoryPath}");

            Visit(configuration.Root);
            return _solutionOutput;
        }

        public void Visit(SolutionRootConfiguration root)
        {
            if (root == null)
                return;

            VisitRoot(root);
        }

        public void Visit(Folder folder)
        {
            using (AppendSubFolderPath(folder.Name))
                VisitRoot(folder.Content);
        }

        private void VisitRoot(SolutionRootConfiguration root)
        {
            foreach (SolutionItems items in root.SolutionItems)
                items.Accept(this);
        }

        public void Visit(Files files)
        {
            foreach (string relativeFilePath in GetMatchingFilePaths(files.Path, defaultFileExtension: "*"))
                AddFoldersAndFileToSolution(relativeFilePath, _solutionOutput.AddFile, files.CreateFolders == true);
        }

        public void Visit(Projects projects)
        {
            foreach (string relativeFilePath in GetMatchingFilePaths(projects.Path, defaultFileExtension: "csproj"))
                AddFoldersAndFileToSolution(relativeFilePath, _solutionOutput.AddProject, projects.CreateFolders == true);
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
                string filePath = _fileSystem.Combine(CurrentWorkspaceDirectoryPath, relativeFilePath);
                if (!_knownConfigurationFilePaths.Add(filePath))
                    continue;

                SubSolutionConfiguration configuration;
                using (Stream configurationStream = _fileSystem.OpenStream(filePath))
                using (TextReader configurationReader = new StreamReader(configurationStream))
                    configuration = SubSolutionConfiguration.Load(configurationReader);

                var workspaceDirectoryPath = SubSolutionContext.ComputeWorkspaceDirectoryPath(configuration, filePath, _fileSystem);

                using (NewWorkspaceDirectory(workspaceDirectoryPath))
                {
                    if (subSolutions.CreateRootFolder == true)
                    {
                        using (AppendSubFolderPath(SubSolutionContext.ComputeSolutionName(configuration, filePath, _fileSystem)))
                            configuration.Root.Accept(this);
                    }
                    else
                    {
                        configuration.Root.Accept(this);
                    }
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

            return _fileSystem.GetFilesMatchingGlobPattern(CurrentWorkspaceDirectoryPath, globPattern);
        }

        private void AddFoldersAndFileToSolution(string relativeFilePath, Action<string, IEnumerable<string>> addFile, bool createFolders)
        {
            string filePath = _fileSystem.Combine(CurrentWorkspaceDirectoryPath, relativeFilePath);
            relativeFilePath = _fileSystem.MakeRelativePath(_initialWorkspaceDirectoryPath, filePath);

            if (createFolders)
            {
                string relativeDirectoryPath = _fileSystem.GetParentDirectoryPath(relativeFilePath) ?? string.Empty;
                string[] solutionFolderPath = _fileSystem.SplitPath(relativeDirectoryPath);

                using (AppendSubFolderPath(solutionFolderPath))
                {
                    Log($"Add: {relativeFilePath}");
                    addFile(relativeFilePath, CurrentFolderPath);
                }
            }
            else
            {
                Log($"Add: {relativeFilePath}");
                addFile(relativeFilePath, CurrentFolderPath);
            }
        }
    }
}