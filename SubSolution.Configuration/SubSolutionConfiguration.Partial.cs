using System.Threading.Tasks;

namespace SubSolution.Configuration
{
    public partial class SolutionItems
    {
        public abstract Task AcceptAsync(ISubSolutionConfigurationVisitor visitor);
    }

    public partial class Folder
    {
        public override Task AcceptAsync(ISubSolutionConfigurationVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class Files
    {
        public override Task AcceptAsync(ISubSolutionConfigurationVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class Projects
    {
        public override Task AcceptAsync(ISubSolutionConfigurationVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class Solutions
    {
        public override Task AcceptAsync(ISubSolutionConfigurationVisitor visitor) => visitor.VisitAsync(this);
    }

    public partial class SubSolutions
    {
        public override Task AcceptAsync(ISubSolutionConfigurationVisitor visitor) => visitor.VisitAsync(this);
    }
}