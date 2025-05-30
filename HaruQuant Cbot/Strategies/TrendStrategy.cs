using System;
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
        private string[] _symbolsToTrade;

        public TrendStrategy(Corebot robot) : base(robot, "TrendStrategy")
        {
            // The base constructor already initializes Robot and Logger
        }

        public override void Initialize()
        {
            // Get the list of symbols to trade
            _symbolsToTrade = Robot.GetSymbolsToTrade();
            Logger.Info($"Initialized with {_symbolsToTrade.Length} symbols to trade.");

            // Initialize indicators using parameters from StrategyBase (which are from CoreBot)
            _fastMa = Robot.Indicators.MovingAverage(SourceSeries, FastPeriod, MAType);
            _slowMa = Robot.Indicators.MovingAverage(SourceSeries, SlowPeriod, MAType);
            _biasMa = Robot.Indicators.MovingAverage(SourceSeries, BiasPeriod, MAType);

            Logger.Info("TrendStrategy initialized with Fast MA ({FastPeriod}), Slow MA ({SlowPeriod}), Bias MA ({BiasPeriod}).");
        }

        public override void OnTick()
        {
            // Trend strategies typically don't act on every tick to avoid noise. Logic will be primarily in OnBar.
        }

        public override void OnBar()
        {
            foreach (var symbolName in _symbolsToTrade)
            {
                try
                {
                    //var symbol = Robot.Symbols.GetSymbol(symbolName);
                    var bars = Robot.MarketData.GetBars(TimeFrame, symbolName);

                    // Ensure enough data for MAs
                    if (bars.Count < BiasPeriod || bars.Count < SlowPeriod || bars.Count < FastPeriod || bars.Count < 3)
                    {
                        Logger.Debug($"Not enough data to evaluate MA crossover for {symbolName}.");
                        continue;
                    }

                    // Get MA values for the last two completed bars
                    double currentFastMa = _fastMa.Result.Last(1);
                    double currentSlowMa = _slowMa.Result.Last(1);
                    double currentBiasMa = _biasMa.Result.Last(1);

                    // Previous completed bar
                    double previousFastMa = _fastMa.Result.Last(2);
                    double previousSlowMa = _slowMa.Result.Last(2);

                    // Buy Condition
                    if (previousFastMa < previousSlowMa && currentFastMa > currentSlowMa && currentSlowMa > currentBiasMa)
                    {
                        Logger.Info($"BUY signal detected for {symbolName}.");
                        ExecuteTrade(TradeType.Buy, symbolName, "TrendStrategy Buy");
                    }

                    // Sell Condition
                    if (previousFastMa > previousSlowMa && currentFastMa < currentSlowMa && currentSlowMa < currentBiasMa)
                    {
                        Logger.Info($"SELL signal detected for {symbolName}.");
                        ExecuteTrade(TradeType.Sell, symbolName, "TrendStrategy Sell");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing {symbolName}: {ex.Message}");
                }
            }
        }

        public override void OnStop()
        {
            Logger.Info("TrendStrategy stopped.");
            // Any cleanup specific to this strategy can go here.
        }
    }
} 