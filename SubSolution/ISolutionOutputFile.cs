using SubSolution.Configuration.FileSystems;

namespace SubSolution
{
    public interface ISolutionOutputFile : ISolution
    {
        IConfigurationFileSystem FileSystem { get; }
        string OutputPath { get; }
    }
}