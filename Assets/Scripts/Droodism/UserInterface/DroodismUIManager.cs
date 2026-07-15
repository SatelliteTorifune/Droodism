using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Droodism.Crew;
using ModApi;
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
using UnityEngine;
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
        

        public List<EvaScript> DroodScriptsList = new List<EvaScript>();

        private Dictionary<string, (double Current, double Previous)> FuelMap = new Dictionary<string, (double, double)>
        {
            { "Oxygen", (0, 0) },
            { "H2O", (0, 0) },
            { "Food", (0, 0) },
            { "LPCO2", (0, 0) },
            { "Wasted Water", (0, 0) },
            { "Solid Waste", (0, 0) }
        };

        /// <summary>
        /// 使用指数移动平均(EMA)平滑后的各燃料消耗率，Key = fuelTypeId, Value = 平滑后的消耗率(单位/秒)
        /// </summary>
        private Dictionary<string, double> _smoothedRates = new Dictionary<string, double>();

        /// <summary>
        /// EMA 平滑因子（alpha）。值越小曲线越平滑，值越大响应越快。
        /// 0.1 表示新原始值占 10% 权重，历史平滑值占 90%，可有效过滤高频噪声。
        /// </summary>
        private const double SmoothingFactor = 0.2f;

        /// <summary>
        /// 高帧率阈值（秒）。当 deltaTime 小于此值（即帧率 > 120 FPS）时启用 EMA 平滑，
        /// 否则直接使用原始帧率计算值以保证低帧率下的响应速度。
        /// </summary>
        private const float HighFpsThreshold = 1f / 60f;

        private List<string> fuelTypeIDList = new List<string>
            { "Oxygen", "H2O", "Food", "LPCO2", "Wasted Water", "Solid Waste" };

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
            { "LPCO2", new FuelUIData() },
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

            /*
            if (CraftFuelSourceInspectorModel != null&&this.inspectorPanel!=null&&this.inspectorPanel.Visible)
            {
                CraftFuelSourceInspectorModel.Visible = Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.Part.GetModifier<EvaData>()==null&&Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count>1;
            }*/

            foreach (var id in fuelTypeIDList)
            {
                var source = GetIFuelSourceByID(id);
                if (source != null)
                {
                    try
                    {
                        UpdateFuelTemplateItem(source);
                    }
                    catch (Exception e)
                    {
                        Mod.Log("DroodismUiManager:UpdateFuelTemplateItem"+e);
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
            double totalCapacity = fuelSource.TotalCapacity;
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

            double fuelPercentage = (currentFuel / totalCapacity);
            bool isWasted = (fuelTypeId.Contains("Waste") || fuelTypeId.Contains("LPCO2"));
            string FuelAmountPercentagestr = Mod.Instance.FormatFuel(fuelSource.TotalFuel * fuelDensity, _massTypes) +
                                             "/" + Mod.Instance.FormatFuel(totalCapacity * fuelDensity,
                                                 _massTypes);
            float deltaTime = (float)Game.Instance.FlightScene.TimeManager.DeltaTime;

            // 仅在帧率 > 120 FPS (deltaTime < 1/120s) 时启用 EMA 平滑，
            // 低帧率下直接使用原始帧率值以保证响应速度。
            double fuelConsumption;
            bool hasSmoothed = _smoothedRates.ContainsKey(fuelTypeId);

            if (deltaTime <= 0f)
            {
                // deltaTime 为零或负值时复用上一帧的平滑值，避免零除
                fuelConsumption = hasSmoothed ? _smoothedRates[fuelTypeId] : 0.0;
            }
            else if (deltaTime < HighFpsThreshold && hasSmoothed)
            {
                // 高帧率：应用 EMA 平滑，过滤单帧噪声
                double rawRate = (currentFuel - previousFuel) / deltaTime;
                fuelConsumption = rawRate * SmoothingFactor + _smoothedRates[fuelTypeId] * (1.0 - SmoothingFactor);
                _smoothedRates[fuelTypeId] = fuelConsumption;
            }
            else
            {
                // 低帧率或首次初始化，直接使用原始值
                double rawRate = (currentFuel - previousFuel) / deltaTime;
                _smoothedRates[fuelTypeId] = rawRate;
                fuelConsumption = rawRate;
            }

            string fuelConsumptionStr = Mod.Instance.FormatFuel(fuelConsumption * fuelDensity, _massTypes) + "/s";

            string timeLeft = isWasted
                ? (fuelConsumption >= 0
                    ? $"<color=#E05D6A>{Units.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity - fuelSource.TotalFuel) / fuelConsumption))}</color>"
                    : $"<color=#81EE80>{Units.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel / fuelConsumption))}</color>")
                : (fuelConsumption >= 0
                    ? $"<color=#81EE80>{Units.GetStopwatchTimeString(Math.Abs((fuelSource.TotalCapacity - fuelSource.TotalFuel) / fuelConsumption))}</color>"
                    : $"<color=#E05D6A>{Units.GetStopwatchTimeString(Math.Abs(fuelSource.TotalFuel / fuelConsumption))}</color>");
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
                    new XAttribute("tooltip", Locale.GetString("Droodism.DroodismUIManager.ToggleDroodismUI")),
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
            
            var craftScript = Game.Instance.FlightScene.CraftNode.CraftScript;
            if (craftScript==null)
            {
                return;
            }

            UpdateInfo();
            //别想了,这个做不到
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
                inspectorPanel.Visible =  !inspectorPanel.Visible;
            }
            catch (Exception)
            {
                CreateInspectorPanel();
                inspectorPanel.Visible =  !inspectorPanel.Visible;
            }
        }


        private bool areFuelTransferButtonsVisible = false;
        private Dictionary<string, IconButtonRowModel> FuelButtonRows = new Dictionary<string, IconButtonRowModel>();

        public void CreateInspectorPanel()
        {
            // 清空 FuelButtonRows 以避免重复添加
            FuelButtonRows.Clear();
            
            // 大家好啊,我是分割线
            inspectorModel = new InspectorModel(Locale.GetString("Droodism.DroodismUIManager.InspectorTitle"), "<color=green>" + Locale.GetString("Droodism.DroodismUIManager.LifeSupportInspector"));

            inspectorModel.Add(new TextModel(Locale.GetString("Droodism.DroodismUIManager.CrewCount"), () => DroodCountTotal.ToString()));
            inspectorModel.Add(new TextModel(Locale.GetString("Droodism.DroodismUIManager.AstronautCount"), () => AstronautCount.ToString()));
            inspectorModel.Add(new TextModel(Locale.GetString("Droodism.DroodismUIManager.TouristCount"), () => TouristCount.ToString()));

            #region FuelSourceManagerGroup

            CraftFuelSourceInspectorModel = new GroupModel(Locale.GetString("Droodism.DroodismUIManager.CraftResourcesInspector"));

            // 大家好啊,我是分割线
            foreach (var fuelTypeId in fuelTypeIDList)
            {
                addFuelTypeTemplateItem(fuelTypeId);
            }

            CraftFuelSourceInspectorModel.Add(
                new TextButtonModel(Locale.GetString("Droodism.DroodismUIManager.ResourcesFillWasteDrain"), b => setAllReciveMode()));
            CraftFuelSourceInspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.DroodismUIManager.ResourcesDrainWasteFill"), b => setAllSendMode()));
            CraftFuelSourceInspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.DroodismUIManager.ResetAllTransferMode"),
                b => resetAllReciveMode()));
            CraftFuelSourceInspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.DroodismUIManager.ToggleSingleTypeTransferMode"),
                b => SwitchFuelTransferVisibility()));
            CraftFuelSourceInspectorModel.Add(new TextModel("", () => ""));



            inspectorModel.AddGroup(CraftFuelSourceInspectorModel);

            #endregion

            GroupModel CrewInspectorGroup = new GroupModel(Locale.GetString("Droodism.DroodismUIManager.CrewInspector"));
            foreach (EvaScript eva in DroodScriptsList)
            {
                SupportLifeScript supportLifeScript = eva.PartScript?.GetModifier<SupportLifeScript>();
                if (supportLifeScript != null)
                {

                    var droodismCrewData = supportLifeScript.Data.DroodismCrewData;
                    string color =droodismCrewData==null?"white": droodismCrewData.CrewRole == DroodType.Engineer ? "#0072FF" :
                        droodismCrewData.CrewRole == 
                        DroodType.Scientist ? "#62BF05" : droodismCrewData.CrewRole == DroodType.Pilot?"#FF0003":"white";
                    CrewInspectorGroup.Add<TextModel>(new TextModel(droodismCrewData == null
                        ? Locale.GetString("Droodism.DroodismUIManager.UnknownRole")
                        : $"<color={color}>{droodismCrewData.CrewRole.ToString()}</color>", () => eva.Data.CrewName));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.MissionTime"),
                        (Func<string>)(() => Units.GetStopwatchTimeString(supportLifeScript.MissionDurationTime)),
                        tooltip: string.Format(Locale.GetString("Droodism.DroodismUIManager.MissionTimeTooltip"), eva.Data.CrewName)));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.RemainOxygen"), (Func<string>)(() =>
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
                            return "<color=green>" + Locale.GetString("Droodism.DroodismUIManager.UsingExternalOxygen") + "</color>";
                        }

                        return "<color=purple>" + Locale.GetString("Droodism.DroodismUIManager.NotAvailable") + "</color>";
                    })));


                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.RemainWater"), (Func<string>)(() =>
                    {
                        float waterPercentage = (float)(supportLifeScript.Data._waterAmountBuffer /
                                                        supportLifeScript.Data.DesireWaterCapacity);
                        string waterTextColor =
                            waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                        return $"<color={waterTextColor}>{Units.GetPercentageString(waterPercentage)}</color>";
                    })));

                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.RemainFood"), (Func<string>)(() =>
                    {
                        float foodPercentage = (float)(supportLifeScript.Data._foodAmountBuffer /
                                                       supportLifeScript.Data.DesireFoodCapacity);
                        string foodTextColor =
                            foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                        return $"<color={foodTextColor}>{Units.GetPercentageString(foodPercentage)}</color>";
                    })));


                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.CO2Level"), (Func<string>)(() =>
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
                            return "<color=green>" + Locale.GetString("Droodism.DroodismUIManager.UsingExternalOxygen") + "</color>";
                        }
                    })));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.WastedWaterLevel"), (Func<string>)(() =>
                    {
                        float Percentage = (float)(supportLifeScript.Data._wastedWaterAmountBuffer /
                                                   supportLifeScript.Data.DesireWastedWaterCapacity);
                        string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                        return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
                    })));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.SolidWasteLevel"), (Func<string>)(() =>
                    {
                        float Percentage = (float)(supportLifeScript.Data._solidWasteAmountBuffer /
                                                   supportLifeScript.Data.DesireSolidWasteCapacity);
                        string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                        return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
                    })));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.RadiationDose"),
                        (Func<string>)(() => $"{supportLifeScript.Data.CumulativeRad:F4} rad"),
                        tooltip:  string.Format(Locale.GetString("Droodism.DroodismUIManager.RadiationDoseTooltip"), eva.Data.CrewName),determineVisibility:() => supportLifeScript.Data.CumulativeRad>0f));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.RadiationStats"),
                        (Func<string>)(() => $"{supportLifeScript.CurrentCumulativeRadiationStats}"),
                        tooltip:  string.Format(Locale.GetString("Droodism.DroodismUIManager.RadiationStatsTooltip"), eva.Data.CrewName),determineVisibility:() => supportLifeScript.Data.CumulativeRad >0f));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.RadiationRate"),
                        (Func<string>)(() => $"{supportLifeScript.RadiationDoseRateRadPerHour:F2} rad/h"),
                        tooltip:  string.Format(Locale.GetString("Droodism.DroodismUIManager.RadiationRateTooltip"), eva.Data.CrewName),determineVisibility:() => supportLifeScript.RadiationDoseRateRadPerHour>0f));
                    CrewInspectorGroup.Add<TextModel>(new TextModel(Locale.GetString("Droodism.DroodismUIManager.RadiationRateStats"),
                        (Func<string>)(() => $"{supportLifeScript.CurrentRadiationRateStats}"),
                        tooltip: string.Format(Locale.GetString("Droodism.DroodismUIManager.RadiationRateStatsTooltip"), eva.Data.CrewName),determineVisibility:() => supportLifeScript.RadiationDoseRateRadPerHour>0f));
                    TextButtonModel evaButtonModel = new TextButtonModel(Locale.GetString("Parts.EvaScript.Eva"), (Action<TextButtonModel>) (b => eva.CrewCompartment.UnloadCrewMember(eva,true)), determineVisiblity:  (() =>  eva.CrewCompartment !=  null));
                    evaButtonModel.Style = ButtonModel.ButtonStyle.Primary;
                    CrewInspectorGroup.Add<TextButtonModel>(evaButtonModel);
                    TextButtonModel selectPartButtonModel = new TextButtonModel(Locale.GetString("Droodism.DroodismUIManager.SelectDrood"), (Action<TextButtonModel>) (b =>
                    {
                        Game.Instance.FlightScene.ViewManager.GameView.SelectedPart = eva.PartScript;

                    }), determineVisiblity:  (() =>  eva.CrewCompartment !=  null));
                    CrewInspectorGroup.Add<TextButtonModel>(selectPartButtonModel);
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

                CraftFuelSourceInspectorModel.Add(new TextModel("", () => ""));
                CraftFuelSourceInspectorModel.Add(new TextModel("", () => ""));
                // 添加燃料名称和数据（燃料量、消耗率、剩余时间）
                CraftFuelSourceInspectorModel.Add(new TextModel(
                    Locale.GetString($"Fuel.{fuelTypeId}.Name"),
                    () => FuelUIDataMap.ContainsKey(fuelTypeId)
                        ? $"{FuelUIDataMap[fuelTypeId].TimeLeft}"
                        : Locale.GetString("Droodism.DroodismUIManager.Empty")));

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
                    Locale.GetString("Flight.Inspector.FuelTank.FuelTransfer.None.Tooltip"));
                IconButtonModel fuelTransferButtonFill = new IconButtonModel(
                    "Ui/Sprites/Flight/IconFuelTransferFill",
                    (Action<IconButtonModel>)(x => SetFuelTransferMode(FuelTransferMode.Fill, fuelSource.FuelType.Id)),
                    Locale.GetString("Flight.Inspector.FuelTank.FuelTransfer.Fill.Tooltip"));
                IconButtonModel fuelTransferButtonDrain = new IconButtonModel(
                    "Ui/Sprites/Flight/IconFuelTransferDrain",
                    (Action<IconButtonModel>)(x => SetFuelTransferMode(FuelTransferMode.Drain, fuelSource.FuelType.Id)),
                    Locale.GetString("Flight.Inspector.FuelTank.FuelTransfer.Drain.Tooltip"));
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
                if (ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva")&&ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count==1)
                {
                    ModApi.Common.Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                        Locale.GetString("Droodism.DroodismUIManager.CannotSetFuelTransferSingleDrood"));
                    return;
                }

                GetIFuelSourceByID(fuelTypeId).FuelTransferMode = fuelTransferMode;
            }

            void setAllReciveMode()
            {
                SetFuelTransferMode(FuelTransferMode.Fill, "Oxygen");
                SetFuelTransferMode(FuelTransferMode.Fill, "H2O");
                SetFuelTransferMode(FuelTransferMode.Fill, "Food");
                SetFuelTransferMode(FuelTransferMode.Drain, "LPCO2");
                SetFuelTransferMode(FuelTransferMode.Drain, "Wasted Water");
                SetFuelTransferMode(FuelTransferMode.Drain, "Solid Waste");
            }

            void setAllSendMode()
            {
                SetFuelTransferMode(FuelTransferMode.Drain, "Oxygen");
                SetFuelTransferMode(FuelTransferMode.Drain, "H2O");
                SetFuelTransferMode(FuelTransferMode.Drain, "Food");
                SetFuelTransferMode(FuelTransferMode.Fill, "LPCO2");
                SetFuelTransferMode(FuelTransferMode.Fill, "Wasted Water");
                SetFuelTransferMode(FuelTransferMode.Fill, "Solid Waste");
            }

            void resetAllReciveMode()
            {
                SetFuelTransferMode(FuelTransferMode.None, "Oxygen");
                SetFuelTransferMode(FuelTransferMode.None, "H2O");
                SetFuelTransferMode(FuelTransferMode.None, "Food");
                SetFuelTransferMode(FuelTransferMode.None, "LPCO2");
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
            ForceRebuildInspetorPanel();
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
        private void ForceRebuildInspetorPanel()
        {
            bool vistem = false;
            if (inspectorPanel != null)
            {
                vistem = inspectorPanel.Visible;
                inspectorPanel.Visible = false;
            }
            inspectorPanel = null;
            CreateInspectorPanel();
            inspectorPanel.Visible = vistem;
        }


        #endregion

     
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
                    case "LPCO2":
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
