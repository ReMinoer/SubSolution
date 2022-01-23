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
        private ConcurrentDictionary<string, GenerateAfterSaveActionBar> _actionBarsByFilePath;

        static public async Task<IDisposable> RunAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return new SavedSubSlnWatcher();
        }

        private SavedSubSlnWatcher()
        {
            _actionBarsByFilePath = new ConcurrentDictionary<string, GenerateAfterSaveActionBar>(PathComparer.Default);

            VS.Events.DocumentEvents.Saved += OnDocumentSaved;
        }

        public void Dispose()
        {
            VS.Events.DocumentEvents.Saved -= OnDocumentSaved;
        }

        private async void OnDocumentSaved(string filePath)
        {
            try
            {
                if (Path.GetExtension(filePath) != ".subsln")
                    return;

                if (_actionBarsByFilePath.TryRemove(filePath, out GenerateAfterSaveActionBar actionBar))
                    actionBar.Dispose();

                actionBar = new GenerateAfterSaveActionBar(filePath);
                await actionBar.ShowAsync(CancellationToken.None);

                _actionBarsByFilePath.TryAdd(filePath, actionBar);
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }
    }
}