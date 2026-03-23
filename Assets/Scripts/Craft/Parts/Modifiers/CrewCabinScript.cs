using ModApi;
using ModApi.Craft;
using ModApi.Design;
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
    public class CrewCabinScript :PartModifierScript<CrewCabinData>,IFlightStart,IFlightUpdate
    {
        
        public void FlightStart(in FlightFrameData frame)
        {
            
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
           
        }
    }
}