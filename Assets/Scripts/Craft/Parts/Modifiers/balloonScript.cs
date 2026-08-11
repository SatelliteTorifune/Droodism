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

    public class balloonScript : PartModifierScript<balloonData>,IFlightFixedUpdate,IFlightStart,IFlightUpdate
    {
        private Transform _sphere;
        private Transform _offset;
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
            string[] strArray = "Sphere".Split('/', StringSplitOptions.None);
            Transform subPart = this.transform;
            foreach (string n in strArray)
                subPart = subPart.Find(n) ?? subPart;
            if (subPart.name == strArray[strArray.Length - 1])
                this.SetSubPart(subPart);
            else
                this.SetSubPart(Utilities.FindFirstGameObjectMyselfOrChildren("Sphere", this.gameObject)?.transform);
           
            
        }


        private void SetSubPart(Transform subPart)
        {
            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                UnityEngine.Object.Destroy((UnityEngine.Object) this._offset.gameObject);
                this._offset = (Transform) null;
            }
            this._sphere = subPart;
            if (!((UnityEngine.Object) this._sphere != (UnityEngine.Object) null) || (double) this.Data.PositionOffset1.magnitude <= 0.0)
                return;
            this._offset = new GameObject("SubPartRotatorOffset").transform;
            this._offset.SetParent(this._sphere.parent, false);
            this._offset.position = this._sphere.TransformPoint(Data.PositionOffset1);
        }
    }
}