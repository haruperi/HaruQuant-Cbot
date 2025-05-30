using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.API.Collections;
using HaruQuantCbot.Trading;
using cAlgo.Robots.Utils; // For Logger, BotConfig etc. if needed by strategies directly
// Assuming CoreBot is in cAlgo.Robots namespace
// If CoreBot is in a different namespace, adjust accordingly.
// For now, let's assume it's in cAlgo.Robots as per the provided CoreBot.cs

namespace cAlgo.Robots.Strategies
{
    public abstract class StrategyBase
    {
        protected readonly Corebot Robot;
        protected readonly Logger Logger; // Each strategy can have its own logger instance or use Robot's
        protected readonly RiskManager RiskManager;
        protected readonly TradeManager TradeManager;

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
        protected double SlippageInPips => Robot.SlippageInPips;
        protected double MaxSpreadInPips => Robot.MaxSpreadInPips;
        #endregion
        
        #region cTrader API Accessors
        protected Symbol Symbol => Robot.Symbol;
        protected Bars Bars => Robot.Bars;
        protected TimeFrame TimeFrame => Robot.TimeFrame;
        protected string[] ActiveSymbols => Robot.GetSymbolsToTrade();
        #endregion


        protected StrategyBase(Corebot robot, string strategyName)
        {
            Robot = robot;
            Logger = new Logger(robot, strategyName, BotConfig.BotVersion);
            RiskManager = new RiskManager(robot);
            TradeManager = new TradeManager(robot);
        }

        public abstract void Initialize();
        public abstract void OnTick();
        public abstract void OnBar();
        public abstract void OnStop();

    }
} 