using System;
using UI.Xml;
using ModApi.Ui;
using UnityEngine;
using ModApi.Math;
using System.Collections.Generic;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Mods;
using ModApi.Audio;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;

namespace Assets.Scripts
{
    public class NewDroodismUI:MonoBehaviour
    {
        private XmlLayoutController controller;
        private XmlElement mainPanel;
        private XmlElement FuelPercentageItemTemplate;
        private XmlElement fuelPercentageList;
        private List<XmlElement> fuelXMLItems = new List<XmlElement>();

        public bool _mainPanelVisible = false;
        private bool _createEventPanelVisible = false;
        private bool _notifPanelVisible = false;

        private int _lastClickedId = -1;
        private float _lastClickTime = 0.0f;
        private int _editEventId = -1;
        public readonly string[] _massTypes = { "g", "kg", "t", "kt" };

        private List<string> fuelTypeIDList = new List<string>() {  "Oxygen","H2O","Food","CO2","Wasted Water","Solid Waste"};
        public void OnTogglePanelState() 
        { 
            _mainPanelVisible = !_mainPanelVisible;
        }
        public void OnLayoutRebuilt(IXmlLayoutController layoutController)
        {
            controller = (XmlLayoutController)layoutController;
            mainPanel = controller.xmlLayout.GetElementById("droodism-inspect-panel");
            FuelPercentageItemTemplate = controller.xmlLayout.GetElementById("fuel-percentage-item-template");
            fuelPercentageList = controller.xmlLayout.GetElementById("FuelPercentageList");
            InitializeFuelItems();
        }
        private void InitializeFuelItems()
        {
            // 清除现有项
            foreach (var item in new List<XmlElement>(fuelPercentageList.childElements))
                fuelPercentageList.RemoveChildElement(item, true);
            fuelXMLItems.Clear();
            
            // 为每种燃料类型创建UI项
            foreach (var fuelType in fuelTypeIDList)
            {
                AddFuelListItem(fuelType);
            }
        }
        private void AddFuelListItem(string fuelType)
        {
            // 克隆模板
            XmlElement listItem = Instantiate(FuelPercentageItemTemplate);
            XmlElement component = listItem.GetComponent<XmlElement>();
            
            // 初始化并添加到列表
            component.Initialise(fuelPercentageList.xmlLayoutInstance, 
                (RectTransform)listItem.transform, 
                FuelPercentageItemTemplate.tagHandler);
            fuelPercentageList.AddChildElement(component);
            component.SetActive(true);
            component.SetAttribute("fuel-type-id", fuelType);
            component.ApplyAttributes();
            
            // 设置初始文本
            component.GetElementByInternalId("FuelTypeName").SetText(Game.Instance.PropulsionData.GetFuelType(fuelType).Name);
            
            fuelXMLItems.Add(component);
            Debug.LogFormat("NewDroodismUI:AddFuelListItem:{0}", fuelType);
        }
        
        public void UpdateFuelPercentageItemTemplate()
        {
            if (fuelXMLItems.Count == 0) return;
            if ( Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count == 1 && Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva"))
            {
                for (int i = 0; i < fuelTypeIDList.Count; i++)
                {
                    UpdateEvaFuelParameterValue(fuelTypeIDList[i],out double fuelAmount,out double fuelCapacity);
                    UpdateFuelItem(fuelXMLItems[i], fuelTypeIDList[i], fuelAmount,fuelCapacity);
                } 
            }
            else
            
            {for (int i = 0; i < fuelTypeIDList.Count; i++)
            {
                UpdateCraftFuelParameterValue(fuelTypeIDList[i],out double fuelAmount,out double fuelCapacity);
                UpdateFuelItem(fuelXMLItems[i], fuelTypeIDList[i], fuelAmount,fuelCapacity);
            } }
            
        }
        private void UpdateFuelItem(XmlElement item, string fuelType, double fuelAmount,double fuelCapacity)
        {
            float fuelDensity = Game.Instance.PropulsionData.GetFuelType(fuelType).Density;
            double percentage = fuelAmount / fuelCapacity;
            bool isWasted = (fuelType.Contains("Waste") ||fuelType.Contains("CO2"));
            string temp=Mod.Inctance.FormatFuel(fuelAmount*fuelDensity, _massTypes) + "/" + Mod.Inctance.FormatFuel(fuelCapacity*fuelDensity, _massTypes);
            Color progressColor = GetProgressBarColor(percentage,isWasted);
            
            item.GetElementByInternalId("FuelPercentage").SetText("<color=#"+ColorUtility.ToHtmlStringRGB(progressColor)+$">{percentage:P2}</color>");
            
            item.GetElementByInternalId("FuelAmountPercentage").SetText(temp);
            
            XmlElement progressBar = item.GetElementByInternalId("FuelProgressBar");
            progressBar.SetAndApplyAttribute("width", $"{percentage*100}%");
            progressBar.SetAndApplyAttribute("offsetXY", $"{-100 + (100 * (float)percentage)},0");
            progressBar.SetAndApplyAttribute("color", $"#{ColorUtility.ToHtmlStringRGB(progressColor)}");
            
        }

        private Color GetProgressBarColor(double percentage,bool isWasted)
        {
            if (!isWasted)
            {
                if (percentage > 0.6) return new Color(0.1f, 0.8f, 0.1f);   // 蓝色
                if (percentage > 0.3) return new Color(1, 0.6f, 0);       // 橙色
                return new Color(1, 0.2f, 0.2f);     // 红色
            }
            else
            {
                if (percentage > 0.9) return new Color(1, 0.2f, 0.2f);// 蓝色
                if (percentage > 0.4) return new Color(1, 0.6f, 0);       // 橙色
                return new Color(0.1f, 0.8f, 0.1f);    
            }
            
                                
        }

        public void SetUIVisibility(bool state)
        {
            mainPanel.SetActive(state && _mainPanelVisible);
        }

        public void OnFuelPercentageItemClick(XmlElement item)
        {
            try
            {
                Debug.LogFormat("Fuel Percentage Item Clicked{0}", item.GetAttribute("fuel-type-id"));
            }
            catch (Exception e)
            {
                Debug.LogFormat($"我操{e}");
            }
        }

        private void UpdateEvaFuelParameterValue(string fuelTypeId, out double fuelAmount, out double fuelCapacity)
        {
            
            fuelAmount = 0;
            fuelCapacity = 0;
            if (Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count == 1 &&
                Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva"))
            {
                var craftSources = Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Modifiers;
            
                // Iterate through each modifier
                foreach (var source in craftSources)
                {
                    // Check if the modifier's name contains "Tank"
                    if (source.GetData().Name.Contains("Tank"))
                    {
                        // Disable the inspector for this modifier
                        source.GetData().InspectorEnabled = false;
                        // Cast the modifier to FuelTankScript
                        FuelTankScript fts = source as FuelTankScript;
                        // Check if the fuel tank's type name contains the specified fuel type
                        if (fts.FuelType.Id.Contains(fuelTypeId))
                        {
                            // Return the fuel tank if it matches the specified type
                           fuelAmount=fts.TotalFuel;
                           fuelCapacity=fts.TotalCapacity;
                        }
                    }
                
                }
            }
        }

        private void UpdateCraftFuelParameterValue(string fuelTypeId,out double fuelAmount,out double fuelCapacity)
        {
            List<IFuelSource> ManymanySources=new List<IFuelSource>();
            IFuelSource randomNameSourve=null;
            fuelAmount = 0;
            fuelCapacity = 0;
            var craftSources =Game.Instance.FlightScene.CraftNode.CraftScript.FuelSources.FuelSources;
            foreach (var source in craftSources)
            {
                if (source.FuelType.Id.Contains(fuelTypeId))
                {
                    ManymanySources.Add(source);
                }
            }

            foreach (var source2 in ManymanySources)
            {
                
            }
            
                
        }
        
    }
}