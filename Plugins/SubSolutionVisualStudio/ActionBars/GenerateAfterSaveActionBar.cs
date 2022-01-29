using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SubSolutionVisualStudio.ActionBars.Base;
using SubSolutionVisualStudio.Helpers;

namespace SubSolutionVisualStudio.ActionBars
{
    public class GenerateAfterSaveActionBar : DocumentActionBarBase
    {
        protected override ImageMoniker Moniker => KnownMonikers.VisualStudioSettingsFile;
        protected override IEnumerable<IVsInfoBarTextSpan> TextSpans { get; } = new[]
        {
            new InfoBarTextSpan("Do you want to regenerate a solution from your saved .subsln file ?   "),
            new InfoBarButton("Preview")
        };

        public GenerateAfterSaveActionBar(string subSlnPath)
            : base(subSlnPath)
        {
        }

        protected override async Task<bool> RunActionAsync(IVsInfoBarActionItem actionItem, VisualStudioOutputLogger outputLogger)
        {
            await SubSolutionHelpers.GenerateAndUpdateSolutionAsync(DocumentFilePath, outputLogger);
            return true;
        }
    }
}