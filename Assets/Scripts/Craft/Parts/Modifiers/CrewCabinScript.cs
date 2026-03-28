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
    public class CrewCabinScript :ResourceProcessorPartScript<CrewCabinData>,IFlightStart,IFlightUpdate
    {
        private RadiationBeltConfig RadiationBeltConfig;
        private string currentPlanetName;

        private IFuelSource WaterSource,LiquidHydrogenSouce;

        public override void FlightStart(in FlightFrameData frame)
        {
            this.PartScript.CraftScript.CraftNode.ChangedSoI += OnChangedSOI;
            currentPlanetName = PartScript.CraftScript.CraftNode.Parent.Name;
            RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(currentPlanetName);
           
        }

        public override void FlightUpdate(in FlightFrameData frame)
        {
            CheckRadiationState(frame);
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            model.Add<TextModel>(new TextModel("<color=yellow>Radiation Shield Type ", (Func<string>) (() =>this.Data.RadiationShieldType)));
            model.Add<TextModel>(new TextModel("<color=yellow>Radiation Shield Duration ", (Func<string>) (() =>Units.GetPercentageString((float)(this.Data.RadiationShieldDuration/this.Data.RadiationShieldDurationUpperLimit)))));
        }

        private void OnChangedSOI(IOrbitNode orbit)
        {
            currentPlanetName = PartScript.CraftScript.CraftNode.Parent.Name;
            RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(currentPlanetName);
        }

        protected override void UpdateFuelSources()
        {
            var patchScript = this.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            base.UpdateFuelSources();
            WaterSource = patchScript.WaterFuelSource;
            LiquidHydrogenSouce = this.GetRegularCraftFuelSource("LH2");
        }

        private void CheckRadiationState(in FlightFrameData data)
        {
            if (Data.RadiationShieldDuration<=0)
            {
                return;
            }
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
            // Compute protection factors by belt
            float innerProtection = Mathf.Clamp01(GetInnerRadiationProtection());
            float outerProtection = Mathf.Clamp01(GetOuterRadiationProtection());

            // Effective dose that consumes shield is reduced by protection
            float effectiveDoseRate =
                Mathf.Max(0f, innerDoseRateRadPerHour) * (1f - innerProtection) +
                Mathf.Max(0f, outerDoseRateRadPerHour) * (1f - outerProtection);

            // Fallback: if breakdown per belt is unavailable, use total with averaged protection
            if (effectiveDoseRate <= 0f && totalRadiationDoseRateRadPerHour > 0f)
            {
                float avgProtection = (innerProtection + outerProtection) * 0.5f;
                effectiveDoseRate = totalRadiationDoseRateRadPerHour * (1f - avgProtection);
            }

            this.Data.RadiationShieldDuration -= effectiveDoseRate * deltaHours; 
            
            
           
        }
        public float GetInnerRadiationProtection()
        {
            var t = Data.RadiationShieldType ?? "None";
            switch (t)
            {
                case "None":
                    return 0f; // no protection
                case "":
                    return 0f; // unknown, assume moderate
                case "Aluminium":
                    return 0.2f; // inner belt (protons) protection is modest
                case "PolyEthylene":
                    return 0.8f; // very good against protons
                case "Borated PolyEthylene":
                    return 0.85f; // excellent for protons + neutrons
                case "Water":
                    return 0.75f; // good and practical
                case "Liquid Hydrogen":
                    return 0.95f; // best theoretical for protons
                case "Boron Nitride Nanotubes":
                    return 0.8f; // strong composite option
                default:
                    return 0f;
            }
        }

        public float GetOuterRadiationProtection()
        {
            var t = Data.RadiationShieldType ?? "None";
            switch (t)
            {
                case "None":
                    return 0f;
                case "":
                    return 0f;
                case "Aluminium":
                    return 0.6f; // good for electrons
                case "PolyEthylene":
                    return 0.6f; // good overall
                case "Borated PolyEthylene":
                    return 0.65f; // good with added neutron control
                case "Water":
                    return 0.6f;
                case "Liquid Hydrogen":
                    return 0.8f; // excellent overall
                case "Boron Nitride Nanotubes":
                    return 0.7f; // composite performance
                default:
                    return 0f;
            }
        }
    }
}