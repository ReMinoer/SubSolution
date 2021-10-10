using System;
using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolutionProject
    {
        ProjectType? Type { get; }
        Guid TypeGuid { get; }
        IReadOnlyList<string> ProjectDependencies { get; }
        IReadOnlyList<string> Configurations { get; }
        IReadOnlyList<string> Platforms { get; }
        bool CanBuild { get; }
        bool CanDeploy { get; }
        bool AlwaysDeploy { get; }
    }
}