using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolution
    {
        ISolutionFolder Root { get; }
        IReadOnlyCollection<ISolutionConfiguration> Configurations { get; }
    }
}