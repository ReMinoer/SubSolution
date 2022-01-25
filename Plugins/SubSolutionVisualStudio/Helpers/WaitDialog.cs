using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace SubSolutionVisualStudio.Helpers
{
    public class WaitDialog : IDisposable
    {
        private readonly IVsThreadedWaitDialog4 _threadedWaitDialog;
        private readonly string _message;
        private readonly int _maxProgress;

        static public async Task<WaitDialog> ShowAsync(string title, string message, int maxProgress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var threadedWaitDialogFactory = (IVsThreadedWaitDialogFactory)await VS.Services.GetThreadedWaitDialogAsync();

            var waitDialog = new WaitDialog(threadedWaitDialogFactory, title, maxProgress);
            waitDialog._threadedWaitDialog.StartWaitDialog(title, message, string.Empty, null, string.Empty, iDelayToShowDialog: 1, true, true);
            return waitDialog;
        }

        private WaitDialog(IVsThreadedWaitDialogFactory threadedWaitDialogFactory, string message, int maxProgress)
        {
            _threadedWaitDialog = threadedWaitDialogFactory.CreateInstance();
            _message = message;
            _maxProgress = maxProgress;
        }

        public async Task UpdateAsync(string progressMessage, int progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _threadedWaitDialog.UpdateProgress(_message, progressMessage, progressMessage, progress, _maxProgress, false, out _);
        }

        public void Dispose()
        {
            _threadedWaitDialog.EndWaitDialog();
        }
    }
}