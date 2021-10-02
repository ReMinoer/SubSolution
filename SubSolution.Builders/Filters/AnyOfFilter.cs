using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubSolution.Builders.Filters
{
    public class AnyOfFilter<T> : IFilter<T>
    {
        public List<IFilter<T>> Filters { get; } = new List<IFilter<T>>();
        public string TextFormat => $"({string.Join(" or ", Filters.Select(x => x.TextFormat))})";

        public Task PrepareAsync() => Task.WhenAll(Filters.Select(x => x.PrepareAsync()));
        public bool Match(T item) => Filters.Any(x => x.Match(item));
    }
}