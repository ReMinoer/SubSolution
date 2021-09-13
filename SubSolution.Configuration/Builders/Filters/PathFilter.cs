using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.FileSystems;

namespace SubSolution.Configuration.Builders.Filters
{
    public class PathFilter : IFilter<string>
    {
        public IFileSystem FileSystem { get; }
        public string WorkingDirectory { get; }
        public string GlobPattern { get; }
        public string TextFormat => $"Path=\"{GlobPattern}\"";

        private IEnumerable<string> _matchingPaths = Enumerable.Empty<string>();

        public PathFilter(IFileSystem fileSystem, string workingDirectory, string globPattern)
        {
            FileSystem = fileSystem;
            WorkingDirectory = workingDirectory;
            GlobPattern = globPattern;
        }

        public Task PrepareAsync()
        {
            _matchingPaths = FileSystem.GetFilesMatchingGlobPattern(WorkingDirectory, GlobPattern);
            return Task.CompletedTask;
        }

        public bool Match(string path) => _matchingPaths.Contains(path, FileSystem.PathComparer);
    }
}