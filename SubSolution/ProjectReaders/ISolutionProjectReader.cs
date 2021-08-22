using System.Threading.Tasks;

namespace SubSolution.ProjectReaders
{
    public interface ISolutionProjectReader
    {
        Task<ISolutionProject> ReadAsync(string projectPath, string rootDirectory);
    }
}