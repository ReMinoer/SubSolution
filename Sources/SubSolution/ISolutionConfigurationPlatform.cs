using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolutionConfigurationPlatform
    {
        string ConfigurationName { get; }
        string PlatformName { get; }
        string FullName => ConfigurationName + '|' + PlatformName;
        IReadOnlyDictionary<string, SolutionProjectContext> ProjectContexts { get; }
    }
}