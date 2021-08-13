using System;
using System.Collections.Generic;

namespace SubSolution.Raw
{
    public interface IRawSolution
    {
        Version SlnFormatVersion { get; }
        int MajorVisualStudioVersion { get; }
        Version VisualStudioVersion { get; }
        Version MinimumVisualStudioVersion  { get; }
        IReadOnlyList<IRawSolutionProject> Projects { get; }
        IReadOnlyList<IRawSolutionSection> GlobalSections { get; }
    }
}