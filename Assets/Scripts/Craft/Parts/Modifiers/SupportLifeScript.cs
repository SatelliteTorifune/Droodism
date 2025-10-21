using ModApi.Craft;
using ModApi.Craft.Parts;
using Assets.Scripts.State;
using Assets.Scripts.Flight;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Flight.Events;
using ModApi.Flight.GameView;
using ModApi.Planet;
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

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class SupportLifeScript : 
        PartModifierScript<SupportLifeData>,
        IDesignerStart,
        IFlightStart,
        IFlightUpdate
    {
        /// <summary>
        /// 引用EvaScript组件,来获取这个小蓝人的一些乱七八糟的狗屎鸡巴数据玩意
        /// Reference to the EvaScript component,get current part's eva data and other stuff
        /// </summary>
        private EvaScript _evaScript;
        public bool IsHibernating { get; private set; }

        //当前小蓝人的CrewCompartment,目前没用到
        private CrewCompartmentScript droodCrewCompartmentScript;

        public IFuelSource _oxygenSource,_waterSource,_foodSource,_co2Source,_wastedWaterSource,_solidWasteSource;
        
        
        /// <summary>
        /// 当前所在行星的名称。
        /// Name of the current planet the craft is on.
        /// </summary>
        private string currentPlanetName;

        /// <summary>
        /// 当前行星的数据接口。
        /// Data interface for the current planet.
        /// </summary>
        private IPlanetData planetData;
        /// <summary>
        /// 小蓝人的钩爪
        /// 本来我想做成类似那种钩爪钩到craft自动补充氧气啥的,但是这个鸡巴script的数学判断啥的弔毛玩意我他妈看不懂一点
        /// what can i say,Torifune out.
        /// </summary>
        private GrapplingHookScript _grapplingHook;
        
        FlightSceneScript _flightSceneScript;

        /// <summary>
        /// 小蓝人目前的任务时长,从初次发射开始算的
        /// </summary>
        public long MissionDurationTime{get;private set;}
        

        
        
        /// <summary>
        /// 指示小蓝人是否在跑或是否为游客。
        /// Flags indicating if the crew member is running or if they are a tourist.
        /// </summary>
        public bool isRunning, isTourist, isFirstTime, AddingTankFlag;
        /// <summary>
        /// 在创建modifiers时调用，启用零件属性。
        /// Called when modifiers are created, enables part properties.
        /// </summary>
        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            this.Data.PartPropertiesEnabled = true;
            isFirstTime = true;
        }

        /// <summary>
        /// 实现IDesignerStart接口，在设计器场景开始时调用。
        /// Implements the IDesignerStart interface, called at the start of the designer scene.
        /// </summary>
        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
        }
        
        /// <summary>
        /// 不好意思因为我的代码水平就是一坨屎所以OnInitialLaunch和FlightStart里面的代码很奇异搞笑
        /// </summary>
        public override void OnInitialLaunch()
        {
            base.OnInitialLaunch();
            Data.MissionStartTime = (long)Game.Instance.FlightScene.FlightState.Time;
            Debug.LogFormat("OnInitialLaunch");
            isFirstTime = true;
            base.OnInitialLaunch();
            if (this.PartScript.Data.PartType.Name == "Eva-Tourist")
            {
                isTourist = true;
            }
            Data._foodAmountBuffer=this.Data.DesireFoodCapacity;
            Data._oxygenAmountBuffer=this.Data.DesireOxygenCapacity;
            Data._waterAmountBuffer=this.Data.DesireWaterCapacity;
            Data._co2AmountBuffer=0;
            Data._wastedWaterAmountBuffer=0;
            Data._solidWasteAmountBuffer=0;
            UpdateCurrentPlanet();
            
            try
            {
                RefreshFuelSource();
                Debug.LogFormat("OnInitialLaunch调用RefreshFuelSource");
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("OnInitialLaunch调用RefreshFuelSource出问题了{0}", e);
            }

            Data.LastLoadTime = (long)FlightSceneScript.Instance.FlightState.Time;
            
        }
        /// <summary>
        /// 实现IFlightStart接口，在飞行场景开始时调用。
        /// Implements the IFlightStart interface, called at the start of the flight scene.
        /// </summary>
        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            
            Debug.LogFormat("FlightStart");
            Game.Instance.FlightScene.FlightEnded+=OnFlightEnded;
            Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
            Game.Instance.FlightScene.PlayerChangedSoi += OnPlayerChangedSoi;
            Game.Instance.FlightScene.CraftNode.PhysicsDisabled += OnPhysicsDisabled;
            Game.Instance.FlightScene.CraftNode.PhysicsEnabled += OnPhysicsEnabled;
            base.OnInitialized();
            this.Data.InspectorEnabled = true;
            if (this.PartScript.Data.PartType.Name == "Eva-Tourist")
            {
                isTourist = true;
            }
            _evaScript = GetComponent<EvaScript>();
            droodCrewCompartmentScript = GetComponent<CrewCompartmentScript>();
            UpdateCurrentPlanet();
            Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
            LoadFuelTanks();
            //我他妈没在OnInitialLaunch里implent这个函数是为了方便你们这群小逼崽子瞎鸡巴改xml乱搞你们知道吗
            //SetRole();
            
        }

        
        /// <summary>
        /// 实现IFlightUpdate接口，在飞行期间每帧调用。
        /// Implements the IFlightUpdate interface, called every frame during flight.
        /// </summary>
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0) 
                return;
            //remove before release
            //Debug.LogFormat($"几把的{PartScript.CraftScript.ReferenceFrame.Center},reference frame{PartScript.CraftScript.FramePosition}");
            //Drood的PartScript.CraftScript.ReferenceFrame.Center一直在变
            UpdateRunningStatus();
            if (!IsHibernating)
            {
                if (!UsingInternalOxygen())
                {
                    AutoRefillLogic(frame);
                }
                ConsumptionLogic(frame);
            }
            

            MissionDurationTime = (long)Game.Instance.FlightScene.FlightState.Time - Data.MissionStartTime;
        }

        
        /// <summary>
        /// 这b玩意看不懂那你去吃我屎吧,你不会百度翻译吗?
        /// </summary>
        private void UpdateRunningStatus()
        {
            if (_evaScript.EvaActive && _evaScript.IsPlayerCraft && !_evaScript.IsWalking && _evaScript.IsGroundedTerrain && PartScript.CraftScript.SurfaceVelocity.magnitude >= 0.8)
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }
        }
        public override void OnPartDestroyed()
        {
            base.OnPartDestroyed();
            if (!Assets.Scripts.Game.InFlightScene)
                return;
            if (this.Data.crewData != null)
                this.Data.crewData.MissionTimeTotal += 
            MissionDurationTime;
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
                Debug.LogErrorFormat("GetLocalFuelSource出问题了{0}", e);
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
            if (_oxygenSource != null&&UsingInternalOxygen())
            {
                double num1 = (double)Data.OxygenComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1);
                if (_oxygenSource.IsEmpty)
                {
                    var localFuelSource = GetLocalFuelSource("Oxygen");
                    if (localFuelSource.IsEmpty)
                    {
                        DamageDrood(_oxygenSource, frame, Data.OxygenDamageScale);
                    }
                    localFuelSource.RemoveFuel(num1);
                }
                else
                {
                    _oxygenSource.RemoveFuel(num1);
                }
                
            }
            else
            {
                if (_oxygenSource == null)
                    Debug.LogWarning("_oxygenSource is Null");
            }

            if (_co2Source != null && UsingInternalOxygen())
            {
                
                double num1 = (double)Data.OxygenComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)*1.375*Data.evaConsumeEfficiency;
                
                if (_co2Source.TotalCapacity - _co2Source.TotalFuel <= 0.00001)
                {
                    var localFuelSource = GetLocalFuelSource("CO2");
                    if (localFuelSource == null)
                    {
                        Debug.LogWarning("local CO2 is Null");
                        return;
                    }
                    if (localFuelSource.TotalCapacity - localFuelSource.TotalFuel <= 0.00001)
                    {
                        DamageWaste(_co2Source, frame, Data.OxygenDamageScale);
                    }
                    if (!_oxygenSource.IsEmpty)
                    {
                        localFuelSource.AddFuel(num1);
                    }
                    
                }

                else
                {
                    if (!_oxygenSource.IsEmpty)
                    {
                        _co2Source.AddFuel(num1);
                    }
                    
                }
            }
            else
            {
                if (_co2Source == null)
                    Debug.LogWarning("_co2Source is Null");
            }
            if (_foodSource != null)
            {
                double num1 = (double)Data.FoodComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1);
                if (_foodSource.IsEmpty)
                {
                    var localFood = GetLocalFuelSource("Food");
                    if (localFood.IsEmpty)
                    {
                        DamageDrood(_foodSource, frame, Data.FoodDamageScale);
                    }
                    localFood.RemoveFuel(num1);
                }
                else
                {
                    _foodSource.RemoveFuel(num1);
                }
            }
            else
            {
                Debug.LogWarning("_foodSource is Null");
            }
            if (_solidWasteSource != null)
            {
                
                double num1 = (double)Data.FoodComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)*1.1*Data.evaConsumeEfficiency*0.04;
                
                if (_solidWasteSource.TotalCapacity - _solidWasteSource.TotalFuel <= 0.00001)
                {
                    var localFuelSource = GetLocalFuelSource("Solid Waste");
                    if (localFuelSource == null)
                    {
                        Debug.LogWarning("local Solid Waste is Null");
                        return;
                    }
                    if (localFuelSource.TotalCapacity - localFuelSource.TotalFuel <= 0.00001)
                    {
                        DamageWaste(_solidWasteSource, frame, Data.FoodDamageScale);
                    }
                    if (!_foodSource.IsEmpty)
                    {
                        localFuelSource.AddFuel(num1);
                    }
                    
                }

                else
                {
                    if (!_foodSource.IsEmpty)
                    {
                        _solidWasteSource.AddFuel(num1);
                    }
                    
                }
            }
            else
            {
                if (_solidWasteSource == null)
                    Debug.LogWarning("_solidWasteSource is Null");
            }
            if (_waterSource != null)
            {
                double num1 = (double)Data.WaterComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1);
                if (_waterSource.IsEmpty)
                {
                    var localWater = GetLocalFuelSource("H2O");
                    if (localWater.IsEmpty)
                    {
                        DamageDrood(_waterSource, frame, Data.WaterDamageScale);
                    }
                    localWater.RemoveFuel(num1);
                }
                else
                {
                    _waterSource.RemoveFuel(num1);
                }
            }
            else
            {
                Debug.LogWarning("_waterSource is Null");
            }
            if (_wastedWaterSource != null)
            {
                
                double num1 = (double)Data.WaterComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)*1.1*Data.evaConsumeEfficiency;
                
                if (_wastedWaterSource.TotalCapacity - _wastedWaterSource.TotalFuel <= 0.00001)
                {
                    var localFuelSource = GetLocalFuelSource("Wasted Water");
                    if (localFuelSource == null)
                    {
                        Debug.LogWarning("local Wasted Water is Null");
                        return;
                    }
                    if (localFuelSource.TotalCapacity - localFuelSource.TotalFuel <= 0.00001)
                    {
                        DamageWaste(_wastedWaterSource, frame, Data.WaterDamageScale);
                    }
                    if (!_waterSource.IsEmpty)
                    {
                        localFuelSource.AddFuel(num1);
                    }
                    
                }

                else
                {
                    if (!_waterSource.IsEmpty)
                    {
                        _wastedWaterSource.AddFuel(num1);
                    }
                    
                }
            }

            if (_co2Source==null||_oxygenSource==null||_foodSource==null||_waterSource==null||_wastedWaterSource==null||_solidWasteSource==null)
            {
               LoadFuelTanks();
               Debug.Log("ConsumptionLogic调用LoadFuelTanks");
            }
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
        public void RefreshFuelSource()
        {
            if (PartScript == null || PartScript.Modifiers == null)
            {
                return;
            }
            
            if (Game.InFlightScene)
            {
                try
                {
                    CraftRefreshFuelSource();
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("RefreshFuelSource调用CraftRefeshFuelSource歇逼了{0}", e);
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
        private void CraftRefreshFuelSource()
        {
            bool isEva = false;
            List<(string, double, double)>DataLocal = new List<(string, double, double)>();
            //Debug.LogFormat("调用CraftRefeshFuelSource 开始");
            try
            {
                if (PartScript.CraftScript.ActiveCommandPod.Part.PartScript==PartScript&&!_evaScript.ActiveWhileInCrewCompartment)
                {
                    isEva = true;
                    IsHibernating = false;
                    //Debug.LogFormat("这个drood是ActiveCommandPod");
                    _oxygenSource=_waterSource=_foodSource=_co2Source=_wastedWaterSource=_solidWasteSource=null;
                }
                
                try
                {
                    if (isEva==false)
                    {
                        IsHibernating = false;
                        var stCommandPodPatchScript = PartScript.GetModifier<EvaScript>().CrewCompartment?.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                        _oxygenSource = stCommandPodPatchScript?.OxygenFuelSource;
                        _foodSource = stCommandPodPatchScript?.FoodFuelSource;
                        _waterSource = stCommandPodPatchScript?.WaterFuelSource;
                        _co2Source = stCommandPodPatchScript?.CO2FuelSource;
                        _wastedWaterSource = stCommandPodPatchScript?.WastedWaterFuelSource;
                        _solidWasteSource = stCommandPodPatchScript?.SolidWasteFuelSource;
                    }
                }
                catch (Exception e)
                {
                   Debug.LogFormat("CraftRefreshSource:No Eva part:{0}",e);
                }
                
               
                if (_oxygenSource != null && _foodSource != null && _waterSource != null&&_co2Source!= null&& _wastedWaterSource!= null&& _solidWasteSource != null)
                {
                    //Debug.LogFormat("调用CraftRefeshFuelSource 刷新完成 Oxygen:{0},Food:{1},Water:{2},CO2:{3},WastedWater:{4},SolidWaste:{5}", _oxygenSource.TotalFuel, _foodSource.TotalFuel, _waterSource.TotalFuel, _co2Source.TotalFuel, _wastedWaterSource.TotalFuel, _solidWasteSource.TotalFuel);
                    ReFill(_oxygenSource, GetLocalFuelSource("Oxygen"));
                    ReFill(_foodSource, GetLocalFuelSource("Food"));
                    ReFill(_waterSource, GetLocalFuelSource("H2O"));
                    RemoveWaste(_co2Source, GetLocalFuelSource("CO2"));
                    RemoveWaste(_wastedWaterSource, GetLocalFuelSource("Wasted Water"));
                    RemoveWaste(_solidWasteSource, GetLocalFuelSource("Solid Waste"));
                    SaveFuelAmountBuffer();
                    return;
                    
                }
                HandleFuelSource("Oxygen", Data.DesireOxygenCapacity, Data._oxygenAmountBuffer, ref _oxygenSource);
                HandleFuelSource("Food", Data.DesireFoodCapacity, Data._foodAmountBuffer, ref _foodSource);
                HandleFuelSource("H2O", Data.DesireWaterCapacity, Data._waterAmountBuffer, ref _waterSource);
                HandleFuelSource("CO2", Data.DesireOxygenCapacity*1.1, Data._co2AmountBuffer, ref _co2Source);
                HandleFuelSource("Wasted Water", Data.DesireWaterCapacity*1.1, Data._wastedWaterAmountBuffer, ref _wastedWaterSource);
                HandleFuelSource("Solid Waste", Data.DesireFoodCapacity*1.1, Data._solidWasteAmountBuffer, ref _solidWasteSource);
                SaveFuelAmountBuffer();
                //Debug.Log("CraftRefreshFuelSource:完成");
                
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("CraftRefreshFuelSource出问题了{0}", e);
            }
            void HandleFuelSource(string fuelType, double capacity, double bufferAmount, ref IFuelSource fuelSource)
            {
                try
                {
                    if (fuelType=="Wasted Water"||fuelType=="Solid Waste"||fuelType=="CO2")
                    {
                        if (fuelSource == null || fuelSource.TotalCapacity-fuelSource.TotalFuel <=0.00001)
                        {
                            fuelSource = GetLocalFuelSource(fuelType);
                            if (fuelSource != null)
                            {
                                //Debug.Log($"设置为本地 {fuelType}");
                            }
                            else
                            {
                                Debug.LogWarning($"未找到 {fuelType} 类型的 FuelSource");
                                try
                                {
                                    DataLocal.Add((fuelType, capacity, bufferAmount));
                                    Debug.Log($"已记录 {fuelType} 类型");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"从 CraftRefreshFuelSource 记录 {fuelType} 出错: {e}");
                                }
                            }
                        }
                        else
                        {
                            if (fuelSource!=null&&GetLocalFuelSource(fuelType)!=null)
                            {
                                RemoveWaste(fuelSource, GetLocalFuelSource(fuelType));
                            }
                            else
                            {
                                Debug.LogWarning($"未找到 {fuelType} 类型的 FuelSource,无法调用RemoveWaste");
                            }
                           
                        
                        }
                    }
                    else
                    {
                        if (fuelSource == null || fuelSource.IsEmpty)
                        {
                            fuelSource = GetLocalFuelSource(fuelType);
                            if (fuelSource != null)
                            {
                                //Debug.Log($"设置为本地 {fuelType}");
                            }
                            else
                            {
                                Debug.LogWarning($"未找到 {fuelType} 类型的 FuelSource");
                                try
                                {
                                    DataLocal.Add((fuelType, capacity, bufferAmount));
                                    Debug.Log($"已记录 {fuelType} 类型");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"从 CraftRefreshFuelSource 记录 {fuelType} 出错: {e}");
                                }
                            }
                        }
                        else
                        {
                            if (fuelSource!=null&&GetLocalFuelSource(fuelType)!=null)
                            {
                                ReFill(fuelSource, GetLocalFuelSource(fuelType));
                            }
                            else
                            {
                                Debug.LogWarning($"未找到 {fuelType} 类型的 FuelSource,无法调用RemoveWaste");
                            }
                        
                        } 
                    }
                    
                }
                catch (Exception e)
                {
                    Debug.LogError($"处理 {fuelType} FuelSource 出错: {e}");
                }
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
                Debug.LogFormat($"Remove{Craft.FuelType.Name} 成功:{0}实际{1}",drood.TotalFuel,Craft.TotalFuel);
            }
            else
            {
                drood.RemoveFuel(Craft.TotalCapacity - Craft.TotalFuel);
                Craft.AddFuel(Craft.TotalCapacity - Craft.TotalFuel);
                Debug.LogWarningFormat($"Remove{Craft.FuelType.Name} 满了成功:{0}实际{1}", drood.TotalFuel, Craft.TotalFuel);
            }
        }

        
        /// <summary>
        /// 在加载飞船时调用，触发飞船结构变化处理。
        /// Called when the craft is loaded, triggers craft structure change handling.
        /// </summary>
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            if(!Game.InFlightScene)
                return;
            base.OnCraftLoaded(craftScript, movedToNewCraft);
            if (!PartScript.Data.IsRootPart)
            {
                RefreshFuelSource();
                //Debug.LogFormat("OnCraftLoaded 调用RefreshFuelSource");

            }
            
        }
        
        /// <summary>
        /// 出于一种奇异搞笑我也不知道为什么会出现的bug,如果一个Drood在Unload时(比如说保存游戏,快速保存,超出物理距离不再加载)带有FuelTankModifier,那么小蓝人就会处在Eva和在craft内的半死不活的叠加bug状态,所以在Unload时需要移除所有FuelTankModifier并用SupportLifeData 中各个燃料的buffer保存unload时的燃料数量,然后在再次加载的时候读取buffer恢复燃料数量,然后才能添加FuelTankModifier,这个modifier内用于处理游戏内的情况,至于快速保存那些,使用了单独的harmonyPatch对quickSave内的craft的xml进行处理.
        /// </summary>
        /// for some very strange and goofy reason, if a Drood has a FuelTankModifier when it's unloaded(like when you save the game, quick save, or it's out of physical range and not loaded), the phenomenon of the half-dead-and-half-alive bug(the drood itself is still there in the crew compartment, but you can not go EVA ,although you can still switch to the drood) will happen, so I have to remove all FuelTankModifiers and save the fuel amount buffer in SupportLifeData when unloading, then when reloading, it will read the buffer and restore the fuel amount, and then add the FuelTankModifier, this script is used to handle the flight situation, and for the quick save, I used a separate harmonyPatch to handle the craft's xml in the quickSave.

        #region 处理这坨屎用到的东西
         public void LoadFuelTanks()
        {
            AddingTankFlag=true;
            List<(string, double, double)> DataLocal = new List<(string, double, double)>();
            Debug.Log("LoadFuelTanks调用");
            _oxygenSource = GetLocalFuelSource("Oxygen");
            _foodSource = GetLocalFuelSource("Food");
            _waterSource = GetLocalFuelSource("H2O");
            _co2Source = GetLocalFuelSource("CO2");
            _wastedWaterSource = GetLocalFuelSource("Wasted Water");
            _solidWasteSource = GetLocalFuelSource("Solid Waste");
            if (_oxygenSource != null && _waterSource != null && _foodSource != null && _co2Source != null &&
                _wastedWaterSource != null && _solidWasteSource != null)
            {
                //Debug.Log("有本地燃料源");
                return;
            }

            if (_oxygenSource == null)
            {
                DataLocal.Add(("Oxygen", this.Data.DesireOxygenCapacity, Data._oxygenAmountBuffer));
            }
            if (_foodSource == null)
            {
                DataLocal.Add(("Food", this.Data.DesireFoodCapacity, Data._foodAmountBuffer));
            }
            if (_waterSource == null)
            {
                DataLocal.Add(("H2O", this.Data.DesireWaterCapacity, Data._waterAmountBuffer));
            }
            if (_co2Source == null)
            {
                DataLocal.Add(("CO2", 0.42*600, Data._co2AmountBuffer));
            }

            if (_wastedWaterSource == null)
            {
                DataLocal.Add(("Wasted Water", 3*0.35f, Data._wastedWaterAmountBuffer));
            }

            if (_solidWasteSource == null)
            {
                DataLocal.Add(("Solid Waste", 0.1f, Data._solidWasteAmountBuffer));
            }
            foreach (var data in DataLocal)
            {
                AddTank(data.Item1, data.Item2, data.Item3);
                Debug.LogFormat($"添加 {data.Item1} 类型,容量{data.Item2},实际{data.Item3}");
            }
            // 强制刷新 CraftFuelSources
            if (Game.InFlightScene && PartScript.CraftScript != null)
            {
                RefreshFuelSource();
                Debug.Log("LoadFuelTank 调用RefreshFuelSource");
            }
            AddingTankFlag=false;
            Debug.Log("LoadFuelTanks结束,AddingTankFlag=false");
            
            
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
                    Debug.LogError($"Failed to create FuelTankData for {fuelType}");
                    return;
                }
        
                tankData.InspectorEnabled = false;
                tankData.SubPriority = -1;
        
                var fuelTankScript = tankData.CreateScript() as FuelTankScript;
                if (fuelTankScript == null)
                {
                    Debug.LogError($"Failed to create FuelTankScript for {fuelType}");
                    return;
                }
        
                // 验证 FuelType
                if (fuelTankScript.FuelType == null || fuelTankScript.FuelType.Id != fuelType)
                {
                    Debug.LogError($"FuelTankScript for {fuelType} has invalid FuelType: {fuelTankScript.FuelType?.Id}");
                    return;
                }
        
                // 设置 FuelTransferMode
                fuelTankScript.FuelTransferMode = FuelTransferMode.None;
        
                // 添加到 Modifiers 前验证 PartScript
                if (PartScript == null || PartScript.Modifiers == null)
                {
                    Debug.LogError($"PartScript or Modifiers is null for Part ID: {PartScript?.Data.Id}");
                    return;
                }
        
                PartScript.Modifiers.Add(fuelTankScript);
                Debug.Log($"Successfully added FuelTank for {fuelType} to Part ID: {PartScript.Data.Id}");
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to add FuelTank for {fuelType}: {e}");
            }
            
        }
        public override void OnDesignerPullout(Assembly assembly)
        {
            base.OnDesignerPullout(assembly);
        }
        
        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            Debug.Log("OnFlightEnded");
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
            //Debug.LogFormat("OnPhysicsEnabled{0}",reason);
            if (reason == PhysicsChangeReason.Warp||reason == PhysicsChangeReason.LoadedIntoGameView)
            {
                return;
            }
            LoadFuelTanks();
            if (ModSettings.Instance.ConsumeResourceWhenUnloaded==true&&!IsHibernating)
            {
                RemoveFuelAmonutInstantly();
                AddWastedAmountInstantly();
            }
            
        }
        public void OnPhysicsDisabled(ICraftNode craftNode, PhysicsChangeReason reason)
        {
            //Debug.LogFormat("OnPhysicsDisabled 原因:{0}",reason);
            if (reason == PhysicsChangeReason.Warp||reason== PhysicsChangeReason.UnloadedFromGameView)
            {
                return;
            }
            OnCraftUnloaded();
            //Debug.LogFormat("OnPhysicsDisabled调用OnCraftUnloaded");
        }
        public static XElement RemoveFuelTankXML(XElement partElement)
        {
            if (partElement == null)
            {
                Debug.LogError("Part element is null.");
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
                        Debug.Log($"Removing FuelTank with fuelType={tank.Attribute("fuelType")?.Value}");
                        tank.Remove();
                    }
                }
                else
                {
                    Debug.Log("No FuelTank nodes found to remove.");
                }

                return partElement;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error removing FuelTank nodes: {ex.Message}");
                return null;
            }
        }
        private void OnCraftUnloaded()
        {
            
            Debug.LogFormat("{0} 调用OnCraftUnloaded",PartScript.Data.Id);
            try
            {
                Data.LastLoadTime = (long)FlightSceneScript.Instance.FlightState.Time;
                SaveFuelAmountBuffer();
                PartScript.Data.LoadXML(RemoveFuelTankXML(this.PartScript.Data.GenerateXml(this.PartScript.CraftScript.Transform,false)),15);
                
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("RemoveExtraTanks调用RemoveFuelTankXML出问题了{0}", e);
            }
            

        }

        public void SaveFuelAmountBuffer()
        {
            try
            {
                if (_oxygenSource != null && _foodSource != null && _foodSource != null)
                {
                    Data._oxygenAmountBuffer = _oxygenSource.TotalFuel;
                    Data._foodAmountBuffer = _foodSource.TotalFuel;
                    Data._waterAmountBuffer = _waterSource.TotalFuel;
                    Data._co2AmountBuffer = _co2Source.TotalFuel;
                    Data._wastedWaterAmountBuffer = _wastedWaterSource.TotalFuel;
                    Data._solidWasteAmountBuffer = _solidWasteSource.TotalFuel;
                    //Debug.LogFormat("缓冲区燃料:食物{0},oxygen{1},water,{2},二氧化碳:{3},WastedWater:{4},SolidWaste:{5}", Data._solidWasteAmountBuffer, Data._foodAmountBuffer, Data._oxygenAmountBuffer, Data._waterAmountBuffer, Data._co2AmountBuffer, Data._wastedWaterAmountBuffer);
                }
                else
                {
                    Debug.LogErrorFormat("SaveFuelAmountBuffer时燃料源为空");
                }
                
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("缓冲区燃料爆了{0}", e);
            }
        }

        private void RemoveFuelAmonutInstantly()
        {
                
            double xishu=360;
            var time= Game.Instance.FlightScene.FlightState.Time-Data.LastLoadTime;
            Debug.LogFormat("调用RemoveFuelAmonutInstantly ,间隔{0}",time);
            if (this._oxygenSource == null)
            {
                Debug.LogFormat("调用RemoveFuelAmonutInstantly失败,_oxygenSource有他妈null");
                return;
            }
            if (this._foodSource == null)
            {
                Debug.LogFormat("调用RemoveFuelAmonutInstantly失败,_foodSource有他妈null");
                return;
            }
            if (this._waterSource == null)
            {
                Debug.LogFormat("调用RemoveFuelAmonutInstantly失败,_waterSource有他妈null");
                return;
            }
            if (Data.FoodComsumeRate*(time/xishu) > this._foodSource.TotalFuel)
            {
                this._foodSource.RemoveFuel(_foodSource.TotalCapacity);
                Debug.LogFormat("调用RemoveFuelAmonutInstantly,理论:{0}实际{1}",Data.FoodComsumeRate*(time/xishu),this._foodSource.TotalFuel);
            }
            else
            {
                this._foodSource.RemoveFuel(Data.FoodComsumeRate*(time/xishu));
            }
            
            if (Data.WaterComsumeRate*(time/xishu) > this._waterSource.TotalFuel)
            {
                this._waterSource.RemoveFuel(_waterSource.TotalCapacity);
            }
            else
            {
                this._waterSource.RemoveFuel(Data.WaterComsumeRate*(time/xishu));
            }

            if (UsingInternalOxygen())
            {
                if (Data.OxygenComsumeRate*(time/xishu) > this._oxygenSource.TotalFuel)
                {
                    this._oxygenSource.RemoveFuel(_oxygenSource.TotalCapacity);
                }
                else
                {
                    this._oxygenSource.RemoveFuel(Data.OxygenComsumeRate*(time/xishu)*0.001);
                }
            }
            
        }
        
        private void AddWastedAmountInstantly()
        {
            double xishu=360;
            var time= Game.Instance.FlightScene.FlightState.Time-Data.LastLoadTime;
            Debug.LogFormat("AddWastedAmountInstantly ,间隔{0}",time);
            if (this._co2Source == null)
            {
                Debug.LogFormat("调用AddWastedAmountInstantly失败,_co2Source有他妈null");
                return;
            }

            if (_wastedWaterSource==null)
            {
                Debug.LogFormat("调用AddWastedAmountInstantly失败,_wastedWaterSource有他妈null");
            }

            if (_solidWasteSource == null)
            {
                Debug.LogFormat("调用AddWastedAmountInstantly失败,_solidWasteSource有他妈null");
            }
            if (Data.WaterComsumeRate*Data.evaConsumeEfficiency*1.1*(time/xishu) >= this._wastedWaterSource.TotalCapacity-_wastedWaterSource.TotalFuel)
            {
                this._wastedWaterSource.AddFuel(this._wastedWaterSource.TotalCapacity-_wastedWaterSource.TotalFuel);
                Debug.LogFormat("调用AddWastedAmountInstantly,满的,理论:{0}实际{1}",Data.WaterComsumeRate*Data.evaConsumeEfficiency*(time/xishu),this._wastedWaterSource.TotalCapacity-_wastedWaterSource.TotalFuel);
            }
            else
            {
                this._wastedWaterSource.AddFuel(
                    0.9 * Data.WaterComsumeRate * Data.evaConsumeEfficiency * (time / xishu));
                Debug.LogFormat("调用AddWastedAmountInstantly,理论:{0}",Data.WaterComsumeRate*Data.evaConsumeEfficiency*(time/xishu)*0.001);
            }
            if (Data.FoodComsumeRate*Data.evaConsumeEfficiency*1.1*(time/xishu) >= this._solidWasteSource.TotalCapacity-_solidWasteSource.TotalFuel)
            {
                this._solidWasteSource.AddFuel(this._solidWasteSource.TotalCapacity-_solidWasteSource.TotalFuel);
                Debug.LogFormat("调用AddWastedAmountInstantly,满的,理论:{0}实际{1}",Data.FoodComsumeRate*Data.evaConsumeEfficiency*(time/xishu),this._solidWasteSource.TotalCapacity-_solidWasteSource.TotalFuel);
            }
            else
            {
                this._solidWasteSource.AddFuel(Data.FoodComsumeRate*Data.evaConsumeEfficiency*(time/xishu)*1.1*0.00006);
                Debug.LogFormat("调用AddWastedAmountInstantly,理论:{0}",Data.FoodComsumeRate*Data.evaConsumeEfficiency*(time/xishu)*1.1*0.06);
            }

            if (UsingInternalOxygen())
            {
                if (Data.OxygenComsumeRate*Data.evaConsumeEfficiency*1.375*(time/xishu) >= this._co2Source.TotalCapacity-_co2Source.TotalFuel)
                {
                    this._co2Source.AddFuel(this._co2Source.TotalCapacity-_co2Source.TotalFuel);
                    Debug.LogFormat("调用AddWastedAmountInstantly,满的,理论:{0}实际{1}",Data.OxygenComsumeRate*Data.evaConsumeEfficiency*(time/xishu),this._co2Source.TotalCapacity-_co2Source.TotalFuel);
                }
                else
                {
                    this._co2Source.AddFuel(Data.OxygenComsumeRate*Data.evaConsumeEfficiency*(time/xishu)*1.1);
                    Debug.LogFormat("调用AddWastedAmountInstantly,理论:{0}",Data.OxygenComsumeRate*Data.evaConsumeEfficiency*(time/xishu));
                }
            }
            
            
            
        }
        #endregion
        #region 无所弔谓
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
                RefreshFuelSource();
                //Debug.Log("OnCraftStructureChanged调用RefreshFuelSource();");
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
                    if(_evaScript.IsInWater && PartScript.CraftScript.FlightData.AltitudeAboveSeaLevel < 0.1)
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
        public void UpdateCurrentPlanet()
        {
            if (!Game.InFlightScene)
            {
                return;
            }
            try
            {
                currentPlanetName = Game.Instance.FlightScene?.CraftNode.CraftScript.FlightData.Orbit.Parent.PlanetData
                    .Name;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("UpdateCurrentPlanet调用出问题了{0}", e);
            }

        }

        private void OnPlayerChangedSoi(ICraftNode craftNode, IOrbitNode orbitNode)
        {
            UpdateCurrentPlanet();
        }
        /// <summary>
        /// SOI变化时的事件处理程序，更新当前行星。
        /// Event handler for when the sphere of influence changes, updates the current planet.
        /// </summary>
        private void OnSoiChanged(IOrbitNode source)
        {
            UpdateCurrentPlanet();
        }
        

        #endregion

        /// <summary>
        /// 如果燃料源为空，则对小蓝人造成伤害。
        /// Applies damage to the crew member if a fuel source is empty.
        /// </summary>
        /// <param name="_fuelSource">要检查的燃料源。The fuel source to check.</param>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        /// <param name="DamageScale">伤害的大小。Scale of the damage.</param>
        private void DamageDrood(IFuelSource _fuelSource, FlightFrameData frame, float DamageScale)
        {
            if (_fuelSource == null || _evaScript == null || PartScript == null || 
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
                Debug.LogError("DamageDrood: null object found: - " +
                               $"_fuelSource={_fuelSource != null}, _evaScript={_evaScript != null}, " +
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
                        $"<color=red>Crew Member {_evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because running out of {_fuelSource.FuelType.Name}, " +
                        $"he/she has {Mod.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1) * DamageScale))} left",
                        false, 2f);
                }
            }
        }

        private void DamageWaste(IFuelSource _fuelSource, FlightFrameData frame, float DamageScale)
        {
            if (_fuelSource == null || _evaScript == null || PartScript == null ||
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
                Debug.LogError("DamageWaste: null object found: - " +
                               $"_fuelSource={_fuelSource != null}, _evaScript={_evaScript != null}, " +
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
                    $"<color=red>Crew Member {_evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because {_fuelSource.FuelType.Name} level is too high, " +
                    $"he/she has {Mod.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1) * DamageScale))} left",
                    false, 2f);
            }
        }

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
            model.Add<TextModel>(new TextModel("<color=yellow>Mission Time", (Func<string>) (() =>Mod.GetStopwatchTimeString(MissionDurationTime))));
            GroupModel groupModel = new GroupModel("<color=green><size=115%>Life Support Info");
            model.AddGroup(groupModel);
            groupModel.Add<TextModel>(new TextModel("Remain Oxygen", (Func<string>) (() =>
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
            
            groupModel.Add<TextModel>(new TextModel("Oxygen Supply Time", (Func<string>) (() =>
            {
                if (UsingInternalOxygen() && localOxygen != null && _evaScript != null)
                {
                    float percentage = (float)(localOxygen.TotalFuel / localOxygen.TotalCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>"+Mod.GetStopwatchTimeString(localOxygen.TotalFuel / (Data.OxygenComsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                else if (!UsingInternalOxygen())
                {
                    return "<color=green>Infinity</color>";
                }
                return "N/A";
            })));
            
            groupModel.Add<TextModel>(new TextModel("Remain Water", (Func<string>) (() =>
            {
                if (localWater != null && localWater.TotalCapacity > 0)
                {
                    float waterPercentage = (float)(localWater.TotalFuel / localWater.TotalCapacity);
                    string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={waterTextColor}>{Units.GetPercentageString(waterPercentage)}</color>";
                }
                return "<color=purple>N/A</color>";
            })));
            
            groupModel.Add<TextModel>(new TextModel("Water Supply Time", (Func<string>) (() =>
            {
                if (localWater != null && _evaScript != null)
                {
                    float waterPercentage = (float)(localWater.TotalFuel / localWater.TotalCapacity);
                    string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={waterTextColor}>"+Mod.GetStopwatchTimeString(localWater.TotalFuel / (Data.WaterComsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                return "N/A";
            })));
            
            groupModel.Add<TextModel>(new TextModel("Remain Food", (Func<string>) (() =>
            {
                if (localFood != null && localFood.TotalCapacity > 0)
                {
                    float foodPercentage = (float)(localFood.TotalFuel / localFood.TotalCapacity);
                    string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>{Units.GetPercentageString(foodPercentage)}</color>";
                }
                return "<color=purple>N/A</color>";
            })));
        
            groupModel.Add<TextModel>(new TextModel("Food Supply Time", (Func<string>) (() =>
            {
                if (localFood != null && _evaScript != null)
                {
                    float foodPercentage = (float)(localFood.TotalFuel / localFood.TotalCapacity);
                    string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>"+Mod.GetStopwatchTimeString(localFood.TotalFuel / (Data.FoodComsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                return "N/A";
            })));
            AddGM("CO2 Level",localCo2);
            AddGM("Wasted Water Level",localWastedWater);
            AddGM("Solid Waste Level",localSolidWaste);
            
            void AddGM(string Title, IFuelSource source)
            {
                groupModel.Add<TextModel>(new TextModel(Title, (Func<string>) (() =>
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

            if (!isTourist)
            {
                TextButtonModel textButtonModel1 =
                    new TextButtonModel("Plant Flag", (Action<TextButtonModel>)(b => this.PlantFlagClick()));
                model.Add<TextButtonModel>(textButtonModel1);
                TextButtonModel textButtonModel2 =
                    new TextButtonModel("Deploy Parachute", (Action<TextButtonModel>)(b => this.DeployParachute()));
                model.Add<TextButtonModel>(textButtonModel2);
                
            }
            
        }   
        private void PlantFlagClick()
        {
            ICraftScript craftScript = this.PartScript.CraftScript;
            IFlightSceneUI ui = ModApi.Common.Game.Instance.FlightScene.FlightSceneUI;
            if (!(craftScript.Data.Assembly.Parts.Count == 1 &&craftScript.RootPart.Data.PartType.Name.Contains("Eva"))&&_evaScript.ActiveWhileInCrewCompartment)
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

            if (_evaScript.IsInWater)
            {
                ui.ShowMessage("Can Not Plant Flag,Drood is in water",false,10);
                return;
            }
            Mod.Instance.SpawnFlag();
        }

        private void DeployParachute()
        {
            ICraftScript craftScript = this.PartScript.CraftScript;
            IFlightSceneUI ui = ModApi.Common.Game.Instance.FlightScene.FlightSceneUI;

            bool isEva()
            {
                if (_evaScript.EvaActive)
                {
                    return !_evaScript.ActiveWhileInCrewCompartment;
                }
               return craftScript.Data.Assembly.Parts.Count == 1 && craftScript.RootPart.Data.PartType.Name.Contains("Eva");
            }
            if (!isEva())
            {
                ui.ShowMessage("Can Not Deploy Parachute,Not in Eva",false,10);
                return;
            }
            if (_evaScript.IsGrounded)
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
            
            if (_evaScript.IsInWater)
            {
                ui.ShowMessage("Can Not Deploy Parachute,Drood is in water",false,10);
                return;
            }

            //Debug.LogFormat($"调用前world{PartScript.Transform.position},localPosition{PartScript.Transform.localPosition} ,PCI{craftScript.FlightData.Position}");
            //PartScript.BodyScript.RigidBody.ResetCenterOfMass();
            CraftScript craftScript1 = this.PartScript.CraftScript as CraftScript;
            craftScript1.RecenterTransformOnCoM(true);
            //Debug.LogFormat($"recenter后 world{PartScript.Transform.position},localPosition{PartScript.Transform.localPosition} ,PCI{craftScript.FlightData.Position}");
            //Mod.Instance.SpawnParaGlider(PartScript.CraftScript as CraftScript);
            你去吃粑粑去吧();

        }

        /// <summary>
        /// 情况太鸡巴诡异了你知道吗?
        /// </summary>
        private void 你去吃粑粑去吧()
        {
           
            Vector3 orgVelocity = this.PartScript.BodyScript.RigidBody.velocity;
            Quaternion orgRotation = this.PartScript.BodyScript.RigidBody.rotation;
            Vector3 orgAngularVelocity = this.PartScript.BodyScript.RigidBody.angularVelocity; // 修正这里！
            
            //看看这里
            var parachutePartScript = CreateParachutePartScript();
        
            //我说我真的燃尽了
            Vector3d originalCraftPciPos = PartScript.CraftScript.FlightData.Position; 
            //Debug.LogFormat($"Drood的原来的world{PartScript.Transform.position}, local {PartScript.Transform.localPosition},pci{originalCraftPciPos}");
            Vector3 correctLocalPos = ConvertPCIToLocal(null,PartScript.CraftScript.ReferenceFrame.Center);
            //Debug.LogFormat($"计算出的修正后的local{correctLocalPos}");
            #region 我不想看
            //ConvertPciFromAToBLocal(this.PartScript as PartScript, script, originalCraftPciPos); // 自定义函数，见下
        
            
            // Step 3: 创建 Body 和 Group（此时 script 存在）
            BodyData body = CraftBuilder.CreateBodyData(new List<PartData>(){parachutePartScript.Data}, PartScript.CraftScript.Transform);
            CraftScript craftScript = PartScript.CraftScript as CraftScript;
            craftScript?.Data.Assembly.AddBody(body);
            BodyScript bodyScript = CraftBuilder.CreateBodyScript(craftScript, body);
            bodyScript.MoveToCraft(craftScript);
            bodyScript.OnInitialized();
        
            // 创建 Group 并设置 parent
            PartGroupScript groupScript = CreatePopPartGroup(bodyScript, parachutePartScript, this.PartScript.Data.Id + 1);
        
            
            // Step 4: 现在设置正确 localPosition（相对 group）
            // 因为 SetParent(worldPositionStays: false)，localPosition 会基于世界重新计算
            // 所以先设世界位置（如果需），或直接设 local（如果 group 是 zero）
            //就是这里

            #endregion

            //看这
            
            //parachutePartScript.BodyScript.Transform.position = correctLocalPos;
            //Debug.LogFormat($"script, world: {parachutePartScript.Transform.position}, local pos: {parachutePartScript.Transform.localPosition},PCI{parachutePartScript.CraftScript.FlightData.Position}");
            
            CraftBuilder.CalculateInertiaTensors(bodyScript, false);
            //_evaScript.OnNodeLoaded();
            _evaScript.OnPreNodeLoaded();
            
            parachutePartScript.BodyScript.RigidBody.velocity = orgVelocity;
            parachutePartScript.BodyScript.RigidBody.rotation = orgRotation;
            parachutePartScript.BodyScript.RigidBody.angularVelocity = orgAngularVelocity;
            
            //试试看
            //parachutePartScript.BodyScript.Transform.position = correctLocalPos;
            
            _evaScript.LoadIntoCrewCompartment(parachutePartScript.GetModifier<CrewCompartmentScript>(), null, announceBoarding: false);
            
        }

        #region  这他妈又是什么玩意我操
        private Vector3 ConvertPCIToLocal(IPartScript part, Vector3d pci)
        {
            Vector3 vector = this.PartScript.CraftScript.ReferenceFrame.PlanetToFrameVector(pci);
            bool flag = part != null;
            Vector3 result;
            if (flag)
            {
                result = part.Transform.InverseTransformDirection(vector);
            }
            else
            {
                result = this.PartScript.CraftScript.CenterOfMass.InverseTransformDirection(vector);
            }
            return result;
        }
        #endregion
        #region Paraglider
        private PartScript CreateParachutePartScript()
        {
            //Assembly assembly=new Assembly(ParachutePartElementWithPosition(orgPosition), 15, Game.Instance.PartTypes);
            Assembly assembly=new Assembly(ParachutePartElement(), 15, Game.Instance.PartTypes);
            CraftBuilder.CreatePartGameObjects(assembly.Parts, PartScript.CraftScript);
            PartData part = assembly.Parts[0];
            PartScript partScript = part.PartScript as PartScript;
            partScript.UpdateAttachPoints();
            PartScript.CraftScript.Data.Assembly.Absorb(assembly);
            //pop.Attach(partScript.GetModifier<PopulationScript>());
            return partScript;
        }
        private XElement ParachutePartElement()
        {
            
            DesignerPart designerpart = Game.Instance.CachedDesignerParts.Parts.First(d => d.Name == "Glider");
            XElement assembly = new XElement("Assembly", designerpart.AssemblyElement.Element("Parts"));
            //Debug.LogFormat($"看这里:{assembly.ToString()}");
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
        
    }
}