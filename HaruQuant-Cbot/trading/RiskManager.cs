using System;
using cAlgo.API;
using cAlgo.Robots.Utils;

namespace cAlgo.Robots.Trading
{
    /// <summary>
    /// Manages risk-related calculations and trading constraints
    /// </summary>
    public class RiskManager
    {
        private readonly Robot _robot;
        private readonly Logger _logger;

        public RiskManager(Robot robot, Logger logger)
        {
            _robot = robot ?? throw new ArgumentNullException(nameof(robot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.Info("RiskManager initialized successfully");
        }

        /// <summary>
        /// Checks if current time is within trading hours
        /// </summary>
        public bool IsWithinTradingHours(bool useTradingHours, HourOfDay tradingHourStart, HourOfDay tradingHourEnd)
        {
            if (!useTradingHours)
                return true;

            var currentHour = _robot.Server.Time.Hour;
            var startHour = (int)tradingHourStart;
            var endHour = (int)tradingHourEnd;

            if (startHour <= endHour)
            {
                return currentHour >= startHour && currentHour <= endHour;
            }
            else
            {
                // Handle overnight trading hours (e.g., 22:00 to 06:00)
                return currentHour >= startHour || currentHour <= endHour;
            }
        }

        /// <summary>
        /// Calculates position size based on risk management settings
        /// </summary>
        public long CalculatePositionSize(
            RiskDefaultSize riskSizeMode,
            double defaultPositionSize,
            double riskPerTrade,
            int defaultStopLoss,
            double fixedRiskAmount,
            RiskBase riskBase,
            double fixedRiskBalance)
        {
            try
            {
                double volumeInUnits;

                switch (riskSizeMode)
                {
                    case RiskDefaultSize.FixedLots:
                        volumeInUnits = _robot.Symbol.QuantityToVolumeInUnits(defaultPositionSize);
                        break;

                    case RiskDefaultSize.Auto:
                        // Calculate volume based on risk percentage
                        var accountValue = GetAccountValue(riskBase, fixedRiskBalance);
                        var riskAmount = accountValue * (riskPerTrade / 100.0);
                        var stopLossInPips = defaultStopLoss;
                        var stopLossValue = stopLossInPips * _robot.Symbol.PipValue * _robot.Symbol.LotSize;
                        
                        if (stopLossValue > 0)
                        {
                            var lots = riskAmount / stopLossValue;
                            volumeInUnits = _robot.Symbol.QuantityToVolumeInUnits(lots);
                        }
                        else
                        {
                            volumeInUnits = _robot.Symbol.QuantityToVolumeInUnits(defaultPositionSize);
                        }
                        break;

                    case RiskDefaultSize.FixedAmount:
                        volumeInUnits = _robot.Symbol.QuantityToVolumeInUnits(fixedRiskAmount / 100000.0); // Assuming standard lot conversion
                        break;

                    default:
                        volumeInUnits = _robot.Symbol.QuantityToVolumeInUnits(defaultPositionSize);
                        break;
                }

                // Normalize volume
                volumeInUnits = _robot.Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

                _logger?.Debug($"Calculated position size: {volumeInUnits} units ({_robot.Symbol.VolumeInUnitsToQuantity(volumeInUnits)} lots)");

                return (long)Math.Round(volumeInUnits);
            }
            catch (Exception ex)
            {
                _logger?.Error("Error calculating position size", ex);
                return (long)Math.Round(_robot.Symbol.QuantityToVolumeInUnits(defaultPositionSize));
            }
        }

        /// <summary>
        /// Gets account value based on risk base setting
        /// </summary>
        public double GetAccountValue(RiskBase riskBase, double fixedRiskBalance)
        {
            switch (riskBase)
            {
                case RiskBase.Equity:
                    return _robot.Account.Equity;
                case RiskBase.Balance:
                    return _robot.Account.Balance;
                case RiskBase.FreeMargin:
                    return _robot.Account.FreeMargin;
                case RiskBase.FixedBalance:
                    return fixedRiskBalance;
                default:
                    return _robot.Account.Equity;
            }
        }

        /// <summary>
        /// Calculates stop loss and take profit levels
        /// </summary>
        public (double? stopLoss, double? takeProfit) CalculateStopLossAndTakeProfit(
            TradeType tradeType,
            StopLossMode stopLossMode,
            TakeProfitMode takeProfitMode,
            int defaultStopLoss,
            int defaultTakeProfit)
        {
            try
            {
                double? stopLoss = null;
                double? takeProfit = null;

                var currentPrice = tradeType == TradeType.Buy ? _robot.Symbol.Ask : _robot.Symbol.Bid;

                // Calculate Stop Loss
                if (stopLossMode != StopLossMode.None)
                {
                    var stopLossPips = defaultStopLoss;
                    
                    if (tradeType == TradeType.Buy)
                    {
                        stopLoss = currentPrice - (stopLossPips * _robot.Symbol.PipSize);
                    }
                    else
                    {
                        stopLoss = currentPrice + (stopLossPips * _robot.Symbol.PipSize);
                    }
                    
                    stopLoss = Math.Round(stopLoss.Value, _robot.Symbol.Digits);
                }

                // Calculate Take Profit
                if (takeProfitMode != TakeProfitMode.None)
                {
                    var takeProfitPips = defaultTakeProfit;
                    
                    if (tradeType == TradeType.Buy)
                    {
                        takeProfit = currentPrice + (takeProfitPips * _robot.Symbol.PipSize);
                    }
                    else
                    {
                        takeProfit = currentPrice - (takeProfitPips * _robot.Symbol.PipSize);
                    }
                    
                    takeProfit = Math.Round(takeProfit.Value, _robot.Symbol.Digits);
                }

                return (stopLoss, takeProfit);
            }
            catch (Exception ex)
            {
                _logger?.Error("Error calculating stop loss and take profit", ex);
                return (null, null);
            }
        }
    }
}
