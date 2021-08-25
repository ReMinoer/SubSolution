using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolutionConfigurationPlatform
    {
        string ConfigurationName { get; }
        string PlatformName { get; }
        string FullName => ConfigurationName + '|' + PlatformName;
        IReadOnlyList<string> MatchingProjectConfigurationNames { get; }
        IReadOnlyList<string> MatchingProjectPlatformNames { get; }
        IReadOnlyDictionary<string, SolutionProjectContext> ProjectContexts { get; }
    }
}