﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using SubSolution.FileSystems.Base;

namespace SubSolution.FileSystems
{
    [ExcludeFromCodeCoverage]
    public class StandardFileSystem : SubSolutionFileSystemBase
    {
        static private StandardFileSystem? _instance;
        static public StandardFileSystem Instance => _instance ??= new StandardFileSystem();

        static private readonly char[] DirectorySeparators = {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

        private StandardFileSystem()
        {
        }

        public override string GetFileNameWithoutExtension(string fileName) => Path.GetFileNameWithoutExtension(fileName);
        public override string? GetParentDirectoryPath(string path) => Path.GetDirectoryName(path);
        public override string Combine(string firstPath, string secondPath) => Path.Combine(firstPath, secondPath);

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