using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolution
    {
        ISolutionFolder Root { get; }
        IReadOnlyList<ISolutionConfigurationPlatform> ConfigurationPlatforms { get; }
        string SolutionName { get; }
        string OutputPath { get; }
        string OutputDirectory { get; }
    }
}