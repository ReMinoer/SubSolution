using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubSolution.ProjectReaders
{
    public interface IProjectGraph
    {
        Task<IReadOnlyCollection<string>> GetDependencies(string projectPath);
        Task<IReadOnlyCollection<string>> GetDependents(string absoluteProjectPath);
    }
}