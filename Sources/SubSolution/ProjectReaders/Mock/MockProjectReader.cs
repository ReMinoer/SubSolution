using System.Collections.Generic;
using System.Threading.Tasks;
using SubSolution.FileSystems;

namespace SubSolution.ProjectReaders.Mock
{
    public class MockProjectReader : IProjectReader
    {
        public Dictionary<string, ISolutionProject> Projects { get; }
        public ISolutionProject DefaultProject { get; set; }

        public MockProjectReader(IFileSystem fileSystem, ISolutionProject defaultProject)
        {
            Projects = new Dictionary<string, ISolutionProject>(fileSystem.PathComparer);
            DefaultProject = defaultProject;
        }

        public Task<ISolutionProject> ReadAsync(string absoluteProjectPath)
        {
            if (Projects.TryGetValue(absoluteProjectPath, out ISolutionProject project))
                return Task.FromResult(project);

            return Task.FromResult<ISolutionProject>(new SolutionProject(DefaultProject));
        }
    }
}