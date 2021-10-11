using System;
using Microsoft.Extensions.Logging;
using SubSolution.Utils;

namespace SubSolution.CommandLine
{
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _minLogLevel;

        public ConsoleLogger(LogLevel minLogLevel)
        {
            _minLogLevel = minLogLevel;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= _minLogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
                Console.WriteLine(formatter(state, exception));
        }

        public IDisposable BeginScope<TState>(TState state) => new Disposable();
    }
}