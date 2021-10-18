using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolutionConfigurationPlatform
    {
        string ConfigurationName { get; }
        string PlatformName { get; }
        string FullName { get; }
        IReadOnlyDictionary<string, SolutionProjectContext> ProjectContexts { get; }
    }
}