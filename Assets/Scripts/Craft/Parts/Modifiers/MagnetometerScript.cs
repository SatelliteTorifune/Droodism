using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class MagnetometerScript : PartModifierScript<MagnetometerData>,IFlightStart,IFlightUpdate
    {
        public void FlightStart(in FlightFrameData frame)
        {
            Game.Instance.GameState.Career.ReceiveTechPoints(10);
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            WorkingLogic(frame);
        }

        private void WorkingLogic(in FlightFrameData frame)
        {
            
        }

        
    }
}