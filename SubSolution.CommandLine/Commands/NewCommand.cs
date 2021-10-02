using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.Builders.Configuration;
using SubSolution.CommandLine.Utils;

namespace SubSolution.CommandLine.Commands
{
    [Verb("new", HelpText = "Create a new .subsln configuration file")]
    public class NewCommand : ICommand
    {
        [Value(0, MetaName = "files", Required = true, HelpText = "Paths of the files to create, relative to working directory. " +
            "If no extension provided, default extension is \".subsln\".")]
        public IEnumerable<string>? FilePaths { get; set; }

        [Option('f', "force", HelpText = "Force to overwrite existing files, without asking user.")]
        public bool Force { get; set; }
        [Option('o', "open", HelpText = "Open .sln file with its default application.")]
        public bool Open { get; set; }

        public Task ExecuteAsync()
        {
            if (FilePaths is null)
                return Task.CompletedTask;

            foreach (string filePath in FilePaths)
            {
                string path = filePath;
                if (Path.GetExtension(filePath) == string.Empty)
                    path += ".subsln";

                Console.WriteLine($"Creating {path}...");

                if (File.Exists(path) && !Force)
                {
                    Console.WriteLine($"File {path} already exist.");
                    if (!CommandLineUtils.AskUserValidation("Do you want to overwrite it ?"))
                    {
                        Console.WriteLine($"Abort creation of {path}.");
                        continue;
                    }
                }

                var subSolutionConfiguration = new SubSolutionConfiguration
                {
                    Root = new SolutionRoot()
                };

                subSolutionConfiguration.Save(path);
                Console.WriteLine($"Created {path}.");

                if (Open)
                {
                    Console.WriteLine($"Opening {path}...");

                    var fileStartInfo = new ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(fileStartInfo);
                }
            }

            return Task.CompletedTask;
        }
    }
}