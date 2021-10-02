using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.Configuration.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.MsBuild;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands
{
    [Verb("show", HelpText = "Show a representation of .sln or .subsln files content.")]
    public class ShowCommand : ICommand
    {
        [Value(0, MetaName = "files", Required = true, HelpText = "Paths of the solution files to show, relative to working directory. Can be .sln or .subsln files.")]
        public IEnumerable<string>? FilePaths { get; set; }

        public async Task ExecuteAsync()
        {
            if (FilePaths is null)
                return;

            foreach (string relativeFilePath in FilePaths)
            {
                if (!File.Exists(relativeFilePath))
                {
                    Console.WriteLine($"File {relativeFilePath} not found.");
                    continue;
                }

                ISolution solution;
                string fileExtension = Path.GetExtension(relativeFilePath);
                switch (fileExtension)
                {
                    case ".sln":
                    {
                        await using FileStream fileStream = File.OpenRead(relativeFilePath);
                        RawSolution rawSolution = await RawSolution.ReadAsync(fileStream);

                        RawSolutionConverter converter = new RawSolutionConverter(StandardFileSystem.Instance, new MsBuildProjectReader());
                        (ISolution convertedSolution, _) = await converter.ConvertAsync(rawSolution, string.Empty);
                        solution = convertedSolution;

                        break;
                    }
                    case ".subsln":
                    {
                        SolutionBuilderContext context = await SolutionBuilderContext.FromConfigurationFileAsync(relativeFilePath, new MsBuildProjectReader());
                        SolutionBuilder solutionBuilder = new SolutionBuilder(context);
                        solution = await solutionBuilder.BuildAsync(context.Configuration);

                        break;
                    }
                    default:
                        Console.WriteLine($"Unknown file extension \"{fileExtension}\"");
                        return;
                }
                
                var solutionLogger = new SolutionLogger();
                string logMessage = solutionLogger.Convert(solution);

                Console.WriteLine();
                Console.WriteLine(logMessage);
            }
        }
    }
}