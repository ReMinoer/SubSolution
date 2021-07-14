using System;
using System.Collections.Generic;
using SubSolution.Configuration;
using SubSolution.Configuration.FileSystems;
using SubSolution.FileSystems;

namespace SubSolution.Builders
{
    public class SolutionBuildContext : ISolutionBuildContext
    {
        public ISolutionBuilder SolutionBuilder { get; }
        public string OriginWorkspaceDirectoryPath { get; }
        public string CurrentWorkspaceDirectoryPath { get; }
        public string[] CurrentFolderPath { get; }
        public ISet<string> KnownConfigurationFilePaths { get; }
        public ISubSolutionFileSystem FileSystem { get; }

        private SolutionBuildContext(ISolutionBuilder solutionBuilder, string originWorkspaceDirectoryPath, ISet<string> parsedConfigurationFilePaths, string? currentWorkspaceDirectoryPath, string[]? currentFolderPath, ISubSolutionFileSystem? fileSystem)
        {
            SolutionBuilder = solutionBuilder;
            OriginWorkspaceDirectoryPath = originWorkspaceDirectoryPath;
            CurrentWorkspaceDirectoryPath = currentWorkspaceDirectoryPath ?? originWorkspaceDirectoryPath;
            CurrentFolderPath = currentFolderPath ?? Array.Empty<string>();
            KnownConfigurationFilePaths = parsedConfigurationFilePaths;
            FileSystem = fileSystem ?? StandardSubSolutionFileSystem.Instance;
        }

        public SolutionBuildContext(ISolutionBuilder solutionBuilder, string originWorkspaceDirectoryPath, string? configurationFilePath = null, string? currentWorkspaceDirectoryPath = null, string[]? currentFolderPath = null, ISubSolutionFileSystem? fileSystem = null)
            : this(solutionBuilder, originWorkspaceDirectoryPath, new HashSet<string>(), currentWorkspaceDirectoryPath, currentFolderPath, fileSystem)
        {
            if (configurationFilePath != null)
                KnownConfigurationFilePaths.Add(configurationFilePath);
        }

        public ISolutionBuildContext GetSubFolderContext(params string[] relativeFolderPath)
        {
            return new SolutionBuildContext(SolutionBuilder, OriginWorkspaceDirectoryPath, KnownConfigurationFilePaths, CurrentWorkspaceDirectoryPath, CombineSolutionFolderPaths(CurrentFolderPath, relativeFolderPath), FileSystem);
        }

        public ISolutionBuildContext GetNewWorkspaceDirectoryContext(string workspaceDirectoryPath)
        {
            return new SolutionBuildContext(SolutionBuilder, OriginWorkspaceDirectoryPath, KnownConfigurationFilePaths, workspaceDirectoryPath, CurrentFolderPath, FileSystem);
        }

        static private string[] CombineSolutionFolderPaths(string[] firstPath, string[] secondPath)
        {
            string[] result = new string[firstPath.Length + secondPath.Length];

            firstPath.CopyTo(result, 0);
            secondPath.CopyTo(result, firstPath.Length);

            return result;
        }
    }
}