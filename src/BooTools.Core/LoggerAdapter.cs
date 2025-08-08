using Microsoft.Extensions.Logging;
using System;

namespace BooTools.Core
{
    /// <summary>
    /// Adapts the new Microsoft.Extensions.Logging.ILogger interface to the old BooTools.Core.ILogger interface.
    /// This allows legacy components to write to the central logging system.
    /// </summary>
    public class LoggerAdapter : ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public LoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }

        public void LogInfo(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            _logger.LogError(ex, message);
        }
    }
}
