using System;
using System.IO;
using System.Threading;
using cAlgo.API;

namespace cAlgo.Robots.Utils
{
    /// <summary>
    /// Comprehensive logging service for system-wide use across all cBot components.
    /// Provides both console and file logging with automatic log rotation and error handling.
    /// </summary>
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
        private const int MaxFileSizeMB = 10; // Increased from 1MB to 10MB
        private const int MaxBackupFiles = 10; // Increased backup files to handle more history
        private string _currentLogFile;

        /// <summary>
        /// Initializes a new instance of the Logger class
        /// </summary>
        /// <param name="robot">The cBot robot instance</param>
        /// <param name="botName">Name of the bot for log file naming</param>
        /// <param name="botVersion">Version of the bot for log file naming</param>
        /// <param name="enableConsoleLogging">Whether to enable console logging (default: true)</param>
        /// <param name="enableFileLogging">Whether to enable file logging (default: true)</param>
        /// <param name="logFileName">Custom log file name (default: "cbot_log.txt")</param>
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
                
                // Log initialization
                Log($"Logger initialized for {_botName} v{_botVersion}", LogLevel.Info);
                Log($"Log directory: {_logDirectory}", LogLevel.Info);
                Log($"Current log file: {_currentLogFile}", LogLevel.Info);
            }
        }

        /// <summary>
        /// Rotates the log file if it exceeds the maximum size limit
        /// </summary>
        private void RotateLogFileIfNeeded()
        {
            if (!_enableFileLogging || string.IsNullOrEmpty(_currentLogFile))
                return;

            try
            {
                var fileInfo = new FileInfo(_currentLogFile);
                if (!fileInfo.Exists)
                    return;

                // Check if current file size exceeds the limit (10MB)
                if (fileInfo.Length >= MaxFileSizeMB * 1024 * 1024)
                {
                    // Use console logging to avoid recursion during rotation
                    _robot.Print($"Log file size exceeded {MaxFileSizeMB}MB, rotating log files...");

                    // Perform rotation without logging to prevent recursion/stack overflow
                    PerformLogRotation();
                    
                    // Create new log file with timestamp
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _currentLogFile = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_{timestamp}_cbot_log.txt");
                    
                    _robot.Print("Log file rotation completed successfully");
                }
            }
            catch (Exception ex)
            {
                _robot.Print($"Error rotating log file: {ex.Message}");
                // Try to continue with a new log file even if rotation failed
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _currentLogFile = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_{timestamp}_emergency_log.txt");
                    _robot.Print($"Created emergency log file: {_currentLogFile}");
                }
                catch (Exception emergencyEx)
                {
                    _robot.Print($"Failed to create emergency log file: {emergencyEx.Message}");
                    // Disable file logging to prevent further issues
                    _currentLogFile = null;
                }
            }
        }

        /// <summary>
        /// Performs the actual log rotation without any logging to prevent recursion
        /// </summary>
        private void PerformLogRotation()
        {
            try
            {
                // Delete oldest backup files to make room (delete oldest first)
                for (int i = MaxBackupFiles; i >= MaxBackupFiles - 2; i--)
                {
                    string oldBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_{i}.txt");
                    if (File.Exists(oldBackup))
                    {
                        File.Delete(oldBackup);
                    }
                }

                // Shift existing backups (move from newest to oldest to avoid conflicts)
                for (int i = MaxBackupFiles - 3; i >= 1; i--)
                {
                    string sourceBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_{i}.txt");
                    string destBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_{i + 1}.txt");
                    if (File.Exists(sourceBackup))
                    {
                        // Ensure destination doesn't exist before moving
                        if (File.Exists(destBackup))
                        {
                            File.Delete(destBackup);
                        }
                        File.Move(sourceBackup, destBackup);
                    }
                }

                // Move current log to backup 1
                string firstBackup = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_backup_1.txt");
                if (File.Exists(firstBackup))
                {
                    File.Delete(firstBackup);
                }
                File.Move(_currentLogFile, firstBackup);
            }
            catch (Exception ex)
            {
                _robot.Print($"Error during log rotation process: {ex.Message}");
                // Don't throw - let the caller handle creating a new log file
            }
        }

        /// <summary>
        /// Writes a message to the log file with retry logic
        /// </summary>
        /// <param name="message">The message to write</param>
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
                        // Check rotation first, but handle errors gracefully
                        try
                        {
                            RotateLogFileIfNeeded();
                        }
                        catch (Exception rotationEx)
                        {
                            // If rotation fails, continue with current file
                            _robot.Print($"Log rotation failed, continuing with current file: {rotationEx.Message}");
                        }

                        // Ensure we still have a valid log file
                        if (string.IsNullOrEmpty(_currentLogFile))
                        {
                            _robot.Print("No valid log file available, skipping file logging for this message");
                            return;
                        }

                        // Write to file with proper disposal
                        using (var fileStream = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(fileStream))
                        {
                            writer.WriteLine(message);
                            writer.Flush();
                        }
                    }
                    success = true;
                }
                catch (UnauthorizedAccessException accessEx)
                {
                    _robot.Print($"Access denied to log file: {accessEx.Message}");
                    break; // Don't retry on access denied
                }
                catch (DirectoryNotFoundException dirEx)
                {
                    _robot.Print($"Log directory not found: {dirEx.Message}");
                    // Try to recreate directory
                    try
                    {
                        Directory.CreateDirectory(_logDirectory);
                        retryCount++; // Retry after recreating directory
                    }
                    catch
                    {
                        break; // Give up if can't create directory
                    }
                }
                catch (IOException ioEx)
                {
                    retryCount++;
                    if (retryCount < MaxRetries)
                    {
                        Thread.Sleep(RetryDelayMs);
                    }
                    else
                    {
                        _robot.Print($"IO Error after {MaxRetries} attempts: {ioEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _robot.Print($"Unexpected error writing to log file: {ex.Message}");
                    break; // Don't retry on unexpected errors
                }
            }

            if (!success)
            {
                _robot.Print($"Failed to write to log file after {MaxRetries} attempts. Message will be lost: {message.Substring(0, Math.Min(100, message.Length))}...");
            }
        }

        /// <summary>
        /// Logs a message with the specified log level
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The log level</param>
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] - {message}";

            if (_enableConsoleLogging)
            {
                _robot.Print(formattedMessage);
            }

            WriteToFile(formattedMessage);
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Warning(string message)
        {
            Log(message, LogLevel.Warning);
        }

        /// <summary>
        /// Logs an error message with optional exception details
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="ex">Optional exception to include in the log</param>
        public void Error(string message, Exception ex = null)
        {
            string errorMessage = message;
            if (ex != null)
            {
                errorMessage += Environment.NewLine + $"Exception: {ex.GetType().Name} - {ex.Message}" + Environment.NewLine + $"Stack Trace: {ex.StackTrace}";
            }
            Log(errorMessage, LogLevel.Error);
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        /// <summary>
        /// Logs trade-related information with additional context
        /// </summary>
        /// <param name="tradeAction">The trade action (e.g., "BUY", "SELL", "CLOSE")</param>
        /// <param name="symbol">The trading symbol</param>
        /// <param name="volume">The trade volume</param>
        /// <param name="price">The trade price</param>
        /// <param name="additionalInfo">Additional trade information</param>
        public void LogTrade(string tradeAction, string symbol, double volume, double price, string additionalInfo = "")
        {
            string tradeMessage = $"TRADE: {tradeAction} {volume} {symbol} @ {price:F5}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                tradeMessage += $" | {additionalInfo}";
            }
            Log(tradeMessage, LogLevel.Info);
        }

        /// <summary>
        /// Logs strategy-related information
        /// </summary>
        /// <param name="strategyName">The name of the strategy</param>
        /// <param name="action">The strategy action</param>
        /// <param name="details">Additional strategy details</param>
        public void LogStrategy(string strategyName, string action, string details = "")
        {
            string strategyMessage = $"STRATEGY [{strategyName}]: {action}";
            if (!string.IsNullOrEmpty(details))
            {
                strategyMessage += $" | {details}";
            }
            Log(strategyMessage, LogLevel.Info);
        }

        /// <summary>
        /// Logs performance metrics
        /// </summary>
        /// <param name="metric">The performance metric name</param>
        /// <param name="value">The metric value</param>
        /// <param name="unit">The unit of measurement</param>
        public void LogPerformance(string metric, double value, string unit = "")
        {
            string performanceMessage = $"PERFORMANCE: {metric} = {value:F4}";
            if (!string.IsNullOrEmpty(unit))
            {
                performanceMessage += $" {unit}";
            }
            Log(performanceMessage, LogLevel.Info);
        }

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        /// <returns>The path to the current log file</returns>
        public string GetCurrentLogFile()
        {
            return _currentLogFile;
        }

        /// <summary>
        /// Gets the log directory path
        /// </summary>
        /// <returns>The path to the log directory</returns>
        public string GetLogDirectory()
        {
            return _logDirectory;
        }

        /// <summary>
        /// Flushes any pending log writes (called on bot shutdown)
        /// </summary>
        public void Flush()
        {
            Log("Logger shutting down...", LogLevel.Info);
            // File writes are already flushed immediately, but this provides a clean shutdown message
        }
    }
}
