using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Events;
using ModApi.Mods;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
    {
        private static readonly string EvaPartName = "Eva";
        private static readonly string EvaTouristPartName = "Eva-Tourist";
        private static readonly string GeneratorPartName = "Generator1";
        private static readonly string EvaDataModifierName = "EvaData";
        private static readonly string SupportLifeDataModifierName = "SupportLifeData";

        

        /// <summary>
        /// Called when a craft is loaded. Adds life support and related modifiers to specific parts.
        /// </summary>
        private void OnCraftLoaded()
        {
            var craft = Craft;
            if (craft?.Data?.Assembly?.Parts == null) return;

            // Process Drood parts
            foreach (var part in GetPartsWithEvaModifier(craft, withoutLifeSupport: true))
            {
                AddLifeSupportModifier(part);
            }

            // Process Generator parts
            foreach (var part in GetPartsByType(craft, GeneratorPartName))
            {
                AddLifeSupportGeneratorModifiers(part);
            }

            // Process Command Pods
            foreach (var part in GetCommandPods(craft))
            {
                PatchCommandPod(part);
            }

            // Process Crew Compartments
            foreach (var part in GetCrewCompartments(craft))
            {
                AddCrewCompartmentPatch(part);
            }

            GetDroodCountInDesigner();
        }

        /// <summary>
        /// Called when a part is added to the craft. Adds appropriate modifiers based on part type.
        /// </summary>
        private void OnPartAdded(object sender, CreatedPartEventArgs e)
        {
            var part = e.Part;
            if (part == null) return;

            if (part.Name == EvaPartName || part.Name == EvaTouristPartName)
            {
                AddLifeSupportModifier(part);
            }
            else if (part.Name == GeneratorPartName)
            {
                AddLifeSupportGeneratorModifiers(part);
            }
            else if (part.PartType.IsCommandPod && !part.PartType.Name.Contains(EvaPartName))
            {
                PatchCommandPod(part);
            }
            else if (part.GetModifier<CrewCompartmentData>() != null && !part.PartType.Name.Contains(EvaPartName))
            {
                AddCrewCompartmentPatch(part);
            }
        }

        /// <summary>
        /// Retrieves parts with EvaData modifier, optionally excluding those with SupportLifeData.
        /// </summary>
        private List<PartData> GetPartsWithEvaModifier(CraftScript craft, bool withoutLifeSupport = false)
        {
            return craft.Data.Assembly.Parts.Where(part => part.PartScript.Modifiers.Any(modifier => modifier.GetData().Name == EvaDataModifierName && (!withoutLifeSupport || part.PartScript.Modifiers.All(m =>m.GetData().Name != SupportLifeDataModifierName)))).ToList();
        }

        /// <summary>
        /// Retrieves parts of a specific type by name.
        /// </summary>
        private List<PartData> GetPartsByType(CraftScript craft, string partTypeName)
        {
            return craft.Data.Assembly.Parts.Where(part => part.PartType.Name == partTypeName).ToList();
        }

        /// <summary>
        /// Retrieves command pod parts, excluding those containing "Eva" in their name.
        /// </summary>
        private List<PartData> GetCommandPods(CraftScript craft)
        {
            return craft.Data.Assembly.Parts.Where(part => part.PartType.IsCommandPod && !part.PartType.Name.Contains(EvaPartName)).ToList();
        }

        /// <summary>
        /// Retrieves crew compartment parts, excluding those containing "Eva" in their name.
        /// </summary>
        private List<PartData> GetCrewCompartments(CraftScript craft)
        {
            return craft.Data.Assembly.Parts.Where(part => part.GetModifier<CrewCompartmentData>() != null && !part.PartType.Name.Contains(EvaPartName)).ToList();
        }

        /// <summary>
        /// Adds a SupportLife modifier to the specified part if it doesn't already exist.
        /// </summary>
        private static void AddLifeSupportModifier(PartData part)
        {
            if (part == null) return;

            var supportLifeData = part.GetModifier<SupportLifeData>();
            if (supportLifeData == null)
            {
                supportLifeData = PartModifierData.CreateFromDefaultXml<SupportLifeData>(part);
                supportLifeData.PartPropertiesEnabled = true;
                supportLifeData.InspectorEnabled = true;
                //LOG($"Added SupportLifeData to part {part.Name}");
            }
        }

        /// <summary>
        /// Adds LifeSupportGeneratorData and Water_DesalinationData modifiers to the specified part.
        /// </summary>
        private static void AddLifeSupportGeneratorModifiers(PartData part)
        {
            if (part == null) return;

            var lsgData = part.GetModifier<LifeSupportGeneratorData>();
            if (lsgData == null)
            {
                lsgData = PartModifierData.CreateFromDefaultXml<LifeSupportGeneratorData>(part);
                lsgData.PartPropertiesEnabled = false;
                lsgData.InspectorEnabled = true;
                //LOG($"Added LifeSupportGeneratorData to part {part.Name}");
            }

            var waterData = part.GetModifier<Water_DesalinationData>();
            if (waterData == null)
            {
                waterData = PartModifierData.CreateFromDefaultXml<Water_DesalinationData>(part);
                waterData.PartPropertiesEnabled = true;
                waterData.InspectorEnabled = true;
                //LOG($"Added Water_DesalinationData to part {part.Name}");
            }
        }

        /// <summary>
        /// Patches a command pod part with STCommandPodPatchData if not already present.
        /// </summary>
        private static void PatchCommandPod(PartData part)
        {
            if (part == null) return;

            var targetScript = part.GetModifier<STCommandPodPatchData>();
            if (targetScript == null)
            {
                targetScript = PartModifierData.CreateFromDefaultXml<STCommandPodPatchData>(part);
                targetScript.PartPropertiesEnabled = false;
                targetScript.InspectorEnabled = false;
                //LOG($"Patched CommandPod {part.Name} with STCommandPodPatchData");
            }
        }

        /// <summary>
        /// Adds a CrewCabinData modifier to the specified crew compartment part.
        /// </summary>
        private static void AddCrewCompartmentPatch(PartData part)
        {
            if (part == null) return;

            var targetScript = part.GetModifier<CrewCabinData>();
            if (targetScript == null)
            {
                targetScript = PartModifierData.CreateFromDefaultXml<CrewCabinData>(part);
                targetScript.PartPropertiesEnabled = false;
                targetScript.InspectorEnabled = false;
                //LOG($"Added CrewCabinData to part {part.Name}");
            }
        }
    }
}