using System.Threading.Tasks;

namespace SubSolution.CommandLine
{
    public interface ICommand
    {
        Task<ErrorCode> ExecuteAsync();
    }
}