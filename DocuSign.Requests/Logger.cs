using System;
using System.IO;
using System.Text;

namespace DocuSign.Requests
{
    public class Logger
    {
        private static readonly object _lock = new object();
        private readonly string _logFilePath;
        private readonly string _logFileName;
        private readonly string _logLevel;

        public Logger(string logFilePath, string logFileName, string logLevel = "Information")
        {
            _logFilePath = logFilePath;
            _logFileName = logFileName;
            _logLevel = logLevel;

            // Ensure log directory exists
            if (!Directory.Exists(_logFilePath))
            {
                Directory.CreateDirectory(_logFilePath);
            }
        }

        private string GetLogFileName()
        {
            string fileName = _logFileName.Replace("{date}", DateTime.Now.ToString("yyyyMMdd"));
            return Path.Combine(_logFilePath, fileName);
        }

        public void LogInformation(string message, params object[] args)
        {
            Log("INFO", message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            Log("WARN", message, args);
        }

        public void LogError(string message, params object[] args)
        {
            Log("ERROR", message, args);
        }

        public void LogError(string message, Exception ex, params object[] args)
        {
            string fullMessage = args.Length > 0 ? string.Format(message, args) : message;
            if (ex != null)
            {
                fullMessage = $"{fullMessage}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
            Log("ERROR", fullMessage);
        }

        public void LogDebug(string message, params object[] args)
        {
            if (_logLevel == "Debug" || _logLevel == "Trace")
            {
                Log("DEBUG", message, args);
            }
        }

        private void Log(string level, string message, params object[] args)
        {
            try
            {
                lock (_lock)
                {
                    string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {formattedMessage}";
                    
                    string logFile = GetLogFileName();
                    File.AppendAllText(logFile, logEntry + Environment.NewLine);

                    // Also write to console for debugging
                    Console.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                // If logging fails, write to console
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        public void LogMethodEntry(string methodName, params object[] parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Entering method: {methodName}");
            
            if (parameters != null && parameters.Length > 0)
            {
                sb.Append(" with parameters: ");
                for (int i = 0; i < parameters.Length; i++)
                {
                    sb.Append($"[{i}]={parameters[i]?.ToString() ?? "null"}");
                    if (i < parameters.Length - 1) sb.Append(", ");
                }
            }
            
            LogDebug(sb.ToString());
        }

        public void LogMethodExit(string methodName, object returnValue = null)
        {
            string message = $"Exiting method: {methodName}";
            if (returnValue != null)
            {
                message += $" with return value: {returnValue}";
            }
            LogDebug(message);
        }

        public void CleanOldLogs(int retentionDays)
        {
            try
            {
                if (Directory.Exists(_logFilePath))
                {
                    var files = Directory.GetFiles(_logFilePath, "*.log");
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < DateTime.Now.AddDays(-retentionDays))
                        {
                            File.Delete(file);
                            LogInformation($"Deleted old log file: {file}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error cleaning old logs", ex);
            }
        }
    }
}
