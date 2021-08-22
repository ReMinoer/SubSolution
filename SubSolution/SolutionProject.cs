using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SubSolution
{
    public class SolutionProject : ISolutionProject
    {
        public string Path { get; set; }
        public List<string> Configurations { get; }
        public List<string> Platforms { get; }

        private readonly ReadOnlyCollection<string> _readOnlyConfigurations;
        private readonly ReadOnlyCollection<string> _readOnlyPlatforms;

        IReadOnlyCollection<string> ISolutionProject.Configurations => _readOnlyConfigurations;
        IReadOnlyCollection<string> ISolutionProject.Platforms => _readOnlyPlatforms;

        public SolutionProject(string path)
        {
            Path = path;
            Configurations = new List<string>();
            Platforms = new List<string>();

            _readOnlyConfigurations = Configurations.AsReadOnly();
            _readOnlyPlatforms = Platforms.AsReadOnly();
        }
    }
}