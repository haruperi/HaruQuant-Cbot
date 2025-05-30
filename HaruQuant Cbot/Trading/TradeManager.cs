using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.Robots.Utils;
using cAlgo.Robots;

namespace HaruQuantCbot.Trading
{
    public class TradeManager
    {
        private readonly Corebot _robot;
        private readonly Logger _logger;
        private readonly RiskManager _riskManager;

        public TradeManager(Corebot robot)
        {
            _robot = robot;
            _logger = new Logger(robot, "TradeManager", BotConfig.BotVersion);
            _riskManager = new RiskManager(robot);
        }

   
        
       
        public bool ExecuteTrade(TradeType tradeType, Symbol symbol, string comment = "")
        {
            // Executes a market order with proper risk management and trade management
            try
            {
                var (isTradeValid, positionSize, stopLoss, takeProfit) = _riskManager.Run(symbol, tradeType);

                if (!isTradeValid)
                {
                    _logger.Warning($"Trade not valid for {symbol.Name}");
                    return false;
                }

                // Apply trade management settings
                if (_robot.HideStopLoss)
                    stopLoss = 0;
                if (_robot.HideTakeProfit)
                    takeProfit = 0;

                var result = _robot.ExecuteMarketOrder(tradeType, symbol.Name, positionSize, _robot.OrderLabel, stopLoss, takeProfit);

                if (result.IsSuccessful)
                {
                    _logger.Info($"Trade executed successfully: {tradeType} {symbol.Name} {positionSize} {_robot.OrderLabel} {stopLoss} {takeProfit}");
                    
                    // Initialize trade management if enabled
                    if (_robot.UseTrailingStop)
                    {
                        InitializeTrailingStop(result.Position);
                    }

                    return true;
                }
                else
                {
                    _logger.Error($"Trade failed for {symbol.Name}: {result.Error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error executing trade for {symbol.Name}: {ex.Message}");
                return false;
            }
        }

   
        private void InitializeTrailingStop(Position position)
        {
            try
            {
                if (position == null) return;

                var trailDistance = _robot.TrailDistance;
                var trailFrom = _robot.TrailFrom;

                if (trailDistance <= 0 || trailFrom <= 0) return;

                // Convert pips to price
                var trailDistanceInPrice = position.Symbol.PipSize * trailDistance;

                // Set trailing stop
                var result = position.ModifyStopLossPrice(position.StopLoss);
                if (!result.IsSuccessful)
                {
                    _logger.Error($"Failed to set initial stop loss for trailing stop: {result.Error}");
                    return;
                }

                // Enable trailing stop
                result = position.ModifyTrailingStop(true);
                if (!result.IsSuccessful)
                {
                    _logger.Error($"Failed to enable trailing stop: {result.Error}");
                    return;
                }

                _logger.Info($"Trailing stop initialized for position {position.Id}: Distance={trailDistance}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing trailing stop: {ex.Message}");
            }
        }


        public bool ModifyPosition(Position position, double? stopLoss = null, double? takeProfit = null)
        {
            try
            {
                if (position == null) return false;

                // Convert pips to price if needed
                double? stopLossPrice = stopLoss.HasValue ? position.Symbol.PipSize * stopLoss.Value : null;
                double? takeProfitPrice = takeProfit.HasValue ? position.Symbol.PipSize * takeProfit.Value : null;

                var result = position.ModifyStopLossPrice(stopLossPrice);
                if (!result.IsSuccessful)
                {
                    _logger.Error($"Failed to modify stop loss for position {position.Id}: {result.Error}");
                    return false;
                }

                result = position.ModifyTakeProfitPrice(takeProfitPrice);
                if (!result.IsSuccessful)
                {
                    _logger.Error($"Failed to modify take profit for position {position.Id}: {result.Error}");
                    return false;
                }

                _logger.Info($"Position {position.Id} modified successfully: SL={stopLoss}, TP={takeProfit}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error modifying position: {ex.Message}");
                return false;
            }
        }


        public bool ClosePosition(Position position)
        {
            try
            {
                if (position == null) return false;

                var result = position.Close();

                if (result.IsSuccessful)
                {
                    _logger.Info($"Position {position.Id} closed successfully");
                    return true;
                }
                else
                {
                    _logger.Error($"Failed to close position {position.Id}: {result.Error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error closing position: {ex.Message}");
                return false;
            }
        }

   
        public bool PartiallyClosePosition(Position position, double volume)
        {
            try
            {
                if (position == null) return false;

                var result = position.ModifyVolume(volume);

                if (result.IsSuccessful)
                {
                    _logger.Info($"Position {position.Id} partially closed: {volume} units");
                    return true;
                }
                else
                {
                    _logger.Error($"Failed to partially close position {position.Id}: {result.Error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error partially closing position: {ex.Message}");
                return false;
            }
        }


        public void UpdateTrailingStops()
        {
            if (!_robot.UseTrailingStop) return;

            try
            {
                foreach (var position in _robot.Positions)
                {
                    if (!position.HasTrailingStop)
                    {
                        InitializeTrailingStop(position);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating trailing stops: {ex.Message}");
            }
        }
    }
} 