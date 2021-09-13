using System.Threading.Tasks;

namespace SubSolution.Configuration
{
    public interface IAsyncVisitable<in TVisitor>
    {
        Task AcceptAsync(TVisitor visitor);
    }

    public partial class SolutionItems : IAsyncVisitable<ISolutionItemSourcesVisitor>
    {
        public abstract Task AcceptAsync(ISolutionItemSourcesVisitor visitor);
    }

    public partial class Folder
    {
        public override Task AcceptAsync(ISolutionItemSourcesVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class Files
    {
        public override Task AcceptAsync(ISolutionItemSourcesVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class Projects
    {
        public override Task AcceptAsync(ISolutionItemSourcesVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class Solutions
    {
        public override Task AcceptAsync(ISolutionItemSourcesVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class SubSolutions
    {
        public override Task AcceptAsync(ISolutionItemSourcesVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class ProjectFilters : IAsyncVisitable<IProjectFiltersVisitor>
    {
        public abstract Task AcceptAsync(IProjectFiltersVisitor visitor);
    }

    public partial class ProjectNot
    {
        public override Task AcceptAsync(IProjectFiltersVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class ProjectMatchAll
    {
        public override Task AcceptAsync(IProjectFiltersVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class ProjectMatchAnyOf
    {
        public override Task AcceptAsync(IProjectFiltersVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class ProjectPath
    {
        public override Task AcceptAsync(IProjectFiltersVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class FileFilters : IAsyncVisitable<IFileFiltersVisitor>
    {
        public abstract Task AcceptAsync(IFileFiltersVisitor visitor);
    }

    public partial class FileNot
    {
        public override Task AcceptAsync(IFileFiltersVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class FileMatchAll
    {
        public override Task AcceptAsync(IFileFiltersVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class FileMatchAnyOf
    {
        public override Task AcceptAsync(IFileFiltersVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class FilePath
    {
        public override Task AcceptAsync(IFileFiltersVisitor visitor) => visitor.VisitAsync(this);
    }
}