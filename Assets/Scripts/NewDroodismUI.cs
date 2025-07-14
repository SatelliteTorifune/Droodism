using System;
using UI.Xml;
using ModApi.Ui;
using UnityEngine;
using ModApi.Math;
using System.Collections.Generic;
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

        private bool _mainPanelVisible = false;
        private bool _createEventPanelVisible = false;
        private bool _notifPanelVisible = false;

        private int _lastClickedId = -1;
        private float _lastClickTime = 0.0f;
        private int _editEventId = -1;

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
            Debug.LogFormat("NewDroodismUI:1");
            
            fuelXMLItems.Clear();
            Debug.LogFormat("NewDroodismUI:2");
            
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
            
            // 更新每个燃料项的进度
            for (int i = 0; i < fuelTypeIDList.Count; i++)
            {
                UpdateFuelItem(fuelXMLItems[i], fuelTypeIDList[i], UpdateFuelPercentageValue(fuelTypeIDList[i]));
            }
        }
        private void UpdateFuelItem(XmlElement item, string fuelType, double percentage)
        {
            bool isWasted = (fuelType.Contains("Waste") ||fuelType.Contains("CO2"));
            Color progressColor = GetProgressColor(percentage,isWasted);
            item.GetElementByInternalId("FuelPercentage").SetText("<color=#"+ColorUtility.ToHtmlStringRGB(progressColor)+$">{percentage:P2}</color>");
            XmlElement progressBar = item.GetElementByInternalId("FuelProgressBar");
            progressBar.SetAndApplyAttribute("width", $"{percentage*100}%");
            progressBar.SetAndApplyAttribute("offsetXY", $"{-100 + (100 * (float)percentage)},0");
            progressBar.SetAndApplyAttribute("color", $"#{ColorUtility.ToHtmlStringRGB(progressColor)}");
            
        }

        private Color GetProgressColor(double percentage,bool isWasted)
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
            Debug.LogFormat("Fuel Percentage Item Clicked{0}",item);
        }
        

        public double UpdateFuelPercentageValue(string fuelTypeId)
        {
            var craftSources = Game.Instance.FlightScene.CraftNode.CraftScript.FuelSources.FuelSources;
            
            foreach (var source in craftSources)
            {
                if (source.FuelType.Id.Contains(fuelTypeId))
                {
                    return (source.TotalFuel/source.TotalCapacity);
                }
            }
            return 0.0d;
        }
        
    }
}