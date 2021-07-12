using System.Collections.Generic;
using SubSolution.Configuration.FileSystems;

namespace SubSolution.Configuration
{
    public interface ISolutionBuildContext
    {
        ISolutionBuilder SolutionBuilder { get; }
        string OriginWorkspaceDirectoryPath { get; }
        string CurrentWorkspaceDirectoryPath { get; }
        string[] CurrentFolderPath { get; }
        ISet<string> KnownConfigurationFilePaths { get; }
        ISubSolutionFileSystem FileSystem { get; }
        ISolutionBuildContext GetSubFolderContext(params string[] relativeFolderPath);
        ISolutionBuildContext GetNewWorkspaceDirectoryContext(string workspaceDirectoryPath);
    }
}