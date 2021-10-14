using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubSolution.ProjectReaders
{
    public interface IProjectGraph
    {
        Task<IReadOnlyCollection<string>> GetDependenciesAsync(string projectPath);
        Task<IReadOnlyCollection<string>> GetDependentsAsync(string absoluteProjectPath, bool directOnly = false);
    }
}