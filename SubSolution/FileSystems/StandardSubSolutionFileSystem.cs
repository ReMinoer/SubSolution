using System;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SubSolution.Configuration.FileSystems;
using SubSolution.FileSystems.Base;

namespace SubSolution.FileSystems
{
    public class StandardSubSolutionFileSystem : SubSolutionFileSystemBase
    {
        static private StandardSubSolutionFileSystem? _instance;
        static public StandardSubSolutionFileSystem Instance => _instance ??= new StandardSubSolutionFileSystem();

        static private readonly char[] DirectorySeparators = {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

        private StandardSubSolutionFileSystem()
        {
        }

        public override string GetFileNameWithoutExtension(string fileName) => StandardFileSystem.Instance.GetFileNameWithoutExtension(fileName);
        public override string? GetParentDirectoryPath(string path) => StandardFileSystem.Instance.GetParentDirectoryPath(path);
        public override string Combine(string firstPath, string secondPath) => StandardFileSystem.Instance.Combine(firstPath, secondPath);

        public override string[] SplitPath(string path) => path.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
        public override Stream OpenStream(string filePath) => File.OpenRead(filePath);

        protected override DirectoryInfoBase? GetDirectoryInfo(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return null;

            return new DirectoryInfoWrapper(new DirectoryInfo(directoryPath));
        }
    }
}