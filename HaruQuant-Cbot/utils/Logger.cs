using System;
using System.IO;
using System.Threading;
using cAlgo.API;

namespace cAlgo.Robots.Utils
{
    
    public class Logger
    {
        /***
        Comprehensive logging service for system-wide use across all cBot components.
        Provides both console and file logging with automatic log rotation and error handling.
        
        Notes:
            - Supports both console and file logging
            - Automatic log rotation when files reach 10MB
            - Thread-safe operations with retry logic
            - Emergency fallback mechanisms for critical failures
    ***/
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

        
        public Logger(Robot robot, string botName, string botVersion, bool enableConsoleLogging = true, bool enableFileLogging = true, string logFileName = "cbot_log.txt")
        {
            /***
            Initializes a new instance of the Logger class
            
            Args:
                robot: The cBot robot instance
                botName: Name of the bot for log file naming
                botVersion: Version of the bot for log file naming
                enableConsoleLogging: Whether to enable console logging (default: true)
                enableFileLogging: Whether to enable file logging (default: true)
                logFileName: Custom log file name (default: "cbot_log.txt")
                
            Notes:
                - Creates log directory structure automatically
                - Sets up timestamped log files
                - Initializes logging configuration
            ***/
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
   
        private void RotateLogFileIfNeeded()
        {
            /***
            Rotates the log file if it exceeds the maximum size limit
            
            Notes:
                - Checks if current log file exceeds 10MB limit
                - Performs backup rotation without recursion
                - Creates new timestamped log file
                - Handles rotation failures with emergency fallback
           ***/
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
                    _robot.Print($"Logger | RotateLogFileIfNeeded | Log file size exceeded {MaxFileSizeMB}MB, rotating log files...");

                    // Perform rotation without logging to prevent recursion/stack overflow
                    PerformLogRotation();
                    
                    // Create new log file with timestamp
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _currentLogFile = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_{timestamp}_cbot_log.txt");
                    
                    _robot.Print($"Logger | RotateLogFileIfNeeded | Log file rotation completed successfully");
                }
            }
            catch (Exception ex)
            {
                _robot.Print($"Logger | RotateLogFileIfNeeded | Error rotating log file: {ex.Message}");
                // Try to continue with a new log file even if rotation failed
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _currentLogFile = Path.Combine(_logDirectory, $"{_botName}_{_botVersion}_{timestamp}_emergency_log.txt");
                    _robot.Print($"Logger | RotateLogFileIfNeeded | Created emergency log file: {_currentLogFile}");
                }
                catch (Exception emergencyEx)
                {
                    _robot.Print($"Logger | RotateLogFileIfNeeded | Failed to create emergency log file: {emergencyEx.Message}");
                    // Disable file logging to prevent further issues
                    _currentLogFile = null;
                }
            }
        }

        private void PerformLogRotation()
        {
            /***
            Performs the actual log rotation without any logging to prevent recursion
            
            Notes:
                - Deletes oldest backup files first
                - Shifts existing backups to make room
                - Moves current log to backup_1 position
                - Handles file conflicts and errors gracefully
            ***/
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
                _robot.Print($"Logger | PerformLogRotation | Error during log rotation process: {ex.Message}");
                // Don't throw - let the caller handle creating a new log file
            }
        }

        
        private void WriteToFile(string message)
        {
            /***
            Writes a message to the log file with retry logic
            
            Args:
                message: The message to write to the file
                
            Notes:
                - Thread-safe file writing with lock
                - Retry logic for transient file access issues
                - Handles various file system exceptions
                - Emergency fallback for critical failures
            ***/

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
                            _robot.Print($"Logger | WriteToFile | Log rotation failed, continuing with current file: {rotationEx.Message}");
                        }

                        // Ensure we still have a valid log file
                        if (string.IsNullOrEmpty(_currentLogFile))
                        {
                            _robot.Print($"Logger | WriteToFile | No valid log file available, skipping file logging for this message");
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
                    _robot.Print($"Logger | WriteToFile | Access denied to log file: {accessEx.Message}");
                    break; // Don't retry on access denied
                }
                catch (DirectoryNotFoundException dirEx)
                {
                    _robot.Print($"Logger | WriteToFile | Log directory not found: {dirEx.Message}");
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
                        _robot.Print($"Logger | WriteToFile | IO Error after {MaxRetries} attempts: {ioEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _robot.Print($"Logger | WriteToFile | Unexpected error writing to log file: {ex.Message}");
                    break; // Don't retry on unexpected errors
                }
            }

            if (!success)
            {
                _robot.Print($"Logger | WriteToFile | Failed to write to log file after {MaxRetries} attempts. Message will be lost: {message.Substring(0, Math.Min(100, message.Length))}...");
            }
        }
 
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            /***
            Logs a message with the specified log level
            
            Args:
                message: The message to log
                level: The log level (default: Info)
                
            Notes:
                - Formats message with timestamp and log level
                - Outputs to both console and file if enabled
                - Thread-safe operation
            ***/

            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] - {message}";

            if (_enableConsoleLogging)
            {
                _robot.Print(formattedMessage);
            }

            WriteToFile(formattedMessage);
        }

        
        public void Info(string message)
        {
            /***
            Logs an informational message
            
            Args:
                message: The message to log
                
            Notes:
                - Convenience method for Info level logging
                - Uses standard Log() method internally
            ***/
            Log(message, LogLevel.Info);
        }

        
        public void Warning(string message)
        {
            /***
            Logs a warning message
            
            Args:
                message: The message to log
                
            Notes:
                - Convenience method for Warning level logging
                - Uses standard Log() method internally
            ***/
            Log(message, LogLevel.Warning);
        }

        
        public void Error(string message, Exception ex = null)
        {
            /***
            Logs an error message with optional exception details
            
            Args:
                message: The error message to log
                ex: Optional exception to include in the log
                
            Notes:
                - Includes full exception details if provided
                - Adds exception type, message, and stack trace
                - Uses Error level logging
            ***/

            string errorMessage = message;
            if (ex != null)
            {
                errorMessage += Environment.NewLine + $"Exception: {ex.GetType().Name} - {ex.Message}" + Environment.NewLine + $"Stack Trace: {ex.StackTrace}";
            }
            Log(errorMessage, LogLevel.Error);
        }

        
        public void Debug(string message)
        {
            /***
            Logs a debug message
            
            Args:
                message: The message to log
                
            Notes:
                - Convenience method for Debug level logging
                - Uses standard Log() method internally
            ***/
            Log(message, LogLevel.Debug);
        }

        
        public void LogTrade(string tradeAction, string symbol, double volume, double price, string additionalInfo = "")
        {
            /***
            Logs trade-related information with additional context
            
            Args:
                tradeAction: The trade action (e.g., "BUY", "SELL", "CLOSE")
                symbol: The trading symbol
                volume: The trade volume
                price: The trade price
                additionalInfo: Additional trade information
                
            Notes:
                - Formats trade information in standardized format
                - Includes all relevant trade parameters
                - Uses Info level logging
            ***/

            string tradeMessage = $"TRADE: {tradeAction} {volume} {symbol} @ {price:F5}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                tradeMessage += $" | {additionalInfo}";
            }
            Log(tradeMessage, LogLevel.Info);
        }

        
        public void LogStrategy(string strategyName, string action, string details = "")
        {
            /***
            Logs strategy-related information
            
            Args:
                strategyName: The name of the strategy
                action: The strategy action
                details: Additional strategy details
                
            Notes:
                - Formats strategy information with clear identification
                - Includes strategy name and action details
                - Uses Info level logging
            ***/
            string strategyMessage = $"STRATEGY [{strategyName}]: {action}";
            if (!string.IsNullOrEmpty(details))
            {
                strategyMessage += $" | {details}";
            }
            Log(strategyMessage, LogLevel.Info);
        }

        
        public void LogPerformance(string metric, double value, string unit = "")
        {
            /***
            Logs performance metrics
            
            Args:
                metric: The performance metric name
                value: The metric value
                unit: The unit of measurement
                
            Notes:
                - Formats performance data in standardized format
                - Includes metric name, value, and units
                - Uses Info level logging
            ***/
            string performanceMessage = $"PERFORMANCE: {metric} = {value:F4}";
            if (!string.IsNullOrEmpty(unit))
            {
                performanceMessage += $" {unit}";
            }
            Log(performanceMessage, LogLevel.Info);
        }

        
        public string GetCurrentLogFile()
        {
            /***
            Gets the current log file path
            
            Returns:
                The path to the current log file
                
            Notes:
                - Returns null if file logging is disabled
                - Useful for external log file access
            ***/
            return _currentLogFile;
        }

        
        public string GetLogDirectory()
        {
            /***
            Gets the log directory path
            
            Returns:
                The path to the log directory
                
            Notes:
                - Returns the base directory for all log files
                - Useful for log file management
            ***/
            return _logDirectory;
        }

        
        public void Flush()
        {
            /***
            Flushes any pending log writes (called on bot shutdown)
            
            Notes:
                - Ensures all log data is written to disk
                - Called during bot shutdown process
                - Logs final shutdown message
           ***/
            Log("Logger shutting down...", LogLevel.Info);
            // File writes are already flushed immediately, but this provides a clean shutdown message
        }
    }
}
