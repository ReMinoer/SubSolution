using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace SubSolutionVisualStudio.ViewModels
{
    public class SolutionFileViewModel : ISubSolutionTreeItemViewModel
    {
        public string DisplayName { get; }
        public string Path { get; }

        public ImageMoniker Moniker => KnownMonikers.TextFile;
        public string MonikerToolTip => "File";
        public IEnumerable<ISubSolutionTreeItemViewModel> SubItems => Enumerable.Empty<ISubSolutionTreeItemViewModel>();

        public SolutionFileViewModel(string relativeFilePath)
        {
            DisplayName = System.IO.Path.GetFileName(relativeFilePath);
            Path = relativeFilePath;
        }
    }
}