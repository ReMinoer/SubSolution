using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolutionConfiguration
    {
        string Configuration { get; }
        string Platform { get; }
        IReadOnlyCollection<ISolutionProjectContext> ProjectContexts { get; }
    }
}