using System;
using System.IO;
// using System.IO.IsolatedStorage; // No longer directly used here
using System.Linq;
// using System.Text.Json; // No longer directly used here
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Robots.Utils; // This now brings in BotState, ErrorHandlerService, and BotErrorException
// using cAlgo.Robots.ErrorHandling; // No longer needed as classes moved to Utils

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess, AddIndicators = true)]
    public class Corebot : Robot
    {
        private Logger _logger;
        private BotState _botState;
        private ErrorHandlerService _errorHandler; // Added ErrorHandlerService
        private const string StateFileName = "BotState.json"; // File name for isolated storage

        protected override void OnStart()
        {
            _logger = new Logger(this, BotConfig.BotName, BotConfig.BotVersion);
            _errorHandler = new ErrorHandlerService(_logger); // Initialize ErrorHandlerService
            
            // Load state using the static method in BotState
            _botState = BotState.Load(_logger, StateFileName);

            _logger.Info($"{BotConfig.BotName} v{BotConfig.BotVersion} started successfully!");
            
            // Populate _botState with any initial runtime data if necessary, after loading
            // For example, if reconciliation with current positions is needed immediately.
            // However, the main population of what to save should happen before calling _botState.Save()

            if (_botState.ActiveTradeLabels.Any())
            {
                _logger.Info($"Restored state with {_botState.ActiveTradeLabels.Count} active trade labels.");
                // Add reconciliation logic here: Compare _botState.ActiveTradeLabels with Positions
            }
        }

        protected override void OnTick()
        {
            // We don't need to do anything on tick for this strategy
        }
        
        protected override void OnBar()
        {
            // Bar event handler
            // If you choose to save state periodically:
            // if (_botState != null) 
            // {
            //     // Ensure _botState is populated with the latest data before saving
            //     UpdateStateBeforePeriodicSave(); 
            //     _botState.Save(_logger, StateFileName);
            // }
        }

        protected override void OnStop()
        {
            if (_botState != null)
            {
                // Ensure _botState is populated with the final data before saving
                _botState.ActiveTradeLabels.Clear();
                foreach (var position in Positions)
                {
                    if (!string.IsNullOrEmpty(position.Label))
                    {
                        _botState.ActiveTradeLabels.Add(position.Label);
                    }
                }
                // _botState.CustomStrategyParameter = currentStrategyValueToSave;

                _botState.Save(_logger, StateFileName);
            }
            _logger.Info($"{BotConfig.BotName} shutdown.");
        }

        // Removed SaveState() and LoadState() methods from CoreBot

        // Optional: If you save periodically in OnBar, you might have a method like this
        // private void UpdateStateBeforePeriodicSave()
        // {
        //     // Example: Update any dynamic properties of _botState here
        //     // _botState.SomeDynamicValue = GetCurrentDynamicValue();
        // }
    }
} 