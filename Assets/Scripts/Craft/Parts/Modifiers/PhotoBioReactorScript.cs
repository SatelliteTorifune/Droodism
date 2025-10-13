using System.Numerics;
using ModApi;
using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.Math;
using ModApi.Ui.Inspector;

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
        private IFuelSource _battery,_co2Source,_waterSource,_foodSource,_solidWastedSource,_oxygenSource,_wastedWaterSource;
        public float _efficiency, _rechargeRate, _rechargePointingEfficiency;
        private float _area = 10;
        private Transform _panel = (Transform) null;
        private Transform MainPipe,L1,L2,L3,R1,R2,R3=(Transform)null;
        bool isBoosted = false;
        private Transform _offset;
        private Vector3 _offsetPositionInverse;
        private string deviceStatus = "";
        public bool usingArtificialLight = false;
        private float growProgress;
        public void FlightStart(in FlightFrameData frame)
        {
            _efficiency = 0;
            _rechargePointingEfficiency = 0f;
            this._rechargeRate = 0f;
            ReFreshSources();
        }

        //private float current = 0;
        public void FlightUpdate(in FlightFrameData frame)
        {
            float target = this.Data.Part.Activated ? 1f : 0.0f;
            if (Data.CurrentEnabledPercent != target)
            {
                this.Data.CurrentEnabledPercent = Mathf.MoveTowards(this.Data.CurrentEnabledPercent, target, frame.DeltaTime * this.Data.RotationRate);
                DeployAnimate(Data.CurrentEnabledPercent);
            }
            
            if (Data.Part.Activated&&Data.CurrentEnabledPercent>=0.99f)
            {
                WorkingLogic(frame);
            }
            else
            {
                deviceStatus = "<color=yellow>Device is Offline</color>";
            }
            
        }

        private void WorkingLogic(in FlightFrameData frame)
        {
            if(_co2Source==null||_waterSource==null||_battery==null||_oxygenSource==null||_wastedWaterSource==null)
            {
                return; 
            }

            this._rechargeRate = 0.0f;
            this._rechargePointingEfficiency = 0.0f;
            if (!_co2Source.IsEmpty && !_waterSource.IsEmpty && !_battery.IsEmpty &&_wastedWaterSource.TotalCapacity - _wastedWaterSource.TotalFuel >= 0.00001f)
            {
                if (usingArtificialLight)
                {
                    deviceStatus = "<color=green>Using Artificial Light,Generating Food</color>";
                    isBoosted = false;
                    _co2Source.RemoveFuel(Data.Co2ConsumptionRate * frame.DeltaTimeWorld);
                    _waterSource.RemoveFuel(Data.WaterConsumptionRate * frame.DeltaTimeWorld);
                    _battery.RemoveFuel(Data.PowerConsumptionRate * frame.DeltaTimeWorld);
                    _wastedWaterSource.AddFuel(Data.WastedWaterGenerationRate * frame.DeltaTimeWorld);
                    if (_oxygenSource.TotalCapacity - _oxygenSource.TotalFuel >= 0.00001f)
                    {
                        _oxygenSource.AddFuel(Data.OxygenGenerationRate * frame.DeltaTimeWorld);
                    }

                    if (_solidWastedSource != null&&!_solidWastedSource.IsEmpty)
                    {
                        _solidWastedSource.RemoveFuel(Data.SolidWasteConsumptionRate * frame.DeltaTimeWorld);
                        isBoosted = true;
                    }
                    growProgress += Math.Abs((float)frame.DeltaTimeWorld*Data.GrowSpeed*Data.Efficiency*(isBoosted?Data.BoosteScale:1.0f));
                }

                if (!usingArtificialLight)
                {
                    ICraftFlightData flightData = this.PartScript.CraftScript.FlightData;
                    this._efficiency = this.Data.Efficiency;
                    this._rechargeRate = (float) flightData.SolarRadiationIntensity * this._efficiency * this._area;
                    if ((double) this._rechargeRate > 0.0)
                    {
                        this._rechargePointingEfficiency = Mathf.Max(0.0f, Vector3.Dot(_panel.up, -flightData.SolarRadiationFrameDirection));
                        this._rechargeRate *= this._rechargePointingEfficiency;
                    }
                    else
                        this._rechargePointingEfficiency = 0.0f;
                    if(_rechargePointingEfficiency<=0.001f)
                    {
                        if (growProgress>=0)
                        {
                            growProgress -= Data.DecaySpeed;
                        }
                        deviceStatus = "<color=red>Unable to generate food:Unable to get solar radiation</color>";
                        return;
                    }
                    deviceStatus = "<color=green>Using Solar Light,Generating Food</color>";
                    isBoosted = false;
                    _co2Source.RemoveFuel(Data.Co2ConsumptionRate*_rechargePointingEfficiency * frame.DeltaTimeWorld);
                    _waterSource.RemoveFuel(Data.WaterConsumptionRate*_rechargePointingEfficiency* frame.DeltaTimeWorld);
                    _wastedWaterSource.AddFuel(Data.WastedWaterGenerationRate*_rechargePointingEfficiency * frame.DeltaTimeWorld);
                    if (_oxygenSource.TotalCapacity - _oxygenSource.TotalFuel >= 0.00001f)
                    {
                        _oxygenSource.AddFuel(Data.OxygenGenerationRate*_rechargePointingEfficiency * frame.DeltaTimeWorld);
                    }

                    if (_solidWastedSource != null&&!_solidWastedSource.IsEmpty)
                    {
                        _solidWastedSource.RemoveFuel(Data.SolidWasteConsumptionRate*_rechargePointingEfficiency * frame.DeltaTimeWorld);
                        isBoosted = true;
                    }
                    growProgress += Math.Abs((float)frame.DeltaTimeWorld*Data.GrowSpeed*_rechargePointingEfficiency*Data.Efficiency*(isBoosted?Data.BoosteScale:1.0f));
                    
                }
            }
            if (_co2Source.IsEmpty || _waterSource.IsEmpty||_battery.IsEmpty)
            {
                if (growProgress>=0)
                {
                    growProgress -= Data.DecaySpeed;
                }
                deviceStatus = "<color=red>Unable to generate food:Lack of Resources</color>";
                return;
            }
            
            if (_wastedWaterSource.TotalCapacity - _wastedWaterSource.TotalFuel <= 0.00001f)
            {
                if (growProgress>=0)
                {
                    growProgress -= Data.DecaySpeed;
                }
                deviceStatus = "<color=red>Unable to generate food:Wasted Water is full</color>";
                return;
            }
            
            if (growProgress >= Data.GrowProgressTotal)
            {
                growProgress = 0;
                OnProgressBarFull();
            }
            
            
            
        }
        
       
        private void OnProgressBarFull()
        {
            
            growProgress = 0;
            if (_foodSource == null)
            {
                return;
            }
            
            if ((_foodSource.TotalCapacity-_foodSource.TotalFuel)<=Data.FoodGeneratedScale)
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
            if (MainPipe != null)
            {
                _panel = MainPipe.Find("PipeMesh1");
                L1 = MainPipe.Find("L1");
                L2 = L1.Find("L2");
                L3 = L2.Find("L3");
                R1= MainPipe.Find("R1");
                R2= R1.Find("R2");
                R3= R2.Find("R3");

                if (L1 == null) Debug.LogWarning("L1 not found under MainPipe");
                if (L2 == null) Debug.LogWarning("L2 not found under L1");
                if (L3 == null) Debug.LogWarning("L3 not found under L3");
            }
        }

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
            this._offset.position = this.MainPipe.TransformPoint(Data.PositionOffset1);
            this._offsetPositionInverse = this._offset.InverseTransformPoint(this.MainPipe.position);
        }
        public float AngleMultiplier { get; set; } = 1f;
        private void DeployAnimate(float percent)
        {
            if ((UnityEngine.Object) this.MainPipe == (UnityEngine.Object) null||(UnityEngine.Object) this.L1 == (UnityEngine.Object) null)
            {
                Debug.LogWarning((object) "SubPartRotator has no defined sub part.", (UnityEngine.Object) this);
            }
            MainPipe.localRotation = this.Data.AngleLerp != SubPartRotatorData.AngleLerpType.Quaternion ? Quaternion.Euler(Vector3.Lerp(this.Data.DisabledRotation * this.AngleMultiplier, new Vector3(90,0,0) * this.AngleMultiplier, percent)) : Quaternion.Lerp(Quaternion.Euler(this.Data.DisabledRotation * this.AngleMultiplier), Quaternion.Euler(new Vector3(90,0,0) * this.AngleMultiplier),percent);
            RotateSub(ref L1,new Vector3(0,0,45) * this.AngleMultiplier,this.Data.DisabledRotation * this.AngleMultiplier);
            RotateSub(ref L2,new Vector3(0,0,-75) * this.AngleMultiplier,new Vector3(0,0,-90));
            RotateSub(ref L3,new Vector3(0,0,60)  * this.AngleMultiplier,this.Data.DisabledRotation);
            RotateSub(ref R1,new Vector3(0,0,-45) * this.AngleMultiplier,this.Data.DisabledRotation);
            RotateSub(ref R2,new Vector3(0,0,75) * this.AngleMultiplier,new Vector3(0,0,90));
            RotateSub(ref R3,new Vector3(0,0,-60) * this.AngleMultiplier,this.Data.DisabledRotation);
            

            void RotateSub(ref Transform t,Vector3 euler,Vector3 disableRotation)
            {
                t.localRotation = this.Data.AngleLerp != SubPartRotatorData.AngleLerpType.Quaternion ? Quaternion.Euler(Vector3.Lerp(disableRotation * this.AngleMultiplier, euler, percent)) : Quaternion.Lerp(Quaternion.Euler(disableRotation * this.AngleMultiplier), Quaternion.Euler(euler),percent);
            }

            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                this._offset.localRotation = this.MainPipe.localRotation;
                this.MainPipe.position = this._offset.TransformPoint(this._offsetPositionInverse);
            }
            this.Data.CurrentEnabledPercent = percent;
        }
        private void ReFreshSources()
        {
            
            _battery = PartScript.BatteryFuelSource;
            try
            {
                var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                if (patchScript == null)
                {
                    _waterSource=_wastedWaterSource=_battery=_solidWastedSource=_oxygenSource=_co2Source=null;
                }

                if (patchScript!=null)
                {
                    _waterSource = patchScript.WaterFuelSource;
                    _oxygenSource=patchScript.OxygenFuelSource;
                    _co2Source=patchScript.CO2FuelSource;
                    _wastedWaterSource=patchScript.WastedWaterFuelSource;
                    _solidWastedSource=patchScript.SolidWasteFuelSource;
                    _foodSource=patchScript.FoodFuelSource;
                }
                
            }
            catch (Exception)
            {
                _waterSource=_wastedWaterSource=_battery=_solidWastedSource=_oxygenSource=_co2Source=null;
            }
            
            
        }
        

        private void UpdateScale()
        {
            
        }
        #endregion
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            var pointingEfficiencyModel = new TextModel("Pointing Efficiency", (Func<string>) (() => Units.GetPercentageString(this._rechargePointingEfficiency)));
            var GrowProgressPercentModel = new TextModel("Food Generation Percentage", (Func<string>) (() => Units.GetPercentageString(growProgress/Data.GrowProgressTotal)));
            var GrowProgressBarModel = new ProgressBarModel("Food Generation Progress", () =>
                (float)(growProgress/Data.GrowProgressTotal));
            var statues=new TextModel("Status",()=> deviceStatus);
            var toggleArtificialLight = new ToggleModel("Use Artificial Light", () => usingArtificialLight, (Action<bool>) (b=>
            {
                usingArtificialLight = b;
                pointingEfficiencyModel.Visible = !b;

            }),"Using Electronic Artificial Light to Working");
            model.Add(pointingEfficiencyModel);
            model.Add(GrowProgressBarModel);
            model.Add(GrowProgressPercentModel);
            model.Add(statues);
            model.Add(toggleArtificialLight);
        }
    }
    
}