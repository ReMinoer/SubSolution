using System.Threading.Tasks;

namespace SubSolution.Builders.Filters
{
    public class NotFilter<T> : IFilter<T>
    {
        public IFilter<T> Filter { get; }
        public string TextFormat => "not " + Filter.TextFormat;

        public NotFilter(IFilter<T> filter)
        {
            Filter = filter;
        }

        public Task PrepareAsync() => Filter.PrepareAsync();
        public bool Match(T item) => !Filter.Match(item);
    }
}