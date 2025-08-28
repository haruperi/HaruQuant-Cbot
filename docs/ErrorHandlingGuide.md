# HaruQuant CoreBot - Error Handling Framework Guide

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Core Components](#core-components)
4. [Error Categories & Severity](#error-categories--severity)
5. [Recovery Actions](#recovery-actions)
6. [System Health Monitoring](#system-health-monitoring)
7. [Integration Guide](#integration-guide)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)
10. [API Reference](#api-reference)

## Overview

The HaruQuant CoreBot Error Handling Framework provides enterprise-grade error management, automatic recovery, and system health monitoring for production trading environments. It ensures maximum uptime, protects trading capital, and provides comprehensive logging for all system events.

### Key Features
- **Centralized Error Management**: All errors processed through unified system
- **Automatic Categorization**: Intelligent error classification based on exception types
- **Smart Recovery**: Context-aware recovery actions with fallback mechanisms
- **Health Monitoring**: Real-time system component health tracking
- **Graceful Degradation**: Continued operation with reduced functionality during issues
- **Thread Safety**: Production-ready concurrent operation support

### Benefits
- **Maximum Uptime**: Automatic recovery from common failures
- **Capital Protection**: Risk-aware error handling for trading operations
- **Operational Visibility**: Comprehensive logging and monitoring
- **Maintenance Efficiency**: Clear error categorization and recovery guidance
- **Scalability**: Designed for high-volume trading environments

## Architecture

The error handling framework consists of three main components working together:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   ErrorHandler  │    │  CrashRecovery  │    │     Logger      │
│                 │    │                 │    │                 │
│ • Categorization│◄──►│ • Health Monitor│◄──►│ • File Logging  │
│ • Severity      │    │ • Auto Recovery │    │ • Console Log   │
│ • Recovery Plan │    │ • Degradation   │    │ • Rotation      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         ▲                       ▲                       ▲
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 ▼
                        ┌─────────────────┐
                        │    CoreBot      │
                        │                 │
                        │ • Integration   │
                        │ • Lifecycle     │
                        │ • Coordination  │
                        └─────────────────┘
```

## Core Components

### 1. ErrorHandler.cs
Central error processing engine that handles error categorization, severity assessment, and recovery planning.

#### Key Responsibilities:
- **Error Processing**: Receive and analyze all system errors
- **Categorization**: Classify errors by type and impact
- **Severity Assessment**: Determine urgency and impact level
- **Recovery Planning**: Recommend appropriate recovery actions
- **Statistics Tracking**: Monitor error patterns and frequencies
- **Health Assessment**: Evaluate overall system health

#### Usage Example:
```csharp
// Handle an exception with context
var recoveryAction = _errorHandler.HandleException(
    exception: ex,
    context: "Trade execution during OnBar",
    attemptRecovery: true
);

// Handle a custom error
_errorHandler.HandleError(
    category: ErrorCategory.Trading,
    severity: ErrorSeverity.High,
    message: "Position size exceeds risk limits",
    context: "Risk validation failed",
    attemptRecovery: true
);
```

### 2. CrashRecovery.cs
System health monitoring and automatic recovery orchestration.

#### Key Responsibilities:
- **Component Monitoring**: Track health of all system components
- **Health Checks**: Periodic validation of system functionality
- **Recovery Execution**: Perform automated recovery actions
- **Degraded Mode**: Manage reduced functionality during issues
- **Emergency Procedures**: Handle critical system failures

#### Monitored Components:
- **Logger**: Logging system functionality
- **ErrorHandler**: Error processing capability
- **TradingEngine**: Order execution and position management
- **RiskManager**: Risk calculation and validation
- **DataProcessor**: Market data processing
- **NetworkConnection**: Broker connectivity
- **StrategyEngine**: Trading strategy execution

#### Usage Example:
```csharp
// Check if system is in recovery mode
if (_crashRecovery.IsInRecoveryMode())
{
    _logger.Debug("Limited functionality - system recovering");
    return;
}

// Get component health
var componentHealth = _crashRecovery.GetComponentHealth("TradingEngine");
if (componentHealth.Status >= SystemHealth.Critical)
{
    _logger.Warning("Trading engine unhealthy - suspending operations");
}
```

### 3. Enhanced Constants.cs
Defines all error management enumerations and configuration constants.

#### Error Management Enums:
```csharp
public enum ErrorSeverity
{
    Low = 0,        // Minor issues, continue operation
    Medium = 1,     // Moderate issues, log and monitor
    High = 2,       // Serious issues, may require intervention
    Critical = 3    // System-threatening, immediate action required
}

public enum ErrorCategory
{
    System = 0,         // System-level errors
    Trading = 1,        // Trading operation errors
    Network = 2,        // Network/connectivity errors
    Data = 3,           // Data processing errors
    Strategy = 4,       // Strategy execution errors
    Risk = 5,           // Risk management errors
    Configuration = 6,  // Configuration/parameter errors
    External = 7        // External service errors
}

public enum RecoveryAction
{
    None = 0,           // No recovery action needed
    Retry = 1,          // Retry the operation
    Fallback = 2,       // Use fallback mechanism
    Restart = 3,        // Restart component
    Alert = 4,          // Alert user/admin
    Stop = 5            // Stop operation
}

public enum SystemHealth
{
    Healthy = 0,        // All systems operating normally
    Warning = 1,        // Minor issues detected
    Degraded = 2,       // Performance impacted
    Critical = 3,       // Major issues, limited functionality
    Failed = 4          // System failure, requires intervention
}
```

## Error Categories & Severity

### Error Categories

#### System Errors
- **Description**: Infrastructure and platform-level issues
- **Examples**: OutOfMemoryException, StackOverflowException, IOException
- **Recovery**: Restart components, alert administrators
- **Impact**: Can affect entire system operation

#### Trading Errors  
- **Description**: Order execution and position management issues
- **Examples**: Insufficient margin, invalid order parameters, execution failures
- **Recovery**: Stop trading, use fallback mechanisms
- **Impact**: Direct impact on trading performance and capital

#### Network Errors
- **Description**: Connectivity and communication issues
- **Examples**: TimeoutException, connection drops, API failures
- **Recovery**: Retry with backoff, check connectivity
- **Impact**: Data delays, execution issues

#### Data Errors
- **Description**: Market data processing and validation issues
- **Examples**: Invalid prices, missing data, calculation errors
- **Recovery**: Retry data retrieval, use cached data
- **Impact**: Strategy decisions based on bad data

#### Strategy Errors
- **Description**: Trading strategy execution problems
- **Examples**: Logic errors, indicator failures, signal conflicts
- **Recovery**: Restart strategy, use fallback logic
- **Impact**: Missed opportunities, incorrect signals

#### Risk Errors
- **Description**: Risk management and validation failures
- **Examples**: Position size violations, margin breaches, limit exceeded
- **Recovery**: Stop trading immediately
- **Impact**: Capital protection critical

#### Configuration Errors
- **Description**: Parameter and setup issues
- **Examples**: Invalid parameters, missing settings, permission issues
- **Recovery**: Alert for manual correction
- **Impact**: System may not function as intended

#### External Errors
- **Description**: Third-party service and integration issues
- **Examples**: API rate limits, service downtime, authentication failures
- **Recovery**: Retry with delays, use alternatives
- **Impact**: Reduced functionality from external dependencies

### Severity Levels

#### Critical (Level 3)
- **Definition**: System-threatening errors requiring immediate action
- **Examples**: OutOfMemoryException, trading engine failure, risk breaches
- **Response**: Immediate alerts, emergency procedures, possible shutdown
- **Threshold**: 1+ error triggers Emergency Mode

#### High (Level 2)
- **Definition**: Serious issues that may require intervention
- **Examples**: Trading execution failures, strategy engine crashes
- **Response**: Stop affected operations, alert administrators
- **Threshold**: 3+ errors trigger Critical Health status

#### Medium (Level 1)
- **Definition**: Moderate issues requiring monitoring
- **Examples**: Network timeouts, data validation failures
- **Response**: Log warnings, implement fallbacks, monitor closely
- **Threshold**: 5+ errors trigger Degraded Health status

#### Low (Level 0)
- **Definition**: Minor issues that don't affect core functionality
- **Examples**: Minor validation errors, recoverable timeouts
- **Response**: Log for analysis, continue normal operation
- **Threshold**: 10+ errors trigger Warning Health status

## Recovery Actions

### Automatic Recovery Actions

#### None (0)
- **When Used**: Low severity errors that don't require intervention
- **Action**: Continue normal operation, log for analysis
- **Examples**: Minor validation warnings, informational errors

#### Retry (1)
- **When Used**: Transient errors likely to resolve on retry
- **Action**: Attempt operation again with exponential backoff
- **Examples**: Network timeouts, temporary API failures
- **Implementation**: 3 retries with 100ms delays

#### Fallback (2)
- **When Used**: Primary mechanism fails but alternatives exist
- **Action**: Switch to backup method or cached data
- **Examples**: Use cached data when live feed fails, alternative execution path

#### Restart (3)
- **When Used**: Component failure requiring reinitialization
- **Action**: Reinitialize failed component
- **Examples**: Strategy engine restart, indicator recalculation

#### Alert (4)
- **When Used**: Issues requiring human intervention
- **Action**: Send notifications to administrators
- **Examples**: Configuration errors, authorization failures

#### Stop (5)
- **When Used**: Dangerous conditions requiring immediate cessation
- **Action**: Halt operations to prevent damage
- **Examples**: Risk limit breaches, critical system failures

### Recovery Strategy Matrix

| Category | Low Severity | Medium Severity | High Severity | Critical Severity |
|----------|-------------|----------------|---------------|------------------|
| **System** | None | None | Restart | Alert |
| **Trading** | None | Fallback | Stop | Stop |
| **Network** | None | Retry | Retry | Alert |
| **Data** | None | Retry | Retry | Restart |
| **Strategy** | None | Fallback | Restart | Alert |
| **Risk** | None | Stop | Stop | Stop |
| **Configuration** | None | Alert | Alert | Alert |
| **External** | None | Retry | Retry | Alert |

## System Health Monitoring

### Health States

#### Healthy (0)
- **Description**: All systems operating normally
- **Indicators**: Error rates within normal thresholds
- **Action**: Normal operation, routine monitoring

#### Warning (1)
- **Description**: Minor issues detected but functionality maintained
- **Indicators**: Elevated low-severity error rates
- **Action**: Increased monitoring, prepare for potential issues

#### Degraded (2)
- **Description**: Performance impacted but core functions operational
- **Indicators**: Multiple medium-severity errors
- **Action**: Reduced functionality, focus on essential operations

#### Critical (3)
- **Description**: Major issues with limited functionality
- **Indicators**: High-severity errors affecting core components
- **Action**: Emergency procedures, notify administrators

#### Failed (4)
- **Description**: System failure requiring immediate intervention
- **Indicators**: Critical errors, component failures
- **Action**: Emergency shutdown, preserve system state

### Health Monitoring Process

#### Continuous Monitoring
- **Frequency**: Every 30 seconds
- **Components**: All registered system components
- **Checks**: Functionality, connectivity, performance
- **Logging**: Health changes logged with timestamps

#### Component Health Checks

```csharp
// Example health check implementation
private SystemHealth CheckTradingEngineHealth()
{
    try
    {
        // Verify account connectivity
        if (_robot.Account == null) return SystemHealth.Failed;
        
        // Check data access
        var balance = _robot.Account.Balance;
        var positions = _robot.Positions.Count;
        
        return SystemHealth.Healthy;
    }
    catch (Exception ex)
    {
        _logger.Error("Trading engine health check failed", ex);
        return SystemHealth.Critical;
    }
}
```

#### Health Escalation
1. **Component Failure**: Individual component marked unhealthy
2. **Recovery Attempt**: Automatic recovery triggered
3. **System Assessment**: Overall system health updated
4. **Mode Changes**: Entry/exit from recovery mode
5. **Notifications**: Alerts sent for critical issues

## Integration Guide

### Basic Integration

#### 1. Service Initialization
```csharp
// In CoreBot OnStart()
private void InitializeErrorHandling()
{
    // Initialize in order
    _logger = new Logger(this, BotConfig.BotName, BotConfig.BotVersion);
    _errorHandler = new ErrorHandler(this, _logger);
    _crashRecovery = new CrashRecovery(this, _logger, _errorHandler);
}
```

#### 2. Error Handling in Methods
```csharp
protected override void OnTick()
{
    try
    {
        // Your trading logic here
        ProcessTickData();
    }
    catch (Exception ex)
    {
        _errorHandler?.HandleException(ex, "OnTick processing", true);
    }
}
```

#### 3. Health-Aware Processing
```csharp
protected override void OnBar()
{
    try
    {
        // Check system health before processing
        var health = _errorHandler?.GetSystemHealth() ?? SystemHealth.Healthy;
        if (health >= SystemHealth.Critical)
        {
            _logger?.Warning($"System health critical ({health}) - limiting processing");
            return;
        }
        
        // Normal processing
        ExecuteStrategy();
    }
    catch (Exception ex)
    {
        _errorHandler?.HandleException(ex, "OnBar strategy execution", true);
    }
}
```

### Advanced Integration

#### Custom Error Handling
```csharp
// Custom error with specific context
public void ValidateTradeParameters(double volume, double stopLoss)
{
    try
    {
        if (volume <= 0)
        {
            _errorHandler.HandleError(
                ErrorCategory.Trading,
                ErrorSeverity.High,
                $"Invalid volume: {volume}",
                context: $"Trade validation - Volume: {volume}, SL: {stopLoss}",
                attemptRecovery: false
            );
            return;
        }
        
        // Validation logic continues...
    }
    catch (Exception ex)
    {
        _errorHandler.HandleException(ex, "Trade parameter validation", true);
    }
}
```

#### Recovery Mode Handling
```csharp
public void ExecuteTrade()
{
    // Check if system is in recovery mode
    if (_crashRecovery?.IsInRecoveryMode() == true)
    {
        _logger?.Warning("System in recovery mode - trade execution suspended");
        return;
    }
    
    // Check component health
    var tradingHealth = _crashRecovery?.GetComponentHealth("TradingEngine");
    if (tradingHealth?.Status >= SystemHealth.Critical)
    {
        _logger?.Error("Trading engine unhealthy - aborting trade");
        return;
    }
    
    // Execute trade with error handling
    try
    {
        var result = ExecuteMarketOrder(TradeType.Buy, Symbol, Volume);
        _logger?.LogTrade("BUY", Symbol.Name, Volume, Symbol.Bid, $"Order ID: {result.Position?.Id}");
    }
    catch (Exception ex)
    {
        _errorHandler?.HandleException(ex, "Market order execution", true);
    }
}
```

### Configuration Integration

#### Parameter-Based Configuration
```csharp
// Use CoreBot parameters to control error handling
[Parameter("Error Handling Enabled", Group = "SYSTEM", DefaultValue = true)]
public bool ErrorHandlingEnabled { get; set; }

[Parameter("Auto Recovery Enabled", Group = "SYSTEM", DefaultValue = true)]
public bool AutoRecoveryEnabled { get; set; }

[Parameter("Health Check Interval (seconds)", Group = "SYSTEM", DefaultValue = 30)]
public int HealthCheckInterval { get; set; }
```

## Best Practices

### Error Handling Guidelines

#### 1. Comprehensive Coverage
```csharp
// ✅ Good: Wrap all risky operations
try
{
    var result = CalculatePositionSize(riskPercent, stopLossPips);
    _logger.Debug($"Position size calculated: {result} lots");
    return result;
}
catch (Exception ex)
{
    _errorHandler.HandleException(ex, $"Position size calculation - Risk: {riskPercent}%, SL: {stopLossPips} pips", true);
    return GetDefaultPositionSize(); // Fallback
}

// ❌ Bad: Unhandled operations
var result = CalculatePositionSize(riskPercent, stopLossPips); // Could throw
return result;
```

#### 2. Contextual Information
```csharp
// ✅ Good: Rich context
_errorHandler.HandleError(
    ErrorCategory.Risk,
    ErrorSeverity.High,
    "Position size exceeds maximum allowed",
    context: $"Symbol: {Symbol.Name}, Calculated: {calculatedSize}, Max: {maxAllowed}, Account: {Account.Balance}",
    attemptRecovery: true
);

// ❌ Bad: Minimal context
_errorHandler.HandleError(ErrorCategory.Risk, ErrorSeverity.High, "Size too large");
```

#### 3. Appropriate Severity
```csharp
// ✅ Good: Risk errors are critical
_errorHandler.HandleError(ErrorCategory.Risk, ErrorSeverity.Critical, "Margin call triggered");

// ✅ Good: Data delays are medium severity
_errorHandler.HandleError(ErrorCategory.Data, ErrorSeverity.Medium, "Price feed delay detected");

// ✅ Good: Minor validation is low severity  
_errorHandler.HandleError(ErrorCategory.Configuration, ErrorSeverity.Low, "Optional parameter missing");
```

#### 4. Recovery Awareness
```csharp
// ✅ Good: Check health before major operations
var systemHealth = _errorHandler?.GetSystemHealth() ?? SystemHealth.Healthy;
if (systemHealth >= SystemHealth.Critical)
{
    _logger?.Warning("System critical - deferring strategy execution");
    return;
}

// ✅ Good: Respect recovery mode
if (_crashRecovery?.IsInRecoveryMode() == true)
{
    _logger?.Info("Recovery mode active - using conservative settings");
    riskPercent *= 0.5; // Reduce risk during recovery
}
```

### Performance Considerations

#### 1. Error Handling Overhead
- **Minimal Impact**: Framework designed for high-frequency operation
- **Efficient Logging**: Asynchronous where possible, minimal allocations
- **Smart Throttling**: Health checks every 30 seconds, not every tick

#### 2. Memory Management
- **Bounded Queues**: Recent errors limited to 100 entries
- **Automatic Cleanup**: Old error records automatically removed
- **Resource Disposal**: Proper cleanup in OnStop()

#### 3. Thread Safety
- **Lock Minimization**: Fine-grained locking for specific operations
- **Main Thread Integration**: Robot property access via BeginInvokeOnMainThread()
- **Concurrent Collections**: Thread-safe data structures where appropriate

### Testing Strategies

#### 1. Error Simulation
```csharp
// Test error handling with simulated errors
public void TestErrorHandling()
{
    // Simulate network error
    _errorHandler.HandleError(
        ErrorCategory.Network,
        ErrorSeverity.Medium,
        "Simulated connection timeout",
        context: "Testing error handling",
        attemptRecovery: true
    );
    
    // Verify system response
    var health = _errorHandler.GetSystemHealth();
    Assert.That(health, Is.EqualTo(SystemHealth.Warning));
}
```

#### 2. Recovery Testing
```csharp
// Test recovery mechanisms
public void TestRecoveryMode()
{
    // Force critical error
    _errorHandler.HandleError(ErrorCategory.System, ErrorSeverity.Critical, "Test critical error");
    
    // Verify recovery mode activation
    Assert.That(_crashRecovery.IsInRecoveryMode(), Is.True);
    
    // Test degraded functionality
    var componentHealth = _crashRecovery.GetComponentHealth("TradingEngine");
    Assert.That(componentHealth.Status, Is.EqualTo(SystemHealth.Critical));
}
```

## Troubleshooting

### Common Issues

#### 1. High Error Rates
**Symptoms**: Frequent health degradation, excessive error logging
**Causes**: Network issues, configuration problems, strategy logic errors
**Solutions**:
- Check network connectivity and latency
- Validate cBot parameters and configuration
- Review strategy logic for edge cases
- Increase error thresholds if appropriate

#### 2. Recovery Loops
**Symptoms**: Constant recovery attempts, system never stabilizes
**Causes**: Persistent underlying issues, incorrect recovery logic
**Solutions**:
- Identify root cause of recurring errors
- Adjust recovery cooldown periods
- Review component health check logic
- Consider manual intervention for persistent issues

#### 3. False Health Alarms
**Symptoms**: Health degradation without apparent issues
**Causes**: Overly sensitive thresholds, temporary system load
**Solutions**:
- Adjust health monitoring thresholds
- Review error categorization accuracy
- Consider system load patterns
- Implement more sophisticated health metrics

#### 4. Performance Impact
**Symptoms**: Slower execution, increased latency
**Causes**: Excessive logging, frequent health checks, lock contention
**Solutions**:
- Reduce logging verbosity for high-frequency operations
- Optimize health check frequency
- Review lock usage and minimize contention
- Profile performance during error handling

### Diagnostic Tools

#### 1. Error Statistics
```csharp
// Get error counts by category
foreach (ErrorCategory category in Enum.GetValues(typeof(ErrorCategory)))
{
    var count = _errorHandler.GetErrorCount(category);
    _logger.Info($"Error count [{category}]: {count}");
}
```

#### 2. Recent Error Analysis
```csharp
// Review recent errors
var recentErrors = _errorHandler.GetRecentErrors(20);
foreach (var error in recentErrors)
{
    _logger.Info($"{error.Timestamp}: [{error.Category}] {error.Message}");
}
```

#### 3. Component Health Report
```csharp
// Check all component health
var components = new[] { "Logger", "ErrorHandler", "TradingEngine", "RiskManager" };
foreach (var component in components)
{
    var health = _crashRecovery.GetComponentHealth(component);
    _logger.Info($"{component}: {health.Status} (Failures: {health.FailureCount})");
}
```

#### 4. Recovery History
```csharp
// Review recovery attempts
var recoveryEvents = _crashRecovery.GetRecentRecoveryEvents(10);
foreach (var recovery in recoveryEvents)
{
    _logger.Info($"{recovery.Timestamp}: {recovery.ComponentName} - {recovery.Action} (Success: {recovery.Success})");
}
```

## API Reference

### ErrorHandler Class

#### Methods

##### HandleError
```csharp
public RecoveryAction HandleError(
    ErrorCategory category, 
    ErrorSeverity severity, 
    string message, 
    Exception exception = null, 
    string context = "", 
    bool attemptRecovery = true)
```
Handles an error with comprehensive context and automatic recovery.

**Parameters:**
- `category`: The category of the error
- `severity`: The severity level of the error  
- `message`: The error message
- `exception`: Optional exception details
- `context`: Additional context information
- `attemptRecovery`: Whether to attempt automatic recovery

**Returns:** The recommended recovery action

##### HandleException
```csharp
public RecoveryAction HandleException(
    Exception exception, 
    string context = "", 
    bool attemptRecovery = true)
```
Handles an exception with automatic categorization.

**Parameters:**
- `exception`: The exception to handle
- `context`: Additional context information
- `attemptRecovery`: Whether to attempt automatic recovery

**Returns:** The recommended recovery action

##### GetSystemHealth
```csharp
public SystemHealth GetSystemHealth()
```
Gets the current system health status.

**Returns:** Current system health status

##### GetErrorCount
```csharp
public int GetErrorCount(ErrorCategory category)
```
Gets error statistics for a specific category.

**Parameters:**
- `category`: The error category

**Returns:** Error count for the category

##### GetRecentErrors
```csharp
public List<ErrorEvent> GetRecentErrors(int count = 10)
```
Gets the most recent errors.

**Parameters:**
- `count`: Number of recent errors to retrieve

**Returns:** List of recent error events

### CrashRecovery Class

#### Methods

##### IsInRecoveryMode
```csharp
public bool IsInRecoveryMode()
```
Gets the current system recovery status.

**Returns:** True if system is in recovery mode

##### GetComponentHealth
```csharp
public ComponentHealth GetComponentHealth(string componentName)
```
Gets component health information.

**Parameters:**
- `componentName`: Name of the component

**Returns:** Component health information

##### GetRecentRecoveryEvents
```csharp
public List<RecoveryEvent> GetRecentRecoveryEvents(int count = 10)
```
Gets recent recovery events.

**Parameters:**
- `count`: Number of recent events to retrieve

**Returns:** List of recent recovery events

### Data Structures

#### ErrorEvent
```csharp
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
```

#### ComponentHealth
```csharp
public class ComponentHealth
{
    public string ComponentName { get; set; }
    public SystemHealth Status { get; set; }
    public DateTime LastCheck { get; set; }
    public DateTime LastFailure { get; set; }
    public int FailureCount { get; set; }
    public string LastError { get; set; }
    public bool IsEnabled { get; set; }
}
```

#### RecoveryEvent
```csharp
public class RecoveryEvent
{
    public DateTime Timestamp { get; set; }
    public string ComponentName { get; set; }
    public RecoveryAction Action { get; set; }
    public bool Success { get; set; }
    public string Details { get; set; }
}
```

---

## Summary

The HaruQuant CoreBot Error Handling Framework provides comprehensive, production-ready error management for automated trading systems. By implementing intelligent error categorization, automatic recovery mechanisms, and continuous health monitoring, it ensures maximum uptime and capital protection in challenging market conditions.

For additional support or questions about the error handling framework, refer to the comprehensive logging output and system health monitoring capabilities built into the framework itself.

**Framework Version:** 1.0.0  
**Last Updated:** August 28, 2025  
**Compatibility:** cTrader Automate API, .NET 6.0+
