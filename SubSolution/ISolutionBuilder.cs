using System.Threading.Tasks;
using SubSolution.Configuration;

namespace SubSolution
{
    public interface ISolutionBuilder
    {
        Task<ISolution> BuildAsync(SubSolutionConfiguration configuration);
    }
}