namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class ResourcePackScript : PartModifierScript<ResourcePackData>
    {
        internal void UpdateScale()
        {
            Transform transform = ((Component) this).transform.Find("pack");
            if (transform == null)
            {
                return;
            }

            foreach (AttachPointScript attachPointScript in this.PartScript.AttachPointScripts)
            {
                attachPointScript.AttachPoint.Scale = 0.8f * this.Data.ResScale;
            }
            transform.localScale= Vector3.one*80*this.Data.ResScale;
            UpdateFuel();
        }

        private FuelTankScript localOxygenSource,
            localCO2Source,
            localWaterSource,
            localWastedWaterSource,
            localFoodSource,
            localSolidWasteSource;
    
        private void UpdateFuel()
        {
            foreach (var partModifierScript in PartScript.Modifiers)
            {
                IFuelSource fuelTankScript = partModifierScript as IFuelSource;
                if (fuelTankScript.FuelType.Name=="Oxygen")
                {
                    localOxygenSource = fuelTankScript as FuelTankScript;
                }

                if (fuelTankScript.FuelType.Name == "CO2")
                {
                    localCO2Source = fuelTankScript as FuelTankScript;
                }

                if (fuelTankScript.FuelType.Name == "Water")
                {
                    localWaterSource = fuelTankScript as FuelTankScript;
                }

                if (fuelTankScript.FuelType.Name == "WastedWater")
                {
                    localWastedWaterSource = fuelTankScript as FuelTankScript;
                }

                if (fuelTankScript.FuelType.Name == "Food")
                {
                    localFoodSource = fuelTankScript as FuelTankScript;
                }

                if (fuelTankScript.FuelType.Name == "SolidWaste")
                {
                    localSolidWasteSource = fuelTankScript as FuelTankScript;
                }
            }

            localOxygenSource.Data.Capacity = localOxygenSource.Data.Fuel = 610*Data.ResScale;
            localFoodSource.Data.Capacity = localFoodSource.Data.Fuel = 0.5*Data.ResScale;
            localWaterSource.Data.Capacity = localWaterSource.Data.Fuel = 3*Data.ResScale;
            localCO2Source.Data.Capacity = 300*Data.ResScale;
            localSolidWasteSource.Data.Capacity =0.8*Data.ResScale;
            localWastedWaterSource.Data.Capacity = 1.25*Data.ResScale;

            localSolidWasteSource.Data.Fuel = localCO2Source.Data.Fuel = localWastedWaterSource.Data.Fuel = 0;


        }
    }
}