using System;
using UI.Xml;
using ModApi.Ui;
using UnityEngine;
using ModApi.Math;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Flight;
using ModApi.Mods;
using ModApi.Audio;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Craft.Propulsion;
using ModApi.State;
using UnityEngine.Serialization;

namespace Assets.Scripts
{
    public class NewDroodismUI:MonoBehaviour
    {
        private XmlLayoutController controller;
        private XmlElement mainPanel;
        private XmlElement FuelPercentageItemTemplate;
        private XmlElement fuelPercentageList;
        private List<XmlElement> fuelXMLItems = new List<XmlElement>();
        
        public bool mainPanelVisible = false;
        public bool fuelItemInspectorVisible = false;
        private bool _notifPanelVisible = false;
        
        public readonly string[] _massTypes = { "g", "kg", "t", "kt" };
        public List<Vector3> DroodPosistion = new List<Vector3>();
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
            string FuelAmountPercentage=Mod.Inctance.FormatFuel(fuelSource.TotalFuel*fuelDensity, _massTypes) + "/" + Mod.Inctance.FormatFuel(fuelSource.TotalCapacity*fuelDensity, _massTypes);
            Color progressColor = GetProgressBarColor(percentage,isWasted);
            double fuelConsumption=(currentFuel-previousFuel)/Game.Instance.FlightScene.TimeManager.DeltaTime;
            string fuelConsumptionStr=Mod.Inctance.FormatFuel(fuelConsumption*fuelDensity, _massTypes)+"/s";
            
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

        public void OnFuelPercentageItemClick(XmlElement item)
        {
            string fuelTypeId = item.GetAttribute("fuel-type-id"); 
            Debug.LogFormat("NewDroodismUI:OnFuelPercentageItemClick:燃料名称{0}", Game.Instance.PropulsionData.GetFuelType(fuelTypeId).Name);
            ShowFuelItemWindow(fuelTypeId);
            Mod.Inctance.SpawnFlag();
            
        }

        private void ShowFuelItemWindow(string fuelTypeId)
        {
            fuelItemInspectorVisible = true;
            FuelType currentFuelType=Game.Instance.PropulsionData.GetFuelType(fuelTypeId);
            XmlElement fuelItemWindow = controller.xmlLayout.GetElementById("droodism-fuel-item-inspector");
            fuelItemWindow.GetElementByInternalId("FuelItemInspectorTitle").SetText(Game.Instance.PropulsionData.GetFuelType(fuelTypeId).Name+" Inspcector Window");
            
        }
        public void CloseShowFuelItemWindow()
        {
            fuelItemInspectorVisible = false;
        }

        private void OnFuelItemInspectorToggle(XmlElement item)
        {
            Debug.LogFormat("NewDroodismUI:OnFuelItemInspectorToggle:item:{0}", item);
        }
        #region UI数据更新相关函数
        public void UpdateFuelPercentageItemTemplate()
        {
            if (fuelXMLItems.Count == 0) return;
            for (int i = 0; i < fuelTypeIDList.Count; i++)
            {
                var source = UpdateCraftFuelParameterValue(fuelTypeIDList[i]);
                if (source!= null)
                {
                    UpdateFuelTemplateItem(fuelXMLItems[i],source);
                }
            } 
        }

        private IFuelSource UpdateCraftFuelParameterValue(string fuelTypeId)
        {
            switch ((ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count == 1 &&
                     ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name
                         .Contains("Eva"))
                        ? "Eva"
                        : "Other")
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
                    FuelSourceGroup fuelSourceGroup = new FuelSourceGroup(1,1,ModApi.Common.Game.Instance.PropulsionData.GetFuelType(fuelTypeId));
                    foreach (var source in ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.FuelSources.FuelSources)
                    {
                        if (source.FuelType.Id == fuelTypeId&&source.IsDestroyed==false&&DroodPosistion.Contains(source.Position)==false)
                        {
                            fuelSourceGroup.AddFuelSource(source);
                        }
                    }

                    return fuelSourceGroup;
                    
            }
            return null;

        }

        public void UpdateDroodInfo()
        {
            DroodPosistion.Clear();
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
                        DroodPosistion.Add(pd.Position);
                    }

                    if (pd.PartType.Name=="Eva-Tourist")
                    {
                        DroodCountTotal++;
                        AstronautCount++;
                        DroodPosistion.Add(pd.Position);
                    }

                }
            }
        }

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
                    if (pd.PartScript.GetModifier<GeneratorScript>().Data.FuelType.Id=="LOX/LH2"||pd.Activated)
                    {
                        var data = pd.PartScript.GetModifier<LifeSupportGeneratorScript>().Data;
                        if (data!= null)
                        {
                            oxygenRate += data.OxygenConvertEfficiency *
                                          pd.PartScript.GetModifier<GeneratorScript>().Data.FuelFlow;
                            h2oRate += data.WaterConvertEfficiency * pd.PartScript.GetModifier<GeneratorScript>().Data.FuelFlow;
                        }
                        
                    }
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
                    if (data.UseEletricityWhenFold == true) 
                    {
                        if (pd.Activated)
                        {
                            co2Rate -= data.Co2ConsumptionRate;
                            wastedWaterRate=+data.WaterConsumptionRate;
                            h2oRate -= data.WaterConsumptionRate;
                            solidWasteRate-=data.SolidWasteConsumptionRate;
                            oxygenRate+=data.OxygenGenerationRate;
                            photoBioReactorGrowRate =pd.PartScript.GetModifier<PhotoBioReactorScript>()._rechargePointingEfficiency*data.GrowSpeed * data.Efficiency * (UpdateCraftFuelParameterValue("Solid Waste").IsEmpty ? 1 : data.BoosteScale);
                        }
                        else
                        {
                            co2Rate -= data.Co2ConsumptionRate;
                            wastedWaterRate=+data.WaterConsumptionRate;
                            h2oRate -= data.WaterConsumptionRate;
                            solidWasteRate-=data.SolidWasteConsumptionRate;
                            oxygenRate+=data.OxygenGenerationRate;
                            photoBioReactorGrowRate =data.GrowSpeed * data.Efficiency * (UpdateCraftFuelParameterValue("Solid Waste").IsEmpty ? 1 : data.BoosteScale);
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
                            photoBioReactorGrowRate =data.GrowSpeed * data.Efficiency * (UpdateCraftFuelParameterValue("Solid Waste").IsEmpty ? 1 : data.BoosteScale);
                        }
                    }
                }
            }
        }
        #endregion
       
    }
}