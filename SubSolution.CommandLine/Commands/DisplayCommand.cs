using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.Builders;
using SubSolution.CommandLine.Commands.Base;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands
{
    [Verb("display", HelpText = "Display the content of .sln or .subsln files.")]
    public class DisplayCommand : CommandBase
    {
        [Value(0, MetaName = "files", Required = true, HelpText = "Paths of the solution files to display. Can be .sln or .subsln files.")]
        public IEnumerable<string>? FilePaths { get; set; }

        protected override async Task ExecuteCommandAsync()
        {
            if (FilePaths is null)
                return;

            foreach (string filePath in FilePaths)
            {
                if (!CheckFileExist(filePath))
                    continue;

                Log($"Displaying {filePath}...");

                ISolution? solution = await ConvertAnySolution(filePath);
                if (solution is null)
                    continue;

                LogSolution(solution);
            }
        }

        private async Task<ISolution?> ConvertAnySolution(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            switch (fileExtension)
            {
                case ".sln":
                {
                    RawSolution? rawSolution = await ReadSolution(filePath);
                    if (rawSolution is null)
                        return null;

                    return await ConvertRawSolution(rawSolution, filePath);
                }
                case ".subsln":
                {
                    SolutionBuilderContext? context = await GetBuildContext(filePath);
                    if (context is null)
                        return null;

                    return await BuildSolution(context);
                }
                default:
                {
                    LogError($"Unknown file extension \"{fileExtension}\" for file {filePath}.");
                    UpdateErrorCode(ErrorCode.FailReadSolution);
                    return null;
                }
            }
        }
    }
}