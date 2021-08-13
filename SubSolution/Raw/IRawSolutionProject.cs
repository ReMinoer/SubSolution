using System;
using System.Collections.Generic;

namespace SubSolution.Raw
{
    public interface IRawSolutionProject
    {
        Guid TypeGuid { get; }
        IReadOnlyList<string> Arguments { get; }
        IReadOnlyList<IRawSolutionSection> Sections { get; }
    }
}