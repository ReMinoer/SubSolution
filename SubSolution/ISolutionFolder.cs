using System.Collections.Generic;
using SubSolution.Utils;

namespace SubSolution
{
    public interface ISolutionFolder
    {
        IReadOnlyCollection<string> FilePaths { get; }
        IReadOnlyCollection<string> ProjectPaths { get; }
        ICovariantReadOnlyDictionary<string, ISolutionFolder> SubFolders { get; }
    }
}