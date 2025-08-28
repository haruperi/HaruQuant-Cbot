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
        
        // Moving Average indicators for trend following strategy
        private MovingAverage _fastMA;
        private MovingAverage _slowMA;
        private MovingAverage _biasMA;

        protected override void OnStart()
        {
            try
            {
                // Initialize core system services in order
                InitializeLogger();
                InitializeErrorHandler();
                InitializeCrashRecovery();
                InitializeRiskManager();
                
                // Initialize trading indicators
                InitializeIndicators();
                
                _logger.Info("=== CoreBot Starting ===");
                _logger.Info($"Bot: {BotConfig.BotName} v{BotConfig.BotVersion}");
                _logger.Info($"Symbol: {Symbol.Name}");
                _logger.Info($"Account: {Account.Number} ({Account.BrokerName})");
                _logger.Info($"Trading Mode: {MyTradingMode}");
                _logger.Info($"Active Strategy: {ActiveStrategy}");
                _logger.Info($"Symbols to Trade: {SymbolsToTrade}");
                
                if (SymbolsToTrade == SymbolsToTrade.Custom && !string.IsNullOrEmpty(CustomSymbols))
                {
                    _logger.Info($"Custom Symbols: {CustomSymbols}");
                }

                _logger.Info($"Risk Management - Base: {RiskBase}, Size Mode: {RiskSizeMode}");
                _logger.Info($"Risk Per Trade: {RiskPerTrade}%");
                _logger.Info($"Trading Hours: {(UseTradingHours ? $"{TradingHourStart} to {TradingHourEnd}" : "24/7")}");
                _logger.Info($"Trading Direction: {TradingDirection}");
                
                // Perform initial system health check
                var systemHealth = _errorHandler.GetSystemHealth();
                _logger.Info($"Initial System Health: {systemHealth}");
                
                _logger.Info("CoreBot initialization completed successfully");
                
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
                    _logger.Error("Error during OnStart", ex);
                }
                
                // Re-throw to ensure cTrader is aware of the startup failure
                throw;
            }
        }

        /// <summary>
        /// Initializes the Logger service with user parameters
        /// </summary>
        private void InitializeLogger()
        {
            _logger = new Logger(
                robot: this,
                botName: BotConfig.BotName,
                botVersion: BotConfig.BotVersion,
                enableConsoleLogging: EnableConsoleLogging,
                enableFileLogging: EnableFileLogging,
                logFileName: LogFileName
            );
            
            _logger.Info("Logger service initialized successfully");
        }

        /// <summary>
        /// Initializes the ErrorHandler service
        /// </summary>
        private void InitializeErrorHandler()
        {
            _errorHandler = new ErrorHandler(this, _logger);
            _logger.Info("ErrorHandler service initialized successfully");
        }

        /// <summary>
        /// Initializes the CrashRecovery service
        /// </summary>
        private void InitializeCrashRecovery()
        {
            _crashRecovery = new CrashRecovery(this, _logger, _errorHandler);
            _logger.Info("CrashRecovery service initialized successfully");
        }

        /// <summary>
        /// Initializes the RiskManager service
        /// </summary>
        private void InitializeRiskManager()
        {
            _riskManager = new RiskManager(this, _logger);
            _logger.Info("RiskManager service initialized successfully");
        }

        /// <summary>
        /// Initializes the trading indicators
        /// </summary>
        private void InitializeIndicators()
        {
            try
            {
                // Use SourceSeries if specified, otherwise default to Close prices
                var source = SourceSeries ?? Bars.ClosePrices;
                
                // Initialize Moving Averages
                _fastMA = Indicators.MovingAverage(source, FastPeriod, MAType);
                _slowMA = Indicators.MovingAverage(source, SlowPeriod, MAType);
                _biasMA = Indicators.MovingAverage(source, BiasPeriod, MAType);
                
                _logger.Info($"Indicators initialized successfully:");
                _logger.Info($"  Fast MA: {FastPeriod} period {MAType}");
                _logger.Info($"  Slow MA: {SlowPeriod} period {MAType}");
                _logger.Info($"  Bias MA: {BiasPeriod} period {MAType}");
                _logger.Info($"  Source: {(SourceSeries != null ? "Custom" : "Close prices")}");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize indicators", ex);
                _errorHandler?.HandleException(ex, "Indicator initialization failure", attemptRecovery: false);
                throw;
            }
        }

        /// <summary>
        /// Executes the simple trend following strategy
        /// </summary>
        private void ExecuteTrendFollowingStrategy()
        {
            try
            {
                // Ensure we have enough bars for calculation
                if (Bars.Count < Math.Max(Math.Max(FastPeriod, SlowPeriod), BiasPeriod) + 1)
                {
                    _logger?.Debug($"Not enough bars for strategy calculation. Current: {Bars.Count}, Required: {Math.Max(Math.Max(FastPeriod, SlowPeriod), BiasPeriod) + 1}");
                    return;
                }

                // Get current and previous MA values
                var currentFastMA = _fastMA.Result.LastValue;
                var currentSlowMA = _slowMA.Result.LastValue;
                var currentBiasMA = _biasMA.Result.LastValue;
                
                var previousFastMA = _fastMA.Result.Last(1);
                var previousSlowMA = _slowMA.Result.Last(1);

                _logger?.Debug($"MA Values - Fast: {currentFastMA:F5}/{previousFastMA:F5}, Slow: {currentSlowMA:F5}/{previousSlowMA:F5}, Bias: {currentBiasMA:F5}");

                // Check trading hours if enabled
                if (UseTradingHours && !_riskManager.IsWithinTradingHours(UseTradingHours, TradingHourStart, TradingHourEnd))
                {
                    _logger?.Debug("Outside trading hours - no new trades");
                    return;
                }

                // Check if we already have positions
                var buyPositions = Positions.FindAll(OrderLabel, Symbol.Name, TradeType.Buy);
                var sellPositions = Positions.FindAll(OrderLabel, Symbol.Name, TradeType.Sell);

                // Buy signal: previousFastMa < previousSlowMa && currentFastMa > currentSlowMa && currentSlowMa > currentBiasMa
                bool buySignal = previousFastMA < previousSlowMA && 
                                currentFastMA > currentSlowMA && 
                                currentSlowMA > currentBiasMA;

                // Sell signal: previousFastMa > previousSlowMa && currentFastMa < currentSlowMa && currentSlowMa < currentBiasMa
                bool sellSignal = previousFastMA > previousSlowMA && 
                                 currentFastMA < currentSlowMA && 
                                 currentSlowMA < currentBiasMA;

                _logger?.Debug($"Signals - Buy: {buySignal}, Sell: {sellSignal}");

                // Execute buy signal
                if (buySignal && (TradingDirection == TradingDirection.Both || TradingDirection == TradingDirection.Buy))
                {
                    if (buyPositions.Length < MaxBuyTrades)
                    {
                        ExecuteMarketOrder(TradeType.Buy);
                    }
                    else
                    {
                        _logger?.Debug($"Maximum buy trades reached: {buyPositions.Length}/{MaxBuyTrades}");
                    }
                }

                // Execute sell signal
                if (sellSignal && (TradingDirection == TradingDirection.Both || TradingDirection == TradingDirection.Sell))
                {
                    if (sellPositions.Length < MaxSellTrades)
                    {
                        ExecuteMarketOrder(TradeType.Sell);
                    }
                    else
                    {
                        _logger?.Debug($"Maximum sell trades reached: {sellPositions.Length}/{MaxSellTrades}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("Error in trend following strategy execution", ex);
                _errorHandler?.HandleException(ex, "Strategy execution failure", attemptRecovery: true);
            }
        }



        /// <summary>
        /// Executes a market order with proper risk management
        /// </summary>
        private void ExecuteMarketOrder(TradeType tradeType)
        {
            try
            {
                // Calculate position size
                var volumeInUnits = _riskManager.CalculatePositionSize(
                    RiskSizeMode, DefaultPositionSize, RiskPerTrade, DefaultStopLoss, FixedRiskAmount, RiskBase, FixedRiskBalance);
                
                // Calculate stop loss and take profit
                var (stopLoss, takeProfit) = _riskManager.CalculateStopLossAndTakeProfit(
                    tradeType, StopLossMode, TakeProfitMode, DefaultStopLoss, DefaultTakeProfit);

                _logger?.Info($"Executing {tradeType} order:");
                _logger?.Info($"  Volume: {volumeInUnits} units");
                _logger?.Info($"  Stop Loss: {stopLoss:F5}");
                _logger?.Info($"  Take Profit: {takeProfit:F5}");

                // Execute the trade
                var result = ExecuteMarketOrder(tradeType, Symbol.Name, volumeInUnits, OrderLabel, stopLoss, takeProfit);

                if (result.IsSuccessful)
                {
                    _logger?.Info($"Order executed successfully. Position ID: {result.Position.Id}");
                }
                else
                {
                    _logger?.Error($"Order execution failed: {result.Error}");
                    _errorHandler?.HandleError(ErrorCategory.Trading, ErrorSeverity.Medium, 
                        $"Order execution failed: {result.Error}", context: $"TradeType: {tradeType}", attemptRecovery: false);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error executing {tradeType} order", ex);
                _errorHandler?.HandleException(ex, "Order execution failure", attemptRecovery: true);
            }
        }



        protected override void OnTick()
        {
            try
            {
                // OnTick logic will be implemented here
                // For now, just log occasionally to avoid spam
                if (Bars.TickVolumes.Count % 1000 == 0)
                {
                    _logger?.Debug($"OnTick processed - Tick count: {Bars.TickVolumes.Count}");
                    
                    // Check if system is in recovery mode
                    if (_crashRecovery?.IsInRecoveryMode() == true)
                    {
                        _logger?.Debug("System in recovery mode - OnTick processing limited");
                        return;
                    }
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
                _logger?.Debug($"OnBar - New bar opened at {Bars.OpenTimes.LastValue} | Open: {Bars.OpenPrices.LastValue:F5} | Close: {Bars.ClosePrices.LastValue:F5}");
                
                // Check system health before proceeding with strategy logic
                var systemHealth = _errorHandler?.GetSystemHealth() ?? SystemHealth.Healthy;
                if (systemHealth >= SystemHealth.Critical)
                {
                    _logger?.Warning($"System health is {systemHealth} - limiting OnBar processing");
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
                _logger?.Info("=== CoreBot Stopping ===");
                
                // Log final system statistics
                if (_errorHandler != null)
                {
                    var systemHealth = _errorHandler.GetSystemHealth();
                    _logger?.Info($"Final System Health: {systemHealth}");
                    
                    // Log error statistics
                    foreach (ErrorCategory category in Enum.GetValues(typeof(ErrorCategory)))
                    {
                        var errorCount = _errorHandler.GetErrorCount(category);
                        if (errorCount > 0)
                        {
                            _logger?.Info($"Error Count [{category}]: {errorCount}");
                        }
                    }
                }
                
                // Log final account information
                _logger?.Info($"Final Account Balance: {Account.Balance:F2} {Account.Asset.Name}");
                _logger?.Info($"Open Positions: {Positions.Count}");
                _logger?.Info($"Pending Orders: {PendingOrders.Count}");
                
                // Dispose of system services
                _crashRecovery?.Dispose();
                
                // Final log entry and cleanup
                _logger?.Info("CoreBot shutdown completed successfully");
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
                    _logger?.Error("Error during OnStop", ex);
                    _logger?.Flush();
                }
                catch
                {
                    // Ignore errors during error handling in shutdown
                    Print("Failed to log shutdown error");
                }
            }
        }
    }
}
