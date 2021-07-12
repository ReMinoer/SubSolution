using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SubSolution.Configuration.FileSystems
{
    [ExcludeFromCodeCoverage]
    public class StandardFileSystem : IConfigurationFileSystem
    {
        static private StandardFileSystem? _instance;
        static public StandardFileSystem Instance => _instance ??= new StandardFileSystem();

        private StandardFileSystem()
        {
        }

        public string GetFileNameWithoutExtension(string fileName) => Path.GetFileNameWithoutExtension(fileName);
        public string? GetParentDirectoryPath(string path) => Path.GetDirectoryName(path);
        public string Combine(string firstPath, string secondPath) => Path.Combine(firstPath, secondPath);
    }
}