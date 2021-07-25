using System;
using System.IO;
using Microsoft.Extensions.Logging;
using SubSolution.Configuration;
using SubSolution.FileSystems;

namespace SubSolution
{
    public class SubSolutionContext
    {
        public SubSolutionConfiguration Configuration { get; }
        public string? ConfigurationFilePath { get; }
        public string SolutionPath { get; }
        public string WorkspaceDirectoryPath { get; }
        public ISubSolutionFileSystem? FileSystem { get; }

        public ILogger? Logger { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Trace;

        private SubSolutionContext(SubSolutionConfiguration configuration, string? configurationFilePath, string solutionPath, string workspaceDirectoryPath, ISubSolutionFileSystem? fileSystem)
        {
            Configuration = configuration;
            ConfigurationFilePath = configurationFilePath;
            SolutionPath = solutionPath;
            WorkspaceDirectoryPath = workspaceDirectoryPath;
            FileSystem = fileSystem;
        }

        static public SubSolutionContext FromConfigurationFile(string configurationFilePath, ISubSolutionFileSystem? fileSystem = null)
        {
            SubSolutionConfiguration configuration;

            using (Stream configurationStream = (fileSystem ?? StandardFileSystem.Instance).OpenStream(configurationFilePath))
            using (TextReader configurationReader = new StreamReader(configurationStream))
            {
                configuration = SubSolutionConfiguration.Load(configurationReader);
            }

            return CreateFileContext(configuration, configurationFilePath, fileSystem);
        }

        static private SubSolutionContext CreateFileContext(SubSolutionConfiguration configuration, string configurationFilePath, ISubSolutionFileSystem? fileSystem)
        {
            string defaultOutputDirectory = (fileSystem ?? StandardFileSystem.Instance).GetParentDirectoryPath(configurationFilePath) ?? Environment.CurrentDirectory;
            string solutionPath = ComputeSolutionPath(configuration, defaultOutputDirectory, configurationFilePath, fileSystem);
            string workspaceDirectoryPath = ComputeWorkspaceDirectoryPath(configuration, configurationFilePath, fileSystem);

            return new SubSolutionContext(configuration, configurationFilePath, solutionPath, workspaceDirectoryPath, fileSystem);
        }

        static public SubSolutionContext FromConfiguration(SubSolutionConfiguration configuration, string? defaultWorkspaceDirectory = null, ISubSolutionFileSystem? fileSystem = null)
        {
            string solutionPath = ComputeSolutionPath(configuration, Environment.CurrentDirectory, nameof(SubSolution), fileSystem);
            string? workspaceDirectoryPath = configuration.WorkspaceDirectory ?? defaultWorkspaceDirectory;

            if (workspaceDirectoryPath is null)
                throw new ArgumentNullException(nameof(defaultWorkspaceDirectory), "configuration.WorkspaceDirectory or defaultWorkspaceDirectory must be not null.");

            return new SubSolutionContext(configuration, null, solutionPath, workspaceDirectoryPath, fileSystem);
        }

        static public string ComputeSolutionName(SubSolutionConfiguration configuration, string configurationFilePath, ISubSolutionFileSystem? fileSystem)
        {
            string? solutionName = configuration.SolutionName;

            if (solutionName == null)
                return (fileSystem ?? StandardFileSystem.Instance).GetFileNameWithoutExtension(configurationFilePath);

            if (solutionName.EndsWith(".sln"))
                return solutionName[..^4];

            return solutionName;
        }

        static public string ComputeSolutionPath(SubSolutionConfiguration configuration, string defaultOutputDirectory, string configurationFilePath, ISubSolutionFileSystem? fileSystem)
        {
            string outputDirectory = configuration.OutputDirectory ?? defaultOutputDirectory;
            string solutionFileName = ComputeSolutionName(configuration, configurationFilePath, fileSystem) + ".sln";

            return (fileSystem ?? StandardFileSystem.Instance).Combine(outputDirectory, solutionFileName);
        }

        static public string ComputeWorkspaceDirectoryPath(SubSolutionConfiguration configuration, string configurationFilePath, ISubSolutionFileSystem? fileSystem)
        {
            return configuration.WorkspaceDirectory ?? (fileSystem ?? StandardFileSystem.Instance).GetParentDirectoryPath(configurationFilePath)!;
        }
    }
}