using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using SubSolution.FileSystems.Base;
using SubSolution.Utils;

namespace SubSolution.FileSystems.Mock
{
    [ExcludeFromCodeCoverage]
    public class MockFileSystem : FileSystemBase
    {
        // Do not use System.IO !
        protected readonly Regex _rootRegex;

        public const char DirectorySeparator = '/';
        public const char AltDirectorySeparator = '\\';
        public static readonly char[] DirectorySeparators = { AltDirectorySeparator, DirectorySeparator };

        private readonly Dictionary<string, byte[]> _fileContents = new Dictionary<string, byte[]>();

        public MockFileSystem(Regex rootRegex)
        {
            _rootRegex = rootRegex;
        }

        public void AddFileContent(string filePath, byte[] content)
        {
            _fileContents.Add(NormalizePath(filePath), content);
        }

        public override IEqualityComparer<string> PathComparer { get; } = new PathComparer(PathCaseComparison.RespectCase);

        public override bool IsAbsolutePath(string path)
        {
            string[] pathParts = SplitPath(path);
            if (pathParts.Length == 0)
                return false;

            return _rootRegex.IsMatch(pathParts[0]);
        }

        public override Stream OpenStream(string filePath)
        {
            if (_fileContents.TryGetValue(NormalizePath(filePath), out byte[] content))
                return new MemoryStream(content);

            throw new FileNotFoundException($"\"{filePath}\" have no associated content.", filePath);
        }

        public override string GetName(string path)
        {
            string[] pathParts = SplitPath(path);
            if (pathParts.Length == 0)
                return string.Empty;

            return pathParts[^1];
        }

        public override string GetFileNameWithoutExtension(string path)
        {
            string fileName = GetName(path);

            int dotIndex = fileName.LastIndexOf('.');
            if (dotIndex == -1)
                return fileName;

            return fileName[..dotIndex];
        }

        public override string? GetParentDirectoryPath(string path)
        {
            int lastSeparatorIndex = TrimPath(path).LastIndexOfAny(DirectorySeparators);
            if (lastSeparatorIndex == -1)
                return null;

            return path[..lastSeparatorIndex];
        }

        public override string Combine(string? firstPath, string? secondPath)
        {
            char separator = GetLastSeparator(firstPath) ?? GetLastSeparator(secondPath) ?? DirectorySeparator;
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