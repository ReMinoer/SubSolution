using System.Collections.Generic;
using SubSolution.Utils;

namespace SubSolution
{
    public interface ISolutionFolder
    {
        IReadOnlyCollection<string> FilePaths { get; }
        IReadOnlyDictionary<string, ISolutionProject> Projects { get; }
        ICovariantReadOnlyDictionary<string, ISolutionFolder> SubFolders { get; }
    }
}