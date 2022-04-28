using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace SubSolution.VisualStudio.ViewModels
{
    public class SolutionRootViewModel : SolutionFolderViewModel
    {
        public override string DisplayName { get; }
        public override string Path { get; }
        public override ImageMoniker Moniker => KnownMonikers.Solution;

        public SolutionRootViewModel(string solutionPath, ISolutionFolder solutionFolder)
            : base(string.Empty, string.Empty, solutionFolder)
        {
            DisplayName = System.IO.Path.GetFileNameWithoutExtension(solutionPath);
            Path = solutionPath;
        }
    }
}