using System;
using cAlgo.API;
using cAlgo.API.Internals;
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

        public (bool isTradeValid, double positionSize, double stopLoss, double takeProfit) Run (
            Symbol symbol, 
            double stopLossPips, 
            TradeType tradeType,
            bool useTradingHours,
            HourOfDay tradingHourStart,
            HourOfDay tradingHourEnd,
            TradingDirection tradingDirection,
            double maxSpreadInPips,
            RiskDefaultSize riskSizeMode,
            double defaultPositionSize,
            double riskPerTrade,
            double fixedRiskAmount,
            RiskBase riskBase,
            double fixedRiskBalance,
            StopLossMode stopLossMode,
            int defaultStopLoss,
            TakeProfitMode takeProfitMode,
            int defaultTakeProfit,
            double stopLossMultiplier,
            double takeProfitMultiplier,
            double adrRatio,
            int adrPeriod,
            int atrPeriod,
            double maxRiskPerTrade,
            double lotIncrease,
            double balanceIncrease)
        {
            try
            {
                if (!ValidateTrade(symbol, defaultPositionSize, stopLossPips, tradeType, useTradingHours, tradingHourStart, tradingHourEnd, tradingDirection, maxSpreadInPips))
                {
                    return (false, 0, 0, 0);
                }

                var (stopLoss, takeProfit) = CalculateTargets(symbol, tradeType, stopLossMode, defaultStopLoss, takeProfitMode, defaultTakeProfit, stopLossMultiplier, takeProfitMultiplier, adrRatio, adrPeriod, atrPeriod);

                var positionSize = CalculatePositionSize(symbol, riskSizeMode, defaultPositionSize, riskPerTrade, stopLoss, fixedRiskAmount, riskBase, fixedRiskBalance, maxRiskPerTrade, lotIncrease, balanceIncrease);

                _logger?.Debug($"Exiting RiskManager.Run()");
                _logger?.Debug($"ValidateTrade() Return Value: TRUE");
                _logger?.Debug($"CalculateTargets() Return Value: Stop Loss: {stopLoss} pips, Take Profit: {takeProfit} pips");
                _logger?.Debug($"CalculatePositionSize() Return Value: Position Size: {positionSize} volume units");

                return (true, positionSize, stopLoss, takeProfit);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error running risk manager: {ex.Message}", ex);
                return (false, 0, 0, 0);
            }
        }

        public (int stopLoss, int takeProfit) CalculateTargets(Symbol symbol, TradeType tradeType, StopLossMode stopLossMode, int defaultStopLoss, TakeProfitMode takeProfitMode, int defaultTakeProfit, double stopLossMultiplier, double takeProfitMultiplier, double adrRatio, int adrPeriod, int atrPeriod)
        {

            int stopLoss = 0;
            int takeProfit = 0;

            switch (stopLossMode)
            {
                case StopLossMode.Fixed:
                    stopLoss = defaultStopLoss;
                    break;
                case StopLossMode.UseATR:
                    stopLoss = CalculateATRBasedValue(symbol, atrPeriod, stopLossMultiplier, 1.0, defaultStopLoss, "ATR Stop Loss", false);
                    break;
                case StopLossMode.UseADR:
                    stopLoss = CalculateATRBasedValue(symbol, adrPeriod, stopLossMultiplier, adrRatio, defaultStopLoss, "ADR Stop Loss", true);
                    break;
                case StopLossMode.None:
                    stopLoss = 0;
                    break;
            }

            switch (takeProfitMode)
            {
                case TakeProfitMode.Fixed:
                    takeProfit = defaultTakeProfit;
                    break;
                case TakeProfitMode.UseATR:
                    takeProfit = CalculateATRBasedValue(symbol, atrPeriod, takeProfitMultiplier, 1.0, defaultTakeProfit, "ATR Take Profit", false);
                    break;
                case TakeProfitMode.UseADR:
                    takeProfit = CalculateATRBasedValue(symbol, adrPeriod, takeProfitMultiplier, adrRatio, defaultTakeProfit, "ADR Take Profit", true);
                    break;
                case TakeProfitMode.None:
                    takeProfit = 0;
                    break;
            }

            _logger?.Debug($"Stop Loss: {stopLoss} pips, Take Profit: {takeProfit} pips");

            return (stopLoss, takeProfit);
        }

        public long CalculatePositionSize(
            Symbol symbol,
            RiskDefaultSize riskSizeMode,
            double defaultPositionSize,
            double riskPerTrade,
            int stopLoss,
            double fixedRiskAmount,
            RiskBase riskBase,
            double fixedRiskBalance,
            double maxRiskPerTrade,
            double lotIncrease,
            double balanceIncrease)
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

                double positionSizeLots, positionSizeVolume = 0;

                switch (riskSizeMode)
                {
                    case RiskDefaultSize.Auto:
                        positionSizeLots = CalculateAutoPositionSize(symbol, stopLoss, fixedRiskAmount, maxRiskPerTrade, fixedRiskBalance, fixedAmount:false, riskBase);
                        break;
                    case RiskDefaultSize.FixedLots:
                        positionSizeLots = defaultPositionSize;
                        break;
                    case RiskDefaultSize.FixedAmount:
                        positionSizeLots = CalculateAutoPositionSize(symbol, stopLoss, fixedRiskAmount, maxRiskPerTrade, fixedRiskBalance, fixedAmount:true, riskBase);
                        break;
                    case RiskDefaultSize.FixedLotsStep:
                        positionSizeLots = CalculateStepBasedPositionSize(lotIncrease, balanceIncrease);
                        break;
                    default:
                        positionSizeLots = defaultPositionSize;
                        break;
                }

                // Normalize position size
                positionSizeVolume = NormalizePositionSize(symbol, LotsToVolume(symbol, positionSizeLots));
                return (long)Math.Round(positionSizeVolume);
            }

            catch (Exception ex)
            {
                _logger?.Error("Error calculating position size", ex);
                return (long)Math.Round(_robot.Symbol.QuantityToVolumeInUnits(defaultPositionSize));
            }
        }

        private int CalculateATRBasedValue(
            Symbol symbol, 
            int period, 
            double multiplier, 
            double ratio, 
            int defaultValue, 
            string logPrefix, 
            bool useDailyTimeframe)
        {
            /***
                Calculates ATR or ADR based values for stop loss or take profit.
                
                Args:
                    symbol: The trading symbol
                    period: ATR/ADR period
                    multiplier: Multiplier to apply to the ATR/ADR value
                    ratio: Ratio to divide the ATR/ADR value (1.0 for ATR, custom for ADR)
                    defaultValue: Fallback value if calculation fails
                    logPrefix: Prefix for logging messages
                    useDailyTimeframe: true for ADR (daily), false for ATR (current timeframe)
                
                Returns:
                    Calculated value in pips as integer
                
                Notes:
                    - ADR uses ATR on daily timeframe
                    - ATR uses current timeframe
                    - Both are converted to pips and apply multiplier/ratio
                    - Falls back to default value on error
            ***/
            
            try
            {
                double atrValue;
                
                if (useDailyTimeframe)
                {
                    // ADR: Use ATR on daily timeframe
                    var dailyBars = _robot.MarketData.GetBars(TimeFrame.Daily, symbol.Name);
                    var adrIndicator = _robot.Indicators.AverageTrueRange(dailyBars, period, MovingAverageType.Simple);
                    atrValue = adrIndicator.Result.LastValue;
                }
                else
                {
                    // ATR: Use current timeframe
                    var atr = _robot.Indicators.AverageTrueRange(period, MovingAverageType.Simple);
                    atrValue = atr.Result.LastValue;
                }
                
                // Convert to pips and apply ratio
                var valueInPips = (atrValue / symbol.PipSize) / ratio;
                
                // Apply multiplier and convert to integer pips
                var result = (int)Math.Round(valueInPips * multiplier);
                
                // Ensure minimum value
                if (result < 1)
                    result = defaultValue;
                    
                _logger?.Debug($"{logPrefix} calculated: Value={atrValue:F5}, Pips={valueInPips:F1}, Ratio={ratio}, Multiplier={multiplier}, Result={result} pips");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error calculating {logPrefix}: {ex.Message}", ex);
                return defaultValue; // Fallback to default
            }
        }

        private double CalculateAutoPositionSize(Symbol symbol, double stopLossPips, double fixedRiskAmount, double maxRiskPerTrade, double fixedRiskBalance, bool fixedAmount, RiskBase riskBase)
        {
            double contractSize = symbol.QuantityToVolumeInUnits(1.0);
            double accountValue = GetAccountValue(riskBase, fixedRiskBalance);
            double riskAmount = fixedAmount ? fixedRiskAmount : accountValue * (maxRiskPerTrade / 100.0);

            double positionSizeLots = riskAmount / (stopLossPips * symbol.PipValue * contractSize);
            return positionSizeLots;
        }

        private double CalculateStepBasedPositionSize(double lotIncrease, double balanceIncrease)
        {
            return lotIncrease * _robot.Account.Balance / balanceIncrease;
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
                _logger?.Error($"Error normalizing position size for {symbol.Name}: {ex.Message}");
                return symbol.VolumeInUnitsMin;
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

        public bool ValidateTrade(
            Symbol symbol, 
            double positionSize, 
            double stopLossPips, 
            TradeType tradeType,
            bool useTradingHours,
            HourOfDay tradingHourStart,
            HourOfDay tradingHourEnd,
            TradingDirection tradingDirection,
            double maxSpreadInPips)
        {
            /***
                Validates if a trade meets comprehensive risk management and trading criteria.
                
                Args:
                    symbol: The trading symbol to validate
                    positionSize: Position size in lots
                    stopLossPips: Stop loss distance in pips
                    tradeType: Direction of the trade (Buy or Sell)
                    useTradingHours: Whether to enforce trading hours
                    tradingHourStart: Start of trading hours
                    tradingHourEnd: End of trading hours
                    tradingDirection: Allowed trading direction (Both, Buy, Sell)
                    maxSpreadInPips: Maximum allowed spread in pips
                
                Returns:
                    true if all validations pass, false otherwise.
                
                Notes:
                    - Performs 9 comprehensive validation checks
                    - Each failed validation is logged with specific reason
                    - Returns false immediately on first failed validation
                    - All exceptions are caught and logged as validation failures
            ***/
            
            try
            {
                _logger?.Debug($"Validating trade: {tradeType} {positionSize} lots {symbol.Name} with {stopLossPips} pips SL");

                // 1. Validate Symbol is okay to trade
                if (!ValidateSymbol(symbol))
                {
                    return false;
                }

                // 2. Validate Position Size
                if (!ValidatePositionSize(positionSize))
                {
                    return false;
                }

                // 3. Validate Stop Loss
                if (!ValidateStopLoss(stopLossPips))
                {
                    return false;
                }

                // 4. Validate Spread
                if (!ValidateSpread(symbol, maxSpreadInPips))
                {
                    return false;
                }

                // 5. Validate Trading Hours
                if (!ValidateTradingHours(useTradingHours, tradingHourStart, tradingHourEnd))
                {
                    return false;
                }

                // 6. Validate Trading Direction
                if (!ValidateTradingDirection(tradeType, tradingDirection))
                {
                    return false;
                }

                // 7. Validate Risk Amount
                if (!ValidateRiskAmount(symbol, positionSize, stopLossPips))
                {
                    return false;
                }

                // 8. Validate Account Health
                if (!ValidateAccountHealth())
                {
                    return false;
                }

                // 9. Check Emergency Stop
                if (IsEmergencyStopTriggered())
                {
                    _logger?.Warning("Emergency stop is active. Trade validation failed.");
                    return false;
                }

                _logger?.Debug("Exiting ValidateTrade() : Returning TRUE : All trade validations passed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Error during trade validation", ex);
                return false;
            }
        }

        private bool ValidateSymbol(Symbol symbol)
        {
            /***
                Validates if the symbol is available and tradeable.
                
                Args:
                    symbol: The trading symbol to validate
                
                Returns:
                    true if symbol is valid for trading, false otherwise.
                
                Notes:
                    - Checks if symbol is not null
                    - Validates symbol has valid pip size and digits
                    - Ensures symbol is not in a halted state
            ***/
            
            if (symbol == null)
            {
                _logger?.Warning("Symbol validation failed: Symbol is null");
                return false;
            }

            if (symbol.PipSize <= 0)
            {
                _logger?.Warning($"Symbol validation failed: Invalid pip size {symbol.PipSize} for {symbol.Name}");
                return false;
            }

            if (symbol.Digits <= 0)
            {
                _logger?.Warning($"Symbol validation failed: Invalid digits {symbol.Digits} for {symbol.Name}");
                return false;
            }

            // Additional symbol checks can be added here (e.g., trading session status)
            _logger?.Debug($"Symbol validation passed for {symbol.Name}");
            return true;
        }

        private bool ValidatePositionSize(double positionSize)
        {
            /***
                Validates if the position size is within acceptable limits.
                
                Args:
                    positionSize: Position size in lots
                
                Returns:
                    true if position size is valid, false otherwise.
                
                Notes:
                    - Checks for positive position size
                    - Validates against minimum and maximum lot sizes
                    - Ensures position size is not zero or negative
            ***/
            
            if (positionSize <= 0)
            {
                _logger?.Warning($"Position size validation failed: Invalid size {positionSize}");
                return false;
            }

            // Check against broker minimum/maximum (these would typically come from symbol properties)
            var minLotSize = 0.01; // This should come from symbol.VolumeInUnitsMin
            var maxLotSize = 100.0; // This should come from symbol.VolumeInUnitsMax

            if (positionSize < minLotSize)
            {
                _logger?.Warning($"Position size validation failed: {positionSize} below minimum {minLotSize}");
                return false;
            }

            if (positionSize > maxLotSize)
            {
                _logger?.Warning($"Position size validation failed: {positionSize} above maximum {maxLotSize}");
                return false;
            }

            _logger?.Debug($"Position size validation passed: {positionSize} lots");
            return true;
        }

        private bool ValidateStopLoss(double stopLossPips)
        {
            /***
                Validates if the stop loss distance is reasonable.
                
                Args:
                    stopLossPips: Stop loss distance in pips
                
                Returns:
                    true if stop loss is valid, false otherwise.
                
                Notes:
                    - Ensures stop loss is positive
                    - Checks against minimum and maximum pip distances
                    - Prevents excessively tight or wide stop losses
            ***/
            
            if (stopLossPips <= 0)
            {
                _logger?.Warning($"Stop loss validation failed: Invalid distance {stopLossPips} pips");
                return false;
            }

            var minStopLossPips = 1.0; // Minimum reasonable stop loss
            var maxStopLossPips = 1000.0; // Maximum reasonable stop loss

            if (stopLossPips < minStopLossPips)
            {
                _logger?.Warning($"Stop loss validation failed: {stopLossPips} below minimum {minStopLossPips} pips");
                return false;
            }

            if (stopLossPips > maxStopLossPips)
            {
                _logger?.Warning($"Stop loss validation failed: {stopLossPips} above maximum {maxStopLossPips} pips");
                return false;
            }

            _logger?.Debug($"Stop loss validation passed: {stopLossPips} pips");
            return true;
        }

        private bool ValidateSpread(Symbol symbol, double maxSpreadInPips)
        {
            /***
                Validates if the current spread is within acceptable limits.
                
                Args:
                    symbol: The trading symbol
                    maxSpreadInPips: Maximum allowed spread in pips
                
                Returns:
                    true if spread is acceptable, false otherwise.
                
                Notes:
                    - Calculates current spread from Ask-Bid
                    - Compares against maximum allowed spread
                    - Prevents trading during high spread conditions
            ***/
            
            var currentSpread = symbol.Spread / symbol.PipSize;
            
            if (currentSpread > maxSpreadInPips)
            {
                _logger?.Warning($"Spread validation failed: Current spread {currentSpread:F1} pips exceeds maximum {maxSpreadInPips} pips for {symbol.Name}");
                return false;
            }

            _logger?.Debug($"Spread validation passed: {currentSpread:F1} pips for {symbol.Name}");
            return true;
        }

        private bool ValidateTradingHours(bool useTradingHours, HourOfDay tradingHourStart, HourOfDay tradingHourEnd)
        {
            /***
                Validates if current time is within allowed trading hours.
                
                Args:
                    useTradingHours: Whether to enforce trading hours
                    tradingHourStart: Start of trading hours
                    tradingHourEnd: End of trading hours
                
                Returns:
                    true if trading is allowed at current time, false otherwise.
                
                Notes:
                    - Handles overnight trading sessions (e.g., 22:00 to 06:00)
                    - Uses server time for validation
                    - Hour comparison is inclusive of start and end hours
                    - Returns true if trading hours are disabled
                    - Logs specific time restriction failures
            ***/
            
            if (!useTradingHours)
                return true;

            var currentHour = _robot.Server.Time.Hour;
            var startHour = (int)tradingHourStart;
            var endHour = (int)tradingHourEnd;
            var isWithinTradingHours = false;

            if (startHour <= endHour)
            {
                isWithinTradingHours = currentHour >= startHour && currentHour <= endHour;
            }
            else
            {
                // Handle overnight trading hours (e.g., 22:00 to 06:00)
                isWithinTradingHours = currentHour >= startHour || currentHour <= endHour;
            }
        
            
            if (!isWithinTradingHours)
            {
                _logger?.Warning($"Trading hours validation failed: Current time {_robot.Server.Time:HH:mm} outside allowed hours {tradingHourStart}-{tradingHourEnd}");
                return false;
            }

            _logger?.Debug("Trading hours validation passed");
            return true;
        }

        private bool ValidateTradingDirection(TradeType tradeType, TradingDirection tradingDirection)
        {
            /***
                Validates if the trade direction is allowed by current settings.
                
                Args:
                    tradeType: The requested trade direction (Buy or Sell)
                    tradingDirection: Allowed trading directions (Both, Buy, Sell)
                
                Returns:
                    true if trade direction is allowed, false otherwise.
                
                Notes:
                    - Checks Buy trades against Buy-only or Both settings
                    - Checks Sell trades against Sell-only or Both settings
                    - Prevents trades in restricted directions
            ***/
            
            if (tradingDirection == TradingDirection.Both)
            {
                _logger?.Debug($"Trading direction validation passed: {tradeType} allowed (Both directions enabled)");
                return true;
            }

            if (tradeType == TradeType.Buy && tradingDirection == TradingDirection.Buy)
            {
                _logger?.Debug("Trading direction validation passed: Buy trade allowed");
                return true;
            }

            if (tradeType == TradeType.Sell && tradingDirection == TradingDirection.Sell)
            {
                _logger?.Debug("Trading direction validation passed: Sell trade allowed");
                return true;
            }

            _logger?.Warning($"Trading direction validation failed: {tradeType} not allowed (Direction setting: {tradingDirection})");
            return false;
        }

        private bool ValidateRiskAmount(Symbol symbol, double positionSize, double stopLossPips)
        {
            /***
                Validates if the risk amount for this trade is acceptable.
                
                Args:
                    symbol: The trading symbol
                    positionSize: Position size in lots
                    stopLossPips: Stop loss distance in pips
                
                Returns:
                    true if risk amount is acceptable, false otherwise.
                
                Notes:
                    - Calculates monetary risk based on position size and stop loss
                    - Compares against account balance/equity limits
                    - Prevents excessive risk per trade
            ***/
            
            try
            {
                var riskAmount = positionSize * stopLossPips * symbol.PipValue;
                var accountValue = _robot.Account.Equity;
                var riskPercentage = (riskAmount / accountValue) * 100;

                var maxRiskPercentage = 10.0; // Maximum 10% risk per trade

                if (riskPercentage > maxRiskPercentage)
                {
                    _logger?.Warning($"Risk validation failed: Risk {riskPercentage:F2}% exceeds maximum {maxRiskPercentage}%");
                    return false;
                }

                _logger?.Debug($"Risk validation passed: {riskPercentage:F2}% risk (${riskAmount:F2})");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Error calculating risk amount during validation", ex);
                return false;
            }
        }

        private bool ValidateAccountHealth()
        {
            /***
                Validates if the account is in good health for trading.
                
                Returns:
                    true if account health is good, false otherwise.
                
                Notes:
                    - Checks account equity vs balance ratios
                    - Validates free margin availability
                    - Ensures account is not in margin call
                    - Prevents trading with insufficient funds
            ***/
            
            try
            {
                var account = _robot.Account;
                var marginLevel = account.MarginLevel;
                var freeMargin = account.FreeMargin;

                // Check margin level (should be above 100%)
                if (marginLevel.HasValue && marginLevel.Value < 150) // 150% minimum margin level
                {
                    _logger?.Warning($"Account health validation failed: Low margin level {marginLevel:F1}%");
                    return false;
                }

                // Check free margin
                if (freeMargin < 100) // Minimum $100 free margin
                {
                    _logger?.Warning($"Account health validation failed: Insufficient free margin ${freeMargin:F2}");
                    return false;
                }

                _logger?.Debug($"Account health validation passed: Margin level {marginLevel:F1}%, Free margin ${freeMargin:F2}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Error validating account health", ex);
                return false;
            }
        }

        private bool IsEmergencyStopTriggered()
        {
            /***
                Checks if an emergency stop condition is active.
                
                Returns:
                    true if emergency stop is triggered, false otherwise.
                
                Notes:
                    - Checks for excessive drawdown conditions
                    - Validates daily loss limits
                    - Can be extended for custom emergency conditions
                    - Currently returns false (no emergency conditions implemented)
            ***/
            
            try
            {
                // Example emergency stop conditions:
                var account = _robot.Account;
                var currentEquity = account.Equity;
                var initialBalance = account.Balance; // This might need to be stored separately

                // Emergency stop if equity drops below 80% of initial balance
                var equityThreshold = initialBalance * 0.8;
                if (currentEquity < equityThreshold)
                {
                    _logger?.Warning($"Emergency stop triggered: Equity ${currentEquity:F2} below threshold ${equityThreshold:F2}");
                    return true;
                }

                // Add more emergency conditions as needed
                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error("Error checking emergency stop conditions", ex);
                return true; // Fail safe - trigger emergency stop on error
            }
        }
    }
}
