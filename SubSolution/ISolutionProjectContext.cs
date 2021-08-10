namespace SubSolution
{
    public interface ISolutionProjectContext
    {
        ISolutionProject Project { get; }
        string Configuration { get; }
        string Platform { get; }
        bool Build { get; }
        bool Deploy { get; }
    }
}