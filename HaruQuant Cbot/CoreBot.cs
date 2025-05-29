using System;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Robots.Utils;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, AddIndicators = true)]
    public class Corebot : Robot
    {
        private Logger _logger;

        protected override void OnStart()
        {
            // Initialize the logger
            _logger = new Logger(this, BotConfig.BotName, BotConfig.BotVersion);

            // Print startup message
            _logger.Info($"{BotConfig.BotName} v{BotConfig.BotVersion} started successfully!");

        }

        protected override void OnTick()
        {
            // We don't need to do anything on tick for this strategy
        }
        
        protected override void OnBar()
        {
            // Bar event handler
        }

        protected override void OnStop()
        {
            // Print shutdown message
            _logger.Info($"{BotConfig.BotName} shutdown.");
        }
    }
} 