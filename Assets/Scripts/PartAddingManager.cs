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
                if (part.Name.Contains("Capsule"))
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

        public void AddFuelTankModifier(PartData part)
        {
            if (part==null||part.Modifiers.Count>11)
                return;
            Debug.LogFormat("AddFuelTankModifier Called");
            void AddSingleFuelTankModifier(string fuelType, double fuelCapacity, double fuelAmount)
            {
                Assets.Scripts.Craft.Parts.Modifiers.FuelTankData _fuelTankData;
                _fuelTankData = PartModifierData.CreateFromDefaultXml<FuelTankData>(part);
                _fuelTankData.Fuel=fuelAmount;
                _fuelTankData.Capacity=fuelCapacity;
                SetFuelType(_fuelTankData, fuelType);
                SetAutoFuelType(_fuelTankData);
                _fuelTankData.InspectorEnabled = false;
                _fuelTankData.PartPropertiesEnabled = false;
                _fuelTankData.SubPriority = 29;


            }
            AddSingleFuelTankModifier("Oxygen",60,60);
            AddSingleFuelTankModifier("H2O",0.3,0.3);
            AddSingleFuelTankModifier("Food",5,5);
            AddSingleFuelTankModifier("Wasted Water",0.3,0);
            AddSingleFuelTankModifier("CO2",60,0);
            AddSingleFuelTankModifier("Solid Waste",5,0);
            try
            {
                XElement _xml;
                _xml = part.GenerateXml(part.PartScript.CraftScript.Transform, false);
                part.LoadXML(_xml, 15);
            }
            catch (Exception e)
            {
                Debug.LogFormat("最后一步出事儿啦{0}",e);
            }
            
        }
        
        private void SetFuelType(FuelTankData fuelTankData, string fuelTypeId)
        {
            try
            {
                var fieldInfo = AccessTools.Field(typeof(FuelTankData), "_fuelType");
                if (fieldInfo == null)
                {
                    Debug.LogError($"Cannot find _fuelType field in FuelTankData");
                    return;
                }

                fieldInfo.SetValue(fuelTankData, fuelTypeId);
                Debug.Log($"Set FuelType {fuelTypeId} for FuelTankData");
            }
            catch (Exception e)
            {
                Debug.LogError($"SetFuelType 出错 for {fuelTypeId}: {e}");
            }
        }
        
        private void SetAutoFuelType(FuelTankData fuelTankData)
        {
            try
            {
                var fieldInfo = AccessTools.Field(typeof(FuelTankData), "_autoFuelType");
                if (fieldInfo == null)
                {
                    Debug.LogError($"Cannot find _fuelType field in FuelTankData");
                    return;
                }

                fieldInfo.SetValue(fuelTankData, false);
                Debug.Log($"Set AutoFuelType for FuelTankData");
            }
            catch (Exception e)
            {
                Debug.LogError($"SetFuelType 出错 for {fuelTankData.FuelType.Name}: {e}");
            }
        }
        
    }
}