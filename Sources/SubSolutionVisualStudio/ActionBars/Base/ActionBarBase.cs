using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SubSolutionVisualStudio.Helpers;
using Task = System.Threading.Tasks.Task;

namespace SubSolutionVisualStudio.ActionBars.Base
{
    public abstract class ActionBarBase : IDisposable
    {
        protected InfoBar? InfoBar { get; private set; }

        protected abstract IEnumerable<IVsInfoBarTextSpan> TextSpans { get; }
        protected abstract ImageMoniker Moniker { get; }
        protected abstract Task<WindowFrame?> GetWindowFrameAsync(CancellationToken cancellationToken);
        protected abstract Task<bool> RunActionAsync(IVsInfoBarActionItem actionItem, VisualStudioOutputLogger outputLogger);

        public async Task ShowAsync(CancellationToken cancellationToken)
        {
            var infoBarModel = new InfoBarModel(TextSpans, Moniker);

            WindowFrame? windowFrame = await GetWindowFrameAsync(cancellationToken);
            if (windowFrame is null)
                return;
            
            InfoBar = await VS.InfoBar.CreateAsync(windowFrame, infoBarModel);
            if (InfoBar is null)
                return;

            InfoBar.ActionItemClicked += OnActionClicked;
            await InfoBar.TryShowInfoBarUIAsync();
        }

        private async void OnActionClicked(object s, InfoBarActionItemEventArgs e)
        {
            VisualStudioOutputLogger outputLogger = await VisualStudioOutputLogger.GetDefaultAsync();
            try
            {
                if (InfoBar is null)
                    return;

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (await RunActionAsync(e.ActionItem, outputLogger))
                    CloseInfoBar();
            }
            catch (Exception ex)
            {
                outputLogger.LogError(ex, "Failed to run a SubSolution info bar action.");
                CloseInfoBar();
            }
        }

        private void CloseInfoBar()
        {
            if (InfoBar is null)
                return;

            InfoBar.ActionItemClicked -= OnActionClicked;
            InfoBar.Close();
            InfoBar = null;
        }

        public void Dispose()
        {
            CloseInfoBar();
        }
    }
}