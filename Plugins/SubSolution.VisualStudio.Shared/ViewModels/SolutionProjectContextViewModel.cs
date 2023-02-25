using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace SubSolution.VisualStudio.ViewModels
{
    public class SolutionProjectContextViewModel : ISubSolutionTreeItemViewModel
    {
        private readonly SolutionProjectContext _projectContext;

        public string DisplayName { get; }
        public string Path { get; }

        public ImageMoniker Moniker
        {
            get
            {
                if (_projectContext.Deploy)
                    return KnownMonikers.PackageDeployment;
                if (_projectContext.Build)
                    return KnownMonikers.BuildSolution;

                return KnownMonikers.CancelBuild;
            }
        }

        public string MonikerToolTip
        {
            get
            {
                if (_projectContext.Build && _projectContext.Deploy)
                    return "Build & Deploy";
                if (_projectContext.Build)
                    return "Build";
                if (_projectContext.Deploy)
                    return "Deploy only";

                return "No Build";
            }
        }

        public IEnumerable<ISubSolutionTreeItemViewModel> SubItems => Enumerable.Empty<ISubSolutionTreeItemViewModel>();

        public SolutionProjectContextViewModel(string projectPath, SolutionProjectContext projectContext)
        {
            _projectContext = projectContext;

            DisplayName = $"{System.IO.Path.GetFileNameWithoutExtension(projectPath)}" +
                $" -> {_projectContext.GetConfigurationPlatformName(" | ")}";

            Path = projectPath;
        }
    }
}