using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.Logging;
using SubSolution.Utils;

namespace SubSolutionVisualStudio.Helpers
{
    public class VisualStudioOutputLogger : ILogger
    {
        public OutputWindowPane OutputPane { get; }

        private VisualStudioOutputLogger(OutputWindowPane outputPane)
        {
            OutputPane = outputPane;
        }

        static private VisualStudioOutputLogger? _defaultLogger;

        static public async Task<VisualStudioOutputLogger> GetDefaultAsync()
        {
            return _defaultLogger ??= await CreatePanelAsync("SubSolution");
        }

        static public async Task<VisualStudioOutputLogger> CreatePanelAsync(string name)
        {
            return new VisualStudioOutputLogger(await VS.Windows.CreateOutputWindowPaneAsync(name));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            OutputPane.WriteLine(formatter(state, exception));
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        IDisposable ILogger.BeginScope<TState>(TState state) => new Disposable();
    }
}