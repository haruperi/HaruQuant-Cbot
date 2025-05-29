# HaruQuant Cbot

A sophisticated algorithmic trading bot for cTrader platform, implementing multiple trading strategies with advanced risk management and performance analytics.

## Features

- Multiple trading strategies:
  - Trend Following
  - Mean Reversion
  - Breakout
  - Scalping (planned)
- Advanced risk management
- Real-time performance monitoring
- Customizable parameters
- Multi-timeframe analysis
- Comprehensive logging and debugging

## Requirements

- cTrader platform
- .NET Framework (compatible with cTrader)
- Visual Studio 2022 (recommended)

## Installation

1. Clone this repository
2. Open the solution in Visual Studio
3. Build the project
4. Import the compiled .cbot file into cTrader

## Configuration

The bot can be configured through cTrader's interface with the following main parameters:

- Trading strategy selection
- Risk management settings
- Position sizing parameters
- Technical indicator parameters
- Timeframe settings

## Usage

1. Import the bot into cTrader
2. Configure the parameters according to your trading preferences
3. Run the bot on your desired timeframe and symbol
4. Monitor performance through cTrader's interface

## Development

This project follows a modular architecture with the following main components:

- Core Bot Module
- Market Module
- Trading Module
- Strategy Module
- Analysis Module
- Optimization Module
- UI Module
- Data Module
- External Module

## Contributing

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a new Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

Trading involves risk. This bot is provided as-is without any guarantees. Always test thoroughly in a demo environment before using with real funds. 

## Error Handling Framework

This document outlines the error handling framework implemented in the HaruQuant cBot. The framework is designed to provide a centralized, consistent, and extensible way to manage exceptions throughout the bot's lifecycle.

### Core Components

1.  **`ErrorHandlerService.cs`** (Located in `HaruQuant Cbot/Utils/`)
    *   **Purpose**: This is the central class responsible for processing and logging errors.
    *   **Key Methods**:
        *   `ErrorHandlerService(Logger logger)`: Constructor that takes a `Logger` instance for logging.
        *   `void HandleError(Exception ex, string contextMessage = null, bool logAsWarning = false)`: Logs standard errors.
            *   `ex`: The exception that occurred.
            *   `contextMessage`: (Optional) A string describing the context in which the error happened (e.g., "OnBar.DataProcessing", "TradeExecution.PlaceOrder"). This is crucial for debugging.
            *   `logAsWarning`: (Optional) If `true`, logs the error as a warning. Otherwise, logs as an error.
        *   `void HandleCriticalError(Exception ex, string contextMessage = null)`: Logs critical errors that might jeopardize the bot's operation.
            *   `ex`: The critical exception.
            *   `contextMessage`: (Optional) Context of the critical error.
    *   **Functionality**:
        *   Logs exception messages and stack traces.
        *   Distinguishes between `BotErrorException` and other system exceptions for tailored logging if needed (currently logs them similarly but marks `BotErrorException`).
        *   Provides placeholders for future enhancements like notifications or automated recovery actions.

2.  **`BotErrorException.cs`** (Located in `HaruQuant Cbot/Utils/`)
    *   **Purpose**: A custom exception class derived from `System.Exception`. It is used to represent errors that are specific to the cBot's operational logic or business rules.
    *   **Usage**: Throw this exception when an error condition specific to your bot's strategy or internal workings occurs. This allows for more specific `catch` blocks.
    *   **Constructors**:
        *   `BotErrorException()`
        *   `BotErrorException(string message)`
        *   `BotErrorException(string message, Exception innerException)`
    *   **Extensibility**: Can be extended with additional properties (e.g., `ErrorCode`, `Severity`) if more detailed error information is required programmatically.

### Integration into `CoreBot.cs`

*   An instance of `ErrorHandlerService` is created in the `OnStart()` method of `CoreBot.cs` and is available via the private `_errorHandler` field.
    ```csharp
    // In CoreBot.cs
    private ErrorHandlerService _errorHandler;
    // ...
    protected override void OnStart()
    {
        _logger = new Logger(this, BotConfig.BotName, BotConfig.BotVersion);
        _errorHandler = new ErrorHandlerService(_logger); 
        // ...
    }
    ```
*   The necessary `using cAlgo.Robots.Utils;` statement is included in `CoreBot.cs` to access these utility classes.

### How to Use the Framework

1.  **Identify Critical Code Blocks**: Wrap any operations that have a potential to fail in `try-catch` blocks. This includes:
    *   Trade execution calls (`ExecuteMarketOrder`, `CreateLimitOrder`, etc.).
    *   Accessing external resources or APIs (though less common directly in cBots unless through platform features).
    *   Complex calculations or data manipulations that might encounter unexpected states.
    *   File I/O (like the state saving/loading mechanism).

2.  **Implement `try-catch` Blocks**:
    ```csharp
    try
    {
        // Code that might throw an exception
        // Example: var result = Positions.Find("someLabel");
        // if (result == null) 
        // {
        //     throw new BotErrorException("Expected position with label 'someLabel' not found.");
        // }
        // PerformSomeRiskyOperation();
    }
    catch (BotErrorException botEx) // Catch your custom cBot exceptions first
    {
        _errorHandler.HandleError(botEx, "MyMethod.BotSpecificLogic");
        // Optionally, take specific actions based on botEx
    }
    catch (InvalidOperationException ioe) // Catch more specific system exceptions
    {
        _errorHandler.HandleError(ioe, "MyMethod.InvalidOperation", logAsWarning: true);
    }
    catch (ArgumentNullException argNullEx) // Example of handling critical input errors
    {
        _errorHandler.HandleCriticalError(argNullEx, "MyMethod.CriticalInputValidation");
        // Consider if the bot can continue or if it needs to stop or enter a safe mode
    }
    catch (Exception ex) // Catch-all for any other unexpected exceptions
    {
        _errorHandler.HandleError(ex, "MyMethod.GeneralUnexpected");
    }
    ```

3.  **Throw `BotErrorException` for Bot-Specific Issues**:
    When your bot's internal logic detects an error state that isn't a system exception, throw a `BotErrorException`.
    ```csharp
    public void ProcessSignal(Signal signal)
    {
        if (signal == null)
        {
            throw new ArgumentNullException(nameof(signal), "Signal cannot be null.");
        }
        if (!IsValidSignal(signal))
        {
            throw new BotErrorException($"Invalid signal received: {signal.Type}", new InvalidDataException("Signal data validation failed."));
        }
        // ... process valid signal
    }
    ```

4.  **Provide Context**: Always provide a meaningful `contextMessage` to `HandleError` and `HandleCriticalError`. This message should help quickly identify the location and nature of the problem from the logs.

### Best Practices

*   **Be Specific in Catching**: Catch the most specific exceptions first, then more general ones. Avoid catching just `System.Exception` unless it's the last resort.
*   **Don't Swallow Exceptions**: If you catch an exception but can't handle it properly, either re-throw it or log it using the `ErrorHandlerService`. Avoid empty `catch` blocks.
*   **Use `BotErrorException` Appropriately**: Use it for errors related to your trading logic, strategy rules, or custom operations, not for general programming errors like `NullReferenceException` (unless you're wrapping it to add more context).
*   **Log Sufficient Detail**: Ensure the `Logger` (used by `ErrorHandlerService`) is configured to log enough detail (e.g., timestamps, exception type, message, stack trace).
*   **Test Error Paths**: Intentionally introduce errors during development and testing to ensure your error handling works as expected.
*   **Iterate and Improve**: As the bot evolves, review and refine your error handling strategy. Add more specific error types or handling logic as needed.

### Future Enhancements (Considerations)

*   **Notification System**: Extend `ErrorHandlerService` to send notifications (e.g., email, Telegram) for critical errors.
*   **Automated Recovery**: Implement mechanisms for certain errors to trigger recovery actions (e.g., retrying an operation, closing all positions, stopping the bot).
*   **Error Codes**: Add an `ErrorCode` enum to `BotErrorException` to allow for programmatic decision-making based on specific error types.
*   **Global Exception Handler (If Applicable)**: While cBots run within the cTrader platform, if you were building a standalone .NET application, you might use `AppDomain.CurrentDomain.UnhandledException` for unhandled exceptions. In cTrader, robust `try-catch` within event handlers (`OnTick`, `OnBar`, `OnStart`, `OnStop`) is key.

By following this framework, you can build a more robust and maintainable cBot that handles unexpected situations gracefully. 