using System.Diagnostics.CodeAnalysis;
using SubSolution.Builders;
using SubSolution.Generators;

[assembly: ExcludeFromCodeCoverage]

namespace SubSolution.Console
{
    static class Program
    {
        static private void Main(string[] args)
        {
            if (args.Length == 0)
                System.Console.WriteLine("No SubSolution configuration path provided!");

            SubSolutionContext context = SubSolutionContext.FromConfigurationFile(args[0]);

            ISolutionBuilder solutionBuilder = new SolutionBuilder(context);
            ISolutionOutput solution = solutionBuilder.Build(context.Configuration);

            ISolutionGenerator generator = new DotNetCommandLineGenerator();
            generator.Generate(solution);
        }
    }
}
