using System;
using System.Collections.Generic;

namespace SubSolution.Raw
{
    public interface IRawSolutionProject
    {
        Guid TypeGuid { get; }
        string Name { get; }
        string Path { get; }
        Guid ProjectGuid { get; }
        IReadOnlyList<IRawSolutionSection> Sections { get; }
    }
}