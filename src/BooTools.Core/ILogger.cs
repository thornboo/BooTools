using System;

namespace BooTools.Core
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception ex);
        void LogDebug(string message);
    }
} 