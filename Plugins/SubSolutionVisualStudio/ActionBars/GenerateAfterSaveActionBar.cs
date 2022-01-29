﻿using System.Collections.Generic;
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
            new InfoBarButton("Ignore", Action.Ignore),
            new InfoBarTextSpan("   "),
            new InfoBarButton("Preview", Action.Preview)
        };

        private enum Action
        {
            Ignore,
            Preview
        }

        public GenerateAfterSaveActionBar(string subSlnPath)
            : base(subSlnPath)
        {
        }

        protected override async Task<bool> RunActionAsync(IVsInfoBarActionItem actionItem, VisualStudioOutputLogger outputLogger)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            switch (actionItem.ActionContext)
            {
                case Action.Preview:
                    await SubSolutionHelpers.GenerateAndUpdateSolutionAsync(DocumentFilePath, outputLogger);
                    break;
            }

            return true;
        }
    }
}