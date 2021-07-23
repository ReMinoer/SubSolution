namespace SubSolution.Configuration
{
    public partial class SolutionRootConfiguration
    {
        public void Accept(ISubSolutionConfigurationVisitor visitor) => visitor.Visit(this);
    }

    public partial class SolutionItems
    {
        public abstract void Accept(ISubSolutionConfigurationVisitor visitor);
    }

    public partial class Folder
    {
        public override void Accept(ISubSolutionConfigurationVisitor visitor) => visitor.Visit(this);
    }

    public partial class Files
    {
        public override void Accept(ISubSolutionConfigurationVisitor visitor) => visitor.Visit(this);
    }

    public partial class Projects
    {
        public override void Accept(ISubSolutionConfigurationVisitor visitor) => visitor.Visit(this);
    }

    public partial class SubSolutions
    {
        public override void Accept(ISubSolutionConfigurationVisitor visitor) => visitor.Visit(this);
    }
}