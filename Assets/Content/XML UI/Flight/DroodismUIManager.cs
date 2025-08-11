using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Flight.UI;
using ModApi.Ui;
using UnityEngine;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Flight;
using ModApi.Math;
using ModApi.Scenes.Events;
using ModApi.Ui.Inspector;
using Rewired;
using UnityEngine.Serialization;
using Game = ModApi.Common.Game;

namespace Assets.Scripts
{
    public class DroodismUIManager : MonoBehaviour
    {

        public const string droodismBottomId = "toggle-droodism-ui-bottom";
        public NewDroodismUI newDroodismUIIntance;
        public static DroodismUIManager Instance;
        
        private IInspectorPanel inspectorPanel;
        private InspectorModel inspectorModel;
        public int DroodCountTotal,AstronautCount,TouristCount;
        public readonly string[] _massTypes = { "g", "kg", "t", "kt" };

        private Dictionary<string, (double Current, double Previous)> FuelMap = new Dictionary<string, (double, double)>
        {
            { "Oxygen", (0, 0) },
            { "H2O", (0, 0) },
            {"Food", (0, 0) },
            {"CO2", (0, 0) },
            {"Wasted Water", (0, 0) },
            {"Solid Waste", (0, 0) }
        };
        private void Awake()
        {
            Instance = this;
        }
        private void Start() 
        {
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.NavPanel, OnBuildFlightUI);

        }

        private void Update()
        {   
            if (!Game.Instance.SceneManager.InFlightScene) 
            {
                return;
            }
            UpdateDroodismUIPanel();
        }
        private void UpdateDroodismUIPanel() 
        {
            if (newDroodismUIIntance == null) 
            {
                return;
            }
            newDroodismUIIntance.SetMainUIVisibility(Game.Instance.FlightScene.FlightSceneUI.Visible);
            if (newDroodismUIIntance.mainPanelVisible)
            {
                newDroodismUIIntance.UpdateFuelPercentageItemTemplate();
            }
        }
        
        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                
                newDroodismUIIntance = Game.Instance.UserInterface.BuildUserInterfaceFromResource<NewDroodismUI>(
                    "Droodism/Flight/DroodismInspectPanel",
                    (script, controller) => script.OnLayoutRebuilt(controller));
                UpdateDroodCount();
                ModApi.Common.Game.Instance.FlightScene.CraftChanged+=OnCraftChanged;
                ModApi.Common.Game.Instance.FlightScene.CraftStructureChanged += OnCraftStructureChanged;
                ModApi.Common.Game.Instance.FlightScene.Initialized+=OnSceneInitialized;

            }
        }

        private static void OnBuildFlightUI(BuildUserInterfaceXmlRequest request)
        {
            var ns = XmlLayoutConstants.XmlNamespace;
            var inspectButton = request.XmlDocument
                .Descendants(ns + "ContentButton")
                .First(x => (string)x.Attribute("id") == "toggle-flight-inspector");
            inspectButton.Parent.Add(
                new XElement(
                    ns + "ContentButton",
                    new XAttribute("id", droodismBottomId),
                    new XAttribute("class", "panel-button audio-btn-click"),
                    new XAttribute("tooltip", "Toggle Droodism UI."),
                    new XAttribute("name", "NavPanel.ToggleDroodismInspector"),
                    new XElement(
                        ns + "Image",
                        new XAttribute("class", "panel-button-icon"),
                        new XAttribute("sprite", "Droodism/Sprites/DroodsimUIIcon"))));
        }

        #region 事件更新
        private void OnCraftChanged(ICraftNode craftNode)
        {
            craftNode.CraftNodeMerged+=OnCraftMerged;
            craftNode.CraftScript.RootPart.MovedToNewCraft+=MovedToNewCraft;
            UpdateDroodCount();
            log("OnCraftChanged");
        }

        private void OnSceneInitialized(IFlightScene flightScene)
        {
            UpdateDroodCount();
        }

        private void OnCraftStructureChanged()
        {
           UpdateDroodCount();
           log("OnCraftStructureChanged");
        }

        private void MovedToNewCraft(ICraftScript craftNodeA, ICraftScript craftNodeB)
        {
            UpdateDroodCount();
            log("MovedToNewCraft");

        }
        private void OnCraftMerged(ICraftNode craftNodeA, ICraftNode craftNodeB)
        {
            UpdateDroodCount();
            log("OnCraftMerged");
        }
        #endregion
        public void OnToggleDroodismInspectorPanelState()
        { 
            newDroodismUIIntance.OnTogglePanelState();
            
            if (inspectorPanel==null)
            {
                UpdateDroodCount(); 
                CreateInspectorPanel();
            }
            if (inspectorPanel != null)
            {
                try
                {
                    inspectorPanel.Visible = !inspectorPanel.Visible;
                }
                catch (Exception)
                {
                    UpdateDroodCount(); 
                    CreateInspectorPanel();
                }
                
            }
        }
        

        public void CreateInspectorPanel()
        {
            log("傻逼");
            //大家好啊,我是分割线
            inspectorModel=new InspectorModel("Droodism Resources Inspector","<color=green>Life Support Resources Inspector");
            //大家好啊,我是分割线
            inspectorModel.Add(new TextModel("Crew Count", ()=>DroodCountTotal.ToString()));
            inspectorModel.Add(new TextModel("Astronaut Count", ()=>AstronautCount.ToString()));
            inspectorModel.Add(new TextModel("Tourist Count", ()=>TouristCount.ToString()));
            //大家好啊,我是分割线
            inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, new InspectorPanelCreationInfo()
            {
                PanelWidth = 400,
                Resizable = true,
            });
            
            void fuckU(string fuelTypeId)
            {
                bool isWasted = fuelTypeId.Contains("Wasted") || fuelTypeId == "CO2";
                IFuelSource fuelSource = GetIFuelSourceByID(fuelTypeId);
                inspectorModel.Add(new TextModel(ModApi.Common.Game.Instance.PropulsionData.GetFuelType(fuelTypeId).Name, ()=>DroodCountTotal.ToString()));
                
                
            }

        }

        #region 数据更新处理

        private void UpdateDroodCount()
        {
            DroodCountTotal = AstronautCount = TouristCount = 0;
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
                    TouristCount++;
                }

            }
        }

        #endregion

        private void UpdateFuelTemplateItem(IFuelSource fuelSource)
        {
            if (Game.Instance.FlightScene.TimeManager.Paused)
            {
                return;
            }

            double currentFuel, previousFuel;
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

            bool isWasted = (fuelTypeId.Contains("Waste") || fuelTypeId.Contains("CO2"));
            string FuelAmountPercentage = Mod.Inctance.FormatFuel(fuelSource.TotalFuel * fuelDensity, _massTypes) + "/" + Mod.Inctance.FormatFuel(fuelSource.TotalCapacity * fuelDensity, _massTypes);
            Color progressColor = GetProgressBarColor(percentage, isWasted);
            double fuelConsumption = (currentFuel - previousFuel) / Game.Instance.FlightScene.TimeManager.DeltaTime;
            string fuelConsumptionStr = Mod.Inctance.FormatFuel(fuelConsumption * fuelDensity, _massTypes) + "/s";

            string timeLeft = isWasted
                ? (fuelConsumption >= 0
                    ? $"<color=#E05D6A>{Mod.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity - fuelSource.TotalFuel) / fuelConsumption))}</color>"
                    : $"<color=#81EE80>{Mod.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel / fuelConsumption))}</color>")
                : (fuelConsumption >= 0
                    ? $"<color=#81EE80>{Mod.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity - fuelSource.TotalFuel) / fuelConsumption))}</color>"
                    : $"<color=#E05D6A>{Mod.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel / fuelConsumption))}</color>");
            ;
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
        private static void log(string message)
        {
            Debug.LogFormat("DroodismUIManager:"+message);
        }

    }
    
}
