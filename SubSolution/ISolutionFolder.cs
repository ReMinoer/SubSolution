using System;
using System.Collections.Generic;
using SubSolution.Utils;

namespace SubSolution
{
    public interface ISolutionFolder
    {
        IReadOnlyCollection<string> FilePaths { get; }
        IReadOnlyDictionary<string, ISolutionProject> Projects { get; }
        ICovariantReadOnlyDictionary<string, ISolutionFolder> SubFolders { get; }
        void FilterProjects(Func<string, ISolutionProject, bool> predicate);
        void FilterFiles(Func<string, bool> predicate);
    }
}