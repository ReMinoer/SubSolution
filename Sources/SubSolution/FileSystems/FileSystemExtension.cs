using System;

namespace SubSolution.FileSystems
{
    static public class FileSystemExtension
    {
        static public string MoveRelativePathRoot(this IFileSystem fileSystem, string relativePath, string previousRootPath, string newRootPath)
        {
            string absolutePath = fileSystem.MakeAbsolutePath(previousRootPath, relativePath);
            return fileSystem.MakeRelativePathIfPossible(newRootPath, absolutePath);
        }

        static public string MakeRelativePathIfPossible(this IFileSystem fileSystem, string rootPath, string absolutePath)
        {
            if (fileSystem.SplitPath(rootPath)[0] != fileSystem.SplitPath(absolutePath)[0])
                return absolutePath;

            return fileSystem.MakeRelativePath(rootPath, absolutePath);
        }

        static public string ChangeFileExtension(this IFileSystem fileSystem, string filePath, string newExtension)
        {
            string newFileName = fileSystem.GetFileNameWithoutExtension(filePath) + "." + newExtension;
            string? parentDirectoryPath = fileSystem.GetParentDirectoryPath(filePath);
            if (parentDirectoryPath is null)
                return newFileName;

            return fileSystem.Combine(parentDirectoryPath, newFileName);
        }

        static public ProjectFileExtension GetProjectExtension(this IFileSystem fileSystem, string projectPath)
        {
            string? extension = fileSystem.GetExtension(projectPath);
            if (extension is null)
                throw new ArgumentException($"Path {projectPath} has no extension.");

            if (!ProjectFileExtensions.ByExtensions.TryGetValue(extension, out ProjectFileExtension projectExtension))
                throw new NotSupportedException($"Project extension \"{extension}\" is unknown.");

            return projectExtension;
        }
    }
}