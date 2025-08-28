using System;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None, AddIndicators = true)]
    public class CoreBot : Robot
    {


        protected override void OnStart()
        {
            Print("OnStart");
        }

        protected override void OnTick()
        {
            Print("OnTick");
        }

        protected override void OnBar()
        {
            Print("OnBar");
        }

        protected override void OnStop()
        {
            Print("OnStop");
        }
    }
}
