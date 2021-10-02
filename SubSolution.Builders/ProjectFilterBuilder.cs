using System.Threading.Tasks;
using SubSolution.Builders.Base;
using SubSolution.Builders.Configuration;
using SubSolution.FileSystems;

namespace SubSolution.Builders
{
    public class ProjectFilterBuilder : FilterBuilderBase<(string, ISolutionProject), ProjectFilters, IProjectFiltersVisitor>, IProjectFiltersVisitor
    {
        public ProjectFilterBuilder(IFileSystem fileSystem, string workspaceDirectoryPath)
            : base(fileSystem, workspaceDirectoryPath)
        {
        }

        protected override Task AcceptAsync(ProjectFilters visitable) => visitable.AcceptAsync(this);
        protected override string GetItemPath((string, ISolutionProject) item) => item.Item1;

        public Task VisitAsync(ProjectNot projectNot) => BuildNot(projectNot.ProjectFilters);
        public Task VisitAsync(ProjectMatchAll projectMatchAll) => BuildAll(projectMatchAll.ProjectFilters);
        public Task VisitAsync(ProjectMatchAnyOf projectMatchAnyOf) => BuildAnyOf(projectMatchAnyOf.ProjectFilters);
        public Task VisitAsync(ProjectPath projectPath) => BuildPath(projectPath.Match, "csproj");
    }
}