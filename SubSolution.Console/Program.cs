using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SubSolution.Configuration.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems;
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

            SolutionBuilderContext context = await SolutionBuilderContext.FromConfigurationFileAsync(args[0], new MsBuildProjectReader());
            context.Logger = logger;
            context.LogLevel = LogLevel.Debug;

            SolutionBuilder solutionBuilder = new SolutionBuilder(context);
            Solution solution = await solutionBuilder.BuildAsync(context.Configuration);

            var solutionLogger = new SolutionLogger
            {
                ShowProjectContexts = true
            };
            
            string logMessage = solutionLogger.Convert(solution);
            logger.LogInformation(logMessage);

            //var generator = new DotNetCommandLineGenerator();
            //generator.Generate(solution);

            var solutionConverter = new SolutionConverter(StandardFileSystem.Instance);
            RawSolution rawSolution = solutionConverter.Convert(solution);

            await using (FileStream createStream = File.Create(solution.OutputPath))
                await rawSolution.WriteAsync(createStream);

            //await using (FileStream readStream = File.OpenRead(solution.OutputPath))
            //    await RawSolution.ReadAsync(readStream);
        }
    }
}
