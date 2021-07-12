namespace SubSolution.Configuration
{
    public interface ISolutionBuilder
    {
        string SolutionOutputPath { get; }
        void AddFile(string filePath, string[] solutionFolderPath);
        void AddProject(string projectPath, string[] solutionFolderPath);
    }
}