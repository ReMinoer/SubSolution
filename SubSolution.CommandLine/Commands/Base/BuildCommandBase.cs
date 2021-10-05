using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubSolution.CommandLine.Commands.Base
{
    public abstract class BuildCommandBase : CommandBase
    {
        public abstract IEnumerable<string>? FilePaths { get; set; }

        protected abstract Task ExecuteCommandAsync(string configurationFilePath);

        protected override async Task ExecuteCommandAsync()
        {
            bool anyFile = false;
            foreach (string pathPattern in GetPathPatterns())
            {
                bool anyMatchingFile = false;
                IEnumerable<string> configurationFilePaths = GetMatchingFilePaths(pathPattern);

                foreach (string configurationFilePath in configurationFilePaths)
                {
                    if (anyFile)
                        LogEmptyLine();

                    anyFile = true;
                    anyMatchingFile = true;

                    await ExecuteCommandAsync(configurationFilePath);
                }

                if (!anyMatchingFile)
                {
                    LogError($"No files matching {pathPattern}.");
                    UpdateErrorCode(ErrorCode.FileNotFound);
                }
            }
        }

        private IEnumerable<string> GetPathPatterns()
        {
            if (FilePaths is null || !FilePaths.Any())
                return new[] { "**/*.subsln" };

            return FilePaths;
        }
    }
}