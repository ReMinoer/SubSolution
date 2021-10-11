using System.Collections.Generic;
using SubSolution.FileSystems;

namespace SubSolution.Builders.GlobPatterns
{
    public interface IGlobPatternFileSystem : IFileSystem
    {
        IEnumerable<string> GetFilesMatchingGlobPattern(string directoryPath, string globPattern);
    }
}