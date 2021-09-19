using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolutionProject
    {
        IReadOnlyList<string> ProjectDependencies { get; }
        IReadOnlyList<string> Configurations { get; }
        IReadOnlyList<string> Platforms { get; }
        bool CanBuild { get; }
        bool CanDeploy { get; }
    }
}