using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace SubSolution.FileSystems.Base
{
    public abstract class FileSystemBase : IFileSystem
    {
        public abstract IEqualityComparer<string> PathComparer { get; }
        public abstract string GetName(string path);
        public abstract string GetFileNameWithoutExtension(string fileName);
        public abstract string? GetParentDirectoryPath(string path);
        public abstract string Combine(string firstPath, string secondPath);
        public abstract string[] SplitPath(string path);
        public abstract bool IsAbsolutePath(string path);
        public abstract Stream OpenStream(string filePath);

        public string MakeRelativePath(string rootPath, string filePath)
        {
            string[] rootPathSplit = SplitPath(rootPath);
            string[] filePathSplit = SplitPath(filePath);

            int commonPartsCount = rootPathSplit.Zip(filePathSplit, (x, y) => x == y).TakeWhile(x => x).Count();
            int backMoveCount = rootPathSplit.Length - commonPartsCount;

            return Enumerable.Repeat("..", backMoveCount).Concat(filePathSplit[commonPartsCount..]).Aggregate(Combine);
        }

        public string MakeAbsolutePath(string rootPath, string relativeFilePath)
        {
            if (IsAbsolutePath(relativeFilePath))
                return relativeFilePath;

            string[] rootPathSplit = SplitPath(rootPath);
            string[] relativeFilePathSplit = SplitPath(relativeFilePath);

            int backtrackCount = relativeFilePathSplit.TakeWhile(x => x == "..").Count();

            return rootPathSplit[..^backtrackCount].Concat(relativeFilePathSplit[backtrackCount..]).Aggregate(Combine);
        }

        public IEnumerable<string> GetFilesMatchingGlobPattern(string directoryPath, string globPattern)
        {
            var directoryInfo = GetDirectoryInfo(directoryPath);

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(globPattern);

            return matcher.Execute(directoryInfo).Files.Select(x => x.Path);
        }

        protected abstract DirectoryInfoBase? GetDirectoryInfo(string root);
    }
}