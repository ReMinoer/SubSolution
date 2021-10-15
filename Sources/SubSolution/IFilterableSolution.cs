namespace SubSolution
{
    public interface IFilterableSolution : ISolution
    {
        new IFilterableSolutionFolder Root { get; }
    }
}