using System;

namespace BooTools.Core
{
    public class ConsoleLogger : ILogger
    {
        private readonly FileLogger _fileLogger;
        private readonly bool _showConsole;

        public ConsoleLogger(bool showConsole = true, FileLogger.LogLevel minLogLevel = FileLogger.LogLevel.INFO)
        {
            _fileLogger = new FileLogger(null, minLogLevel);
            _showConsole = showConsole;
            
            if (_showConsole)
            {
                // 尝试分配控制台
                try
                {
                    if (!Console.IsOutputRedirected)
                    {
                        Console.WriteLine("=== BooTools 日志输出 ===");
                    }
                }
                catch
                {
                    // 如果无法分配控制台，就只使用文件日志
                }
            }
        }

        public void SetLogLevel(FileLogger.LogLevel level)
        {
            _fileLogger.SetLogLevel(level);
        }

        public FileLogger.LogLevel GetLogLevel()
        {
            return _fileLogger.GetLogLevel();
        }

        public void LogInfo(string message)
        {
            var logMessage = $"[INFO] {message}";
            if (_showConsole)
            {
                try
                {
                    Console.WriteLine(logMessage);
                }
                catch
                {
                    // 忽略控制台输出错误
                }
            }
            _fileLogger.LogInfo(message);
        }

        public void LogWarning(string message)
        {
            var logMessage = $"[WARNING] {message}";
            if (_showConsole)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(logMessage);
                    Console.ResetColor();
                }
                catch
                {
                    // 忽略控制台输出错误
                }
            }
            _fileLogger.LogWarning(message);
        }

        public void LogError(string message)
        {
            var logMessage = $"[ERROR] {message}";
            if (_showConsole)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(logMessage);
                    Console.ResetColor();
                }
                catch
                {
                    // 忽略控制台输出错误
                }
            }
            _fileLogger.LogError(message);
        }

        public void LogError(string message, Exception ex)
        {
            var logMessage = $"[ERROR] {message}";
            if (_showConsole)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(logMessage);
                    Console.WriteLine($"异常详情: {ex.Message}");
                    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                    Console.ResetColor();
                }
                catch
                {
                    // 忽略控制台输出错误
                }
            }
            _fileLogger.LogError(message, ex);
        }

        public void LogDebug(string message)
        {
            var logMessage = $"[DEBUG] {message}";
            if (_showConsole)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(logMessage);
                    Console.ResetColor();
                }
                catch
                {
                    // 忽略控制台输出错误
                }
            }
            _fileLogger.LogDebug(message);
        }

        public string GetLogFilePath()
        {
            return _fileLogger.GetLogFilePath();
        }

        public FileLogger GetFileLogger()
        {
            return _fileLogger;
        }
    }
} 