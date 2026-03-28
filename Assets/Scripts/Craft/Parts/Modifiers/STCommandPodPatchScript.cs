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
        private float cumulativeDoseRad;

        public void FlightStart(in FlightFrameData frame)
        {
            this.PartScript.CraftScript.CraftNode.ChangedSoI += OnChangedSOI;
            this.Config = RadiationBeltConfig.LoadFromFile(this.PartScript.CraftScript.CraftNode.Parent.Name);
            this.cumulativeDoseRad = 0f;
        }

        private void OnChangedSOI(IOrbitNode node)
        {
            this.Config = RadiationBeltConfig.LoadFromFile(this.PartScript.CraftScript.CraftNode.Parent.Name);
        }
        

        public void FlightUpdate(in FlightFrameData flightFrameData)
        {
           return;
            string planetName = this.PartScript.CraftScript.CraftNode.Parent.Name;
            
            //我不知道你是啥想法,总之这个东西开启了会每帧都调用从文件load对应的config,损失性能,但是对于debug来说很有用
            if (ModSettings.Instance.ActiveUpdateRadiationBeltConfig)
            {
                this.Config = RadiationBeltManager.Instance.GetRuntimeConfigForPlanet(planetName);
            }
            

            Vector3 vesselPci = this.PartScript.CraftScript.FlightData.Position.ToVector3();
            
            /*
            RadiationBeltManager.Instance.TryGetTotalBeltIntensityPciMeters(this.Config,
                planetName,
                vesselPci,
                out var totalIntensity,
                out var innerIntensity,
                out var outerIntensity);*/
            RadiationBeltManager.Instance.TryGetDoseRateRadPerHour(this.Config,
                planetName,
                vesselPci,
                out var totalDoseRateRadPerHour,
                out var innerDoseRateRadPerHour,
                out var outerDoseRateRadPerHour);

            float deltaHours = Mathf.Max(0f, (float)flightFrameData.DeltaTimeWorld) / 3600f;
            if (deltaHours <= 1e-9f)
            {
                deltaHours = Mod.GetDeltaTimeHours();
            }
            this.cumulativeDoseRad += totalDoseRateRadPerHour * deltaHours;
            string acuteBand = GetAcuteBand(this.cumulativeDoseRad, totalDoseRateRadPerHour);

            Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                $"inner({1:F3}/{innerDoseRateRadPerHour:F2} rad/h), " +
                $"outer({1:F3}/{outerDoseRateRadPerHour:F2} rad/h), " +
                $"rate {totalDoseRateRadPerHour:F2} rad/h, dose {this.cumulativeDoseRad:F4} rad, +{(totalDoseRateRadPerHour * deltaHours):F6} rad/frame, {acuteBand}");
            
        }

        

        private static string GetAcuteBand(float cumulativeDoseRad, float doseRateRadPerHour)
        {
            if (doseRateRadPerHour > 10f || cumulativeDoseRad >= 500f) return "ARS critical";
            if (cumulativeDoseRad >= 200f) return "ARS severe";
            if (cumulativeDoseRad >= 100f) return "ARS mild";
            return "nominal";
        }
       
        
    }

}
