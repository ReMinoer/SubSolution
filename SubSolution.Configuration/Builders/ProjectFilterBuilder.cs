using System.Threading.Tasks;
using SubSolution.Configuration.Builders.Base;
using SubSolution.FileSystems;

namespace SubSolution.Configuration.Builders
{
    public class ProjectFilterBuilder : FilterBuilderBase<(string, ISolutionProject), ProjectFilters, IProjectFiltersVisitor>, IProjectFiltersVisitor
    {
        public ProjectFilterBuilder(IFileSystem fileSystem, string workingDirectory)
            : base(fileSystem, workingDirectory)
        {
        }

        protected override Task AcceptAsync(ProjectFilters visitable) => visitable.AcceptAsync(this);
        protected override string GetItemPath((string, ISolutionProject) item) => item.Item1;

        public Task VisitAsync(ProjectNot fileNot) => BuildNot(fileNot.ProjectFilters);
        public Task VisitAsync(ProjectMatchAll fileMatchAll) => BuildAll(fileMatchAll.ProjectFilters);
        public Task VisitAsync(ProjectMatchAnyOf fileMatchAnyOf) => BuildAnyOf(fileMatchAnyOf.ProjectFilters);
        public Task VisitAsync(ProjectPath filePath) => BuildPath(filePath.Match, "csproj");
    }
}