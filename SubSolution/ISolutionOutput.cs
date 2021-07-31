namespace SubSolution
{
    public interface ISolutionOutput : ISolution
    {
        string OutputPath { get; }
        void SetOutputDirectory(string outputDirectory);
    }
}