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

    public class balloonScript : PartModifierScript<balloonData>,IFlightUpdate,IFlightStart
    {
        public void FlightUpdate(in FlightFrameData frame)
        {
           
        }

        public void FlightStart(in FlightFrameData frame)
        {
            if (PartScript.Data.Activated)
            {
                this.PartScript.BodyScript.RigidBody.AddForce(Data.FloatingForceMultiplier * PartScript.Transform.up);
            }
        }
    }
}