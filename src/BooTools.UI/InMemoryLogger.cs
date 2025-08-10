using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace BooTools.UI
{
    /// <summary>
    /// A thread-safe, in-memory logger provider that captures log messages.
    /// </summary>
    public sealed class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, InMemoryLogger> _loggers = new();

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new InMemoryLogger());

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    /// <summary>
    /// The actual logger that writes messages to a static, thread-safe queue.
    /// </summary>
    public sealed class InMemoryLogger : ILogger
    {
        // Static queue to hold all log messages from all logger instances
        public static readonly ConcurrentQueue<string> LogMessages = new();

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true; // Capture all levels

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLevelString = logLevel.ToString().ToUpper();
            
            LogMessages.Enqueue($"[{timestamp}] [{logLevelString}] {message}");
        }

        // A simple class for empty scopes
        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
