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
    
    public class STCommandPodPatchScript : PartModifierScript<STCommandPodPatchData>,IFlightUpdate,IFlightStart
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
            
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"内{inner},外{outer}");

            _debugFrameCounter++;
            if (_debugFrameCounter % 120 == 0)
            {
                double radiusMeters = manager.GetPlanetRadiusMeters(planetName);
                double rMeters = vesselPci.magnitude;
                double rNorm = radiusMeters > 1e-6 ? rMeters / radiusMeters : 0.0;

                float renderLocalRadius = -1f;
                Vector3 parentLossy = Vector3.one;
                Vector3 beltLossy = Vector3.one;
                var beltRenderer = manager.BeltInstance;
                if (beltRenderer != null && beltRenderer.Parent != null)
                {
                    Vector3 vesselWorld = this.PartScript.Transform.position;
                    Vector3 local = beltRenderer.transform.InverseTransformPoint(vesselWorld);
                    renderLocalRadius = local.magnitude;
                    parentLossy = beltRenderer.Parent.transform.lossyScale;
                    beltLossy = beltRenderer.transform.lossyScale;
                }

                Mod.Log(
                    $"[RadProbe] body={planetName} r={rMeters:F0}m R={radiusMeters:F0}m r/R={rNorm:F3} " +
                    $"renderLocalR={renderLocalRadius:F3} dIn={dIn:F4} dOut={dOut:F4} inInner={inner} inOuter={outer} " +
                    $"parentScale={parentLossy} beltScale={beltLossy}");
            }
        }

        
    }
}