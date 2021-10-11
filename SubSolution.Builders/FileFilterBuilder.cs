using System.Threading.Tasks;
using SubSolution.Builders.Base;
using SubSolution.Builders.Configuration;
using SubSolution.Builders.GlobPatterns;

namespace SubSolution.Builders
{
    public class FileFilterBuilder : FilterBuilderBase<string, FileFilters, IFileFiltersVisitor>, IFileFiltersVisitor
    {
        public FileFilterBuilder(IGlobPatternFileSystem fileSystem, string workspaceDirectoryPath)
            : base(fileSystem, workspaceDirectoryPath)
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