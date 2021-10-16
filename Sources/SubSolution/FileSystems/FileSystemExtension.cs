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
    }
}