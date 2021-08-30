using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SubSolution.Builders;
using SubSolution.FileSystems;
using SubSolution.Generators;
using SubSolution.MsBuild;
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

            SubSolutionContext context = await SubSolutionContext.FromConfigurationFileAsync(args[0], new SolutionProjectReader());
            context.Logger = logger;
            context.LogLevel = LogLevel.Debug;

            ISolutionBuilder solutionBuilder = new SolutionBuilder(context);
            ISolution solution = await solutionBuilder.BuildAsync(context.Configuration);

            var logGenerator = new LogGenerator(logger, LogLevel.Information)
            {
                ShowProjectContexts = true
            };
            logGenerator.Generate(solution);

            //var generator = new DotNetCommandLineGenerator();
            //generator.Generate(solution);

            var rawGenerator = new SolutionToRawSolutionGenerator(StandardFileSystem.Instance);
            RawSolution rawSolution = rawGenerator.Generate(solution);

            await using (FileStream createStream = File.Create(solution.OutputPath))
                await rawSolution.WriteAsync(createStream);

            //await using (FileStream readStream = File.OpenRead(solution.OutputPath))
            //    await RawSolution.ReadAsync(readStream);
        }
    }
}
