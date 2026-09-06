using ModApi;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class balloonScript : PartModifierScript<balloonData>,IFlightStart,IFlightFixedUpdate,IFlightUpdate,IPartSubPartSetUp
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
            if (_sphere != null)
            {
                _sphere.localScale = (this.PartScript.Data.Activated ? 8f : 1f) * Vector3.one;
            }
            
        }

        public void FlightFixedUpdate(in FlightFrameData frame)
        {
            if (PartScript.Data.Activated)
            {
                float floatingForce = Game.Instance.FlightScene.CraftNode.CraftScript.FlightData.AtmosphereSample
                    .AirDensity;
               
                this.PartScript.BodyScript.RigidBody.AddForceAtPosition(Data.FloatingForceMultiplier * floatingForce*PartScript.CraftScript.FlightData.GravityFrameNormalized*-1, PartScript.Transform.position);
            }
        }
        protected void UpdateComponents()
        {
            SetSubPart(IPartSubPartSetUp.FindSubPart(this, "Sphere"));
        }


        public void SetSubPart(Transform subPart)
        {
            _sphere = subPart;
            _offset = IPartSubPartSetUp.ApplySubPart(_offset, _sphere, Data.PositionOffset, out _);
        }
    }
}