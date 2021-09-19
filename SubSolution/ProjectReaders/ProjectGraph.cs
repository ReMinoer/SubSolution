using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution.ProjectReaders
{
    public class ProjectGraph : IProjectGraph
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProjectReader _projectReader;

        private readonly ConcurrentDictionary<string, Task<IReadOnlyCollection<string>>> _getDependencyTasks;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _dependencies;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _dependents;

        public ProjectGraph(IFileSystem fileSystem, IProjectReader projectReader)
        {
            _fileSystem = fileSystem;
            _projectReader = projectReader;

            _getDependencyTasks = new ConcurrentDictionary<string, Task<IReadOnlyCollection<string>>>(fileSystem.PathComparer);
            _dependencies = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(fileSystem.PathComparer);
            _dependents = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(fileSystem.PathComparer);
        }

        public async Task<IReadOnlyCollection<string>> GetDependencies(string projectPath)
        {
            return await _getDependencyTasks.GetOrAdd(projectPath, LoadAllDependencies);
        }

        private async Task<IReadOnlyCollection<string>> LoadAllDependencies(string projectPath)
        {
            ISolutionProject project = await _projectReader.ReadAsync(projectPath);

            ConcurrentDictionary<string, bool> projectDependenciesPaths = _dependencies.GetOrAdd(projectPath, _ => new ConcurrentDictionary<string, bool>());
            string projectDirectoryPath = _fileSystem.GetParentDirectoryPath(projectPath)!;

            await Task.WhenAll(project.ProjectDependencies.Select(async relativeDependencyPath =>
            {
                // Add dependency
                string dependencyPath = _fileSystem.MakeAbsolutePath(projectDirectoryPath, relativeDependencyPath);
                projectDependenciesPaths.TryAdd(dependencyPath, true);

                // Add dependency dependencies
                IReadOnlyCollection<string> dependencyDependenciesPaths = await GetDependencies(dependencyPath);
                foreach (string dependencyDependencyPath in dependencyDependenciesPaths)
                    projectDependenciesPaths.TryAdd(dependencyDependencyPath, true);
            }));

            // Add project as dependents of its dependencies
            foreach (string projectDependencyPath in projectDependenciesPaths.Keys)
            {
                ConcurrentDictionary<string, bool> dependencyDependentsPaths = _dependents.GetOrAdd(projectDependencyPath, _ => new ConcurrentDictionary<string, bool>());
                dependencyDependentsPaths.TryAdd(projectPath, true);
            }

            return new ReadOnlyCollection<string>(projectDependenciesPaths.Keys);
        }

        public async Task<IReadOnlyCollection<string>> GetDependents(string absoluteTargetPath)
        {
            await Task.WhenAll(_getDependencyTasks.Values);

            if (_dependents.TryGetValue(absoluteTargetPath, out ConcurrentDictionary<string, bool> dependents))
                return new ReadOnlyCollection<string>(dependents.Keys);

            return Array.Empty<string>();
        }
    }
}