using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace SubSolution.Generators
{
    [ExcludeFromCodeCoverage]
    public class DotNetCommandLineGenerator
    {
        public void Generate(ISolution solution, string outputPath)
        {
            string workingDirectory = Path.GetDirectoryName(outputPath)!;
            string solutionName = Path.GetFileNameWithoutExtension(outputPath);
            string solutionFileName = Path.GetFileName(outputPath);

            CreateSolution(workingDirectory, solutionName);
            AddSolutionRoot(solution, workingDirectory, solutionFileName);
        }

        private void CreateSolution(string workingDirectory, string solutionName)
        {
            RunDotNetCommand(workingDirectory, Separate("new", "sln", "--force", "-n", Quote(solutionName)));
        }

        private void AddSolutionRoot(ISolution solution, string workingDirectory, string solutionFileName) => AddSolutionFolder(workingDirectory, solutionFileName, solution.Root, new Stack<string>());
        private void AddSolutionFolder(string workingDirectory, string solutionFileName, ISolutionFolder solutionFolder, Stack<string> solutionFolderNames)
        {
            AddProjects(workingDirectory, solutionFileName, solutionFolder.ProjectPaths, solutionFolderNames);

            foreach (string subFolderName in solutionFolder.SubFolders.Keys)
            {
                ISolutionFolder subFolder = solutionFolder.SubFolders[subFolderName];

                solutionFolderNames.Push(subFolderName);
                AddSolutionFolder(workingDirectory, solutionFileName, subFolder, solutionFolderNames);
                solutionFolderNames.Pop();
            }
        }

        private void AddProjects(string workingDirectory, string solutionFileName, IReadOnlyCollection<string> projectPaths, IReadOnlyCollection<string> solutionFolderNames)
        {
            if (projectPaths.Count == 0)
                return;

            if (solutionFolderNames.Count > 0)
            {
                string solutionFolder = string.Join('\\', solutionFolderNames.Reverse());
                RunDotNetCommand(workingDirectory, Separate("sln", Quote(solutionFileName), "add", "-s", Quote(solutionFolder), Separate(projectPaths.Select(Quote))));
            }
            else
                RunDotNetCommand(workingDirectory, Separate("sln", Quote(solutionFileName), "add", "--in-root", Separate(projectPaths.Select(Quote))));
        }

        private void RunDotNetCommand(string workingDirectory, string arguments)
        {
            string formattedArguments = Separate(arguments);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = formattedArguments,
                WorkingDirectory = workingDirectory
            };

            Process? process = Process.Start(processStartInfo);
            if (process is null)
                throw new InvalidOperationException($"Failed to run dotnet command: dotnet {formattedArguments}");

            process.WaitForExit();
        }

        private string Quote(string argument) => $"\"{argument}\"";
        private string Separate(IEnumerable<string> arguments) => string.Join(' ', arguments);
        private string Separate(params string[] arguments) => Separate(arguments.AsEnumerable());
    }
}