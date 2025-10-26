using System;
using UI.Xml;
using ModApi.Ui;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.Flight.UI;

namespace Assets.Scripts
{
    //骗你的,其实这个所谓的New管的是LegacyUI,new是相对于远古版本的纯用flightInspectorPanel的版本的
    public class LegacyDroodismUI:MonoBehaviour
    {
        private XmlLayoutController controller;
        private XmlElement mainPanel,FuelPercentageItemTemplate,fuelPercentageList,FuelTransferItemList,FuelTransferItemModeTemplet;
        private List<XmlElement> fuelPercentXMLItems = new List<XmlElement>();
        private List<XmlElement> fuelTransferXMLItems = new List<XmlElement>();
        
        public bool mainPanelVisible = false;
        public bool fuelItemInspectorVisible = false;
        
        public readonly string[] _massTypes = { "g", "kg", "t", "kt" };
        public int DroodCountTotal, AstronautCount;
        public int TouristCount;
        private IReadOnlyList<string> fuelTypeIDList = new List<string>() {  "Oxygen","H2O","Food","CO2","Wasted Water","Solid Waste"};
        private double oxygenRate, h2oRate, foodRate, co2Rate, wastedWaterRate, solidWasteRate;
        private double photoBioReactorGrowRate;
        private Dictionary<string, (double Current, double Previous)> FuelMap = new Dictionary<string, (double, double)>
        {
            { "Oxygen", (0, 0) },
            { "H2O", (0, 0) },
            {"Food", (0, 0) },
            {"CO2", (0, 0) },
            {"Wasted Water", (0, 0) },
            {"Solid Waste", (0, 0) }
        };
        
        
        //哎哟我操这坨屎真的是你妈两个臭比的,你说这能要是能优化性能我给你嗦几把,我真的不想看这个破玩意,屎山代码看的我太头大了
        public void OnTogglePanelState() 
        {
            mainPanelVisible = !mainPanelVisible;
            
        }
       
        public void OnLayoutRebuilt(IXmlLayoutController layoutController)
        {
            controller = (XmlLayoutController)layoutController;
            mainPanel = controller.xmlLayout.GetElementById("droodism-inspect-panel");
            FuelPercentageItemTemplate = controller.xmlLayout.GetElementById("fuel-percentage-item-template");
            fuelPercentageList = controller.xmlLayout.GetElementById("FuelPercentageList");
            FuelTransferItemModeTemplet = controller.xmlLayout.GetElementById("droodism-fuel-transfer-mode-template");
            FuelTransferItemList=controller.xmlLayout.GetElementById("FuelTransferPercentageList");
            InitializeFuelItems();
            InitializeFuelTransferMode();
        }

        
        private void InitializeFuelItems()
        {
            // 清除现有项
            foreach (var item in new List<XmlElement>(fuelPercentageList.childElements))
                fuelPercentageList.RemoveChildElement(item, true);
            fuelPercentXMLItems.Clear();
            
            // 为每种燃料类型创建UI项
            foreach (var fuelType in fuelTypeIDList)
            {
                AddFuelListItem(fuelType);
            }
        }
        
        private void InitializeFuelTransferMode()
        {
            foreach (var item in new List<XmlElement>(FuelTransferItemList.childElements))
            {
                FuelTransferItemList.RemoveChildElement(item, true);
            }
            fuelTransferXMLItems.Clear();
            AddFuelTransferModeItem(FuelTransferMode.None);
            AddFuelTransferModeItem(FuelTransferMode.Fill);
            AddFuelTransferModeItem(FuelTransferMode.Drain);
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
            
            fuelPercentXMLItems.Add(component);
            //Debug.LogFormat("LegacyDroodismUI:AddFuelListItem:{0}", fuelType);
            //Mod.Instance.那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
        }
        private void AddFuelTransferModeItem(FuelTransferMode mode)
        {
            // 克隆模板
            XmlElement listItem = Instantiate(FuelTransferItemModeTemplet);
            XmlElement component = listItem.GetComponent<XmlElement>();
            // 初始化并添加到列表
            component.Initialise(FuelTransferItemList.xmlLayoutInstance, 
                (RectTransform)listItem.transform, 
                FuelTransferItemModeTemplet.tagHandler);
            FuelTransferItemList.AddChildElement(component);
            component.SetActive(true);
            component.SetAttribute("fuel-transfer-mode-id", mode.ToString());
            component.ApplyAttributes();
            
            // 设置初始文本
            component.GetElementByInternalId("FuelTransferTypeName").SetText(mode.ToString());
            
            fuelPercentXMLItems.Add(component);
            Debug.LogFormat("LegacyDroodismUI:AddFuelTransferListItem:{0}", mode.ToString());
            //Mod.Instance.那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
        }
        /// <summary>
        /// 更新UpdateFuelTemplate项目用的,属于是我拉的第二坨屎山,纯纯恶臭,我也不知道为什么要这么写,本来是为了解决性能问题的,但是这函数在Upddate()里面调用,而且还有贼鸡巴多的别的函数和foreach调用,你说这能要是能优化性能我给你嗦几把.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="fuelSource"></param>
        private void UpdateFuelTemplateItem(XmlElement item, IFuelSource fuelSource)
        {
            if (Game.Instance.FlightScene.TimeManager.Paused)
            {
                return;
            }
            double currentFuel,previousFuel;
            string fuelTypeId = fuelSource.FuelType.Id;
            if (FuelMap.ContainsKey(fuelTypeId))
            {
                currentFuel = fuelSource.TotalFuel;
                previousFuel = FuelMap[fuelTypeId].Previous;
                FuelMap[fuelTypeId] = (currentFuel, FuelMap[fuelTypeId].Previous);
            }
            else
            {
                currentFuel = fuelSource.TotalFuel;
                previousFuel = 0;
            }
            float fuelDensity = fuelSource.FuelType.Density;
            double percentage = fuelSource.TotalFuel / fuelSource.TotalCapacity;
            
            bool isWasted = (fuelTypeId.Contains("Waste") ||fuelTypeId.Contains("CO2"));
            string FuelAmountPercentage=Mod.Instance.FormatFuel(fuelSource.TotalFuel*fuelDensity, _massTypes) + "/" + Mod.Instance.FormatFuel(fuelSource.TotalCapacity*fuelDensity, _massTypes);
            Color progressColor = GetProgressBarColor(percentage,isWasted);
            double fuelConsumption=(currentFuel-previousFuel)/Game.Instance.FlightScene.TimeManager.DeltaTime;
            string fuelConsumptionStr=Mod.Instance.FormatFuel(fuelConsumption*fuelDensity, _massTypes)+"/s";
            
            string timeLeft =isWasted?(fuelConsumption >= 0 ?  $"<color=#E05D6A>{Mod.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity-fuelSource.TotalFuel)/fuelConsumption))}</color>" : $"<color=#81EE80>{Mod.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel/fuelConsumption))}</color>"): (fuelConsumption >= 0 ?  $"<color=#81EE80>{Mod.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity-fuelSource.TotalFuel)/fuelConsumption))}</color>" : $"<color=#E05D6A>{Mod.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel/fuelConsumption))}</color>");
            ;
           
            
            item.GetElementByInternalId("FuelPercentage").SetText("<color=#"+ColorUtility.ToHtmlStringRGB(progressColor)+$">{percentage:P2}</color>");
            item.GetElementByInternalId("FuelConsumption").SetText(fuelConsumptionStr);
            item.GetElementByInternalId("FuelTimeLeft").SetText(timeLeft);
            item.GetElementByInternalId("FuelAmountPercentage").SetText(FuelAmountPercentage);
            XmlElement progressBar = item.GetElementByInternalId("FuelProgressBar");
            progressBar.SetAndApplyAttribute("width", $"{percentage*100}%");
            progressBar.SetAndApplyAttribute("offsetXY", $"{-100 + (100 * (float)percentage)},0");
            progressBar.SetAndApplyAttribute("color", $"#{ColorUtility.ToHtmlStringRGB(progressColor)}");
            if (FuelMap.ContainsKey(fuelTypeId))
            {
                FuelMap[fuelTypeId] = (currentFuel, currentFuel);
            }
           
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

        public void SetMainUIVisibility(bool state)
        {
            mainPanel.SetActive(state && mainPanelVisible);
            controller.xmlLayout.GetElementById("droodism-fuel-item-inspector").SetActive(state && fuelItemInspectorVisible);
        }

        #region transfer相关
        
        //打开设置transferMode的窗口函数
        //你点一下就能开属于是
        public void OnFuelPercentageItemClick(XmlElement item)
        {
            //InitializeFuelTransferMode();
            string fuelTypeId = item.GetAttribute("fuel-type-id"); 
            ShowFuelItemWindow(fuelTypeId);
        }

        private void ShowFuelItemWindow(string fuelTypeId)
        {
            fuelItemInspectorVisible = !fuelItemInspectorVisible;
            XmlElement fuelItemWindow = controller.xmlLayout.GetElementById("droodism-fuel-item-inspector");
            fuelItemWindow.GetElementByInternalId("FuelItemInspectorTitle").SetText(Game.Instance.PropulsionData.GetFuelType(fuelTypeId).Name);
        }
        
        
        
        public void CloseShowFuelItemWindow()
        {
            fuelItemInspectorVisible = false;
        }

        private void OnFuelPercentageModeClick(XmlElement item)
        {
            if ((ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva")))
                return;
            var fuelSouce=getFuelSource(controller.xmlLayout.GetElementById("droodism-fuel-item-inspector").GetElementByInternalId("FuelItemInspectorTitle").GetText());
            string mode = item.GetAttribute("fuel-transfer-mode-id"); 
            switch (mode)
            {
                case "None":
                    fuelSouce.FuelTransferMode = FuelTransferMode.None;
                    break;
                case "Fill":
                    fuelSouce.FuelTransferMode = FuelTransferMode.Fill;
                    break;
                case "Drain":
                    fuelSouce.FuelTransferMode = FuelTransferMode.Drain;
                    break;
                
            }

            IFuelSource getFuelSource(string fuelTypeName)
            {
                var patchScript =  Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();

                switch (fuelTypeName)
                {
                    case "Oxygen":
                        return patchScript.OxygenFuelSource;
                    case "Water":
                        return patchScript.WaterFuelSource;
                    case "Food":
                        return patchScript.FoodFuelSource;
                    case "Carbon Dioxide":
                        return patchScript.CO2FuelSource;
                    case "Wasted Water":
                        return patchScript.WastedWaterFuelSource;
                    case "Solid Waste":
                        return patchScript.SolidWasteFuelSource;
                }
                return null;
            }
        }
#endregion
        private void OnFuelItemInspectorToggle(XmlElement item)
        {
            Debug.LogFormat("LegacyDroodismUI:OnFuelItemInspectorToggle:item:{0}", item);
        }
        #region UI数据更新相关函数
        public void UpdateFuelPercentageItemTemplate()
        {
            if (fuelPercentXMLItems.Count == 0) return;
            for (int i = 0; i < fuelTypeIDList.Count; i++)
            {
                var source = GetIFuelSourceByID(fuelTypeIDList[i]);
                if (source!= null)
                {
                    UpdateFuelTemplateItem(fuelPercentXMLItems[i],source);
                }
            } 
        }

        private IFuelSource GetIFuelSourceByID(string fuelTypeId)
        {
            switch (ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva") ? "Eva" : "Other")
            {
                case "Eva": 
                    foreach (var modifier in ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Modifiers)
                    {
                        if(modifier.GetData().Name.Contains("FuelTank"))
                        {
                            FuelTankScript fts=modifier as FuelTankScript;
                            if (fts.FuelType.Id == fuelTypeId)
                            {
                                return fts;
                            }
                        }
                    }

                    break;
                case "Other":

                    try
                    {
                        var patchScript =  Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();

                        switch (fuelTypeId)
                        {
                            case "Oxygen":
                                return patchScript.OxygenFuelSource;
                            case "H2O":
                                return patchScript.WaterFuelSource;
                            case "Food":
                                return patchScript.FoodFuelSource;
                            case "CO2":
                                return patchScript.CO2FuelSource;
                            case "Wasted Water":
                                return patchScript.WastedWaterFuelSource;
                            case "Solid Waste":
                                return patchScript.SolidWasteFuelSource;
                        }
                        return null;
                    }
                    catch (Exception)
                    {
                        //我知道这里会发鸡巴癫,but lmao i don't give a fuck about it.
                    }
                    break;
                    
                
                    
            }
            return null;

        }

        
        public void UpdateDroodInfo()
        {
            DroodCountTotal = AstronautCount = TouristCount = 0;
            UpdateDroodCount();
            

            void UpdateDroodCount()
            {
                foreach (var pd in ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts)
                {
                    if (pd.PartType.Name=="Eva")
                    {
                        DroodCountTotal++;
                        AstronautCount++;
                      
                    }

                    if (pd.PartType.Name=="Eva-Tourist")
                    {
                        DroodCountTotal++;
                        AstronautCount++;
                    }

                }
            }
        }

        /// <summary>
        /// 暂时没有用,设想了一下做Resource Consumption when unloaded太吃屎了
        /// </summary>
        private void UpdateFuelConsumption()
        {
            oxygenRate=h2oRate=foodRate=co2Rate=wastedWaterRate=solidWasteRate=0;
            foreach (var pd in ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts)
            {
                if (pd.PartType.Name=="Eva"||pd.PartType.Name=="Eva-Tourist")
                {
                    var supportLifeData = pd.PartScript.GetModifier<SupportLifeScript>().Data;
                    oxygenRate -= supportLifeData.OxygenComsumeRate;
                    h2oRate -= supportLifeData.WaterComsumeRate;
                    foodRate -= supportLifeData.FoodComsumeRate;
                    co2Rate += (supportLifeData.OxygenComsumeRate * supportLifeData.evaConsumeEfficiency);
                    wastedWaterRate += (supportLifeData.WaterComsumeRate * supportLifeData.evaConsumeEfficiency);
                    solidWasteRate += (supportLifeData.FoodComsumeRate * supportLifeData.evaConsumeEfficiency);
                }

                if (pd.PartType.Name=="Generator1")
                {
                    if (pd.Activated)
                    {
                        switch (pd.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id)
                        {
                            case "LOX/LH2":
                                var data = pd.PartScript.GetModifier<LifeSupportGeneratorScript>().Data;
                                if (data!= null)
                                {
                                    oxygenRate += data.OxygenConvertEfficiency *
                                                  pd.PartScript.GetModifier<GeneratorScript>().Data.FuelFlow;
                                    h2oRate += data.WaterConvertEfficiency * pd.PartScript.GetModifier<GeneratorScript>().Data.FuelFlow;
                                }
                                break;
                            //TODO:懒得写了,什么时候正儿八经解决这个我再补上
                        }
                    }
                   
                    
                    //TODO 添加Kerolox/Jet 二氧化碳
                    //TODO 添加液氧甲烷 的水和二氧化碳
                }

                if (pd.PartType.Name=="ElectrolyticDevice")
                {
                    if (pd.Activated)
                    {
                        h2oRate -= pd.PartScript.GetModifier<ElectrolyticDeviceScript>().Data.WaterComsuptionRate;
                        oxygenRate += pd.PartScript.GetModifier<ElectrolyticDeviceScript>().Data.OxygenGenerationRate;
                    }
                }

                if (pd.PartType.Name=="SewageTreatDevice")
                {
                    if (pd.Activated)
                    {
                        wastedWaterRate-=pd.PartScript.GetModifier<SewageTreatDeivceScript>().Data.WastedWaterComsumeRate* pd.PartScript.GetModifier<SewageTreatDeivceScript>().Data.Scale;
                        h2oRate += pd.PartScript.GetModifier<SewageTreatDeivceScript>().Data.WastedWaterComsumeRate * pd.PartScript.GetModifier<SewageTreatDeivceScript>().Data.ConvertEffiency * pd.PartScript.GetModifier<SewageTreatDeivceScript>().Data.Scale;
                    }
                }

                if (pd.PartType.Name == "PhotoBioReactor")
                {
                    
                    var data = pd.PartScript.GetModifier<PhotoBioReactorScript>().Data;
                    if (pd.PartScript.GetModifier<PhotoBioReactorScript>().usingArtificialLight) 
                    {
                        if (pd.Activated)
                        {
                            co2Rate -= data.Co2ConsumptionRate;
                            wastedWaterRate=+data.WaterConsumptionRate;
                            h2oRate -= data.WaterConsumptionRate;
                            solidWasteRate-=data.SolidWasteConsumptionRate;
                            oxygenRate+=data.OxygenGenerationRate;
                            photoBioReactorGrowRate =pd.PartScript.GetModifier<PhotoBioReactorScript>()._rechargePointingEfficiency*data.GrowSpeed * data.Efficiency * (GetIFuelSourceByID("Solid Waste").IsEmpty ? 1 : data.BoosteScale);
                        }
                        else
                        {
                            co2Rate -= data.Co2ConsumptionRate;
                            wastedWaterRate=+data.WaterConsumptionRate;
                            h2oRate -= data.WaterConsumptionRate;
                            solidWasteRate-=data.SolidWasteConsumptionRate;
                            oxygenRate+=data.OxygenGenerationRate;
                            photoBioReactorGrowRate =data.GrowSpeed * data.Efficiency * (GetIFuelSourceByID("Solid Waste").IsEmpty ? 1 : data.BoosteScale);
                        }
                    }
                    else
                    {
                        if (pd.Activated)
                        {
                            co2Rate -= data.Co2ConsumptionRate;
                            wastedWaterRate=+data.WaterConsumptionRate;
                            h2oRate -= data.WaterConsumptionRate;
                            solidWasteRate-=data.SolidWasteConsumptionRate;
                            oxygenRate+=data.OxygenGenerationRate;
                            photoBioReactorGrowRate =data.GrowSpeed * data.Efficiency * (GetIFuelSourceByID("Solid Waste").IsEmpty ? 1 : data.BoosteScale);
                        }
                    }
                }
            }
        }
        #endregion
       
    }
}