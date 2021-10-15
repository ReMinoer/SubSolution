using System;

namespace SubSolution
{
    public interface IFilterableSolutionFolder : ISolutionFolder
    {
        void FilterProjects(Func<string, ISolutionProject, bool> predicate);
        void FilterFiles(Func<string, bool> predicate);
    }
}