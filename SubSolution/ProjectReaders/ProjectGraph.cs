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
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _directDependencies;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _directDependents;

        public ProjectGraph(IFileSystem fileSystem, IProjectReader projectReader)
        {
            _fileSystem = fileSystem;
            _projectReader = projectReader;

            _getDependencyTasks = new ConcurrentDictionary<string, Task<IReadOnlyCollection<string>>>(fileSystem.PathComparer);
            _dependencies = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(fileSystem.PathComparer);
            _dependents = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(fileSystem.PathComparer);
            _directDependencies = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(fileSystem.PathComparer);
            _directDependents = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>(fileSystem.PathComparer);
        }

        public async Task<IReadOnlyCollection<string>> GetDependencies(string projectPath)
        {
            return await _getDependencyTasks.GetOrAdd(projectPath, LoadAllDependencies);
        }

        private async Task<IReadOnlyCollection<string>> LoadAllDependencies(string projectPath)
        {
            ISolutionProject project = await _projectReader.ReadAsync(projectPath);

            ConcurrentDictionary<string, bool> dependenciesPaths = _dependencies.GetOrAdd(projectPath, _ => new ConcurrentDictionary<string, bool>());
            ConcurrentDictionary<string, bool> directDependenciesPaths = _directDependencies.GetOrAdd(projectPath, _ => new ConcurrentDictionary<string, bool>());

            string projectDirectoryPath = _fileSystem.GetParentDirectoryPath(projectPath)!;

            await Task.WhenAll(project.ProjectDependencies.Select(async relativeDependencyPath =>
            {
                // Add direct dependencies
                string dependencyPath = _fileSystem.MakeAbsolutePath(projectDirectoryPath, relativeDependencyPath);
                directDependenciesPaths.TryAdd(dependencyPath, true);

                // Add indirect dependencies
                IReadOnlyCollection<string> dependencyDependenciesPaths = await GetDependencies(dependencyPath);
                foreach (string dependencyDependencyPath in dependencyDependenciesPaths)
                    dependenciesPaths.TryAdd(dependencyDependencyPath, true);
            }));

            // Add indirect dependents
            foreach (string dependencyPath in dependenciesPaths.Keys)
            {
                ConcurrentDictionary<string, bool> dependencyDependentsPaths = _dependents.GetOrAdd(dependencyPath, _ => new ConcurrentDictionary<string, bool>());
                dependencyDependentsPaths.TryAdd(projectPath, true);
            }

            // Add direct dependents + combine direct & indirect
            foreach (string directDependencyPath in directDependenciesPaths.Keys)
            {
                ConcurrentDictionary<string, bool> dependencyDirectDependentsPaths = _directDependents.GetOrAdd(directDependencyPath, _ => new ConcurrentDictionary<string, bool>());
                dependencyDirectDependentsPaths.TryAdd(projectPath, true);

                ConcurrentDictionary<string, bool> dependencyDependentsPaths = _dependents.GetOrAdd(directDependencyPath, _ => new ConcurrentDictionary<string, bool>());
                dependencyDependentsPaths.TryAdd(projectPath, true);

                dependenciesPaths.TryAdd(directDependencyPath, true);
            }

            return new ReadOnlyCollection<string>(dependenciesPaths.Keys);
        }

        public async Task<IReadOnlyCollection<string>> GetDependents(string absoluteTargetPath, bool directOnly = false)
        {
            await Task.WhenAll(_getDependencyTasks.Values);

            ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> dependentsDictionary = directOnly ? _directDependents : _dependents;

            if (dependentsDictionary.TryGetValue(absoluteTargetPath, out ConcurrentDictionary<string, bool> dependents))
                return new ReadOnlyCollection<string>(dependents.Keys);

            return Array.Empty<string>();
        }
    }
}