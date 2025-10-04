using System;
using System.IO;

// By: SpinxDev 2025 xD
namespace UIEdit.Utils
{
    public static class Logger
    {
        #region Injection Properties
        private static readonly string LogsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string ActionsLogPath = Path.Combine(LogsDirectory, "Actions.log");
        private static readonly string ErrorLogPath = Path.Combine(LogsDirectory, "Error.log");
        private static readonly object _lockObject = new object();
        #endregion

        #region Constructor
        static Logger()
        {
            if (!Directory.Exists(LogsDirectory))
            {
                Directory.CreateDirectory(LogsDirectory);
            }
        }
        #endregion
        /// <summary>
        /// Logs a general action message.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="message"></param>
        public static void LogAction(string functionName, string message)
        {
            try
            {
                var logEntry = FormatLogEntry(functionName, message);
                WriteToFile(ActionsLogPath, logEntry);
            }
            catch (Exception ex)
            {
                LogError("Logger.LogAction", $"Falha ao escrever log de acao: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs an error message with optional exception details.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public static void LogError(string functionName, string message, Exception exception = null)
        {
            try
            {
                var fullMessage = message;
                if (exception != null)
                {
                    fullMessage += $" | Exception: {exception.Message} | StackTrace: {exception.StackTrace}";
                }

                var logEntry = FormatLogEntry(functionName, fullMessage);
                WriteToFile(ErrorLogPath, logEntry);
            }
            catch (Exception ex)
            {
                try
                {
                    var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Error_Fallback.log");
                    var fallbackEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][Logger.LogError] - Falha critica no sistema de logging: {ex.Message}";
                    File.AppendAllText(fallbackPath, fallbackEntry + Environment.NewLine);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Logs an action with its parameters.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="action"></param>
        /// <param name="parameters"></param>
        public static void LogActionWithParams(string functionName, string action, params object[] parameters)
        {
            try
            {
                var paramString = string.Join(", ", parameters);
                var message = $"{action} | Parametros: {paramString}";
                LogAction(functionName, message);
            }
            catch (Exception ex)
            {
                LogError("Logger.LogActionWithParams", $"Falha ao escrever log de acao com parametros: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs the start of an operation with its name.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="operation"></param>
        public static void LogOperationStart(string functionName, string operation)
        {
            LogAction(functionName, $"INICIO: {operation}");
        }

        /// <summary>
        /// Logs the end of an operation with its success or failure status.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="operation"></param>
        /// <param name="success"></param>
        public static void LogOperationEnd(string functionName, string operation, bool success = true)
        {
            var status = success ? "SUCESSO" : "FALHA";
            LogAction(functionName, $"FIM: {operation} - {status}");
        }

        /// <summary>
        /// Logs file operations such as open, save, delete, including file name and operation status.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="operation"></param>
        /// <param name="filePath"></param>
        /// <param name="success"></param>
        public static void LogFileOperation(string functionName, string operation, string filePath, bool success = true)
        {
            var fileName = Path.GetFileName(filePath);
            var status = success ? "SUCESSO" : "FALHA";
            LogAction(functionName, $"{operation} arquivo: {fileName} - {status}");
        }

        /// <summary>
        /// Logs operations performed on UI controls, including control name, type, and additional details if provided.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="operation"></param>
        /// <param name="controlName"></param>
        /// <param name="controlType"></param>
        /// <param name="details"></param>
        public static void LogControlOperation(string functionName, string operation, string controlName, string controlType, string details = "")
        {
            var message = $"{operation} controle: {controlName} ({controlType})";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }
            LogAction(functionName, message);
        }

        /// <summary>
        /// Formats a log entry with timestamp, function name, and message.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string FormatLogEntry(string functionName, string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{functionName}] - {message}";
        }

        /// <summary>
        /// Writes a log entry to the specified file in a thread-safe manner.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="logEntry"></param>
        private static void WriteToFile(string filePath, string logEntry)
        {
            lock (_lockObject)
            {
                File.AppendAllText(filePath, logEntry + Environment.NewLine);
            }
        }

        /// <summary>
        /// Cleans up log files older than the specified number of days.
        /// </summary>
        /// <param name="daysToKeep"></param>
        public static void CleanOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var logFile in new[] { ActionsLogPath, ErrorLogPath })
                {
                    if (File.Exists(logFile))
                    {
                        var fileInfo = new FileInfo(logFile);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            File.Delete(logFile);
                            LogAction("Logger.CleanOldLogs", $"Arquivo de log antigo removido: {Path.GetFileName(logFile)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Logger.CleanOldLogs", $"Falha ao limpar logs antigos: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the name of the calling function for logging purposes.
        /// </summary>
        /// <returns></returns>
        public static string GetCallerFunctionName()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                var frame = stackTrace.GetFrame(2);
                var method = frame?.GetMethod();
                return method?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}