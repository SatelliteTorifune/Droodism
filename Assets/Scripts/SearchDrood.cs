using System;
using System.Collections.Generic;
using System.Linq;
using ModApi.Design.Events;
using ModApi.Scenes.Events;
using System.Xml;
using UnityEngine;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Design;
using ModApi.Craft.Parts;

namespace Assets.Scripts
{
    
    public class SearchDrood
    {
        
        
        public static void OnPartAdded (object sender, DesignerPartAddedEventArgs e)
        {
            
            Debug.LogFormat($"the part is added :{e.DesignerPart.Name}:{e.DesignerPart.GenerateXml()}");
            if (e.DesignerPart.Name=="Drood"||e.DesignerPart.Name=="Tourist")
            {
                Debug.Log("执行添加modifier的操作!");
                //理论上来说我这里应该加自己的一个Modifier,然后还有一堆其他的FuelTank
            }
            
        } 
        public static void AddLSModifier()
        {
            XElement element = new XElement("SupportLife");
            element.SetAttributeValue("",10f);
            //Tanks[fuelType] = PartModifierData.CreateFromStateXml(element, StarsSpaceGeneratorEditor.Data.Part, 15) as FuelTankData;
            //Tanks[fuelType].CreateScript();
        }

        public static void CheckDrood(CraftScript craft)
        {
            List<int> DroodIds = new List<int>();
            var parts = craft.Data.Assembly.Parts;
            foreach (PartData part in parts)
            {
                var modifiers = part.PartScript.Modifiers; // 假设
                if (modifiers != null)
                {
                    foreach (PartModifierScript _pms in modifiers)
                    {
                        PartModifierData _modifierData = _pms.GetData();
                        if (_modifierData.Name=="EvaData")
                        {
                            DroodIds.Add(part.Id);
                        }
                    }
                    
                }
            }
            
            
            for (int i = 0; i < DroodIds.Count; i++)
            {
                Debug.LogFormat("{0}",DroodIds[i]);
            }
        }
    }
}