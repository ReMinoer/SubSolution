using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SubSolution.FileSystems.Base;

namespace SubSolution.FileSystems
{
    [ExcludeFromCodeCoverage]
    public class StandardFileSystem : FileSystemBase
    {
        static private StandardFileSystem? _instance;
        static public StandardFileSystem Instance => _instance ??= new StandardFileSystem();

        static private readonly char[] DirectorySeparators = {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};
        public override IEqualityComparer<string> PathComparer => FileSystems.PathComparer.Default;
        public override bool IsCaseSensitive => FileSystems.PathComparer.IsEnvironmentCaseSensitive();

        private StandardFileSystem()
        {
        }

        public override string GetName(string path) => Path.GetFileName(path);
        public override string GetFileNameWithoutExtension(string fileName) => Path.GetFileNameWithoutExtension(fileName);
        public override string? GetExtension(string fileName)
        {
            string extension = Path.GetExtension(fileName).TrimStart('.');
            return !string.IsNullOrEmpty(extension) ? extension : null;
        }

        public override string? GetParentDirectoryPath(string path) => Path.GetDirectoryName(path);
        public override string Combine(string firstPath, string secondPath)
        {
            if (GetParentDirectoryPath(firstPath) is null)
                firstPath += Path.DirectorySeparatorChar;

            return Path.Combine(firstPath, secondPath);
        }

        public override string[] SplitPath(string path) => path.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
        public override bool IsAbsolutePath(string path) => Path.IsPathRooted(path);
        public override Stream OpenStream(string filePath) => File.OpenRead(filePath);
    }
}