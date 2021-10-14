using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolution
    {
        ISolutionFolder Root { get; }
        IReadOnlyList<ISolutionConfigurationPlatform> ConfigurationPlatforms { get; }
        string OutputDirectoryPath { get; }
    }
}