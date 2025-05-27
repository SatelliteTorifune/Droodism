
using System.Xml;
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
                _supportLifeData.OxygenComsumeRate = 0.01f;
                _supportLifeData.FoodComsumeRate = 0.01f;
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
        
        
    }
    
}