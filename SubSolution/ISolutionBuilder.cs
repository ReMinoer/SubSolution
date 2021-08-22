using System.Threading.Tasks;
using SubSolution.Configuration;

namespace SubSolution
{
    public interface ISolutionBuilder
    {
        Task<ISolutionOutput> BuildAsync(SubSolutionConfiguration configuration);
    }
}