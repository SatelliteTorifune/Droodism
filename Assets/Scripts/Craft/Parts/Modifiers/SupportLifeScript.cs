using ModApi.Craft;
using ModApi.Craft.Parts;
using Assets.Scripts.Flight;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Craft.Parts.Modifiers.Input;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Droodism;
using Assets.Scripts.Droodism.Crew;
using Assets.Scripts.Droodism.ResourceWarning;
using Assets.Scripts.Droodism.RadiationBelt;
using ModApi;
using ModApi.Flight.Events;
using ModApi.Planet;
using ModApi.Flight.GameView;
using UnityEngine;
using ModApi.Flight.Sim;
using ModApi.Flight.UI;
using ModApi.Math;
using ModApi.Settings.Core;
using ModApi.Ui.Inspector;
using Assembly = ModApi.Craft.Assembly;

//鸡巴的我自己都看不懂我写的是什么鸡巴玩意了你还指望我给你写注释吗?
//顺便一提如果真有除了我以外的人在github上或者逆向出来了看到了这行字,那么我只能说一句牛逼,你简直是找屎大王,能闻着味道找到我编程以来拉的最大的一坨
//2025 7 25 我操你妈我受不了了我怎么还在和这坨我最先拉出来的屎山作斗争啊我操
//2025 8 24 孩子们我回来了,我是拉屎大王
//2025 8 31 嗨嗨嗨我又来了嗷
//2025 9 8 为什么
//2025 10 17一想到我还在这个Modifier苦战,往上面喷屎山我就忍不住轻哼起来.
//2025 10 22 我希望这是我最后一次碰这个class
//2025 11 10 Welcome back ,I will  fix this piece of shit once and for all.
//2026 3 23 孩子们我又回来了,猜猜我又拉了什么屎?
//2026 4 13 不是,我怎么还在给这个b玩意加东西
//2026 4 26 这个破fuelSource刷新的还在追我
//2026 5 6 我讨厌你
//2026 7 5 欲辩已忘言

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class SupportLifeScript : 
        PartModifierScript<SupportLifeData>,
        IFlightStart,
        IFlightUpdate,
        IFlightFixedUpdate
    {
        #region 字段与属性

        /// <summary>
        /// 引用EvaScript组件,来获取这个小蓝人的一些乱七八糟的狗屎鸡巴数据玩意
        /// Reference to the EvaScript component,get current part's eva data and other stuff
        /// </summary>
        internal EvaScript evaScript;
        
        /// <summary>
        /// 当前小蓝人是否处在休眠
        /// </summary>
        public bool IsHibernating { get; private set; }
        
        public IFuelSource OxygenSource { get; private set; }
        public IFuelSource WaterSource { get; private set; }
        public IFuelSource FoodSource { get; private set; }
        public IFuelSource Co2Source { get; private set; }
        public IFuelSource WastedWaterSource { get; private set; }
        public IFuelSource SolidWasteSource { get; private set; }

        /// <summary>
        /// 当前所在行星的名称。
        /// Name of the current planet the craft is on.
        /// </summary>
        private string currentPlanetName;
        
        /// <summary>
        /// 小蓝人目前的任务时长,从初次发射开始算的
        /// </summary>
        public long MissionDurationTime{get;private set;}
        
        /// <summary>
        /// 指示小蓝人是否在跑或是否为游客。
        /// Flags indicating if the crew member is running or if they are a tourist.
        /// </summary>
        public bool IsRunning { get; private set; }
        public bool IsTourist { get; private set; }
       
        /// <summary>
        /// 当前计算辐射值累计的配置模型
        /// Config Model for Calculating Radiation Level
        /// </summary>
        public RadiationBeltConfig RadiationBeltConfig { get; private set; }
        
        /// <summary>
        /// 指示小蓝人是否可以被治疗。
        /// </summary>
        public bool CanHeal { get; private set; }

        /// <summary>
        /// 当前辐射每小时吸收速率
        /// </summary>
        public float RadiationDoseRateRadPerHour { get; private set; }

        /// <summary>
        /// 累计辐射值状态
        /// </summary>
        public string CurrentCumulativeRadiationStats{ get; private set; }
                
        /// <summary>
        /// 当前辐射状态
        /// </summary>
        public string CurrentRadiationRateStats{ get; private set; }
        
        /// <summary>
        /// 外辐射带保护值
        /// </summary>
        private float outerRadiationProtection;

        /// <summary>
        /// 内辐射带保护值
        /// </summary>
        private float innerRadiationProtection;
        
        /// <summary>
        /// 来自craft内部的辐射保护值
        /// </summary>
        private float craftRadiationProtection;

        private static BreathablePlanets _breathablePlanetsCache;
        private static bool _breathablePlanetsCacheLoaded;

        /// <summary>
        /// 大气组分呼吸检测缓存：当前缓存的父行星引用
        /// </summary>
        private IPlanetNode _cachedBreathableBody;
        /// <summary>
        /// 大气组分呼吸检测缓存：当前行星是否可呼吸
        /// </summary>
        private bool _cachedIsBreathable;
        /// <summary>
        /// 可呼吸所需的最低氧气质量分数阈值
        /// </summary>
        private const float MinBreathableOxygenFraction = 0.1f;

        /// <summary>
        /// 来自craft内部的辐射源：RTG零件列表
        /// </summary>
        private List<PartData> RTGParts = new List<PartData>();
        /// <summary>
        /// 来自craft内部的辐射源：NTR引擎零件列表
        /// </summary>
        private List<PartData> NTRParts = new List<PartData>();

        /// <summary>
        /// 是否正在修复零件
        /// </summary>
        private bool isRepairing;

        #endregion

        #region 生命周期函数循环

        /// <summary>
        /// 在创建modifiers时调用，启用零件属性。
        /// Called when modifiers are created, enables part properties.
        /// </summary>
        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            this.Data.PartPropertiesEnabled = true;
            this.evaScript = PartScript.GetModifier<EvaScript>();
        }
        
        /// <summary>
        /// 不好意思因为我的代码水平就是一坨屎所以OnInitialLaunch和FlightStart里面的代码很奇异搞笑
        /// </summary>
        public override void OnInitialLaunch()
        {
            Data.MissionStartTime = (long)Game.Instance.FlightScene.FlightState.Time;
            Data._foodAmountBuffer=this.Data.DesireFoodCapacity;
            Data._oxygenAmountBuffer=this.Data.DesireOxygenCapacity;
            Data._waterAmountBuffer=this.Data.DesireWaterCapacity;
            Data._co2AmountBuffer=0;
            Data._wastedWaterAmountBuffer=0;
            Data._solidWasteAmountBuffer=0;
            Refresh();
            Data.LastLoadTime = (long)FlightSceneScript.Instance.FlightState.Time;
            
        }
        
        /// <summary>
        /// 实现IFlightStart接口，在飞行场景开始时调用。
        /// Implements the IFlightStart interface, called at the start of the flight scene.
        /// </summary>
        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            Game.Instance.FlightScene.FlightEnded+=OnFlightEnded;
            Game.Instance.FlightScene.PlayerChangedSoi += OnPlayerChangedSoi;
            Game.Instance.FlightScene.CraftNode.PhysicsDisabled += OnPhysicsDisabled;
            Game.Instance.FlightScene.CraftNode.PhysicsEnabled += OnPhysicsEnabled;
        
            this.Data.InspectorEnabled = true;
            if (this.PartScript.Data.PartType.Name == "Eva-Tourist")
            {
                IsTourist = true;
            }
            evaScript = this.PartScript.GetModifier<EvaScript>();
            UpdateCurrentPlanet();
            this.RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(currentPlanetName);
            LoadRadiationData();
        }

        /// <summary>
        /// 实现IFlightUpdate接口，在飞行期间每帧调用。
        /// Implements the IFlightUpdate interface, called every frame during flight.
        /// </summary>
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0) 
                return;

            UpdateRunningStatus();
            CheckRadiationState(frame);
            DamageRadiation(frame);
            UpdateHealingStatus();
            
            if (!IsHibernating)
            {
                if (!UsingInternalOxygen())   
                {
                    AutoRefillLogic(frame);
                }
                ConsumptionLogic(frame);
                PilotDamageReduction(frame);
            }
            if (ModSettings.Instance.ActiveUpdateRadiationBeltConfig)
            {
                this.RadiationBeltConfig = RadiationBeltManager.Instance.GetRuntimeConfigForPlanet(currentPlanetName);
            }
            
            MissionDurationTime = (long)Game.Instance.FlightScene.FlightState.Time - Data.MissionStartTime;
            if (isRepairing)
            {
                var selectedPart = Game.Instance.FlightScene.ViewManager.GameView.SelectedPart;
                if (selectedPart != null)
                {
                    RepairPartWorkingLogic(frame, selectedPart.Data);
                }
                else
                {
                    isRepairing = false;
                }
            }
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0) 
                return;
            if (Data.ParachuteTypes!="None"&&Data.AutoDeployEnabled)
            {
                AutoDeployParachute();
            }
        }

        /// <summary>
        /// 在加载飞船时调用，触发飞船结构变化处理。
        /// Called when the craft is loaded, triggers craft structure change handling.
        /// </summary>
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {   
            base.OnCraftLoaded(craftScript, movedToNewCraft);
            if(!Game.InFlightScene)
                return;
            Refresh();
        }

        /// <summary>
        /// 在飞船结构变化时调用，如果在飞行场景中，则刷新燃料源。
        /// Called when the craft structure changes, refreshes fuel sources if in flight scene.
        /// </summary>
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            if(!Game.InFlightScene)
                return;
            base.OnCraftStructureChanged(craftScript);
            Refresh();
            Mod.Log("OnCraftStructureChanged调用RefreshFuelSource();");
        }

        /// <summary>
        /// Called when the craft ends, removes extra fuel tanks.
        /// </summary>
        public override void FlightEnd()
        {
            Game.Instance.FlightScene.FlightEnded -= OnFlightEnded;
            Game.Instance.FlightScene.PlayerChangedSoi -= OnPlayerChangedSoi;
            if (Game.Instance.FlightScene.CraftNode != null)
            {
                Game.Instance.FlightScene.CraftNode.PhysicsDisabled -= OnPhysicsDisabled;
                Game.Instance.FlightScene.CraftNode.PhysicsEnabled -= OnPhysicsEnabled;
            }
            OnCraftUnloaded();
        }

        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            FlightEnd();
        }

        public void OnPhysicsEnabled(ICraftNode craftNode, PhysicsChangeReason reason)
        {
            if (reason == PhysicsChangeReason.Warp||reason == PhysicsChangeReason.LoadedIntoGameView)
            {
                return;
            }
            if (ModSettings.Instance.ConsumeResourceWhenUnloaded==true&&!IsHibernating)
            {
                // 先刷新燃料源引用，确保 RemoveFuelAmountInstantly / AddWastedAmountInstantly
                // 能正确访问到 craft 油箱，否则只会消耗本地 buffer（仅 ~5小时容量）
                Refresh();
                RemoveFuelAmountInstantly();
                AddWastedAmountInstantly();
            }
        }

        private void OnPhysicsDisabled(ICraftNode craftNode, PhysicsChangeReason reason)
        {
            if (reason == PhysicsChangeReason.Warp||reason== PhysicsChangeReason.UnloadedFromGameView)
            {
                return;
            }

            OnCraftUnloaded();
        }

        /// <summary>
        /// Called when a part this modifier is attached to is pulled out in the designer.  This is called before DesignerStart() and while the previous part is still selected.
        /// </summary>
        /// <param name="assembly">The assembly this modifier's part is within (will contain all parts if contained in a sub-assembly).</param>
        public override void OnDesignerPullout(Assembly assembly)
        {
            base.OnDesignerPullout(assembly);
            var eva = this.PartScript.GetModifier<EvaScript>();
            if (!eva.Data.RequiresCrewMember)
            {
              return;
            }

            Data.SetDroodismCrewData();
        }

        public override void OnCloned()
        {
            base.OnCloned();
            var eva = this.PartScript.GetModifier<EvaScript>();
            if (!eva.Data.RequiresCrewMember)
            {
                return;
            }
            Data.SetDroodismCrewData();
        }

        #endregion

        #region 状态更新

        /// <summary>
        /// 这b玩意看不懂那你去吃我屎吧,你不会百度翻译吗?
        /// </summary>
        private void UpdateRunningStatus()
        {
            IsRunning = evaScript.EvaActive && 
                        evaScript.IsPlayerCraft && 
                        !evaScript.IsWalking &&
                        evaScript.IsGroundedTerrain && 
                        PartScript.CraftScript.SurfaceVelocity.magnitude >= 0.8;
        }

        /// <summary>
        /// 检测这这个小蓝人能不能被治疗
        /// </summary>
        private void UpdateHealingStatus()
        {
            bool lackInput = Data._oxygenAmountBuffer <= 0 || Data._foodAmountBuffer <= 0 ||
                             Data._waterAmountBuffer <= 0;
            bool wasteIsFull = Data.DesireCO2Capacity - Data._co2AmountBuffer <= 0.00001 ||
                               Data.DesireWastedWaterCapacity - Data._wastedWaterAmountBuffer <= 0.00001 ||
                                 Data.DesireSolidWasteCapacity - Data._solidWasteAmountBuffer <= 0.00001;
            bool severeRadiation = this.Data.CumulativeRad >= this.Data.RadiationDamageThresholdLevel3;

            this.CanHeal = !(lackInput || wasteIsFull || severeRadiation);
        }

        public void SetHibernating(bool hibernatingState, PartType partType)
        {
            if (partType.Id!="HibernatingChamber")
            {
                IsHibernating = false;
                return;
            }
            IsHibernating = hibernatingState;
        }

        /// <summary>
        /// 根据大气条件和行星确定是否使用内部氧气。
        /// Determines if internal oxygen is being used based on atmospheric conditions and planet.
        /// </summary>
        /// <returns>如果使用内部氧气则返回true，否则返回false。True if using internal oxygen, false otherwise.</returns>
        public bool UsingInternalOxygen()
        {
            float airDensity = PartScript.CraftScript.AtmosphereSample.AirDensity;
            if (airDensity <= 0.4)
            {
                return true;
            }
            
            if (IsBreathablePlanet(currentPlanetName) || IsBreathableByComposition())
            {
                if(evaScript.IsInWater && PartScript.CraftScript.FlightData.AltitudeAboveSeaLevel < 0.1)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        #endregion

        #region 燃料源管理

        /// <summary>
        /// 根据当前场景和EVA状态刷新燃料源。
        /// Refreshes the fuel sources based on the current scene and EVA status.
        /// </summary>
        public void Refresh()
        {
            if (PartScript == null || PartScript.Modifiers == null)
            {
                return;
            }
            
            if (Game.InFlightScene)
            {
                try
                {
                    RefreshFuelSource();
                    CheckInCraftRadiationSource();
                    RefreshRadiationCompartment();
                }
                catch (Exception e)
                {
                    Mod.Log("SupportLifeScript.Refresh 出错:{0}", e);
                }
            }
        }
        
        /// <summary>
        /// 在非EVA模式下刷新燃料源。
        /// Refreshes fuel sources when not in EVA mode.
        /// </summary>
        /// 这破玩意我给重构了,我操,牛逼
        private void RefreshFuelSource()
        {
            bool isEva = IsActiveEvaOutsideCompartment();
            
            if (isEva)
            {
                IsHibernating = false;
                OxygenSource = WaterSource = FoodSource = WastedWaterSource = Co2Source = SolidWasteSource = null;
            }
            else
            {
                RefreshCraftFuelSources();
            }
            
            SyncCraftAndLocalFuelSources();
            ResourceWarningScript.Instance.ResetSessionFlags();

            bool IsActiveEvaOutsideCompartment()
            {
                return PartScript?.CraftScript?.ActiveCommandPod?.Part?.PartScript == PartScript && !evaScript.ActiveWhileInCrewCompartment;
            }
    
            void RefreshCraftFuelSources()
            {
                var evaModifier = PartScript.GetModifier<EvaScript>();
                if (evaModifier == null) return;
                var patch = evaModifier.CrewCompartment?.PartScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>();
                if (patch==null)
                {
                    Mod.Log("NULl DEtected in SupportLifeScript.RefreshFuelSource.RefreshCraftFuelSources");
                    return;
                }
                try
                {
                    OxygenSource = patch?.OxygenFuelSource;
                    FoodSource = patch?.FoodFuelSource;
                    WaterSource = patch?.WaterFuelSource;
                    Co2Source = patch?.CO2FuelSource;
                    WastedWaterSource = patch?.WastedWaterFuelSource;
                    SolidWasteSource = patch?.SolidWasteFuelSource;
                }
                catch (Exception e)
                {
                    Mod.Log("CraftRefreshSource::{0}", e);
                }
            }
    
            void SyncCraftAndLocalFuelSources()
            {
                TrySyncPair(OxygenSource,ref Data._oxygenAmountBuffer,Data.DesireOxygenCapacity, false);
                TrySyncPair(FoodSource,ref Data._foodAmountBuffer,Data.DesireFoodCapacity, false);
                TrySyncPair(WaterSource,ref Data._waterAmountBuffer,Data.DesireWaterCapacity, false);
                TrySyncPair(Co2Source,ref Data._co2AmountBuffer,Data.DesireCO2Capacity, true);
                TrySyncPair(WastedWaterSource,ref Data._wastedWaterAmountBuffer,Data.DesireWastedWaterCapacity, true);
                TrySyncPair(SolidWasteSource,ref Data._solidWasteAmountBuffer,Data.DesireSolidWasteCapacity, true);
            }
            
            void TrySyncPair(IFuelSource craftSource,ref double amount,float capacity, bool wasteMode)
            {
                if (craftSource == null)
                {
                    return;
                }
    
                if (wasteMode)
                {
                    if (craftSource.TotalCapacity - craftSource.TotalFuel > 0.00001 && amount > 0.00001)
                    {
                        RemoveWaste(craftSource, ref amount);
                    }
                    return;
                }
                if (!craftSource.IsEmpty && capacity - amount > 0.00001)
                {
                    ReFill(craftSource, ref amount, capacity);
                }
            }
        }

        /// <summary>
        /// 从Craft补充维生资源
        /// </summary>
        private void ReFill(IFuelSource craft,ref double amount,float capacity)
        {
            if (craft.TotalFuel >= capacity - amount)
            {
                craft.RemoveFuel(capacity - amount);
                amount = capacity;
            }
            else
            {
                double availableFuel = craft.TotalFuel;
                craft.RemoveFuel(availableFuel);
                amount += availableFuel;
            }
        }
        
        /// <summary>
        /// 给Drood移除产生的废物
        /// </summary>
        private void RemoveWaste(IFuelSource Craft,ref double drood)
        {
            if (Craft.TotalCapacity-Craft.TotalFuel>drood)
            {
                double addedAmount = drood;
                Craft.AddFuel(drood);
                drood = 0;
                Mod.Log($"Remove{Craft.FuelType.Name} 成功:应加{addedAmount}实际{Craft.TotalFuel}");
            }
            else
            {
                double remainingSpace = Craft.TotalCapacity - Craft.TotalFuel;
                drood -= remainingSpace;
                Craft.AddFuel(remainingSpace);
                Mod.Log($"Remove{Craft.FuelType.Name} 满了成功:剩余{drood}实际{Craft.TotalFuel}");
            }
        }

        #endregion

        #region 资源消耗与废物处理

        /// <summary>
        /// 处理氧气、食物和水的消耗逻辑。
        /// Handles the consumption logic for oxygen, food, and water.
        /// 你可能觉得这个函数也太不优雅了,对,因为氧气这个b玩意比较特殊我要单独处理
        /// </summary>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        private void ConsumptionLogic(in FlightFrameData frame)
        {
            bool usingInternalOxygen = UsingInternalOxygen();
            double baseRate = frame.DeltaTimeWorld * (IsRunning ? 1.75 : 1) * (IsTourist ? 1.05 : 1);

            if (usingInternalOxygen)
            {
                double oxygenConsumeAmount = (double)Data.OxygenConsumeRate * baseRate;
                bool oxygenConsumed = ConsumeInputResource(OxygenSource,ref Data._oxygenAmountBuffer, oxygenConsumeAmount, frame, Data.OxygenDamageScale,"Oxygen");
                if (oxygenConsumed)
                {
                    double co2Amount = oxygenConsumeAmount * 1.375 * Data.evaConsumeEfficiency;
                    ProduceWasteResource(Co2Source, ref Data._co2AmountBuffer,Data.DesireCO2Capacity, co2Amount, frame, Data.OxygenDamageScale,"CO2");
                }
            }

            double foodConsumeAmount = (double)Data.FoodConsumeRate * baseRate;
            bool foodConsumed = ConsumeInputResource(FoodSource,ref Data._foodAmountBuffer, foodConsumeAmount, frame, Data.FoodDamageScale,"Food");
            if (foodConsumed)
            {
                double solidWasteAmount = foodConsumeAmount * 1.1 * Data.evaConsumeEfficiency * 0.04;
                ProduceWasteResource(SolidWasteSource,ref Data._solidWasteAmountBuffer,Data.DesireSolidWasteCapacity, solidWasteAmount, frame, Data.FoodDamageScale,"Solid Waste");
            }

            double waterConsumeAmount = (double)Data.WaterConsumeRate * baseRate;
            bool waterConsumed = ConsumeInputResource(WaterSource,ref Data._waterAmountBuffer, waterConsumeAmount, frame, Data.WaterDamageScale,"H2O");
            if (waterConsumed)
            {
                double wastedWaterAmount = waterConsumeAmount * 1.1 * Data.evaConsumeEfficiency;
                ProduceWasteResource(WastedWaterSource, ref Data._wastedWaterAmountBuffer,Data.DesireWastedWaterCapacity, wastedWaterAmount, frame, Data.WaterDamageScale,"Wasted Water");
            }
        }
        
        /// <summary>
        /// 具体维生资源消耗函数
        /// </summary>
        private bool ConsumeInputResource(IFuelSource craftSource,ref double localSourceAmount, double amount, in FlightFrameData frame, float damageScale,string fuelTypeName)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (craftSource==null||craftSource.IsEmpty)
            {
                if (localSourceAmount>0)
                {
                    Data.AddLifeSupportFuel(fuelTypeName,-amount);
                    return true;
                }
                DamageDrood(fuelTypeName, frame, damageScale);
                return false;
            }

            if (craftSource!=null&&!craftSource.IsEmpty)
            {
                craftSource.RemoveFuel(amount);
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// 具体维生废物生成函数
        /// </summary>
        private void ProduceWasteResource(IFuelSource craftSource,ref double localSourceAmount,float localCapacity, double amount, in FlightFrameData frame, float damageScale,string fuelTypeName)
        {
            if (amount <= 0)
            {
                return;
            }
            
            if (craftSource==null||craftSource.TotalCapacity - craftSource.TotalFuel <= 0.00001)
            {
                if (localCapacity-localSourceAmount>0.00001)
                {
                    Data.AddLifeSupportFuel(fuelTypeName,amount);
                    return;
                }
                DamageWaste(fuelTypeName, frame, damageScale);
                return;
            }

            if (craftSource!=null&&craftSource.TotalCapacity - craftSource.TotalFuel > 0.00001)
            {
                craftSource.AddFuel(amount);
            }
        }

        /// <summary>
        /// 处理不使用自带氧气时的氧气自动补充逻辑。
        /// Handles the auto-refill logic for oxygen when not using internal oxygen.
        /// </summary>
        private void AutoRefillLogic(in FlightFrameData frame)
        {
            if (Data.DesireOxygenCapacity-Data._oxygenAmountBuffer >= 0.00001)
            {
                Data._oxygenAmountBuffer +=Data.DesireOxygenCapacity * frame.DeltaTimeWorld * 0.01f;
            }
        }

        #endregion

        #region 非加载时资源处理

        private void OnCraftUnloaded()
        {
            try
            {
                Data.LastLoadTime = (long)FlightSceneScript.Instance.FlightState.Time;
                PartScript.Data.LoadXML(RemoveFuelTankXML(this.PartScript.Data.GenerateXml(this.PartScript.CraftScript.Transform,false)),15);
            }
            catch (Exception e)
            {
                Mod.Log("RemoveExtraTanks调用RemoveFuelTankXML出问题了{0}", e);
            }
        }
        
        private void RemoveFuelAmountInstantly()
        {
            double elapsedSeconds = Game.Instance.FlightScene.FlightState.Time - Data.LastLoadTime;
            double activityMultiplier = (IsTourist ? 1.05 : 1.0);

            // --- 氧气消耗（仅在使用内部氧气时）---
            if (UsingInternalOxygen())
            {
                double oxygenToConsume = Data.OxygenConsumeRate * activityMultiplier * elapsedSeconds;
                double fromSource = 0;
                if (OxygenSource != null && !OxygenSource.IsEmpty)
                {
                    fromSource = Math.Min(oxygenToConsume, OxygenSource.TotalFuel);
                    OxygenSource.RemoveFuel(fromSource);
                }

                double remainingOxygen = oxygenToConsume - fromSource;
                if (remainingOxygen > 0)
                {
                    double fromBuffer = Math.Min(remainingOxygen, Data._oxygenAmountBuffer);
                    Data.AddLifeSupportFuel("Oxygen", -fromBuffer);
                }
            }

            // --- 水消耗 ---
            double waterToConsume = Data.WaterConsumeRate * activityMultiplier * elapsedSeconds;
            double fromWaterSource = 0;
            if (WaterSource != null && !WaterSource.IsEmpty)
            {
                fromWaterSource = Math.Min(waterToConsume, WaterSource.TotalFuel);
                WaterSource.RemoveFuel(fromWaterSource);
            }

            double remainingWater = waterToConsume - fromWaterSource;
            if (remainingWater > 0)
            {
                double fromBuffer = Math.Min(remainingWater, Data._waterAmountBuffer);
                Data.AddLifeSupportFuel("H2O", -fromBuffer);
            }

            // --- 食物消耗 ---
            double foodToConsume = Data.FoodConsumeRate * activityMultiplier * elapsedSeconds;
            double fromFoodSource = 0;
            if (FoodSource != null && !FoodSource.IsEmpty)
            {
                fromFoodSource = Math.Min(foodToConsume, FoodSource.TotalFuel);
                FoodSource.RemoveFuel(fromFoodSource);
            }

            double remainingFood = foodToConsume - fromFoodSource;
            if (remainingFood > 0)
            {
                double fromBuffer = Math.Min(remainingFood, Data._foodAmountBuffer);
                Data.AddLifeSupportFuel("Food", -fromBuffer);
            }
        }
        
        private void AddWastedAmountInstantly()
        {
            double elapsedSeconds = Game.Instance.FlightScene.FlightState.Time - Data.LastLoadTime;
            double activityMultiplier = (IsTourist ? 1.05 : 1.0);

            // --- CO2 产生（仅在使用内部氧气时）---
            if (UsingInternalOxygen())
            {
                double co2ToAdd = Data.OxygenConsumeRate * activityMultiplier * elapsedSeconds * 1.375 * Data.evaConsumeEfficiency;
                AddWasteToSource(Co2Source, ref Data._co2AmountBuffer, Data.DesireCO2Capacity, co2ToAdd);
            }

            // --- 废水产生 ---
            double wastedWaterToAdd = 1.1 * Data.WaterConsumeRate * Data.evaConsumeEfficiency * activityMultiplier * elapsedSeconds;
            AddWasteToSource(WastedWaterSource, ref Data._wastedWaterAmountBuffer, Data.DesireWastedWaterCapacity, wastedWaterToAdd);

            // --- 固体废物产生（注意 0.04 系数，与 ConsumptionLogic 对齐）---
            double solidWasteToAdd = Data.FoodConsumeRate * Data.evaConsumeEfficiency * 1.1 * 0.04 * activityMultiplier * elapsedSeconds;
            AddWasteToSource(SolidWasteSource, ref Data._solidWasteAmountBuffer, Data.DesireSolidWasteCapacity, solidWasteToAdd);
        }

        /// <summary>
        /// 将废物先填入 craft fuel source，满了再填入本地 buffer
        /// </summary>
        private void AddWasteToSource(IFuelSource craftSource, ref double buffer, float bufferCapacity, double amountToAdd)
        {
            if (amountToAdd <= 0) return;

            if (craftSource != null)
            {
                double availableSpace = craftSource.TotalCapacity - craftSource.TotalFuel;
                double intoCraft = Math.Min(amountToAdd, availableSpace);
                craftSource.AddFuel(intoCraft);
                double overflow = amountToAdd - intoCraft;
                if (overflow > 0)
                {
                    double intoBuffer = Math.Min(overflow, bufferCapacity - buffer);
                    buffer += intoBuffer;
                }
            }
            else
            {
                double intoBuffer = Math.Min(amountToAdd, bufferCapacity - buffer);
                buffer += intoBuffer;
            }
        }

        #endregion

        #region 可呼吸星球检测

        private static BreathablePlanets GetBreathablePlanets()
        {
            if (!_breathablePlanetsCacheLoaded)
            {
                _breathablePlanetsCacheLoaded = true;
                try
                {
                    _breathablePlanetsCache = BreathablePlanets.LoadFromFile();
                }
                catch (Exception ex)
                {
                    _breathablePlanetsCache = new BreathablePlanets
                    {
                        BreathablePlanet = Array.Empty<string>()
                    };
                }
            }
            return _breathablePlanetsCache;
        }

        private static bool IsBreathablePlanet(string planetName)
        {
            var config = GetBreathablePlanets();
            if (config?.BreathablePlanet == null)
            {
                return false;
            }
            return config.BreathablePlanet.Contains(planetName);
        }

        /// <summary>
        /// 通过扫描大气组分中的氧气质量分数来判断当前行星是否可呼吸。
        /// 移植自 JetEngineScript.ComputeOxygenMultiplier 的 LOX 查找逻辑。
        /// 带缓存，行星体不变时不重算。
        /// </summary>
        private bool IsBreathableByComposition()
        {
            var parent = PartScript.CraftScript?.CraftNode?.Parent;
            if (parent == null) return false;

            // 缓存：行星体不变就不重新计算
            if (parent != _cachedBreathableBody)
            {
                _cachedBreathableBody = parent;
                _cachedIsBreathable = ComputeBreathable(parent);
            }
            return _cachedIsBreathable;
        }

        /// <summary>
        /// 扫描行星的大气组分列表，查找氧气（LOX/O2）并判断其质量分数
        /// 是否达到可呼吸阈值
        /// </summary>
        private static bool ComputeBreathable(IPlanetNode parent)
        {
            var composition = parent?.PlanetData?.AtmosphereData?.Composition;
            if (composition == null) return false;

            foreach (var atmosphereComponent in composition)
            {
                if (atmosphereComponent == null) continue;

                // 兼容 GasId 的两种可能命名
                if (atmosphereComponent.GasId == "LOX" )//|| atmosphereComponent.GasId == "O2")
                {
                    return atmosphereComponent.MassFraction >= MinBreathableOxygenFraction;
                }
            }
            return false;
        }

        #endregion

        #region 行星与SOI更新

        /// <summary>
        /// 根据飞行场景数据更新当前行星名称。
        /// Updates the current planet name based on the flight scene data.
        /// </summary>
        private void UpdateCurrentPlanet()
        {
            if (!Game.InFlightScene)
            {
                return;
            }
            try
            {
                currentPlanetName = this.PartScript.CraftScript.FlightData.Orbit.Parent.PlanetData
                    .Name;
              
                Mod.Log("currentPlanetName update"+currentPlanetName);
            }
            catch (Exception e)
            {
                Mod.Log("UpdateCurrentPlanet调用出问题了{0}", e);
            }
            this.RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(currentPlanetName);
        }
        
        /// <summary>
        /// 重载,手动输入名称
        /// </summary>
        private void UpdateCurrentPlanet(string name)
        {
            if (!Game.InFlightScene)
            {
                return;
            }
            currentPlanetName = name;
            this.RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(name);
        }

        private void OnPlayerChangedSoi(ICraftNode craftNode, IOrbitNode orbitNode)
        {
            UpdateCurrentPlanet(orbitNode.Name);
        }

        #endregion

        #region 伤害处理

        private void DamageDrood(string fuelType, FlightFrameData frame, float DamageScale)
        {
            if (evaScript == null || PartScript == null || 
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
                Mod.Log("DamageDrood: null object found: - " +
                        $", evaScript={evaScript != null}, " +
                        $"PartScript={PartScript != null}, Game.Instance={Game.Instance != null}, Settings={Game.Instance?.Settings != null}");
                return;
            }

            if (frame.DeltaTimeWorld <= 0)
            {
                return;
            }   
            
            float num2 = (IsRunning ? 1.75f : 1f) * (IsTourist ? 1.05f : 1f) * DamageScale * (float)frame.DeltaTimeWorld;
            if ( 
                 (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                this.PartScript.TakeDamage(num2 * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>" + string.Format(Locale.GetString("Droodism.SupportLifeScript.CrewMemberDamage"),
                        evaScript.Data.CrewName, this.PartScript.Data.Id, fuelType,
                        Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((IsRunning ? 1.75 : 1) * (IsTourist ? 1.05 : 1) * DamageScale))),
                    false, 2f);
            }
        }
        
        private void DamageWaste(string resourceName, FlightFrameData frame, float DamageScale)
        {
            if (evaScript == null || PartScript == null || 
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
                return;
            }

            if (frame.DeltaTimeWorld <= 0)
            {
                return;
            }   
            
            float num2 = (IsRunning ? 1.75f : 1f) * (IsTourist ? 1.05f : 1f) * DamageScale * (float)frame.DeltaTimeWorld;
            
            if (
                 (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                this.PartScript.TakeDamage(num2 * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>" + string.Format(Locale.GetString("Droodism.SupportLifeScript.CrewMemberWasteDamage"),
                        evaScript.Data.CrewName, this.PartScript.Data.Id, resourceName,
                        Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((IsRunning ? 1.75 : 1) * (IsTourist ? 1.05 : 1) * DamageScale))),
                    false, 2f);
            }
        }
        
        private void DamageRadiation(in FlightFrameData  frame)
        {
            if (evaScript == null || PartScript == null)
            {
                return;
            } 

            float dtWorld = (float)frame.DeltaTimeWorld;
            if (dtWorld <= 0f)
            {
                return;
            }
            float impactScale = (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale;
            if (impactScale <= 0f)
            {
                return;
            }

            float damageMultiplier = 0f;
            string damageReason = "";
            if (this.RadiationDoseRateRadPerHour > 5f) // Only start checking for damage when rate exceeds 5 rad/h
            {
                float rateFactor = Mathf.Clamp(this.RadiationDoseRateRadPerHour / 50f, 0f, 4f);
                if (this.Data.CumulativeRad >= this.Data.RadiationDamageThresholdLevel3)
                {
                    damageMultiplier = 0.05f * (1 + rateFactor); // Severe damage amplified by rate
                    damageReason = "<color=red>severe cumulative radiation exposure</color>";
                }
                else if (this.Data.CumulativeRad >= this.Data.RadiationDamageThresholdLevel2)
                {
                    damageMultiplier = 0.001f * (1 + rateFactor); // Moderate damage amplified by rate
                    damageReason = "<color=orange>moderate cumulative radiation exposure</color>";
                }
                else if (this.Data.CumulativeRad >= this.Data.RadiationDamageThresholdLevel1)
                {
                    damageMultiplier = 0.00025f * (1 + rateFactor); // Mild damage amplified by rate
                    damageReason = "<color=yellow>mild cumulative radiation exposure</color>";
                }
                else
                {
                    // For low cumulative radiation, only high rates cause damage
                    if (this.RadiationDoseRateRadPerHour > 30f)
                    {
                        damageMultiplier = 0.05f * rateFactor; // Minor damage from high rate alone
                        damageReason = "<color=purple>high radiation rate exposure</color>";
                    }
                }
            }

            if (this.Data.CumulativeRad >= this.Data.RadiationDamageThresholdLevel3 && this.RadiationDoseRateRadPerHour <= 5f)
            {
                damageMultiplier = 0.05f;
                damageReason = "<color=red><size=120%>severe cumulative radiation exposure</color></size>";
            }

            if (damageMultiplier > 0f)
            {
                this.PartScript.TakeDamage(damageMultiplier * dtWorld * impactScale);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>Crew Member {evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage due to {damageReason} " +"<br>"+
                    $"he/she has {Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / (damageMultiplier * impactScale))} left",
                    false, 2f);
            }
        }
        
        private void PilotDamageReduction(in FlightFrameData frame)
        {
            if (this.Data.DroodismCrewData == null)
            {
                return;
            }
            if (this.Data.DroodismCrewData.CrewRole != DroodType.Pilot)
            {
                return;
            }

            if (evaScript.Data.GTolerance > 0.0 && evaScript.Data.GDamageScale > 0.0 &&
                (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                this.PartScript.TakeDamage((Mathf.Max(0.0f, this.evaScript.Gs - this.evaScript.Data.GTolerance) * frame.DeltaTime * this.evaScript.Data.GDamageScale * (float) (Setting<float>) Game.Instance.Settings.Game.Flight.ImpactDamageScale)*-0.4f, PartDamageType.GForce);
            }
        }

        #endregion

        #region 辐射计算

        private void LoadRadiationData()
        {
            if (Data.DroodismCrewData==null)
            {
                this.Data.CumulativeRad = 0;
                Mod.Log("Current Drood's DroodismCrewData is null ,return 0");
                return;
            }

            this.Data.CumulativeRad = this.Data.DroodismCrewData.RadiationRate;
        }

        internal void SaveDroodismCrewData(bool saveImmediately = true)
        {
            if (Data.DroodismCrewData==null)
            {
                return;
            }

            try
            {
                DroodismCrewDataManager.Instance.SetLifetimeRadiation(evaScript.Data.CrewId, this.Data.CumulativeRad,saveImmediately);
                DroodismCrewDataManager.Instance.AddMissionTime(evaScript.Data.CrewId, MissionDurationTime,saveImmediately);
            }
            catch (Exception e)
            {
                Scripts.Mod.Log("Droodism.SupportLifeScript.SaveDroodismCrewData"+e);
            }
        }

        /// <summary>
        /// 计算来自craft内部的辐射源
        /// </summary>
        private void CheckInCraftRadiationSource()
        {
            if (!ModSettings.Instance.ReceiveCraftRadiation)
            {
                return;
            }
            RTGParts.Clear();
            NTRParts.Clear();
            foreach (var partData in PartScript.CraftScript.Data.Assembly.Parts)
            {
                if (partData.PartType.Name == "Generator2")
                {
                    RTGParts.Add(partData);
                }
                if (partData.PartType.Name == "Rocket Engine")
                {
                    var rocketEngineScript = partData.PartScript.GetModifier<RocketEngineScript>();
                    if (rocketEngineScript!=null)
                    {
                        if ( rocketEngineScript.Data.EngineType.Name == "Nuclear Thermal")
                        {
                            NTRParts.Add(partData);
                        }
                    }
                }
            }
        }

        private float GetRTGRadiationDoseRate()
        {
            if (!ModSettings.Instance.ReceiveCraftRadiation||RTGParts.Count==0)
            {
                return 0;
            }

            float finalResult = 0;
            foreach (var partData in RTGParts)
            {
                float distance = Mathf.Clamp(Vector3.Distance(partData.PartScript.GameObject.transform.position, this.PartScript.GameObject.transform.position), 0.1f, 10f);
                finalResult +=(1/ (distance*distance))*0.2f;
            }
            return finalResult;
        }

        private float GetNTRRadiationDoseRate()
        {
            if (!ModSettings.Instance.ReceiveCraftRadiation||NTRParts.Count==0)
            {
                return 0;
            }
            float finalResult = 0;
            foreach (var partData in NTRParts)
            {
                if (partData.Activated)
                {
                    float distance = Mathf.Clamp(Vector3.Distance(partData.PartScript.GameObject.transform.position, this.PartScript.GameObject.transform.position), 0.1f, 10f);
                    finalResult +=(1/ (distance*distance))*0.2f;
                }
            }
            return finalResult;
        }

        private void RefreshRadiationCompartment()
        {
            var eva = this.PartScript?.GetModifier<EvaScript>();
            
            if (eva.EvaActive||eva.ActiveWhileInCrewCompartment)
            {
                innerRadiationProtection= outerRadiationProtection =craftRadiationProtection = 0;
                return;
            }
          
            var crewCabin = eva.CrewCompartment?.PartScript.GetModifier<CrewCabinScript>();
            if (crewCabin!=null)
            {
                outerRadiationProtection =crewCabin.Data.RadiationShieldDuration>0? crewCabin.GetOuterRadiationProtection():0;
                innerRadiationProtection=crewCabin.Data.RadiationShieldDuration>0? crewCabin.GetInnerRadiationProtection():0;
                craftRadiationProtection = crewCabin.Data.RadiationShieldDuration > 0 ? crewCabin.GetCraftRadiationProtection() : 0;
            }

            if (crewCabin==null)
            {
                innerRadiationProtection= outerRadiationProtection =craftRadiationProtection = 0;
            }
        }
        
        /// <summary>
        /// 对辐射剂量计算
        /// </summary>
        private void CheckRadiationState(in FlightFrameData data)
        {
            RadiationBeltManager.Instance.TryGetDoseRateRadPerHour(this.RadiationBeltConfig,
                currentPlanetName,
                PartScript.CraftScript.FlightData.Position.ToVector3(),
                out var totalRadiationDoseRateRadPerHour,
                out var innerDoseRateRadPerHour,
                out var outerDoseRateRadPerHour);
            float deltaHours = Mathf.Max(0f, (float)data.DeltaTimeWorld) / 3600f;
            if (deltaHours <= 1e-9f)
            {
                deltaHours = Mod.GetDeltaTimeHours();
            }

            this.RadiationDoseRateRadPerHour = innerDoseRateRadPerHour*(1-innerRadiationProtection)+outerDoseRateRadPerHour*(1-outerRadiationProtection)+GetNTRRadiationDoseRate()*(1-craftRadiationProtection)+GetRTGRadiationDoseRate()*(1-craftRadiationProtection);
            this.Data.CumulativeRad += RadiationDoseRateRadPerHour * deltaHours;
            CurrentCumulativeRadiationStats = GetAcuteBand((float)this.Data.CumulativeRad);
            CurrentRadiationRateStats = GetRadiationRateStats(RadiationDoseRateRadPerHour);
        }
       
        private static string GetAcuteBand(float cumulativeDoseRad)
        {
            if (cumulativeDoseRad >= 500f)
                return"<color=red>Critical";
            if (cumulativeDoseRad >= 200f)
                return"<color=orange>Severe";
            if (cumulativeDoseRad >= 100f)
                return"<color=yellow>Mild";
            return"<color=green>nominal";
        }

        private static string GetRadiationRateStats(float rate)
        {
            if (rate > 10f)
                return"<color=red>Critical";
            if (rate > 5f)
                return"<color=orange>Severe"; 
            if (rate > 1f)
                return"<color=yellow>Mild";
            return"<color=green>nominal";
        }

        #endregion

        #region UI

        /// <summary>
        /// 为零件生成inspector model，添加生命支持信息。
        /// Generates the inspector model for the part, adding life support information.
        /// </summary>
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            //单独看任务时间的
            if (!this.IsTourist)
            {
                model.Add<TextModel>(new TextModel("<color=yellow>" + Locale.GetString("Droodism.SupportLifeScript.CrewRole"), (Func<string>) (() =>Data.DroodismCrewData==null?Locale.GetString("Droodism.SupportLifeScript.Unknown"):Data.DroodismCrewData.CrewRole.ToString())));
            }
            model.Add<TextModel>(new TextModel("<color=yellow>" + Locale.GetString("Droodism.SupportLifeScript.MissionTime"), (Func<string>) (() =>Units.GetStopwatchTimeString(MissionDurationTime))));
            //维生资源
            GroupModel lifeSupportGroupModel = new GroupModel("<color=green><size=115%>" + Locale.GetString("Droodism.SupportLifeScript.LifeSupportInfo"));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.RemainOxygen"), (Func<string>) (() =>
            {
                if (UsingInternalOxygen())
                {
                    float percentage = (float)(Data._oxygenAmountBuffer / Data.DesireOxygenCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>{Units.GetPercentageString(percentage)}</color>";
                }
                else if (!UsingInternalOxygen())
                {
                    return "<color=green>" + Locale.GetString("Droodism.SupportLifeScript.UsingExternalOxygen") + "</color>";
                }
                return "<color=purple>" + Locale.GetString("Droodism.SupportLifeScript.NotAvailable") + "</color>";
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.OxygenSupplyTime"), (Func<string>) (() =>
            {
                if (UsingInternalOxygen())
                {
                    float percentage = (float)(Data._oxygenAmountBuffer / Data.DesireOxygenCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>"+Units.GetStopwatchTimeString(Data._oxygenAmountBuffer / (Data.OxygenConsumeRate * (IsRunning ? 1.75 : 1) * (IsTourist ? 1.05 : 1)));
                }
                else if (!UsingInternalOxygen())
                {
                    return "<color=green>" + Locale.GetString("Droodism.SupportLifeScript.Infinity") + "</color>";
                }
                return Locale.GetString("Droodism.SupportLifeScript.NotAvailable");
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.RemainWater"), (Func<string>) (() =>
            {
                float waterPercentage = (float)(Data._waterAmountBuffer / Data.DesireWaterCapacity);
                string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                return $"<color={waterTextColor}>{Units.GetPercentageString(waterPercentage)}</color>";
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.WaterSupplyTime"), (Func<string>) (() =>
            {
                float waterPercentage = (float)(Data._waterAmountBuffer / Data.DesireWaterCapacity);
                string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                return $"<color={waterTextColor}>"+Units.GetStopwatchTimeString(Data._waterAmountBuffer / (Data.WaterConsumeRate * (IsRunning ? 1.75 : 1) * (IsTourist ? 1.05 : 1)));
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.RemainFood"), (Func<string>) (() =>
            {
                float foodPercentage = (float)(Data._foodAmountBuffer / Data.DesireFoodCapacity);
                string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                return $"<color={foodTextColor}>{Units.GetPercentageString(foodPercentage)}</color>";
            })));
        
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.FoodSupplyTime"), (Func<string>) (() =>
            {
                float foodPercentage = (float)(Data._foodAmountBuffer / Data.DesireFoodCapacity);
                string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                return $"<color={foodTextColor}>"+Units.GetStopwatchTimeString(Data._foodAmountBuffer / (Data.FoodConsumeRate * (IsRunning ? 1.75 : 1) * (IsTourist ? 1.05 : 1)));
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.CO2Level"), (Func<string>) (() =>
            {
                if (UsingInternalOxygen())
                {
                    float Percentage = (float)(Data._co2AmountBuffer / Data.DesireCO2Capacity);
                    string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                    return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
                }
                else
                {
                    return "<color=green>" + Locale.GetString("Droodism.SupportLifeScript.UsingExternalOxygen") + "</color>";
                }
            })));
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.WastedWaterLevel"), (Func<string>) (() =>
            {
                float Percentage = (float)(Data._wastedWaterAmountBuffer / Data.DesireWastedWaterCapacity);
                string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
            })));
            lifeSupportGroupModel.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.SolidWasteLevel"), (Func<string>) (() =>
            {
                float Percentage = (float)(Data._solidWasteAmountBuffer / Data.DesireSolidWasteCapacity);
                string color = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                return $"<color={color}>{Units.GetPercentageString(Percentage)}</color>";
            })));
            
            model.AddGroup(lifeSupportGroupModel);

            //辐射强度
            GroupModel RadiationInspector = new GroupModel("<color=yellow><size=115%>" + Locale.GetString("Droodism.SupportLifeScript.RadiationInspector"));
            RadiationInspector.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.CurrentRadiationDose"), (Func<string>) (() => $"{this.Data.CumulativeRad:F4} rad"),determineVisibility:() => this.Data.CumulativeRad >0));
            RadiationInspector.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.CurrentRadiationDoseStats"), (Func<string>) (() => CurrentCumulativeRadiationStats),determineVisibility:() => this.Data.CumulativeRad >0));
            RadiationInspector.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.CurrentRadiationDoseRatePerHour"), (Func<string>) (() => $"{this.RadiationDoseRateRadPerHour:F2} rad/h"),determineVisibility:() => this.RadiationDoseRateRadPerHour >0));
            RadiationInspector.Add<TextModel>(new TextModel(Locale.GetString("Droodism.SupportLifeScript.CurrentRadiationLevel"), (Func<string>) (() => $"{CurrentRadiationRateStats}"),determineVisibility:() => this.RadiationDoseRateRadPerHour >0));
            
            model.AddGroup(RadiationInspector);

            //插旗与开伞
            if (!IsTourist)
            {
                if (Data.ParachuteTypes=="ParaGlider")
                {
                    model.Add(new ToggleModel(Locale.GetString("Droodism.SupportLifeScript.AutoDeployParaGlider"),()=>Data.AutoDeployEnabled,b=>
                    {
                        Data.AutoDeployEnabled=b;
                    },Locale.GetString("Droodism.SupportLifeScript.EnableAutoDeployment")));
                    model.Add(new SliderModel(Locale.GetString("Droodism.SupportLifeScript.AutoDeployHeight"), (Func<float>) (() => this.Data.AutoDeployHeight), (Action<float>) (s => this.Data.AutoDeployHeight = s), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                  
                    model.Add(new SliderModel(Locale.GetString("Droodism.SupportLifeScript.FullyDeployHeight"), (Func<float>) (() => Mathf.Min(Data.MinDeployHeight, Data.AutoDeployHeight)), (Action<float>) (s =>
                    {
                        this.Data.MinDeployHeight = Mathf.Min(s, Data.AutoDeployHeight);
                    }), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                    model.Add<TextButtonModel>(new TextButtonModel("<color=red>" + Locale.GetString("Droodism.SupportLifeScript.ManualDeployParaGlider"), (Action<TextButtonModel>)(b => this.DeployParaglider())));
                }

                if (Data.ParachuteTypes=="Parachute")
                {
                    model.Add(new ToggleModel(Locale.GetString("Droodism.SupportLifeScript.AutoDeployParachute"),()=>Data.AutoDeployEnabled,b=>
                    {
                        Data.AutoDeployEnabled=b;
                    },Locale.GetString("Droodism.SupportLifeScript.EnableAutoDeployment")));
                    model.Add(new SliderModel(Locale.GetString("Droodism.SupportLifeScript.AutoDeployHeight"), (Func<float>) (() => this.Data.AutoDeployHeight), (Action<float>) (s => this.Data.AutoDeployHeight = s), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                    model.Add<TextButtonModel>(new TextButtonModel("<color=red>" + Locale.GetString("Droodism.SupportLifeScript.ManualDeployParachute"), (Action<TextButtonModel>)(b => this.DeployParaglider())));
                    model.Add(new SliderModel(Locale.GetString("Droodism.SupportLifeScript.FullyDeployHeight"), (Func<float>) (() => Mathf.Min(Data.MinDeployHeight, Data.AutoDeployHeight)), (Action<float>) (s =>
                    {
                        this.Data.MinDeployHeight = Mathf.Min(s, Data.AutoDeployHeight);
                    }), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                }
            }
            //特殊能力这一块

            if (IsTourist||this.Data.DroodismCrewData==null)
            {
                return;
            }

            if (this.Data.DroodismCrewData.CrewRole == DroodType.Pilot)
            {
                model.Add<TextButtonModel>(new TextButtonModel(Locale.GetString("Droodism.SupportLifeScript.PlantFlag"), (Action<TextButtonModel>)(b => this.PlantFlagClick())));
            }

            if (this.Data.DroodismCrewData.CrewRole == DroodType.Engineer)
            {
                model.Add(new TextModel(Locale.GetString("Droodism.SupportLifeScript.RemainRepairingTools"),(Func<string>) (() => $"{this.Data.UtilizationFactor:F1}")));
                model.Add(new TextButtonModel(Locale.GetString("Droodism.SupportLifeScript.Repair"), (Action<TextButtonModel>)(b => { this.RepairPart(); })));
            }
        }

        #endregion

        #region 插旗与开伞

        private void AutoDeployParachute()
        {
            bool isEva()
            {
                if (evaScript.EvaActive)
                {
                    return !evaScript.ActiveWhileInCrewCompartment;
                }return evaScript.PartScript.CraftScript.Data.Assembly.Parts.Count == 1 && evaScript.PartScript.CraftScript.RootPart.Data.PartType.Name.Contains("Eva");
            }
            if (!isEva()||evaScript.IsGrounded||evaScript.IsInWater||evaScript.PartScript.CraftScript.FlightData.AltitudeAboveGroundLevel<=10||evaScript.PartScript.CraftScript.FlightData.AtmosphereSample.AirDensity<=0.01||evaScript.PartScript.CraftScript.FlightData.SurfaceVelocityMagnitude >= evaScript.PartScript.CraftScript.FlightData.AtmosphereSample.SpeedOfSound||evaScript.PartScript.CraftScript.FlightData.VerticalSurfaceVelocity>0)
            {
                return;
            }

            if (evaScript.PartScript.CraftScript.FlightData.AltitudeAboveGroundLevel>Data.AutoDeployHeight)
            {
                return;
            }
            DeployParaglider();
        }

        private void PlantFlagClick()
        {
            ICraftScript craftScript = this.PartScript.CraftScript;
            IFlightSceneUI ui = ModApi.Common.Game.Instance.FlightScene.FlightSceneUI;
            if (!(craftScript.Data.Assembly.Parts.Count == 1 &&craftScript.RootPart.Data.PartType.Name.Contains("Eva"))&&evaScript.ActiveWhileInCrewCompartment)
            {
                ui.ShowMessage(Locale.GetString("Droodism.FlagPlanting.CannotPlantFlagNotInEva"),false,6);
                return;
            }
            if (craftScript.FlightData.Grounded==false)
            {
                ui.ShowMessage(Locale.GetString("Droodism.FlagPlanting.CannotPlantFlagNotGrounded"),false,6);
                return;
            }
            if (craftScript.FlightData.SurfaceVelocityMagnitude>=1)
            {
                ui.ShowMessage(Locale.GetString("Droodism.FlagPlanting.CannotPlantFlagVelocityHigh"),false,6);
                return;
            }

            if (evaScript.IsInWater)
            {
                ui.ShowMessage(Locale.GetString("Droodism.FlagPlanting.CannotPlantFlagInWater"),false,6);
                return;
            }

            if (this.Data.UtilizationFactor<300)
            {
                ui.ShowMessage(Locale.GetString("Droodism.FlagPlanting.CannotPlantFlagAlreadyPlanted"),false,6);
                return;
            }

            Data.UtilizationFactor = 0f;
            Mod.Instance.SpawnFlag();
        }
        
        public void DeployParaglider()
        {
            ICraftScript craftScript = this.PartScript.CraftScript;
            IFlightSceneUI ui = ModApi.Common.Game.Instance.FlightScene.FlightSceneUI;

            bool isEva()
            {
                if (evaScript.EvaActive)
                {
                    return !evaScript.ActiveWhileInCrewCompartment;
                }return craftScript.Data.Assembly.Parts.Count == 1 && craftScript.RootPart.Data.PartType.Name.Contains("Eva");
            }
            if (!isEva())
            {
                ui.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.CannotDeployParachuteNotInEva"),false,6));
                return;
            }
            if (evaScript.IsGrounded)
            {
                ui.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.CannotDeployParachuteGrounded"),false,6));
                return;
            }

            if (craftScript.FlightData.AltitudeAboveGroundLevel<=10)
            {
                ui.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.CannotDeployParachuteHere"),false,6));
                return;
            }
            if (craftScript.FlightData.AtmosphereSample.AirDensity<=0.01)
            {
                ui.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.CannotDeployParachuteAirThin"),false,6));
                return;
            }

            if (craftScript.FlightData.SurfaceVelocityMagnitude >=
                craftScript.FlightData.AtmosphereSample.SpeedOfSound)
            {
                ui.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.CannotDeployParachuteSpeedHigh"),false,6));
                return;
            }
            
            if (evaScript.IsInWater)
            {
                ui.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.CannotDeployParachuteInWater"),false,6));
                return;
            }
            
            CraftScript craftScript1 = this.PartScript.CraftScript as CraftScript;
            craftScript1.RecenterTransformOnCoM(true);
            SpawnParachuteAndRide();
        }

        /// <summary>
        /// 情况太鸡巴诡异了你知道吗?
        /// </summary>
        private void SpawnParachuteAndRide()
        {
            Vector3 orgVelocity = this.PartScript.BodyScript.RigidBody.velocity;
            Vector3 orgAngularVelocity = this.PartScript.BodyScript.RigidBody.angularVelocity;
            
            Vector3 orgEularAngle=this.PartScript.Transform.eulerAngles;
            var parachutePartScript = CreateParachutePartScript();
            #region 我不想看
         
            BodyData body = CraftBuilder.CreateBodyData(new List<PartData>(){parachutePartScript.Data}, PartScript.CraftScript.Transform);
            CraftScript craftScript = PartScript.CraftScript as CraftScript;
            craftScript?.Data.Assembly.AddBody(body);
            BodyScript bodyScript = CraftBuilder.CreateBodyScript(craftScript, body);
            bodyScript.MoveToCraft(craftScript);
            bodyScript.OnInitialized();
        
            // 创建 Group 并设置 parent
            PartGroupScript groupScript = CreatePopPartGroup(bodyScript, parachutePartScript, this.PartScript.Data.Id + 1);
            #endregion

            CraftBuilder.CalculateInertiaTensors(bodyScript, false);
            evaScript.OnPreNodeLoaded();
            
            parachutePartScript.BodyScript.RigidBody.velocity = orgVelocity;
            parachutePartScript.Transform.eulerAngles = orgEularAngle;
            parachutePartScript.BodyScript.RigidBody.angularVelocity = orgAngularVelocity;
            evaScript.LoadIntoCrewCompartment(parachutePartScript.GetModifier<CrewCompartmentScript>(), null, announceBoarding: false);
        }

        private PartScript CreateParachutePartScript()
        {
            Assembly assembly=new Assembly(ParachutePartElement(), 15, Game.Instance.PartTypes);
            CraftBuilder.CreatePartGameObjects(assembly.Parts, PartScript.CraftScript);
            PartData part = assembly.Parts[0];
            PartScript partScript = part.PartScript as PartScript;
            partScript?.UpdateAttachPoints();
            PartScript.CraftScript.Data.Assembly.Absorb(assembly);
            return partScript;
        }

        private XElement ParachutePartElement()
        {
            DesignerPart designerpart = Game.Instance.CachedDesignerParts.Parts.First(d => d.Name == (Data.ParachuteTypes=="ParaGlider"?"Glider":"DroodParachute"));
            XElement assembly = new XElement("Assembly", designerpart.AssemblyElement.Element("Parts"));
            return assembly;
        }

        private PartGroupScript CreatePopPartGroup(BodyScript bodyScript, PartScript partScript, int id)
        {
            GameObject gameObject = new GameObject("PartGroup");
            PartGroupScript partGroupScript = gameObject.AddComponent<PartGroupScript>();
            partGroupScript.gameObject.name = $"PartGroup-{id}";
            partGroupScript.Id = id;
            partGroupScript.BodyScript = bodyScript;
            partGroupScript.transform.parent = bodyScript.transform;
            partGroupScript.transform.localPosition = bodyScript.transform.localPosition;
            partGroupScript.transform.localScale = Vector3.one;
            partGroupScript.transform.rotation = Quaternion.identity;
            partScript.transform.SetParent(partGroupScript.transform, worldPositionStays: false);
            partScript.AssignToPartGroup(partGroupScript);
            partGroupScript.Data.Parts.Add(partScript.Data);
            return partGroupScript;
        }

        #endregion

        #region 零件修复

        private void RepairPart()
        {
            var part = Game.Instance.FlightScene.ViewManager.GameView.SelectedPart;
            if (part==null)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.SupportLifeScript.NoPartSelected"), false, 3f);
                isRepairing = false;
                return;
            }
            
            if (part.Data.PartType.Name==("Eva")||part.Data.PartType.Name==("Eva-Tourist"))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.SupportLifeScript.CannotRepairDrood"), false, 3f);
                isRepairing = false;
                return;
            }
            if ((part.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.PartTooFarToRepair"), part.Data.PartType.Name), false, 3f);
                isRepairing = false;
                return;
            }

            if (part.Data.Damage <= 0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.PartDoesNotNeedRepair"), part.Data.PartType.Name), false, 3f);
                isRepairing = false;
                return;
            }

            isRepairing = true;
        }

        private void RepairPartWorkingLogic(in FlightFrameData data,PartData pd)
        {
            if (pd==null)
            {
                isRepairing = false;
                return;
            }
            if (pd.PartType.Name==("Eva")||pd.PartType.Name==("Eva-Tourist"))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.SupportLifeScript.CannotHealDrood"), false, 3f);
                isRepairing = false;
                return;
            }
            if ((pd.PartScript.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.RepairInterruptedTooFar"), pd.PartType.Name), false, 3f);
                isRepairing = false;
                return;
            }
            
            if (pd.Damage<=0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.RepairCompleted"), pd.PartType.Name), false, 3f);
                isRepairing = false;
                return;
            }

            if (Data.UtilizationFactor<=0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.RepairInterruptedNoTools"), pd.PartType.Name), false, 4f);
                isRepairing = false;
                return;
            }
            
            if (Data.UtilizationFactor>0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.SupportLifeScript.RepairProgress"), pd.PartType.Name, Units.GetPercentageString(Mathf.Clamp01((100f - pd.Damage) / 100f))), false, 3f);
                float num = (float)data.DeltaTimeWorld * 2f;
                Data.UtilizationFactor -=num ;
                pd.Damage -= num;
            }
        }

        #endregion

        #region XML工具

        /// <summary>
        /// 防止以前遗留的FuelTank发癫,移除
        /// </summary>
        private static XElement RemoveFuelTankXML(XElement partElement)
        {
            if (partElement == null)
            {
               Mod.Log("Part element is null.");
                return null;
            }

            try
            {
                string[] fuelTypesToRemove = { "Oxygen", "Food", "H2O","CO2","Wasted Water","Solid Waste" };
                var tanksToRemove = partElement.Elements("FuelTank")
                    .Where(fuelTank => fuelTypesToRemove.Contains(fuelTank.Attribute("fuelType")?.Value))
                    .ToList();

                if (tanksToRemove.Any())
                {
                    foreach (var tank in tanksToRemove)
                    {
                       Mod.Log($"Removing FuelTank with fuelType={tank.Attribute("fuelType")?.Value}");
                        tank.Remove();
                    }
                }
                else
                {
                 
                }

                return partElement;
            }
            catch (Exception ex)
            {
               Mod.Log($"Error removing FuelTank nodes: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
