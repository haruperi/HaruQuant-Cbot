# HaruQuant-Cbot

A comprehensive, production-ready cTrader cBot with advanced risk management, error handling, and automated trading capabilities.

## Project Overview

HaruQuant-Cbot is a sophisticated algorithmic trading bot built for the cTrader platform. It implements a modular architecture with comprehensive error handling, crash recovery, and risk management systems designed for reliable production trading.

## Current Development Status

**Version:** 1.0.0-alpha  
**Development Phase:** Core Infrastructure Complete  
**Next Phase:** Strategy Implementation & Testing

## Project Lifecycle

### Phase 1: Foundation Infrastructure ✅ COMPLETED

#### 1.1 Core Bot Setup
- [x] **CoreBot.cs** - Main entry point class with lifecycle management
- [x] **Parameter System** - 75+ configurable parameters across 7 logical groups
- [x] **Constants.cs** - System-wide configuration and enums
- [x] **Project Structure** - Modular architecture with separated concerns

#### 1.2 Logging System
- [x] **Logger.cs** - Comprehensive logging with file rotation (10MB files)
- [x] **Logging Guidelines** - Mandatory format: `ClassName | MethodName | message`
- [x] **Log Levels** - Debug, Info, Warning, Error with appropriate usage
- [x] **File Management** - Automatic log rotation and cleanup

#### 1.3 Error Handling Framework
- [x] **ErrorHandler.cs** - Centralized error management and categorization
- [x] **Error Categories** - System, Trading, Risk, Network, Data, Strategy, Configuration, External
- [x] **Severity Levels** - Low, Medium, High, Critical with automatic escalation
- [x] **Recovery Actions** - None, Retry, Fallback, Restart, Alert, Stop

#### 1.4 Crash Recovery System
- [x] **CrashRecovery.cs** - Automated system health monitoring
- [x] **Component Health** - Individual component monitoring and recovery
- [x] **Recovery Modes** - Normal, Recovery, Emergency with graceful degradation
- [x] **Threading Safety** - Main thread invocation for Robot property access

#### 1.5 Risk Management
- [x] **RiskManager.cs** - Comprehensive position sizing and validation
- [x] **Position Sizing** - Auto, Fixed Lots, Fixed Amount, Step-based
- [x] **Risk Validation** - 9-point validation system for trade safety
- [x] **Risk Bases** - Equity, Balance, Free Margin, Fixed Balance
- [x] **Stop Loss/Take Profit** - Fixed, ATR-based, ADR-based calculations

#### 1.6 Trade Management
- [x] **TradeManager.cs** - Integrated trade execution and management
- [x] **Risk Integration** - Full integration with RiskManager
- [x] **Error Handling** - Comprehensive error handling for trade operations
- [x] **Order Management** - Market orders with comprehensive validation

### Phase 2: Strategy Implementation ⚠️ IN PROGRESS

#### 2.1 Basic Strategy Framework
- [x] **Trend Following Strategy** - Simple MA crossover implementation
- [x] **Moving Averages** - Fast, Slow, Bias MA with configurable periods
- [x] **Signal Generation** - Buy/Sell signal detection logic
- [x] **Strategy Integration** - Full integration with risk and trade management

#### 2.2 Strategy Components (Planned)
- [ ] **Strategy Base Classes** - Abstract base for strategy development
- [ ] **Signal Generators** - Modular signal generation framework
- [ ] **Multi-timeframe Analysis** - Higher timeframe confirmation
- [ ] **Pattern Recognition** - Candlestick and chart pattern detection

### Phase 3: Advanced Features (Planned)

#### 3.1 Enhanced Strategy System
- [ ] **Multiple Strategies** - Mean Reversion, Breakout, Scalping
- [ ] **Strategy Switching** - Dynamic strategy selection
- [ ] **Portfolio Management** - Multi-symbol trading
- [ ] **Performance Tracking** - Real-time performance metrics

#### 3.2 Market Analysis
- [ ] **Market Condition Detection** - Trend, Range, Volatility analysis
- [ ] **Volatility Metrics** - ATR, ADR, custom volatility measures
- [ ] **Correlation Analysis** - Multi-instrument correlation tracking
- [ ] **Economic Calendar** - News and event integration

#### 3.3 External Integrations
- [ ] **Notifications** - Email, Telegram, Push notifications
- [ ] **Data Export** - Trade history and performance reporting
- [ ] **API Integrations** - External data sources
- [ ] **Cloud Sync** - Configuration and data synchronization

## Current Architecture

### Core Components

```
HaruQuant-Cbot/
├── CoreBot.cs              # Main entry point and lifecycle management
├── utils/
│   ├── Constants.cs        # System-wide configuration and enums
│   ├── Logger.cs           # Comprehensive logging system
│   ├── ErrorHandler.cs     # Centralized error management
│   └── CrashRecovery.cs    # Automated recovery and health monitoring
├── trading/
│   ├── RiskManager.cs      # Position sizing and risk validation
│   └── TradeManager.cs     # Trade execution and management
└── docs/
    └── ErrorHandlingGuide.md
```

## Code Execution Flow (Step-by-Step)

### 1. 🚀 **Bot Startup Sequence (OnStart)**

```
cTrader Platform
    ↓
CoreBot.OnStart() [Entry Point]
    ↓
Try-Catch Block Initialization
    ↓
┌─ InitializeLogger()
│   ├─ new Logger(robot, botName, version, settings...)
│   ├─ Logger constructor validates parameters
│   ├─ Sets up file logging with 10MB rotation
│   └─ Log: "Logger service initialized successfully"
    ↓
├─ InitializeErrorHandler()
│   ├─ new ErrorHandler(robot, logger)
│   ├─ Initialize error counters for all categories
│   ├─ Setup error tracking infrastructure
│   └─ Log: "ErrorHandler service initialized successfully"
    ↓
├─ InitializeCrashRecovery()
│   ├─ new CrashRecovery(robot, logger, errorHandler)
│   ├─ Initialize component health tracking
│   ├─ Start health monitoring timers (30s intervals)
│   ├─ Setup recovery event queue
│   └─ Log: "CrashRecovery service initialized successfully"
    ↓
├─ InitializeRiskManager()
│   ├─ new RiskManager(robot, logger)
│   ├─ Validate constructor parameters
│   └─ Log: "RiskManager service initialized successfully"
    ↓
├─ InitializeTradeManager()
│   ├─ new TradeManager(robot, logger, errorHandler, riskManager)
│   ├─ Validate all dependencies
│   └─ Log: "TradeManager service initialized successfully"
    ↓
├─ InitializeIndicators()
│   ├─ Set source = SourceSeries ?? Bars.ClosePrices
│   ├─ _fastMA = Indicators.MovingAverage(source, FastPeriod, MAType)
│   ├─ _slowMA = Indicators.MovingAverage(source, SlowPeriod, MAType)
│   ├─ _biasMA = Indicators.MovingAverage(source, BiasPeriod, MAType)
│   └─ Log indicator configurations
    ↓
├─ Log Bot Information
│   ├─ Bot name, version, symbol, account details
│   ├─ Trading mode, strategy, risk settings
│   └─ Trading hours and direction settings
    ↓
├─ Initial System Health Check
│   ├─ _errorHandler.GetSystemHealth()
│   ├─ UpdateSystemHealth() → analyze recent errors
│   ├─ Determine health status (Healthy/Warning/Degraded/Critical/Failed)
│   └─ Log: "Initial System Health: {status}"
    ↓
├─ Log Successful Startup
│   └─ _errorHandler.HandleError() with success notification
    ↓
└─ Exception Handling
    ├─ Print critical error to cTrader console
    ├─ ErrorHandler.HandleException() if available
    ├─ Logger.Error() if available
    └─ Re-throw exception to notify cTrader of failure
```

### 2. ⚡ **Real-Time Processing (OnTick)**

```
cTrader Platform (Every Price Tick)
    ↓
CoreBot.OnTick() [High Frequency - Multiple times per second]
    ↓
Try-Catch Block
    ↓
├─ Recovery Mode Check
│   ├─ _crashRecovery.IsInRecoveryMode()
│   ├─ If TRUE → return (skip processing)
│   └─ If FALSE → continue
    ↓
├─ Tick Volume Logging (Every 1000 ticks)
│   ├─ Check: Bars.TickVolumes.Count % 1000 == 0
│   └─ Log: "OnTick processed - Tick count: {count}"
    ↓
├─ Future Tick Processing (Placeholder)
│   └─ Reserved for high-frequency strategy logic
    ↓
└─ Exception Handling
    └─ _errorHandler.HandleException() → automatic recovery
```

### 3. 📊 **Bar Processing (OnBar)**

```
cTrader Platform (Every New Bar/Candle)
    ↓
CoreBot.OnBar() [Strategy Execution Trigger]
    ↓
Try-Catch Block
    ↓
├─ Log New Bar Information
│   └─ Log: "New bar opened at {time} | Open: {price} | Close: {price}"
    ↓
├─ System Health Check
│   ├─ _errorHandler.GetSystemHealth()
│   ├─ If health >= SystemHealth.Critical
│   │   ├─ Log: "System health is {status} - limiting processing"
│   │   └─ return (skip strategy execution)
│   └─ If health OK → continue
    ↓
├─ ExecuteTrendFollowingStrategy()
│   ├─ Bar Count Validation
│   │   ├─ required = Max(FastPeriod, SlowPeriod, BiasPeriod) + 1
│   │   ├─ If Bars.Count < required → return
│   │   └─ Log: "Not enough bars for calculation"
│   │
│   ├─ Get Moving Average Values
│   │   ├─ currentFastMA = _fastMA.Result.LastValue
│   │   ├─ currentSlowMA = _slowMA.Result.LastValue
│   │   ├─ currentBiasMA = _biasMA.Result.LastValue
│   │   ├─ previousFastMA = _fastMA.Result.Last(1)
│   │   ├─ previousSlowMA = _slowMA.Result.Last(1)
│   │   └─ Log all MA values
│   │
│   ├─ Signal Generation
│   │   ├─ buySignal = (previousFastMA < previousSlowMA) AND
│   │   │              (currentFastMA > currentSlowMA) AND
│   │   │              (currentSlowMA > currentBiasMA)
│   │   │
│   │   ├─ sellSignal = (previousFastMA > previousSlowMA) AND
│   │   │               (currentFastMA < currentSlowMA) AND
│   │   │               (currentSlowMA < currentBiasMA)
│   │   └─ Log: "Signals - Buy: {buySignal}, Sell: {sellSignal}"
│   │
│   ├─ Trade Execution
│   │   ├─ If buySignal → ExecuteMarketOrder(TradeType.Buy, ...)
│   │   └─ If sellSignal → ExecuteMarketOrder(TradeType.Sell, ...)
│   │
│   └─ Exception Handling
│       └─ Log strategy execution errors
    ↓
└─ Exception Handling
    └─ _errorHandler.HandleException() → automatic recovery
```

### 4. 💼 **Trade Execution Flow (ExecuteOrder)**

```
ExecuteOrder(TradeType tradeType) [Called from Strategy]
    ↓
Try-Catch Block
    ↓
├─ Log Trade Attempt
│   └─ Log: "Attempting to execute {tradeType} order using TradeManager"
    ↓
├─ TradeManager.ExecuteTrade()
│   ├─ Pass ALL parameters from CoreBot
│   │   ├─ tradeType, OrderLabel, UseTradingHours
│   │   ├─ TradingHourStart, TradingHourEnd, TradingDirection
│   │   ├─ MaxSpreadInPips, RiskSizeMode, DefaultPositionSize
│   │   ├─ RiskPerTrade, FixedRiskAmount, RiskBase, FixedRiskBalance
│   │   ├─ StopLossMode, DefaultStopLoss, TakeProfitMode, DefaultTakeProfit
│   │   ├─ StopLossMultiplier, TakeProfitMultiplier, ADRRatio
│   │   └─ ADRPeriod, ATRPeriod, LotIncrease, BalanceIncrease
│   │
│   ├─ TradeManager Internal Flow:
│   │   ├─ Log: "MINIMAL ExecuteTrade | {tradeType} {symbol}"
│   │   │
│   │   ├─ RiskManager.Run() [Complete Risk Assessment]
│   │   │   ├─ ValidateTrade() [9-Point Validation]
│   │   │   │   ├─ ValidateSymbol() → check null, pip size, digits
│   │   │   │   ├─ ValidatePositionSize() → check min/max limits
│   │   │   │   ├─ ValidateStopLoss() → check pip distance
│   │   │   │   ├─ ValidateSpread() → check current spread vs max
│   │   │   │   ├─ ValidateTradingHours() → check time restrictions
│   │   │   │   ├─ ValidateTradingDirection() → check allowed directions
│   │   │   │   ├─ ValidateRiskAmount() → check monetary risk limits
│   │   │   │   ├─ ValidateAccountHealth() → check margin levels
│   │   │   │   └─ IsEmergencyStopTriggered() → check emergency conditions
│   │   │   │
│   │   │   ├─ CalculateTargets() [Stop Loss & Take Profit]
│   │   │   │   ├─ Stop Loss Calculation
│   │   │   │   │   ├─ Fixed → use DefaultStopLoss
│   │   │   │   │   ├─ UseATR → CalculateATRBasedValue()
│   │   │   │   │   ├─ UseADR → CalculateATRBasedValue() with daily timeframe
│   │   │   │   │   └─ None → 0
│   │   │   │   │
│   │   │   │   └─ Take Profit Calculation (same logic as SL)
│   │   │   │
│   │   │   ├─ CalculatePositionSize()
│   │   │   │   ├─ Auto → CalculateAutoPositionSize()
│   │   │   │   ├─ FixedLots → use DefaultPositionSize
│   │   │   │   ├─ FixedAmount → CalculateAutoPositionSize() with fixed amount
│   │   │   │   ├─ FixedLotsStep → CalculateStepBasedPositionSize()
│   │   │   │   └─ NormalizePositionSize() → apply symbol constraints
│   │   │   │
│   │   │   └─ Return: (isValid, positionSize, stopLoss, takeProfit)
│   │   │
│   │   ├─ Risk Validation Check
│   │   │   ├─ If isTradeValid == false
│   │   │   │   ├─ Log: "Trade validation FAILED"
│   │   │   │   └─ Return: TradeResult.Failed()
│   │   │   └─ If valid → continue
│   │   │
│   │   ├─ Execute Market Order
│   │   │   ├─ _robot.ExecuteMarketOrder()
│   │   │   │   ├─ Parameters: tradeType, symbol, positionSize, label
│   │   │   │   ├─ stopLoss (in pips), takeProfit (in pips)
│   │   │   │   └─ Return: TradeResult from cTrader
│   │   │   │
│   │   │   ├─ If Successful
│   │   │   │   ├─ Log: "Order executed successfully"
│   │   │   │   ├─ Log: position details, prices
│   │   │   │   └─ Return: TradeResult.Success()
│   │   │   │
│   │   │   └─ If Failed
│   │   │       ├─ Log: "Order execution failed"
│   │   │       └─ Return: TradeResult.Failed()
│   │   │
│   │   └─ Exception Handling
│   │       ├─ Log: execution errors
│   │       └─ Return: TradeResult.Failed()
│   │
│   └─ Return: TradeResult object
    ↓
├─ Process TradeManager Result
│   ├─ If result.IsSuccessful
│   │   ├─ Log: "Order executed successfully via TradeManager"
│   │   └─ Log: execution details (Position ID, prices)
│   │
│   └─ If result.Failed
│       ├─ Log: "Order execution failed via TradeManager"
│       └─ ErrorHandler.HandleError() → log trading error
    ↓
└─ Exception Handling
    ├─ Log: execution error
    └─ ErrorHandler.HandleException() → attempt recovery
```

### 5. 🛑 **Bot Shutdown (OnStop)**

```
cTrader Platform (Bot Stop Requested)
    ↓
CoreBot.OnStop() [Cleanup and Finalization]
    ↓
Try-Catch Block
    ↓
├─ Log Shutdown Start
│   └─ Log: "=== CoreBot Stopping ==="
    ↓
├─ Final System Statistics
│   ├─ _errorHandler.GetSystemHealth()
│   ├─ Log: "Final System Health: {status}"
│   │
│   ├─ Error Count Summary
│   │   ├─ Loop through all ErrorCategory enums
│   │   ├─ _errorHandler.GetErrorCount(category)
│   │   └─ Log: "Error Count [{category}]: {count}" (if > 0)
│   │
│   └─ Final Account Information
│       ├─ Log: "Final Account Balance: {balance}"
│       ├─ Log: "Open Positions: {count}"
│       └─ Log: "Pending Orders: {count}"
    ↓
├─ Resource Cleanup
│   ├─ _crashRecovery.Dispose()
│   │   ├─ _healthCheckTimer.Dispose()
│   │   ├─ _recoveryTimer.Dispose()
│   │   └─ Log: "CrashRecovery system disposed"
│   │
│   └─ _logger.Flush() → ensure all logs written to file
    ↓
├─ Final Success Log
│   ├─ Log: "CoreBot shutdown completed successfully"
│   └─ Print: "CoreBot stopped successfully" (to cTrader console)
    ↓
└─ Exception Handling
    ├─ Print: shutdown error to console
    ├─ Try: ErrorHandler.HandleException()
    ├─ Try: Logger.Error()
    └─ Ignore any errors during error handling (fail-safe)
```

### 6. 🔄 **Background Health Monitoring (Continuous)**

```
CrashRecovery Timer (Every 30 seconds)
    ↓
PerformHealthCheck()
    ↓
BeginInvokeOnMainThread() [Thread Safety]
    ↓
Try-Catch Block
    ↓
├─ Lock Health Data
│   └─ lock (_lockObject)
    ↓
├─ Check Each Component
│   ├─ Loop through all tracked components
│   │   ├─ Logger, ErrorHandler, TradingEngine
│   │   ├─ RiskManager, DataProcessor, NetworkConnection
│   │   └─ StrategyEngine
│   │
│   ├─ For Each Component:
│   │   ├─ previousStatus = component.Status
│   │   ├─ component.Status = CheckComponentHealth()
│   │   ├─ component.LastCheck = DateTime.Now
│   │   │
│   │   ├─ If status degraded
│   │   │   ├─ component.LastFailure = DateTime.Now
│   │   │   ├─ component.FailureCount++
│   │   │   ├─ Log: "Component health degraded"
│   │   │   └─ If Critical → TriggerComponentRecovery()
│   │   │
│   │   └─ If status improved
│   │       └─ Log: "Component recovered to healthy"
│   │
│   └─ Update System Recovery Mode
│       ├─ If all healthy AND in recovery → ExitRecoveryMode()
│       ├─ If problems AND not in recovery → EnterRecoveryMode()
│       └─ Update _lastHealthyState timestamp
    ↓
└─ Exception Handling
    └─ Log: health check errors
```

### System Flow Summary

1. **Initialization** (OnStart)
   - Sequential service initialization with dependency injection
   - Indicator setup and configuration validation
   - Initial health assessment and logging

2. **Runtime Processing** (OnTick/OnBar)
   - Continuous health monitoring and recovery management
   - Strategy signal generation and validation
   - Comprehensive risk assessment and trade execution

3. **Shutdown** (OnStop)
   - Complete system statistics and error summaries
   - Resource cleanup and disposal
   - Final state preservation and logging

### Configuration System

#### Parameter Groups
- **IDENTITY** - Bot information and preset details
- **SYSTEM SETTINGS** - Logging and system configuration
- **STRATEGY** - Trading strategy and indicator settings
- **RISK MANAGEMENT** - Position sizing and risk controls
- **TRADING SETTINGS** - Trading hours and execution settings
- **NOTIFICATION SETTINGS** - Alert and notification configuration
- **DISPLAY SETTINGS** - Chart display and UI settings

## Development Standards

### Logging Format
```csharp
_logger.LogLevel($"ClassName | MethodName | log message");
```

### Documentation Format
```csharp
/***
    Function description here.
    
    Args:
        parameter1: Description of first parameter
        parameter2: Description of second parameter
    
    Returns:
        Description of return value(s) and types.
        
    Notes:
        - Additional implementation details
        - Performance considerations
        - Usage examples or warnings
***/
```

### Error Handling
- All exceptions must be logged with full context
- Use ErrorHandler for centralized error management
- Implement appropriate recovery actions
- Maintain system stability during errors

## Building and Running

### Prerequisites
- cTrader platform
- .NET 6.0 or later
- Visual Studio 2022 (recommended)

### Build Process
1. Open HaruQuant-Cbot.sln in Visual Studio
2. Build solution (generates .algo file)
3. Copy .algo file to cTrader cBots folder
4. Configure parameters in cTrader

### Configuration
- Set up logging preferences
- Configure risk management parameters
- Set trading hours and symbol preferences
- Adjust strategy parameters for your needs

## Testing Status

### Unit Testing
- [ ] **Framework Setup** - NUnit/MSTest configuration
- [ ] **Component Tests** - Individual component validation
- [ ] **Integration Tests** - Module interaction testing
- [ ] **Error Handling Tests** - Exception and recovery testing

### Backtesting
- [x] **Basic Strategy** - Trend following strategy tested
- [ ] **Risk Management** - Position sizing validation
- [ ] **Error Scenarios** - Error handling under stress
- [ ] **Performance Metrics** - Statistical analysis

## Performance Considerations

### Optimization
- Thread-safe operations for multi-threading
- Efficient memory management
- Minimal OnTick processing overhead
- Optimized indicator calculations

### Resource Management
- Log file rotation (10MB limit)
- Memory-efficient data structures
- Proper disposal of resources
- Exception handling without memory leaks

## Future Roadmap

### Short Term (Next Release)
- [ ] Complete strategy framework implementation
- [ ] Enhanced backtesting capabilities
- [ ] Performance optimization
- [ ] Comprehensive unit testing

### Medium Term
- [ ] Multi-strategy support
- [ ] Advanced risk management features
- [ ] External notification systems
- [ ] Performance analytics dashboard

### Long Term
- [ ] Machine learning integration
- [ ] Portfolio management
- [ ] Cloud-based configuration
- [ ] Advanced market analysis

## Contributing

### Development Workflow
1. Review current phase requirements
2. Follow established coding standards
3. Implement comprehensive logging
4. Add appropriate error handling
5. Update documentation
6. Test thoroughly before commit

### Commit Message Format
- `feat:` - New features or functionality additions
- `fix:` - Bug fixes and error corrections
- `docs:` - Documentation updates and improvements
- `style:` - Code style changes and refactoring
- `perf:` - Performance improvements and optimizations
- `test:` - Test additions and modifications

## License

This project is licensed under the MIT License - see the LICENSE.txt file for details.

## Support

For questions, issues, or contributions, please refer to the project documentation and follow the established development standards.

---

**Last Updated:** 2024-12-19  
**Current Version:** 1.0.0-alpha  
**Next Milestone:** Strategy Framework Completion
