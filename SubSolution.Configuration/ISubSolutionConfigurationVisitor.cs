namespace SubSolution.Configuration
{
    public interface ISubSolutionConfigurationVisitor
    {
        public void Visit(SolutionRootConfiguration root);
        public void Visit(Folder folder);
        public void Visit(Files files);
        public void Visit(Projects projects);
        public void Visit(SubSolutions subSolutions);
    }
}