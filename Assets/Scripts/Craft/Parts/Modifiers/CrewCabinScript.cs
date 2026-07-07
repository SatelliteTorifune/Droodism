using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Droodism.RadiationBelt;
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
        private bool _recalcShieldMassFlag;
        private RadiationBeltConfig RadiationBeltConfig;
        private string currentPlanetName;

        private IFuelSource WaterSource,LiquidHydrogenSouce;

        private List<PartData> RTGParts = new List<PartData>();
        private List<PartData> NTRParts = new List<PartData>();

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
            model.Add<TextModel>(new TextModel("<color=yellow>" + Locale.GetString("Droodism.CrewCabinScript.RadiationShieldType"), (Func<string>) (() =>this.Data.RadiationShieldType)));
            model.Add<TextModel>(new TextModel("<color=yellow>" + Locale.GetString("Droodism.CrewCabinScript.RadiationShieldDuration"), (Func<string>) (() =>Data.RadiationShieldDurationUpperLimit==0?Locale.GetString("Droodism.DroodismUIManager.NotAvailable"):Units.GetPercentageString((float)(this.Data.RadiationShieldDuration/this.Data.RadiationShieldDurationUpperLimit)))));
            if (this.Data.RadiationShieldType == "Water" || Data.RadiationShieldType == "Liquid Hydrogen")
            {
                model.Add<TextButtonModel>(new TextButtonModel(Locale.GetString("Droodism.CrewCabinScript.RefillShield"), (Action<TextButtonModel>)(b => this.RefillWater())));
            }
        }

        private void OnChangedSOI(IOrbitNode orbit)
        {
            currentPlanetName = PartScript.CraftScript.CraftNode.Parent.Name;
            RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(currentPlanetName);
        }

        protected override void UpdateFuelSources()
        {
            var patchScript = this.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            try
            {
                if (patchScript==null)
                {
                    Mod.Log("CrewCabinScript.UpdateFuelSources: patchScript is null");
                    return;
                }

                try
                {
                    BatterySource = PartScript.BatteryFuelSource;
                    WaterSource = patchScript.WaterFuelSource;
                    LiquidHydrogenSouce = this.GetRegularCraftFuelSource("LH2");
                }
                catch (Exception e)
                {
                }
            }
            catch (Exception e)
            {
              
            }
           
            
        }
        
        private void CheckRadiationState(in FlightFrameData data)
        {
            if (Data.RadiationShieldDuration<=0)
            {
                return;
            }
            CheckInCraftRadiationSource();
            var craftPCIPos = this.PartScript.CraftScript.FlightData.Position.ToVector3();
            
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
            float craftProtection = Mathf.Clamp01(GetCraftRadiationProtection());

            // Effective dose that consumes shield is reduced by protection
            float effectiveDoseRate =
                Mathf.Max(0f, innerDoseRateRadPerHour) * (1f - innerProtection) +
                Mathf.Max(0f, outerDoseRateRadPerHour) * (1f - outerProtection) +
                GetNTRRadiationDoseRate() * (1f - craftProtection) +
                GetRTGRadiationDoseRate() * (1f - craftProtection);

            this.Data.RadiationShieldDuration -= effectiveDoseRate * deltaHours*0.1f; 
            
            
           
        }

        private void TryRefreshCachedShieldMassAndRecalcIfNeeded()
        {
            if (_recalcShieldMassFlag) return;
            if (this.Data == null || this.PartScript?.CraftScript == null) return;

            var newMass = this.Data.ComputeShieldMassDry();
            var oldMass = this.Data.CachedShieldMassDry;
            if (Mathf.Abs(newMass - oldMass) < 1e-5f) return;

            this.Data.CachedShieldMassDry = newMass;
            _recalcShieldMassFlag = true;
            this.PartScript.CraftScript.SetStructureChanged();
            _recalcShieldMassFlag = false;
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

        public float GetCraftRadiationProtection()
        {
            var t = Data.RadiationShieldType ?? "None";
            switch (t)
            {
                case "None":
                    return 0f;
                case "":
                    return 0f;
                case "Aluminium":
                    return 0.4f;
                case "PolyEthylene":
                    return 0.8f;
                case "Borated PolyEthylene":
                    return 0.85f;
                case "Water":
                    return 0.35f;
                case "Liquid Hydrogen":
                    return 0.45f;
                case "Boron Nitride Nanotubes":
                    return 0.9f;
                default:
                    return 0f;
            }
        }

        private void CheckInCraftRadiationSource()
        {
            if (!ModSettings.Instance.ReceiveCraftRadiation)
            {
                return;
            }
            RTGParts.Clear();
            NTRParts.Clear();
            foreach (var partData in PartScript.CraftScript.Data.Assembly.Parts)
            {
                if (partData.PartType.Name == "Generator2")
                {
                    RTGParts.Add(partData);
                }
                if (partData.PartType.Name == "Rocket Engine")
                {
                    var rocketEngineScript = partData.PartScript.GetModifier<RocketEngineScript>();
                    if (rocketEngineScript != null)
                    {
                        if (rocketEngineScript.Data.EngineType.Name == "Nuclear Thermal")
                        {
                            NTRParts.Add(partData);
                        }
                    }
                }
            }
        }

        private float GetRTGRadiationDoseRate()
        {
            if (!ModSettings.Instance.ReceiveCraftRadiation || RTGParts.Count == 0)
            {
                return 0;
            }

            float finalResult = 0;
            foreach (var partData in RTGParts)
            {
                float distance = Mathf.Clamp(Vector3.Distance(partData.PartScript.GameObject.transform.position, this.PartScript.GameObject.transform.position), 0.1f, 10f);
                finalResult += (1f / (distance * distance)) * 0.2f;
            }
            return finalResult;
        }

        private float GetNTRRadiationDoseRate()
        {
            if (!ModSettings.Instance.ReceiveCraftRadiation || NTRParts.Count == 0)
            {
                return 0;
            }
            float finalResult = 0;
            foreach (var partData in NTRParts)
            {
                if (partData.Activated)
                {
                    float distance = Mathf.Clamp(Vector3.Distance(partData.PartScript.GameObject.transform.position, this.PartScript.GameObject.transform.position), 0.1f, 10f);
                    finalResult += (1f / (distance * distance)) * 0.2f;
                }
            }
            return finalResult;
        }

        public override void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            groupModel.Add<TextModel>(new TextModel("<color=yellow>" + Locale.GetString("Droodism.CrewCabinScript.RadiationShieldType") + "</color>",(Func<string>) (()=> this.Data.RadiationShieldType),tooltip: Locale.GetString("Droodism.CrewCabinScript.RadiationShieldTypeTooltip")));
            groupModel.Add<ProgressBarModel>(new ProgressBarModel(()=>
                Locale.GetString("Droodism.CrewCabinScript.RadiationDuration"), 
                () => (float)(this.Data.RadiationShieldDuration / this.Data.RadiationShieldDurationUpperLimit)));
        }

        public void RefillWater()
        {
            if (this.Data == null) return;

            var upper = this.Data.RadiationShieldDurationUpperLimit;
            var current = this.Data.RadiationShieldDuration;
            var missingDurability = upper - current;
            if (missingDurability <= 0d) return;

            IFuelSource refillSource = null;
            double durabilityPerFuelUnit = 0d;
            switch (this.Data.RadiationShieldType)
            {
                case "Water":
                    refillSource = this.WaterSource;
                    durabilityPerFuelUnit = 0.8d;
                    break;
                case "Liquid Hydrogen":
                    refillSource = this.LiquidHydrogenSouce;
                    durabilityPerFuelUnit = 0.65d;
                    break;
                default:
                    return;
            }

            if (refillSource == null || durabilityPerFuelUnit <= 0d || refillSource.TotalFuel <= 0d) return;

            // 需要的资源量 = 缺失耐久 / 每单位资源可恢复的耐久。
            var requiredFuelAmount = missingDurability / durabilityPerFuelUnit;
            var removedFuelAmount = refillSource.RemoveFuel(requiredFuelAmount);
            if (removedFuelAmount <= 0d) return;

            var restoredDurability = removedFuelAmount * durabilityPerFuelUnit;
            var rebuilt = Math.Min(upper, current + restoredDurability);
            this.Data.RebuildShield(rebuilt);
            TryRefreshCachedShieldMassAndRecalcIfNeeded();
        }
        
        
        
    }
}