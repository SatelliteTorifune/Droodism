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

        private float growProgress;
        public void FlightStart(in FlightFrameData frame)
        {
            _efficiency = 0;
            _rechargeEfficiency = 0f;
            this._rechargeRate = 0f;
            ReFreshSources();
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
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
            WorkingLogic(frame, PartScript.Data.Activated);
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
            
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.ReFreshSources();
        
        #endregion
        
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