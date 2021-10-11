using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SubSolution
{
    static public class SolutionFolderExtension
    {
        static public IReadOnlyDictionary<string, ISolutionProject> GetAllProjects(this ISolutionFolder rootFolder)
        {
            var result = new Dictionary<string, ISolutionProject>();
            Add(rootFolder);
            return new ReadOnlyDictionary<string, ISolutionProject>(result);

            void Add(ISolutionFolder solutionFolder)
            {
                foreach ((string projectPath, ISolutionProject project) in solutionFolder.Projects)
                    result.Add(projectPath, project);

                foreach (ISolutionFolder subFolder in solutionFolder.SubFolders.Values)
                    Add(subFolder);
            }
        }

        static public IReadOnlyCollection<string> GetAllProjectPaths(this ISolutionFolder rootFolder)
        {
            var result = new List<string>();
            Add(rootFolder);
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