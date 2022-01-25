using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using SubSolution;
using SubSolutionVisualStudio.Helpers;

namespace SubSolutionVisualStudio.ViewModels
{
    public class SolutionProjectViewModel : ISubSolutionTreeItemViewModel
    {
        private readonly ISolutionProject _solutionProject;

        public string DisplayName { get; }
        public string Path { get; }
        public ImageMoniker Moniker => SubSolutionMonikers.GetObjectTypeMoniker(_solutionProject.Type);

        public string MonikerToolTip
        {
            get
            {
                if (_solutionProject.Type is not null)
                {
                    string projectName = ProjectTypes.DisplayNames[_solutionProject.Type.Value];
                    if (_solutionProject.Type == ProjectType.Folder)
                        return projectName;
                    
                    return $"{projectName} Project";
                }
                
                return "Project";
            }
        }

        public IEnumerable<ISubSolutionTreeItemViewModel> SubItems => Enumerable.Empty<ISubSolutionTreeItemViewModel>();

        public SolutionProjectViewModel(string relativeFilePath, ISolutionProject solutionProject)
        {
            _solutionProject = solutionProject;

            DisplayName = System.IO.Path.GetFileNameWithoutExtension(relativeFilePath);
            Path = relativeFilePath;
        }
    }
}