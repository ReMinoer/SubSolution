using System;
using System.Collections.Generic;
using System.Linq;

namespace SubSolution
{
    public class SolutionProject : ISolutionProject
    {
        public ProjectType? Type { get; }
        public Guid TypeGuid { get; }
        public List<string> ProjectDependencies { get; }
        public List<string> Configurations { get; }
        public List<string> Platforms { get; }
        public bool CanBuild { get; set; }
        public bool CanDeploy { get; set; }
        public bool AlwaysDeploy { get; set; }

        private readonly IReadOnlyList<string> _readOnlyProjectDependencies;
        private readonly IReadOnlyList<string> _readOnlyConfigurations;
        private readonly IReadOnlyList<string> _readOnlyPlatforms;

        IReadOnlyList<string> ISolutionProject.ProjectDependencies => _readOnlyProjectDependencies;
        IReadOnlyList<string> ISolutionProject.Configurations => _readOnlyConfigurations;
        IReadOnlyList<string> ISolutionProject.Platforms => _readOnlyPlatforms;

        private SolutionProject()
        {
            ProjectDependencies = new List<string>();
            Configurations = new List<string>();
            Platforms = new List<string>();

            _readOnlyProjectDependencies = ProjectDependencies.AsReadOnly();
            _readOnlyConfigurations = Configurations.AsReadOnly();
            _readOnlyPlatforms = Platforms.AsReadOnly();
        }

        public SolutionProject(ProjectType projectType)
            : this()
        {
            Type = projectType;
            TypeGuid = ProjectTypes.Guids[projectType];
        }

        public SolutionProject(Guid projectTypeGuid)
            : this()
        {
            TypeGuid = projectTypeGuid;

            if (ProjectTypes.ByGuids.TryGetValue(projectTypeGuid, out ProjectType type))
                Type = type;
            else
                Type = null;
        }

        public SolutionProject(ISolutionProject project)
        {
            Type = project.Type;
            TypeGuid = project.TypeGuid;
            ProjectDependencies = project.ProjectDependencies.ToList();
            Configurations = project.Configurations.ToList();
            Platforms = project.Platforms.ToList();
            CanBuild = project.CanBuild;
            CanDeploy = project.CanDeploy;

            _readOnlyProjectDependencies = ProjectDependencies.AsReadOnly();
            _readOnlyConfigurations = Configurations.AsReadOnly();
            _readOnlyPlatforms = Platforms.AsReadOnly();
        }
    }
}