using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SubSolution.Builders;
using SubSolution.FileSystems;
using SubSolution.Generators;
using SubSolution.Raw;

[assembly: ExcludeFromCodeCoverage]

namespace SubSolution.Console
{
    static class Program
    {
        static private async Task Main(string[] args)
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

            var logGenerator = new LogGenerator(logger, LogLevel.Information);
            logGenerator.Generate(solution);

            //var generator = new DotNetCommandLineGenerator();
            //generator.Generate(solution);

            var rawGenerator = new RawSolutionGenerator(StandardFileSystem.Instance);
            RawSolution rawSolution = rawGenerator.Generate(solution);

            await using (FileStream createStream = File.Create(solution.OutputPath))
                await rawSolution.WriteAsync(createStream);

            //await using (FileStream readStream = File.OpenRead(solution.OutputPath))
            //    await RawSolution.ReadAsync(readStream);
        }
    }
}
