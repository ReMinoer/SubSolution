using SubSolution.Configuration;

namespace SubSolution
{
    public interface ISolutionBuilder
    {
        ISolutionOutput Build(SubSolutionConfiguration configuration);
    }
}