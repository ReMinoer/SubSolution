using System.Collections.Generic;
using System.Threading.Tasks;
using SubSolution.FileSystems;

namespace SubSolution.ProjectReaders
{
    public class CacheSolutionProjectReader : ISolutionProjectReader
    {
        private readonly ISubSolutionFileSystem _fileSystem;
        private readonly ISolutionProjectReader _baseProjectReader;
        private readonly Dictionary<string, ISolutionProject> _projectCacheByPath;

        public CacheSolutionProjectReader(ISubSolutionFileSystem fileSystem, ISolutionProjectReader baseProjectReader)
        {
            _fileSystem = fileSystem;
            _baseProjectReader = baseProjectReader;

            _projectCacheByPath = new Dictionary<string, ISolutionProject>(_fileSystem.PathComparer);
        }

        public async Task<ISolutionProject> ReadAsync(string projectPath, string rootDirectory)
        {
            string absoluteProjectPath = _fileSystem.Combine(rootDirectory, projectPath);
            
            if (_projectCacheByPath.TryGetValue(absoluteProjectPath, out ISolutionProject cacheProject))
            {
                var projectCopy = new SolutionProject(projectPath);
                projectCopy.Configurations.AddRange(cacheProject.Configurations);
                projectCopy.Platforms.AddRange(cacheProject.Platforms);

                return projectCopy;
            }

            ISolutionProject newProject = await _baseProjectReader.ReadAsync(projectPath, rootDirectory);
            _projectCacheByPath.Add(absoluteProjectPath, newProject);

            return newProject;
        }
    }
}