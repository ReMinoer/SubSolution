using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.FileSystems;

namespace SubSolution.Configuration.Builders.Filters
{
    public class PathFilter : IFilter<string>
    {
        public string GlobPattern { get; }
        public IFileSystem FileSystem { get; }
        public string WorkspaceDirectoryPath { get; }

        public string TextFormat => $"Path=\"{GlobPattern}\"";

        private IEnumerable<string> _matchingPaths = Enumerable.Empty<string>();

        public PathFilter(string globPattern, IFileSystem fileSystem, string workspaceDirectoryPath)
        {
            GlobPattern = globPattern;
            FileSystem = fileSystem;
            WorkspaceDirectoryPath = workspaceDirectoryPath;
        }

        public Task PrepareAsync()
        {
            _matchingPaths = FileSystem.GetFilesMatchingGlobPattern(WorkspaceDirectoryPath, GlobPattern);
            return Task.CompletedTask;
        }

        public bool Match(string path) => _matchingPaths.Contains(path, FileSystem.PathComparer);
    }
}