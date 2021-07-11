namespace SubSolution.Configuration.FileSystems
{
    public interface IConfigurationFileSystem
    {
        string GetFileNameWithoutExtension(string fileName);
        string? GetParentDirectoryPath(string path);
        string Combine(string firstPath, string secondPath);
    }
}