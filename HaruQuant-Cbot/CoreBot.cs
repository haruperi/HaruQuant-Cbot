using System;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Robots.Utils;
using cAlgo.Robots.Trading;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None, AddIndicators = true)]
    public class CoreBot : Robot
    {
        #region Parameters of CBot

        #region Identity
        [Parameter(BotConfig.BotName + " " + BotConfig.BotVersion, Group = "IDENTITY", DefaultValue = "https://haruperi.ltd/trading/")]
        public string ProductInfo { get; set; }

        [Parameter("Preset information", Group = "IDENTITY", DefaultValue = "XAUUSD Range5 | 01.01.2024 to 29.04.2024 | $1000")]
        public string PresetInfo { get; set; }
        #endregion

        #region System Settings
        [Parameter("Enable Console Logging", Group = "SYSTEM SETTINGS", DefaultValue = true)]
        public bool EnableConsoleLogging { get; set; }

        [Parameter("Enable File Logging", Group = "SYSTEM SETTINGS", DefaultValue = true)]
        public bool EnableFileLogging { get; set; }

        [Parameter("Log File Name", Group = "SYSTEM SETTINGS", DefaultValue = "cbot_log.txt")]
        public string LogFileName { get; set; }
        #endregion

        // ------------------------------------Strategy Settings------------------------------------

        #region Strategy
        [Parameter("Trading Mode", Group = "STRATEGY", DefaultValue = TradingMode.Both)]
        public TradingMode MyTradingMode { get; set; }

        // ActiveStrategy now uses the locally defined Strategy enum
        [Parameter("Strategy", Group = "STRATEGY", DefaultValue = Strategy.TrendFollowing)]
        public Strategy ActiveStrategy { get; set; }

        [Parameter("Symbols To Trade", Group = "STRATEGY", DefaultValue = SymbolsToTrade.Custom)]
        public SymbolsToTrade SymbolsToTrade { get; set; }

        [Parameter("Custom Symbols (comma-separated)", Group = "STRATEGY", DefaultValue = "")]
        public string CustomSymbols { get; set; }

        [Parameter("MA Type", Group = "STRATEGY", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType { get; set; }

        [Parameter("Source", Group = "STRATEGY")]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Fast Period", Group = "STRATEGY", DefaultValue = 12, Step = 1)]
        public int FastPeriod { get; set; }

        [Parameter("Slow Period", Group = "STRATEGY", DefaultValue = 48, Step = 1)]
        public int SlowPeriod { get; set; }

        [Parameter("Bias Period", Group = "STRATEGY", DefaultValue = 288, Step = 1)]
        public int BiasPeriod { get; set; }

        [Parameter("RSI Period", Group = "STRATEGY", DefaultValue = 12, Step = 1)]
        public int RSIPeriod { get; set; }

        [Parameter("RSI Overbought Level", Group = "STRATEGY", DefaultValue = 70, Step = 1)]
        public int RSIOverboughtLevel { get; set; }

        [Parameter("RSI Oversold Level", Group = "STRATEGY", DefaultValue = 30, Step = 1)]
        public int RSIOversoldLevel { get; set; }

        [Parameter("Trading Timeframe", Group = "STRATEGY", DefaultValue = "Minute5")]
        public TimeFrame TradingTimeframe { get; set; }

        [Parameter("Higher Timeframe", Group = "STRATEGY", DefaultValue = "Hour")]
        public TimeFrame HigherTimeframe { get; set; }

        [Parameter("HT Min Distance (Pips)", Group = "STRATEGY", DefaultValue = 5)]
        public int HTMinDistancePips { get; set; }

        [Parameter("LT Min Distance (Pips)", Group = "STRATEGY", DefaultValue = 2)]
        public int LTMinDistancePips { get; set; }
        #endregion

        // ------------------------------------Risk Management------------------------------------

        #region Risk Management

        [Parameter("Risk Base", Group = "RISK MANAGEMENT", DefaultValue = RiskBase.Equity)]
        public RiskBase RiskBase { get; set; }

        [Parameter("Risk Size", Group = "RISK MANAGEMENT", DefaultValue = RiskDefaultSize.Auto)]
        public RiskDefaultSize RiskSizeMode { get; set; }

        [Parameter("Stop Loss Mode", Group = "RISK MANAGEMENT", DefaultValue = StopLossMode.Fixed)]
        public StopLossMode StopLossMode { get; set; }

        [Parameter("Take Profit Mode", Group = "RISK MANAGEMENT", DefaultValue = TakeProfitMode.Fixed)]
        public TakeProfitMode TakeProfitMode { get; set; }

        [Parameter("Risk Per Trade", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0.01, Step = 0.01, MaxValue = 100)]
        public double RiskPerTrade { get; set; }

        [Parameter("Fixed Risk Balance", Group = "RISK MANAGEMENT", DefaultValue = 1000, MinValue = 20)]
        public double FixedRiskBalance { get; set; }

        [Parameter("Fixed Risk Amount", Group = "RISK MANAGEMENT", DefaultValue = 1000, MinValue = 20)]
        public double FixedRiskAmount { get; set; }

        [Parameter("Balance Increase", Group = "RISK MANAGEMENT", DefaultValue = 100, MinValue = 20)]
        public double BalanceIncrease { get; set; }

        [Parameter("Lot Increase", Group = "RISK MANAGEMENT", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01, MaxValue = 100)]
        public double LotIncrease { get; set; }

        [Parameter("Lot Decrease Ratio", Group = "RISK MANAGEMENT", DefaultValue = 0.3, MinValue = 0.01, Step = 0.01, MaxValue = 1)]
        public double LotDecreaseRatio { get; set; }

        [Parameter("Default Position Size", Group = "RISK MANAGEMENT", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01, MaxValue = 100)]
        public double DefaultPositionSize { get; set; }

        [Parameter("Default Stop Loss", Group = "RISK MANAGEMENT", DefaultValue = 20)]
        public int DefaultStopLoss { get; set; }

        [Parameter("Default Take Profit", Group = "RISK MANAGEMENT", DefaultValue = 40)]
        public int DefaultTakeProfit { get; set; }

        [Parameter("ATR Period", Group = "RISK MANAGEMENT", DefaultValue = 12)]
        public int ATRPeriod { get; set; }

        [Parameter("ADR Period", Group = "RISK MANAGEMENT", DefaultValue = 10)]
        public int ADRPeriod { get; set; }

        [Parameter("Stop Loss Multiplier", Group = "RISK MANAGEMENT", DefaultValue = 1.0)]
        public double StopLossMultiplier { get; set; }

        [Parameter("Take Profit Multiplier", Group = "RISK MANAGEMENT", DefaultValue = 2.0)]
        public double TakeProfitMultiplier { get; set; }

        [Parameter("Manage Trade", Group = "RISK MANAGEMENT", DefaultValue = ManageTrade.Decomposition)]
        public ManageTrade ManageTrade { get; set; }

        [Parameter("Trade Distance Multiplier", Group = "RISK MANAGEMENT", DefaultValue = 1.0, MinValue = 1, Step = 0.5, MaxValue = 5)]
        public double TradeDistanceMultiplier { get; set; }

        [Parameter("Use Trailing Stop", Group = "RISK MANAGEMENT", DefaultValue = false)]
        public bool UseTrailingStop { get; set; }

        [Parameter("Trail Distance", Group = "RISK MANAGEMENT", DefaultValue = 10)]
        public int TrailDistance { get; set; }

        [Parameter("Trail From", Group = "RISK MANAGEMENT", DefaultValue = 10)]
        public int TrailFrom { get; set; }

        [Parameter("ADR Ratio", Group = "RISK MANAGEMENT", DefaultValue = 3.0)]
        public double ADRRatio { get; set; }

        [Parameter("Hide Stop Loss", Group = "RISK MANAGEMENT", DefaultValue = false)]
        public bool HideStopLoss { get; set; }

        [Parameter("Hide Take Profit", Group = "RISK MANAGEMENT", DefaultValue = false)]
        public bool HideTakeProfit { get; set; }

        [Parameter("Max Number of Buy Trades", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0, Step = 1, MaxValue = 100)]
        public int MaxBuyTrades { get; set; }

        [Parameter("Max Number of Sell Trades", Group = "RISK MANAGEMENT", DefaultValue = 1, MinValue = 0, Step = 1, MaxValue = 100)]
        public int MaxSellTrades { get; set; }

        #endregion

        // ------------------------------------Trading Settings------------------------------------

        #region Trading Settings
        [Parameter("Use Trading Hours", Group = "TRADING SETTINGS", DefaultValue = true)]
        public bool UseTradingHours { get; set; }

        [Parameter("Trading Start Hour", Group = "TRADING SETTINGS", DefaultValue = HourOfDay.H02)]
        public HourOfDay TradingHourStart { get; set; }

        [Parameter("Trading End Hour", Group = "TRADING SETTINGS", DefaultValue = HourOfDay.H23)]
        public HourOfDay TradingHourEnd { get; set; }
        
        [Parameter("Trading Direction", Group = "TRADING SETTINGS", DefaultValue = TradingDirection.Both)]
        public TradingDirection TradingDirection { get; set; }

        [Parameter("Order Label", Group = "TRADING SETTINGS", DefaultValue = "HaruQuant Cbot")]
        public string OrderLabel { get; set; }

        [Parameter("Slippage (Pips)", Group = "TRADING SETTINGS", DefaultValue = 1, MinValue = 0)]
        public double SlippageInPips { get; set; }

        [Parameter("Max Spread (Pips)", Group = "TRADING SETTINGS", DefaultValue = 3, MinValue = 0)]
        public double MaxSpreadInPips { get; set; }
        #endregion

        // ------------------------------------Notification Settings------------------------------------
        #region Notification Settings

        [Parameter("Popup Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool PopupNotification { get; set; }

        [Parameter("Sound Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool SoundNotification { get; set; }

        [Parameter("Email Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool EmailNotification { get; set; }

        [Parameter("Email address", Group = "NOTIFICATION SETTINGS", DefaultValue = "notify@testmail.com")]
        public string EmailAddress { get; set; }

        [Parameter("Telegram Notification", Group = "NOTIFICATION SETTINGS", DefaultValue = false)]
        public bool TelegramEnabled { get; set; }

        [Parameter("API Token", Group = "NOTIFICATION SETTINGS", DefaultValue = "")]
        public string TelegramToken { get; set; }

        [Parameter("Chat IDs (separate by comma)", Group = "NOTIFICATION SETTINGS", DefaultValue = "")]
        public string TelegramChatIDs { get; set; }
        #endregion

        // ------------------------------------Display Settings------------------------------------

        #region Display Settings
        [Parameter("Show Objects", Group = "DISPLAY SETTINGS", DefaultValue = true)]
        public bool ShowObjects { get; set; }

        [Parameter("FontSize", Group = "DISPLAY SETTINGS", DefaultValue = 12)]
        public int FontSize { get; set; }

        [Parameter("Space to Corner", Group = "DISPLAY SETTINGS", DefaultValue = 10)]
        public int MarginSpace { get; set; }

        [Parameter("Horizontal Alignment", Group = "DISPLAY SETTINGS", DefaultValue = HorizontalAlignment.Left)]
        public HorizontalAlignment PanelHorizontalAlignment { get; set; }

        [Parameter("Vertical Alignment", Group = "DISPLAY SETTINGS", DefaultValue = VerticalAlignment.Top)]
        public VerticalAlignment PanelVerticalAlignment { get; set; }

        [Parameter("Text Color", Group = "DISPLAY SETTINGS", DefaultValue = "Snow")]
        public string ColorText { get; set; }

        [Parameter("Show How To Use", Group = "DISPLAY SETTINGS", DefaultValue = true)]
        public bool ShowHowToUse { get; set; }
        #endregion

        #endregion

        // Core system services for error handling and logging
        private Logger _logger;
        private ErrorHandler _errorHandler;
        private CrashRecovery _crashRecovery;
        private RiskManager _riskManager;
        private TradeManager _tradeManager;
        
        // Moving Average indicators for trend following strategy
        private MovingAverage _fastMA;
        private MovingAverage _slowMA;
        private MovingAverage _biasMA;

        #region BOT BASE FUNCTIONS

        protected override void OnStart()
        {
            try
            {
                // Initialize core system services in order
                InitializeLogger();
                InitializeErrorHandler();
                InitializeCrashRecovery();
                InitializeRiskManager();
                InitializeTradeManager();
                
                // Initialize trading indicators
                InitializeIndicators();
                
                _logger.Info($"CoreBot | OnStart | === CoreBot Starting ===");
                _logger.Info($"CoreBot | OnStart | Bot: {BotConfig.BotName} v{BotConfig.BotVersion}");
                _logger.Info($"CoreBot | OnStart | Symbol: {Symbol.Name}");
                _logger.Info($"CoreBot | OnStart | Account: {Account.Number} ({Account.BrokerName})");
                _logger.Info($"CoreBot | OnStart | Trading Mode: {MyTradingMode}");
                _logger.Info($"CoreBot | OnStart | Active Strategy: {ActiveStrategy}");
                _logger.Info($"CoreBot | OnStart | Symbols to Trade: {SymbolsToTrade}");
                
                if (SymbolsToTrade == SymbolsToTrade.Custom && !string.IsNullOrEmpty(CustomSymbols))
                {
                    _logger.Info($"CoreBot | OnStart | Custom Symbols: {CustomSymbols}");
                }

                _logger.Info($"CoreBot | OnStart | Risk Management - Base: {RiskBase}, Size Mode: {RiskSizeMode}");
                _logger.Info($"CoreBot | OnStart | Risk Per Trade: {RiskPerTrade}%");
                _logger.Info($"CoreBot | OnStart | Trading Hours: {(UseTradingHours ? $"{TradingHourStart} to {TradingHourEnd}" : "24/7")}");
                _logger.Info($"CoreBot | OnStart | Trading Direction: {TradingDirection}");
                
                // Perform initial system health check
                var systemHealth = _errorHandler.GetSystemHealth();
                _logger.Info($"CoreBot | OnStart | Initial System Health: {systemHealth}");
                
                _logger.Info($"CoreBot | OnStart | CoreBot initialization completed successfully");
                
                // Log successful startup to error handler
                _errorHandler.HandleError(ErrorCategory.System, ErrorSeverity.Low, 
                    "CoreBot started successfully", context: "OnStart completion", attemptRecovery: false);
            }
            catch (Exception ex)
            {
                Print($"CRITICAL ERROR during OnStart: {ex.Message}");
                
                // Handle startup errors with fallback logging
                if (_errorHandler != null)
                {
                    _errorHandler.HandleException(ex, "OnStart initialization failure", attemptRecovery: true);
                }
                else if (_logger != null)
                {
                    _logger.Error($"CoreBot | OnStart | Error during OnStart - {ex.Message}", ex);
                }
                
                // Re-throw to ensure cTrader is aware of the startup failure
                throw;
            }
        }


        protected override void OnTick()
        {
            try
            {
                // Check if system is in recovery mode
                if (_crashRecovery?.IsInRecoveryMode() == true)
                {
                    return; // Skip processing during recovery
                }
                
                
                // OnTick logic will be implemented here
                // For now, just log occasionally to avoid spam
                if (Bars.TickVolumes.Count % 1000 == 0)
                {
                    _logger?.Debug($"CoreBot | OnTick | OnTick processed - Tick count: {Bars.TickVolumes.Count}");
                }
                
                // Future: Main tick processing logic will be implemented here
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleException(ex, "OnTick processing failure", attemptRecovery: true);
            }
        }

        protected override void OnBar()
        {
            try
            {
                _logger?.Debug($"CoreBot | OnBar | New bar opened at {Bars.OpenTimes.LastValue} | Open: {Bars.OpenPrices.LastValue:F5} | Close: {Bars.ClosePrices.LastValue:F5}");
                
                // Check system health before proceeding with strategy logic
                var systemHealth = _errorHandler?.GetSystemHealth() ?? SystemHealth.Healthy;
                if (systemHealth >= SystemHealth.Critical)
                {
                    _logger?.Warning($"CoreBot | OnBar | System health is {systemHealth} - limiting OnBar processing");
                    return;
                }
                
                // Execute trend following strategy
                ExecuteTrendFollowingStrategy();
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleException(ex, "OnBar processing failure", attemptRecovery: true);
            }
        }

        protected override void OnStop()
        {
            try
            {
                _logger?.Info($"CoreBot | OnStop | === CoreBot Stopping ===");
                
                // Log final system statistics
                if (_errorHandler != null)
                {
                    var systemHealth = _errorHandler.GetSystemHealth();
                    _logger?.Info($"CoreBot | OnStop | Final System Health: {systemHealth}");
                    
                    // Log error statistics
                    foreach (ErrorCategory category in Enum.GetValues(typeof(ErrorCategory)))
                    {
                        var errorCount = _errorHandler.GetErrorCount(category);
                        if (errorCount > 0)
                        {
                            _logger?.Info($"CoreBot | OnStop | Error Count [{category}]: {errorCount}");
                        }
                    }
                }
                
                // Log final account information
                _logger?.Info($"CoreBot | OnStop | Final Account Balance: {Account.Balance:F2} {Account.Asset.Name}");
                _logger?.Info($"CoreBot | OnStop | Open Positions: {Positions.Count}");
                _logger?.Info($"CoreBot | OnStop | Pending Orders: {PendingOrders.Count}");
                
                // Dispose of system services
                _crashRecovery?.Dispose();
                
                // Final log entry and cleanup
                _logger?.Info($"CoreBot | OnStop | CoreBot shutdown completed successfully");
                _logger?.Flush();
                
                Print("CoreBot stopped successfully");
            }
            catch (Exception ex)
            {
                Print($"Error during OnStop: {ex.Message}");
                
                // Final attempt to log shutdown error
                try
                {
                    _errorHandler?.HandleException(ex, "OnStop shutdown failure", attemptRecovery: false);
                    _logger?.Error($"CoreBot | OnStop | Error during OnStop - {ex.Message}", ex);
                    _logger?.Flush();
                }
                catch
                {
                    // Ignore errors during error handling in shutdown
                    Print("Failed to log shutdown error");
                }
            }
        }

        #endregion


        #region CUSTOM FUNCTIONS
        
        #region INITIALIZATION FUNCTIONS

        private void InitializeLogger()
        {
            /***
            Initializes the Logger service with user parameters
            
            Notes:
                - Creates Logger instance with user-configured settings
                - Sets up console and file logging based on parameters
                - Logs successful initialization
            ***/

            _logger = new Logger(
                robot: this,
                botName: BotConfig.BotName,
                botVersion: BotConfig.BotVersion,
                enableConsoleLogging: EnableConsoleLogging,
                enableFileLogging: EnableFileLogging,
                logFileName: LogFileName
            );
            
            _logger.Info($"CoreBot | InitializeLogger | Logger service initialized successfully");
        }

        
        private void InitializeErrorHandler()
        {
            /***
            Initializes the ErrorHandler service
            
            Notes:
                - Creates ErrorHandler instance with logger dependency
                - Sets up centralized error management
                - Logs successful initialization
            ***/
            _errorHandler = new ErrorHandler(this, _logger);
            _logger.Info($"CoreBot | InitializeErrorHandler | ErrorHandler service initialized successfully");
        }

        
        private void InitializeCrashRecovery()
        {
            /***
            Initializes the CrashRecovery service
            
            Notes:
                - Creates CrashRecovery instance with dependencies
                - Sets up system health monitoring and recovery
                - Logs successful initialization
        ***/
            _crashRecovery = new CrashRecovery(this, _logger, _errorHandler);
            _logger.Info($"CoreBot | InitializeCrashRecovery | CrashRecovery service initialized successfully");
        }

        
        private void InitializeRiskManager()
        {
            /***
            Initializes the RiskManager service
            
            Notes:
                - Creates RiskManager instance for risk calculations
                - Sets up position sizing and risk validation
                - Logs successful initialization
            ***/
            _riskManager = new RiskManager(this, _logger);
            _logger.Info($"CoreBot | InitializeRiskManager | RiskManager service initialized successfully");
        }

        
        private void InitializeTradeManager()
        {
            /***
            Initialize the TradeManager service for comprehensive trade management
            
            Notes:
                - Creates TradeManager with all required dependencies
                - Sets up comprehensive trade execution and management
                - Integrates with risk management and error handling
                - Logs successful initialization
            ***/
            _tradeManager = new TradeManager(this, _logger, _errorHandler, _riskManager);
            
            _logger.Info($"CoreBot | InitializeTradeManager | TradeManager service initialized and configured successfully");
        }

        
        private void InitializeIndicators()
        {
            /***
            Initializes the trading indicators
            
            Notes:
                - Sets up MovingAverage indicators for strategy
                - Configures fast, slow, and bias moving averages
                - Uses user-defined periods and MA type
                - Logs indicator configuration details
            ***/
            try
            {
                // Use SourceSeries if specified, otherwise default to Close prices
                var source = SourceSeries ?? Bars.ClosePrices;
                
                // Initialize Moving Averages
                _fastMA = Indicators.MovingAverage(source, FastPeriod, MAType);
                _slowMA = Indicators.MovingAverage(source, SlowPeriod, MAType);
                _biasMA = Indicators.MovingAverage(source, BiasPeriod, MAType);
                
                _logger.Info($"CoreBot | InitializeIndicators | Indicators initialized successfully:");
                _logger.Info($"CoreBot | InitializeIndicators | Fast MA: {FastPeriod} period {MAType}");
                _logger.Info($"CoreBot | InitializeIndicators | Slow MA: {SlowPeriod} period {MAType}");
                _logger.Info($"CoreBot | InitializeIndicators | Bias MA: {BiasPeriod} period {MAType}");
                _logger.Info($"CoreBot | InitializeIndicators | Source: {(SourceSeries != null ? "Custom" : "Close prices")}");
            }
            catch (Exception ex)
            {
                _logger.Error($"CoreBot | InitializeIndicators | Failed to initialize indicators - {ex.Message}", ex);
                _errorHandler?.HandleException(ex, "Indicator initialization failure", attemptRecovery: false);
                throw;
            }
        }

        #endregion

        #region STRATEGY FUNCTIONS
        
        private void ExecuteTrendFollowingStrategy()
        {
            /***
            Executes the simple trend following strategy
            
            Notes:
                - Implements MA crossover trend following logic
                - Checks for buy and sell signals based on MA relationships
                - Executes trades when valid signals are detected
                - Includes comprehensive logging of strategy decisions
            ***/
            try
            {
                // Ensure we have enough bars for calculation
                if (Bars.Count < Math.Max(Math.Max(FastPeriod, SlowPeriod), BiasPeriod) + 1)
                {
                    _logger?.Debug($"CoreBot | ExecuteTrendFollowingStrategy | Not enough bars for strategy calculation. Current: {Bars.Count}, Required: {Math.Max(Math.Max(FastPeriod, SlowPeriod), BiasPeriod) + 1}");
                    return;
                }

                // Get current and previous MA values
                var currentFastMA = _fastMA.Result.LastValue;
                var currentSlowMA = _slowMA.Result.LastValue;
                var currentBiasMA = _biasMA.Result.LastValue;
                
                var previousFastMA = _fastMA.Result.Last(1);
                var previousSlowMA = _slowMA.Result.Last(1);

                _logger?.Debug($"CoreBot | ExecuteTrendFollowingStrategy | MA Values - Fast: {currentFastMA:F5}/{previousFastMA:F5}, Slow: {currentSlowMA:F5}/{previousSlowMA:F5}, Bias: {currentBiasMA:F5}");

                // Buy signal: previousFastMa < previousSlowMa && currentFastMa > currentSlowMa && currentSlowMa > currentBiasMa
                bool buySignal = previousFastMA < previousSlowMA && 
                                currentFastMA > currentSlowMA && 
                                currentSlowMA > currentBiasMA;

                // Sell signal: previousFastMa > previousSlowMa && currentFastMa < currentSlowMa && currentSlowMa < currentBiasMa
                bool sellSignal = previousFastMA > previousSlowMA && 
                                 currentFastMA < currentSlowMA && 
                                 currentSlowMA < currentBiasMA;

                _logger?.Debug($"CoreBot | ExecuteTrendFollowingStrategy | Signals - Buy: {buySignal}, Sell: {sellSignal}");

                // Execute buy signal
                if (buySignal)
                {
                   ExecuteOrder(TradeType.Buy);
                }

                // Execute sell signal
                if (sellSignal)
                {
                    ExecuteOrder(TradeType.Sell);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"CoreBot | ExecuteTrendFollowingStrategy | Error in trend following strategy execution - {ex.Message}", ex);
                _errorHandler?.HandleException(ex, "Strategy execution failure", attemptRecovery: true);
            }
        }

        #endregion

        #region TRADE FUNCTIONS

         private void ExecuteOrder(TradeType tradeType)
            {
                /***
                Executes a market order using TradeManager with comprehensive risk management
                
                Args:
                    tradeType: The direction of the trade (Buy or Sell)
                    
                Notes:
                    - Uses TradeManager for comprehensive trade execution
                    - Applies all risk management parameters
                    - Includes full validation and error handling
                    - Logs execution results and any failures
                    ***/
                try
                {
                    _logger?.Info($"CoreBot | ExecuteOrder | Attempting to execute {tradeType} order using TradeManager");

                    // Execute the trade using TradeManager with all RiskManager parameters
                    var result = _tradeManager.ExecuteTrade(
                        tradeType,
                        OrderLabel,
                        UseTradingHours,
                        TradingHourStart,
                        TradingHourEnd,
                        TradingDirection,
                        MaxSpreadInPips,
                        RiskSizeMode,
                        DefaultPositionSize,
                        RiskPerTrade,
                        FixedRiskAmount,
                        RiskBase,
                        FixedRiskBalance,
                        StopLossMode,
                        DefaultStopLoss,
                        TakeProfitMode,
                        DefaultTakeProfit,
                        StopLossMultiplier,
                        TakeProfitMultiplier,
                        ADRRatio,
                        ADRPeriod,
                        ATRPeriod,
                        LotIncrease,
                        BalanceIncrease
                    );

                    if (result.IsSuccessful)
                    {
                        _logger?.Info($"CoreBot | ExecuteOrder | Order executed successfully via TradeManager. Position ID: {result.Position.Id}");
                        _logger?.Info($"CoreBot | ExecuteOrder | Execution details - Entry: {result.ExecutionPrice}, SL: {result.StopLossPrice}, TP: {result.TakeProfitPrice}");
                    }
                    else
                    {
                        _logger?.Error($"CoreBot | ExecuteOrder | Order execution failed via TradeManager: {result.ErrorMessage}");
                        _errorHandler?.HandleError(ErrorCategory.Trading, ErrorSeverity.Medium, 
                            $"TradeManager execution failed: {result.ErrorMessage}", context: $"TradeType: {tradeType}", attemptRecovery: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"CoreBot | ExecuteOrder | Error executing {tradeType} order via TradeManager - {ex.Message}", ex);
                    _errorHandler?.HandleException(ex, "TradeManager execution failure", attemptRecovery: true);
                }
            }
        
        #endregion
        #endregion

    }
}
