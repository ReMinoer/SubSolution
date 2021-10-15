using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.Builders;
using SubSolution.CommandLine.Commands.Base;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands
{
    [Verb("validate", HelpText = "Validate .subsln files and their associated .sln files are up-to-date.")]
    public class ValidateCommand : BuildCommandBase
    {
        [Value(0, MetaName = "files", HelpText = "Paths of SubSolution configuration files to validate. " +
            "Also accept relative file path pattern using wildcards \"*\", \"**\" and \"?\". " +
            "If not provided, all .subsln files in working directory and its sub-directories will be validated.")]
        public override IEnumerable<string>? FilePaths { get; set; }

        [Option('s', "show", HelpText = "Show representation of validated solution.")]
        public bool Show { get; set; }

        protected override async Task ExecuteCommandAsync(string configurationFilePath)
        {
            SolutionBuilderContext? context = await GetBuildContextAsync(configurationFilePath);
            if (context is null)
                return;

            Log($"Validating {configurationFilePath}...");

            if (!File.Exists(context.SolutionPath))
            {
                LogError($"Solution file {context.SolutionPath} not generated yet.");
                UpdateErrorCode(ErrorCode.NotValidated);
                return;
            }

            Solution? expectedSolution = await BuildSolutionAsync(context);
            if (expectedSolution is null)
                return;

            if (Show)
                LogSolution(expectedSolution);

            (RawSolution? rawSolution, bool changed) = await UpdateSolutionAsync(expectedSolution, context.SolutionPath);
            if (rawSolution is null)
                return;

            if (changed)
            {
                LogError($"Solution {context.SolutionPath} is not up-to-date.");
                UpdateErrorCode(ErrorCode.NotValidated);
                return;
            }

            Log($"Solution {context.SolutionPath} is up-to-date.");
        }
    }
}