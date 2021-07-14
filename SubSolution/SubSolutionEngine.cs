using System;
using System.IO;
using SubSolution.Builders;
using SubSolution.Configuration;
using SubSolution.Configuration.FileSystems;
using SubSolution.FileSystems;

namespace SubSolution
{
    static public class SubSolutionEngine
    {
        static public SolutionBuilder ProcessConfigurationFile(string configurationFilePath, ISubSolutionFileSystem? fileSystem = null)
        {
            SubSolutionConfiguration configuration;

            using (Stream configurationStream = (fileSystem ?? StandardSubSolutionFileSystem.Instance).OpenStream(configurationFilePath))
            using (TextReader configurationReader = new StreamReader(configurationStream))
            {
                configuration = SubSolutionConfiguration.Load(configurationReader);
            }

            string solutionPath = configuration.ComputeSolutionPath(Environment.CurrentDirectory, configurationFilePath, fileSystem);
            string workspaceDirectoryPath = configuration.ComputeWorkspaceDirectoryPath(configurationFilePath, fileSystem);

            return Process(configuration, configurationFilePath, solutionPath, workspaceDirectoryPath, fileSystem);
        }

        static public SolutionBuilder ProcessConfiguration(SubSolutionConfiguration configuration, string? defaultWorkspaceDirectory = null, ISubSolutionFileSystem? fileSystem = null)
        {
            string solutionPath = configuration.ComputeSolutionPath(Environment.CurrentDirectory, nameof(SubSolution), fileSystem);
            string? workspaceDirectoryPath = configuration.WorkspaceDirectory ?? defaultWorkspaceDirectory;

            if (workspaceDirectoryPath is null)
                throw new ArgumentNullException(nameof(defaultWorkspaceDirectory), "configuration.WorkspaceDirectory or defaultWorkspaceDirectory must be not null.");
            
            return Process(configuration, null, solutionPath, workspaceDirectoryPath, fileSystem);
        }
        
        static private SolutionBuilder Process(SubSolutionConfiguration configuration, string? configurationFilePath, string solutionPath, string workspaceDirectoryPath, ISubSolutionFileSystem? fileSystem)
        {
            var solutionBuilder = new SolutionBuilder(solutionPath, fileSystem);
            var solutionBuildContext = new SolutionBuildContext(solutionBuilder, workspaceDirectoryPath, configurationFilePath, fileSystem: fileSystem);

            configuration.Root?.AddToSolution(solutionBuildContext);
            return solutionBuilder;
        }
    }
}