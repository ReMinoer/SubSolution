using System.Collections.Generic;
using System.Linq;

namespace SubSolution
{
    public class SolutionProject : ISolutionProject
    {
        public List<string> Configurations { get; }
        public List<string> Platforms { get; }
        public bool CanBuild { get; set; }
        public bool CanDeploy { get; set; }

        private readonly IReadOnlyList<string> _readOnlyConfigurations;
        private readonly IReadOnlyList<string> _readOnlyPlatforms;

        IReadOnlyList<string> ISolutionProject.Configurations => _readOnlyConfigurations;
        IReadOnlyList<string> ISolutionProject.Platforms => _readOnlyPlatforms;

        public SolutionProject()
        {
            Configurations = new List<string>();
            Platforms = new List<string>();

            _readOnlyConfigurations = Configurations.AsReadOnly();
            _readOnlyPlatforms = Platforms.AsReadOnly();
        }

        public SolutionProject(ISolutionProject project)
        {
            Configurations = project.Configurations.ToList();
            Platforms = project.Platforms.ToList();
            CanBuild = project.CanBuild;
            CanDeploy = project.CanDeploy;

            _readOnlyConfigurations = Configurations.AsReadOnly();
            _readOnlyPlatforms = Platforms.AsReadOnly();
        }
    }
}