using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Flight.UI;
using ModApi.Ui;
using UnityEngine;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Flight.GameView;
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
        
        [FormerlySerializedAs("DroodScripts")] public List<EvaScript> DroodScriptsList = new List<EvaScript>();

        private Dictionary<string, (double Current, double Previous)> FuelMap = new Dictionary<string, (double, double)>
        {
            { "Oxygen", (0, 0) },
            { "H2O", (0, 0) },
            {"Food", (0, 0) },
            {"CO2", (0, 0) },
            {"Wasted Water", (0, 0) },
            {"Solid Waste", (0, 0) }
        };
        private List<String> fuelTypeIDList = new List<string>() { "Oxygen", "H2O", "Food", "CO2", "Wasted Water", "Solid Waste" };
        private struct FuelUIData
        {
            public string FuelAmountPercentageStr;
            public string FuelConsumptionStr; 
            public string TimeLeft;
            public float FuelPercentage;
        }

        private Dictionary<string, FuelUIData> FuelUIDataMap = new Dictionary<string, FuelUIData>
        {
            { "Oxygen", new FuelUIData() },
            { "H2O", new FuelUIData() },
            { "Food", new FuelUIData() },
            { "CO2", new FuelUIData() },
            { "Wasted Water", new FuelUIData() },
            { "Solid Waste", new FuelUIData() }
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
            

            if (!ModSettings.Instance.UseLegacyUI)
            {
                for (int i = 0; i < fuelTypeIDList.Count; i++)
                {
                    var source = GetIFuelSourceByID(fuelTypeIDList[i]);
                    if (source!= null)
                    {
                        UpdateFuelTemplateItem(source);
                    }
                } 
            }
            
           
            
        }
        private void UpdateFuelTemplateItem(IFuelSource fuelSource)
        {
            if (Game.Instance.FlightScene.TimeManager.Paused)
            {
                return;
            }
            double currentFuel, previousFuel;
            string fuelTypeId = fuelSource.FuelType.Id;
            float fuelDensity = fuelSource.FuelType.Density;
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
            double fuelPercentage = (currentFuel / fuelSource.TotalCapacity);
            bool isWasted = (fuelTypeId.Contains("Waste") || fuelTypeId.Contains("CO2"));
            string FuelAmountPercentagestr = Mod.Instance.FormatFuel(fuelSource.TotalFuel * fuelDensity, _massTypes) + "/" + Mod.Instance.FormatFuel(fuelSource.TotalCapacity * fuelDensity, _massTypes);
            double fuelConsumption = (currentFuel - previousFuel) / Game.Instance.FlightScene.TimeManager.DeltaTime;
            string fuelConsumptionStr = Mod.Instance.FormatFuel(fuelConsumption * fuelDensity, _massTypes) + "/s";

            string timeLeft = isWasted
                ? (fuelConsumption >= 0
                    ? $"<color=#E05D6A>{Mod.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity - fuelSource.TotalFuel) / fuelConsumption))}</color>"
                    : $"<color=#81EE80>{Mod.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel / fuelConsumption))}</color>")
                : (fuelConsumption >= 0
                    ? $"<color=#81EE80>{Mod.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity - fuelSource.TotalFuel) / fuelConsumption))}</color>"
                    : $"<color=#E05D6A>{Mod.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel / fuelConsumption))}</color>");
            ;
            string color = isWasted
                ? fuelConsumption > 0 ? "E05D6A" : fuelConsumption < 0 ? "81EE80" : "FF9900"
                : fuelConsumption > 0 ? "81EE80" : fuelConsumption < 0 ? "E05D6A" : "FF9900";
            //在这里完成数据外传update
            FuelUIDataMap[fuelTypeId] = new FuelUIData
            {
                FuelAmountPercentageStr =FuelAmountPercentagestr, 
                FuelConsumptionStr = "<color=#"+color+$">{fuelConsumptionStr}</color>",
                TimeLeft = timeLeft,
                FuelPercentage = (float)fuelPercentage,
            };
            if (FuelMap.ContainsKey(fuelTypeId))
            {
                FuelMap[fuelTypeId] = (currentFuel, currentFuel);
            }

        }
        private void UpdateDroodismUIPanel() 
        {
            if (newDroodismUIIntance == null) 
            {
                return;
            }

            if (ModSettings.Instance.UseLegacyUI)
            {
                newDroodismUIIntance.SetMainUIVisibility(Game.Instance.FlightScene.FlightSceneUI.Visible);
                if (newDroodismUIIntance.mainPanelVisible)
                {
                    newDroodismUIIntance.UpdateFuelPercentageItemTemplate();
                }
            }

            if (!ModSettings.Instance.UseLegacyUI)
            {
                newDroodismUIIntance.SetMainUIVisibility(false);
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
        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                
                newDroodismUIIntance = Game.Instance.UserInterface.BuildUserInterfaceFromResource<NewDroodismUI>(
                    "Droodism/Flight/DroodismInspectPanel",
                    (script, controller) => script.OnLayoutRebuilt(controller));
                UpdateInfo();
                CreateInspectorPanel();
                inspectorPanel.Visible = false;
                inspectorPanel.CloseButtonClicked+=OnCloseButtonClicked;
                ModApi.Common.Game.Instance.FlightScene.CraftChanged+=OnCraftChanged;
                ModApi.Common.Game.Instance.FlightScene.CraftStructureChanged += OnCraftStructureChanged;
                ModApi.Common.Game.Instance.FlightScene.Initialized+=OnSceneInitialized;
                ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPodChanged+=OnActiveCommandPodChanged;
                Game.Instance.FlightScene.FlightEnded+=FlightSceneEnded;

            }
        }
        private void FlightSceneEnded(object sender, FlightEndedEventArgs e)
        {
            Game.Instance.FlightScene.CraftStructureChanged -= OnCraftStructureChanged;
            Game.Instance.FlightScene.CraftChanged -= OnCraftChanged;
            Game.Instance.FlightScene.Initialized -= OnSceneInitialized;
            Game.Instance.FlightScene.FlightEnded -= FlightSceneEnded;
            ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPodChanged-=OnActiveCommandPodChanged;
        }

        private void OnCloseButtonClicked(IInspectorPanel inspectorPanel)
        {
            inspectorPanel.Visible = false;
        }

        private void OnActiveCommandPodChanged(ICraftScript a, ICommandPod b, ICommandPod c)
        {
            
        }
        private void OnCraftChanged(ICraftNode craftNode)
        {
            craftNode.CraftNodeMerged+=OnCraftMerged;
            craftNode.CraftScript.RootPart.MovedToNewCraft+=MovedToNewCraft;
            UpdateInfo();
        }

        private void OnSceneInitialized(IFlightScene flightScene)
        {
            UpdateInfo();
        }

        private void OnCraftStructureChanged()
        {
           UpdateInfo();
        }

        private void MovedToNewCraft(ICraftScript craftNodeA, ICraftScript craftNodeB)
        {
            UpdateInfo();
        }
        private void OnCraftMerged(ICraftNode craftNodeA, ICraftNode craftNodeB)
        {
            UpdateInfo();
            log("OnCraftMerged");
        }
        #endregion
        public void OnToggleDroodismInspectorPanelState()
        { 
           
            if (ModSettings.Instance.UseLegacyUI)
            {
                newDroodismUIIntance.OnTogglePanelState();
            }

            if (!ModSettings.Instance.UseLegacyUI)
            {
                UpdateInfo(); 
                try
                {
                    inspectorPanel.Visible = !inspectorPanel.Visible;
                }
                catch (Exception)
                {
                
                    CreateInspectorPanel();
                    inspectorPanel.Visible = !inspectorPanel.Visible;
                }
            }
        }
        

        private bool areButtonsVisible = false;
        private Dictionary<string, IconButtonRowModel> FuelButtonRows = new Dictionary<string, IconButtonRowModel>();
        public void CreateInspectorPanel()
        {
            // 清空 FuelButtonRows 以避免重复添加
            FuelButtonRows.Clear();
        
            // 大家好啊,我是分割线
            inspectorModel = new InspectorModel("Droodism Resources Inspector", "<color=green>Life Support Inspector");
        
            inspectorModel.Add(new TextModel("Crew Count", () => DroodCountTotal.ToString()));
            inspectorModel.Add(new TextModel("Astronaut Count", () => AstronautCount.ToString()));
            inspectorModel.Add(new TextModel("Tourist Count", () => TouristCount.ToString()));

            #region FuelSourceManagerGroup
            
            GroupModel FuelSourceManagerGroup = new GroupModel("Resources Inspector");
        
            // 大家好啊,我是分割线
            foreach (var fuelTypeId in fuelTypeIDList)
            {
                addFuelTypeTemplateItem(fuelTypeId);
            }
        
            FuelSourceManagerGroup.Add(new TextButtonModel("Resources Fill,Waste Drain", b => setAllReciveMode()));
            FuelSourceManagerGroup.Add(new TextButtonModel("Resources Drain,Waste Fill", b => setAllSendMode()));
            FuelSourceManagerGroup.Add(new TextButtonModel("Reset All Transfer Mode", b => resetAllReciveMode()));
            FuelSourceManagerGroup.Add(new TextButtonModel("Toggle Single Type Transfer Mode", b => questionMark()));
            FuelSourceManagerGroup.Add(new TextModel("", () => ""));
        
            
            
            inspectorModel.AddGroup(FuelSourceManagerGroup);
           #endregion
            GroupModel CrewInspectorGroup  = new GroupModel("Crew Inspector");
            foreach (EvaScript eva in DroodScriptsList)
            {
                SupportLifeScript supportLifeScript = eva.PartScript?.GetModifier<SupportLifeScript>();
                if (supportLifeScript!= null)
                {
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Crew Name", () => eva.Data.CrewName));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Mission Time", (Func<string>) (() => Mod.GetStopwatchTimeString(supportLifeScript.MissionDurationTime)), tooltip:  eva.Data.CrewName+";s mission time since launch."));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("", () => ""));
                }
            }
            
            
            inspectorModel.AddGroup(CrewInspectorGroup);
            inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, new InspectorPanelCreationInfo()
            {
                PanelWidth = 400,
                Resizable = true,
            });
        
            void addFuelTypeTemplateItem(string fuelTypeId)
            {
                IFuelSource fuelSource = GetIFuelSourceByID(fuelTypeId);
                bool isWasted = fuelTypeId.Contains("Wasted") || fuelTypeId == "CO2";
        
                FuelSourceManagerGroup.Add(new TextModel("", () => ""));
                FuelSourceManagerGroup.Add(new TextModel("", () => ""));
                // 添加燃料名称和数据（燃料量、消耗率、剩余时间）
                FuelSourceManagerGroup.Add(new TextModel(
                    ModApi.Common.Game.Instance.PropulsionData.GetFuelType(fuelTypeId).Name,
                    () => FuelUIDataMap.ContainsKey(fuelTypeId)
                        ? $"{FuelUIDataMap[fuelTypeId].TimeLeft}"
                        : "empty"));
        
                // 添加进度条
                FuelSourceManagerGroup.Add(new ProgressBarModel(
                    () => $"{FuelUIDataMap[fuelTypeId].FuelAmountPercentageStr}",
                    () => FuelUIDataMap.ContainsKey(fuelTypeId) ? FuelUIDataMap[fuelTypeId].FuelPercentage : 0f));
                FuelSourceManagerGroup.Add(new TextModel("", () => FuelUIDataMap[fuelTypeId].FuelConsumptionStr));
        
                // FuelTransferMode 设置按钮
                IconButtonRowModel iconButtonRowModel = new IconButtonRowModel();
                IconButtonModel fuelTransferButtonNone = new IconButtonModel(
                    "Ui/Sprites/Flight/IconFuelTransferNone",
                    (Action<IconButtonModel>)(x => SetFuelTransferMode(FuelTransferMode.None, fuelSource.FuelType.Id)),
                    "Disable fuel transfer.");
                IconButtonModel fuelTransferButtonFill = new IconButtonModel(
                    "Ui/Sprites/Flight/IconFuelTransferFill",
                    (Action<IconButtonModel>)(x => SetFuelTransferMode(FuelTransferMode.Fill, fuelSource.FuelType.Id)),
                    "Fills the tank during fuel transfer. Requires at least one other tank to be set to Drain.");
                IconButtonModel fuelTransferButtonDrain = new IconButtonModel(
                    "Ui/Sprites/Flight/IconFuelTransferDrain",
                    (Action<IconButtonModel>)(x => SetFuelTransferMode(FuelTransferMode.Drain, fuelSource.FuelType.Id)),
                    "Drains this tank during fuel transfer. Requires at least one other tank to be set to Fill.");
                iconButtonRowModel.Add(fuelTransferButtonFill);
                iconButtonRowModel.Add(fuelTransferButtonNone);
                iconButtonRowModel.Add(fuelTransferButtonDrain);
        
                iconButtonRowModel.UpdateAction = (Action<ItemModel>)(m =>
                {
                    FuelTransferMode fuelTransferMode = fuelSource.FuelTransferMode;
                    fuelTransferButtonNone.Style = fuelTransferMode == FuelTransferMode.None
                        ? ButtonModel.ButtonStyle.Primary
                        : ButtonModel.ButtonStyle.Default;
                    fuelTransferButtonFill.Style = fuelTransferMode == FuelTransferMode.Fill
                        ? ButtonModel.ButtonStyle.Primary
                        : ButtonModel.ButtonStyle.Default;
                    fuelTransferButtonDrain.Style = fuelTransferMode == FuelTransferMode.Drain
                        ? ButtonModel.ButtonStyle.Warning
                        : ButtonModel.ButtonStyle.Default;
                });
        
                // 保存 IconButtonRowModel 到字典
                FuelButtonRows[fuelTypeId] = iconButtonRowModel;
        
                FuelSourceManagerGroup.Add<IconButtonRowModel>(iconButtonRowModel);
                iconButtonRowModel.Visible = areButtonsVisible; // 设置可见性
            }

            #region FuelTransferMode 相关
            void SetFuelTransferMode(FuelTransferMode fuelTransferMode, string fuelTypeId)
            {
                if (ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva"))
                {
                    ModApi.Common.Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Cannot set fuel transfer mode to a single Drood.");
                    return;
                }
                GetIFuelSourceByID(fuelTypeId).FuelTransferMode = fuelTransferMode;
            }
        
            void setAllReciveMode()
            {
                SetFuelTransferMode(FuelTransferMode.Fill, "Oxygen");
                SetFuelTransferMode(FuelTransferMode.Fill, "H2O");
                SetFuelTransferMode(FuelTransferMode.Fill, "Food");
                SetFuelTransferMode(FuelTransferMode.Drain, "CO2");
                SetFuelTransferMode(FuelTransferMode.Drain, "Wasted Water");
                SetFuelTransferMode(FuelTransferMode.Drain, "Solid Waste");
            }
            void setAllSendMode()
            {
                SetFuelTransferMode(FuelTransferMode.Drain, "Oxygen");
                SetFuelTransferMode(FuelTransferMode.Drain, "H2O");
                SetFuelTransferMode(FuelTransferMode.Drain, "Food");
                SetFuelTransferMode(FuelTransferMode.Fill, "CO2");
                SetFuelTransferMode(FuelTransferMode.Fill, "Wasted Water");
                SetFuelTransferMode(FuelTransferMode.Fill, "Solid Waste");
            }
        
            void resetAllReciveMode()
            {
                SetFuelTransferMode(FuelTransferMode.None, "Oxygen");
                SetFuelTransferMode(FuelTransferMode.None, "H2O");
                SetFuelTransferMode(FuelTransferMode.None, "Food");
                SetFuelTransferMode(FuelTransferMode.None, "CO2");
                SetFuelTransferMode(FuelTransferMode.None, "Wasted Water");
                SetFuelTransferMode(FuelTransferMode.None, "Solid Waste");
            }
        
            void questionMark()
            {
                areButtonsVisible = !areButtonsVisible; // 切换可见状态
                foreach (var buttonRow in FuelButtonRows.Values)
                {
                    buttonRow.Visible = areButtonsVisible; // 设置可见性
                }
            }
            
            #endregion
        }

        



        #region 数据更新处理

        private void UpdateInfo()
        {
            DroodScriptsList.Clear();
            DroodCountTotal = AstronautCount = TouristCount = 0;
            UpdateDroodCount();
            void UpdateDroodCount()
            {
                foreach (var pd in ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts)
                {
                    if (pd.PartType.Name=="Eva")
                    {
                        DroodScriptsList.Add(pd.PartScript.GetModifier<EvaScript>());
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

            
        }
        

        #endregion

        
        private IFuelSource GetIFuelSourceByID(string fuelTypeId)
        {
            switch (ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva")&&ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count==1 ? "Eva" : "Other")
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
