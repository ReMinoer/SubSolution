using System.IO;
using System.Threading.Tasks;
using SubSolution.Configuration;

namespace SubSolution.FileSystems
{
    static public class SubSolutionFileSystemExtension
    {
        static public async Task<SubSolutionConfiguration> LoadConfigurationAsync(this ISubSolutionFileSystem fileSystem, string configurationPath)
        {
            await using Stream stream = fileSystem.OpenStream(configurationPath);
            using TextReader textReader = new StreamReader(stream);

            return await Task.Run(() => SubSolutionConfiguration.Load(textReader));
        }

        static public string MoveRelativePathRoot(this ISubSolutionFileSystem fileSystem, string relativePath, string previousRootPath, string newRootPath)
        {
            string absolutePath = fileSystem.Combine(previousRootPath, relativePath);
            return fileSystem.MakeRelativePath(newRootPath, absolutePath);
        }
    }
}