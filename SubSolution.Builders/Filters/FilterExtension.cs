using System;

namespace SubSolution.Builders.Filters
{
    static class FilterExtension
    {
        static public CastFilter<TItem, TFilteredItem> Cast<TItem, TFilteredItem>(this IFilter<TFilteredItem> baseFilter, Func<TItem, TFilteredItem> itemSelector)
        {
            return new CastFilter<TItem, TFilteredItem>(baseFilter, itemSelector);
        }
    }
}