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

    public class balloonScript : PartModifierScript<balloonData>,IFlightFixedUpdate,IFlightStart,IFlightUpdate,IPartSubPartSetUp
    {
        private Transform _sphere;
        private Transform _offset;

        public Transform SubPart => _sphere;
        public void FlightStart(in FlightFrameData frame)
        {
            UpdateComponents();
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            //UpdateScale(_sphere.transform.localScale.x,PartScript.Data.Activated ? 5f:1f);
            _sphere.localScale = (this.PartScript.Data.Activated ? 8f : 1f) * Vector3.one;
            if (PartScript.Data.Activated)
            {
                float floatingFocrce = Game.Instance.FlightScene.CraftNode.CraftScript.FlightData.AtmosphereSample
                    .AirDensity;
               
                this.PartScript.BodyScript.RigidBody.AddForceAtPosition(Data.FloatingForceMultiplier * floatingFocrce*PartScript.CraftScript.FlightData.GravityFrameNormalized*-1, PartScript.Transform.position);
            }
        }


        public void FlightFixedUpdate(in FlightFrameData frame)
        {
            

           
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
        protected void UpdateComponents()
        {
            SetSubPart(IPartSubPartSetUp.FindSubPart(this, "Sphere"));
        }


        public void SetSubPart(Transform subPart)
        {
            _sphere = subPart;
            _offset = IPartSubPartSetUp.ApplySubPart(_offset, _sphere, Data.PositionOffset1, out _);
        }
    }
}