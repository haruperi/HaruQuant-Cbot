using System;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, AddIndicators = true)]
    public class Corebot : Robot
    {


        protected override void OnStart()
        {
            // Print startup message
            Print("HaruQuant Corebot started successfully!");

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
            Print("HaruQuant Corebot shutdown.");
        }
    }
} 