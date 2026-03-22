using ModApi.GameLoop;
using Droodism.RadiationBelt;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class STCommandPodPatchScript : PartModifierScript<STCommandPodPatchData>, IFlightUpdate, IFlightStart
    {
        public ProceduralRadiationBelt belt;
        private int _debugFrameCounter;
        public IFuelSource OxygenFuelSource { get; set; }
        public IFuelSource CO2FuelSource { get; set; }
        public IFuelSource FoodFuelSource { get; set; }
        public IFuelSource SolidWasteFuelSource { get; set; }
        public IFuelSource WaterFuelSource { get; set; }
        public IFuelSource WastedWaterFuelSource { get; set; }

        public void FlightStart(in FlightFrameData frame)
        {

        }

        public void FlightUpdate(in FlightFrameData flightFrameData)
        {

            var manager = RadiationBeltManager.Instance;
            string planetName = this.PartScript.CraftScript.CraftNode.Parent.Name;

            Vector3 vesselPci = this.PartScript.CraftScript.FlightData.Position.ToVector3();
            Vector3 planetCenterPci = Vector3.zero;

            bool inner = manager.TryGetBeltSignedDistancePciMeters(
                planetName, vesselPci, planetCenterPci, true, out var dIn) && dIn < 0f;
            bool outer = manager.TryGetBeltSignedDistancePciMeters(
                planetName, vesselPci, planetCenterPci, false, out var dOut) && dOut < 0f;

            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($" inner {inner},outer {outer}");
        }
    }

}
