using System.Threading.Tasks;
using SubSolution.Configuration.Builders.Base;
using SubSolution.FileSystems;

namespace SubSolution.Configuration.Builders
{
    public class FileFilterBuilder : FilterBuilderBase<string, FileFilters, IFileFiltersVisitor>, IFileFiltersVisitor
    {
        public FileFilterBuilder(IFileSystem fileSystem, string workingDirectory)
            : base(fileSystem, workingDirectory)
        {
        }

        protected override Task AcceptAsync(FileFilters visitable) => visitable.AcceptAsync(this);
        protected override string GetItemPath(string item) => item;

        public Task VisitAsync(FileNot fileNot) => BuildNot(fileNot.FileFilters);
        public Task VisitAsync(FileMatchAll fileMatchAll) => BuildAll(fileMatchAll.FileFilters);
        public Task VisitAsync(FileMatchAnyOf fileMatchAnyOf) => BuildAnyOf(fileMatchAnyOf.FileFilters);
        public Task VisitAsync(FilePath filePath) => BuildPath(filePath.Match, "*");
    }
}