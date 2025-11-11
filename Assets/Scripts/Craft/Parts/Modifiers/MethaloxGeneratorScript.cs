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

    public class MethaloxGeneratorScript : ResourceProcessorPartScript<MethaloxGeneratorData>
    {
        private IFuelSource HPco2Source,HPoxygenSource,waterSource,methaneloxSource;

        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            HPco2Source = GetCraftFuelSource("HPCO2");
            HPoxygenSource = GetCraftFuelSource("HPOxygen");
            waterSource = this.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>()
                .WaterFuelSource;
            methaneloxSource = GetCraftFuelSource("LOX/CH4");
        }
        protected override void WorkingLogic(in FlightFrameData frame)
        {
            if (BatterySource==null||HPco2Source==null||HPoxygenSource==null||waterSource==null||methaneloxSource==null)
            {
                return;
            }

            if (BatterySource.IsEmpty||HPco2Source.IsEmpty||HPoxygenSource.IsEmpty||waterSource.IsEmpty||methaneloxSource.TotalCapacity-methaneloxSource.TotalFuel<0.00001f)
            {
                return;
            }

            BatterySource.RemoveFuel(Data.BatteryComsumption*frame.DeltaTimeWorld);
            HPco2Source.RemoveFuel(Data.Hpco2Comsumption*frame.DeltaTimeWorld);
            HPoxygenSource.RemoveFuel(Data.HpoxygenComsumption*frame.DeltaTimeWorld);
            waterSource.RemoveFuel(Data.WaterComsumption*frame.DeltaTimeWorld);
            methaneloxSource.AddFuel(Data.MethaneloxGeneration*frame.DeltaTimeWorld);
        }
    }
}