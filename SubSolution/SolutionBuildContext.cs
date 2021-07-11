using System;
using SubSolution.Configuration;
using SubSolution.Configuration.FileSystems;
using SubSolution.FileSystems;

namespace SubSolution
{
    public class SolutionBuildContext : ISolutionBuildContext
    {
        public ISolutionBuilder SolutionBuilder { get; }
        public string OriginWorkspaceDirectoryPath { get; }
        public string CurrentWorkspaceDirectoryPath { get; }
        public string[] CurrentFolderPath { get; }
        public ISubSolutionFileSystem FileSystem { get; }

        public SolutionBuildContext(ISolutionBuilder solutionBuilder, string originWorkspaceDirectoryPath, string? currentWorkspaceDirectoryPath = null, string[]? currentFolderPath = null, ISubSolutionFileSystem? fileSystem = null)
        {
            SolutionBuilder = solutionBuilder;
            OriginWorkspaceDirectoryPath = originWorkspaceDirectoryPath;
            CurrentWorkspaceDirectoryPath = currentWorkspaceDirectoryPath ?? originWorkspaceDirectoryPath;
            CurrentFolderPath = currentFolderPath ?? Array.Empty<string>();
            FileSystem = fileSystem ?? StandardSubSolutionFileSystem.Instance;
        }

        public ISolutionBuildContext GetSubFolderContext(params string[] relativeFolderPath)
        {
            return new SolutionBuildContext(SolutionBuilder, OriginWorkspaceDirectoryPath, CurrentWorkspaceDirectoryPath, CombineSolutionFolderPaths(CurrentFolderPath, relativeFolderPath), FileSystem);
        }

        public ISolutionBuildContext GetNewWorkspaceDirectoryContext(string workspaceDirectoryPath)
        {
            return new SolutionBuildContext(SolutionBuilder, OriginWorkspaceDirectoryPath, workspaceDirectoryPath, CurrentFolderPath, FileSystem);
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