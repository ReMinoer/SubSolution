using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SubSolution.FileSystems.Mock;

namespace SubSolution.Builders.GlobPatterns.Mock
{
    public class MockGlobPatternFileSystem : MockFileSystem, IGlobPatternFileSystem
    {
        private readonly Dictionary<string, DirectoryInfoBase> _rootDirectories = new Dictionary<string, DirectoryInfoBase>();
        
        public MockGlobPatternFileSystem(Regex rootRegex)
            : base(rootRegex) { }

        public void AddRoot(string rootName, IEnumerable<string> relativePaths)
        {
            if (!_rootRegex.IsMatch(rootName))
                throw new InvalidOperationException();

            _rootDirectories[rootName] = new MockDirectoryInfo(this, rootName, relativePaths);
        }

        public bool FileExists(string absoluteFilePath)
        {
            string parentDirectoryPath = GetParentDirectoryPath(absoluteFilePath)!;
            DirectoryInfoBase directoryInfo = GetDirectoryInfo(parentDirectoryPath);

            return directoryInfo.EnumerateFileSystemInfos().Any(x => PathComparer.Equals(x.FullName, absoluteFilePath));
        }

        public IEnumerable<string> GetFilesMatchingGlobPattern(string directoryPath, string globPattern)
        {
            DirectoryInfoBase directoryInfo = GetDirectoryInfo(directoryPath);

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(globPattern);

            return matcher.Execute(directoryInfo).Files.Select(x => x.Path);
        }

        private DirectoryInfoBase GetDirectoryInfo(string absoluteDirectoryPath)
        {
            string[] directoryNames = SplitPath(absoluteDirectoryPath);

            string root = directoryNames[0];
            if (!_rootDirectories.TryGetValue(root, out DirectoryInfoBase rootDirectoryInfo))
                rootDirectoryInfo = new MockDirectoryInfo(this, root, Enumerable.Empty<string>());

            return directoryNames[1..].Aggregate(rootDirectoryInfo, (x, directoryName) => x.GetDirectory(directoryName));
        }
    }
}