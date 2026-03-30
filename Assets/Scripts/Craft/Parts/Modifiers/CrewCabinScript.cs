using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
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
    public class CrewCabinScript :ResourceProcessorPartScript<CrewCabinData>
    {
        private bool _recalcShieldMassGuard;
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

        public override void DesignerStart(in DesignerFrameData frame)
        {
            base.DesignerStart(in frame);
            TryRefreshCachedShieldMassAndRecalcIfNeeded();
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            TryRefreshCachedShieldMassAndRecalcIfNeeded();
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            model.Add<TextModel>(new TextModel("<color=yellow>Radiation Shield Type ", (Func<string>) (() =>this.Data.RadiationShieldType)));
            model.Add<TextModel>(new TextModel("<color=yellow>Radiation Shield Duration ", (Func<string>) (() =>Data.RadiationShieldDurationUpperLimit==0?"NaN":Units.GetPercentageString((float)(this.Data.RadiationShieldDuration/this.Data.RadiationShieldDurationUpperLimit)))));
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

        private void WaterAndHydrogen()
        {
            if (this.Data.RadiationShieldType=="Water"&&this.WaterSource!=null)
            {
                
            }
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

            this.Data.RadiationShieldDuration -= effectiveDoseRate * deltaHours*0.1f; 
            
            
           
        }

        private void TryRefreshCachedShieldMassAndRecalcIfNeeded()
        {
            if (_recalcShieldMassGuard) return;
            if (this.Data == null || this.PartScript?.CraftScript == null) return;

            var newMass = this.Data.ComputeShieldMassDry();
            var oldMass = this.Data.CachedShieldMassDry;
            if (Mathf.Abs(newMass - oldMass) < 1e-5f) return;

            this.Data.CachedShieldMassDry = newMass;
            _recalcShieldMassGuard = true;
            this.PartScript.CraftScript.SetStructureChanged();
            _recalcShieldMassGuard = false;
        }

        public float GetInnerRadiationProtection()
        {
            var t = Data.RadiationShieldType ?? "None";
            switch (t)
            {
                case "None":
                    return 0f; // no protection
                case "":
                    return 0f; 
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

        public override void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            groupModel.Add<TextModel>(new TextModel("<color=yellow>Radiation Shield Type</color>",(Func<string>) (()=> this.Data.RadiationShieldType),tooltip: "Current Crew Compartment's Ant-Radiation Material Type"));
            groupModel.Add<ProgressBarModel>(new ProgressBarModel(()=>
                "Radiation Duration", 
                () => (float)(this.Data.RadiationShieldDuration / this.Data.RadiationShieldDurationUpperLimit)));
        }

        public void RefillWater()
        {
            var toFill=this.Data.RadiationShieldDurationUpperLimit-Data.RadiationShieldDurationUpperLimit;
            
        }
        
        public bool UsesMachNumber { get; }
        
    }
}