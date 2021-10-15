using System;
using System.Threading.Tasks;

namespace SubSolution.Builders.Filters
{
    public class CastFilter<TItem, TFilteredItem> : IFilter<TItem>
    {
        public IFilter<TFilteredItem> BaseFilter { get; }
        public Func<TItem, TFilteredItem> ItemSelector { get; }
        public string TextFormat => BaseFilter.TextFormat;

        public CastFilter(IFilter<TFilteredItem> baseFilter, Func<TItem, TFilteredItem> itemSelector)
        {
            BaseFilter = baseFilter;
            ItemSelector = itemSelector;
        }

        public Task PrepareAsync() => BaseFilter.PrepareAsync();
        public bool Match(TItem item) => BaseFilter.Match(ItemSelector(item));
    }
}