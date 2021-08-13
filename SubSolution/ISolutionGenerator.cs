namespace SubSolution
{
    public interface ISolutionGenerator
    {
        void Generate(ISolutionOutput solutionOutput);
    }

    public interface ISolutionGenerator<out T>
    {
        T Generate(ISolutionOutput solutionOutput);
    }
}