using System.Collections.Concurrent;
using System.Threading.Tasks;
using SubSolution.FileSystems;

namespace SubSolution.ProjectReaders
{
    public class CacheProjectReader : IProjectReader
    {
        private readonly IProjectReader _baseProjectReader;
        private readonly ConcurrentDictionary<string, Task<ISolutionProject>> _projectCacheByPath;

        public CacheProjectReader(IFileSystem fileSystem, IProjectReader baseProjectReader)
        {
            _baseProjectReader = baseProjectReader;
            _projectCacheByPath = new ConcurrentDictionary<string, Task<ISolutionProject>>(fileSystem.PathComparer);
        }

        public async Task<ISolutionProject> ReadAsync(string absoluteProjectPath)
        {
            return await _projectCacheByPath.GetOrAdd(absoluteProjectPath, _baseProjectReader.ReadAsync);
        }
    }
}