using System.Threading.Tasks;

namespace SubSolution.Configuration
{
    public interface ISolutionItemSourcesVisitor
    {
        Task VisitAsync(Folder folder);
        Task VisitAsync(Files files);
        Task VisitAsync(Projects projects);
        Task VisitAsync(Solutions solutions);
        Task VisitAsync(SubSolutions subSolutions);
        Task VisitAsync(Dependencies dependencies);
        Task VisitAsync(Dependents dependents);
    }
}