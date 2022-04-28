using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using SubSolution.VisualStudio.ActionBars;
using SubSolution.VisualStudio.Helpers;
using Task = System.Threading.Tasks.Task;

namespace SubSolution.VisualStudio.Watchers
{
    public class OutdatedSolutionWatcher : IDisposable
    {
        private readonly SolutionWatcher _solutionWatcher;
        private readonly VisualStudioOutputLogger _outputLogger;

        private CancellationTokenSource? _cancellation;
        private OutdatedSolutionActionBar? _outdatedSolutionActionBar;
        private BackgroundGenerationErrorActionBar? _generationErrorActionBar;

        static public async Task<IDisposable> RunAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VisualStudioOutputLogger outputLogger = await VisualStudioOutputLogger.CreatePanelAsync("SubSolution - Background Watcher");
            return new OutdatedSolutionWatcher(outputLogger);
        }

        private OutdatedSolutionWatcher(VisualStudioOutputLogger outputLogger)
        {
            _solutionWatcher = new SolutionWatcher();
            _outputLogger = outputLogger;

            RunCheck();

            _solutionWatcher.SolutionOpened += OnSolutionOpened;
            _solutionWatcher.SolutionClosed += OnSolutionClosed;
        }

        public void Dispose()
        {
            _solutionWatcher.SolutionClosed -= OnSolutionClosed;
            _solutionWatcher.SolutionOpened -= OnSolutionOpened;

            DiscardPreviousCheck();
            _solutionWatcher.Dispose();
        }

        private void OnSolutionOpened(object sender, EventArgs e)
        {
            DiscardPreviousCheck();
            RunCheck();
        }

        private void OnSolutionClosed(object sender, EventArgs e)
        {
            DiscardPreviousCheck();
        }

        private void DiscardPreviousCheck()
        {
            _cancellation?.Cancel();
            _cancellation = null;

            _outdatedSolutionActionBar?.Dispose();
            _outdatedSolutionActionBar = null;

            _generationErrorActionBar?.Dispose();
            _generationErrorActionBar = null;
        }

        private void RunCheck()
        {
            _cancellation = new CancellationTokenSource();

            Task.Run(async () => await CreateCheckTaskAsync(_cancellation.Token), _cancellation.Token).FireAndForget(logOnFailure: true);
        }

        private async Task CreateCheckTaskAsync(CancellationToken cancellationToken)
        {
            string? solutionPath = await SubSolutionHelpers.GetCurrentSolutionPathAsync();
            if (solutionPath is null)
                return;

            string subSlnPath = SubSolutionHelpers.GetSubSlnPath(solutionPath);
            if (!File.Exists(subSlnPath))
                return;

            IVsTaskStatusCenterService taskStatusCenter = await VS.Services.GetTaskStatusCenterAsync();

            var options = new TaskHandlerOptions
            {
                Title = "SubSolution - Check solution is up-to-date",
                DisplayTaskDetails = _ => {},
                ActionsAfterCompletion = CompletionActions.RetainAndNotifyOnFaulted
            };

            var progress = new TaskProgressData
            {
                CanBeCanceled = true
            };

            ITaskHandler taskHandler = taskStatusCenter.PreRegister(options, progress);
            Task task = CheckSolutionIsUpToDateAsync(subSlnPath, progress, taskHandler, cancellationToken);
            taskHandler.RegisterTask(task);
        }

        // ReSharper disable once UnusedParameter.Local
        private async Task CheckSolutionIsUpToDateAsync(string subSlnPath, TaskProgressData progress, ITaskHandler taskHandler, CancellationToken token)
        {
            CancellationToken cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, taskHandler.UserCancellation).Token;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                SolutionUpdate solutionUpdate = await SubSolutionHelpers.PrepareUpdateAsync(
                    subSlnPath, _outputLogger,
                    (text, i) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        progress.ProgressText = text;
                        progress.PercentComplete = (int)((float)i / 3 * 100);

                        return Task.CompletedTask;
                    }
                );

                if (!solutionUpdate.HasChanges)
                    return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _outputLogger.LogError(ex, "Failed to check if the solution is up-to-date.");

                _generationErrorActionBar = new BackgroundGenerationErrorActionBar(subSlnPath, _outputLogger.OutputPane);
                await _generationErrorActionBar.ShowAsync(CancellationToken.None);

                throw;
            }

            _outdatedSolutionActionBar = new OutdatedSolutionActionBar(subSlnPath);
            await _outdatedSolutionActionBar.ShowAsync(cancellationToken);
        }
    }
}