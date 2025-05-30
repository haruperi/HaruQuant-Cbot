using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Robots;
using cAlgo.Robots.Utils;
using System.Runtime.CompilerServices;

namespace HaruQuantCbot.Trading
{
    /*
    Manages risk-related operations including position sizing, risk limits, and safety checks
    */
    public class RiskManager
    {
        protected readonly Corebot Robot;
        protected readonly Logger Logger;
        private readonly Dictionary<string, AverageTrueRange> _atrIndicators;
        private readonly double _maxRiskPerTrade;
        private double _dailyPnL;
        private double _peakEquity;
        private DateTime _lastResetTime;

        // Risk Management Parameters
        private readonly RiskBase _riskBase;
        private readonly RiskDefaultSize _riskSizeMode;
        private readonly StopLossMode _stopLossMode;
        private readonly TakeProfitMode _takeProfitMode;
        private readonly double _fixedRiskBalance;
        private readonly double _fixedRiskAmount;
        private readonly double _balanceIncrease;
        private readonly double _lotIncrease;
        private readonly double _defaultPositionSize;
        private readonly int _defaultStopLoss;
        private readonly int _defaultTakeProfit;
        private readonly int _atrPeriod;
        private readonly int _adrPeriod;
        private readonly double _adrRatio;
        private readonly double _stopLossMultiplier;
        private readonly double _takeProfitMultiplier;
        private readonly double _maxSpreadInPips;
        private readonly bool _useTradingHours;
        private readonly HourOfDay _tradingHourStart;
        private readonly HourOfDay _tradingHourEnd;
        private readonly TradingDirection _tradingDirection;

        /// <summary>
        /// Initializes a new instance of the RiskManager class
        /// </summary>
        /// <param name="robot">The robot instance</param>
        public RiskManager(Corebot robot)
        {
            Robot = robot;
            Logger = new Logger(robot, "RiskManager", "1.0");
            _atrIndicators = new Dictionary<string, AverageTrueRange>();

            // Initialize risk parameters from CoreBot
            _maxRiskPerTrade = robot.RiskPerTrade;
            _riskBase = robot.RiskBase;
            _riskSizeMode = robot.RiskSizeMode;
            _stopLossMode = robot.StopLossMode;
            _takeProfitMode = robot.TakeProfitMode;
            _fixedRiskBalance = robot.FixedRiskBalance;
            _fixedRiskAmount = robot.FixedRiskAmount;
            _balanceIncrease = robot.BalanceIncrease;
            _lotIncrease = robot.LotIncrease;
            _defaultPositionSize = robot.DefaultPositionSize;
            _defaultStopLoss = robot.DefaultStopLoss;
            _defaultTakeProfit = robot.DefaultTakeProfit;
            _atrPeriod = robot.ATRPeriod;
            _adrPeriod = robot.ADRPeriod;
            _adrRatio = robot.ADRRatio;
            _stopLossMultiplier = robot.StopLossMultiplier;
            _takeProfitMultiplier = robot.TakeProfitMultiplier;
            _maxSpreadInPips = robot.MaxSpreadInPips;
            _useTradingHours = robot.UseTradingHours;
            _tradingHourStart = robot.TradingHourStart;
            _tradingHourEnd = robot.TradingHourEnd;
            _tradingDirection = robot.TradingDirection;

            // Initialize tracking variables
            _dailyPnL = 0;
            _peakEquity = Robot.Account.Equity;
            _lastResetTime = Robot.Server.Time.Date;
        }

        private AverageTrueRange GetATRIndicator(Symbol symbol)
        {
            if (!_atrIndicators.ContainsKey(symbol.Name))
            {
                var bars = Robot.MarketData.GetBars(Robot.TimeFrame, symbol.Name);
                _atrIndicators[symbol.Name] = Robot.Indicators.AverageTrueRange(bars, _atrPeriod, MovingAverageType.Exponential);
            }
            return _atrIndicators[symbol.Name];
        }

        public (bool isTradeValid, double positionSize, double stopLoss, double takeProfit) Run(Symbol symbol, TradeType tradeType)
        {
            try
            {
                if (!ValidateTradingConditions(symbol, tradeType))
                {
                    return (false, 0, 0, 0);
                }

                var (stopLoss, takeProfit) = CalculateTargets(symbol, tradeType);

                var positionSize = CalculatePositionSize(symbol, stopLoss);

                return (true, positionSize, stopLoss, takeProfit);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error running risk manager: {ex.Message}", ex);
                return (false, 0, 0, 0);
            }
        }

        /************************************************* VALIDATE TRADING CONDITIONS *************************************************/
        #region Validate Trading Conditions

        private bool ValidateSymbolAndSpread(Symbol symbol)
        {
            if (!symbol.IsTradingEnabled)
            {
                Logger.Error($"Error: {symbol.Name} is not available for trading.");
                return false;
            }
            
            if (symbol.Spread > _maxSpreadInPips * symbol.PipSize)
            {
                Logger.Warning($"Warning: {symbol.Name} Spread : {symbol.Spread / symbol.PipSize:F1} pips exceeds maximum allowed {_maxSpreadInPips} pips");
                return false;
            }

            return true;
        }

        private bool ValidateTradingHours()
        {
            if (!_useTradingHours) return true;

            int currentHour = Robot.Server.Time.Hour;
            int startHour = (int)_tradingHourStart;
            int endHour = (int)_tradingHourEnd;

            if (startHour <= endHour)
            {
                if (currentHour < startHour || currentHour > endHour)
                {
                    Logger.Warning("Outside trading hours");
                    return false;
                }
            }
            else if (currentHour < startHour && currentHour > endHour)
            {
                Logger.Warning("Outside trading hours");
                return false;
            }

            return true;
        }

        private bool ValidateTradingDirection(TradeType tradeType)
        {
            if (_tradingDirection == TradingDirection.Both) return true;

            if (_tradingDirection == TradingDirection.Buy && tradeType == TradeType.Sell)
            {
                Logger.Warning("Buy-only trading mode");
                return false;
            }
            if (_tradingDirection == TradingDirection.Sell && tradeType == TradeType.Buy)
            {
                Logger.Warning("Sell-only trading mode");
                return false;
            }

            return true;
        }

        public bool ValidateTradingConditions(Symbol symbol, TradeType tradeType)
        {
            try
            {
                if (!ValidateSymbolAndSpread(symbol)) return false;
                if (!symbol.MarketHours.IsOpened())
                {
                    Logger.Warning($"Warning: {symbol.Name} is not open for trading at the current time.");
                    return false;
                }
                if (!ValidateTradingHours()) return false;
                if (!ValidateTradingDirection(tradeType)) return false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating trading conditions: {ex.Message}", ex);
                return false;
            }
        }
        #endregion

        /************************************************* POSITION SIZE CALCULATION *************************************************/
        #region Calculate Position Size

        public (int stopLoss, int takeProfit) CalculateTargets(Symbol symbol, TradeType tradeType)
        {
            int stopLoss = 0;
            int takeProfit = 0;

            switch (_stopLossMode)
            {
                case StopLossMode.Fixed:
                    stopLoss = _defaultStopLoss;
                    break;
                case StopLossMode.UseATR:
                    var atr = GetATRIndicator(symbol);
                    var atrValue = Math.Abs(atr.Result.Last(0));
                    var atrInPips = atrValue / symbol.PipSize;
                    stopLoss = (int)(atrInPips * _stopLossMultiplier);
                    break;
                case StopLossMode.UseADR:
                    var adrValue = CalculateADR(symbol);
                    var adrInPips = adrValue / _adrRatio;
                    stopLoss = (int)(adrInPips * _stopLossMultiplier);
                    break;
                case StopLossMode.None:
                    stopLoss = 0;
                    break;
            }

            switch (_takeProfitMode)
            {
                case TakeProfitMode.Fixed:
                    takeProfit = _defaultTakeProfit;
                    break;
                case TakeProfitMode.UseATR:
                    var atr = GetATRIndicator(symbol);
                    var atrValue = Math.Abs(atr.Result.Last(0));
                    var atrInPips = atrValue / symbol.PipSize;
                    takeProfit = (int)(atrInPips * _takeProfitMultiplier);
                    break;
                case TakeProfitMode.UseADR:
                    var adrValue = CalculateADR(symbol);
                    var adrInPips = adrValue / _adrRatio;
                    takeProfit = (int)(adrInPips * _takeProfitMultiplier);
                    break;
                case TakeProfitMode.None:
                    takeProfit = 0;
                    break;
            }

            return (stopLoss, takeProfit);
        }

        public double CalculateADR(Symbol symbol)
        {
            try
            {
                double sum = 0;
                int count = 0;

                // Get the daily bars
                var dailyBars = Robot.MarketData.GetBars(TimeFrame.Daily, symbol.Name);
                
                // Calculate average of daily ranges for the specified period, excluding current day
                for (int i = 1; i <= _adrPeriod; i++)
                {
                    var bar = dailyBars.Last(i);
                    sum += (bar.High - bar.Low) / symbol.PipSize;
                    count++;
                }

                return count > 0 ? sum / count : 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating ADR: {ex.Message}", ex);
                return 0;
            }
            
        }

        public double CalculatePositionSize(Symbol symbol, double stopLossPips)
        {
            try
            {

                double positionSizeLots, positionSizeVolume = 0;

                switch (_riskSizeMode)
                {
                    case RiskDefaultSize.Auto:
                        positionSizeLots = CalculateAutoPositionSize(symbol, stopLossPips, fixedAmount:false);
                        break;
                    case RiskDefaultSize.FixedLots:
                        positionSizeLots = _defaultPositionSize;
                        break;
                    case RiskDefaultSize.FixedAmount:
                        positionSizeLots = CalculateAutoPositionSize(symbol, stopLossPips, fixedAmount:true);
                        break;
                    case RiskDefaultSize.FixedLotsStep:
                        positionSizeLots = CalculateStepBasedPositionSize();
                        break;
                    default:
                        positionSizeLots = _defaultPositionSize;
                        break;
                }

                // Normalize position size
                positionSizeVolume = NormalizePositionSize(symbol, LotsToVolume(symbol, positionSizeLots));
                return positionSizeVolume;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating position size: {ex.Message}", ex);
                return 0;
            }
        }

        private double CalculateAutoPositionSize(Symbol symbol, double stopLossPips, bool fixedAmount)
        {
            double contractSize = symbol.QuantityToVolumeInUnits(1.0);
            double accountValue = GetAccountValueBasedOnRiskBase();
            double riskAmount;

            if (fixedAmount){
                 riskAmount = _fixedRiskAmount;
            }else{
                 riskAmount = accountValue * (_maxRiskPerTrade / 100.0);
            }
            Robot.Print("riskAmount: " + riskAmount);
            Robot.Print("stopLossPips: " + stopLossPips);
            Robot.Print("symbol.PipValue: " + symbol.PipValue);
            Robot.Print("contractSize: " + contractSize);
            

            double positionSizeLots = riskAmount / (stopLossPips * symbol.PipValue * contractSize);
            return positionSizeLots;
        }

        private double CalculateStepBasedPositionSize()
        {
            double balanceMultiplier = Math.Floor(Robot.Account.Balance / _balanceIncrease);
            return _defaultPositionSize + (balanceMultiplier * _lotIncrease);
        }

        public double GetAccountValueBasedOnRiskBase()
        {
            // Gets the account value based on the selected risk base
            // Returns: Account value in account currency
            switch (_riskBase)
            {
                case RiskBase.Equity:
                    return Robot.Account.Equity;
                case RiskBase.Balance:
                    return Robot.Account.Balance;
                case RiskBase.FreeMargin:
                    return Robot.Account.FreeMargin;
                case RiskBase.FixedBalance:
                    return _fixedRiskBalance;
                default:
                    return Robot.Account.Equity;
            }
        }

        public static double VolumeToLots(Symbol symbol, double volumeInUnits)
        {
            // Converts volume in units to lots
            // Params: volumeInUnits - Volume in units
            // Returns: Volume in lots
            return symbol.VolumeInUnitsToQuantity(volumeInUnits);
        }

        public static double LotsToVolume(Symbol symbol, double lots)
        {
            // Converts lots to volume in units
            // Params: lots - Volume in lots
            // Returns: Volume in units
            return symbol.QuantityToVolumeInUnits(lots);
        }

        public double NormalizePositionSize(Symbol symbol, double positionSizeVolume)
        {
            // Normalizes position size according to symbol's specifications and configured limits
            // Params: positionSizeVolume - Desired position size volume
            // Returns: Normalized position size volume
            try
            {
                // Ensure minimum volume
                positionSizeVolume = Math.Max(positionSizeVolume, symbol.VolumeInUnitsMin);

                // Ensure maximum volume
                positionSizeVolume = Math.Min(positionSizeVolume, symbol.VolumeInUnitsMax);

                // Round to nearest step
                double step = symbol.VolumeInUnitsStep;
                positionSizeVolume = Math.Round(positionSizeVolume / step) * step;

                // Ensure volume is within symbol's limits
                positionSizeVolume = symbol.NormalizeVolumeInUnits(positionSizeVolume);

                return positionSizeVolume;
            }
            catch (Exception ex)
            {
                Robot.Print($"Error normalizing position size for {symbol.Name}: {ex.Message}");
                return symbol.VolumeInUnitsMin;
            }
        }



        #endregion

     
        public void UpdateRiskMetrics()
        {
            // Updates the risk metrics based on current account state
            try
            {
                // Reset daily P&L if it's a new day
                if (Robot.Server.Time.Date > _lastResetTime)
                {
                    _dailyPnL = 0;
                    _lastResetTime = Robot.Server.Time.Date;
                }

                // Update peak equity
                if (Robot.Account.Equity > _peakEquity)
                    _peakEquity = Robot.Account.Equity;

                // Update daily P&L
                _dailyPnL = Robot.Account.Equity - Robot.Account.Balance;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating risk metrics: {ex.Message}", ex);
            }
        }


        public double GetCurrentDrawdown()
        {
            // Returns the current drawdown percentage
            return (_peakEquity - Robot.Account.Equity) / _peakEquity * 100;
        }

        public double GetDailyPnL()
        {
            return _dailyPnL;
        }

        
    }
} 