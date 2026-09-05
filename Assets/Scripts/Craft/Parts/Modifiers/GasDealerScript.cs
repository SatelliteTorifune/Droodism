using ModApi;
using ModApi.Audio;
using ModApi.Craft;
using ModApi.GameLoop;
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

    public class GasDealerScript : PartModifierScript<GasDealerData>,
        IPartSubPartSetUp,
        IDesignerStart,
        IFlightStart,
        IFlightUpdate

    {
        private ParticleSystem _particleSystem;
        private ParticleSystem.EmissionModule _particleSystemEmission;
        private ParticleSystem.MainModule _particleSystemMain;
       
        
        private Transform _particleSystemTransform;

        public Transform SubPart => _particleSystemTransform;
        
        private IFuelSource highPressureGasSource;
        private IFuelSource lowPressureGasSource;
        private IFuelSource batterySource;
        private bool emergencyGasDepressurization=false;
        private bool isFunctional=true;
       
        public bool isPressuring { get;private set; }  

        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
            UpdatePartType();
        }

        public void FlightStart(in FlightFrameData frame)
        {
            UpdatePartType();
            RefreshFuelSources();
            if (!isPressuring)
            {
                UpdateComponents();
            }
            
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
           
            if (!isFunctional)
                return;
            if (!PartScript.Data.Activated)
            {
                if (emergencyGasDepressurization)
                {
                    EmergencyDepressurization(frame);
                    return;
                }
                return;
            }
            if (emergencyGasDepressurization)
            {
                EmergencyDepressurization(frame);
                return;
            }
            WorkingLogic(frame);
            
        }

        private void WorkingLogic(in FlightFrameData frame)
        {
            if (highPressureGasSource == null||lowPressureGasSource == null)
                return;
            //理论上来说Data.GasFlowRate * lowPressureGasSource.FuelType.Density/highPressureGasSource.FuelType.Density这么写是完全没毛病的,但是出于一种我也不知道的玄学原因,游戏会发癫,凭空给我生成fuel,所以最快的解决方法就算直接*0.973,然后对外宣称这是正常损耗,这样完全不会有人怀疑对吧
            //哈哈,我他妈真是天才.
            if (isPressuring&&!lowPressureGasSource.IsEmpty&&(highPressureGasSource.TotalCapacity - highPressureGasSource.TotalFuel > 1E-06)&&batterySource!=null&&!batterySource.IsEmpty)
            { 
                lowPressureGasSource.RemoveFuel(Data.GasFlowRate* frame.DeltaTimeWorld);
                
                highPressureGasSource.AddFuel(0.972*Data.GasFlowRate * lowPressureGasSource.FuelType.Density/highPressureGasSource.FuelType.Density * frame.DeltaTimeWorld);
                batterySource.RemoveFuel(Data.GasFlowRate * frame.DeltaTimeWorld*Data.BatteryConsumption);
            }
            if (!isPressuring&&!highPressureGasSource.IsEmpty&&lowPressureGasSource.TotalCapacity-lowPressureGasSource.TotalFuel>1E-06&&emergencyGasDepressurization==false)
            {
                highPressureGasSource.RemoveFuel(Data.GasFlowRate* frame.DeltaTimeWorld);
                lowPressureGasSource.AddFuel(0.972*Data.GasFlowRate * highPressureGasSource.FuelType.Density/lowPressureGasSource.FuelType.Density * frame.DeltaTimeWorld);
            }
        }
        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            foreach (var source in PartScript.CraftScript.FuelSources.FuelSources)
            {
                if (source.FuelType.Id== fuelType)
                {
                    return source;
                }
            }
            return null;
        }

        private void EmergencyDepressurization(in FlightFrameData frame)
        {
            if (lowPressureGasSource.IsEmpty&&highPressureGasSource.IsEmpty)
            {
                isFunctional = false;
                
                this._particleSystem.Stop();
                return;
            }

            if (_particleSystem!=null)
            {
                this._particleSystemMain.startColor = (ParticleSystem.MinMaxGradient) new Color(1f, 1f, 1f, (float)Math.Max(0.4, 10* highPressureGasSource.TotalFuel / highPressureGasSource.TotalCapacity));
                
                this._particleSystemMain.gravitySource = 0;
                this._particleSystemMain.gravityModifierMultiplier = 0.01f;
                //this._particleSystemEmission.enabled = true;
                if (!this._particleSystem.isPlaying)
                    this._particleSystem.Play();
            }
            
            if (!highPressureGasSource.IsEmpty)
            {
                highPressureGasSource.RemoveFuel(Data.GasFlowRate * 60*Math.Max(0.1, 2* highPressureGasSource.TotalFuel / highPressureGasSource.TotalCapacity) * frame.DeltaTimeWorld);
            }

            if (!lowPressureGasSource.IsEmpty)
            {
                lowPressureGasSource.RemoveFuel(Data.GasFlowRate * 40 *Math.Max(0.1, 2* lowPressureGasSource.TotalFuel / lowPressureGasSource.TotalCapacity)* frame.DeltaTimeWorld);
            }

            

        }
        
        #region 路边一条


        private void UpdatePartType()
        {
            isPressuring = Data.Part.PartType.Id != "GasDepressurizeDevice";
        }
        public void ToggleParticles(bool active)
        {
            if ((UnityEngine.Object) this._particleSystem == (UnityEngine.Object) null)
                this._particleSystem = this.GetComponentInChildren<ParticleSystem>();
            if (active)
                this._particleSystem.Play();
            else
                this._particleSystem.Stop();
        }
        public void RefreshFuelSources()
        {
            batterySource = PartScript.BatteryFuelSource;
            var patchScript = PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            try
            {
                switch (this.Data.GasType)
                {
                    case "O2" :
                        highPressureGasSource = GetCraftFuelSource("LOX");
                        lowPressureGasSource=patchScript.OxygenFuelSource;
                        break;
                    case "CO2" :
                        highPressureGasSource = GetCraftFuelSource("CO2");
                        lowPressureGasSource =  patchScript.CO2FuelSource;
                        break;
                    case "N2" :
                        highPressureGasSource = GetCraftFuelSource("N2");
                        lowPressureGasSource =  GetCraftFuelSource("LPN2");
                        break;
                }
            }
            catch (Exception e)
            {
                Mod.Log("Error while refreshing fuel sources for GasDealer: " + e.StackTrace);
            }
           
            
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
                 {
                     RefreshFuelSources();
                     base.OnCraftStructureChanged(craftScript);
                 }
        
        #endregion
        private void UpdateComponents()
        {
            SetSubPart(IPartSubPartSetUp.FindSubPart(this, "Device/ParticleSystem"));
            _particleSystem = _particleSystemTransform.GetComponent<ParticleSystem>();
            this._particleSystemEmission = this._particleSystem.emission;
            this._particleSystemMain = this._particleSystem.main;
        }
        public void SetSubPart( Transform subPart )
        {
            this._particleSystemTransform = subPart;
        }
        
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            var engaging = new LabelButtonModel(Locale.GetString("Droodism.GasDealerScript.EmergencyDepressurization"), b =>
            {
                if (isFunctional)
                {
                    emergencyGasDepressurization = true;
                    string msg = Locale.GetString("Droodism.GasDealerScript.SequenceInitiated") + "<br>" +
                                 string.Format(Locale.GetString("Droodism.GasDealerScript.GasReleasing"), this.Data.GetSpinnerNames()) +
                                 "<br>" + Locale.GetString("Droodism.GasDealerScript.ActionIrreversible");
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(msg,false,10);
                }

                if (!isFunctional)
                {
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.GasDealerScript.Malfunctioning"),false,10);
                }

            });
            engaging.ButtonLabel = Locale.GetString("Droodism.GasDealerScript.Engage");
            engaging.Tooltip = Locale.GetString("Droodism.GasDealerScript.EngageTooltip");
            if (!isPressuring)
            {
                if (!isFunctional)
                {
                    engaging.Label = Locale.GetString("Droodism.GasDealerScript.Malfunction");
                    engaging.ButtonLabel = "";
                    engaging.Tooltip = Locale.GetString("Droodism.GasDealerScript.MalfunctionTooltip");
                }
                model.Add(engaging);
            }
        }
    }
}