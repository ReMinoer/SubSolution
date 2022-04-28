using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace SubSolution.VisualStudio.ViewModels
{
    public class SolutionConfigurationPlatformViewModel : ISubSolutionTreeItemViewModel
    {
        public string DisplayName { get; }
        public string Path { get; }
        public IEnumerable<ISubSolutionTreeItemViewModel> SubItems { get; }

        public ImageMoniker Moniker => KnownMonikers.ShowAllConfigurations;
        public string MonikerToolTip => "Configuration-Platform";

        public SolutionConfigurationPlatformViewModel(ISolutionConfigurationPlatform configurationPlatform)
        {
            DisplayName = configurationPlatform.FullName;
            Path = DisplayName;

            SubItems = configurationPlatform.ProjectContexts.Select(x => new SolutionProjectContextViewModel(x.Key, x.Value)).ToArray();
        }
    }
}