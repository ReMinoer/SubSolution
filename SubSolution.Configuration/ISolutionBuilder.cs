namespace SubSolution.Configuration
{
    public interface ISolutionBuilder
    {
        void AddFile(string filePath, string[] solutionFolderPath);
        void AddProject(string projectPath, string[] solutionFolderPath);
    }
}