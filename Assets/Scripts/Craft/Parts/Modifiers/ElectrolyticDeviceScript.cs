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
                if (source.FuelType.Id==fuelType)
                {
                    return source;
                }
            }
            return null;
        }
        
        private Transform fanTransformBase;
        private Transform fan1, fan2;
        private Transform _offset;
        private Vector3 _offsetPositionInverse;

        private float fanSpeed;
        private void ReFreshSources()
        {
            _batterySource = PartScript.BatteryFuelSource;
            try
            {
                var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                if (patchScript == null)
                {
                    _waterSource = _hydrogenSource = _oxgenSource = null;
                }

                if (patchScript!= null)
                {
                    _waterSource = patchScript.WaterFuelSource;
                    _oxgenSource=patchScript.OxygenFuelSource;
                    _hydrogenSource = GetCraftFuelSource("LH2");
                }
            }
            catch (Exception)
            {
                _waterSource = _hydrogenSource = _oxgenSource = null;
            }
            

            
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
               
                if (!_batterySource.IsEmpty&&!_waterSource.IsEmpty&&_oxgenSource.TotalCapacity-_oxgenSource.TotalFuel>0.000001)
                {
                    _waterSource.RemoveFuel(Data.WaterComsuptionRate*frame.DeltaTimeWorld);
                    _batterySource.RemoveFuel(Data.OxygenGenerationRate*frame.DeltaTimeWorld);
                    _oxgenSource.AddFuel(Data.PowerConsumptionRate*frame.DeltaTimeWorld);
                    if (_hydrogenSource!= null&& _hydrogenSource.TotalCapacity-_hydrogenSource.TotalFuel>0.000001)
                    {
                        _hydrogenSource.AddFuel(Data.HydrogenGenerationRate*frame.DeltaTimeWorld);
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
            string[] strArray = "Device/DeviceFan".Split('/', StringSplitOptions.None);
            Transform subPart = this.transform;
            foreach (string n in strArray)
                subPart = subPart.Find(n) ?? subPart;
            if (subPart.name == strArray[strArray.Length - 1])
                this.SetSubPart(subPart);
            else
                this.SetSubPart(Utilities.FindFirstGameObjectMyselfOrChildren("Device/DeviceFan", this.gameObject)?.transform);
            fan1 = fanTransformBase.Find("fan1");
            fan2 = fanTransformBase.Find("fan2");
        }
        
        public void SetSubPart(Transform subPart)
        {
            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                UnityEngine.Object.Destroy((UnityEngine.Object) this._offset.gameObject);
                this._offset = (Transform) null;
            }
            this.fanTransformBase = subPart;
            if (!((UnityEngine.Object) this.fanTransformBase != (UnityEngine.Object) null) || (double) this.Data.PositionOffset.magnitude <= 0.0)
                return;
            this._offset = new GameObject("SubPartRotatorOffset").transform;
            this._offset.SetParent(this.fanTransformBase.parent, false);
            this._offset.position = this.fanTransformBase.TransformPoint(this.Data.PositionOffset);
            this._offsetPositionInverse = this._offset.InverseTransformPoint(this.fanTransformBase.position);
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
                fan1.Rotate(0.0f, 0.0f, zAngle);
                fan2.Rotate(0.0f, 0.0f, -zAngle);
            }
        }
    }
    
}