using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging.Interop;

namespace SubSolutionVisualStudio.ViewModels
{
    public interface ISubSolutionTreeItemViewModel
    {
        abstract string DisplayName { get; }
        abstract string Path { get; }
        abstract ImageMoniker Moniker { get; }
        abstract string MonikerToolTip { get; }
        abstract IEnumerable<ISubSolutionTreeItemViewModel> SubItems { get; }
    }
}