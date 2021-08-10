using System.Collections.Generic;

namespace SubSolution.Raw
{
    public interface IRawSolution
    {
        // https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file
        string SlnFormatVersion { get; }
        string MajorVisualStudioVersion { get; }
        string VisualStudioVersion { get; }
        string MinimumVisualStudioVersion  { get; }
        IReadOnlyList<IRawSolutionProject> Projects { get; }
        IReadOnlyList<IRawSolutionSection> GlobalSections { get; }
    }
}