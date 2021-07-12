using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace SubSolution.FileSystems
{
    [ExcludeFromCodeCoverage]
    internal class MockDirectoryInfo : DirectoryInfoBase
    {
        private const char DirectorySeparator = MockSubSolutionFileSystem.DirectorySeparator;

        private readonly MockDirectoryInfo? _parentDirectoryInfo;
        private readonly Dictionary<string, MockDirectoryInfo> _subDirectories;
        private readonly Dictionary<string, MockFileInfo> _files;

        public MockSubSolutionFileSystem FileSystem { get; }

        public override sealed string FullName { get; }
        public override sealed string Name { get; }
        public override sealed DirectoryInfoBase? ParentDirectory => _parentDirectoryInfo;

        public MockDirectoryInfo(MockSubSolutionFileSystem fileSystem, string rootName, IEnumerable<string> relativePaths)
        {
            if (string.IsNullOrEmpty(rootName))
                throw new ArgumentException(nameof(rootName));

            FileSystem = fileSystem;

            FullName = rootName;
            Name = rootName;

            _parentDirectoryInfo = null;
            _subDirectories = new Dictionary<string, MockDirectoryInfo>();
            _files = new Dictionary<string, MockFileInfo>();

            IEnumerable<string> normalizedPaths = relativePaths.Select(x => FileSystem.NormalizePath(FileSystem.Combine(FullName, x)));
            BuildChildren(normalizedPaths);
        }

        private MockDirectoryInfo(MockDirectoryInfo parentDirectoryInfo, string name, IEnumerable<string> filePaths)
        {
            FileSystem = parentDirectoryInfo.FileSystem;

            Name = name;
            FullName = FileSystem.Combine(parentDirectoryInfo.FullName, name);

            _parentDirectoryInfo = parentDirectoryInfo;

            _subDirectories = new Dictionary<string, MockDirectoryInfo>();
            _files = new Dictionary<string, MockFileInfo>();
            
            BuildChildren(filePaths);
        }

        private MockDirectoryInfo(MockSubSolutionFileSystem fileSystem, MockDirectoryInfo? parentDirectoryInfo, string name,
            Dictionary<string, MockDirectoryInfo>? subDirectories = null, Dictionary<string, MockFileInfo>? files = null)
        {
            FileSystem = fileSystem;

            Name = name;
            FullName = FileSystem.Combine(parentDirectoryInfo?.FullName, name);

            _parentDirectoryInfo = parentDirectoryInfo;
            _subDirectories = subDirectories ?? new Dictionary<string, MockDirectoryInfo>();
            _files = files ?? new Dictionary<string, MockFileInfo>();
        }
        
        private void BuildChildren(IEnumerable<string> normalizedPaths)
        {
            string currentRoot = FullName + DirectorySeparator;
            Dictionary<string, List<string>> subDirectoriesFilePaths = new Dictionary<string, List<string>>();

            foreach (string normalizedPath in normalizedPaths)
            {
                int nextSeparatorIndex = normalizedPath.IndexOf(DirectorySeparator, currentRoot.Length);
                if (nextSeparatorIndex == -1)
                {
                    string fileName = normalizedPath[currentRoot.Length..];

                    if (fileName.Length > 0)
                        _files.Add(fileName, new MockFileInfo(this, fileName));
                }
                else
                {
                    string subFolderName = normalizedPath[currentRoot.Length..nextSeparatorIndex];

                    if (!subDirectoriesFilePaths.TryGetValue(subFolderName, out List<string> subDirectoryFilePaths))
                        subDirectoriesFilePaths[subFolderName] = subDirectoryFilePaths = new List<string>();

                    subDirectoryFilePaths.Add(normalizedPath);
                }
            }

            foreach ((string directoryName, List<string> filePaths) in subDirectoriesFilePaths)
            {
                _subDirectories.Add(directoryName, new MockDirectoryInfo(this, directoryName, filePaths));
            }
        }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            foreach (MockFileInfo fileInfo in _files.Values)
                yield return fileInfo;
            foreach (MockDirectoryInfo directoryInfo in _subDirectories.Values)
                yield return directoryInfo;
        }

        public override DirectoryInfoBase GetDirectory(string name)
        {
            if (name == "..")
            {
                if (_parentDirectoryInfo == null)
                    throw new InvalidOperationException("Cannot find a parent where \"..\" is located in path.");

                return new MockDirectoryInfo(FileSystem, _parentDirectoryInfo._parentDirectoryInfo, "..", _parentDirectoryInfo._subDirectories, _parentDirectoryInfo._files);
            }

            if (_subDirectories.TryGetValue(name, out MockDirectoryInfo directoryInfo))
                return directoryInfo;

            return new MockDirectoryInfo(FileSystem, this, name);
        }
        
        public override FileInfoBase GetFile(string name)
        {
            if (_files.TryGetValue(name, out MockFileInfo fileInfo))
                return fileInfo;

            return new MockFileInfo(this, name);
        }
    }
}