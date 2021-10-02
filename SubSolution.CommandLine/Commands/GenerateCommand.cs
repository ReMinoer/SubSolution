using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SubSolution.Builders;
using SubSolution.CommandLine.Utils;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.MsBuild;
using SubSolution.Raw;
using SubSolution.Utils;

namespace SubSolution.CommandLine.Commands
{
    [Verb("generate", HelpText = "Generate .sln Visual Studio solution files from .subsln configuration files.")]
    public class GenerateCommand : ICommand
    {
        [Value(0, MetaName = "files", HelpText = "File paths of SubSolution configuration files to generate. " +
            "Also accept relative file path pattern using wildcards \"*\", \"**\" and \"?\". " +
            "If not provided, all .subsln files in working directory will be generated.")]
        public IEnumerable<string>? FilePaths { get; set; }

        [Option('p', "preview", HelpText = "Preview the command result without creating files.")]
        public bool Preview { get; set; }
        [Option('f', "force", HelpText = "Force to overwrite existing files, without asking user.")]
        public bool Force { get; set; }
        [Option('s', "show", HelpText = "Show representation of generation result.")]
        public bool Show { get; set; }
        [Option('o', "open", HelpText = "Open generated file with its default application.")]
        public bool Open { get; set; }
        [Option('d', "sub-directories", HelpText = "Include sub-directories .subsln files if no file pattern was provided.")]
        public bool SubDirectories { get; set; }

        public async Task ExecuteAsync()
        {
            if (FilePaths is null || !FilePaths.Any())
                FilePaths = new []{ SubDirectories ? "**/*.subsln" : "*.subsln" };

            bool anyFile = false;
            var generatedSolutionPaths = new List<string>();

            foreach (string pathPattern in FilePaths)
            {
                IEnumerable<string> configurationFilePaths;

                if (!string.IsNullOrEmpty(Path.GetPathRoot(pathPattern)))
                {
                    configurationFilePaths = new[] { pathPattern };
                }
                else
                {
                    string simplifiedPathPattern = GlobPatternUtils.CompleteSimplifiedPattern(pathPattern, "subsln");

                    configurationFilePaths = StandardFileSystem.Instance.GetFilesMatchingGlobPattern(
                        Environment.CurrentDirectory, simplifiedPathPattern);
                }

                foreach (string configurationFilePath in configurationFilePaths)
                {
                    if (anyFile)
                        Console.WriteLine();

                    anyFile = true;

                    if (!File.Exists(configurationFilePath))
                    {
                        Console.WriteLine($"File {configurationFilePath} not found.");
                        continue;
                    }

                    Console.WriteLine($"Generating {configurationFilePath}...");

                    var loggerProvider = new NLogLoggerProvider();
                    ILogger? logger = loggerProvider.CreateLogger(nameof(SubSolution));

                    SolutionBuilderContext context = await SolutionBuilderContext.FromConfigurationFileAsync(configurationFilePath, new MsBuildProjectReader());
                    context.Logger = logger;
                    context.LogLevel = LogLevel.Trace;

                    SolutionBuilder solutionBuilder = new SolutionBuilder(context);
                    Solution solution = await solutionBuilder.BuildAsync(context.Configuration);

                    if (Show)
                    {
                        var solutionLogger = new SolutionLogger();
                        string logMessage = solutionLogger.Convert(solution);

                        Console.WriteLine();
                        Console.WriteLine(logMessage);
                    }

                    var solutionConverter = new SolutionConverter(StandardFileSystem.Instance);

                    RawSolution rawSolution;
                    if (File.Exists(solution.OutputPath))
                    {
                        await using FileStream existingSolutionStream = File.OpenRead(solution.OutputPath);
                        rawSolution = await RawSolution.ReadAsync(existingSolutionStream);

                        solutionConverter.Update(rawSolution, solution);

                        if (solutionConverter.Changes.Count == 0)
                        {
                            Console.WriteLine($"No changes in {solution.OutputPath}.");
                            continue;
                        }

                        foreach (SolutionChange change in solutionConverter.Changes.OrderBy(x => x))
                        {
                            Console.WriteLine(change.GetMessage(StandardFileSystem.Instance));
                        }
                    }
                    else
                    {
                        rawSolution = solutionConverter.Convert(solution);
                    }

                    if (Preview)
                        continue;

                    if (!Force)
                    {
                        if (solutionConverter.Changes.Count > 0)
                        {
                            if (!CommandLineUtils.AskUserValidation("Apply the changes ?"))
                            {
                                Console.WriteLine($"Abort generation of {configurationFilePath}.");
                                continue;
                            }
                        }
                        else if (File.Exists(solution.OutputPath))
                        {
                            Console.WriteLine($"File {solution.OutputPath} already exist.");
                            if (!CommandLineUtils.AskUserValidation("Do you want to overwrite it ?"))
                            {
                                Console.WriteLine($"Abort generation of {configurationFilePath}.");
                                continue;
                            }
                        }
                    }
                    
                    await using FileStream fileStream = File.Create(solution.OutputPath);
                    await rawSolution.WriteAsync(fileStream);

                    generatedSolutionPaths.Add(solution.OutputPath);
                    Console.WriteLine($"Generated {configurationFilePath}.");
                }
            }

            if (!anyFile)
            {
                Console.WriteLine("No matching files found.");
                return;
            }

            if (Open)
            {
                foreach (string generatedSolutionPath in generatedSolutionPaths)
                {
                    Console.WriteLine($"Opening {generatedSolutionPath}...");

                    var fileStartInfo = new ProcessStartInfo(generatedSolutionPath)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(fileStartInfo);
                }
            }
        }
    }
}