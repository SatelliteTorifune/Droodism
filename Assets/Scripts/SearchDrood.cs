
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Design;
using Assets.Scripts.Vizzy.UI;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Events;
using ModApi.Craft.Propulsion;
using ModApi.Ui;
using HarmonyLib;
using UnityEngine;

//using Assets.Scripts.Craft.Parts.Modifiers.SupportLifeScript

namespace Assets.Scripts
{
    
    public class SearchDrood
    {
        
        private DesignerScript _designer;
        public DesignerScript Designer => this._designer;
        
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
                _supportLifeData = RenkosCreateModifierData<SupportLifeData>(part);
                _supportLifeData.OxygenComsumeRate = 10f;
                _supportLifeData.PartPropertiesEnabled = false;
                _supportLifeData.InspectorEnabled = false;
            }
            
            if (part.Modifiers.Count<=6)
            {
                FuelTankData _fuelTankData = RenkosCreateModifierData<FuelTankData>(part);
                _fuelTankData.Capacity = 10;
                _fuelTankData.Fuel = 10;
                _fuelTankData.Utilization = -1;
                _fuelTankData.InspectorEnabled = false;
                _fuelTankData.PartPropertiesEnabled = false;
                AccessTools.Field(typeof(FuelTankData), "_autoFuelType").SetValue(_fuelTankData, false);
                AccessTools.Field(typeof(FuelTankData), "_fuelType").SetValue(_fuelTankData, "Oxygen");
                Debug.LogFormat($"设置后的 _fuelType: {_fuelTankData.FuelType}");
                
            }
            
        }
        public static T RenkosCreateModifierData<T>(PartData part) where T : PartModifierData
        {
            T fromDefaultXml = PartModifierData.CreateFromDefaultXml<T>(part);
            return fromDefaultXml;
        }
        
    }
    
}