using System;

namespace cAlgo.Robots.Utils // Changed namespace
{
    /// <summary>
    /// Represents errors that occur during cBot-specific operations.
    /// This allows for more specific error handling and logging.
    /// </summary>
    [Serializable]
    public class BotErrorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BotErrorException"/> class.
        /// </summary>
        public BotErrorException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotErrorException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BotErrorException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotErrorException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference 
        /// if no inner exception is specified.</param>
        public BotErrorException(string message, Exception innerException)
            : base(message, innerException) { }

        // Future enhancements:
        // - Add custom properties like ErrorCode, Severity, etc.
        // - Example: public ErrorCode SpecificErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotErrorException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected BotErrorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
} 