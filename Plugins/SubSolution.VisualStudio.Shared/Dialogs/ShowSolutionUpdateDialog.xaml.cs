using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using SubSolution.VisualStudio.Helpers;
using SubSolution.VisualStudio.ViewModels;

namespace SubSolution.VisualStudio.Dialogs
{
    public partial class ShowSolutionUpdateDialog
    {
        public string SolutionName { get; }
        public IEnumerable<SolutionChangeViewModel> ItemChanges { get; }
        public IEnumerable<SolutionRootViewModel> SolutionRoot { get; }
        public IEnumerable<SolutionConfigurationPlatformViewModel> ConfigurationPlatforms { get; }

        public ShowSolutionUpdateDialog(SolutionUpdate solutionUpdate)
        {
            SolutionName = Path.GetFileNameWithoutExtension(solutionUpdate.SolutionFilePath);

            ItemChanges = solutionUpdate.Changes
                .Select(x => new SolutionChangeViewModel(x))
                .ToArray();

            SolutionRoot = new []
            {
                new SolutionRootViewModel(solutionUpdate.SolutionFilePath, solutionUpdate.GeneratedSolution.Root)
            };

            ConfigurationPlatforms = solutionUpdate.GeneratedSolution.ConfigurationPlatforms
                .Select(x => new SolutionConfigurationPlatformViewModel(x))
                .ToArray();

            DataContext = this;
            InitializeComponent();
        }

        private void OnClickApply(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
