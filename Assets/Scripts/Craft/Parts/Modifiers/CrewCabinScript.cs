using Droodism.RadiationBelt;
using ModApi;
using ModApi.Craft;
using ModApi.Design;
using ModApi.Flight.Sim;
using ModApi.GameLoop;
using ModApi.Math;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;
    public class CrewCabinScript :PartModifierScript<CrewCabinData>,IFlightStart,IFlightUpdate
    {
        private RadiationBeltConfig RadiationBeltConfig;
        private string currentPlanetName;
        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            this.Data.InspectorEnabled = true;
        }

        public void FlightStart(in FlightFrameData frame)
        {
            this.PartScript.CraftScript.CraftNode.ChangedSoI += OnChangedSOI;
            currentPlanetName = PartScript.CraftScript.CraftNode.Parent.Name;
            RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(currentPlanetName);
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            CheckRadiationState(frame);
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            model.Add<TextModel>(new TextModel("<color=yellow>Radiation Shield Duration ", (Func<string>) (() =>Units.GetPercentageString((float)(this.Data.RadiationShieldDuration/this.Data.RadiationShieldDurationUpperLimit)))));
        }

        private void OnChangedSOI(IOrbitNode orbit)
        {
            currentPlanetName = PartScript.CraftScript.CraftNode.Parent.Name;
            RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(currentPlanetName);
        }
        private void CheckRadiationState(in FlightFrameData data)
        {
            Vector3 craftPCIPos = this.PartScript.CraftScript.FlightData.Position.ToVector3();
            
            RadiationBeltManager.Instance.TryGetDoseRateRadPerHour(this.RadiationBeltConfig,
                currentPlanetName,
                craftPCIPos,
                out var totalRadiationDoseRateRadPerHour,
                out var innerDoseRateRadPerHour,
                out var outerDoseRateRadPerHour);
            
            float deltaHours = Mathf.Max(0f, (float)data.DeltaTimeWorld) / 3600f;
            if (deltaHours <= 1e-9f)
            {
                deltaHours = Mod.GetDeltaTimeHours();
            }
            
            this.Data.RadiationShieldDuration -= totalRadiationDoseRateRadPerHour * deltaHours;
           
        }
    }
}