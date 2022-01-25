using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SubSolutionVisualStudio.ActionBars.Base;
using SubSolutionVisualStudio.Helpers;

namespace SubSolutionVisualStudio.ActionBars
{
    public class OutdatedSolutionActionBar : SolutionExplorerActionBarBase
    {
        private readonly string _subSlnPath;

        protected override ImageMoniker Moniker => KnownMonikers.VisualStudioSettingsFile;
        protected override IEnumerable<IVsInfoBarTextSpan> TextSpans { get; } = new[]
        {
            new InfoBarTextSpan("We detected new changes to apply to your solution after generation of your .subsln file."),
            new InfoBarTextSpan(Environment.NewLine),
            new InfoBarHyperlink("Open .subsln", Action.OpenFile),
            new InfoBarTextSpan("   "),
            new InfoBarHyperlink("Preview", Action.Preview)
        };

        private enum Action
        {
            OpenFile,
            Preview
        }

        public OutdatedSolutionActionBar(string subSlnPath)
        {
            _subSlnPath = subSlnPath;
        }

        protected override async Task<bool> RunActionAsync(IVsInfoBarActionItem actionItem, VisualStudioOutputLogger outputLogger)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (actionItem.ActionContext)
            {
                case Action.OpenFile:
                    await VS.Documents.OpenAsync(_subSlnPath);
                    break;
                case Action.Preview:
                    return await SubSolutionHelpers.GenerateAndUpdateSolutionAsync(_subSlnPath, outputLogger);
            }

            return false;
        }
    }
}