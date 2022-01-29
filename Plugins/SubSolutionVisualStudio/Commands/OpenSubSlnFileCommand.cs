using System.IO;
using System.Xml;
using Community.VisualStudio.Toolkit;
using SubSolution.Builders.Configuration;
using SubSolutionVisualStudio.Commands.Base;
using SubSolutionVisualStudio.Helpers;
using Task = System.Threading.Tasks.Task;

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
                CreateFile(subSlnPath);

            await VS.Documents.OpenAsync(subSlnPath);
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
