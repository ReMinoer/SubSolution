using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
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

            var loggerProvider = new NLogLoggerProvider();
            ILogger? logger = loggerProvider.CreateLogger(nameof(SubSolution));

            SubSolutionContext context = SubSolutionContext.FromConfigurationFile(args[0]);
            context.Logger = logger;
            context.LogLevel = LogLevel.Debug;

            ISolutionBuilder solutionBuilder = new SolutionBuilder(context);
            ISolutionOutput solution = solutionBuilder.Build(context.Configuration);

            ISolutionGenerator logGenerator = new LogGenerator(logger, LogLevel.Information);
            logGenerator.Generate(solution);

            ISolutionGenerator generator = new DotNetCommandLineGenerator();
            generator.Generate(solution);
        }
    }
}
