using System.Diagnostics.CodeAnalysis;
using SubSolution.Builders;
using SubSolution.Generators;

[assembly: ExcludeFromCodeCoverage]

namespace SubSolution.Console
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                System.Console.WriteLine("No SubSolution configuration path provided!");

            SolutionBuilder solution = SubSolutionEngine.ProcessConfigurationFile(args[0]);

            var generator = new DotNetCommandLineGenerator();
            generator.Generate(solution, solution.OutputPath);
        }
    }
}
