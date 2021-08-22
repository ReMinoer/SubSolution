using System.Collections.Generic;

namespace SubSolution
{
    public interface ISolutionProject
    {
        string Path { get; set; }
        IReadOnlyCollection<string> Configurations { get; }
        IReadOnlyCollection<string> Platforms { get; }
    }
}