using System.Collections.Generic;

namespace SubSolution.Raw
{
    public interface IRawSolutionSection
    {
        string Name { get; }
        string? Parameter { get; }
        IReadOnlyList<string> Arguments { get; }
        IReadOnlyList<string> OrderedValuePairs { get; }
        IReadOnlyDictionary<string, string> ValuesByKey { get; }
    }
}