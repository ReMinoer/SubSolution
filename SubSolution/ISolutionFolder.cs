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

        IReadOnlyCollection<string> AllProjectPaths
        {
            get
            {
                List<string> result = new List<string>();
                Add(this);
                return result.AsReadOnly();

                void Add(ISolutionFolder solutionFolder)
                {
                    foreach (string projectPath in solutionFolder.Projects.Keys)
                        result.Add(projectPath);

                    foreach (ISolutionFolder subFolder in solutionFolder.SubFolders.Values)
                        Add(subFolder);
                }
            }
        }
    }
}