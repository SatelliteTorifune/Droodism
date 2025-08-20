using System.Collections.Generic;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Events;
using ModApi.Mods;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod:GameMod

    {
        
        /// <summary>
        /// 在加载Craft时使用"CheckDrood"方法遍历所有modifier得到零件并添加SupportLife的modifier
        /// When load a craft get all Craft's modifier using "CheckDrood" method and adding a "SupportLife"modifie to the part
        /// </summary>
        private void OnCraftLoaded()
        {
            GetDroodCountInDesigner();
            foreach (PartData part in CheckDrood(Craft))
            {
                AddLsModifier(part);
                //PatchCommandPod(part);
            }

            foreach (PartData part in  CheckGenerator(Craft))
            {
                AddLSGModifier(part);
            }

            foreach (PartData part in  CheckCommandPod(Craft))
            {
                PatchCommandPod(part);
            }

        }
        /// <summary>
        /// 在part添加时检测如果是Drood则添加SupportLife modifier和其他属性
        /// Adding SupportLife modifier when the added part is Drood
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPartAdded(object sender,CreatedPartEventArgs e)
        {
            
            //Debug.LogFormat($"{e.Part.PartType.Name},id{e.Part.Id}有{e.Part.Modifiers.Count}个modifier,1:{e.PartType.Name}");
            if (e.Part.Name=="Eva"||e.Part.Name == "Eva-Tourist")
            {
                AddLsModifier(e.Part);
                //PatchCommandPod(e.Part);
            }
            
            if (e.Part.Name == "Generator1")
            {
                AddLSGModifier(e.Part);
            }

            if (e.Part.PartType.IsCommandPod&&!e.Part.PartType.Name.Contains("Eva"))
            {
                PatchCommandPod(e.Part);
            }
            
            
        }
        /// <summary>
        /// CheckDrood方法接受CraftScript参数,遍历所有modifier,得到含有Eva Modifier的Part的类型为PartData的列表
        /// CheckDrood Method receives CraftScript as a parameter,checks all modifier inside the craft,returns with a list (which type is PartData) of Parts with Eva Modifier
        /// </summary>
        /// <param name="craft"></param>
        private List<PartData> CheckDrood(CraftScript craft)
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
            return DroodParts;

        }
        
        /// <summary>
        /// AddLSModifier方法接受PartData参数,为此part添加SupportLife和FuelTank的modifier
        /// AddLSModifier Method receive ParaData as a parameter,adding this part with SupportLife and FuelTank Modifier
        /// </summary>
        /// <param name="part"></param>
        private static void AddLsModifier(PartData part)
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
        
        private List<PartData> CheckGenerator(CraftScript craft)
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
        private List<PartData> CheckCommandPod(CraftScript craft)
        {
            List<PartData> CommandPodParts = new List<PartData>();
            foreach (PartData part in craft.Data.Assembly.Parts)
            {
                if (part.PartType.IsCommandPod&&!part.PartType.Name.Contains("Eva"))
                {
                    CommandPodParts.Add(part);
                }
            } return CommandPodParts;
        }
        private static void AddLSGModifier(PartData part)
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
        
        private  void PatchCommandPod(PartData part)
        {
            if (part==null)
                return;
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