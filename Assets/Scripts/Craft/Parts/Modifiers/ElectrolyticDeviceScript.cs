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

    public class ElectrolyticDeviceScript : PartModifierScript<ElectrolyticDeviceData>,IFlightStart, IDesignerStart,IFlightUpdate
    {
        private IFuelSource _waterSource,_oxgenSource,_hydrogenSource,_batterySource;

        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            var craftSources = PartScript.CraftScript.FuelSources.FuelSources;
            
            foreach (var source in craftSources)
            {
                if (source.FuelType.Id.Contains(fuelType))
                {
                    return source;
                }
            }
            return null;
        }
        
        private Transform fanTransform;
        private Transform fanTransform2;
        private Transform _offset;
        private Vector3 _offsetPositionInverse;

        private float fanSpeed;
        private void ReFreshSources()
        {
            _batterySource = PartScript.BatteryFuelSource;
            var patchScript = PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            if (patchScript == null)
            {
                _waterSource=EmptyFuelSource.GetOrCreate(Game.Instance.PropulsionData.GetFuelType("H2O"));
                _oxgenSource=EmptyFuelSource.GetOrCreate(Game.Instance.PropulsionData.GetFuelType("Oxygen"));
                _hydrogenSource=EmptyFuelSource.GetOrCreate(Game.Instance.PropulsionData.GetFuelType("LH2"));
            }

            _waterSource = patchScript.WaterFuelSource;
            _oxgenSource=patchScript.OxygenFuelSource;
            _hydrogenSource = GetCraftFuelSource("LH2");
        }
        public void FlightStart(in FlightFrameData frame)
        {
           ReFreshSources();
        }

        
        public void FlightUpdate(in FlightFrameData frame)
        {
           WorkingLogic(frame);
           AnimateComponents(PartScript.Data.Activated);
        }

        private void WorkingLogic(in FlightFrameData frame)
        {
            if (!PartScript.Data.Activated)
            {
               return; 
            }
            if (_waterSource != null && _oxgenSource != null && _batterySource != null)
            {
                double waterToRemove=Data.WaterComsuptionRate*frame.DeltaTimeWorld;
                double oxygenToAdd=Data.OxygenGenerationRate*frame.DeltaTimeWorld;
                double batteryToRemove=Data.PowerConsumptionRate*frame.DeltaTimeWorld;
                if (!_batterySource.IsEmpty&&!_waterSource.IsEmpty&&_oxgenSource.TotalCapacity-_oxgenSource.TotalFuel>0.000001)
                {
                    _waterSource.RemoveFuel(waterToRemove);
                    _batterySource.RemoveFuel(batteryToRemove);
                    _oxgenSource.AddFuel(oxygenToAdd);
                    if (_hydrogenSource!= null&& _hydrogenSource.TotalCapacity-_hydrogenSource.TotalFuel>0.000001)
                    {
                        double hydroToRemove=Data.HydrogenGenerationRate*frame.DeltaTimeWorld;
                        _hydrogenSource.AddFuel(hydroToRemove);
                    }
                }
                
                

            }
        }
        #region 路边一条
        
        public void DesignerStart(in DesignerFrameData frame)
        {
            ReFreshSources();
        }
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            ReFreshSources();
        }
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            
            this.UpdateScale();
           
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            this.UpdateScale();
            UpdateComponents();
            
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.ReFreshSources();
        
        #endregion

        void UpdateScale()
        {
            
        }

        private void UpdateComponents()
        {
            string[] strArray = this.Data.SubPartPath.Split('/', StringSplitOptions.None);
            Transform subPart = this.transform;
            foreach (string n in strArray)
                subPart = subPart.Find(n) ?? subPart;
            if (subPart.name == strArray[strArray.Length - 1])
                this.SetSubPart(subPart);
            else
                this.SetSubPart(Utilities.FindFirstGameObjectMyselfOrChildren(this.Data.SubPartPath, this.gameObject)?.transform);
        }
        
        public void SetSubPart(Transform subPart)
        {
            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                UnityEngine.Object.Destroy((UnityEngine.Object) this._offset.gameObject);
                this._offset = (Transform) null;
            }
            this.fanTransform = subPart;
            if (!((UnityEngine.Object) this.fanTransform != (UnityEngine.Object) null) || (double) this.Data.PositionOffset.magnitude <= 0.0)
                return;
            this._offset = new GameObject("SubPartRotatorOffset").transform;
            this._offset.SetParent(this.fanTransform.parent, false);
            this._offset.position = this.fanTransform.TransformPoint(this.Data.PositionOffset);
            this._offsetPositionInverse = this._offset.InverseTransformPoint(this.fanTransform.position);
        }
        
        public void AnimateComponents(bool active)
        {
            
            float b = 0.0f;
            if (active)
                b = 0.25f + 0.5f;
            fanSpeed= Mathf.Lerp(this.fanSpeed, b, Time.deltaTime * 0.5f);
            if ((double) this.fanSpeed > 0.0)
            {
                float zAngle = (float) (-(double) this.fanSpeed * 360.0 * 3.0) * Time.deltaTime;
                fanTransform.Rotate(0.0f, 0.0f, zAngle);
            }
            
        }
    }
    
}