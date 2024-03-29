﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SubSolution.VisualStudio.ActionBars.Base;
using SubSolution.VisualStudio.Helpers;

namespace SubSolution.VisualStudio.ActionBars
{
    public class BackgroundGenerationErrorActionBar : SolutionExplorerActionBarBase
    {
        private readonly string _subSlnFilePath;
        private readonly OutputWindowPane _outputPane;

        protected override ImageMoniker Moniker { get; }
        protected override IEnumerable<IVsInfoBarTextSpan> TextSpans { get; } = new[]
        {
            new InfoBarTextSpan("We failed to generate a preview of your solution from your .subsln file to check it's up-to-date."),
            new InfoBarTextSpan(Environment.NewLine),
            new InfoBarHyperlink("Open .subsln", Action.OpenFile),
            new InfoBarTextSpan("   "),
            new InfoBarHyperlink("See log", Action.SeeLog)
        };

        private enum Action
        {
            OpenFile,
            SeeLog
        }

        public BackgroundGenerationErrorActionBar(string subSlnFilePath, OutputWindowPane outputPane, bool asWarning = false)
        {
            _subSlnFilePath = subSlnFilePath;
            _outputPane = outputPane;

            Moniker = asWarning ? KnownMonikers.XMLSchemaWarning : KnownMonikers.XMLSchemaError;
        }

        protected override async Task<bool> RunActionAsync(IVsInfoBarActionItem actionItem, VisualStudioOutputLogger _)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            switch (actionItem.ActionContext)
            {
                case Action.OpenFile:
                    await VS.Documents.OpenAsync(_subSlnFilePath);
                    break;
                case Action.SeeLog:
                    await _outputPane.ActivateAsync();
                    break;
            }

            return false;
        }
    }
}