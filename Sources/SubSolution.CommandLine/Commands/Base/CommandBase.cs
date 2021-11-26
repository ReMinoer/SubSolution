using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using SubSolution.ProjectReaders;

namespace SubSolution.CommandLine.Commands.Base
{
    public abstract class CommandBase : ICommand
    {
        static protected readonly ConsoleLogger Logger;
        private ErrorCode _errorCode;

        [Option("debug", HelpText = "Enable debug mode to show exceptions and callstacks.")]
        public bool DebugMode { get; set; }

        static CommandBase()
        {
            Logger = new ConsoleLogger(LogLevel.Information);
        }

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

        static protected void Log(string message) => Console.WriteLine(message);
        static protected void LogEmptyLine() => Console.WriteLine();

        protected void LogError(string errorMessage, Exception? exception = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            Console.Error.Write("ERROR: ");
            Console.Error.WriteLine(errorMessage);

            if (exception != null)
            {
                Console.Error.Write("---> ");
                if (DebugMode)
                    Console.Error.WriteLine(exception);
                else
                    Console.Error.WriteLine(exception.Message);
            }

            Console.ResetColor();
        }

        static protected void LogWarning(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            Console.Error.Write("WARNING: ");
            Console.Error.WriteLine(errorMessage);

            Console.ResetColor();
        }

        protected void LogIssue(Issue issue)
        {
            switch (issue.Level)
            {
                case IssueLevel.Error:
                    var projectReadException = issue.Exception as ProjectReadException;
                    LogError(issue.Message, projectReadException?.InnerException ?? issue.Exception);
                    break;
                case IssueLevel.Warning:
                    LogWarning(issue.Message);
                    break;
                default:
                    Log(issue.Message);
                    break;
            }
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

        static protected void OpenFile(string filePath)
        {
            var fileStartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true, 
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
    }
}