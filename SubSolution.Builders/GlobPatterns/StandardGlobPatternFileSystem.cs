using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SubSolution.FileSystems;

namespace SubSolution.Builders.GlobPatterns
{
    public class StandardGlobPatternFileSystem : IGlobPatternFileSystem
    {
        static private StandardGlobPatternFileSystem? _instance;
        static public StandardGlobPatternFileSystem Instance => _instance ??= new StandardGlobPatternFileSystem();

        public IEnumerable<string> GetFilesMatchingGlobPattern(string directoryPath, string globPattern)
        {
            if (!Directory.Exists(directoryPath))
                return Enumerable.Empty<string>();

            var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(directoryPath));

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(globPattern);

            return matcher.Execute(directoryInfo).Files.Select(x => x.Path);
        }

        private StandardFileSystem FileSystem => StandardFileSystem.Instance;

        public IEqualityComparer<string> PathComparer => FileSystem.PathComparer;
        public string GetName(string path) => FileSystem.GetName(path);
        public string GetFileNameWithoutExtension(string fileName) => FileSystem.GetFileNameWithoutExtension(fileName);
        public string? GetParentDirectoryPath(string path) => FileSystem.GetParentDirectoryPath(path);
        public string Combine(string firstPath, string secondPath) => FileSystem.Combine(firstPath, secondPath);
        public string[] SplitPath(string path) => FileSystem.SplitPath(path);
        public bool IsAbsolutePath(string path) => FileSystem.IsAbsolutePath(path);
        public string MakeRelativePath(string rootPath, string filePath) => FileSystem.MakeRelativePath(rootPath, filePath);
        public string MakeAbsolutePath(string rootPath, string relativeFilePath) => FileSystem.MakeAbsolutePath(rootPath, relativeFilePath);
        public Stream OpenStream(string filePath) => FileSystem.OpenStream(filePath);
    }
}