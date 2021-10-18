using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubSolution.Builders.Configuration;
using SubSolution.Builders.GlobPatterns;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;

namespace SubSolution.Builders
{
    public class SolutionBuilderContext
    {
        public SubSolutionConfiguration Configuration { get; }
        public string? ConfigurationFilePath { get; }
        public string SolutionPath { get; }
        public string SolutionDirectoryPath { get; }
        public string SolutionName { get; }
        public string WorkspaceDirectoryPath { get; }
        public IProjectReader ProjectReader { get; }
        public IGlobPatternFileSystem? FileSystem { get; }

        public ILogger? Logger { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Trace;
        public bool IgnoreConfigurationsAndPlatforms { get; set; }

        public SolutionBuilderContext(SubSolutionConfiguration configuration, string? configurationFilePath, string solutionPath, string workspaceDirectoryPath, IProjectReader projectReader, IGlobPatternFileSystem? fileSystem)
        {
            IGlobPatternFileSystem activeFileSystem = fileSystem ?? StandardGlobPatternFileSystem.Instance;

            Configuration = configuration;
            ConfigurationFilePath = configurationFilePath;
            SolutionPath = solutionPath;
            SolutionDirectoryPath = activeFileSystem.GetParentDirectoryPath(solutionPath)!;
            SolutionName = activeFileSystem.GetFileNameWithoutExtension(solutionPath);
            WorkspaceDirectoryPath = workspaceDirectoryPath;
            ProjectReader = projectReader;
            FileSystem = fileSystem;
        }

        static public async Task<SolutionBuilderContext> FromConfigurationFileAsync(string configurationFilePath, IProjectReader projectReader, IGlobPatternFileSystem? fileSystem = null)
        {
            IFileSystem activeFileSystem = fileSystem ?? StandardGlobPatternFileSystem.Instance;

            await using IAsyncDisposable _ = activeFileSystem.OpenStream(configurationFilePath)
                .AsAsyncDisposable(out Stream stream);

            using TextReader textReader = new StreamReader(stream);

            SubSolutionConfiguration configuration = await Task.Run(() => SubSolutionConfiguration.Load(textReader));

            string defaultOutputDirectory = activeFileSystem.GetParentDirectoryPath(configurationFilePath)!;
            string solutionPath = ComputeSolutionPath(configuration, configurationFilePath, defaultOutputDirectory, fileSystem);
            string workspaceDirectoryPath = ComputeWorkspaceDirectoryPath(configuration, configurationFilePath, fileSystem);

            return new SolutionBuilderContext(configuration, configurationFilePath, solutionPath, workspaceDirectoryPath, projectReader, fileSystem);
        }

        static public SolutionBuilderContext FromConfiguration(SubSolutionConfiguration configuration, IProjectReader projectReader, string defaultOutputDirectory, string? defaultWorkspaceDirectory = null, IGlobPatternFileSystem? fileSystem = null)
        {
            string solutionPath = ComputeSolutionPath(configuration, nameof(SubSolution), defaultOutputDirectory, fileSystem);
            string? workspaceDirectoryPath = configuration.WorkspaceDirectory ?? defaultWorkspaceDirectory;

            if (workspaceDirectoryPath is null)
                throw new ArgumentNullException(nameof(defaultWorkspaceDirectory), "configuration.WorkspaceDirectory or defaultWorkspaceDirectory must be not null.");

            workspaceDirectoryPath = (fileSystem ?? StandardGlobPatternFileSystem.Instance)
                .MakeAbsolutePath(Environment.CurrentDirectory, workspaceDirectoryPath);

            return new SolutionBuilderContext(configuration, null, solutionPath, workspaceDirectoryPath, projectReader, fileSystem);
        }

        static private string ComputeSolutionPath(SubSolutionConfiguration configuration, string configurationFilePath, string defaultOutputDirectory, IGlobPatternFileSystem? fileSystem)
        {
            string outputDirectory = configuration.OutputDirectory ?? defaultOutputDirectory;
            string solutionFileName = ComputeSolutionName(configuration, configurationFilePath, fileSystem) + ".sln";

            return (fileSystem ?? StandardGlobPatternFileSystem.Instance).Combine(outputDirectory, solutionFileName);
        }

        static private string ComputeSolutionName(SubSolutionConfiguration configuration, string configurationFilePath, IGlobPatternFileSystem? fileSystem)
        {
            string? solutionName = configuration.SolutionName;

            if (solutionName == null)
                return (fileSystem ?? StandardGlobPatternFileSystem.Instance).GetFileNameWithoutExtension(configurationFilePath);

            if (solutionName.EndsWith(".sln"))
                return solutionName[..^4];

            return solutionName;
        }

        static private string ComputeWorkspaceDirectoryPath(SubSolutionConfiguration configuration, string configurationFilePath, IGlobPatternFileSystem? fileSystem)
        {
            fileSystem ??= StandardGlobPatternFileSystem.Instance;

            string workspaceDirectoryPath = configuration.WorkspaceDirectory ?? fileSystem.GetParentDirectoryPath(configurationFilePath)!;
            return fileSystem.MakeAbsolutePath(Environment.CurrentDirectory, workspaceDirectoryPath);
        }
    }
}