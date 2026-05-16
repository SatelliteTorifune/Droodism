using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Ui;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Math;
using ModApi.Scenes.Events;
using ModApi.Ui.Inspector;
using UnityEngine.Serialization;

namespace Assets.Scripts.Droodism.UserInterface
{
    public class DroodismUIManager : MonoBehaviourBase

    {

        public const string droodismBottomId = "toggle-droodism-ui-bottom";
        public static DroodismUIManager Instance;

        private IInspectorPanel inspectorPanel;
        private InspectorModel inspectorModel;
        public int DroodCountTotal, AstronautCount, TouristCount;
        private readonly string[] _massTypes = { "g", "kg", "t", "kt" };

        private GroupModel CraftFuelSourceInspectorModel;
        

        [FormerlySerializedAs("DroodScripts")] public List<EvaScript> DroodScriptsList = new List<EvaScript>();

        private Dictionary<string, (double Current, double Previous)> FuelMap = new Dictionary<string, (double, double)>
        {
            { "Oxygen", (0, 0) },
            { "H2O", (0, 0) },
            { "Food", (0, 0) },
            { "CO2", (0, 0) },
            { "Wasted Water", (0, 0) },
            { "Solid Waste", (0, 0) }
        };

        private List<string> fuelTypeIDList = new List<string>
            { "Oxygen", "H2O", "Food", "CO2", "Wasted Water", "Solid Waste" };

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
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.NavPanel,
                OnBuildFlightUI);

        }

        void Update()
        {
            if (!Game.InFlightScene)
            {
                return;
            }

            if (CraftFuelSourceInspectorModel != null&&this.inspectorPanel.Visible)
            {
                CraftFuelSourceInspectorModel.Visible = Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.Part.GetModifier<EvaData>()==null&&Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count>1;
            }

            foreach (var id in fuelTypeIDList)
            {
                var source = GetIFuelSourceByID(id);
                if (source != null)
                {
                    UpdateFuelTemplateItem(source);
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
            string FuelAmountPercentagestr = Mod.Instance.FormatFuel(fuelSource.TotalFuel * fuelDensity, _massTypes) +
                                             "/" + Mod.Instance.FormatFuel(fuelSource.TotalCapacity * fuelDensity,
                                                 _massTypes);
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
                : fuelConsumption > 0
                    ? "81EE80"
                    : fuelConsumption < 0
                        ? "E05D6A"
                        : "FF9900";
            //在这里完成数据外传update
            FuelUIDataMap[fuelTypeId] = new FuelUIData
            {
                FuelAmountPercentageStr = FuelAmountPercentagestr,
                FuelConsumptionStr = "<color=#" + color + $">{fuelConsumptionStr}</color>",
                TimeLeft = timeLeft,
                FuelPercentage = (float)fuelPercentage,
            };
            if (FuelMap.ContainsKey(fuelTypeId))
            {
                FuelMap[fuelTypeId] = (currentFuel, currentFuel);
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

                UpdateInfo();
                CreateInspectorPanel();
                inspectorPanel.Visible = false;
                inspectorPanel.CloseButtonClicked += OnCloseButtonClicked;
                ModApi.Common.Game.Instance.FlightScene.CraftChanged += OnCraftChanged;
                ModApi.Common.Game.Instance.FlightScene.CraftStructureChanged += OnCraftStructureChanged;
                ModApi.Common.Game.Instance.FlightScene.Initialized += OnSceneInitialized;
                ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPodChanged +=
                    OnActiveCommandPodChanged;
                Game.Instance.FlightScene.FlightEnded += FlightSceneEnded;

            }
        }

        private void FlightSceneEnded(object sender, FlightEndedEventArgs e)
        {
            Game.Instance.FlightScene.CraftStructureChanged -= OnCraftStructureChanged;
            Game.Instance.FlightScene.CraftChanged -= OnCraftChanged;
            Game.Instance.FlightScene.Initialized -= OnSceneInitialized;
            Game.Instance.FlightScene.FlightEnded -= FlightSceneEnded;
            ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPodChanged -=
                OnActiveCommandPodChanged;
        }

        private void OnCloseButtonClicked(IInspectorPanel inspectorPanel)
        {
            inspectorPanel.Visible = false;
        }

        private void OnActiveCommandPodChanged(ICraftScript source, ICommandPod oldPod, ICommandPod newPod)
        {
           OnCraftStructureChanged();
        }

        private void OnCraftChanged(ICraftNode craftNode)
        {
            
            craftNode.CraftNodeMerged += OnCraftMerged;
            craftNode.CraftScript.RootPart.MovedToNewCraft += MovedToNewCraft;
            UpdateInfo();
            OnCraftStructureChanged();
        }

        private void OnSceneInitialized(IFlightScene flightScene)
        {
            UpdateInfo();
        }

        private void OnCraftStructureChanged()
        {
            UpdateInfo();
            var craftScript = Game.Instance.FlightScene.CraftNode.CraftScript;
            //CraftFuelSources craftFuelSource = craftScript.FuelSources as CraftFuelSources;
            //craftFuelSource?.Rebuild(craftScript);
            foreach (var pd in craftScript.Data.Assembly.Parts)
            {
                if (pd.GetModifier<SupportLifeData>()!=null)
                {
                    pd.GetModifier<SupportLifeData>()?.Script.Refresh();
                }
               
            }
        }

        private void MovedToNewCraft(ICraftScript craftNodeA, ICraftScript craftNodeB)
        {
            UpdateInfo();
        }

        private void OnCraftMerged(ICraftNode craftNodeA, ICraftNode craftNodeB)
        {
            UpdateInfo();
        }

        #endregion

        public void OnToggleDroodismInspectorPanelState()
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


        private bool areFuelTransferButtonsVisible = false;
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

            CraftFuelSourceInspectorModel = new GroupModel("Craft Resources Inspector");

            // 大家好啊,我是分割线
            foreach (var fuelTypeId in fuelTypeIDList)
            {
                addFuelTypeTemplateItem(fuelTypeId);
            }

            CraftFuelSourceInspectorModel.Add(
                new TextButtonModel("Resources Fill,Waste Drain", b => setAllReciveMode()));
            CraftFuelSourceInspectorModel.Add(new TextButtonModel("Resources Drain,Waste Fill", b => setAllSendMode()));
            CraftFuelSourceInspectorModel.Add(new TextButtonModel("Reset All Transfer Mode",
                b => resetAllReciveMode()));
            CraftFuelSourceInspectorModel.Add(new TextButtonModel("Toggle Single Type Transfer Mode",
                b => SwitchFuelTransferVisibility()));
            CraftFuelSourceInspectorModel.Add(new TextModel("", () => ""));



            inspectorModel.AddGroup(CraftFuelSourceInspectorModel);

            #endregion

            GroupModel CrewInspectorGroup = new GroupModel("Crew Inspector");
            foreach (EvaScript eva in DroodScriptsList)
            {
                SupportLifeScript supportLifeScript = eva.PartScript?.GetModifier<SupportLifeScript>();
                if (supportLifeScript != null)
                {

                    CrewInspectorGroup.Add<TextModel>(new TextModel("Crew Name", () => eva.Data.CrewName));
                    CrewInspectorGroup.Add(new TextModel("Crew Role",
                        () => supportLifeScript.Data.DroodismCrewData == null
                            ? "Unknow"
                            : supportLifeScript.Data.DroodismCrewData.CrewRole.ToString()));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Mission Time",
                        (Func<string>)(() => Mod.GetStopwatchTimeString(supportLifeScript.MissionDurationTime)),
                        tooltip: eva.Data.CrewName + ";s mission time since launch."));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Remain Oxygen", (Func<string>)(() =>
                    {
                        if (supportLifeScript.UsingInternalOxygen())
                        {
                            float percentage = (float)(supportLifeScript.Data._oxygenAmountBuffer /
                                                       supportLifeScript.Data.DesireOxygenCapacity);
                            string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                            return $"<color={oxygenTextColor}>{Units.GetPercentageString(percentage)}</color>";
                        }
                        else if (!supportLifeScript.UsingInternalOxygen())
                        {
                            return "<color=green>Using External Oxygen</color>";
                        }

                        return "<color=purple>N/A</color>";
                    })));


                    CrewInspectorGroup.Add<TextModel>(new TextModel("Remain Water", (Func<string>)(() =>
                    {
                        float waterPercentage = (float)(supportLifeScript.Data._waterAmountBuffer /
                                                        supportLifeScript.Data.DesireWaterCapacity);
                        string waterTextColor =
                            waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                        return $"<color={waterTextColor}>{Units.GetPercentageString(waterPercentage)}</color>";
                    })));

                    CrewInspectorGroup.Add<TextModel>(new TextModel("Remain Food", (Func<string>)(() =>
                    {
                        float foodPercentage = (float)(supportLifeScript.Data._foodAmountBuffer /
                                                       supportLifeScript.Data.DesireFoodCapacity);
                        string foodTextColor =
                            foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                        return $"<color={foodTextColor}>{Units.GetPercentageString(foodPercentage)}</color>";
                    })));


                    CrewInspectorGroup.Add<TextModel>(new TextModel("CO2 Level", (Func<string>)(() =>
                    {
                        if (supportLifeScript.UsingInternalOxygen())
                        {
                            float Percentage = (float)(supportLifeScript.Data._co2AmountBuffer /
                                                       supportLifeScript.Data.DesireCO2Capacity);
                            string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                            return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
                        }
                        else
                        {
                            return "<color=green>Using External Oxygen</color>";
                        }
                    })));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Wasted Water Level", (Func<string>)(() =>
                    {
                        float Percentage = (float)(supportLifeScript.Data._wastedWaterAmountBuffer /
                                                   supportLifeScript.Data.DesireWastedWaterCapacity);
                        string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                        return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
                    })));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Solid Waste Level", (Func<string>)(() =>
                    {
                        float Percentage = (float)(supportLifeScript.Data._solidWasteAmountBuffer /
                                                   supportLifeScript.Data.DesireSolidWasteCapacity);
                        string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                        return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
                    })));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Radiation Dose",
                        (Func<string>)(() => $"{supportLifeScript.Data.CumulativeRad:F4} rad"),
                        tooltip: eva.Data.CrewName + ";s Current Radiation Dose"));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Radiation Stats",
                        (Func<string>)(() => $"{supportLifeScript.CurrentCumulativeRadiationStats}"),
                        tooltip: eva.Data.CrewName + ";s Current Radiation Cumulative Does Stats"));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Radiation Rate",
                        (Func<string>)(() => $"{supportLifeScript.RadiationDoseRateRadPerHour:F2} rad/h"),
                        tooltip: eva.Data.CrewName + ";s Current Radiation Increase Rate Per Hour"));
                    CrewInspectorGroup.Add<TextModel>(new TextModel("Radiation Rate Stats",
                        (Func<string>)(() => $"{supportLifeScript.CurrentRadiationRateStats}"),
                        tooltip: eva.Data.CrewName + ";s Current Radiation Rate Stats,if it's red, watch out!"));

                    //分割线!
                    CrewInspectorGroup.Add<TextModel>(new TextModel("", () => ""));
                }
            }


            inspectorModel.AddGroup(CrewInspectorGroup);
            inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel,
                new InspectorPanelCreationInfo()
                {
                    PanelWidth = 400,
                    Resizable = true,
                });

            void addFuelTypeTemplateItem(string fuelTypeId)
            {
                IFuelSource fuelSource = GetIFuelSourceByID(fuelTypeId);
                bool isWasted = fuelTypeId.Contains("Wasted") || fuelTypeId == "CO2";

                CraftFuelSourceInspectorModel.Add(new TextModel("", () => ""));
                CraftFuelSourceInspectorModel.Add(new TextModel("", () => ""));
                // 添加燃料名称和数据（燃料量、消耗率、剩余时间）
                CraftFuelSourceInspectorModel.Add(new TextModel(
                    ModApi.Common.Game.Instance.PropulsionData.GetFuelType(fuelTypeId).Name,
                    () => FuelUIDataMap.ContainsKey(fuelTypeId)
                        ? $"{FuelUIDataMap[fuelTypeId].TimeLeft}"
                        : "empty"));

                // 添加进度条
                CraftFuelSourceInspectorModel.Add(new ProgressBarModel(
                    () => $"{FuelUIDataMap[fuelTypeId].FuelAmountPercentageStr}",
                    () => FuelUIDataMap.ContainsKey(fuelTypeId) ? FuelUIDataMap[fuelTypeId].FuelPercentage : 0f));
                CraftFuelSourceInspectorModel.Add(new TextModel("",
                    () => FuelUIDataMap[fuelTypeId].FuelConsumptionStr));

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

                CraftFuelSourceInspectorModel.Add<IconButtonRowModel>(iconButtonRowModel);
                iconButtonRowModel.Visible = areFuelTransferButtonsVisible; // 设置可见性
            }

            #region FuelTransferMode 相关

            void SetFuelTransferMode(FuelTransferMode fuelTransferMode, string fuelTypeId)
            {
                if (ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name
                    .Contains("Eva"))
                {
                    ModApi.Common.Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                        "Cannot set fuel transfer mode to a single Drood.");
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

            void SwitchFuelTransferVisibility()
            {
                areFuelTransferButtonsVisible = !areFuelTransferButtonsVisible; // 切换可见状态
                foreach (var buttonRow in FuelButtonRows.Values)
                {
                    buttonRow.Visible = areFuelTransferButtonsVisible; // 设置可见性
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
                    if (pd.PartType.Name == "Eva")
                    {
                        DroodScriptsList.Add(pd.PartScript.GetModifier<EvaScript>());
                        DroodCountTotal++;
                        AstronautCount++;

                    }

                    if (pd.PartType.Name == "Eva-Tourist")
                    {
                        DroodCountTotal++;
                        TouristCount++;
                    }

                }
            }


        }


        #endregion

        //TODO implement with refactored script
        public IFuelSource GetIFuelSourceByID(string fuelTypeId)
        {
            try
            {
                var patchScript = Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.Part
                    .PartScript
                    .GetModifier<STCommandPodPatchScript>();
                if (patchScript == null)
                {
                    return null;
                }

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
            }
            catch (Exception)
            {

            }

            return null;

        }

    }
}
