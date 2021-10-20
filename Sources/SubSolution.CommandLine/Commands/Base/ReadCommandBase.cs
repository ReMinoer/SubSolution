using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using SubSolution.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.MsBuild;
using SubSolution.ProjectReaders;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands.Base
{
    public abstract class ReadCommandBase : CommandBase
    {
        static private readonly MsBuildProjectReader ProjectReader;
        static private readonly CacheProjectReader CacheProjectReader;

        static ReadCommandBase()
        {
            ProjectReader = new MsBuildProjectReader(Logger);
            CacheProjectReader = new CacheProjectReader(StandardFileSystem.Instance, ProjectReader);
        }

        [Option('d', "detailed", HelpText = "Show more details in solution output.")]
        public bool ShowDetailedSolution { get; set; }
        [Option('D', "divergent", HelpText = "Show divergent projects contexts in solution output (divergent configuration-platforms, disabled build, disabled deploy).")]
        public bool ShowDivergentProjects { get; set; }
        [Option('p', "paths", HelpText = "Show project paths in solution output.")]
        public bool ShowProjectPaths { get; set; }

        [Option('v', "verbose", HelpText = "Enable verbose log (solution build, project reading, ...)")]
        public bool Verbose { get; set; }

        protected override Task ExecuteCommandAsync()
        {
            ProjectReader.LogLevel = Verbose ? LogLevel.Information : LogLevel.None;

            return Task.CompletedTask;
        }

        protected void LogSolution(ISolution solution)
        {
            var solutionLogger = new SolutionLogger
            {
                ShowProjectTypes = ShowDetailedSolution,
                ShowAllProjectContexts = ShowDetailedSolution && !ShowDivergentProjects,
                ShowDivergentProjectContexts = ShowDivergentProjects,
                ShowFilePaths = ShowProjectPaths
            };
            string logMessage = solutionLogger.Convert(solution);

            LogEmptyLine();
            Log(logMessage);
        }

        protected async Task<SolutionBuilderContext?> GetBuildContextAsync(string configurationFilePath)
        {
            configurationFilePath = StandardFileSystem.Instance.MakeAbsolutePath(Environment.CurrentDirectory, configurationFilePath);

            if (!CheckFileExist(configurationFilePath))
                return null;

            SolutionBuilderContext context = await SolutionBuilderContext.FromConfigurationFileAsync(configurationFilePath, CacheProjectReader);
            context.Logger = Logger;
            context.LogLevel = Verbose ? LogLevel.Information : LogLevel.None;

            return context;
        }

        protected async Task<Solution?> BuildSolutionAsync(SolutionBuilderContext context)
        {
            try
            {
                SolutionBuilder solutionBuilder = new SolutionBuilder(context);
                Solution solution = await solutionBuilder.BuildAsync(context.Configuration);

                foreach (Issue issue in solutionBuilder.Issues)
                    LogIssue(issue);

                if (solutionBuilder.Issues.Any(x => x.Level == IssueLevel.Error))
                {
                    LogError($"Failed to build {context.ConfigurationFilePath}.");
                    UpdateErrorCode(ErrorCode.FailBuildSolution);
                    return null;
                }

                return solution;
            }
            catch (Exception exception)
            {
                LogError($"Failed to build {context.ConfigurationFilePath}.", exception);
                UpdateErrorCode(ErrorCode.FailBuildSolution);
                return null;
            }
        }

        protected async Task<RawSolution?> ReadSolutionAsync(string filePath)
        {
            try
            {
                await using FileStream fileStream = File.OpenRead(filePath);
                return await RawSolution.ReadAsync(fileStream);
            }
            catch (Exception exception)
            {
                LogError($"Failed to read {filePath}.", exception);
                UpdateErrorCode(ErrorCode.FailReadSolution);
                return null;
            }
        }

        protected async Task<ISolution?> ConvertRawSolutionAsync(RawSolution rawSolution, string filePath, bool skipProjectLoading)
        {
            try
            {
                string solutionDirectoryPath = StandardFileSystem.Instance.GetParentDirectoryPath(filePath)!;

                RawSolutionConverter converter = new RawSolutionConverter(StandardFileSystem.Instance, CacheProjectReader);
                ISolution solution = await converter.ConvertAsync(rawSolution, solutionDirectoryPath, skipProjectLoading);

                foreach (Issue issue in converter.Issues)
                    LogIssue(issue);

                if (converter.Issues.Any(x => x.Level == IssueLevel.Error))
                {
                    LogError($"Failed to interpret {filePath}.");
                    UpdateErrorCode(ErrorCode.FailInterpretSolution);
                    return null;
                }

                return solution;
            }
            catch (Exception exception)
            {
                LogError($"Failed to interpret {filePath}.", exception);
                UpdateErrorCode(ErrorCode.FailInterpretSolution);
                return null;
            }
        }
    }
}