using System;
using System.IO;
// using System.IO.IsolatedStorage; // No longer directly used here
using System.Linq;
// using System.Text.Json; // No longer directly used here
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Robots.Utils; // This now brings in BotState, ErrorHandlerService, and BotErrorException
using cAlgo.Robots.Strategies; // <--- Updated to use .Strategies namespace


namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess, AddIndicators = true)]
    public class Corebot : Robot
    {
        #region Parameters of CBot

        #region Identity
        [Parameter(BotConfig.BotName + " " + BotConfig.BotVersion, Group = "IDENTITY", DefaultValue = "https://haruperi.ltd/trading/")]
        public string ProductInfo { get; set; }

        [Parameter("Preset information", Group = "IDENTITY", DefaultValue = "XAUUSD Range5 | 01.01.2024 to 29.04.2024 | $1000")]
        public string PresetInfo { get; set; }
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


        #endregion

        // ------------------------------------Trading Settings------------------------------------

        #region Trading Settings
        [Parameter("Use Trading Hours", Group = "TRADING SETTINGS", DefaultValue = false)]
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
        public int SlippageInPips { get; set; }

        [Parameter("Max Spread (Pips)", Group = "TRADING SETTINGS", DefaultValue = 5, MinValue = 0)]
        public int MaxSpreadInPips { get; set; }
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

        // ------------------------------------Global Settings------------------------------------

        #region Global variables
        // private const string HowToUseText = "How to use:\nCtrl + Left Mouse Button - Draw Breakout line\nShift + Left Mouse Button - Draw Retracement line";
        // private const string HowToUseObjectName = "LinesTraderText";
        
        // private TrendStrategy _trendStrategy;
        // private MeanReversion _meanReversionStrategy;
        // private Swingline _swinglineStrategy;
        // private StrategyBase _activeStrategy; // This was for the enum, let's rename for clarity
        private StrategyBase _activeStrategyInstance; // To hold the instantiated strategy
        private Logger _logger;
        private BotState _botState;
        private ErrorHandlerService _errorHandler; // Added ErrorHandlerService
        private const string StateFileName = "BotState.json"; // File name for isolated storage

        #endregion

        #endregion

// ------------------------------------Standard event handlers------------------------------------
        #region Standard event handlers
        protected override void OnStart()
        {
            _logger = new Logger(this, BotConfig.BotName, BotConfig.BotVersion);
            _errorHandler = new ErrorHandlerService(_logger); // Initialize ErrorHandlerService
            
            // Load state using the static method in BotState
            _botState = BotState.Load(_logger, StateFileName);

            _logger.Info($"{BotConfig.BotName} v{BotConfig.BotVersion} started successfully!");
            
            if (_botState.ActiveTradeLabels.Any())
            {
                _logger.Info($"Restored state with {_botState.ActiveTradeLabels.Count} active trade labels.");
            }

            // Strategy Initialization
            // Switch statement now uses the locally defined Strategy enum
            switch (ActiveStrategy) 
            {
                case Strategy.TrendFollowing:
                    _activeStrategyInstance = new TrendStrategy(this);
                    _logger.Info("TrendFollowing strategy selected.");
                    break;
                case Strategy.MeanReversion:
                    // _activeStrategyInstance = new MeanReversionStrategy(this); 
                    _logger.Info("MeanReversion strategy selected (Not implemented).");
                    break;
                default:
                    _logger.Warning($"Strategy '{ActiveStrategy}' is not yet implemented or recognized. No strategy will be active.");
                    break;
            }

            if (_activeStrategyInstance != null)
            {
                try
                {
                    _activeStrategyInstance.Initialize(); // Call Initialize on the strategy instance
                    _logger.Info($"Active strategy '{_activeStrategyInstance.GetType().Name}' initialized successfully.");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error initializing strategy '{_activeStrategyInstance.GetType().Name}': {ex.Message}", ex);
                    _activeStrategyInstance = null; // Prevent further calls if initialization failed
                }
            }
            else
            {
                _logger.Warning("No active strategy instance was created. Bot will be idle.");
            }
        }

        protected override void OnTick()
        {
            _activeStrategyInstance?.OnTick();
        }
        
        protected override void OnBar()
        {
            _activeStrategyInstance?.OnBar();
        }

        protected override void OnStop()
        {
            _activeStrategyInstance?.OnStop();

            if (_botState != null)
            {
                // Ensure _botState is populated with the final data before saving
                _botState.ActiveTradeLabels.Clear();
                foreach (var position in Positions)
                {
                    if (!string.IsNullOrEmpty(position.Label))
                    {
                        _botState.ActiveTradeLabels.Add(position.Label);
                    }
                }
                // _botState.CustomStrategyParameter = currentStrategyValueToSave;

                _botState.Save(_logger, StateFileName);
            }
            _logger.Info($"{BotConfig.BotName} shutdown.");
        }

        // Removed SaveState() and LoadState() methods from CoreBot

        // Optional: If you save periodically in OnBar, you might have a method like this
        // private void UpdateStateBeforePeriodicSave()
        // {
        //     // Example: Update any dynamic properties of _botState here
        //     // _botState.SomeDynamicValue = GetCurrentDynamicValue();
        // }

        public string[] GetSymbolsToTrade()
        {
            switch (SymbolsToTrade)
            {
                case Utils.SymbolsToTrade.Forex:
                    return Utils.BotConfig.ForexSymbols;
                case Utils.SymbolsToTrade.Commodities:
                    return Utils.BotConfig.CommoditySymbols;
                case Utils.SymbolsToTrade.Indices:
                    return Utils.BotConfig.IndexSymbols;
                case Utils.SymbolsToTrade.All:
                    return Utils.BotConfig.ForexSymbols
                        .Concat(Utils.BotConfig.CommoditySymbols)
                        .Concat(Utils.BotConfig.IndexSymbols)
                        .ToArray();
                case Utils.SymbolsToTrade.Custom:
                    if (string.IsNullOrWhiteSpace(CustomSymbols))
                    {
                        return new[] { Symbol.Name };
                    }
                    return CustomSymbols.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToArray();
                default:
                    return new[] { Symbol.Name };
            }
        }
        #endregion
    }
} 