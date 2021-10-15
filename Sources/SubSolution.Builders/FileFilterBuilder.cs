using System.Threading.Tasks;
using SubSolution.Builders.Base;
using SubSolution.Builders.Configuration;
using SubSolution.Builders.GlobPatterns;

namespace SubSolution.Builders
{
    public class FileFilterBuilder : FilterBuilderBase<string, FileFilters, IFileFiltersVisitor>, IFileFiltersVisitor
    {
        public FileFilterBuilder(IGlobPatternFileSystem fileSystem)
            : base(fileSystem)
        {
        }

        protected override Task AcceptAsync(FileFilters visitable) => visitable.AcceptAsync(this);
        protected override string GetItemPath(string item) => item;

        public Task VisitAsync(FileNot fileNot) => BuildNotAsync(fileNot.FileFilters);
        public Task VisitAsync(FileMatchAll fileMatchAll) => BuildAllAsync(fileMatchAll.FileFilters);
        public Task VisitAsync(FileMatchAnyOf fileMatchAnyOf) => BuildAnyOfAsync(fileMatchAnyOf.FileFilters);
        public Task VisitAsync(FilePath filePath) => BuildPathAsync(filePath.Match, "*");
    }
}