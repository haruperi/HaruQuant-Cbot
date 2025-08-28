using System;
using cAlgo.API;
using cAlgo.Robots.Utils;

namespace cAlgo.Robots.Trading
{
    /***
        RiskManager handles all risk-related calculations and trading constraints.
        
        This class provides centralized risk management functionality including:
        - Position sizing based on risk parameters
        - Trading hours validation
        - Stop loss and take profit calculation
        - Account value retrieval for risk calculations
        
        All calculations are logged and include proper error handling with fallback values.
    ***/
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

        public bool IsWithinTradingHours(bool useTradingHours, HourOfDay tradingHourStart, HourOfDay tradingHourEnd)
        {
            /***
                Validates if the current server time falls within the specified trading hours.
                
                Args:
                    useTradingHours: Boolean flag to enable/disable trading hours restriction
                    tradingHourStart: Starting hour for trading (0-23)
                    tradingHourEnd: Ending hour for trading (0-23)
                
                Returns:
                    true if trading is allowed at current time, false otherwise.
                    Always returns true if useTradingHours is false.
                
                Notes:
                    - Handles overnight trading sessions (e.g., 22:00 to 06:00)
                    - Uses server time for validation
                    - Hour comparison is inclusive of start and end hours
            ***/
            
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

        public long CalculatePositionSize(
            RiskDefaultSize riskSizeMode,
            double defaultPositionSize,
            double riskPerTrade,
            int defaultStopLoss,
            double fixedRiskAmount,
            RiskBase riskBase,
            double fixedRiskBalance)
        {
            /***
                Calculates the position size in volume units based on risk management parameters.
                
                Args:
                    riskSizeMode: The sizing method (FixedLots, Auto, FixedAmount, FixedLotsStep)
                    defaultPositionSize: Default position size in lots for fixed lot sizing
                    riskPerTrade: Risk percentage per trade (1.0 = 1% risk)
                    defaultStopLoss: Stop loss distance in pips for auto sizing
                    fixedRiskAmount: Fixed monetary amount to risk for fixed amount sizing
                    riskBase: Account base for risk calculation (Equity, Balance, FreeMargin, FixedBalance)
                    fixedRiskBalance: Fixed balance value when using FixedBalance risk base
                
                Returns:
                    Position size in volume units (long), normalized and rounded.
                    Returns default position size in case of calculation errors.
                
                Notes:
                    - Auto mode calculates size based on risk percentage and stop loss
                    - FixedAmount mode converts monetary amount to lot size
                    - All volumes are normalized using symbol specifications
                    - Calculation is logged for debugging purposes
            ***/
            
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

        public double GetAccountValue(RiskBase riskBase, double fixedRiskBalance)
        {
            /***
                Retrieves the account value to use for risk calculations based on the risk base setting.
                
                Args:
                    riskBase: The account metric to use (Equity, Balance, FreeMargin, FixedBalance)
                    fixedRiskBalance: Fixed balance value when using FixedBalance mode
                
                Returns:
                    Account value in base currency for risk calculations.
                    Defaults to account equity if invalid risk base provided.
                
                Notes:
                    - Equity: Current account equity (balance + unrealized P&L)
                    - Balance: Account balance (realized P&L only)
                    - FreeMargin: Available margin for new trades
                    - FixedBalance: User-defined fixed balance for consistent sizing
            ***/
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

        public (double? stopLoss, double? takeProfit) CalculateStopLossAndTakeProfit(
            TradeType tradeType,
            StopLossMode stopLossMode,
            TakeProfitMode takeProfitMode,
            int defaultStopLoss,
            int defaultTakeProfit)
        {
            /***
                Calculates stop loss and take profit price levels for a trade.
                
                Args:
                    tradeType: Direction of the trade (Buy or Sell)
                    stopLossMode: Stop loss calculation method (Fixed, None, UseATR, UseADR)
                    takeProfitMode: Take profit calculation method (Fixed, None, UseATR, UseADR)
                    defaultStopLoss: Stop loss distance in pips for fixed mode
                    defaultTakeProfit: Take profit distance in pips for fixed mode
                
                Returns:
                    Tuple containing:
                    - stopLoss: Stop loss price level (null if mode is None)
                    - takeProfit: Take profit price level (null if mode is None)
                    
                    Returns (null, null) if calculation errors occur.
                
                Notes:
                    - Prices are calculated from current Ask (Buy) or Bid (Sell) prices
                    - Stop loss is placed opposite to trade direction
                    - Take profit is placed in trade direction
                    - All prices are normalized to symbol digit precision
                    - Currently only supports Fixed mode, ATR/ADR modes reserved for future enhancement
            ***/
            
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
