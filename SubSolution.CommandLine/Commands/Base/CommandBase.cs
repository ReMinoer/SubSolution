using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SubSolution.CommandLine.Commands.Base
{
    public abstract class CommandBase : ICommand
    {
        static protected readonly ConsoleLogger Logger;
        private ErrorCode _errorCode;

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