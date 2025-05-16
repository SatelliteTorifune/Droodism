using System;
using System.Collections.Generic;
using System.Linq;
using ModApi.Design.Events;
using ModApi.Scenes.Events;
using System.Xml;
using UnityEngine;
using HarmonyLib;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Design;
using Assets.Scripts.Vizzy.UI;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.Ui;

//using Assets.Scripts.Craft.Parts.Modifiers.SupportLifeScript

namespace Assets.Scripts
{
    
    public class SearchDrood
    {
        private DesignerScript _designer;
        public DesignerScript Designer => this._designer;
        public Dictionary<FuelType, FuelTankData> Tanks = new Dictionary<FuelType, FuelTankData>();
        /// <summary>
        /// AddLSModifier方法接受PartData参数,为此part添加SupportLife和FuelTank的modifier
        /// AddLSModifier Method receive ParaData as a parameter,adding this part with SupportLife and FuelTank Modifier
        /// </summary>
        /// <param name="part"></param>
        public static void AddLSModifier(PartData part)
        {

            if (!(part != null))
                return;
            SupportLifeData _supportLifeData = part.GetModifier<SupportLifeData>();
            if (_supportLifeData==null)
            {
                _supportLifeData = RenkosCreateModifierData<SupportLifeData>(part);
                _supportLifeData.OxygenComsumeRate = 10f;
            }
        }
        public static T RenkosCreateModifierData<T>(PartData part) where T : PartModifierData
        {
            T fromDefaultXml = PartModifierData.CreateFromDefaultXml<T>(part);
            return fromDefaultXml;
        }
        
    }
}