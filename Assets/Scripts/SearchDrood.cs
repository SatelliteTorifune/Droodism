
using System.Xml.Linq;
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
                _supportLifeData.FuelSourceAttachPoint = 0;
                _supportLifeData.PartPropertiesEnabled = false;
                _supportLifeData.InspectorEnabled = false;
            }
            
            
        }
        public static T RenkosCreateModifierData<T>(PartData part) where T : PartModifierData
        {
            T fromDefaultXml = PartModifierData.CreateFromDefaultXml<T>(part);
            return fromDefaultXml;
        }

        
        public static FuelType CreateOxygenFuelType()
        {
            XElement fuelTypeXml = new XElement("Fuel");
            fuelTypeXml.SetAttributeValue("id", "Oxygen");
            fuelTypeXml.SetAttributeValue("name", "Oxygen");
            fuelTypeXml.SetAttributeValue("gamma", 0f);
            fuelTypeXml.SetAttributeValue("density", 0.001404f);
            fuelTypeXml.SetAttributeValue("molecularWeight", 32f);
            fuelTypeXml.SetAttributeValue("combustionTemperature", 0);
            fuelTypeXml.SetAttributeValue("price", 200);
            fuelTypeXml.SetAttributeValue("enginePriceScale", 1f);
            fuelTypeXml.SetAttributeValue("explosivePower", 0.5f);
            fuelTypeXml.SetAttributeValue("description", "A custom fuel type for testing");
            fuelTypeXml.SetAttributeValue("fuelTransferRate", 200f);
            fuelTypeXml.SetAttributeValue("displayInDesigner", false);
            fuelTypeXml.SetAttributeValue("storageOverhead", 0.3f);
            XElement visualElement = new XElement("Visual");
            visualElement.SetAttributeValue("exhaustColor", "FFFFFFFF"); 
            visualElement.SetAttributeValue("exhaustColorExpanded", "FFFFFFFF");
            visualElement.SetAttributeValue("exhaustColorTip", "FFFFFFFF");
            visualElement.SetAttributeValue("exhaustColorShock", "FFFFFFFF");
            visualElement.SetAttributeValue("exhaustColorFlame", "FFFFFFFF");
            visualElement.SetAttributeValue("exhaustColorSoot", "FFFFFFFF"); 
            visualElement.SetAttributeValue("exhaustColorSmoke", "FFFFFFFF"); 
            visualElement.SetAttributeValue("shockIntensity", 2f);
            visualElement.SetAttributeValue("globalIntensity", 2f);
            visualElement.SetAttributeValue("rimShade", 0.5f);
            visualElement.SetAttributeValue("smokeOffset", 1f);
            fuelTypeXml.Add(visualElement);
            FuelType customFuelType = new FuelType(fuelTypeXml, Mod.Inctance.Mod);
            return customFuelType;
        }
    }
    
}