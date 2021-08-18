namespace SubSolution.Configuration
{
    public interface ISubSolutionConfigurationVisitor
    {
        void Visit(Folder folder);
        void Visit(Files files);
        void Visit(Projects projects);
        void Visit(SubSolutions subSolutions);
        void Visit(Configuration configuration);
        void Visit(Platform platform);
    }
}