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
            UpdateScale(_sphere.transform.localScale.x,PartScript.Data.Activated ? 5f:1f);
            if (PartScript.Data.Activated)
            {
                float floatingFocrce = Game.Instance.FlightScene.CraftNode.CraftScript.FlightData.AtmosphereSample
                    .AirDensity;
               
                this.PartScript.BodyScript.RigidBody.AddForceAtPosition(Data.FloatingForceMultiplier * floatingFocrce*PartScript.CraftScript.FlightData.GravityFrameNormalized*-1, PartScript.Transform.position);
            }

           
        }

        private void UpdateScale(float current,float target)
        {
            if (current > target)
            {
                _sphere.transform.localScale = new Vector3(_sphere.transform.localScale.x - 1f, _sphere.transform.localScale.y - 1f, _sphere.transform.localScale.z - 1f);
            }

            if (current <= target)
            {
                _sphere.transform.localScale = new Vector3(_sphere.transform.localScale.x + 1f, _sphere.transform.localScale.y + 1f, _sphere.transform.localScale.z + 1f);
            }
            
            
        }
    }
}