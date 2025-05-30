using System;
using System.IO;
using System.Threading;
using cAlgo.API;

namespace cAlgo.Robots.Utils
{
    public class Logger
    {
        private readonly string _logDirectory;
        private readonly Robot _robot;
        private readonly bool _enableConsoleLogging;
        private readonly bool _enableFileLogging;
        private readonly string _botName;
        private readonly string _botVersion;
        private readonly object _lockObject = new object();
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 100;
        private const int MaxFileSizeMB = 1;
        private const int MaxBackupFiles = 5;
        private string _currentLogFile;

        public Logger(Robot robot, string botName, string botVersion, bool enableConsoleLogging = true, bool enableFileLogging = true, string logFileName = "cbot_log.txt")
        {
            _robot = robot;
            _botName = botName;
            _botVersion = botVersion;
            _enableConsoleLogging = enableConsoleLogging;
            _enableFileLogging = enableFileLogging;

            if (_enableFileLogging)
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string cBotDataRootPath = Path.Combine(documentsPath, "cAlgo", "Data", "cBots");
                string botSpecificDataPath = Path.Combine(cBotDataRootPath, _botName);
                _logDirectory = Path.Combine(botSpecificDataPath, "Logs");
                
                Directory.CreateDirectory(_logDirectory);
                
                _currentLogFile = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_{DateTime.Now:yyyyMMdd_HHmmss}_{logFileName}");
            }
        }

        private void RotateLogFileIfNeeded()
        {
            if (!_enableFileLogging || string.IsNullOrEmpty(_currentLogFile))
                return;

            try
            {
                var fileInfo = new FileInfo(_currentLogFile);
                if (!fileInfo.Exists)
                    return;

                // Check if current file size exceeds the limit (1MB)
                if (fileInfo.Length >= MaxFileSizeMB * 1024 * 1024)
                {
                    // Delete oldest backup if we have reached the maximum number of backups
                    string oldestBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_{MaxBackupFiles}.txt");
                    if (File.Exists(oldestBackup))
                    {
                        File.Delete(oldestBackup);
                    }

                    // Shift existing backups
                    for (int i = MaxBackupFiles - 1; i >= 1; i--)
                    {
                        string oldBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_{i}.txt");
                        string newBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_{i + 1}.txt");
                        if (File.Exists(oldBackup))
                        {
                            File.Move(oldBackup, newBackup);
                        }
                    }

                    // Move current log to backup 1
                    string firstBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_1.txt");
                    File.Move(_currentLogFile, firstBackup);

                    // Create new log file
                    _currentLogFile = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_{DateTime.Now:yyyyMMdd_HHmmss}_cbot_log.txt");
                }
            }
            catch (Exception ex)
            {
                _robot.Print($"Error rotating log file: {ex.Message}");
            }
        }

        private void WriteToFile(string message)
        {
            if (!_enableFileLogging || string.IsNullOrEmpty(_currentLogFile))
                return;

            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < MaxRetries)
            {
                try
                {
                    lock (_lockObject)
                    {
                        RotateLogFileIfNeeded();

                        using (var fileStream = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read))
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
