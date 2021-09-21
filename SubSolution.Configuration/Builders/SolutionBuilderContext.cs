﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;

namespace SubSolution.Configuration.Builders
{
    public class SolutionBuilderContext
    {
        public SubSolutionConfiguration Configuration { get; }
        public string? ConfigurationFilePath { get; }
        public string SolutionPath { get; }
        public string SolutionName { get; }
        public string WorkspaceDirectoryPath { get; }
        public IProjectReader ProjectReader { get; }
        public IFileSystem? FileSystem { get; }

        public ILogger? Logger { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Trace;
        public bool IgnoreConfigurationsAndPlatforms { get; set; }

        public SolutionBuilderContext(SubSolutionConfiguration configuration, string? configurationFilePath, string solutionPath, string workspaceDirectoryPath, IProjectReader projectReader, IFileSystem? fileSystem)
        {
            Configuration = configuration;
            ConfigurationFilePath = configurationFilePath;
            SolutionPath = solutionPath;
            SolutionName = (fileSystem ?? StandardFileSystem.Instance).GetFileNameWithoutExtension(solutionPath);
            WorkspaceDirectoryPath = workspaceDirectoryPath;
            ProjectReader = projectReader;
            FileSystem = fileSystem;
        }

        static public async Task<SolutionBuilderContext> FromConfigurationFileAsync(string configurationFilePath, IProjectReader projectReader, IFileSystem? fileSystem = null)
        {
            await using Stream stream = (fileSystem ?? StandardFileSystem.Instance).OpenStream(configurationFilePath);
            using TextReader textReader = new StreamReader(stream);

            SubSolutionConfiguration configuration = await Task.Run(() => SubSolutionConfiguration.Load(textReader));

            string defaultOutputDirectory = (fileSystem ?? StandardFileSystem.Instance).GetParentDirectoryPath(configurationFilePath) ?? Environment.CurrentDirectory;
            string solutionPath = ComputeSolutionPath(configuration, configurationFilePath, defaultOutputDirectory, fileSystem);
            string workspaceDirectoryPath = ComputeWorkspaceDirectoryPath(configuration, configurationFilePath, fileSystem);

            return new SolutionBuilderContext(configuration, configurationFilePath, solutionPath, workspaceDirectoryPath, projectReader, fileSystem);
        }

        static public SolutionBuilderContext FromConfiguration(SubSolutionConfiguration configuration, IProjectReader projectReader, string defaultOutputDirectory, string? defaultWorkspaceDirectory = null, IFileSystem? fileSystem = null)
        {
            string solutionPath = ComputeSolutionPath(configuration, nameof(SubSolution), defaultOutputDirectory, fileSystem);
            string? workspaceDirectoryPath = configuration.WorkspaceDirectory ?? defaultWorkspaceDirectory;

            if (workspaceDirectoryPath is null)
                throw new ArgumentNullException(nameof(defaultWorkspaceDirectory), "configuration.WorkspaceDirectory or defaultWorkspaceDirectory must be not null.");

            return new SolutionBuilderContext(configuration, null, solutionPath, workspaceDirectoryPath, projectReader, fileSystem);
        }

        static private string ComputeSolutionPath(SubSolutionConfiguration configuration, string configurationFilePath, string defaultOutputDirectory, IFileSystem? fileSystem)
        {
            string outputDirectory = configuration.OutputDirectory ?? defaultOutputDirectory;
            string solutionFileName = ComputeSolutionName(configuration, configurationFilePath, fileSystem) + ".sln";

            return (fileSystem ?? StandardFileSystem.Instance).Combine(outputDirectory, solutionFileName);
        }

        static private string ComputeSolutionName(SubSolutionConfiguration configuration, string configurationFilePath, IFileSystem? fileSystem)
        {
            string? solutionName = configuration.SolutionName;

            if (solutionName == null)
                return (fileSystem ?? StandardFileSystem.Instance).GetFileNameWithoutExtension(configurationFilePath);

            if (solutionName.EndsWith(".sln"))
                return solutionName[..^4];

            return solutionName;
        }

        static private string ComputeWorkspaceDirectoryPath(SubSolutionConfiguration configuration, string configurationFilePath, IFileSystem? fileSystem)
        {
            return configuration.WorkspaceDirectory ?? (fileSystem ?? StandardFileSystem.Instance).GetParentDirectoryPath(configurationFilePath)!;
        }
    }
}