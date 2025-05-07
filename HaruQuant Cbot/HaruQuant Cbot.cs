using System;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Robots.Strategies;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, AddIndicators = true)]
    public class HaruQuantCbot : Robot
    {
        [Parameter("Quantity (Lots)", Group = "Volume", DefaultValue = 1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        [Parameter("Risk Per Trade", Group = "Risk Management", DefaultValue = 1, MinValue = 0.01, Step = 0.01, MaxValue = 100)]
        public double RiskPerTrade { get; set; }

        [Parameter("MA Type", Group = "Moving Average", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType { get; set; }

        [Parameter("Source", Group = "Moving Average")]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Fast Period", Group = "Moving Average", DefaultValue = 12)]
        public int FastPeriod { get; set; }

        [Parameter("Slow Period", Group = "Moving Average", DefaultValue = 48)]
        public int SlowPeriod { get; set; }

        [Parameter("Bias Period", Group = "Moving Average", DefaultValue = 288)]
        public int BiasPeriod { get; set; }

        [Parameter("Stop Loss", Group = "Risk Management", DefaultValue = 20)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit", Group = "Risk Management", DefaultValue = 40)]
        public int TakeProfit { get; set; }

        private TrendStrategy _trendStrategy;
        private const string label = "Simple Naive Trend cBot";

        protected override void OnStart()
        {
            // Print startup message
            Print("HaruQuant Cbot started successfully!");
            Print($"Trading on {Symbol.Name} with timeframe {TimeFrame}");

            _trendStrategy = new TrendStrategy(
                this,
                MAType,
                SourceSeries,
                FastPeriod,
                SlowPeriod,
                BiasPeriod,
                RiskPerTrade,
                StopLoss,
                TakeProfit,
                label
            );
        }

        protected override void OnTick()
        {
            // We don't need to do anything on tick for this strategy
        }
        
        protected override void OnBar()
        {
            _trendStrategy.OnBar();
        }

        protected override void OnStop()
        {
            // Print shutdown message
            Print("HaruQuant Cbot stopped.");
        }
    }
} 