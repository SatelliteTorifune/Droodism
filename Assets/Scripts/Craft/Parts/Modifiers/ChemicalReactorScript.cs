using ModApi;
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

   
    public class ChemicalReactorScript : ResourceProcessorPartScript<ChemicalReactorData>, IPartSubPartSetUp
    {
        private IFuelSource LqdOxygenSource,HPco2Source,lH2Source,hydroloxSource,monoSource,methaneloxSource,HPNitrogenSource,waterSource;
        private Transform _particleSystemTransform;
        private ParticleSystem _particleSystem;

        public Transform SubPart => _particleSystemTransform;
        
        public override void FlightUpdate(in FlightFrameData frame)
        {
            if (!PartScript.Data.Activated)
            {
                _particleSystem.Stop();
                return; 
            }
            WorkingLogic(frame);
        }
        public string WorkingType{get;private set;}
        protected override void WorkingLogic(in FlightFrameData frame)
        {
            base.WorkingLogic(in frame);
           
            switch(WorkingType)
            {
                case "LH2+LOX=Hydrolox":
                    HydroloxWorkingLogic(frame);
                    break;
                case "N2+LH2=N2H4":
                    MonopropellantWorkingLogic(frame);
                    break;
                case "H2O+CO2=Methanelox":
                    MethaloxWorkingLogic(frame);
                    break;
            }
        }

        private void HydroloxWorkingLogic(in FlightFrameData frame)
        {
            if (lH2Source == null||LqdOxygenSource == null||hydroloxSource == null)
            {
                _particleSystem.Stop();
                return;  
            }
               
            if (!lH2Source.IsEmpty&&!LqdOxygenSource.IsEmpty&&(hydroloxSource.TotalCapacity - hydroloxSource.TotalFuel > 1E-06)&&
                BatterySource is
                {
                    IsEmpty: false
                })
            { 
                lH2Source.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*12f);
                LqdOxygenSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*3f);
                hydroloxSource.AddFuel(0.972*Data.GenerationRate * frame.DeltaTimeWorld*18f);
                BatterySource.RemoveFuel(Data.GenerationRate * frame.DeltaTimeWorld*Data.BatteryConsumption*4000);
                PlayEffects();
            }
            else
            {
                _particleSystem.Stop();
            }
        }
        private void MonopropellantWorkingLogic(in FlightFrameData frame)
        {
            if (monoSource == null||HPNitrogenSource == null||hydroloxSource == null)
            {
                _particleSystem.Stop();
                return;  
            }
            if (!hydroloxSource.IsEmpty&&!HPNitrogenSource.IsEmpty&&(monoSource.TotalCapacity - monoSource.TotalFuel > 1E-06)&&
                BatterySource is
                {
                    IsEmpty: false
                })
            { 
                PlayEffects();
                HPNitrogenSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*2.1);
                hydroloxSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*0.2f);
                monoSource.AddFuel(Data.GenerationRate * frame.DeltaTimeWorld*0.07f);
                BatterySource.RemoveFuel(Data.GenerationRate * frame.DeltaTimeWorld*Data.BatteryConsumption*4000*7.2f);
            }
            else
            {
                _particleSystem.Stop();
            }
        }
        private void MethaloxWorkingLogic(in FlightFrameData frame)
        {
            if (methaneloxSource == null||waterSource==null||HPco2Source==null)
            {
                _particleSystem.Stop();
                return;
            }

            if (!waterSource.IsEmpty&&!HPco2Source.IsEmpty&&(methaneloxSource.TotalCapacity - methaneloxSource.TotalFuel > 1E-06)&&
                BatterySource is
                {
                    IsEmpty: false
                })
            {
                PlayEffects();
                waterSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*0.45f);
                HPco2Source.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*3.6f);
                methaneloxSource.AddFuel(Data.GenerationRate * frame.DeltaTimeWorld*0.85f);
                BatterySource.RemoveFuel(Data.GenerationRate * frame.DeltaTimeWorld*Data.BatteryConsumption*28200);
            }
            else
            {
                _particleSystem.Stop();
            }
        }
       

        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            this.WorkingType = Data.ReactorType;
            switch (WorkingType)
            {
                case "H2O+CO2=Methanelox":
                    HPco2Source = GetRegularCraftFuelSource("CO2");
                    waterSource=this.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>().WaterFuelSource;
                    methaneloxSource = GetRegularCraftFuelSource("LOX/CH4");
                    break;
                case "N2+LH2=N2H4":
                    hydroloxSource = GetRegularCraftFuelSource("LH2");
                    monoSource = PartScript.CommandPod.MonoFuelSource;
                    HPNitrogenSource = GetRegularCraftFuelSource("N2");
                    break;
                case "LH2+LOX=Hydrolox":
                    lH2Source = GetRegularCraftFuelSource("LH2");
                    LqdOxygenSource = GetRegularCraftFuelSource("LOX");
                    hydroloxSource=GetRegularCraftFuelSource("LOX/LH2");
                    break;
                    
            }
            monoSource = this.PartScript.CommandPod.MonoFuelSource;
        }
        protected override void UpdateComponents()
        {
            SetSubPart(IPartSubPartSetUp.FindSubPart(this, "Device/ParticleSystem"));
            _particleSystem = _particleSystemTransform.GetComponent<ParticleSystem>();
            
        }

        public void SetSubPart( Transform subPart )
        {
            this._particleSystemTransform = subPart;
        }
        private void PlayEffects()
        {
            if (!_particleSystem.isPlaying)
            {
                _particleSystem.Play(); 
            }
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            switch (this.WorkingType)
            {
                case "H2O+CO2=Methanelox":
                    model.Add<TextModel>(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.MethaneloxMassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.85f * methaneloxSource.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.MethaneloxMassFlowTooltip")));
                    model.Add<TextModel>(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.WaterMassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.45f * waterSource.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.WaterMassFlowTooltip")));

                    model.Add<TextModel>((new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.CO2MassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*3.6f * HPco2Source.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.CO2MassFlowTooltip"))));
                    break;
                case ("N2+LH2=N2H4"):
                    model.Add(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.MonopropellantMassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.07f * monoSource.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.MonopropellantMassFlowTooltip")));
                    model.Add(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.HydroloxMassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.2f * hydroloxSource.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.HydroloxMassFlowTooltip")));
                    model.Add(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.NitrogenMassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*2.1f * HPNitrogenSource.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.NitrogenMassFlowTooltip")));
                    break;
                case ("LH2+LOX=Hydrolox"):
                    model.Add(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.HydroloxMassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.972f * hydroloxSource.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.HydroloxMassFlowTooltip")));
                    model.Add(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.LH2MassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*12f * lH2Source.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.LH2MassFlowTooltip")));
                    model.Add(new TextModel(Locale.GetString("Droodism.ChemicalReactorScript.OxygenMassFlow"), (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*3f * LqdOxygenSource.FuelType.Density:0);
                    }), tooltip: Locale.GetString("Droodism.ChemicalReactorScript.OxygenMassFlowTooltip")));
                    break;
            }
        }
    }
}