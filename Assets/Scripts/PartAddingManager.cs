using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Design;
using HarmonyLib;
using Jundroo.ModTools;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.Design;
using ModApi.Mods;
using Panteleymonov;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod:GameMod

    {
        /// <summary>
        /// CheckDrood方法接受CraftScript参数,遍历所有modifier,得到含有Eva Modifier的Part的类型为PartData的列表
        /// CheckDrood Method receives CraftScript as a parameter,checks all modifier inside the craft,returns with a list (which type is PartData) of Parts with Eva Modifier
        /// </summary>
        /// <param name="craft"></param>
        public List<PartData> CheckDrood(CraftScript craft)
        {
            List<PartData> DroodParts = new List<PartData>();
            var parts = craft.Data.Assembly.Parts;
            foreach (PartData part in parts)
            {
                bool isDrood = false;
                bool hasLifeSupport = false;
                List<PartModifierScript> modifiers = part.PartScript.Modifiers;
                if (modifiers != null)
                {
                    foreach (PartModifierScript _pms in modifiers)
                    {

                        PartModifierData _modifierData = _pms.GetData();

                        if (_modifierData.Name == "EvaData")
                        {
                            isDrood = true;
                        }

                        if (_modifierData.Name == "SupportLifeData")
                        {
                            hasLifeSupport = true;
                        }
                    }
                }

                if (isDrood && !hasLifeSupport)
                {

                    DroodParts.Add(part);
                }

            }

            for (int i = 0; i < DroodParts.Count; i++)
            {
                Debug.LogFormat("DroodParts的 ID 是{0}", DroodParts[i].Id);
            }

            return DroodParts;

        }
        
        /// <summary>
        /// AddLSModifier方法接受PartData参数,为此part添加SupportLife和FuelTank的modifier
        /// AddLSModifier Method receive ParaData as a parameter,adding this part with SupportLife and FuelTank Modifier
        /// </summary>
        /// <param name="part"></param>
        public static void AddLsModifier(PartData part)
        {
            if (!(part != null))
                return;
            SupportLifeData _supportLifeData = part.GetModifier<SupportLifeData>();
            if (_supportLifeData==null)
            {
                _supportLifeData = PartModifierData.CreateFromDefaultXml<SupportLifeData>(part);
                _supportLifeData.PartPropertiesEnabled = true;
                _supportLifeData.InspectorEnabled = true;
            }
            
        }
        
        public List<PartData> CheckGenerator(CraftScript craft)
        {
            List<PartData> GeneratorParts = new List<PartData>();
            foreach (PartData part in craft.Data.Assembly.Parts)
            {

               
                if ( part.PartType.Name=="Generator1")
                {
                    GeneratorParts.Add(part);
                }
                
            }

            return GeneratorParts;

        }
        public List<PartData> CheckCommandPod(CraftScript craft)
        {
            List<PartData> CommandPodParts = new List<PartData>();
            foreach (PartData part in craft.Data.Assembly.Parts)
            {
                if (part.Name.Contains("Capsule")||part.Name.Contains("Command"))
                {
                    if (part.PartScript.Modifiers.Count <11)
                    {
                        CommandPodParts.Add(part);
                    }
                    
                }
                
            }

            return CommandPodParts;

        }
        public static void AddLSGModifier(PartData part)
        {
            if (part==null)
                return;
            LifeSupportGeneratorData _supportLifeData = part.GetModifier<LifeSupportGeneratorData>();
            if (_supportLifeData==null)
            {
                _supportLifeData = PartModifierData.CreateFromDefaultXml<LifeSupportGeneratorData>(part);
                _supportLifeData.PartPropertiesEnabled = false;
                _supportLifeData.InspectorEnabled = true;
            }
        }
        
        public static void PatchCommandPod(PartData part)
        {
            if (part==null)
                return;
            CommandPodData cmd=part.GetModifier<CommandPodData>();
            if (cmd==null)
            {return;}
            STCommandPodPatchData targetScript = part.GetModifier<STCommandPodPatchData>();
            if (targetScript==null)
            {
                targetScript = PartModifierData.CreateFromDefaultXml<STCommandPodPatchData>(part);
                targetScript.PartPropertiesEnabled = false;
                targetScript.InspectorEnabled = false;
            }
        }

        
    }
}