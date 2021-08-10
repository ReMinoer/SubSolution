using System;
using System.Collections.Generic;

namespace SubSolution.Raw
{
    public interface IRawSolutionProject
    {
        Guid TypeGuid { get; }
        IReadOnlyList<string> Values { get; }
        IReadOnlyList<IRawSolutionSection> Sections { get; }
    }
}