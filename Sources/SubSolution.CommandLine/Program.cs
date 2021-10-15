using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.CommandLine.Commands;
using SubSolution.CommandLine.Commands.Base;

[assembly: ExcludeFromCodeCoverage]

namespace SubSolution.CommandLine
{
    static class Program
    {
        static private async Task<int> Main(string[] args)
        {
            return (int)await Parser.Default
                .ParseArguments<CreateCommand, GenerateCommand, ValidateCommand, ShowCommand>(args)
                .MapResult<ICommand, Task<ErrorCode>>(async x =>
                {
                    try
                    {
                        return await x.ExecuteAsync();
                    }
                    catch (Exception exception)
                    {
                        CommandBase.LogError("An execution error occurred.", exception);
                        return ErrorCode.FatalException;
                    }
                }, _ => Task.FromResult(ErrorCode.FailParseCommandLine));
        }
    }
}
