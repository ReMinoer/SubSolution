using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SubSolution;
using SubSolution.Converters.Changes;
using SubSolution.Raw;

namespace SubSolutionVisualStudio.Helpers
{
    public class SolutionUpdate
    {
        public string SolutionFilePath { get; }
        public string SubSlnFilePath { get; }
        public ISolution GeneratedSolution { get; }
        public RawSolution UpdatedRawSolution { get; }
        public IReadOnlyCollection<SolutionChange> Changes { get; }
        public bool HasChanges => Changes.Count > 0;

        public SolutionUpdate(string solutionFilePath, string subSlnFilePath, Solution generatedSolution, RawSolution updatedRawSolution, IReadOnlyCollection<SolutionChange> changes)
        {
            SolutionFilePath = solutionFilePath;
            SubSlnFilePath = subSlnFilePath;
            GeneratedSolution = generatedSolution;
            UpdatedRawSolution = updatedRawSolution;
            Changes = changes.OrderBy(x => x).ToList().AsReadOnly();
        }

        public async Task ApplyAsync()
        {
            using FileStream solutionStream = File.Create(SolutionFilePath);
            await UpdatedRawSolution.WriteAsync(solutionStream);
        }
    }
}