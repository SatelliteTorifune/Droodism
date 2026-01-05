using ModApi.Craft;
using ModApi.Craft.Parts.Modifiers;
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

    public class Water_DesalinationScript : ResourceProcessorPartScript<Water_DesalinationData>
    {
        private IFuelSource waterFuelSource;
        public bool IsGenerator{get;private set;}
        public bool IsRightType{get;private set;}
        public override void FlightStart(in FlightFrameData frame)
        {
            base.FlightStart(frame);
            if (this.PartScript.Data.PartType.Name.Contains("Generator"))
            {
                IsGenerator = true;
                if (this.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id=="LOX/LH2"||this.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id=="LOX/CH4")
                {
                    IsRightType = true;
                }
            }
        }
        
        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            waterFuelSource = PartScript?.CommandPod?.Part.PartScript.GetModifier<STCommandPodPatchScript>()
                .WaterFuelSource;
        }
        

        protected override void WorkingLogic(in FlightFrameData frame)
        {
            if (!this.PartScript.WaterPhysics.IsInWater)
            {
                return;
            }

            if (IsGenerator&&!IsRightType)
            {
                return;
            }
            BatterySource.RemoveFuel(Data.PowerConsumption*frame.DeltaTimeWorld);
            waterFuelSource.AddFuel(Data.WaterGenerationScale*frame.DeltaTimeWorld);
        }
    }
}