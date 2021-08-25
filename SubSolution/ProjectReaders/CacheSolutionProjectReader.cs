using System.Collections.Generic;
using System.Threading.Tasks;
using SubSolution.FileSystems;

namespace SubSolution.ProjectReaders
{
    public class CacheSolutionProjectReader : ISolutionProjectReader
    {
        private readonly ISolutionProjectReader _baseProjectReader;
        private readonly Dictionary<string, ISolutionProject> _projectCacheByPath;

        public CacheSolutionProjectReader(ISubSolutionFileSystem fileSystem, ISolutionProjectReader baseProjectReader)
        {
            _baseProjectReader = baseProjectReader;
            _projectCacheByPath = new Dictionary<string, ISolutionProject>(fileSystem.PathComparer);
        }

        public async Task<ISolutionProject> ReadAsync(string absoluteProjectPath)
        {
            if (_projectCacheByPath.TryGetValue(absoluteProjectPath, out ISolutionProject cacheProject))
                return new SolutionProject(cacheProject);

            ISolutionProject newProject = await _baseProjectReader.ReadAsync(absoluteProjectPath);
            _projectCacheByPath.Add(absoluteProjectPath, newProject);

            return newProject;
        }
    }
}