using System;
using System.Collections.Generic;
using System.Linq;
using ModApi.Design.Events;
using ModApi.Scenes.Events;
using System.Xml;
using UnityEngine;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using ModApi.Craft.Parts;

namespace Assets.Scripts
{
    
    public class SearchDrood
    {
        
        
        public static void OnPartAdded (object sender, DesignerPartAddedEventArgs e)
        {
            if (e.DesignerPart.Name=="Drood"||e.DesignerPart.Name=="Tourist")
            {
                Debug.Log("执行添加modifier的操作!");
                AddLSModifier();
                
                
            }
            
        } 
        public static void AddLSModifier()
        {
            Debug.LogFormat("执行添加modifier的操作 函数");
            XElement LSelement = new XElement("SupportLife");
            LSelement.SetAttributeValue("",10f);
            //下面加油箱
            XElement FuelTankOxygen = new XElement("FuelTank");
            FuelTankOxygen.SetAttributeValue("capacity","10");
            FuelTankOxygen.SetAttributeValue("fuel", "10");
            FuelTankOxygen.SetAttributeValue("fuelType","Oxygen");
            FuelTankOxygen.SetAttributeValue("autoFuelType","False");
            FuelTankOxygen.SetAttributeValue("partPropertiesEnabled", false);
            
        }
        /// <summary>
        /// CheckDrood方法接受CraftScript参数,遍历所有modifier,得到含有Eva Modifier的Part的Id的列表
        /// 目前没返回值
        /// </summary>
        public static void CheckDrood(CraftScript craft)
        {
            List<int> DroodIds = new List<int>();
            List<PartData> DroodParts = new List<PartData>();
            
                
            var parts = craft.Data.Assembly.Parts;
            foreach (PartData part in parts)
            {
                bool isDrood = false;
                bool hasLifeSupport = false;
                var modifiers = part.PartScript.Modifiers;
                
                if (modifiers != null)
                {
                    foreach (PartModifierScript _pms in modifiers)
                    {
                        PartModifierData _modifierData = _pms.GetData();
                        
                        if (_modifierData.Name=="EvaData")
                        {
                            
                            isDrood = true;
                        }

                        if (_modifierData.Name == "SupportLifeData")
                        {
                            hasLifeSupport = true;
                        }
                    }
                }
                if (isDrood&&!hasLifeSupport)
                {
                    DroodIds.Add(part.Id);
                    DroodParts.Add(part);
                }
            }

            for (int i = 0; i < DroodIds.Count; i++)
            {
                Debug.LogFormat($"LIST is here {DroodParts[i].Name}");
            }
        }
    }
}