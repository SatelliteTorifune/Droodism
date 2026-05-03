using ModApi.Craft;
using ModApi.Craft.Parts.Modifiers;
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

    public class Water_DesalinationScript : ResourceProcessorPartScript<Water_DesalinationData>
    {
        private IFuelSource waterFuelSource;
        private bool isGenerator;
        private bool isRightType;
        public override void FlightStart(in FlightFrameData frame)
        {
            base.FlightStart(frame);
            if (this.PartScript.Data.PartType.Name.Contains("Generator"))
            {
                isGenerator = true;
                if (this.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id=="LOX/LH2"||this.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id=="LOX/CH4")
                {
                    isRightType = true;
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

            if (isGenerator&&!isRightType)
            {
                return;
            }
            BatterySource.RemoveFuel(Data.PowerConsumption*frame.DeltaTimeWorld);
            waterFuelSource.AddFuel(Data.WaterGenerationScale*frame.DeltaTimeWorld);
        }
    }
}