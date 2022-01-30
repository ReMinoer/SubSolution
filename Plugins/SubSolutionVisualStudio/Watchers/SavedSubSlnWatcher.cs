using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using SubSolution.FileSystems;
using SubSolutionVisualStudio.ActionBars;

namespace SubSolutionVisualStudio.Watchers
{
    public class SavedSubSlnWatcher : IDisposable
    {
        private readonly ConcurrentDictionary<string, GenerateAfterSaveActionBar> _actionBarsByFilePath;

        static public async Task<IDisposable> RunAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return new SavedSubSlnWatcher();
        }

        private SavedSubSlnWatcher()
        {
            _actionBarsByFilePath = new ConcurrentDictionary<string, GenerateAfterSaveActionBar>(PathComparer.Default);

            VS.Events.DocumentEvents.Saved += OnDocumentSaved;
            VS.Events.DocumentEvents.Closed += OnDocumentClosed;
        }

        public void Dispose()
        {
            VS.Events.DocumentEvents.Closed -= OnDocumentClosed;
            VS.Events.DocumentEvents.Saved -= OnDocumentSaved;
        }

        private async void OnDocumentSaved(string filePath)
        {
            try
            {
                if (Path.GetExtension(filePath) != ".subsln")
                    return;

                if (!_actionBarsByFilePath.ContainsKey(filePath))
                {
                    var actionBar = new GenerateAfterSaveActionBar(filePath);
                    actionBar.Closed += OnActionBarClosed;

                    await actionBar.ShowAsync(CancellationToken.None);
                    _actionBarsByFilePath.TryAdd(filePath, actionBar);
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }

        private void OnActionBarClosed(object sender, EventArgs e)
        {
            var actionBar = (GenerateAfterSaveActionBar)sender;

            if (_actionBarsByFilePath.TryRemove(actionBar.DocumentFilePath, out _))
                actionBar.Closed -= OnActionBarClosed;
        }

        private void OnDocumentClosed(string filePath)
        {
            if (_actionBarsByFilePath.TryRemove(filePath, out GenerateAfterSaveActionBar actionBar))
                actionBar.Closed -= OnActionBarClosed;
        }
    }
}