using System.Collections.Generic;
using System.IO;

namespace SubSolution.FileSystems
{
    public interface IFileSystem
    {
        IEqualityComparer<string> PathComparer { get; }
        string GetName(string path);
        string GetFileNameWithoutExtension(string fileName);
        string? GetParentDirectoryPath(string path);
        string Combine(string firstPath, string secondPath);
        string[] SplitPath(string path);
        string MakeRelativePath(string rootPath, string filePath);
        string MakeAbsolutePath(string rootPath, string relativeFilePath);
        Stream OpenStream(string filePath);
        IEnumerable<string> GetFilesMatchingGlobPattern(string directoryPath, string globPattern);
    }
}