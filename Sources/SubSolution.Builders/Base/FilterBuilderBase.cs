﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SubSolution.Builders.Configuration;
using SubSolution.Builders.Filters;
using SubSolution.Builders.GlobPatterns;

namespace SubSolution.Builders.Base
{
    public abstract class FilterBuilderBase<TItem, TVisitable, TVisitor>
        where TVisitable : IAsyncVisitable<TVisitor>
    {
        private readonly IGlobPatternFileSystem _fileSystem;
        public IFilter<TItem> BuiltFilter { get; private set; } = new AllFilter<TItem>();

        protected FilterBuilderBase(IGlobPatternFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        protected abstract Task AcceptAsync(TVisitable visitable);
        protected abstract string GetItemPath(TItem item);

        protected async Task BuildNotAsync(TVisitable visitable)
        {
            await AcceptAsync(visitable);
            BuiltFilter = new NotFilter<TItem>(BuiltFilter);
        }

        protected async Task BuildAllAsync(IEnumerable<TVisitable> visitables)
        {
            var filter = new AllFilter<TItem>();
            foreach (TVisitable visitable in visitables)
            {
                await AcceptAsync(visitable);
                filter.Filters.Add(BuiltFilter);
            }
            BuiltFilter = filter;
        }

        protected async Task BuildAnyOfAsync(IEnumerable<TVisitable> visitables)
        {
            var filter = new AnyOfFilter<TItem>();
            foreach (TVisitable visitable in visitables)
            {
                await AcceptAsync(visitable);
                filter.Filters.Add(BuiltFilter);
            }
            BuiltFilter = filter;
        }

        protected Task BuildPathAsync(string globPattern, params string[] defaultFileExtensions)
        {
            if (defaultFileExtensions.Length == 1)
            {
                globPattern = GlobPatternUtils.CompleteSimplifiedPattern(globPattern, defaultFileExtensions[0]);
                BuiltFilter = new PathFilter(globPattern, _fileSystem.IsCaseSensitive).Cast<TItem, string>(GetItemPath);
            }
            else
            {
                var filter = new AnyOfFilter<TItem>();
                foreach (string defaultFileExtension in defaultFileExtensions)
                {
                    globPattern = GlobPatternUtils.CompleteSimplifiedPattern(globPattern, defaultFileExtension);
                    filter.Filters.Add(new PathFilter(globPattern, _fileSystem.IsCaseSensitive).Cast<TItem, string>(GetItemPath));
                }

                BuiltFilter = filter;
            }

            return Task.CompletedTask;
        }
    }
}