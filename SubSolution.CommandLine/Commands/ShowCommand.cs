using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.Builders;
using SubSolution.CommandLine.Commands.Base;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands
{
    [Verb("show", HelpText = "Show the content of .sln or .subsln files.")]
    public class ShowCommand : ReadCommandBase
    {
        [Value(0, MetaName = "files", Required = true, HelpText = "Paths of the solution files to show. Can be .sln or .subsln files.")]
        public IEnumerable<string>? FilePaths { get; set; }

        protected override async Task ExecuteCommandAsync()
        {
            await base.ExecuteCommandAsync();

            if (FilePaths is null)
                return;

            foreach (string filePath in FilePaths)
            {
                if (!CheckFileExist(filePath))
                    continue;

                Log($"Show {filePath}...");

                ISolution? solution = await ConvertAnySolutionAsync(filePath);
                if (solution is null)
                    continue;

                LogSolution(solution);
            }
        }

        private async Task<ISolution?> ConvertAnySolutionAsync(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            switch (fileExtension)
            {
                case ".sln":
                {
                    RawSolution? rawSolution = await ReadSolutionAsync(filePath);
                    if (rawSolution is null)
                        return null;

                    bool skipProjectLoading = !ShowDetailedSolution && !ShowDivergentProjects;

                    return await ConvertRawSolutionAsync(rawSolution, filePath, skipProjectLoading);
                }
                case ".subsln":
                {
                    SolutionBuilderContext? context = await GetBuildContextAsync(filePath);
                    if (context is null)
                        return null;

                    return await BuildSolutionAsync(context);
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