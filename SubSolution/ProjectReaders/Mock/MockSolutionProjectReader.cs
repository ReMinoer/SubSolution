using System.Threading.Tasks;

namespace SubSolution.ProjectReaders.Mock
{
    public class MockSolutionProjectReader : ISolutionProjectReader
    {
        public string[] ProjectConfigurations { get; set; }
        public string[] ProjectPlatforms { get; set; }

        public MockSolutionProjectReader(string[] projectConfigurations, string[] projectPlatforms)
        {
            ProjectConfigurations = projectConfigurations;
            ProjectPlatforms = projectPlatforms;
        }

        public Task<ISolutionProject> ReadAsync(string projectPath, string rootDirectory)
        {
            var solutionProject = new SolutionProject(projectPath);
            solutionProject.Configurations.AddRange(ProjectConfigurations);
            solutionProject.Platforms.AddRange(ProjectPlatforms);

            return Task.FromResult<ISolutionProject>(solutionProject);
        }
    }
}