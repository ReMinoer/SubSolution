using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

namespace SubSolution.Console
{
    static class Program
    {
        static void Main(string[] args)
        {
            SubSolutionEngine.ProcessConfigurationFile(args[0]);
        }
    }
}
