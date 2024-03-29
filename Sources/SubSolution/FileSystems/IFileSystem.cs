﻿using System.Collections.Generic;
using System.IO;

namespace SubSolution.FileSystems
{
    public interface IFileSystem
    {
        IEqualityComparer<string> PathComparer { get; }
        bool IsCaseSensitive { get; }
        string GetName(string path);
        string GetFileNameWithoutExtension(string fileName);
        string? GetExtension(string fileName);
        string? GetParentDirectoryPath(string path);
        string Combine(string firstPath, string secondPath);
        string[] SplitPath(string path);
        bool IsAbsolutePath(string path);
        string MakeRelativePath(string rootPath, string filePath);
        string MakeAbsolutePath(string rootPath, string relativeFilePath);
        Stream OpenStream(string filePath);
    }
}