﻿using System.Threading.Tasks;

namespace SubSolution.Configuration
{
    public interface IProjectFiltersVisitor
    {
        Task VisitAsync(ProjectNot projectNot);
        Task VisitAsync(ProjectMatchAll projectMatchAll);
        Task VisitAsync(ProjectMatchAnyOf projectMatchAnyOf);
        Task VisitAsync(ProjectPath projectPath);
    }
}