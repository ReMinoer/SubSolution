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
        private readonly string _workspaceDirectoryPath;
        public IFilter<TItem> BuiltFilter { get; private set; } = new AllFilter<TItem>();

        protected FilterBuilderBase(IGlobPatternFileSystem fileSystem, string workspaceDirectoryPath)
        {
            _fileSystem = fileSystem;
            _workspaceDirectoryPath = workspaceDirectoryPath;
        }

        protected abstract Task AcceptAsync(TVisitable visitable);
        protected abstract string GetItemPath(TItem item);

        protected async Task BuildNot(TVisitable visitable)
        {
            await AcceptAsync(visitable);
            BuiltFilter = new NotFilter<TItem>(BuiltFilter);
        }

        protected async Task BuildAll(IEnumerable<TVisitable> visitables)
        {
            var filter = new AllFilter<TItem>();
            foreach (TVisitable visitable in visitables)
            {
                await AcceptAsync(visitable);
                filter.Filters.Add(BuiltFilter);
            }
            BuiltFilter = filter;
        }

        protected async Task BuildAnyOf(IEnumerable<TVisitable> visitables)
        {
            var filter = new AnyOfFilter<TItem>();
            foreach (TVisitable visitable in visitables)
            {
                await AcceptAsync(visitable);
                filter.Filters.Add(BuiltFilter);
            }
            BuiltFilter = filter;
        }

        protected Task BuildPath(string globPattern, string defaultFileExtension)
        {
            globPattern = GlobPatternUtils.CompleteSimplifiedPattern(globPattern, defaultFileExtension);

            BuiltFilter = new PathFilter(globPattern, _fileSystem, _workspaceDirectoryPath).Cast<TItem, string>(GetItemPath);
            return Task.CompletedTask;
        }
    }
}