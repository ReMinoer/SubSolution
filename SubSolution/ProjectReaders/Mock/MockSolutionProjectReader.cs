using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubSolution.ProjectReaders.Mock
{
    public class MockSolutionProjectReader : ISolutionProjectReader
    {
        public Dictionary<string, string[]> ProjectConfigurations { get; } = new Dictionary<string, string[]>();
        public Dictionary<string, string[]> ProjectPlatforms { get; } = new Dictionary<string, string[]>();
        public string[] ProjectDefaultConfigurations { get; }
        public string[] ProjectDefaultPlatforms { get; }
        public bool ProjectCanBuild { get; set; }
        public bool ProjectCanDeploy { get; set; }

        public MockSolutionProjectReader(string[] projectDefaultConfigurations, string[] projectDefaultPlatforms)
        {
            ProjectDefaultConfigurations = projectDefaultConfigurations;
            ProjectDefaultPlatforms = projectDefaultPlatforms;
        }

        public Task<ISolutionProject> ReadAsync(string absoluteProjectPath)
        {
            var solutionProject = new SolutionProject
            {
                CanBuild = ProjectCanBuild,
                CanDeploy = ProjectCanDeploy
            };

            solutionProject.Configurations.AddRange(ProjectConfigurations.TryGetValue(absoluteProjectPath, out string[] x) ? x : ProjectDefaultConfigurations);
            solutionProject.Platforms.AddRange(ProjectPlatforms.TryGetValue(absoluteProjectPath, out string[] y) ? y : ProjectDefaultPlatforms);

            return Task.FromResult<ISolutionProject>(solutionProject);
        }
    }
}