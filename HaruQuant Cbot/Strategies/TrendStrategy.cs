using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Robots.Utils; // For Logger, Enums etc.

namespace cAlgo.Robots.Strategies
{
    public class TrendStrategy : StrategyBase
    {
        private MovingAverage _fastMa;
        private MovingAverage _slowMa;
        private MovingAverage _biasMa;

        public TrendStrategy(Corebot robot) : base(robot, "TrendStrategy")
        {
            // The base constructor already initializes Robot and Logger
        }

        public override void Initialize()
        {
            // Initialize indicators using parameters from StrategyBase (which are from CoreBot)
            _fastMa = Robot.Indicators.MovingAverage(SourceSeries, FastPeriod, MAType);
            _slowMa = Robot.Indicators.MovingAverage(SourceSeries, SlowPeriod, MAType);
            _biasMa = Robot.Indicators.MovingAverage(SourceSeries, BiasPeriod, MAType);

            Logger.Info("TrendStrategy initialized with Fast MA ({FastPeriod}), Slow MA ({SlowPeriod}), Bias MA ({BiasPeriod}).");
        }

        public override void OnTick()
        {
            // Trend strategies typically don't act on every tick to avoid noise.
            // Logic will be primarily in OnBar.
        }

        public override void OnBar()
        {

            // Ensure enough data for MAs
            // Index 0 is the most recent **completed** bar. Index 1 is the one before that.
            // For "current" (forming) bar values, MA.Last(0) or MA.Result.LastValue
            // For "previous" (last completed) bar values, MA.Last(1) or MA.Result[MarketSeries.Close.Count - 2]
            // We need at least 3 bars to have previous (index 2) and current (index 1) values for MAs.
            // MarketSeries.Count - 1 is the current bar index.
            // MarketSeries.Count - 2 is the last closed bar index (previous).
            // MarketSeries.Count - 3 is the bar before the last closed bar (previous previous).

            if (Bars.Count < BiasPeriod || Bars.Count < SlowPeriod || Bars.Count < FastPeriod || Bars.Count < 3)
            {
                Logger.Debug("Not enough data to evaluate MA crossover.");
                return;
            }
            
            // Get MA values for the last two completed bars
            // Current completed bar (index 1 from end of MA result which is aligned with MarketSeries)
            double currentFastMa = _fastMa.Result.Last(1);
            double currentSlowMa = _slowMa.Result.Last(1);
            double currentBiasMa = _biasMa.Result.Last(1);

            // Previous completed bar (index 2 from end of MA result)
            double previousFastMa = _fastMa.Result.Last(2);
            double previousSlowMa = _slowMa.Result.Last(2);
            // double previousBiasMa = _biasMa.Result.Last(2); // Not used in current logic but shown for completeness


            // Buy Condition
            if (previousFastMa < previousSlowMa && currentFastMa > currentSlowMa && currentSlowMa > currentBiasMa)
            {
                Logger.Info("BUY signal detected. Placeholder for ExecuteMarketOrder (BUY).");
                ExecuteTrade(TradeType.Buy, "TrendStrategy Buy");
            }

            // Sell Condition
            if (previousFastMa > previousSlowMa && currentFastMa < currentSlowMa && currentSlowMa < currentBiasMa)
            {
                Logger.Info("SELL signal detected. Placeholder for ExecuteMarketOrder (SELL).");
                ExecuteTrade(TradeType.Sell, "TrendStrategy Sell");
            }

            
        }

        public override void OnStop()
        {
            Logger.Info("TrendStrategy stopped.");
            // Any cleanup specific to this strategy can go here.
        }
    }
} 