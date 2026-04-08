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

   
    public class ChemicalReactorScript : ResourceProcessorPartScript<ChemicalReactorData>
    {
        private IFuelSource HPOxygenSource,co2Source,HPco2Source,lH2Source,hydroloxSource,monoSource,methaneloxSource,HPNitrogenSource,waterSource;
        
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
            }
        }
        private void MonopropellantWorkingLogic(in FlightFrameData frame)
        {
            if (monoSource == null||HPNitrogenSource == null||hydroloxSource == null)
            {
                return;  
            }
            if (!hydroloxSource.IsEmpty&&!HPNitrogenSource.IsEmpty&&(monoSource.TotalCapacity - monoSource.TotalFuel > 1E-06)&&
                BatterySource is
                {
                    IsEmpty: false
                })
            { 
                //TODO 等待数据确认
                HPNitrogenSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*Data.data1);
                hydroloxSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*Data.data2);
                monoSource.AddFuel(Data.GenerationRate * frame.DeltaTimeWorld*Data.data3);
                BatterySource.RemoveFuel(Data.GenerationRate * frame.DeltaTimeWorld*Data.BatteryConsumption*4000*Data.data4);
            }
        }
        private void MethaloxWorkingLogic(in FlightFrameData frame)
        {
            if (methaneloxSource == null||waterSource==null||HPco2Source==null||HPOxygenSource==null)
            {
                return;
            }

            if (!waterSource.IsEmpty&&!HPco2Source.IsEmpty&&!HPOxygenSource.IsEmpty&&(methaneloxSource.TotalCapacity - methaneloxSource.TotalFuel > 1E-06)&&
                BatterySource is
                {
                    IsEmpty: false
                })
            { 
                //TODO 等待添加数据
                waterSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*5f);
                HPco2Source.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*10f);
                HPOxygenSource.RemoveFuel(Data.GenerationRate* frame.DeltaTimeWorld*20f);
                methaneloxSource.AddFuel(0.95*Data.GenerationRate * frame.DeltaTimeWorld*15f);
                BatterySource.RemoveFuel(Data.GenerationRate * frame.DeltaTimeWorld*Data.BatteryConsumption*4000);
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

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
        }
    }
}