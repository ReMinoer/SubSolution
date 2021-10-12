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
            if (!IsEnabled(logLevel))
                return;

            ConsoleColor? color = GetColor(logLevel);
            if (color is null)
            {
                Console.WriteLine(formatter(state, exception));
                return;
            }

            Console.ForegroundColor = color.Value;
            Console.WriteLine(formatter(state, exception));
            Console.ResetColor();
        }

        private ConsoleColor? GetColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Warning:
                    return ConsoleColor.DarkYellow;
                case LogLevel.Error:
                case LogLevel.Critical:
                    return ConsoleColor.Red;
                default:
                    return null;
            }
        }

        public IDisposable BeginScope<TState>(TState state) => new Disposable();
    }
}