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

    public class Water_DesalinationScript : PartModifierScript<Water_DesalinationData>,IFlightStart,IFlightUpdate
    {
        private IFuelSource waterFuelSource,batterySource;
        public bool IsGenerator{get;private set;}
        public void FlightStart(in FlightFrameData frame)
        {
            RefreshFuelSource();
            if (this.PartScript.Data.PartType.Name.Contains("Generator"))
            {
                
                if (this.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id=="LOX/LH2"||this.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id=="LOX/CH4")
                {
                    IsGenerator = false;
                }
                IsGenerator = true;
                
            }
            
        }

        
        public void FlightUpdate(in FlightFrameData frame)
        {
            if (IsGenerator)
            {
                return;
            }
            if (batterySource == null || waterFuelSource == null)
            {
                return;
            }

            if (batterySource.IsEmpty||waterFuelSource.TotalCapacity-waterFuelSource.TotalFuel<=1e-4)
            {
                return;
            }
            
            if (PartScript.Data.Activated)
            {
                
                WokringLogic(frame);
            }
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            RefreshFuelSource();
            base.OnCraftStructureChanged(craftScript);
        }
        public void RefreshFuelSource()
        {
            batterySource = PartScript.BatteryFuelSource;
            waterFuelSource = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>()
                .WaterFuelSource;


        }
        

        private void WokringLogic(in FlightFrameData frame)
        {
            if (this.PartScript.WaterPhysics.IsInWater)
            {
                batterySource.RemoveFuel(Data.PowerConsumption*frame.DeltaTimeWorld);
                waterFuelSource.AddFuel(Data.WaterGenerationScale*frame.DeltaTimeWorld);
            }
        }
    }
}