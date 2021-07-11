using System.Collections.Generic;
using System.IO;

namespace SubSolution.Configuration.FileSystems
{
    public interface ISubSolutionFileSystem : IConfigurationFileSystem
    {
        string[] SplitPath(string path);
        string MakeRelativePath(string rootPath, string filePath);
        Stream OpenStream(string filePath);
        IEnumerable<string> GetFilesMatchingGlobPattern(string directoryPath, string globPattern);
    }
}