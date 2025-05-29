using System;
// For Logger - Assuming Logger class is also in cAlgo.Robots.Utils or its own namespace is referenced correctly.
// If Logger is in cAlgo.Robots.Utils, this using statement is fine.
// If BotErrorException is now in cAlgo.Robots.Utils, it's implicitly available if ErrorHandlerService is also in that namespace.
// No explicit using for BotErrorException needed if they are in the same namespace.

namespace cAlgo.Robots.Utils // Changed namespace
{
    /// <summary>
    /// Provides centralized error handling and logging services for the cBot.
    /// </summary>
    public class ErrorHandlerService
    {
        private readonly Logger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlerService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording errors.</param>
        public ErrorHandlerService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles an exception by logging it with a specified context message.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <param name="contextMessage">An optional message providing context about where the error occurred.</param>
        /// <param name="logAsWarning">If true, logs the error as a warning; otherwise, logs as an error. Default is false.</param>
        public void HandleError(Exception ex, string contextMessage = null, bool logAsWarning = false)
        {
            if (ex == null)
            {
                _logger.Warning("HandleError called with a null exception.");
                return;
            }

            string fullMessage = string.IsNullOrEmpty(contextMessage)
                ? $"An error occurred: {ex.Message}"
                : $"Error in {contextMessage}: {ex.Message}";

            if (ex is BotErrorException botEx) // BotErrorException is in the same cAlgo.Robots.Utils namespace
            {
                // Log BotErrorException with potentially more details if added later
                if (logAsWarning)
                {
                    _logger.Warning($"{fullMessage} (BotErrorException). StackTrace: {ex.StackTrace}");
                }
                else
                {
                    _logger.Error($"{fullMessage} (BotErrorException). StackTrace: {ex.StackTrace}", ex);
                }
            }
            else
            {
                // Log standard exceptions
                if (logAsWarning)
                {
                    _logger.Warning($"{fullMessage} StackTrace: {ex.StackTrace}");
                }
                else
                {
                    _logger.Error($"{fullMessage} StackTrace: {ex.StackTrace}", ex);
                }
            }

            // Future enhancements:
            // - Send notifications (email, Telegram)
            // - Trigger specific recovery mechanisms
            // - Conditionally stop the bot for critical errors
        }

        /// <summary>
        /// Handles a critical exception, logs it, and could potentially trigger more severe actions.
        /// For now, it logs as a critical error.
        /// </summary>
        /// <param name="ex">The critical exception to handle.</param>
        /// <param name="contextMessage">An optional message providing context about where the error occurred.</param>
        public void HandleCriticalError(Exception ex, string contextMessage = null)
        {
            string fullMessage = string.IsNullOrEmpty(contextMessage)
                ? $"A critical error occurred: {ex.Message}"
                : $"Critical error in {contextMessage}: {ex.Message}";
            
            _logger.Error($"CRITICAL: {fullMessage}. StackTrace: {ex.StackTrace}", ex);
            
            // Consider stopping the bot or parts of it, or alerting immediately.
            // Example: robot.Stop(); (if a Robot instance is available)
            // This would require passing the Robot instance to the ErrorHandlerService or having a way to signal it.
        }
    }
} 