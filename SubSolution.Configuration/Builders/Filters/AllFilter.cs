using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubSolution.Configuration.Builders.Filters
{
    public class AllFilter<T> : IFilter<T>
    {
        public List<IFilter<T>> Filters { get; } = new List<IFilter<T>>();
        public string TextFormat => $"({string.Join(" and ", Filters.Select(x => x.TextFormat))})";

        public Task PrepareAsync() => Task.WhenAll(Filters.Select(x => x.PrepareAsync()));
        public bool Match(T item) => Filters.All(x => x.Match(item));
    }
}