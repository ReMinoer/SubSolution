using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SubSolution.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.MsBuild;
using SubSolution.Raw;
using SubSolution.Utils;

namespace SubSolution.CommandLine.Commands.Base
{
    public abstract class CommandBase : ICommand
    {
        private ErrorCode _errorCode;

        public async Task<ErrorCode> ExecuteAsync()
        {
            _errorCode = ErrorCode.Success;
            await ExecuteCommandAsync();
            return _errorCode;
        }

        protected abstract Task ExecuteCommandAsync();

        protected void UpdateErrorCode(ErrorCode newErrorCode)
        {
            if (_errorCode >= ErrorCode.FatalException)
                return;

            _errorCode = newErrorCode;
        }

        static public void Log(string message) => Console.WriteLine(message);
        static public void LogEmptyLine() => Console.WriteLine();
        static public void LogError(string errorMessage, Exception? exception = null)
        {
            Console.Write("ERROR: ");
            Console.WriteLine(errorMessage);

            if (exception != null)
                Console.WriteLine(exception);
        }

        static protected bool AskUserValidation(string question) => AskUserValidation(null, question);
        static protected bool AskUserValidation(string? message, string question)
        {
            LogEmptyLine();
            if (message != null)
                Log(message);

            Console.Write(question + " (y/n): ");

            char answer;
            do
            {
                answer = char.ToLower(Convert.ToChar(Console.Read()));
            }
            while (answer != 'y' && answer != 'n');

            LogEmptyLine();
            return answer == 'y';
        }

        static protected IEnumerable<string> GetMatchingFilePaths(string pathPattern)
        {
            if (StandardFileSystem.Instance.IsAbsolutePath(pathPattern))
                return new[] { pathPattern };

            string simplifiedPathPattern = GlobPatternUtils.CompleteSimplifiedPattern(pathPattern, "subsln");
            return StandardFileSystem.Instance.GetFilesMatchingGlobPattern(Environment.CurrentDirectory, simplifiedPathPattern);
        }

        static protected void OpenFile(string filePath)
        {
            var fileStartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            };
            Process.Start(fileStartInfo);
        }

        protected bool CheckFileExist(string filePath)
        {
            if (File.Exists(filePath))
                return true;

            LogError($"File {filePath} not found.");
            UpdateErrorCode(ErrorCode.FileNotFound);
            return false;
        }

        static protected void LogSolution(ISolution solution)
        {
            var solutionLogger = new SolutionLogger();
            string logMessage = solutionLogger.Convert(solution);

            Console.WriteLine();
            Console.WriteLine(logMessage);
        }

        protected async Task<SolutionBuilderContext?> GetBuildContext(string configurationFilePath)
        {
            if (!CheckFileExist(configurationFilePath))
                return null;

            var loggerProvider = new NLogLoggerProvider();
            ILogger? logger = loggerProvider.CreateLogger(nameof(SubSolution));

            SolutionBuilderContext context = await SolutionBuilderContext.FromConfigurationFileAsync(configurationFilePath, new MsBuildProjectReader());
            context.Logger = logger;
            context.LogLevel = LogLevel.Trace;

            return context;
        }

        protected async Task<Solution?> BuildSolution(SolutionBuilderContext context)
        {
            try
            {
                SolutionBuilder solutionBuilder = new SolutionBuilder(context);
                Solution solution = await solutionBuilder.BuildAsync(context.Configuration);

                foreach (Issue issue in solutionBuilder.Issues)
                    Log(issue.Message);

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

        protected RawSolution? ConvertSolution(ISolution solution)
        {
            try
            {
                var solutionConverter = new SolutionConverter(StandardFileSystem.Instance);
                return solutionConverter.Convert(solution);
            }
            catch (Exception exception)
            {
                LogError($"Failed to interpret {solution.OutputPath}.", exception);
                UpdateErrorCode(ErrorCode.FailInterpretSolution);
                return null;
            }
        }

        protected async Task<(RawSolution? rawSolution, bool changed)> UpdateSolution(ISolution solution)
        {
            RawSolution? rawSolution = await ReadSolution(solution.OutputPath);
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
                    Console.WriteLine(change.GetMessage(StandardFileSystem.Instance));
                }
            }
            catch (Exception exception)
            {
                LogError($"Failed to update {solution.OutputPath}.", exception);
                UpdateErrorCode(ErrorCode.FailUpdateSolution);
                return (null, false);
            }

            return (rawSolution, changed);
        }

        protected async Task<bool> WriteSolution(RawSolution rawSolution, string outputPath)
        {
            try
            {
                await using FileStream fileStream = File.Create(outputPath);
                await rawSolution.WriteAsync(fileStream);
                return true;
            }
            catch (Exception exception)
            {
                LogError($"Failed to write {outputPath}.", exception);
                UpdateErrorCode(ErrorCode.FailWriteSolution);
                return false;
            }
        }

        protected async Task<RawSolution?> ReadSolution(string filePath)
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

        protected async Task<ISolution?> ConvertRawSolution(RawSolution rawSolution, string filePath)
        {
            try
            {
                RawSolutionConverter converter = new RawSolutionConverter(StandardFileSystem.Instance, new MsBuildProjectReader());
                (ISolution solution, List<Issue> issues) = await converter.ConvertAsync(rawSolution, filePath);

                foreach (Issue issue in issues)
                    Log(issue.Message);

                if (issues.Any(x => x.Level == IssueLevel.Error))
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