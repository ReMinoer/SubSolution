using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SubSolution.Builders.Configuration;
using SubSolutionVisualStudio.Commands.Base;
using SubSolutionVisualStudio.Helpers;

namespace SubSolutionVisualStudio.Commands
{
    [Command(PackageIds.OpenSubSlnFileCommand)]
    internal sealed class OpenSubSlnFileCommand : SubSolutionContextualCommandBase<OpenSubSlnFileCommand>
    {
        protected override StatusAnimation AnimationIcon => StatusAnimation.General;

        protected override async Task ExecuteAsync(VisualStudioOutputLogger _)
        {
            string? subSlnPath = await SubSolutionHelpers.GetCurrentSubSlnPathAsync();
            if (subSlnPath is null)
                return;

            if (!File.Exists(subSlnPath))
            {
                if (await AskToCreateFileAsync())
                    CreateFile(subSlnPath);
                else
                    return;
            }

            await VS.Documents.OpenAsync(subSlnPath);
        }

        private async Task<bool> AskToCreateFileAsync()
        {
            VSConstants.MessageBoxResult askUserResult = await VS.MessageBox.ShowAsync(
                "No .subsln file found.",
                "Do you want to create a .subsln file next to the solution ?",
                OLEMSGICON.OLEMSGICON_QUERY,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO);

            return askUserResult == VSConstants.MessageBoxResult.IDYES;
        }

        private void CreateFile(string filePath)
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects()
                    }
                }
            };

            using XmlWriter xmlWriter = new XmlTextWriter(filePath, null)
            {
                Formatting = Formatting.Indented,
                Indentation = 4
            };

            configuration.Save(xmlWriter);
        }
    }
}
