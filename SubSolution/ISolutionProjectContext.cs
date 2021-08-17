namespace SubSolution
{
    public interface ISolutionProjectContext
    {
        string ProjectPath { get; }
        string Configuration { get; }
        string Platform { get; }
        bool Build { get; }
        bool Deploy { get; }
    }
}