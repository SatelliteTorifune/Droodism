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

    public class balloonScript : PartModifierScript<balloonData>,IFlightFixedUpdate,IFlightStart
    {

        public void FlightStart(in FlightFrameData frame)
        {
           
        }

        public void FlightFixedUpdate(in FlightFrameData frame)
        {
            if (PartScript.Data.Activated)
            {
                float floatingFocrce = Game.Instance.FlightScene.CraftNode.CraftScript.FlightData.AtmosphereSample
                    .AirDensity;
               
                this.PartScript.BodyScript.RigidBody.AddForceAtPosition(Data.FloatingForceMultiplier * floatingFocrce*PartScript.CraftScript.FlightData.GravityFrameNormalized*-1, PartScript.Transform.position);
            }
        }
    }
}