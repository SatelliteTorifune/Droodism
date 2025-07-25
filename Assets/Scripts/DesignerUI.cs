using System;
using System.Collections.Generic;
using System.Linq;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Mods;

namespace Assets.Scripts
{
    public partial class Mod:GameMod
    {
        public static readonly string[] _massTypes = { "g", "kg", "t", "kt" };
        private Dictionary<string, (double Current, double Previous)> FuelMap = new Dictionary<string, (double, double)>
        {
            { "Oxygen", (0, 0) },
            { "H2O", (0, 0) },
            {"Food", (0, 0) },
            {"CO2", (0, 0) },
            {"Wasted Water", (0, 0) },
            {"Solid Waste", (0, 0) }
        };
        private static List<string> fuelTypes = new List<string> { "Oxygen", "H2O", "Food", "CO2", "Wasted Water", "Solid Waste" };

        private static string GetFuelAmountInDesigner(string fuelId,bool isWaste)
        {
            foreach (var fuelSource in Game.Instance.Designer.CraftScript.FuelSources.FuelSources)
            {
                if (fuelSource.FuelType.Id == fuelId)
                {
                    return isWaste?Inctance.FormatFuel(fuelSource.TotalCapacity*fuelSource.FuelType.Density,_massTypes):Inctance.FormatFuel(fuelSource.TotalFuel*fuelSource.FuelType.Density,_massTypes);
                }
            }
            return "NaN";

        }
        private string GetDroodCountInDesigner()
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