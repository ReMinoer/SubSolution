using System.Collections.Generic;

namespace SubSolution.Raw
{
    public interface IRawSolutionSection
    {
        string Name { get; }
        IReadOnlyDictionary<string, string> Parameters { get; }
    }
}