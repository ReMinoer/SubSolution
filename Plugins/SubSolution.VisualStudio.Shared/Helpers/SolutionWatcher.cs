using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace SubSolution.VisualStudio.Helpers
{
    public class SolutionWatcher : IDisposable
    {
        private bool _disposed;

        public event EventHandler? SolutionOpened;
        public event EventHandler? SolutionClosed;

        public SolutionWatcher()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SolutionEvents solutionEvents = VS.Events.SolutionEvents;

            solutionEvents.OnAfterOpenSolution += OnAfterOpenSolution;
            solutionEvents.OnAfterCloseSolution += OnAfterCloseSolution;
        }

        private void OnAfterOpenSolution(Community.VisualStudio.Toolkit.Solution? obj)
        {
            SolutionOpened?.Invoke(this, EventArgs.Empty);
        }

        private void OnAfterCloseSolution()
        {
            SolutionClosed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            SolutionEvents solutionEvents = VS.Events.SolutionEvents;

            solutionEvents.OnAfterCloseSolution -= OnAfterCloseSolution;
            solutionEvents.OnAfterOpenSolution -= OnAfterOpenSolution;
        }
    }
}