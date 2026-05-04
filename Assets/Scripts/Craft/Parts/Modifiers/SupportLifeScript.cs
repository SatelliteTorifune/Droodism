using ModApi.Craft;
using ModApi.Craft.Parts;
using Assets.Scripts.Flight;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Droodism;
using Assets.Scripts.Droodism.Crew;
using Droodism.RadiationBelt;
using ModApi.Flight.Events;
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

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class SupportLifeScript : 
        PartModifierScript<SupportLifeData>,
        IFlightStart,
        IFlightUpdate,
        IFlightFixedUpdate
    {
        #region 引用属性字段
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


        public IFuelSource OxygenLocalSource { get; private set; }
        public IFuelSource WaterLocalSource { get; private set; }
        public IFuelSource FoodLocalSource { get; private set; }
        public IFuelSource Co2LocalSource { get; private set; }
        public IFuelSource WastedWaterLocalSource { get; private set; }
        public IFuelSource SolidWasteLocalSource { get; private set; }
        
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
        public bool isRunning, isTourist;
       

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

        //累计辐射值状态
        public string CurrentCumulativeRadiationStats{ get; private set; }
        //辐射值速率
        public string CurrentRadiationRateStats{ get; private set; }

        private float outerRadiationProtection;
        private float innerRadiationProtection;
        #endregion

        #region 逻辑循环啥的
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
            base.OnInitialLaunch();
            Data.MissionStartTime = (long)Game.Instance.FlightScene.FlightState.Time;
            Mod.Log("OnInitialLaunch");
            base.OnInitialLaunch();
            Data._foodAmountBuffer=this.Data.DesireFoodCapacity;
            Data._oxygenAmountBuffer=this.Data.DesireOxygenCapacity;
            Data._waterAmountBuffer=this.Data.DesireWaterCapacity;
            Data._co2AmountBuffer=0;
            Data._wastedWaterAmountBuffer=0;
            Data._solidWasteAmountBuffer=0;
            try
            {
                Refresh();
                Mod.Log("OnInitialLaunch调用RefreshFuelSource");
            }
            catch (Exception e)
            {
                Mod.Log("OnInitialLaunch调用RefreshFuelSource出问题了{0}", e);
            }
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
                isTourist = true;
            }
            evaScript = this.PartScript.GetModifier<EvaScript>();
            UpdateCurrentPlanet();
            
            LoadFuelTanks();
            Mod.Log("FlightStart调用LoadFuelTanks");
            
            //我他妈没在OnInitialLaunch里implement这个函数是为了方便你们这群小逼崽子瞎鸡巴改xml乱搞你们知道吗
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
                if (!UsingInternalOxygen() )   
                {
                    AutoRefillLogic(frame);
                }
                ConsumptionLogic(frame);
            }
            if (ModSettings.Instance.ActiveUpdateRadiationBeltConfig)
            {
                this.RadiationBeltConfig = RadiationBeltManager.Instance.GetRuntimeConfigForPlanet(currentPlanetName);
            }
            
            MissionDurationTime = (long)Game.Instance.FlightScene.FlightState.Time - Data.MissionStartTime;
            if (isRepairing)
            {
                RepairPartWorkingLogic(frame,Game.Instance.FlightScene.ViewManager.GameView.SelectedPart.Data );
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
        #endregion
        
        #region 休眠跑步状态更新
        
        /// <summary>
        /// 这b玩意看不懂那你去吃我屎吧,你不会百度翻译吗?
        /// </summary>
        private void UpdateRunningStatus()
        {
            if (evaScript.EvaActive && evaScript.IsPlayerCraft && !evaScript.IsWalking && evaScript.IsGroundedTerrain && PartScript.CraftScript.SurfaceVelocity.magnitude >= 0.8)
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }
        }

        /// <summary>
        /// 检测这这个小蓝人能不能被治疗
        /// </summary>
        private void UpdateHealingStatus()
        {
            if (OxygenLocalSource == null || FoodLocalSource == null || WaterLocalSource == null ||
                Co2LocalSource == null || WastedWaterLocalSource == null || SolidWasteLocalSource == null)
            {
                this.CanHeal = false;
                return;
            }

            bool lackInput = OxygenLocalSource.IsEmpty || FoodLocalSource.IsEmpty || WaterLocalSource.IsEmpty;
            bool wasteIsFull = Co2LocalSource.TotalCapacity - Co2LocalSource.TotalFuel <= 0.00001 ||
                              WastedWaterLocalSource.TotalCapacity - WastedWaterLocalSource.TotalFuel <= 0.00001 ||
                              SolidWasteLocalSource.TotalCapacity - SolidWasteLocalSource.TotalFuel <= 0.00001;
            bool severeRadiation = this.Data.CumulativeRad >= this.Data.RadiationDamageThresholdLevel3;

            this.CanHeal = !(lackInput || wasteIsFull || severeRadiation);
        }
        public override void OnPartDestroyed()
        {
            base.OnPartDestroyed();
            if (!Assets.Scripts.Game.InFlightScene)
                return;
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
        #endregion
        
        #region 资源查找消耗补充相关函数
        /// <summary>
        /// 从零件的modifiers中检索指定燃料类型的本地燃料源。
        /// Retrieves the local fuel source for the specified fuel type from the part's modifiers.
        /// </summary>
        /// <param name="fuelType">要查找的燃料类型。Type of fuel to find.</param>
        /// <returns>如果找到则返回燃料源，否则返回null。The fuel source if found, otherwise null.</returns>
        /// 一般来说这不太可能返回Null,我他妈花了那么多时间写了给小蓝人添加modifier的函数
        private IFuelSource GetLocalFuelSource(string fuelType)
        {
            try
            {
                var craftSources = PartScript.Modifiers;
                foreach (var source in craftSources)
                {
                    if (source.GetData().Name.Contains("Tank"))
                    {
                        source.GetData().InspectorEnabled = false;
                        FuelTankScript fts = source as FuelTankScript;
                        if (fts == null)
                        {
                            return null;
                        }
                        if (fts.FuelType.Id==fuelType)
                        {
                            return fts;
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Mod.Log("GetLocalFuelSource出问题了{0}", e);
            }
            
            return null;
        }

        /// <summary>
        /// 处理氧气、食物和水的消耗逻辑。
        /// Handles the consumption logic for oxygen, food, and water.
        /// 你可能觉得这个函数也太不优雅了,对,因为氧气这个b玩意比较特殊我要单独处理
        /// /// </summary>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        private void ConsumptionLogic(in FlightFrameData frame)
        {
            if (OxygenLocalSource == null || FoodLocalSource == null || WaterLocalSource == null ||
                Co2LocalSource == null || WastedWaterLocalSource == null || SolidWasteLocalSource == null)
            {
                if (!_isCraftLoading)
                {
                    LoadFuelTanks();
                    Refresh();
                    Mod.Log("ConsumptionLogic缺少local source，已调用LoadFuelTanks和Refresh");
                }
            }

            bool usingInternalOxygen = UsingInternalOxygen();
            double baseRate = frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1);

            
            if (usingInternalOxygen)
            {
                double oxygenConsumeAmount = (double)Data.OxygenConsumeRate * baseRate;
                bool oxygenConsumed = ConsumeInputResource(OxygenSource, OxygenLocalSource, oxygenConsumeAmount, frame, Data.OxygenDamageScale);
                if (oxygenConsumed)
                {
                    double co2Amount = oxygenConsumeAmount * 1.375 * Data.evaConsumeEfficiency;
                    ProduceWasteResource(Co2Source, Co2LocalSource, co2Amount, frame, Data.OxygenDamageScale);
                }
            }

            double foodConsumeAmount = (double)Data.FoodConsumeRate * baseRate;
            bool foodConsumed = ConsumeInputResource(FoodSource, FoodLocalSource, foodConsumeAmount, frame, Data.FoodDamageScale);
            if (foodConsumed)
            {
                double solidWasteAmount = foodConsumeAmount * 1.1 * Data.evaConsumeEfficiency * 0.04;
                ProduceWasteResource(SolidWasteSource, SolidWasteLocalSource, solidWasteAmount, frame, Data.FoodDamageScale);
            }

            double waterConsumeAmount = (double)Data.WaterConsumeRate * baseRate;
            bool waterConsumed = ConsumeInputResource(WaterSource, WaterLocalSource, waterConsumeAmount, frame, Data.WaterDamageScale);
            if (waterConsumed)
            {
                double wastedWaterAmount = waterConsumeAmount * 1.1 * Data.evaConsumeEfficiency;
                ProduceWasteResource(WastedWaterSource, WastedWaterLocalSource, wastedWaterAmount, frame, Data.WaterDamageScale);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="craftSource"></param>
        /// <param name="localSource"></param>
        /// <param name="amount"></param>
        /// <param name="frame"></param>
        /// <param name="damageScale"></param>
        /// <returns></returns>

        private bool ConsumeInputResource(IFuelSource craftSource, IFuelSource localSource, double amount, in FlightFrameData frame, float damageScale)
        {
            if (amount <= 0)
            {
                return false;
            }
            //判断用哪个IFuelSource
            //如果craftSource存在且非空就用这个
            IFuelSource source = craftSource != null && !craftSource.IsEmpty ? craftSource : localSource;
            if (source == null)
            {
                if (craftSource != null)
                {
                    DamageDrood(craftSource, frame, damageScale);
                }
                else if (localSource != null)
                {
                    DamageDrood(localSource, frame, damageScale);
                }
                return false;
            }

            if (source.IsEmpty)
            {
                DamageDrood(source, frame, damageScale);
                return false;
            }

            source.RemoveFuel(amount);
            return true;
        }

        private void ProduceWasteResource(IFuelSource craftSource, IFuelSource localSource, double amount, in FlightFrameData frame, float damageScale)
        {
            if (amount <= 0)
            {
                return;
            }

            IFuelSource target = craftSource != null && craftSource.TotalCapacity - craftSource.TotalFuel > 0.00001
                ? craftSource
                : localSource;

            if (target == null)
            {
                if (craftSource != null)
                {
                    DamageWaste(craftSource, frame, damageScale);
                }
                else if (localSource != null)
                {
                    DamageWaste(localSource, frame, damageScale);
                }
                return;
            }

            if (target.TotalCapacity - target.TotalFuel <= 0.00001)
            {
                DamageWaste(target, frame, damageScale);
                return;
            }

            target.AddFuel(amount);
        }

        /// <summary>
        /// 处理不使用自带氧气时的氧气自动补充逻辑。
        /// Handles the auto-refill logic for oxygen when not using internal oxygen.
        /// </summary>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        private void AutoRefillLogic(in FlightFrameData frame)
        {
            var LocalFuelSource = GetLocalFuelSource("Oxygen");

            if (LocalFuelSource?.TotalCapacity - LocalFuelSource?.TotalFuel >= 0.00001)
            {
                LocalFuelSource?.AddFuel(LocalFuelSource.TotalCapacity * frame.DeltaTimeWorld * 0.01f);
            }
        }

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
                    RefreshRadiationCompartment();
                }
                catch (Exception e)
                {
                }
                
            }
        }
        
        /// <summary>
        /// 在非EVA模式下刷新燃料源。
        /// Refreshes fuel sources when not in EVA mode.
        /// </summary>
        /// 这个吊毛函数太复杂了我得花点时间说一下这玩意到底原理是什么不然我自己忘了
        /// 这个函数的目的非常简单,调用的时候如果是Eva状态就把各个source设定为本地modifier,如果不是就用craft的fuelsource
        /// 但是后面那一坨就出问题了,目前的逻辑是我管你这哪先用craft的,得到null自然就会切换到设置本地modifier那一坨
        /// 这个鸡巴卵子函数的trycatch瞎他妈乱飞,但是I don't give a sh1t,反正it works(on my machine)
        private void RefreshFuelSource()
        {
            bool isEva = IsActiveEvaOutsideCompartment();
            
            if (isEva)
            {
                IsHibernating = false;
                ClearCraftFuelSources();
            }
            else
            {
                RefreshCraftFuelSources();
            }

            RefreshLocalFuelSourceCache();
            SyncCraftAndLocalFuelSources();

            OxygenSource = SelectActiveFuelSource("Oxygen", OxygenSource, OxygenLocalSource, false);
            FoodSource = SelectActiveFuelSource("Food", FoodSource, FoodLocalSource, false);
            WaterSource = SelectActiveFuelSource("H2O", WaterSource, WaterLocalSource, false);
            Co2Source = SelectActiveFuelSource("CO2", Co2Source, Co2LocalSource, true);
            WastedWaterSource = SelectActiveFuelSource("Wasted Water", WastedWaterSource, WastedWaterLocalSource, true);
            SolidWasteSource = SelectActiveFuelSource("Solid Waste", SolidWasteSource, SolidWasteLocalSource, true);

            SaveFuelAmountBuffer();
            bool IsActiveEvaOutsideCompartment()
            {
                return PartScript?.CraftScript?.ActiveCommandPod?.Part?.PartScript == PartScript && !evaScript.ActiveWhileInCrewCompartment;
            }
    
            void ClearCraftFuelSources()
            {
                OxygenSource = null;
                WaterSource = null;
                FoodSource = null;
                Co2Source = null;
                WastedWaterSource = null;
                SolidWasteSource = null;
            }
    
            void RefreshCraftFuelSources()
            {
                try
                {
                    var patch = PartScript.GetModifier<EvaScript>().CrewCompartment?.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
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
    
            void RefreshLocalFuelSourceCache()
            {
                OxygenLocalSource = GetLocalFuelSource("Oxygen");
                FoodLocalSource = GetLocalFuelSource("Food");
                WaterLocalSource = GetLocalFuelSource("H2O");
                Co2LocalSource = GetLocalFuelSource("CO2");
                WastedWaterLocalSource = GetLocalFuelSource("Wasted Water");
                SolidWasteLocalSource = GetLocalFuelSource("Solid Waste");
            }
            
            void SyncCraftAndLocalFuelSources()
            {
                TrySyncPair(OxygenSource, OxygenLocalSource, false);
                TrySyncPair(FoodSource, FoodLocalSource, false);
                TrySyncPair(WaterSource, WaterLocalSource, false);
                TrySyncPair(Co2Source, Co2LocalSource, true);
                TrySyncPair(WastedWaterSource, WastedWaterLocalSource, true);
                TrySyncPair(SolidWasteSource, SolidWasteLocalSource, true);
            }
            
            void TrySyncPair(IFuelSource craftSource, IFuelSource localSource, bool wasteMode)
            {
                if (craftSource == null || localSource == null)
                {
                    return;
                }
    
                if (wasteMode)
                {
                    if (craftSource.TotalCapacity - craftSource.TotalFuel > 0.00001 && localSource.TotalFuel > 0.00001)
                    {
                        RemoveWaste(craftSource, localSource);
                    }
                    return;
                }
                if (!craftSource.IsEmpty && localSource.TotalCapacity - localSource.TotalFuel > 0.00001)
                {
                    ReFill(craftSource, localSource);
                }
    
               
            }
    
            //从本地和craft中选一个
            IFuelSource SelectActiveFuelSource(string fuelType, IFuelSource craftSource, IFuelSource localSource, bool wasteMode)
            {
                bool craftUnavailable = craftSource == null || (wasteMode ? craftSource.TotalCapacity - craftSource.TotalFuel <= 0.00001 : craftSource.IsEmpty);
                if (!craftUnavailable)
                {
                    return craftSource;
                }
    
                if (localSource == null)
                {
                    Mod.Log($"SelectActiveFuelSource: {fuelType} local source is null, defer tank creation to LoadFuelTanks");
                }
    
                return localSource;
            }
        }

        

        
        /// <summary>
        /// 从源燃料源补充目标燃料源。
        /// Refills the target fuel source from the source fuel source.
        /// </summary>
        /// <param name="from">源燃料源。Source fuel source.</param>
        /// <param name="to">目标燃料源。Target fuel source.</param>
        private void ReFill(IFuelSource craft, IFuelSource drood)
        {
            if (craft.TotalFuel >= drood.TotalCapacity - drood.TotalFuel)
            {
                craft.RemoveFuel(drood.TotalCapacity - drood.TotalFuel);
                drood.AddFuel(drood.TotalCapacity - drood.TotalFuel);
            }
            else
            {
                craft.RemoveFuel(craft.TotalFuel);
                drood.AddFuel(craft.TotalFuel);
            }
        }
        
        private void RemoveWaste(IFuelSource Craft, IFuelSource drood)
        {
            if (Craft.TotalCapacity-Craft.TotalFuel>drood.TotalFuel)
            {
                Craft.AddFuel(drood.TotalFuel);
                drood.RemoveFuel(drood.TotalFuel);
                Mod.Log($"Remove{Craft.FuelType.Name} 成功:{0}实际{1}",drood.TotalFuel,Craft.TotalFuel);
            }
            else
            {
                drood.RemoveFuel(Craft.TotalCapacity - Craft.TotalFuel);
                Craft.AddFuel(Craft.TotalCapacity - Craft.TotalFuel);
                Mod.Log($"Remove{Craft.FuelType.Name} 满了成功:{0}实际{1}", drood.TotalFuel, Craft.TotalFuel);
            }
        }
        #endregion
        
        #region 处理"那个"玩意用到的
        /// <summary>
        /// 在加载飞船时调用，触发飞船结构变化处理。
        /// Called when the craft is loaded, triggers craft structure change handling.
        /// </summary>
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            _isCraftLoading = true;
            try
            {
                base.OnCraftLoaded(craftScript, movedToNewCraft);
                if(!Game.InFlightScene)
                    return;
                Refresh();
                Mod.Log("OnCraftLoaded 调用RefreshFuelSource");
            }
            finally
            {
                _isCraftLoading = false;
            }
        }
        
        /// <summary>
        /// 出于一种奇异搞笑我也不知道为什么会出现的bug,如果一个Drood在Unload时(比如说保存游戏,快速保存,超出物理距离不再加载)带有FuelTankModifier,那么小蓝人就会处在Eva和在craft内的半死不活的叠加bug状态,所以在Unload时需要移除所有FuelTankModifier并用SupportLifeData 中各个燃料的buffer保存unload时的燃料数量,然后在再次加载的时候读取buffer恢复燃料数量,然后才能添加FuelTankModifier,这个modifier内用于处理游戏内的情况,至于快速保存那些,使用了单独的harmonyPatch对quickSave内的craft的xml进行处理.
        /// </summary>
        /// for some very strange and goofy reason, if a Drood has a FuelTankModifier when it's unloaded(like when you save the game, quick save, or it's out of physical range and not loaded), the phenomenon of the half-dead-and-half-alive bug(the drood itself is still there in the crew compartment, but you can not go EVA ,although you can still switch to the drood) will happen, so I have to remove all FuelTankModifiers and save the fuel amount buffer in SupportLifeData when unloading, then when reloading, it will read the buffer and restore the fuel amount, and then add the FuelTankModifier, this script is used to handle the flight situation, and for the quick save, I used a separate harmonyPatch to handle the craft's xml in the quickSave.

        
         public void LoadFuelTanks()
        {
            List<(string, double, double)> DataLocal = new List<(string, double, double)>();
            Mod.Log("LoadFuelTanks调用");
            OxygenSource = GetLocalFuelSource("Oxygen");
            FoodSource = GetLocalFuelSource("Food");
            WaterSource = GetLocalFuelSource("H2O");
            Co2Source = GetLocalFuelSource("CO2");
            WastedWaterSource = GetLocalFuelSource("Wasted Water");
            SolidWasteSource = GetLocalFuelSource("Solid Waste");
            if (OxygenSource != null && WaterSource != null && FoodSource != null && Co2Source != null &&
                WastedWaterSource != null && SolidWasteSource != null)
            {
                //Debug.Log("有本地燃料源");
                return;
            }

            if (OxygenSource == null)
            {
                DataLocal.Add(("Oxygen", this.Data.DesireOxygenCapacity, Data._oxygenAmountBuffer));
            }
            if (FoodSource == null)
            {
                DataLocal.Add(("Food", this.Data.DesireFoodCapacity, Data._foodAmountBuffer));
            }
            if (WaterSource == null)
            {
                DataLocal.Add(("H2O", this.Data.DesireWaterCapacity, Data._waterAmountBuffer));
            }
            if (Co2Source == null)
            {
                DataLocal.Add(("CO2", 0.42*600, Data._co2AmountBuffer));
            }

            if (WastedWaterSource == null)
            {
                DataLocal.Add(("Wasted Water", 3*0.35f, Data._wastedWaterAmountBuffer));
            }

            if (SolidWasteSource == null)
            {
                DataLocal.Add(("Solid Waste", 0.1f, Data._solidWasteAmountBuffer));
            }
            foreach (var data in DataLocal)
            {
                AddTank(data.Item1, data.Item2, data.Item3);
                Mod.Log($"添加 {data.Item1} 类型,容量{data.Item2},实际{data.Item3}");
            }
            // 强制刷新 CraftFuelSources
            if (Game.InFlightScene && PartScript.CraftScript != null)
            {
                Refresh();
                Mod.Log("LoadFuelTank 调用RefreshFuelSource");
            }
            Mod.Log("LoadFuelTanks结束");
            
            
        }
        /// <summary>
        /// 为指定的燃料类型添加具有给定容量的燃料罐。
        /// Adds a fuel tank for the specified fuel type with the given capacity.
        /// </summary>
        /// <param name="FuelCapacity">燃料罐的容量。Capacity of the fuel tank.</param>
        /// <param name="FuelType">燃料类型。Type of fuel.</param>
        /// <param name="FuelAmount">初始燃料量。Initial fuel amount.</param>
        private void AddTank(string fuelType, double fuelCapacity, double fuelAmount)
        {
            if (fuelCapacity<fuelAmount)
            {
                fuelAmount = fuelCapacity;
            }
            try
            {
                XElement element = new XElement("FuelTank");
                element.SetAttributeValue("capacity", fuelCapacity);
                element.SetAttributeValue("fuel", fuelAmount);
                element.SetAttributeValue("fuelType", fuelType);
                element.SetAttributeValue("utilization", -1);
                element.SetAttributeValue("autoFuelType", false);
                element.SetAttributeValue("subPriority", -1);
                element.SetAttributeValue("inspectorEnabled", false);
                element.SetAttributeValue("partPropertiesEnabled", false);
                element.SetAttributeValue("staticPriceAndMass", false);
        
                var tankData = PartModifierData.CreateFromStateXml(element, Data.Part, 15) as FuelTankData;
                if (tankData == null)
                {
                   Mod.Log($"Failed to create FuelTankData for {fuelType}");
                    return;
                }
        
                tankData.InspectorEnabled = false;
                tankData.SubPriority = -1;
        
                var fuelTankScript = tankData.CreateScript() as FuelTankScript;
                if (fuelTankScript == null)
                {
                   Mod.Log($"Failed to create FuelTankScript for {fuelType}");
                    return;
                }
        
                // 验证 FuelType
                if (fuelTankScript.FuelType == null || fuelTankScript.FuelType.Id != fuelType)
                {
                   Mod.Log($"FuelTankScript for {fuelType} has invalid FuelType: {fuelTankScript.FuelType?.Id}");
                    return;
                }
        
                // 设置 FuelTransferMode
                fuelTankScript.FuelTransferMode = FuelTransferMode.None;
        
                // 添加到 Modifiers 前验证 PartScript
                if (PartScript == null || PartScript.Modifiers == null)
                {
                   Mod.Log($"PartScript or Modifiers is null for Part ID: {PartScript?.Data.Id}");
                    return;
                }
        
                PartScript.Modifiers.Add(fuelTankScript);
               Mod.Log($"Successfully added FuelTank for {fuelType} to Part ID: {PartScript.Data.Id}");
                
            }
            catch (Exception e)
            {
               Mod.Log($"Failed to add FuelTank for {fuelType}: {e}");
            }
            
        }
        
        
        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
           Mod.Log("OnFlightEnded");
            FlightEnd();
        }
        /// <summary>
        /// Called when the craft ends, removes extra fuel tanks.
        public override void FlightEnd()
        {
            OnCraftUnloaded();
        }
        public void OnPhysicsEnabled(ICraftNode craftNode, PhysicsChangeReason reason)
        {
            Mod.Log("OnPhysicsEnabled{0}",reason);
            if (reason == PhysicsChangeReason.Warp||reason == PhysicsChangeReason.LoadedIntoGameView)
            {
                return;
            }
            LoadFuelTanks();
            Mod.Log("OnPhysicsEnabled调用LoadFuelTanks");
            if (ModSettings.Instance.ConsumeResourceWhenUnloaded==true&&!IsHibernating)
            {
                RemoveFuelAmonutInstantly();
                AddWastedAmountInstantly();
            }
            
        }
        public void OnPhysicsDisabled(ICraftNode craftNode, PhysicsChangeReason reason)
        {
            Mod.Log("OnPhysicsDisabled 原因:{0}",reason);
            if (reason == PhysicsChangeReason.Warp||reason== PhysicsChangeReason.UnloadedFromGameView)
            {
                return;
            }
            OnCraftUnloaded();
            Mod.Log("OnPhysicsDisabled调用OnCraftUnloaded");
        }
        public static XElement RemoveFuelTankXML(XElement partElement)
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
                   Mod.Log("No FuelTank nodes found to remove.");
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

        #region 燃料值保存缓冲
        private void OnCraftUnloaded()
        {
            
            Mod.Log("{0} 调用OnCraftUnloaded",PartScript.CraftScript.CraftNode.NodeId);
            try
            {
                Data.LastLoadTime = (long)FlightSceneScript.Instance.FlightState.Time;
                SaveFuelAmountBuffer();
                PartScript.Data.LoadXML(RemoveFuelTankXML(this.PartScript.Data.GenerateXml(this.PartScript.CraftScript.Transform,false)),15);
                
            }
            catch (Exception e)
            {
                Mod.Log("RemoveExtraTanks调用RemoveFuelTankXML出问题了{0}", e);
            }
            

        }

        public void SaveFuelAmountBuffer()
        {
            try
            {
                if (OxygenSource != null && FoodSource != null && FoodSource != null)
                {
                    Data._oxygenAmountBuffer = GetLocalFuelSource("Oxygen").TotalFuel;
                    Data._foodAmountBuffer =  GetLocalFuelSource("Food").TotalFuel;
                    Data._waterAmountBuffer =  GetLocalFuelSource("H2O").TotalFuel;
                    Data._co2AmountBuffer =  GetLocalFuelSource("CO2").TotalFuel;
                    Data._wastedWaterAmountBuffer =  GetLocalFuelSource("Wasted Water").TotalFuel;
                    Data._solidWasteAmountBuffer =  GetLocalFuelSource("Solid Waste").TotalFuel;
                    Mod.Log("缓冲区燃料:食物{0},oxygen{1},water,{2},二氧化碳:{3},WastedWater:{4},SolidWaste:{5}", Data._solidWasteAmountBuffer, Data._foodAmountBuffer, Data._oxygenAmountBuffer, Data._waterAmountBuffer, Data._co2AmountBuffer, Data._wastedWaterAmountBuffer);
                }
                else
                {
                    Mod.Log("SaveFuelAmountBuffer时燃料源为空");
                }
                
            }
            catch (Exception e)
            {
                Mod.Log("缓冲区燃料爆了{0}", e);
            }
        }

        private void RemoveFuelAmonutInstantly()
        {
                
            double xishu=360;
            var time= Game.Instance.FlightScene.FlightState.Time-Data.LastLoadTime;
            Mod.Log("调用RemoveFuelAmonutInstantly ,间隔{0}",time);
            if (this.OxygenSource == null)
            {
                Mod.Log("调用RemoveFuelAmonutInstantly失败,_oxygenSource有他妈null");
                return;
            }
            if (this.FoodSource == null)
            {
                Mod.Log("调用RemoveFuelAmonutInstantly失败,_foodSource有他妈null");
                return;
            }
            if (this.WaterSource == null)
            {
                Mod.Log("调用RemoveFuelAmonutInstantly失败,_waterSource有他妈null");
                return;
            }
            if (Data.FoodConsumeRate*(time/xishu) > this.FoodSource.TotalFuel)
            {
                this.FoodSource.RemoveFuel(FoodSource.TotalCapacity);
                Mod.Log("调用RemoveFuelAmonutInstantly,理论:{0}实际{1}",Data.FoodConsumeRate*(time/xishu),this.FoodSource.TotalFuel);
            }
            else
            {
                this.FoodSource.RemoveFuel(Data.FoodConsumeRate*(time/xishu));
            }
            
            if (Data.WaterConsumeRate*(time/xishu) > this.WaterSource.TotalFuel)
            {
                this.WaterSource.RemoveFuel(WaterSource.TotalCapacity);
            }
            else
            {
                this.WaterSource.RemoveFuel(Data.WaterConsumeRate*(time/xishu));
            }

            if (UsingInternalOxygen())
            {
                if (Data.OxygenConsumeRate*(time/xishu) > this.OxygenSource.TotalFuel)
                {
                    this.OxygenSource.RemoveFuel(OxygenSource.TotalCapacity);
                }
                else
                {
                    this.OxygenSource.RemoveFuel(Data.OxygenConsumeRate*(time/xishu)*0.001);
                }
            }
            
        }
        
        private void AddWastedAmountInstantly()
        {
            double xishu=360;
            var time= Game.Instance.FlightScene.FlightState.Time-Data.LastLoadTime;
            Mod.Log("AddWastedAmountInstantly ,间隔{0}",time);
            if (this.Co2Source == null)
            {
                Mod.Log("调用AddWastedAmountInstantly失败,_co2Source有他妈null");
                return;
            }

            if (WastedWaterSource==null)
            {
                Mod.Log("调用AddWastedAmountInstantly失败,_wastedWaterSource有他妈null");
            }

            if (SolidWasteSource == null)
            {
                Mod.Log("调用AddWastedAmountInstantly失败,_solidWasteSource有他妈null");
            }
            if (Data.WaterConsumeRate*Data.evaConsumeEfficiency*1.1*(time/xishu) >= this.WastedWaterSource.TotalCapacity-WastedWaterSource.TotalFuel)
            {
                this.WastedWaterSource.AddFuel(this.WastedWaterSource.TotalCapacity-WastedWaterSource.TotalFuel);
                Mod.Log("调用AddWastedAmountInstantly,满的,理论:{0}实际{1}",Data.WaterConsumeRate*Data.evaConsumeEfficiency*(time/xishu),this.WastedWaterSource.TotalCapacity-WastedWaterSource.TotalFuel);
            }
            else
            {
                this.WastedWaterSource.AddFuel(
                    0.9 * Data.WaterConsumeRate * Data.evaConsumeEfficiency * (time / xishu));
                Mod.Log("调用AddWastedAmountInstantly,理论:{0}",Data.WaterConsumeRate*Data.evaConsumeEfficiency*(time/xishu)*0.001);
            }
            if (Data.FoodConsumeRate*Data.evaConsumeEfficiency*1.1*(time/xishu) >= this.SolidWasteSource.TotalCapacity-SolidWasteSource.TotalFuel)
            {
                this.SolidWasteSource.AddFuel(this.SolidWasteSource.TotalCapacity-SolidWasteSource.TotalFuel);
                Mod.Log("调用AddWastedAmountInstantly,满的,理论:{0}实际{1}",Data.FoodConsumeRate*Data.evaConsumeEfficiency*(time/xishu),this.SolidWasteSource.TotalCapacity-SolidWasteSource.TotalFuel);
            }
            else
            {
                this.SolidWasteSource.AddFuel(Data.FoodConsumeRate*Data.evaConsumeEfficiency*(time/xishu)*1.1*0.00006);
                Mod.Log("调用AddWastedAmountInstantly,理论:{0}",Data.FoodConsumeRate*Data.evaConsumeEfficiency*(time/xishu)*1.1*0.06);
            }

            if (UsingInternalOxygen())
            {
                if (Data.OxygenConsumeRate*Data.evaConsumeEfficiency*1.375*(time/xishu) >= this.Co2Source.TotalCapacity-Co2Source.TotalFuel)
                {
                    this.Co2Source.AddFuel(this.Co2Source.TotalCapacity-Co2Source.TotalFuel);
                    Mod.Log("调用AddWastedAmountInstantly,满的,理论:{0}实际{1}",Data.OxygenConsumeRate*Data.evaConsumeEfficiency*(time/xishu),this.Co2Source.TotalCapacity-Co2Source.TotalFuel);
                }
                else
                {
                    this.Co2Source.AddFuel(Data.OxygenConsumeRate*Data.evaConsumeEfficiency*(time/xishu)*1.1);
                    Mod.Log("调用AddWastedAmountInstantly,理论:{0}",Data.OxygenConsumeRate*Data.evaConsumeEfficiency*(time/xishu));
                }
            }
            
            
            
        }
        #endregion
        
        #region SOI,结构变化相关函数
        /// <summary>
        /// 在飞船结构变化时调用，如果在飞行场景中，则刷新燃料源。
        /// Called when the craft structure changes, refreshes fuel sources if in flight scene.
        /// </summary>
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            if(!Game.InFlightScene)
                return;
            base.OnCraftStructureChanged(craftScript);
            if (Game.InFlightScene)
            {
                Refresh();
                Mod.Log("OnCraftStructureChanged调用RefreshFuelSource();");
            }
        }

        /// <summary>
        /// 根据大气条件和行星确定是否使用内部氧气。
        /// Determines if internal oxygen is being used based on atmospheric conditions and planet.
        /// </summary>
        /// <returns>如果使用内部氧气则返回true，否则返回false。True if using internal oxygen, false otherwise.</returns>
        public bool UsingInternalOxygen()
        {
            float airDensity = PartScript.CraftScript.AtmosphereSample.AirDensity;
            if (airDensity == 0)
            {
                return true;
            }

            if (airDensity != 0)
            {
                if (currentPlanetName==("Droo") || currentPlanetName==("Kerbin") ||
                    currentPlanetName==("Earth") || currentPlanetName==("Nebra") ||
                    currentPlanetName==("Laythe") || currentPlanetName==("Oord"))
                {
                    if(evaScript.IsInWater && PartScript.CraftScript.FlightData.AltitudeAboveSeaLevel < 0.1)
                    {
                        return true;
                    }
                    return false;
                }
                
            }
            return true;
        }

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

        private void UpdateCurrentPlanet(string name)
        {
            if (!Game.InFlightScene)
            {
                return;
            }
            try
            {
                currentPlanetName = name;
            }
            catch (Exception e)
            {
                Mod.Log("UpdateCurrentPlanet (string)调用出问题了{0}", e);
            }
            this.RadiationBeltConfig = RadiationBeltConfig.LoadFromFile(name);
        }

        private void OnPlayerChangedSoi(ICraftNode craftNode, IOrbitNode orbitNode)
        {
            UpdateCurrentPlanet(orbitNode.Name);
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

        #region 伤害处理
        /// <summary>
        /// 如果燃料源为空，则对小蓝人造成伤害。
        /// Applies damage to the crew member if a fuel source is empty.
        /// </summary>
        /// <param name="_fuelSource">要检查的燃料源。The fuel source to check.</param>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        /// <param name="DamageScale">伤害的大小。Scale of the damage.</param>
        private void DamageDrood(IFuelSource _fuelSource, FlightFrameData frame, float DamageScale)
        {
            if (_fuelSource == null || evaScript == null || PartScript == null || 
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
               Mod.Log("DamageDrood: null object found: - " +
                               $"_fuelSource={_fuelSource != null}, evaScript={evaScript != null}, " +
                               $"PartScript={PartScript != null}, Game.Instance={Game.Instance != null}, Settings={Game.Instance?.Settings != null}");
                return;
            }

            if (frame.DeltaTimeWorld <= 0)
            {
                return;
            }   
            
            float num2 = (isRunning ? 1.75f : 1f) * (isTourist ? 1.05f : 1f) * DamageScale * (float)frame.DeltaTimeWorld;
            if (_fuelSource.IsEmpty)
            {
                if ( _fuelSource.FuelType != null && 
                     (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
                {
                    this.PartScript.TakeDamage(num2 * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                        $"<color=red>Crew Member {evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because running out of {_fuelSource.FuelType.Name}, " +
                        $"he/she has {Mod.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1) * DamageScale))} left",
                        false, 2f);
                }
            }
        }

        private void DamageWaste(IFuelSource _fuelSource, FlightFrameData frame, float DamageScale)
        {
            if (_fuelSource == null || evaScript == null || PartScript == null ||
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
               Mod.Log("DamageWaste: null object found: - " +
                               $"_fuelSource={_fuelSource != null}, evaScript={evaScript != null}, " +
                               $"PartScript={PartScript != null}, Game.Instance={Game.Instance != null}, Settings={Game.Instance?.Settings != null}");
                return;
            }

            if (frame.DeltaTimeWorld <= 0)
            {
                return;
            }

            float num2 = (isRunning ? 1.75f : 1f) * (isTourist ? 1.05f : 1f) * DamageScale *
                         (float)frame.DeltaTimeWorld;
            if (_fuelSource.FuelType != null &&
                (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                this.PartScript.TakeDamage(num2 * Game.Instance.Settings.Game.Flight.ImpactDamageScale,
                    PartDamageType.Basic);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>Crew Member {evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because {_fuelSource.FuelType.Name} level is too high, " +
                    $"he/she has {Mod.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1) * DamageScale))} left",
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

            if (this.Data.CumulativeRad >= this.Data.RadiationDamageThresholdLevel3)
            {
                damageMultiplier = 0.05f;
                damageReason = "<color=red><size=120%>severe cumulative radiation exposure</color></size>";
               
            }
            else
            {
               
            }

            if (damageMultiplier > 0f)
            {
                this.PartScript.TakeDamage(damageMultiplier * dtWorld * impactScale);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>Crew Member {evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage due to {damageReason} " +"<br>"+
                    $"he/she has {Mod.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / (damageMultiplier * impactScale))} left",
                    false, 2f);
            }
        }
        
        #endregion

        #region UI
        /// <summary>
        /// 为零件生成inspector model，添加生命支持信息。
        /// Generates the inspector model for the part, adding life support information.
        /// </summary>
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            var localOxygen = GetLocalFuelSource("Oxygen");
            var localWater = GetLocalFuelSource("H2O");
            var localFood = GetLocalFuelSource("Food");
            var localCo2 = GetLocalFuelSource("CO2");
            var localWastedWater = GetLocalFuelSource("Wasted Water");
            var localSolidWaste = GetLocalFuelSource("Solid Waste");
            base.OnGenerateInspectorModel(model);
            //单独看任务时间的
            if (!this.isTourist)
            {
                model.Add<TextModel>(new TextModel("<color=yellow>Crew Role", (Func<string>) (() =>Data.DroodismCrewData==null?"Unknow":Data.DroodismCrewData.CrewRole.ToString())));
            }
            model.Add<TextModel>(new TextModel("<color=yellow>Mission Time", (Func<string>) (() =>Mod.GetStopwatchTimeString(MissionDurationTime))));
            //维生资源
            GroupModel lifeSupportGroupModel = new GroupModel("<color=green><size=115%>Life Support Info");
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel("Remain Oxygen", (Func<string>) (() =>
            {
                if (UsingInternalOxygen() && localOxygen != null && localOxygen.TotalCapacity > 0)
                {
                    float percentage = (float)(localOxygen.TotalFuel / localOxygen.TotalCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>{Units.GetPercentageString(percentage)}</color>";
                }
                else if (!UsingInternalOxygen())
                {
                    return "<color=green>Using External Oxygen</color>";
                }
                return "<color=purple>N/A</color>";
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel("Oxygen Supply Time", (Func<string>) (() =>
            {
                if (UsingInternalOxygen() && localOxygen != null && evaScript != null)
                {
                    float percentage = (float)(localOxygen.TotalFuel / localOxygen.TotalCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>"+Mod.GetStopwatchTimeString(localOxygen.TotalFuel / (Data.OxygenConsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                else if (!UsingInternalOxygen())
                {
                    return "<color=green>Infinity</color>";
                }
                return "N/A";
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel("Remain Water", (Func<string>) (() =>
            {
                if (localWater != null && localWater.TotalCapacity > 0)
                {
                    float waterPercentage = (float)(localWater.TotalFuel / localWater.TotalCapacity);
                    string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={waterTextColor}>{Units.GetPercentageString(waterPercentage)}</color>";
                }
                return "<color=purple>N/A</color>";
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel("Water Supply Time", (Func<string>) (() =>
            {
                if (localWater != null && evaScript != null)
                {
                    float waterPercentage = (float)(localWater.TotalFuel / localWater.TotalCapacity);
                    string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={waterTextColor}>"+Mod.GetStopwatchTimeString(localWater.TotalFuel / (Data.WaterConsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                return "N/A";
            })));
            
            lifeSupportGroupModel.Add<TextModel>(new TextModel("Remain Food", (Func<string>) (() =>
            {
                if (localFood != null && localFood.TotalCapacity > 0)
                {
                    float foodPercentage = (float)(localFood.TotalFuel / localFood.TotalCapacity);
                    string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>{Units.GetPercentageString(foodPercentage)}</color>";
                }
                return "<color=purple>N/A</color>";
            })));
        
            lifeSupportGroupModel.Add<TextModel>(new TextModel("Food Supply Time", (Func<string>) (() =>
            {
                if (localFood != null && evaScript != null)
                {
                    float foodPercentage = (float)(localFood.TotalFuel / localFood.TotalCapacity);
                    string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>"+Mod.GetStopwatchTimeString(localFood.TotalFuel / (Data.FoodConsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                return "N/A";
            })));
            AddGM("CO2 Level",localCo2);
            AddGM("Wasted Water Level",localWastedWater);
            AddGM("Solid Waste Level",localSolidWaste);
            
            void AddGM(string Title, IFuelSource source)
            {
                lifeSupportGroupModel.Add<TextModel>(new TextModel(Title, (Func<string>) (() =>
                {
                    if (source != null && source.TotalCapacity > 0)
                    {
                        float Percentage = (float)(source.TotalFuel /source.TotalCapacity);
                        string foodTextColor = Percentage > 0.85 ? "red" : Percentage >= 0.6 ? "yellow" : "green";
                        return $"<color={foodTextColor}>{Units.GetPercentageString(Percentage)}</color>";
                    }
                    return "<color=purple>N/A</color>";
                })));
            }
            model.AddGroup(lifeSupportGroupModel);

            //辐射强度
            GroupModel RadiationInspector = new GroupModel("<color=yellow><size=115%>Radiation Inspector");
            RadiationInspector.Add<TextModel>(new TextModel("Current Radiation Dose", (Func<string>) (() => $"{this.Data.CumulativeRad:F4} rad")));
            RadiationInspector.Add<TextModel>(new TextModel("Current Radiation Dose Stats", (Func<string>) (() => CurrentCumulativeRadiationStats)));
            RadiationInspector.Add<TextModel>(new TextModel("Current Radiation Dose Rate Per Hour", (Func<string>) (() => $"{this.RadiationDoseRateRadPerHour:F2} rad/h")));
            RadiationInspector.Add<TextModel>(new TextModel("Current Radiation Level", (Func<string>) (() => $"{CurrentRadiationRateStats}")));
            
            model.AddGroup(RadiationInspector);

            //插旗与开伞
            if (!isTourist)
            {
                
                if (Data.ParachuteTypes=="ParaGlider")
                {
                    model.Add(new ToggleModel("Auto Deploy ParaGlider",()=>Data.AutoDeployEnabled,b=>
                    {
                        Data.AutoDeployEnabled=b;
                    },"Enable Auto Deployment"));
                    model.Add(new SliderModel("Auto Deploy Height", (Func<float>) (() => this.Data.AutoDeployHeight), (Action<float>) (s => this.Data.AutoDeployHeight = s), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                  
                    model.Add(new SliderModel("Fully Deploy Height", (Func<float>) (() => Mathf.Min(Data.MinDeployHeight, Data.AutoDeployHeight)), (Action<float>) (s =>
                    {
                        this.Data.MinDeployHeight = Mathf.Min(s, Data.AutoDeployHeight);
                    }), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                    model.Add<TextButtonModel>(new TextButtonModel("<color=red>Manual Deploy ParaGlider", (Action<TextButtonModel>)(b => this.DeployParaglider())));
                }

                if (Data.ParachuteTypes=="Parachute")
                {
                    model.Add(new ToggleModel("Auto Deploy Parachute",()=>Data.AutoDeployEnabled,b=>
                    {
                        Data.AutoDeployEnabled=b;
                    },"Enable Auto Deployment"));
                    model.Add(new SliderModel("Auto Deploy Height", (Func<float>) (() => this.Data.AutoDeployHeight), (Action<float>) (s => this.Data.AutoDeployHeight = s), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                    model.Add<TextButtonModel>(new TextButtonModel("<color=red>Manual Deploy Parachute", (Action<TextButtonModel>)(b => this.DeployParaglider())));
                    model.Add(new SliderModel("Fully Deploy Height", (Func<float>) (() => Mathf.Min(Data.MinDeployHeight, Data.AutoDeployHeight)), (Action<float>) (s =>
                    {
                        
                        this.Data.MinDeployHeight = Mathf.Min(s, Data.AutoDeployHeight);
                    }), 100, 3000, true,true)).ValueFormatter = (Func<float, string>) (x => Units.GetDistanceString(x));
                }
            }
            //特殊能力这一块

            if (isTourist||this.Data.DroodismCrewData==null)
            {
                return;
            }

            if (this.Data.DroodismCrewData.CrewRole == DroodType.Pilot)
            {
                model.Add<TextButtonModel>(new TextButtonModel("Plant Flag", (Action<TextButtonModel>)(b => this.PlantFlagClick())));
            }

            if (this.Data.DroodismCrewData.CrewRole == DroodType.Engineer)
            {
                model.Add(new TextModel("Remain Repairing Tools",(Func<string>) (() => $"{this.Data.UtilizationFactor:F1}")));
                model.Add(new TextButtonModel("Repair", (Action<TextButtonModel>)(b => { this.RepairPart(); })));
            }
            
        }
        #endregion
        
        #region 插旗和开伞
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
                ui.ShowMessage("Can Not Plant Flag,Not in Eva",false,10);
                return;
            }
            if (craftScript.FlightData.Grounded==false)
            {
                ui.ShowMessage("Can Not Plant Flag,Not Grounded",false,10);
                return;
            }
            if (craftScript.FlightData.SurfaceVelocityMagnitude>=1)
            {
                ui.ShowMessage("Can Not Plant Flag,Velocity is too high",false,10);
                return;
            }

            if (evaScript.IsInWater)
            {
                ui.ShowMessage("Can Not Plant Flag,Drood is in water",false,10);
                return;
            }

            if (this.Data.UtilizationFactor<300)
            {
                ui.ShowMessage("Can Not Plant Flag,This Drood had already plant one!",false,10);
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
                ui.ShowMessage("Can Not Deploy Parachute,Not in Eva",false,10);
                return;
            }
            if (evaScript.IsGrounded)
            {
                ui.ShowMessage("Can Not Deploy Parachute,Drood is Grounded",false,10);
                return;
            }

            if (craftScript.FlightData.AltitudeAboveGroundLevel<=10)
            {
                ui.ShowMessage("Can Not Deploy Parachute Here",false,10);
                return;
            }
            if (craftScript.FlightData.AtmosphereSample.AirDensity<=0.01)
            {
                ui.ShowMessage("Can Not Deploy Parachute,Air Density is too thin",false,10);
                return;
            }

            if (craftScript.FlightData.SurfaceVelocityMagnitude >=
                craftScript.FlightData.AtmosphereSample.SpeedOfSound)
            {
                ui.ShowMessage("Can Not Deploy Parachute,Speed is too high",false,10);
                return;
            }
            
            if (evaScript.IsInWater)
            {
                ui.ShowMessage("Can Not Deploy Parachute,Drood is in water",false,10);
                return;
            }
            
            CraftScript craftScript1 = this.PartScript.CraftScript as CraftScript;
            craftScript1.RecenterTransformOnCoM(true);
            //this.PartScript.CommandPod.SetPilotSeatRotation(this.PartScript.Data.Rotation,true);
            你去吃粑粑去吧();

        }

        /// <summary>
        /// 情况太鸡巴诡异了你知道吗?
        /// </summary>
        private void 你去吃粑粑去吧()
        {
           
            Vector3 orgVelocity = this.PartScript.BodyScript.RigidBody.velocity;
            Vector3 orgAngularVelocity = this.PartScript.BodyScript.RigidBody.angularVelocity; // 修正这里！
            
            Vector3 orgEularAngle=this.PartScript.Transform.eulerAngles;
            //看看这里
            var parachutePartScript = CreateParachutePartScript();
            #region 我不想看
        
            
            // Step 3: 创建 Body 和 Group（此时 script 存在）
            BodyData body = CraftBuilder.CreateBodyData(new List<PartData>(){parachutePartScript.Data}, PartScript.CraftScript.Transform);
            CraftScript craftScript = PartScript.CraftScript as CraftScript;
            craftScript?.Data.Assembly.AddBody(body);
            BodyScript bodyScript = CraftBuilder.CreateBodyScript(craftScript, body);
            bodyScript.MoveToCraft(craftScript);
            bodyScript.OnInitialized();
        
            // 创建 Group 并设置 parent
            PartGroupScript groupScript = CreatePopPartGroup(bodyScript, parachutePartScript, this.PartScript.Data.Id + 1);
            #endregion

            //看这
            CraftBuilder.CalculateInertiaTensors(bodyScript, false);
            //evaScript.OnNodeLoaded();
            evaScript.OnPreNodeLoaded();
            
            parachutePartScript.BodyScript.RigidBody.velocity = orgVelocity;
            //parachutePartScript.BodyScript.RigidBody.rotation = orgRotation;
            parachutePartScript.Transform.eulerAngles = orgEularAngle;
            parachutePartScript.BodyScript.RigidBody.angularVelocity = orgAngularVelocity;
            evaScript.LoadIntoCrewCompartment(parachutePartScript.GetModifier<CrewCompartmentScript>(), null, announceBoarding: false);
            
        }
        #region Paraglider
        private PartScript CreateParachutePartScript()
        {
            Assembly assembly=new Assembly(ParachutePartElement(), 15, Game.Instance.PartTypes);
            CraftBuilder.CreatePartGameObjects(assembly.Parts, PartScript.CraftScript);
            PartData part = assembly.Parts[0];
            PartScript partScript = part.PartScript as PartScript;
            partScript?.UpdateAttachPoints();
            PartScript.CraftScript.Data.Assembly.Absorb(assembly);
            //pop.Attach(partScript.GetModifier<PopulationScript>());
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
            //partGroupScript.transform.localPosition = Vector3.zero;
            partGroupScript.transform.localPosition = bodyScript.transform.localPosition;
            partGroupScript.transform.localScale = Vector3.one;
            partGroupScript.transform.rotation = Quaternion.identity;
            //worldPositionStays: true before 
            partScript.transform.SetParent(partGroupScript.transform, worldPositionStays: false);
            partScript.AssignToPartGroup(partGroupScript);
            partGroupScript.Data.Parts.Add(partScript.Data);
            return partGroupScript;
        }
        #endregion
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
            }
            catch (Exception e)
            {
            }
            
        }

        /// <summary>
        /// 计算来自craft内部的辐射源
        /// </summary>
        private void CheckInCraftRadiationSource()
        {
            return;
            foreach (var VARIABLE in PartScript.CraftScript.Data.Assembly.Parts)
            {
                
            }
        }

        private void RefreshRadiationCompartment()
        {
            var eva = this.PartScript?.GetModifier<EvaScript>();
            
            if (eva.EvaActive||eva.ActiveWhileInCrewCompartment)
            {
                innerRadiationProtection= outerRadiationProtection = 0;
                return;
            }
          
            var crewCabin = eva.CrewCompartment?.PartScript.GetModifier<CrewCabinScript>();
            if (crewCabin!=null)
            {
                outerRadiationProtection =crewCabin.Data.RadiationShieldDuration>0? crewCabin.GetOuterRadiationProtection():0;
                innerRadiationProtection=crewCabin.Data.RadiationShieldDuration>0? crewCabin.GetInnerRadiationProtection():0;
            }

            if (crewCabin==null)
            {
                innerRadiationProtection= outerRadiationProtection = 0;
            }
           
        }
        /// <summary>
        /// 对辐射剂量计算
        /// </summary>
        /// <param name="data"></param>
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

            this.RadiationDoseRateRadPerHour = innerDoseRateRadPerHour*(1-innerRadiationProtection)+outerDoseRateRadPerHour*(1-outerRadiationProtection);
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

        #region 修复

        private void RepairPart()
        {
            var part = Game.Instance.FlightScene.ViewManager.GameView.SelectedPart;
            if (part==null)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("No part selected", false, 3f);
                isRepairing = false;
                return;
            }
            
            if (part.Data.PartType.Name==("Eva")||part.Data.PartType.Name==("Eva-Tourist"))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Can't Repair Drood", false, 3f);
                isRepairing = false;
                return;
            }
            if ((part.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Selected {part.Data.PartType.Name} is too far away to repair.", false, 3f);
                isRepairing = false;
                return;
            }

            if (part.Data.Damage <= 0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Selected {part.Data.PartType.Name} doesn't need to be repaired.", false, 3f);
                isRepairing = false;
                return;
            }

            isRepairing = true;



        }

        #endregion

        #region Misc

        private bool isRepairing;
        private bool _isCraftLoading;
        private void RepairPartWorkingLogic(in FlightFrameData data,PartData pd)
        {
            if (pd==null)
            {
                isRepairing = false;
                return;
            }
            if (pd.PartType.Name==("Eva")||pd.PartType.Name==("Eva-Tourist"))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Can not heal a drood", false, 3f);
                isRepairing = false;
                return;
            }
            if ((pd.PartScript.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Repairing Process Is Interrupted: {pd.PartType.Name} is too far away from engineer", false, 3f);
                isRepairing = false;
                return;
            }
            
            if (pd.Damage<=0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Repairing Process Completed for {pd.PartType.Name}.", false, 3f);
                isRepairing = false;
                return;
            }

            if (Data.UtilizationFactor<=0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Repairing Process Is Interrupted:Not Enough Repairing Tools for {pd.PartType.Name}", false, 4f);
                isRepairing = false;
                return;
            }
            
            if (Data.UtilizationFactor>0)
            {
               Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Repairing {pd.PartType.Name}:Progress {Units.GetPercentageString(Mathf.Clamp01((100f - pd.Damage) / 100f))}", false, 3f);
                float num = (float)data.DeltaTimeWorld * 2f;
                Data.UtilizationFactor -=num ;
                pd.Damage -= num;
            }
           
        }
        

        #endregion

       

        public bool UsesMachNumber { get; }
    }
   
}
