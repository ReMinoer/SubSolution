using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.Builders.GlobPatterns;
using SubSolution.Converters;
using SubSolution.Converters.Changes;
using SubSolution.FileSystems;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands.Base
{
    public abstract class BuildCommandBase : ReadCommandBase
    {
        public abstract IEnumerable<string>? FilePaths { get; set; }

        protected abstract Task ExecuteCommandAsync(string configurationFilePath);

        protected override async Task ExecuteCommandAsync()
        {
            await base.ExecuteCommandAsync();

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

        static protected IEnumerable<string> GetMatchingFilePaths(string pathPattern)
        {
            var fileSystem = StandardGlobPatternFileSystem.Instance;

            if (fileSystem.IsAbsolutePath(pathPattern))
                return fileSystem.FileExists(pathPattern) ? new[] { pathPattern } : Enumerable.Empty<string>();

            string simplifiedPathPattern = GlobPatternUtils.CompleteSimplifiedPattern(pathPattern, "subsln");
            return fileSystem.GetFilesMatchingGlobPattern(Environment.CurrentDirectory, simplifiedPathPattern);
        }

        protected async Task<(RawSolution? rawSolution, bool changed)> UpdateSolutionAsync(ISolution solution, string existingSolutionPath)
        {
            RawSolution? rawSolution = await ReadSolutionAsync(existingSolutionPath);
            if (rawSolution is null)
                return (null, false);

            bool changed;
            try
            {
                var solutionConverter = new SolutionConverter(StandardFileSystem.Instance);
                solutionConverter.Update(rawSolution, solution);

                changed = solutionConverter.Changes.Count > 0;

                foreach (SolutionChange change in solutionConverter.Changes.OrderBy(x => x))
                {
                    Log(change.GetMessage(StandardFileSystem.Instance));
                }
            }
            catch (Exception exception)
            {
                LogError($"Failed to update {existingSolutionPath}.", exception);
                UpdateErrorCode(ErrorCode.FailUpdateSolution);
                return (null, false);
            }

            return (rawSolution, changed);
        }

        protected RawSolution? ConvertSolution(ISolution solution, string configurationFilePath)
        {
            try
            {
                var solutionConverter = new SolutionConverter(StandardFileSystem.Instance);
                return solutionConverter.Convert(solution);
            }
            catch (Exception exception)
            {
                LogError($"Failed to interpret {configurationFilePath}.", exception);
                UpdateErrorCode(ErrorCode.FailInterpretSolution);
                return null;
            }
        }
    }
}