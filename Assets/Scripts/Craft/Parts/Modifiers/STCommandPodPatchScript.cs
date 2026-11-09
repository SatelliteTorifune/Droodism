namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class STCommandPodPatchScript : PartModifierScript<STCommandPodPatchData>
    {
        
        public IFuelSource OxygenFuelSource { get; set; }
        public IFuelSource CO2FuelSource { get; set; }
        public IFuelSource FoodFuelSource { get; set; }
        public IFuelSource SolidWasteFuelSource { get; set; }
        public IFuelSource WaterFuelSource { get; set; }
        public IFuelSource WastedWaterFuelSource { get; set; }
        
    }

}
