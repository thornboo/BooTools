using System;
using System.IO;
using System.Text;

namespace BooTools.Core
{
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        private LogLevel _minLogLevel = LogLevel.INFO;

        public enum LogLevel
        {
            DEBUG = 0,
            INFO = 1,
            WARNING = 2,
            ERROR = 3
        }

        public FileLogger(string? logFilePath = null, LogLevel minLogLevel = LogLevel.INFO)
        {
            _minLogLevel = minLogLevel;
            
            if (string.IsNullOrEmpty(logFilePath))
            {
                // 将日志文件放在程序运行目录下，使用日期时间命名
                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                // 使用日期时间命名日志文件
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(logDirectory, $"boo-tools_{timestamp}.log");
            }
            else
            {
                _logFilePath = logFilePath;
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            _minLogLevel = level;
        }

        public LogLevel GetLogLevel()
        {
            return _minLogLevel;
        }

        public void LogInfo(string message)
        {
            if (_minLogLevel <= LogLevel.INFO)
            {
                WriteLog("INFO", message);
            }
        }

        public void LogWarning(string message)
        {
            if (_minLogLevel <= LogLevel.WARNING)
            {
                WriteLog("WARNING", message);
            }
        }

        public void LogError(string message)
        {
            if (_minLogLevel <= LogLevel.ERROR)
            {
                WriteLog("ERROR", message);
            }
        }

        public void LogError(string message, Exception ex)
        {
            if (_minLogLevel <= LogLevel.ERROR)
            {
                var fullMessage = $"{message}\n异常详情: {ex.Message}\n堆栈跟踪: {ex.StackTrace}";
                WriteLog("ERROR", fullMessage);
            }
        }

        public void LogDebug(string message)
        {
            if (_minLogLevel <= LogLevel.DEBUG)
            {
                WriteLog("DEBUG", message);
            }
        }

        private void WriteLog(string level, string message)
        {
            lock (_lockObject)
            {
                try
                {
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // 如果写入日志失败，我们不希望程序崩溃
                }
            }
        }

        public string GetLogFilePath()
        {
            return _logFilePath;
        }
    }
} 