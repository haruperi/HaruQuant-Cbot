using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Robots.Trading;

namespace cAlgo.Robots.Strategies
{
    public class TrendStrategy
    {
        private readonly Robot _robot;
        private readonly MovingAverage _slowMa;
        private readonly MovingAverage _fastMa;
        private readonly MovingAverage _biasMa;
        private readonly string _label;
        private readonly RiskManager _riskManager;
        private readonly int _stopLoss;
        private readonly int _takeProfit;

        public TrendStrategy(
            Robot robot,
            MovingAverageType maType,
            DataSeries sourceSeries,
            int fastPeriod,
            int slowPeriod,
            int biasPeriod,
            double riskPerTrade,
            int stopLoss,
            int takeProfit,
            string label)
        {
            _robot = robot;
            _label = label;
            _stopLoss = stopLoss;
            _takeProfit = takeProfit;

            _fastMa = robot.Indicators.MovingAverage(sourceSeries, fastPeriod, maType);
            _slowMa = robot.Indicators.MovingAverage(sourceSeries, slowPeriod, maType);
            _biasMa = robot.Indicators.MovingAverage(sourceSeries, biasPeriod, maType);

            _riskManager = new RiskManager(robot, riskPerTrade, stopLoss);
        }

        public void OnBar()
        {
            var longPosition = _robot.Positions.Find(_label, _robot.SymbolName, TradeType.Buy);
            var shortPosition = _robot.Positions.Find(_label, _robot.SymbolName, TradeType.Sell);

            var currentSlowMa = _slowMa.Result.Last(0);
            var currentFastMa = _fastMa.Result.Last(0);
            var previousSlowMa = _slowMa.Result.Last(1);
            var previousFastMa = _fastMa.Result.Last(1);
            var currentBiasMa = _biasMa.Result.Last(0);

            if (previousFastMa < previousSlowMa && currentFastMa > currentSlowMa && currentSlowMa > currentBiasMa && longPosition == null)
            {
                if (shortPosition != null)
                    _robot.ClosePosition(shortPosition);

                _robot.ExecuteMarketOrder(TradeType.Buy, _robot.SymbolName, _riskManager.CalculatePositionSize(), _label, _stopLoss, _takeProfit);
            }
            else if (previousFastMa > previousSlowMa && currentFastMa < currentSlowMa && currentSlowMa < currentBiasMa && shortPosition == null)
            {
                if (longPosition != null)
                    _robot.ClosePosition(longPosition);

                _robot.ExecuteMarketOrder(TradeType.Sell, _robot.SymbolName, _riskManager.CalculatePositionSize(), _label, _stopLoss, _takeProfit);
            }
        }
    }
}
