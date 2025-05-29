using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Robots.Utils; // For Logger, BotConfig etc. if needed by strategies directly
// Assuming CoreBot is in cAlgo.Robots namespace
// If CoreBot is in a different namespace, adjust accordingly.
// For now, let's assume it's in cAlgo.Robots as per the provided CoreBot.cs

namespace cAlgo.Robots.Strategy
{
    public abstract class StrategyBase
    {
        protected readonly Corebot Robot;
        protected readonly Logger Logger; // Each strategy can have its own logger instance or use Robot's

        #region Strategy Parameters (mirrored from CoreBot)
        protected TradingMode MyTradingMode => Robot.MyTradingMode;
        protected Strategy ActiveStrategyType => Robot.ActiveStrategy; // To know which strategy type is configured
        protected MovingAverageType MAType => Robot.MAType;
        protected DataSeries SourceSeries => Robot.SourceSeries;
        protected int FastPeriod => Robot.FastPeriod;
        protected int SlowPeriod => Robot.SlowPeriod;
        protected int BiasPeriod => Robot.BiasPeriod;
        protected int RSIPeriod => Robot.RSIPeriod;
        protected int RSIOverboughtLevel => Robot.RSIOverboughtLevel;
        protected int RSIOversoldLevel => Robot.RSIOversoldLevel;
        #endregion

        #region Risk Management Parameters (mirrored from CoreBot)
        protected RiskBase RiskBase => Robot.RiskBase;
        protected RiskDefaultSize RiskSizeMode => Robot.RiskSizeMode;
        protected StopLossMode StopLossMode => Robot.StopLossMode;
        protected TakeProfitMode TakeProfitMode => Robot.TakeProfitMode;
        protected double RiskPerTrade => Robot.RiskPerTrade;
        protected double FixedRiskBalance => Robot.FixedRiskBalance;
        protected double FixedRiskAmount => Robot.FixedRiskAmount;
        protected double BalanceIncrease => Robot.BalanceIncrease;
        protected double LotIncrease => Robot.LotIncrease;
        protected double LotDecreaseRatio => Robot.LotDecreaseRatio;
        protected double DefaultPositionSize => Robot.DefaultPositionSize;
        protected int DefaultStopLoss => Robot.DefaultStopLoss;
        protected int DefaultTakeProfit => Robot.DefaultTakeProfit;
        protected int ATRPeriod => Robot.ATRPeriod;
        protected int ADRPeriod => Robot.ADRPeriod;
        protected double StopLossMultiplier => Robot.StopLossMultiplier;
        protected double TakeProfitMultiplier => Robot.TakeProfitMultiplier;
        protected ManageTrade ManageTrade => Robot.ManageTrade;
        protected bool UseTrailingStop => Robot.UseTrailingStop;
        protected int TrailDistance => Robot.TrailDistance;
        protected int TrailFrom => Robot.TrailFrom;
        protected double ADRRatio => Robot.ADRRatio;
        protected bool HideStopLoss => Robot.HideStopLoss;
        protected bool HideTakeProfit => Robot.HideTakeProfit;
        #endregion

        #region Trading Settings Parameters (mirrored from CoreBot)
        protected bool UseTradingHours => Robot.UseTradingHours;
        protected HourOfDay TradingHourStart => Robot.TradingHourStart;
        protected HourOfDay TradingHourEnd => Robot.TradingHourEnd;
        protected TradingDirection TradingDirection => Robot.TradingDirection;
        protected string OrderLabel => Robot.OrderLabel;
        protected int SlippageInPips => Robot.SlippageInPips;
        protected int MaxSpreadInPips => Robot.MaxSpreadInPips;
        #endregion
        
        #region cTrader API Accessors
        protected Symbol Symbol => Robot.Symbol;
        protected MarketSeries MarketSeries => Robot.MarketSeries;
        protected TimeFrame TimeFrame => Robot.TimeFrame;
        protected Account Account => Robot.Account;
        protected Positions Positions => Robot.Positions;
        protected PendingOrders PendingOrders => Robot.PendingOrders;
        protected Server Server => Robot.Server;
        protected Trade Trade => Robot.Trade; // For trade execution methods
        #endregion

        protected StrategyBase(Corebot robot, string strategyName)
        {
            Robot = robot;
            // It's good practice for each strategy to have its own logger context if needed,
            // or you can pass the CoreBot's logger.
            // For simplicity, let's create a new logger instance for the strategy.
            // This assumes Logger can be instantiated this way or you adjust as needed.
            Logger = new Logger(robot, strategyName, BotConfig.BotVersion); 
        }

        public abstract void Initialize();
        public abstract void OnTick();
        public abstract void OnBar();
        public abstract void OnStop();
        
        // Helper method to print messages via the Robot instance
        protected void Print(object message)
        {
            Robot.Print(message);
        }

        // Example of a common utility method that might be used by strategies
        protected virtual bool IsTradingAllowed()
        {
            if (UseTradingHours)
            {
                var currentTime = Server.Time.TimeOfDay;
                var startTime = new TimeSpan((int)TradingHourStart, 0, 0);
                var endTime = new TimeSpan((int)TradingHourEnd, 0, 0);

                if (startTime <= endTime) // e.g. 02:00 to 23:00
                {
                    if (currentTime < startTime || currentTime >= endTime)
                    {
                        return false;
                    }
                }
                else // e.g. 22:00 to 05:00 (overnight)
                {
                    if (currentTime < startTime && currentTime >= endTime)
                    {
                        return false;
                    }
                }
            }
            
            // Check Max Spread
            if (MaxSpreadInPips > 0 && Symbol.Spread / Symbol.PipSize > MaxSpreadInPips)
            {
                Logger.Debug($"Spread ({Symbol.Spread / Symbol.PipSize} pips) exceeds MaxSpreadInPips ({MaxSpreadInPips} pips). Trading not allowed.");
                return false;
            }

            return true;
        }
        
        // Potentially more common methods:
        // protected abstract TradeResult ExecuteMarketOrder(...);
        // protected abstract TradeResult CreatePendingOrder(...);
        // protected abstract void ManageOpenPositions();
        // protected abstract bool CheckEntrySignal(TradeType tradeType);
    }
} 