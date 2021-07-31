using System.IO;
using SubSolution.Configuration;

namespace SubSolution.FileSystems
{
    static public class SubSolutionFileSystemExtension
    {
        static public SubSolutionConfiguration LoadConfiguration(this ISubSolutionFileSystem fileSystem, string configurationPath)
        {
            using Stream stream = fileSystem.OpenStream(configurationPath);
            using TextReader textReader = new StreamReader(stream);

            return SubSolutionConfiguration.Load(textReader);
        }

        static public string MoveRelativePathRoot(this ISubSolutionFileSystem fileSystem, string relativePath, string previousRootPath, string newRootPath)
        {
            string absolutePath = fileSystem.Combine(previousRootPath, relativePath);
            return fileSystem.MakeRelativePath(newRootPath, absolutePath);
        }
    }
}