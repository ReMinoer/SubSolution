using System.Threading.Tasks;

namespace SubSolution.Builders.Configuration
{
    public interface IFileFiltersVisitor
    {
        Task VisitAsync(FileNot fileNot);
        Task VisitAsync(FileMatchAll fileMatchAll);
        Task VisitAsync(FileMatchAnyOf fileMatchAnyOf);
        Task VisitAsync(FilePath filePath);
    }
}