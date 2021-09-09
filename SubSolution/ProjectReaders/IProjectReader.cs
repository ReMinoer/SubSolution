using System.Threading.Tasks;

namespace SubSolution.ProjectReaders
{
    public interface IProjectReader
    {
        Task<ISolutionProject> ReadAsync(string absoluteProjectPath);
    }
}