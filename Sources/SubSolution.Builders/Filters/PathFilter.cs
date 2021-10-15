using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SubSolution.Builders.GlobPatterns;

namespace SubSolution.Builders.Filters
{
    public class PathFilter : IFilter<string>
    {
        public string GlobPattern { get; }
        private readonly Regex _regex;

        public string TextFormat => $"Path=\"{GlobPattern}\"";

        public PathFilter(string globPattern, bool caseSensitive)
        {
            GlobPattern = globPattern;
            _regex = GlobPatternUtils.ConvertToRegex(globPattern, caseSensitive);
        }

        public Task PrepareAsync() => Task.CompletedTask;
        public bool Match(string path) => _regex.IsMatch(path);
    }
}