using System.Threading.Tasks;

namespace SubSolution.Configuration
{
    public interface ISubSolutionConfigurationVisitor
    {
        Task VisitAsync(Folder folder);
        Task VisitAsync(Files files);
        Task VisitAsync(Projects projects);
        Task VisitAsync(SubSolutions subSolutions);
        void Visit(Configuration configuration);
        void Visit(Platform platform);
    }
}