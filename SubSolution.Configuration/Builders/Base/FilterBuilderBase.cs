using System.Collections.Generic;
using System.Threading.Tasks;
using SubSolution.Configuration.Builders.Filters;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution.Configuration.Builders.Base
{
    public abstract class FilterBuilderBase<TItem, TVisitable, TVisitor>
        where TVisitable : IAsyncVisitable<TVisitor>
    {
        protected readonly IFileSystem _fileSystem;
        protected readonly string _workspaceDirectoryPath;
        public IFilter<TItem> BuiltFilter { get; protected set; } = new AllFilter<TItem>();

        protected FilterBuilderBase(IFileSystem fileSystem, string workspaceDirectoryPath)
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