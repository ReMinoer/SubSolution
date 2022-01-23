using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using SubSolution;

namespace SubSolutionVisualStudio.ViewModels
{
    public class SolutionFolderViewModel : ISubSolutionTreeItemViewModel
    {
        public virtual string DisplayName { get; }
        public virtual string Path { get; }
        public IEnumerable<ISubSolutionTreeItemViewModel> SubItems { get; }

        public virtual ImageMoniker Moniker => KnownMonikers.FolderClosed;
        public string MonikerToolTip => "Folder";

        public SolutionFolderViewModel(string parentFolderPath, string name, ISolutionFolder solutionFolder)
        {
            DisplayName = name;
            Path = !string.IsNullOrEmpty(parentFolderPath) ? System.IO.Path.Combine(parentFolderPath, name) : name;

            IEnumerable<SolutionFolderViewModel> folders = solutionFolder.SubFolders.Select(x => new SolutionFolderViewModel(Path, x.Key, x.Value));
            IEnumerable<SolutionProjectViewModel> projects = solutionFolder.Projects.Select(x => new SolutionProjectViewModel(x.Key, x.Value));
            IEnumerable<SolutionFileViewModel> files = solutionFolder.FilePaths.Select(x => new SolutionFileViewModel(x));

            SubItems = folders.Concat<ISubSolutionTreeItemViewModel>(projects).Concat(files).ToArray();
        }
    }
}