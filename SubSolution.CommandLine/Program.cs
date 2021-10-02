using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.CommandLine.Commands;

[assembly: ExcludeFromCodeCoverage]

namespace SubSolution.CommandLine
{
    static class Program
    {
        static private async Task Main(string[] args)
        {
            await Parser.Default
                .ParseArguments<NewCommand, GenerateCommand, ShowCommand>(args)
                .WithParsedAsync<ICommand>(x => x.ExecuteAsync());
        }
    }
}
