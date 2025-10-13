using ModApi;
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
        private Transform _sphere;
        public void FlightStart(in FlightFrameData frame)
        {
            UpdateComponent();
        }

        private void UpdateComponent()
        {
            _sphere=Utilities.FindFirstGameObjectMyselfOrChildren("Sphere",this.gameObject).transform;
        }

        public void FlightFixedUpdate(in FlightFrameData frame)
        {
            //UpdateScale(PartScript.Data.Activated ? 5:1);
            if (PartScript.Data.Activated)
            {
                float floatingFocrce = Game.Instance.FlightScene.CraftNode.CraftScript.FlightData.AtmosphereSample
                    .AirDensity;
               
                this.PartScript.BodyScript.RigidBody.AddForceAtPosition(Data.FloatingForceMultiplier * floatingFocrce*PartScript.CraftScript.FlightData.GravityFrameNormalized*-1, PartScript.Transform.position);
            }
        }

        private void UpdateScale(float target)
        {
            _sphere.localScale = new Vector3(target, target, target);
            
        }
    }
}