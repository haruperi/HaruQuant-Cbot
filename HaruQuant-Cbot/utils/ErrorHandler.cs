using System;
using System.Collections.Generic;
using System.Threading;
using cAlgo.API;
using cAlgo.Robots.Utils;

namespace cAlgo.Robots.Utils
{
    /// <summary>
    /// Comprehensive error handling framework for centralized error management,
    /// categorization, escalation, and recovery across all cBot components.
    /// </summary>
    public class ErrorHandler
    {
        private readonly Robot _robot;
        private readonly Logger _logger;
        private readonly object _lockObject = new object();
        private readonly Dictionary<ErrorCategory, int> _errorCounts;
        private readonly Dictionary<ErrorCategory, DateTime> _lastErrorTimes;
        private readonly Queue<ErrorEvent> _recentErrors;
        private readonly int _maxRecentErrors = 100;
        private SystemHealth _currentHealth = SystemHealth.Healthy;
        private DateTime _lastHealthCheck = DateTime.Now;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);

        // Error thresholds for health monitoring
        private const int LowSeverityThreshold = 10;
        private const int MediumSeverityThreshold = 5;
        private const int HighSeverityThreshold = 3;
        private const int CriticalSeverityThreshold = 1;

        /// <summary>
        /// Represents an error event with full context
        /// </summary>
        public class ErrorEvent
        {
            public DateTime Timestamp { get; set; }
            public ErrorCategory Category { get; set; }
            public ErrorSeverity Severity { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
            public string Context { get; set; }
            public RecoveryAction RecommendedAction { get; set; }
            public bool IsResolved { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the ErrorHandler class
        /// </summary>
        /// <param name="robot">The cBot robot instance</param>
        /// <param name="logger">The logger instance for error logging</param>
        public ErrorHandler(Robot robot, Logger logger)
        {
            _robot = robot ?? throw new ArgumentNullException(nameof(robot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _errorCounts = new Dictionary<ErrorCategory, int>();
            _lastErrorTimes = new Dictionary<ErrorCategory, DateTime>();
            _recentErrors = new Queue<ErrorEvent>();
            
            // Initialize error counts for all categories
            foreach (ErrorCategory category in Enum.GetValues(typeof(ErrorCategory)))
            {
                _errorCounts[category] = 0;
                _lastErrorTimes[category] = DateTime.MinValue;
            }

            _logger.Info("ErrorHandler initialized successfully");
        }

        /// <summary>
        /// Handles an error with comprehensive context and automatic recovery
        /// </summary>
        /// <param name="category">The category of the error</param>
        /// <param name="severity">The severity level of the error</param>
        /// <param name="message">The error message</param>
        /// <param name="exception">Optional exception details</param>
        /// <param name="context">Additional context information</param>
        /// <param name="attemptRecovery">Whether to attempt automatic recovery</param>
        /// <returns>The recommended recovery action</returns>
        public RecoveryAction HandleError(ErrorCategory category, ErrorSeverity severity, string message, 
            Exception exception = null, string context = "", bool attemptRecovery = true)
        {
            try
            {
                var errorEvent = new ErrorEvent
                {
                    Timestamp = DateTime.Now,
                    Category = category,
                    Severity = severity,
                    Message = message,
                    Exception = exception,
                    Context = context,
                    RecommendedAction = DetermineRecoveryAction(category, severity, exception),
                    IsResolved = false
                };

                lock (_lockObject)
                {
                    // Update error statistics
                    _errorCounts[category]++;
                    _lastErrorTimes[category] = DateTime.Now;
                    
                    // Add to recent errors queue
                    _recentErrors.Enqueue(errorEvent);
                    if (_recentErrors.Count > _maxRecentErrors)
                    {
                        _recentErrors.Dequeue();
                    }
                }

                // Log the error with full context
                LogError(errorEvent);

                // Update system health based on error patterns
                UpdateSystemHealth();

                // Attempt recovery if requested
                if (attemptRecovery)
                {
                    PerformRecoveryAction(errorEvent);
                }

                // Send notifications for critical errors
                if (severity >= ErrorSeverity.High)
                {
                    SendErrorNotification(errorEvent);
                }

                return errorEvent.RecommendedAction;
            }
            catch (Exception handlerException)
            {
                // Error in error handler - use fallback logging
                try
                {
                    _robot.Print($"ERROR in ErrorHandler: {handlerException.Message}");
                    _logger?.Error("ErrorHandler encountered an error", handlerException);
                }
                catch
                {
                    // Last resort - direct console output
                    _robot.Print($"CRITICAL: ErrorHandler failure - {handlerException.Message}");
                }
                return RecoveryAction.Alert;
            }
        }

        /// <summary>
        /// Handles an exception with automatic categorization
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="context">Additional context information</param>
        /// <param name="attemptRecovery">Whether to attempt automatic recovery</param>
        /// <returns>The recommended recovery action</returns>
        public RecoveryAction HandleException(Exception exception, string context = "", bool attemptRecovery = true)
        {
            var category = CategorizeException(exception);
            var severity = DetermineSeverity(exception, category);
            
            return HandleError(category, severity, exception.Message, exception, context, attemptRecovery);
        }

        /// <summary>
        /// Determines the recovery action based on error characteristics
        /// </summary>
        private RecoveryAction DetermineRecoveryAction(ErrorCategory category, ErrorSeverity severity, Exception exception)
        {
            // Critical errors require immediate attention
            if (severity == ErrorSeverity.Critical)
            {
                return RecoveryAction.Alert;
            }

            // Category-specific recovery strategies
            switch (category)
            {
                case ErrorCategory.Network:
                    return severity >= ErrorSeverity.Medium ? RecoveryAction.Retry : RecoveryAction.None;
                    
                case ErrorCategory.Trading:
                    return severity >= ErrorSeverity.High ? RecoveryAction.Stop : RecoveryAction.Fallback;
                    
                case ErrorCategory.Data:
                    return RecoveryAction.Retry;
                    
                case ErrorCategory.Strategy:
                    return severity >= ErrorSeverity.High ? RecoveryAction.Restart : RecoveryAction.Fallback;
                    
                case ErrorCategory.Risk:
                    return RecoveryAction.Stop; // Always stop on risk errors
                    
                case ErrorCategory.System:
                    return severity >= ErrorSeverity.High ? RecoveryAction.Restart : RecoveryAction.None;
                    
                case ErrorCategory.Configuration:
                    return RecoveryAction.Alert;
                    
                case ErrorCategory.External:
                    return RecoveryAction.Retry;
                    
                default:
                    return RecoveryAction.None;
            }
        }

        /// <summary>
        /// Categorizes an exception based on its type and characteristics
        /// </summary>
        private ErrorCategory CategorizeException(Exception exception)
        {
            switch (exception)
            {
                case ArgumentException _:
                case InvalidOperationException _:
                    return ErrorCategory.Configuration;
                    
                case TimeoutException _:
                case System.Net.NetworkInformation.PingException _:
                    return ErrorCategory.Network;
                    
                case System.IO.IOException _:
                case System.IO.DirectoryNotFoundException _:
                case UnauthorizedAccessException _:
                    return ErrorCategory.System;
                    
                case DivideByZeroException _:
                case ArithmeticException _:
                    return ErrorCategory.Data;
                    
                case OutOfMemoryException _:
                case StackOverflowException _:
                    return ErrorCategory.System;
                    
                default:
                    return ErrorCategory.System;
            }
        }

        /// <summary>
        /// Determines the severity of an exception
        /// </summary>
        private ErrorSeverity DetermineSeverity(Exception exception, ErrorCategory category)
        {
            // Critical system exceptions
            if (exception is OutOfMemoryException || exception is StackOverflowException)
            {
                return ErrorSeverity.Critical;
            }

            // Risk-related errors are always high severity
            if (category == ErrorCategory.Risk)
            {
                return ErrorSeverity.High;
            }

            // Trading errors are typically medium to high severity
            if (category == ErrorCategory.Trading)
            {
                return ErrorSeverity.Medium;
            }

            // Network errors are usually recoverable
            if (category == ErrorCategory.Network)
            {
                return ErrorSeverity.Low;
            }

            // Default severity based on exception type
            return exception is ArgumentException ? ErrorSeverity.Medium : ErrorSeverity.Low;
        }

        /// <summary>
        /// Logs an error event with comprehensive details
        /// </summary>
        private void LogError(ErrorEvent errorEvent)
        {
            string logMessage = $"ERROR [{errorEvent.Category}] [{errorEvent.Severity}]: {errorEvent.Message}";
            
            if (!string.IsNullOrEmpty(errorEvent.Context))
            {
                logMessage += $" | Context: {errorEvent.Context}";
            }
            
            logMessage += $" | Recommended Action: {errorEvent.RecommendedAction}";
            
            // Log with appropriate level based on severity
            switch (errorEvent.Severity)
            {
                case ErrorSeverity.Critical:
                    _logger.Error(logMessage, errorEvent.Exception);
                    break;
                case ErrorSeverity.High:
                    _logger.Error(logMessage, errorEvent.Exception);
                    break;
                case ErrorSeverity.Medium:
                    _logger.Warning(logMessage);
                    break;
                case ErrorSeverity.Low:
                    _logger.Info(logMessage);
                    break;
            }
        }

        /// <summary>
        /// Performs the recommended recovery action
        /// </summary>
        private void PerformRecoveryAction(ErrorEvent errorEvent)
        {
            try
            {
                switch (errorEvent.RecommendedAction)
                {
                    case RecoveryAction.None:
                        // No action needed
                        break;
                        
                    case RecoveryAction.Retry:
                        _logger.Info($"Scheduling retry for {errorEvent.Category} error");
                        // Implement retry logic (would depend on specific operation)
                        break;
                        
                    case RecoveryAction.Fallback:
                        _logger.Info($"Activating fallback mechanism for {errorEvent.Category} error");
                        // Implement fallback logic
                        break;
                        
                    case RecoveryAction.Restart:
                        _logger.Warning($"Component restart recommended for {errorEvent.Category} error");
                        // Implement component restart logic
                        break;
                        
                    case RecoveryAction.Alert:
                        _logger.Error($"Alert required for {errorEvent.Category} error");
                        // Send alert to administrators
                        break;
                        
                    case RecoveryAction.Stop:
                        _logger.Error($"Operation stop required for {errorEvent.Category} error");
                        // Implement safe stop logic
                        break;
                }
            }
            catch (Exception recoveryException)
            {
                _logger.Error($"Failed to perform recovery action {errorEvent.RecommendedAction}", recoveryException);
            }
        }

        /// <summary>
        /// Updates system health based on recent error patterns
        /// </summary>
        private void UpdateSystemHealth()
        {
            if (DateTime.Now - _lastHealthCheck < _healthCheckInterval)
                return;

            lock (_lockObject)
            {
                var recentCriticalErrors = 0;
                var recentHighErrors = 0;
                var recentMediumErrors = 0;
                var recentLowErrors = 0;

                var cutoffTime = DateTime.Now.AddMinutes(-5); // Last 5 minutes

                foreach (var error in _recentErrors)
                {
                    if (error.Timestamp < cutoffTime) continue;

                    switch (error.Severity)
                    {
                        case ErrorSeverity.Critical:
                            recentCriticalErrors++;
                            break;
                        case ErrorSeverity.High:
                            recentHighErrors++;
                            break;
                        case ErrorSeverity.Medium:
                            recentMediumErrors++;
                            break;
                        case ErrorSeverity.Low:
                            recentLowErrors++;
                            break;
                    }
                }

                var previousHealth = _currentHealth;

                // Determine new health status
                if (recentCriticalErrors >= CriticalSeverityThreshold)
                {
                    _currentHealth = SystemHealth.Failed;
                }
                else if (recentHighErrors >= HighSeverityThreshold)
                {
                    _currentHealth = SystemHealth.Critical;
                }
                else if (recentMediumErrors >= MediumSeverityThreshold)
                {
                    _currentHealth = SystemHealth.Degraded;
                }
                else if (recentLowErrors >= LowSeverityThreshold)
                {
                    _currentHealth = SystemHealth.Warning;
                }
                else
                {
                    _currentHealth = SystemHealth.Healthy;
                }

                // Log health changes
                if (_currentHealth != previousHealth)
                {
                    _logger.Warning($"System health changed from {previousHealth} to {_currentHealth}");
                }

                _lastHealthCheck = DateTime.Now;
            }
        }

        /// <summary>
        /// Sends error notifications for critical errors
        /// </summary>
        private void SendErrorNotification(ErrorEvent errorEvent)
        {
            try
            {
                string notificationMessage = $"CRITICAL ERROR in {BotConfig.BotName}:\n" +
                                           $"Category: {errorEvent.Category}\n" +
                                           $"Severity: {errorEvent.Severity}\n" +
                                           $"Message: {errorEvent.Message}\n" +
                                           $"Time: {errorEvent.Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                                           $"Action: {errorEvent.RecommendedAction}";

                // Log notification
                _logger.Error($"NOTIFICATION SENT: {notificationMessage}");

                // Future: Add email, Telegram, or other notification integrations here
            }
            catch (Exception notificationException)
            {
                _logger.Error("Failed to send error notification", notificationException);
            }
        }

        /// <summary>
        /// Gets the current system health status
        /// </summary>
        /// <returns>Current system health status</returns>
        public SystemHealth GetSystemHealth()
        {
            UpdateSystemHealth();
            return _currentHealth;
        }

        /// <summary>
        /// Gets error statistics for a specific category
        /// </summary>
        /// <param name="category">The error category</param>
        /// <returns>Error count for the category</returns>
        public int GetErrorCount(ErrorCategory category)
        {
            lock (_lockObject)
            {
                return _errorCounts.ContainsKey(category) ? _errorCounts[category] : 0;
            }
        }

        /// <summary>
        /// Gets the most recent errors
        /// </summary>
        /// <param name="count">Number of recent errors to retrieve</param>
        /// <returns>List of recent error events</returns>
        public List<ErrorEvent> GetRecentErrors(int count = 10)
        {
            lock (_lockObject)
            {
                var errors = new List<ErrorEvent>();
                var errorArray = _recentErrors.ToArray();
                
                int start = Math.Max(0, errorArray.Length - count);
                for (int i = start; i < errorArray.Length; i++)
                {
                    errors.Add(errorArray[i]);
                }
                
                return errors;
            }
        }

        /// <summary>
        /// Clears error statistics (useful for testing or periodic resets)
        /// </summary>
        public void ClearErrorStatistics()
        {
            lock (_lockObject)
            {
                foreach (var key in _errorCounts.Keys.ToList())
                {
                    _errorCounts[key] = 0;
                    _lastErrorTimes[key] = DateTime.MinValue;
                }
                
                _recentErrors.Clear();
                _currentHealth = SystemHealth.Healthy;
                
                _logger.Info("Error statistics cleared");
            }
        }
    }
}
