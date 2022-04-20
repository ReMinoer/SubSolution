using System.Threading.Tasks;
using SubSolution.Builders.Base;
using SubSolution.Builders.Configuration;
using SubSolution.Builders.GlobPatterns;
using SubSolution.ProjectReaders;

namespace SubSolution.Builders
{
    public class ProjectFilterBuilder : FilterBuilderBase<(string, ISolutionProject), ProjectFilters, IProjectFiltersVisitor>, IProjectFiltersVisitor
    {
        public ProjectFilterBuilder(IGlobPatternFileSystem fileSystem)
            : base(fileSystem)
        {
        }

        protected override Task AcceptAsync(ProjectFilters visitable) => visitable.AcceptAsync(this);
        protected override string GetItemPath((string, ISolutionProject) item) => item.Item1;

        public Task VisitAsync(ProjectNot projectNot) => BuildNotAsync(projectNot.ProjectFilters);
        public Task VisitAsync(ProjectMatchAll projectMatchAll) => BuildAllAsync(projectMatchAll.ProjectFilters);
        public Task VisitAsync(ProjectMatchAnyOf projectMatchAnyOf) => BuildAnyOfAsync(projectMatchAnyOf.ProjectFilters);
        public Task VisitAsync(ProjectPath projectPath) => BuildPathAsync(projectPath.Match, ProjectFileExtensions.ExtensionPatterns);
    }
}