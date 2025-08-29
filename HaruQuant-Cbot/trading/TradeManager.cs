using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.Robots.Utils;

namespace cAlgo.Robots.Trading
{
    
    public class TradeManager
    {
        /***
         TradeManager - Resposnible for all trading execution functions.
        
        Args:
            robot: CoreBot instance for API access
            logger: Logger service for logging
            errorHandler: ErrorHandler for error management
            riskManager: RiskManager for trade validation and sizing
        
        Returns:
            TradeManager instance with all trading execution functions.
            
        Notes:
            
    ***/
        #region Fields
        private readonly Robot _robot;
        private readonly Logger _logger;
        private readonly ErrorHandler _errorHandler;
        private readonly RiskManager _riskManager;
        #endregion

        #region Constructor
        public TradeManager(Robot robot, Logger logger, ErrorHandler errorHandler, RiskManager riskManager)
        {
            /***
            Initializes TradeManager with required dependencies.
            
            Args:
                robot: CoreBot instance for cTrader API access
                logger: Logger service for logging
                errorHandler: ErrorHandler for error management
                riskManager: RiskManager for trade validation and sizing
            
            Returns:
                 TradeManager instance.
                
            Notes:
                - Pure dependency injection
        ***/
            _robot = robot ?? throw new ArgumentNullException(nameof(robot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
            
            _logger.Info("TradeManager | Constructor | Initialization SUCCESS");
        }
        #endregion

        #region Public Methods
        public TradeResult ExecuteTrade(
            /***
            ExecuteTrade - basic trade execution with RiskManager integration.
            
            Args:
                tradeType: Buy or Sell trade direction
                label: Trade label for identification
                All other parameters: Passed from CoreBot to RiskManager
            
            Returns:
                TradeResult object with execution status and basic details.
                
            Notes:
                - Uses RiskManager for all calculations
                - Logging for debugging
                - No additional position management
                - Direct trade execution
            ***/
            TradeType tradeType,
            string OrderLabel,
            bool UseTradingHours,
            HourOfDay TradingHourStart,
            HourOfDay TradingHourEnd,
            TradingDirection TradingDirection,
            double MaxSpreadInPips,
            RiskDefaultSize RiskSizeMode,
            double DefaultPositionSize,
            double RiskPerTrade,
            double FixedRiskAmount,
            RiskBase RiskBase,
            double FixedRiskBalance,
            StopLossMode StopLossMode,
            int DefaultStopLoss,
            TakeProfitMode TakeProfitMode,
            int DefaultTakeProfit,
            double StopLossMultiplier,
            double TakeProfitMultiplier,
            double AdrRatio,
            int AdrPeriod,
            int AtrPeriod,
            double LotIncrease,
            double BalanceIncrease)
            {
                try
                {
                    _logger.Info($"TradeManager | ExecuteTrade | {tradeType} {_robot.Symbol.Name}");
                    
                    // Call RiskManager.Run with all parameters
                    var riskResult = _riskManager.Run(
                        _robot.Symbol, tradeType,
                        UseTradingHours, TradingHourStart, TradingHourEnd,
                        TradingDirection, MaxSpreadInPips,
                        RiskSizeMode, DefaultPositionSize, RiskPerTrade, FixedRiskAmount,
                        RiskBase, FixedRiskBalance,
                        StopLossMode, DefaultStopLoss,
                        TakeProfitMode, DefaultTakeProfit,
                        StopLossMultiplier, TakeProfitMultiplier,
                        AdrRatio, AdrPeriod, AtrPeriod,
                        LotIncrease, BalanceIncrease
                    );
                    
                    if (!riskResult.isTradeValid)
                    {
                        _logger.Warning($"TradeManager | ExecuteTrade | RiskManager REJECTED trade");
                        return new TradeResult { IsSuccessful = false, ErrorMessage = "RiskManager validation failed" };
                    }
                    
                    // Calculate prices
                    var symbol = _robot.Symbol;
                    double entryPrice = tradeType == TradeType.Buy ? symbol.Ask : symbol.Bid;
                    
                    double? stopLossPrice = null;
                    if (riskResult.stopLoss > 0)
                    {
                        if (tradeType == TradeType.Buy)
                            stopLossPrice = entryPrice - (riskResult.stopLoss * symbol.PipSize);
                        else
                            stopLossPrice = entryPrice + (riskResult.stopLoss * symbol.PipSize);
                    }
                    
                    double? takeProfitPrice = null;
                    if (riskResult.takeProfit > 0)
                    {
                        if (tradeType == TradeType.Buy)
                            takeProfitPrice = entryPrice + (riskResult.takeProfit * symbol.PipSize);
                        else
                            takeProfitPrice = entryPrice - (riskResult.takeProfit * symbol.PipSize);
                    }
                    
                    // Execute trade
                    var volumeInUnits = (long)riskResult.positionSize;
                    
                    _logger.Info($"TradeManager | ExecuteTrade | Executing: {volumeInUnits} units | SL: {stopLossPrice} | TP: {takeProfitPrice}");
                    
                    var result = _robot.ExecuteMarketOrder(
                        tradeType,
                        symbol.Name,
                        volumeInUnits,
                        OrderLabel,
                        riskResult.stopLoss,
                        riskResult.takeProfit
                    );
                    
                    if (result.IsSuccessful)
                    {
                        double actualLots = result.Position.VolumeInUnits / symbol.LotSize;
                        _logger.Info($"TradeManager | ExecuteTrade | SUCCESS | {actualLots:F3} lots | ID: {result.Position?.Id}");
                        _logger.Info($"TradeManager | ExecuteTrade | Prices | Entry: {entryPrice} | SL: {stopLossPrice} | TP: {takeProfitPrice}");
                        _logger.Info($"TradeManager | ExecuteTrade | Position created at: {result.Position?.EntryTime} | Current time: {_robot.Time}");
                        
                        return new TradeResult 
                        { 
                            IsSuccessful = true, 
                            Position = result.Position,
                            ExecutionPrice = entryPrice,
                            StopLossPrice = stopLossPrice,
                            TakeProfitPrice = takeProfitPrice
                        };
                    }
                    else
                    {
                        _logger.Error($"TradeManager | ExecuteTrade | FAILED | Error: {result.Error}");
                        return new TradeResult { IsSuccessful = false, ErrorMessage = result.Error.ToString() };
                    }
                }
                catch (Exception ex)
                {
                    _errorHandler.HandleException(ex, "TradeManager.ExecuteTrade", true);
                    return new TradeResult { IsSuccessful = false, ErrorMessage = ex.Message };
                }
            }
        
        
        #endregion
    }

    #region Data Structures

    
    public class TradeResult
    {
        /***
        Trade execution result data structure.
        
        Contains only essential information about trade execution.
        ***/
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public Position Position { get; set; }
        public double ExecutionPrice { get; set; }
        public double? StopLossPrice { get; set; }
        public double? TakeProfitPrice { get; set; }
    }

    #endregion
}