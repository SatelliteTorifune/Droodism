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
            //别管,先解决前面map的问题
            return;
            
            this.belt = RadiationBeltManager.Instance.BeltInstance;
            if (belt == null)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("空引用了");
                return;
            }
            Transform planetTransform = belt.Parent.transform;
            //bool inInner = belt.config.IsInInnerBelt(PartScript.Transform.position, planetTransform);
            //bool inOuter = belt.config.IsInOuterBelt(PartScript.Transform.position, planetTransform);
            //Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"内{inInner},外{inOuter}");
        }

        
    }
}