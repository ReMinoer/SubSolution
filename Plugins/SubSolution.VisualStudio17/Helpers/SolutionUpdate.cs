using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SubSolution.Converters.Changes;
using SubSolution.Raw;

namespace SubSolution.VisualStudio.Helpers
{
    public class SolutionUpdate
    {
        public string SolutionFilePath { get; }
        public string SubSlnFilePath { get; }
        public ISolution GeneratedSolution { get; }
        public RawSolution UpdatedRawSolution { get; }
        public IReadOnlyCollection<SolutionChange> Changes { get; }
        public IReadOnlyCollection<Issue> Issues { get; }
        public bool HasChanges => Changes.Count > 0;

        public SolutionUpdate(string solutionFilePath,
            string subSlnFilePath,
            ISolution generatedSolution,
            RawSolution updatedRawSolution,
            IEnumerable<SolutionChange> changes,
            IEnumerable<Issue> issues)
        {
            SolutionFilePath = solutionFilePath;
            SubSlnFilePath = subSlnFilePath;
            GeneratedSolution = generatedSolution;
            UpdatedRawSolution = updatedRawSolution;
            Changes = changes.OrderBy(x => x).ToList().AsReadOnly();
            Issues = issues.ToList().AsReadOnly();
        }

        public async Task ApplyAsync()
        {
            using FileStream solutionStream = File.Create(SolutionFilePath);
            await UpdatedRawSolution.WriteAsync(solutionStream);
        }
    }
}