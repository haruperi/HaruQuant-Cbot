using System;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None, AddIndicators = true)]
    public class HaruQuantCbot : Robot
    {
        protected override void OnStart()
        {
         // Funtion to initialize the cBot
        }

        protected override void OnTick()
        {
            // Funtion to handle the tick event
        }
        
        protected override void OnBar()
        {
            // Funtion to handle the bar event
        }

        protected override void OnStop()
        {
            // Funtion to handle the stop event
        }
    }
}