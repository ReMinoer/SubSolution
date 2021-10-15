namespace SubSolution.FileSystems
{
    static public class FileSystemExtension
    {
        static public string MoveRelativePathRoot(this IFileSystem fileSystem, string relativePath, string previousRootPath, string newRootPath)
        {
            string absolutePath = fileSystem.MakeAbsolutePath(previousRootPath, relativePath);
            return fileSystem.MakeRelativePath(newRootPath, absolutePath);
        }
    }
}