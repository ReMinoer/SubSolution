using System;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using SubSolutionVisualStudio.Helpers;
using Task = System.Threading.Tasks.Task;

namespace SubSolutionVisualStudio.Commands.Base
{
    internal abstract class SubSolutionContextualCommandBase<T> : BaseCommand<T>
        where T : class, new()
    {
        protected abstract StatusAnimation AnimationIcon { get; }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await VS.StatusBar.StartAnimationAsync(AnimationIcon);

            VisualStudioOutputLogger outputLogger = await VisualStudioOutputLogger.GetDefaultAsync();
            try
            {
                await ExecuteAsync(outputLogger);
            }
            catch (Exception ex)
            {
                outputLogger.LogError(ex, $"Failed to execute command \"{nameof(T)}\".");

                await VS.StatusBar.EndAnimationAsync(AnimationIcon);
            }
        }

        protected abstract Task ExecuteAsync(VisualStudioOutputLogger outputLogger);
    }
}