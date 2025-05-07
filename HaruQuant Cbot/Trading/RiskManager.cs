using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots.Trading
{
    public class RiskManager
    {
        private readonly Robot _robot;
        private readonly double _riskPerTrade;
        private readonly int _stopLoss;

        public RiskManager(Robot robot, double riskPerTrade, int stopLoss)
        {
            _robot = robot;
            _riskPerTrade = riskPerTrade;
            _stopLoss = stopLoss;
        }

        public double CalculatePositionSize()
        {
            try
            {
                double riskAmount = _robot.Account.Equity * (_riskPerTrade / 100.0);
                double positionSizeUnits = riskAmount / (_stopLoss * _robot.Symbol.PipValue);
                return _robot.Symbol.NormalizeVolumeInUnits(positionSizeUnits);
            }
            catch (Exception ex)
            {
                _robot.Print($"Error calculating position size: {ex.Message}");
                return 0.01; // Return minimum lot size in case of error
            }
        }
    }
}
