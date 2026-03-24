using System.Windows.Forms;
using ModApi.GameLoop;
using Droodism.RadiationBelt;
using ModApi.Flight.Sim;

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
        
        public IFuelSource OxygenFuelSource { get; set; }
        public IFuelSource CO2FuelSource { get; set; }
        public IFuelSource FoodFuelSource { get; set; }
        public IFuelSource SolidWasteFuelSource { get; set; }
        public IFuelSource WaterFuelSource { get; set; }
        public IFuelSource WastedWaterFuelSource { get; set; }
        
        private RadiationBeltConfig Config { get; set; }

        public void FlightStart(in FlightFrameData frame)
        {
            this.PartScript.CraftScript.CraftNode.ChangedSoI += OnChangedSOI;
            this.Config = RadiationBeltConfig.LoadFromFile(this.PartScript.CraftScript.CraftNode.Parent.Name);
        }

        private void OnChangedSOI(IOrbitNode node)
        {
            this.Config = RadiationBeltConfig.LoadFromFile(this.PartScript.CraftScript.CraftNode.Parent.Name);
        }
        

        public void FlightUpdate(in FlightFrameData flightFrameData)
        {
            string planetName = this.PartScript.CraftScript.CraftNode.Parent.Name;
            
            //我不知道你是啥想法,总之这个东西开启了会每帧都调用从文件load对应的config,损失性能,但是对于debug来说很有用
            if (ModSettings.Instance.ActiveUpdateRadiationBeltConfig)
            {
                this.Config = RadiationBeltManager.Instance.GetRuntimeConfigForPlanet(planetName);
            }
            

            Vector3 vesselPci = this.PartScript.CraftScript.FlightData.Position.ToVector3();

            bool inner = RadiationBeltManager.Instance.TryGetBeltSignedDistancePciMeters(this.Config,
                planetName, vesselPci, true, out var dIn) && dIn < 0f;
            bool outer = RadiationBeltManager.Instance.TryGetBeltSignedDistancePciMeters(this.Config,
                planetName, vesselPci, false, out var dOut) && dOut < 0f;

            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($" inner {inner},outer {outer}");
            
        }
       
        
    }

}
