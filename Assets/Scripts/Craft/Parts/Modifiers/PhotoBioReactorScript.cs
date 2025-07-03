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

    public class PhotoBioReactorScript : PartModifierScript<PhotoBioReactorData>, IFlightStart, IFlightUpdate,
        IDesignerStart
    {
        private IFuelSource _battery,_co2Source,_waterSource,_foodSource,_solidWastedSource,_oxygenSource;
        private float _efficiency, _rechargeRate, _rechargeEfficiency,_area;
        private Transform _panel = (Transform) null;
        private Transform MainPipe=(Transform)null;

        private float growProgress;
        public void FlightStart(in FlightFrameData frame)
        {
            _efficiency = 0;
            _rechargeEfficiency = 0f;
            this._rechargeRate = 0f;
            ReFreshSources();
        }

        private float current = 0;
        public void FlightUpdate(in FlightFrameData frame)
        {
            /*
            ICraftFlightData flightData = this.PartScript.CraftScript.FlightData;
            this._efficiency = this.Data.Efficiency;
            this._rechargeRate = (float) flightData.SolarRadiationIntensity * this._efficiency * this._area;
            if ((double) this._rechargeRate > 0.0)
            {
                this._rechargeEfficiency = Mathf.Max(0.0f, Vector3.Dot(this._panel.up, -flightData.SolarRadiationFrameDirection));
                this._rechargeRate *= this._rechargeEfficiency;
            }
            else
                this._rechargeEfficiency = 0.0f;
            this._battery.AddFuel((double) this._rechargeRate * frame.DeltaTimeWorld * (1.0 / 1000.0));
            WorkingLogic(frame, PartScript.Data.Activated);*/
            float target = this.Data.Part.Activated ? 1f : 0.0f;
            if ((double) Data.CurrentEnabledPercent != (double) target)
            {
                this.Data.CurrentEnabledPercent = Mathf.MoveTowards(this.Data.CurrentEnabledPercent, target, frame.DeltaTime * this.Data.RotationRate);
                DeployAnimate(Data.CurrentEnabledPercent);
                
            }
        }

        
        public void WorkingLogic(in FlightFrameData frame, bool isUsingArtificialLight)
        {
            if (_co2Source==null||_waterSource==null||_oxygenSource==null||_battery==null)
            {
                return;
            }
            if(isUsingArtificialLight)
            {
                if (!_co2Source.IsEmpty&&!_waterSource.IsEmpty&&!_battery.IsEmpty&&!_oxygenSource.IsEmpty)
                {
                    bool isBoosted = false;   
                    _co2Source.RemoveFuel(Data.Co2ConsumptionRate * frame.DeltaTimeWorld);
                    _waterSource.RemoveFuel(Data.WaterConsumptionRate * frame.DeltaTimeWorld);
                    _battery.RemoveFuel(Data.PowerConsumptionRate * frame.DeltaTimeWorld);
                    if (_solidWastedSource!= null)
                    {
                        if (!_solidWastedSource.IsEmpty)
                        {
                            _solidWastedSource.RemoveFuel(Data.SolidWasteConsumptionRate * frame.DeltaTimeWorld);
                            isBoosted = true;
                        }
                    }
                    growProgress += Data.GrowSpeed*Data.Efficiency*(isBoosted?Data.BoosteScale:1.0f);
                    if (growProgress >= Data.GrowProgressTotal)
                    {
                        growProgress = 0;
                        OnProgressBarFull();
                    }
                    if (_oxygenSource.TotalCapacity-_oxygenSource.TotalFuel<=0.00001f)
                    {
                        _oxygenSource.AddFuel(Data.OxygenGenerationRate * frame.DeltaTimeWorld);
                    }
                }
                else
                {
                    if (growProgress > 0)
                    {
                        growProgress -= Data.DecaySpeed;
                    } 
                }
            }
            else
            {
                
            }
        }
        private void OnProgressBarFull()
        {
            if (_foodSource == null)
            {
                Debug.LogFormat("PhotobioReactor:OnProgressBarFull: No food source found");
                return;
            }
            
            if (_foodSource.TotalCapacity-_foodSource.TotalFuel<=Data.FoodGeneratedScale)
            {
                _foodSource.AddFuel(_foodSource.TotalCapacity-_foodSource.TotalFuel);
            }
            else
            {
                _foodSource.AddFuel(Data.FoodGeneratedScale);
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

        private Transform _offset;
        private Vector3 _offsetPositionInverse;
        public void SetSubPart(Transform subPart)
        {
            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                UnityEngine.Object.Destroy((UnityEngine.Object) this._offset.gameObject);
                this._offset = (Transform) null;
            }
            this.MainPipe = subPart;
            if (!((UnityEngine.Object) this.MainPipe != (UnityEngine.Object) null) || (double) this.Data.PositionOffset1.magnitude <= 0.0)
                return;
            this._offset = new GameObject("SubPartRotatorOffset").transform;
            this._offset.SetParent(this.MainPipe.parent, false);
            this._offset.position = this.MainPipe.TransformPoint(this.Data.PositionOffset1);
            this._offsetPositionInverse = this._offset.InverseTransformPoint(this.MainPipe.position);
        }
        float fanSpeed = 0.0f;
        public float AngleMultiplier { get; set; } = 1f;
        private void DeployAnimate(float percent)
        {
            if ((UnityEngine.Object) this.MainPipe == (UnityEngine.Object) null)
            {
                Debug.LogWarning((object) "SubPartRotator has no defined sub part.", (UnityEngine.Object) this);
            }
            MainPipe.localRotation = this.Data.AngleLerp != SubPartRotatorData.AngleLerpType.Quaternion ? Quaternion.Euler(Vector3.Lerp(this.Data.DisabledRotation * this.AngleMultiplier, this.Data.EnabledRotation * this.AngleMultiplier, percent)) : Quaternion.Lerp(Quaternion.Euler(this.Data.DisabledRotation * this.AngleMultiplier), Quaternion.Euler(this.Data.EnabledRotation * this.AngleMultiplier),percent);
            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                this._offset.localRotation = this.MainPipe.localRotation;
                this.MainPipe.position = this._offset.TransformPoint(this._offsetPositionInverse);
            }
            this.Data.CurrentEnabledPercent = percent;
        }

        private void ReFreshSources()
        {
            _waterSource = GetCraftFuelSource("H2O");
            _co2Source = GetCraftFuelSource("CO2");
            _foodSource = GetCraftFuelSource("FO2");
            _oxygenSource = GetCraftFuelSource("Oxygen");
            _solidWastedSource = GetCraftFuelSource("Solid Wasted");
            _battery = PartScript.BatteryFuelSource;
        }
        
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

        private void UpdateScale()
        {
            
        }
    }
    
}