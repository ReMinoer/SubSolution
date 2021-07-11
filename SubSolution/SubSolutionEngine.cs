using System;
using SubSolution.Builders;
using SubSolution.Configuration;
using SubSolution.Configuration.FileSystems;

namespace SubSolution
{
    static public class SubSolutionEngine
    {
        static public SolutionBuilder Process(string configurationFilePath) => Process(configurationFilePath, SolutionBuilder.FromPath);

        static public TSolutionBuilder Process<TSolutionBuilder>(string configurationFilePath, Func<string, TSolutionBuilder> getSolutionBuilder,
            ISubSolutionFileSystem? fileSystem = null)
            where TSolutionBuilder : ISolutionBuilder
        {
            var configuration = SubSolutionConfiguration.Load(configurationFilePath);

            var solutionPath = configuration.ComputeSolutionPath(Environment.CurrentDirectory, configurationFilePath, fileSystem);
            var workspaceDirectoryPath = configuration.ComputeWorkspaceDirectoryPath(configurationFilePath, fileSystem);

            return Process(configuration, getSolutionBuilder, solutionPath, workspaceDirectoryPath, fileSystem);
        }

        static public TSolutionBuilder Process<TSolutionBuilder>(SubSolutionConfiguration configuration, Func<string, TSolutionBuilder> getSolutionBuilder,
            string? defaultWorkspaceDirectory = null,
            ISubSolutionFileSystem? fileSystem = null)
            where TSolutionBuilder : ISolutionBuilder
        {
            var solutionPath = configuration.ComputeSolutionPath(Environment.CurrentDirectory, nameof(SubSolution));
            var workspaceDirectoryPath = configuration.WorkspaceDirectory ?? defaultWorkspaceDirectory;

            if (workspaceDirectoryPath is null)
                throw new ArgumentNullException(nameof(defaultWorkspaceDirectory), "configuration.WorkspaceDirectory or defaultWorkspaceDirectory must be not null.");
            
            return Process(configuration, getSolutionBuilder, solutionPath, workspaceDirectoryPath, fileSystem);
        }
        
        static private TSolutionBuilder Process<TSolutionBuilder>(SubSolutionConfiguration configuration, Func<string, TSolutionBuilder> getSolutionBuilder,
            string solutionPath,
            string workspaceDirectoryPath,
            ISubSolutionFileSystem? fileSystem = null)
            where TSolutionBuilder : ISolutionBuilder
        {
            var solutionBuilder = getSolutionBuilder(solutionPath);
            var solutionBuildContext = new SolutionBuildContext(solutionBuilder, workspaceDirectoryPath, fileSystem: fileSystem);

            configuration.Root?.AddToSolution(solutionBuildContext);
            return solutionBuilder;
        }
    }
}