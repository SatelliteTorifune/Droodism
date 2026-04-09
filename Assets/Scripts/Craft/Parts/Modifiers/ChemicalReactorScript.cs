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

   
    public class ChemicalReactorScript : ResourceProcessorPartScript<ChemicalReactorData>
    {
        private IFuelSource HPOxygenSource,co2Source,HPco2Source,lH2Source,hydroloxSource,monoSource,methaneloxSource,HPNitrogenSource,waterSource;
        private Transform _particleSystemTransform;
        private ParticleSystem _particleSystem;
        
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
                case "H2O+CO2+LOX=Methanelox":
                    MethaloxWorkingLogic(frame);
                    break;
            }
        }

        private void HydroloxWorkingLogic(in FlightFrameData frame)
        {
            if (lH2Source == null||HPOxygenSource == null||hydroloxSource == null)
            {
                _particleSystem.Stop();
                return;  
            }
               
            if (!lH2Source.IsEmpty&&!HPOxygenSource.IsEmpty&&(hydroloxSource.TotalCapacity - hydroloxSource.TotalFuel > 1E-06)&&
                BatterySource is
                {
                    IsEmpty: false
                })
            { 
                lH2Source.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*12f);
                HPOxygenSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*22f);
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
            if (methaneloxSource == null||waterSource==null||HPco2Source==null||HPOxygenSource==null)
            {
                _particleSystem.Stop();
                return;
            }

            if (!waterSource.IsEmpty&&!HPco2Source.IsEmpty&&!HPOxygenSource.IsEmpty&&(methaneloxSource.TotalCapacity - methaneloxSource.TotalFuel > 1E-06)&&
                BatterySource is
                {
                    IsEmpty: false
                })
            {
                PlayEffects();
                waterSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*0.45f);
                HPco2Source.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*3.6f);
                HPOxygenSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*2.1);
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
                case "H2O+CO2+LOX=Methanelox":
                    HPco2Source = GetRegularCraftFuelSource("HPCO2");
                    waterSource=this.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>().WaterFuelSource;
                    HPOxygenSource=GetRegularCraftFuelSource("HPOxygen");
                    methaneloxSource = GetRegularCraftFuelSource("LOX/CH4");
                    break;
                case "N2+LH2=N2H4":
                    hydroloxSource = GetRegularCraftFuelSource("LH2");
                    monoSource = PartScript.CommandPod.MonoFuelSource;
                    HPNitrogenSource = GetRegularCraftFuelSource("HPN2");
                    break;
                case "LH2+LOX=Hydrolox":
                    lH2Source = GetRegularCraftFuelSource("LH2");
                    HPOxygenSource=GetRegularCraftFuelSource("HPOxygen");
                    hydroloxSource=GetRegularCraftFuelSource("LOX/LH2");
                    break;
                    
            }
            var patch = PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            monoSource = this.PartScript.CommandPod.MonoFuelSource;
        }
        protected override void UpdateComponents()
        {
            string[]	strArray	= "Device/ParticleSystem".Split( '/', StringSplitOptions.None );
            Transform	subPart		= this.transform;
            foreach ( string n in strArray )
                subPart = subPart.Find( n ) ?? subPart;
            if ( subPart.name == strArray[strArray.Length - 1] )
                this.SetSubPart(subPart);
            else
                this.SetSubPart( ModApi.Utilities.FindFirstGameObjectMyselfOrChildren( "Device/ParticleSystem/", this.gameObject ) ?.transform );
            _particleSystem = _particleSystemTransform.GetComponent<ParticleSystem>();
            
        }
        private void SetSubPart( Transform subPart )
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
                case "H2O+CO2+LOX=Methanelox":
                    model.Add<TextModel>(new TextModel("<color=green>Methanelox Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.85f * methaneloxSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of Methanelox being generated per second."));
                    model.Add<TextModel>(new TextModel("<color=yellow>Water Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.45f * waterSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of water being reacted per second."));
                    
                    model.Add<TextModel>((new TextModel("<color=yellow>CO2 Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*3.6f * HPco2Source.FuelType.Density:0);
                    }), tooltip: "The kilograms of CO2 being reacted per second.")));
                    model.Add(new TextModel("<color=yellow>Oxygen Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*2.1f * HPOxygenSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of High Pressure Oxygen being reacted per second."));
                    break;
                case ("N2+LH2=N2H4"):
                    model.Add(new TextModel("<color=green>Monopropellant Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.07f * monoSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of Monopropellant being generated per second."));
                    model.Add(new TextModel("<color=yellow>Hydrolox Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.2f * hydroloxSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of Hydrolox being reacted per second."));
                    model.Add(new TextModel("<color=yellow>Nitrogen Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*2.1f * HPNitrogenSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of High Pressure Nitrogen being reacted per second."));
                    break;
                case ("LH2+LOX=Hydrolox"):
                    model.Add(new TextModel("<color=green>Hydrolox Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*0.972f * hydroloxSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of Hydrolox being generated per second."));
                    model.Add(new TextModel("<color=yellow>LH2 Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*12f * lH2Source.FuelType.Density:0);
                    }), tooltip: "The kilograms of LH2 being reacted per second."));
                    model.Add(new TextModel("<color=yellow>Oxygen Mass Flow", (Func<string>)(() =>
                    {
                        return Units.GetMassFlowRateString(this.PartScript.Data.Activated?Data.GenerationRate*22f * HPOxygenSource.FuelType.Density:0);
                    }), tooltip: "The kilograms of High Pressure Oxygen being reacted per second."));
                    break;
            }
        }
    }
}