using System.Threading.Tasks;

namespace SubSolution.Configuration.Builders.Filters
{
    public interface IFilter<in T>
    {
        string TextFormat { get; }
        Task PrepareAsync();
        bool Match(T item);
    }
}