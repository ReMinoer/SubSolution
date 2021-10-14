namespace SubSolution
{
    public interface IMovableSolution : ISolution
    {
        string OutputDirectoryPath { get; }
        void ChangeOutputDirectory(string outputDirectoryPath);
    }
}