using System;
using System.IO;
using System.Threading;
using cAlgo.API;

namespace cAlgo.Robots.Utils
{
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly Robot _robot;
        private readonly bool _enableConsoleLogging;
        private readonly bool _enableFileLogging;
        private readonly string _botName;
        private readonly string _botVersion;
        private readonly object _lockObject = new object();
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 100;

        public Logger(Robot robot, string botName, string botVersion, bool enableConsoleLogging = true, bool enableFileLogging = true, string logFileName = "cbot_log.txt")
        {
            _robot = robot;
            _botName = botName;
            _botVersion = botVersion;
            _enableConsoleLogging = enableConsoleLogging;
            _enableFileLogging = enableFileLogging;

            if (_enableFileLogging)
            {
                // Revert to using the user's Documents folder as the base path
                // Path: {UserDocuments}/cAlgo/Data/cBots/{BotName}/Logs/
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string cBotDataRootPath = Path.Combine(documentsPath, "cAlgo", "Data", "cBots");
                string botSpecificDataPath = Path.Combine(cBotDataRootPath, _botName);
                string logsDirectory = Path.Combine(botSpecificDataPath, "Logs"); // Changed "logs" to "Logs" for consistency
                
                Directory.CreateDirectory(logsDirectory); // This will create all necessary parent directories
                
                _logFilePath = Path.Combine(logsDirectory, $"{_botName}_{_botVersion}_{DateTime.Now:yyyyMMdd_HHmmss}_{logFileName}");
            }
        }

        private void WriteToFile(string message)
        {
            if (!_enableFileLogging || string.IsNullOrEmpty(_logFilePath))
                return;

            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < MaxRetries)
            {
                try
                {
                    lock (_lockObject)
                    {
                        using (var fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(fileStream))
                        {
                            writer.WriteLine(message);
                            writer.Flush();
                        }
                    }
                    success = true;
                }
                catch (IOException)
                {
                    retryCount++;
                    if (retryCount < MaxRetries)
                    {
                        Thread.Sleep(RetryDelayMs);
                    }
                }
                catch (Exception ex)
                {
                    _robot.Print($"Error writing to log file: {ex.Message}");
                    break;
                }
            }

            if (!success)
            {
                _robot.Print($"Failed to write to log file after {MaxRetries} attempts. Message: {message}");
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] - {message}";

            if (_enableConsoleLogging)
            {
                _robot.Print(formattedMessage);
            }

            WriteToFile(formattedMessage);
        }

        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }

        public void Warning(string message)
        {
            Log(message, LogLevel.Warning);
        }

        public void Error(string message, Exception ex = null)
        {
            string errorMessage = message;
            if (ex != null)
            {
                errorMessage += Environment.NewLine + $"Exception: {ex.GetType().Name} - {ex.Message}" + Environment.NewLine + $"Stack Trace: {ex.StackTrace}";
            }
            Log(errorMessage, LogLevel.Error);
        }

        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }
    }
} 
