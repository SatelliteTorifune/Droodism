using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Mods;

namespace Assets.Scripts
{
    public partial class Mod:GameMod
    {
        public static readonly string[] _massTypes = { "g", "kg", "t", "kt" };
        public static List<string> fuelTypes = new List<string> { "Oxygen", "H2O", "Food", "LPCO2", "Wasted Water", "Solid Waste"};

        public static string GetFuelAmountInDesigner(string fuelId)
        {
            var patch = Game.Instance.Designer.CraftScript.PrimaryCommandPod.Part.PartScript
                .GetModifier<STCommandPodPatchScript>();
            if (patch==null)
            {
                return "NaN";
            }

            try
            {
                switch (fuelId)
                {
                    case "Oxygen":
                        return Instance.FormatFuel(patch.OxygenFuelSource.TotalFuel * patch.OxygenFuelSource.FuelType.Density, _massTypes);
                    case "H2O":
                        return Instance.FormatFuel(patch.WaterFuelSource.TotalFuel * patch.WaterFuelSource.FuelType.Density, _massTypes);
                    case "Food":
                        return Instance.FormatFuel(patch.FoodFuelSource.TotalFuel * patch.FoodFuelSource.FuelType.Density, _massTypes);
                    case "LPCO2":
                        return Instance.FormatFuel(patch.CO2FuelSource.TotalCapacity * patch.CO2FuelSource.FuelType.Density, _massTypes);
                    case "Wasted Water":
                        return Instance.FormatFuel(patch.WastedWaterFuelSource.TotalCapacity * patch.WastedWaterFuelSource.FuelType.Density, _massTypes);
                    case "Solid Waste":
                        return Instance.FormatFuel(patch.SolidWasteFuelSource.TotalCapacity * patch.SolidWasteFuelSource.FuelType.Density, _massTypes);
                    default:
                        return "NaN";
                }
            }
            catch (Exception e)
            {
                return "NaN";
            }
           
           
        }
        public string GetDroodCountInDesigner()
        {
            int DroodCountInDesigner = 0;
            ICraftScript craftScript = Game.Instance.Designer.CraftScript;
            var list = craftScript.Data.Assembly.Parts.Where<PartData>(
                (Func<PartData, bool>)(x => !x.PartScript.Disconnected));
            foreach (var pd in list)
            {
                if (pd.PartType.Name=="Eva"||pd.PartType.Name=="Eva-Tourist")
                {
                    DroodCountInDesigner++;
                } 
            }
            return DroodCountInDesigner.ToString();
           
        }
    }
}