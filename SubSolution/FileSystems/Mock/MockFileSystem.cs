using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SubSolution.FileSystems.Base;
using SubSolution.Utils;

namespace SubSolution.FileSystems.Mock
{
    [ExcludeFromCodeCoverage]
    public class MockFileSystem : SubSolutionFileSystemBase
    {
        // Do not use System.IO !

        public const char DirectorySeparator = '/';
        public const char AltDirectorySeparator = '\\';
        public static readonly char[] DirectorySeparators = { AltDirectorySeparator, DirectorySeparator };

        private readonly Dictionary<string, DirectoryInfoBase> _rootDirectories = new Dictionary<string, DirectoryInfoBase>();
        private readonly Dictionary<string, byte[]> _fileContents = new Dictionary<string, byte[]>();

        public void AddRoot(string rootName, IEnumerable<string> relativePaths)
        {
            _rootDirectories[rootName] = new MockDirectoryInfo(this, rootName, relativePaths);
        }

        public void AddFileContent(string filePath, byte[] content)
        {
            _fileContents.Add(NormalizePath(filePath), content);
        }

        public override IEqualityComparer<string> PathComparer { get; } = new PathComparer(PathCaseComparison.RespectCase);

        public override Stream OpenStream(string filePath)
        {
            if (_fileContents.TryGetValue(NormalizePath(filePath), out byte[] content))
                return new MemoryStream(content);

            throw new FileNotFoundException($"\"{filePath}\" have no associated content.", filePath);
        }

        public override string GetName(string path)
        {
            var pathParts = SplitPath(path);
            if (pathParts.Length == 0)
                return string.Empty;

            return pathParts[^1];
        }

        public override string GetFileNameWithoutExtension(string path)
        {
            var fileName = GetName(path);

            var dotIndex = fileName.LastIndexOf('.');
            if (dotIndex == -1)
                return fileName;

            return fileName[..dotIndex];
        }

        public override string? GetParentDirectoryPath(string path)
        {
            var lastSeparatorIndex = TrimPath(path).LastIndexOfAny(DirectorySeparators);
            if (lastSeparatorIndex == -1)
                return null;

            return path[..lastSeparatorIndex];
        }

        public override string Combine(string? firstPath, string? secondPath)
        {
            var separator = GetLastSeparator(firstPath) ?? GetLastSeparator(secondPath) ?? DirectorySeparator;
            firstPath = TrimPath(firstPath);
            secondPath = TrimPath(secondPath);

            if (string.IsNullOrEmpty(firstPath) && string.IsNullOrEmpty(secondPath))
                return string.Empty;
            if (string.IsNullOrEmpty(firstPath))
                return secondPath;
            if (string.IsNullOrEmpty(secondPath))
                return firstPath;
            
            return ReplaceSeparators(firstPath, separator) + separator + ReplaceSeparators(secondPath, separator);
        }

        public string NormalizePath(string path)
        {
            return TrimPath(path).Replace(AltDirectorySeparator, DirectorySeparator);
        }

        public override string[] SplitPath(string path)
        {
            return TrimPath(path).Split(DirectorySeparators);
        }

        private string TrimPath(string? path)
        {
            return path?.TrimEnd(DirectorySeparators) ?? string.Empty;
        }

        protected override DirectoryInfoBase? GetDirectoryInfo(string directoryPath)
        {
            string[] directoryNames = SplitPath(directoryPath);

            string root = directoryNames[0];
            if (!_rootDirectories.TryGetValue(root, out DirectoryInfoBase rootDirectoryInfo))
                rootDirectoryInfo = new MockDirectoryInfo(this, root, Enumerable.Empty<string>());

            return directoryNames[1..].Aggregate(rootDirectoryInfo, (x, directoryName) => x.GetDirectory(directoryName));
        }

        private char? GetLastSeparator(string? path)
        {
            var lastAbsoluteSeparatorIndex = path?.LastIndexOf(AltDirectorySeparator) ?? -1;
            var lastRelativeSeparatorIndex = path?.LastIndexOf(DirectorySeparator) ?? -1;

            if (lastAbsoluteSeparatorIndex == -1 && lastRelativeSeparatorIndex == -1)
                return null;

            if (lastAbsoluteSeparatorIndex > lastRelativeSeparatorIndex)
                return AltDirectorySeparator;
            return DirectorySeparator;
        }

        private string ReplaceSeparators(string filePath, char separator)
        {
            return filePath.Replace(DirectorySeparator, separator).Replace(AltDirectorySeparator, separator);
        }
    }
}