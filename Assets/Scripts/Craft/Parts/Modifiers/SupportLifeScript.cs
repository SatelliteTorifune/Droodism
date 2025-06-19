using ModApi.Craft;
using ModApi.Craft.Parts;
using Assets.Scripts.Flight;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Input;
using ModApi;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Flight.GameView;
using ModApi.Planet;
using UnityEngine;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.Settings.Core;
using ModApi.Ui.Inspector;

//鸡巴的我自己都看不懂我写的是什么鸡巴玩意了你还指望我给你写注释吗?
//顺便一提如果真有除了我以外的人在github上或者逆向出来了看到了这行字,那么我只能说一句牛逼,你简直是找屎大王,能闻着味道找到我编程以来拉的最大的一坨

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    /// <summary>
    /// 定义一个Life Support的脚本类，继承自PartModifierScript，并实现设计器和飞行相关的接口。
    /// Defines a life support system script class, inheriting from PartModifierScript and implementing designer and flight-related interfaces.
    /// </summary>
    public class SupportLifeScript :
        PartModifierScript<SupportLifeData>,
        IDesignerStart,
        IFlightStart,
        IFlightUpdate
    {
        /// <summary>
        /// 引用EvaScript组件，用于管理舱外活动（EVA）相关功能。
        /// Reference to the EvaScript component, used to manage Extra-Vehicular Activity (EVA) related functionality.
        /// </summary>
        private EvaScript _evaScript;

        private CrewCompartmentScript _crewCompartmentScript;

        /// <summary>
        /// 氧气、食物和水的燃料源接口。
        /// Interfaces for the fuel sources of oxygen, food, and water.
        /// </summary>
        private IFuelSource _oxygenSource, _foodSource, _waterSource, _co2Source, _wastedWaterSource, _solidWasteSource;

        /// <summary>
        /// 氧气、食物和水的燃料罐脚本。
        /// Scripts for the fuel tanks of oxygen, food, and water.
        /// </summary>
        private FuelTankScript _oxygenFuelTank, _foodFuelTank, _waterFuelTank;

        /// <summary>
        /// 飞船当前所在行星的名称。
        /// Name of the current planet the craft is on.
        /// </summary>
        private string currentPlanetName;

        /// <summary>
        /// 当前行星的数据接口。
        /// Data interface for the current planet.
        /// </summary>
        private IPlanetData planetData;

        /// <summary>
        /// 氧气消耗速率。
        /// Rate at which oxygen is consumed.
        /// </summary>
        private float _oxygenConsumeRate;

        /// <summary>
        /// 飞船中的氧气燃料源。
        /// Fuel source for oxygen in the craft.
        /// </summary>
        private CraftFuelSource craftOxygenFuelSource;

        /// <summary>
        /// 抓钩脚本（如果适用）。
        /// Script for the grappling hook, if applicable.
        /// </summary>
        private GrapplingHookScript _grapplingHook;

        FlightSceneScript _flightSceneScript;

        public double LastUnloadedTime;



        /// <summary>
        /// 指示小蓝人是否在奔或是否为游客。
        /// Flags indicating if the crew member is running or if they are a tourist.
        /// </summary>
        public bool isRunning, isTourist, isFirstTime;

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
        /// 为指定的燃料类型添加具有给定容量的燃料罐。
        /// Adds a fuel tank for the specified fuel type with the given capacity.
        /// </summary>
        /// <param name="FuelCapacity">燃料罐的容量。Capacity of the fuel tank.</param>
        private void AddTank(string fuelType, double fuelCapacity, double fuelAmount)
        {
            if (fuelCapacity < fuelAmount)
            {
                fuelAmount = fuelCapacity;
            }

            Debug.LogFormat("Adding FuelTank for {0} with capacity {1} and fuel {2}", fuelType, fuelCapacity,
                fuelAmount);
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
                    Debug.LogError(
                        $"FuelTankScript for {fuelType} has invalid FuelType: {fuelTankScript.FuelType?.Id}");
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

                // 强制刷新 CraftFuelSources
                if (Game.InFlightScene && PartScript.CraftScript != null)
                {
                    //RefreshFuelSource();
                    Debug.LogFormat("从Addtank中强制刷新CraftFuelSources");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to add FuelTank for {fuelType}: {e}");
            }
        }

        public override void OnInitialLaunch()
        {
            isFirstTime = true;
            base.OnInitialLaunch();
            if (this.PartScript.Data.PartType.Name == "Eva-Tourist")
            {
                isTourist = true;
            }

            Data._foodAmountBuffer = this.Data.DesireFoodCapacity;
            Data._oxygenAmountBuffer = this.Data.DesireOxygenCapacity;
            Data._waterAmountBuffer = this.Data.DesireWaterCapacity;
            Data._co2AmountBuffer = 0;
            Data._wastedWaterAmountBuffer = 0;
            Data._solidWasteAmountBuffer = 0;
        }

        /// <summary>
        /// 实现IFlightStart接口，在飞行场景开始时调用。
        /// Implements the IFlightStart interface, called at the start of the flight scene.
        /// </summary>
        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
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
            _crewCompartmentScript = GetComponent<CrewCompartmentScript>();

            UpdateCurrentPlanet();
            Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
            PartData partData = this.Data.Part;
            LoadFuelTanks();

            try
            {
                CraftRefeshFuelSource();
                Debug.LogFormat("FlightStart调用RefeshFuelSource成功");
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("FlightStart调用RefeshFuelSource出问题了{0}", e);
            }
            
            Mod.Inctance.UpdateDroodCount();
        }

        /// <summary>
        /// 实现IFlightUpdate接口，在飞行期间每帧调用。
        /// Implements the IFlightUpdate interface, called every frame during flight.
        /// </summary>
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0)
                return;
            if (_evaScript.EvaActive && _evaScript.IsPlayerCraft && !_evaScript.IsWalking && _evaScript.IsGrounded &&
                (this.PartScript.CraftScript.SurfaceVelocity.magnitude >= 0.8))
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }

            try
            {
                ConsumptionLogic(frame);
            }
            catch (Exception e)
            {
                Debug.LogError($"SupportLifeScript.FlightUpdate.ConsumptionLogic出问题了{e}");
            }

            AutoRefillLogic(frame);
        }

        public void LoadFuelTanks()
        {
            Debug.Log("LoadFuelTanks调用");
            
            _oxygenSource = GetLocalFuelSource("Oxygen");
            _foodSource = GetLocalFuelSource("Food");
            _waterSource = GetLocalFuelSource("H2O");
            _co2Source = GetLocalFuelSource("CO2");
            _wastedWaterSource = GetLocalFuelSource("Wasted Water");
            _solidWasteSource = GetLocalFuelSource("Solid Waste");
            Debug.LogFormat("LoadFUelTank获取到本地燃料源{0},{1},{2},{3},{4},{5}", _oxygenSource, _foodSource, _waterSource, _co2Source, _wastedWaterSource, _solidWasteSource);

            void LoadFuelTanksHandeler(ref IFuelSource source, double capacity, double amount)
            {
                try
                {
                    Debug.Log("这一步有问题那么这辈子有了");
                    if (source == null)
                    {
                    
                        Debug.LogFormat($"好了{source.FuelType.Id}有问题了");
                        AddTank(source.FuelType.Id, capacity, amount);
                        Debug.LogFormat("添加 {0} 类型的 FuelTank容量{1}实际{2}", source.FuelType.Id, capacity, amount);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogFormat("LoadFuelTanksHandeler出问题了{0}", e);
                }
                
            }

            LoadFuelTanksHandeler(ref _oxygenSource, this.Data.DesireOxygenCapacity, Data._oxygenAmountBuffer);
            LoadFuelTanksHandeler(ref _foodSource, this.Data.DesireFoodCapacity, Data._foodAmountBuffer);
            LoadFuelTanksHandeler(ref _waterSource, this.Data.DesireWaterCapacity, Data._waterAmountBuffer);
            LoadFuelTanksHandeler(ref _co2Source, Data.DesireOxygenCapacity * 1.1, Data._co2AmountBuffer);
            LoadFuelTanksHandeler(ref _wastedWaterSource, Data.DesireWaterCapacity * 1.1, Data._wastedWaterAmountBuffer);
            LoadFuelTanksHandeler(ref _solidWasteSource, Data.DesireFoodCapacity * 1.1, Data._solidWasteAmountBuffer);
            
            CraftRefeshFuelSource();
            Debug.LogFormat("LoadFuelTanks调用CraftRefeshFuelSource成功");

        }

        /// <summary>
        /// 从飞船中检索指定燃料类型的燃料源。
        /// Retrieves the fuel source for the specified fuel type from the craft.
        /// </summary>
        /// <param name="fuelType">要查找的燃料类型。Type of fuel to find.</param>
        /// <returns>如果找到则返回燃料源，否则返回null。The fuel source if found, otherwise null.</returns>
        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            if (PartScript.Data.IsRootPart)
            {
                return null;
            }

            try
            {
                var craftSources = PartScript.CraftScript.FuelSources.FuelSources;
                foreach (var source in craftSources)
                {
                    if (source.FuelType.Id.Contains(fuelType))
                    {
                        return source;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("GetCraftFuelSource出问题了{0}", e);
            }

            return null;


        }

        /// <summary>
        /// 从零件的modifiers中检索指定燃料类型的本地燃料源。
        /// Retrieves the local fuel source for the specified fuel type from the part's modifiers.
        /// </summary>
        /// <param name="fuelType">要查找的燃料类型。Type of fuel to find.</param>
        /// <returns>如果找到则返回燃料源，否则返回null。The fuel source if found, otherwise null.</returns>
        private IFuelSource GetLocalFuelSource(string fuelType)
        {
            try
            {
                var craftSources = PartScript.Modifiers;
                if (craftSources.Count<=8)
                {
                    return null;
                }
                foreach (var source in craftSources)
                {
                    if (source.GetData().Name.Contains("Tank"))
                    {
                        source.GetData().InspectorEnabled = false;
                        FuelTankScript fts = source as FuelTankScript;
                        if (fts.FuelType.Id.Contains(fuelType))
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
        /// </summary>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        private void ConsumptionLogic(in FlightFrameData frame)
        {
            try
            {
                void checkSouce(ref IFuelSource source, string fuelType)
                {
                    if (source == null)
                    {
                        Debug.Log($"{fuelType} source is null");
                        RefreshFuelSource();   
                        
                        Debug.LogFormat("从ConsumptionLogic调用RefeshFuelSource");
                    }
                }
                checkSouce(ref _oxygenSource, "Oxygen");
                checkSouce(ref _foodSource, "Food");
                checkSouce(ref _waterSource, "H2O");
                checkSouce(ref _co2Source, "CO2");
                checkSouce(ref _wastedWaterSource, "Wasted Water");
                checkSouce(ref _solidWasteSource, "Solid Waste");
                
                
        
                // 验证 Data
                if (Data == null)
                {
                    Debug.LogError("Data is null in ConsumptionLogic");
                    return;
                }
        
                // Oxygen 和 CO2 处理
                if (_oxygenSource != null && _co2Source != null && UsingInternalOxygen())
                {
                    double num1 = Data.OxygenComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1);
                    double num2 = num1 * 1.375 * Data.evaConsumeEfficiency;
        
                    if (_oxygenSource.IsEmpty)
                    {
                        _oxygenSource = GetLocalFuelSource("Oxygen");
                        if (_oxygenSource == null || _oxygenSource.IsEmpty)
                        {
                            Debug.LogWarning("Oxygen source 为空，无法消耗");
                            DamageDrood(_oxygenSource, frame, Data.OxygenDamageScale);
                        }
                        else
                        {
                            _oxygenSource.RemoveFuel(num1);
                        }
                    }
                    else
                    {
                        _oxygenSource.RemoveFuel(num1);
                    }
        
                    if (_co2Source.TotalCapacity - _co2Source.TotalFuel >= 0.00001 && !_oxygenSource.IsEmpty)
                    {
                        var localCO2Source = GetLocalFuelSource("CO2");
                        if (localCO2Source != null && localCO2Source.TotalCapacity - localCO2Source.TotalFuel >= 0.00001)
                        {
                            localCO2Source.AddFuel(num2);
                        }
                        if(localCO2Source!=null&&localCO2Source.TotalCapacity - localCO2Source.TotalFuel < 0.00001)
                        {
                            DamageDroodWaste(localCO2Source, frame, Data.OxygenDamageScale);
                        }
                    }
                }
        
                // Food 和 Solid Waste 处理
                if (_foodSource != null && _solidWasteSource != null)
                {
                    double num1 = Data.FoodComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1);
                    double num2 = num1 * 1.1 * Data.evaConsumeEfficiency;
        
                    if (_foodSource.IsEmpty)
                    {
                        _foodSource = GetLocalFuelSource("Food");
                        if (_foodSource == null || _foodSource.IsEmpty)
                        {
                            Debug.LogWarning("Food source 为空，无法消耗");
                            DamageDrood(_foodSource, frame, Data.FoodDamageScale);
                        }
                        else
                        {
                            _foodSource.RemoveFuel(num1);
                        }
                    }
                    else
                    {
                        _foodSource.RemoveFuel(num1);
                    }
        
                    if (_solidWasteSource.TotalCapacity - _solidWasteSource.TotalFuel >= 0.00001)
                    {
                        var localSolidWasteSource = GetLocalFuelSource("Solid Waste");
                        if (localSolidWasteSource != null && localSolidWasteSource.TotalCapacity - localSolidWasteSource.TotalFuel >= 0.00001)
                        {
                            localSolidWasteSource.AddFuel(num2);
                        }
                        if(localSolidWasteSource != null && localSolidWasteSource.TotalCapacity - localSolidWasteSource.TotalFuel <= 0.00001)
                        {
                            DamageDroodWaste(localSolidWasteSource, frame, Data.FoodDamageScale);
                        }
                    }
                }
        
                // Water 和 Wasted Water 处理
                if (_waterSource != null && _wastedWaterSource != null)
                {
                    double num1 = Data.WaterComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1);
                    double num2 = num1 * 1.05 * Data.evaConsumeEfficiency;
        
                    if (_waterSource.IsEmpty)
                    {
                        _waterSource = GetLocalFuelSource("H2O");
                        if (_waterSource == null || _waterSource.IsEmpty)
                        {
                            Debug.LogWarning("Water source 为空，无法消耗");
                            DamageDrood(_waterSource, frame, Data.WaterDamageScale);
                        }
                        else
                        {
                            _waterSource.RemoveFuel(num1);
                        }
                    }
                    else
                    {
                        _waterSource.RemoveFuel(num1);
                    }
        
                    if (_wastedWaterSource.TotalCapacity - _wastedWaterSource.TotalFuel >= 0.00001 && !_waterSource.IsEmpty)
                    {
                        _wastedWaterSource = GetLocalFuelSource("Wasted Water");

                        
                        if (_wastedWaterSource != null && _wastedWaterSource.TotalCapacity - _wastedWaterSource.TotalFuel >= 0.00001)
                        {
                            _wastedWaterSource.AddFuel(num2);
                        }
                        if (_wastedWaterSource != null && _wastedWaterSource.TotalCapacity - _wastedWaterSource.TotalFuel <= 0.00001)
                        {
                            DamageDroodWaste(_wastedWaterSource, frame, Data.WaterDamageScale);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ConsumptionLogic 出错: {e}");
            }
        }

        /// <summary>
        /// 处理不使用内部氧气时的氧气自动补充逻辑。
        /// Handles the auto-refill logic for oxygen when not using internal oxygen.
        /// </summary>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        private void AutoRefillLogic(in FlightFrameData frame)
        {
            if (_oxygenSource != null&&_co2Source!= null)
            {
                if (!UsingInternalOxygen())
                {
                    if (_oxygenSource.TotalCapacity - _oxygenSource.TotalFuel >= 0.00001)
                    {
                        _oxygenSource.AddFuel(_oxygenSource.TotalCapacity * frame.DeltaTimeWorld * 0.10000000149011612);
                    }
                    if (!_co2Source.IsEmpty)
                    {
                        _co2Source.RemoveFuel(_co2Source.TotalCapacity * frame.DeltaTimeWorld * 0.10000000149011612);
                    }
                }
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
                    CraftRefeshFuelSource();
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
        private void CraftRefeshFuelSource()
        {
            try
            {
                UpdateCurrentPlanet();
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("{0}", e);
            }
            void HandleFuelSource(string fuelType, double capacity, double bufferAmount, ref IFuelSource fuelSource)
            {
                try
                {
                    if (fuelType.Contains("Waste")||fuelType=="CO2")
                    {
                        if (fuelSource == null || fuelSource.TotalCapacity-fuelSource.TotalFuel <=0.00001)
                        {
                            fuelSource = GetLocalFuelSource(fuelType);
                            if (fuelSource != null)
                            {
                                Debug.Log($"设置为本地 {fuelType}");
                            }
                            else
                            {
                                Debug.LogWarning($"未找到 {fuelType} 类型的 FuelSource");
                                try
                                {
                                    AddTank(fuelType, capacity, bufferAmount);
                                    fuelSource = GetLocalFuelSource(fuelType); // 重新获取
                                    Debug.Log($"从 CraftRefeshFuelSource 调用 AddTank {fuelType} 成功");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"从 CraftRefeshFuelSource 调用 AddTank {fuelType} 出错: {e}");
                                }
                            }
                        }
                        else
                        {
                           RemoveWaste(fuelSource, GetLocalFuelSource(fuelType));
                        
                        }
                    }
                    else
                    {
                        if (fuelSource == null || fuelSource.IsEmpty)
                        {
                            fuelSource = GetLocalFuelSource(fuelType);
                            if (fuelSource != null)
                            {
                                Debug.Log($"设置为本地 {fuelType}");
                            }
                            else
                            {
                                Debug.LogWarning($"未找到 {fuelType} 类型的 FuelSource");
                                try
                                {
                                    AddTank(fuelType, capacity, bufferAmount);
                                    fuelSource = GetLocalFuelSource(fuelType); // 重新获取
                                    Debug.Log($"从 CraftRefeshFuelSource 调用 AddTank {fuelType} 成功");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"从 CraftRefeshFuelSource 调用 AddTank {fuelType} 出错: {e}");
                                }
                            }
                        }
                        else
                        {
                            ReFill(fuelSource, GetLocalFuelSource(fuelType));
                        
                        } 
                    }
                    
                }
                catch (Exception e)
                {
                    Debug.LogError($"处理 {fuelType} FuelSource 出错: {e}");
                }
            }
            Debug.LogFormat("调用CraftRefeshFuelSource");
            try
            {
                _oxygenSource = GetCraftFuelSource("Oxygen");
                _foodSource = GetCraftFuelSource("Food");
                _waterSource = GetCraftFuelSource("H2O");
                _co2Source=GetCraftFuelSource("CO2");
                _wastedWaterSource=GetCraftFuelSource("Wasted Water");
                _solidWasteSource=GetCraftFuelSource("Solid Waste");
                if (_oxygenSource != null && _foodSource != null && _waterSource != null&&_co2Source!= null&& _wastedWaterSource!= null&& _solidWasteSource != null)
                {
                    Debug.LogFormat("刷新完成 Oxygen:{0},Food:{1},Water:{2},CO2:{3},WastedWater:{4},SolidWaste:{5}", _oxygenSource.TotalFuel, _foodSource.TotalFuel, _waterSource.TotalFuel, _co2Source.TotalFuel, _wastedWaterSource.TotalFuel, _solidWasteSource.TotalFuel);
                    SaveFuelAmountBuffer();
                    
                }
                HandleFuelSource("Oxygen", Data.DesireOxygenCapacity, Data._oxygenAmountBuffer, ref _oxygenSource);
                HandleFuelSource("Food", Data.DesireFoodCapacity, Data._foodAmountBuffer, ref _foodSource);
                HandleFuelSource("H2O", Data.DesireWaterCapacity, Data._waterAmountBuffer, ref _waterSource);
                HandleFuelSource("CO2", Data.DesireOxygenCapacity*1.1, Data._co2AmountBuffer, ref _co2Source);
                HandleFuelSource("Wasted Water", Data.DesireWaterCapacity*1.1, Data._wastedWaterAmountBuffer, ref _wastedWaterSource);
                HandleFuelSource("Solid Waste", Data.DesireFoodCapacity*1.1, Data._solidWasteAmountBuffer, ref _solidWasteSource);
                SaveFuelAmountBuffer();
                
                
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("CraftRefeshFuelSource出问题了{0}", e);
            }
            
        }

        
        /// <summary>
        /// 从源燃料源补充目标燃料源。
        /// Refills the target fuel source from the source fuel source.
        /// </summary>
        /// <param name="Craft">源燃料源。Source fuel source.</param>
        /// <param name="Eva">目标燃料源。Target fuel source.</param>
        private void ReFill(IFuelSource Craft, IFuelSource Eva)
        {
            if (Craft.TotalFuel >= Eva.TotalCapacity - Eva.TotalFuel)
            {
                Craft.RemoveFuel(Eva.TotalCapacity - Eva.TotalFuel);
                Eva.AddFuel(Eva.TotalCapacity - Eva.TotalFuel);
            }
            else
            {
                Craft.RemoveFuel(Craft.TotalFuel);
                Eva.AddFuel(Craft.TotalFuel);
            }
            
        }

        private void RemoveWaste(IFuelSource Craft, IFuelSource Eva)
        {
            if (Craft.TotalCapacity-Craft.TotalFuel>Eva.TotalFuel)
            {
                Craft.AddFuel(Eva.TotalFuel);
                Eva.RemoveFuel(Eva.TotalFuel);
            }
            else
            {
                Eva.RemoveFuel(Craft.TotalCapacity - Craft.TotalFuel);
                Craft.AddFuel(Craft.TotalCapacity - Craft.TotalFuel);
            }
        }
        

        /// <summary>
        /// 在加载飞船时调用，触发飞船结构变化处理。
        /// Called when the craft is loaded, triggers craft structure change handling.
        /// </summary>
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            base.OnCraftLoaded(craftScript, movedToNewCraft);
            
            Debug.LogFormat("OnCraftLoaded called for Part ID: {0}, MovedToNewCraft: {1}", PartScript.Data.Id, movedToNewCraft);
            
        }
        
        
        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            Debug.Log("OnFlightEnded");
            OnCraftUnloaded();
        }
        /// <summary>
        /// Called when the craft ends, removes extra fuel tanks.
        public void OnPhysicsEnabled(ICraftNode craftNode, PhysicsChangeReason reason)
        {
            Debug.LogFormat("OnPhysicsEnabled{0}",reason);
            if (reason == PhysicsChangeReason.Warp)
            {
                return;
            }
            
            if (isFirstTime)
            {
                return;
            }
            LoadFuelTanks();
            RefreshFuelSource();
            Debug.LogFormat("从OnPhysicsEnabled调用RefreshFuelSource");
            RemoveFuelAmonutInstantly();
        }
        public void OnPhysicsDisabled(ICraftNode craftNode, PhysicsChangeReason reason)
        {
            isFirstTime = false;
            Debug.LogFormat("OnPhysicsDisabled 原因:{0}",reason);
            if (reason == PhysicsChangeReason.Warp||reason== PhysicsChangeReason.UnloadedFromGameView)
            {
                return;
            }
            OnCraftUnloaded();
            Debug.LogFormat("OnPhysicsDisabled调用OnCraftUnloaded");
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
                LastUnloadedTime = Game.Instance.GameState.GetCurrentTime();
                SaveFuelAmountBuffer();
                this.PartScript.Data.LoadXML(
                RemoveFuelTankXML(this.PartScript.Data.GenerateXml(this.PartScript.CraftScript.Transform,false)),15);
                
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
                if (_oxygenSource != null && _foodSource != null && _foodSource != null&& _co2Source!= null&& _wastedWaterSource!= null&& _solidWasteSource != null)
                {
                    Data._oxygenAmountBuffer = _oxygenSource.TotalFuel;
                    Data._foodAmountBuffer = _foodSource.TotalFuel;
                    Data._waterAmountBuffer = _waterSource.TotalFuel;
                    Data._co2AmountBuffer = _co2Source.TotalFuel;
                    Data._wastedWaterAmountBuffer = _wastedWaterSource.TotalFuel;
                    Data._solidWasteAmountBuffer = _solidWasteSource.TotalFuel;
                    Debug.LogFormat("缓冲区燃料:食物{0},oxygen{1},water,{2},co2,{3},wastedWater,{4},solidWaste,{5}", Data._foodAmountBuffer, Data._oxygenAmountBuffer, Data._waterAmountBuffer, Data._co2AmountBuffer, Data._wastedWaterAmountBuffer, Data._solidWasteAmountBuffer);
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

        public void RemoveFuelAmonutInstantly()
        {
            
            var time= Game.Instance.GameState.GetCurrentTime()-LastUnloadedTime;
            Debug.LogFormat($"调用RemoveFuelAmonutInstantly ,间隔是{Units.GetStopwatchTimeString(time)}");

            #region Debug MSG
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

            if (_co2Source == null)
            {
                Debug.LogFormat("调用RemoveFuelAmonutInstantly失败,_co2Source有他妈null");
                return;
            }

            if (_wastedWaterSource == null)
            {
                Debug.LogFormat("调用RemoveFuelAmonutInstantly失败,_wastedWaterSource有他妈null");
                return;
            }

            if (_solidWasteSource == null)
            {
                Debug.LogFormat("调用RemoveFuelAmonutInstantly失败,_solidWasteSource有他妈null");
                return;
            
            }
            #endregion
            
            if (Data.FoodComsumeRate*(time/360) > this._foodSource.TotalFuel)
            {
                this._foodSource.RemoveFuel(_foodSource.TotalCapacity);
                Debug.LogFormat("调用RemoveFuelAmonutInstantly,理论:{0}实际{1}",Data.FoodComsumeRate*(time/360),this._foodSource.TotalFuel);
            }
            else
            {
                this._foodSource.RemoveFuel(Data.FoodComsumeRate*(time/360));
            }
            
            if (Data.WaterComsumeRate*(time/360) > this._waterSource.TotalFuel)
            {
                this._waterSource.RemoveFuel(_waterSource.TotalCapacity);
            }
            else
            {
                this._waterSource.RemoveFuel(Data.WaterComsumeRate*(time/360));
            }

            if (UsingInternalOxygen())
            {
                if (Data.OxygenComsumeRate*(time/360) > this._oxygenSource.TotalFuel)
                {
                    this._oxygenSource.RemoveFuel(_oxygenSource.TotalCapacity);
                }
                else
                {
                    this._oxygenSource.RemoveFuel(Data.OxygenComsumeRate*(time/360));
                }
            }
            
        }
        #region 无所弔谓
        /// <summary>
        /// 在飞船结构变化时调用，如果在飞行场景中，则刷新燃料源。
        /// Called when the craft structure changes, refreshes fuel sources if in flight scene.
        /// </summary>
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            if (Game.InFlightScene)
            {
                RefreshFuelSource();
                Debug.LogWarningFormat("从OnCraftStructureChanged调用RefreshFuelSource");
            }
        }
        
        /// <summary>
        /// 飞船燃料源变化时的事件处理程序，刷新燃料源。
        /// Event handler for when craft fuel sources change, refreshes fuel sources.
        /// </summary>
        private void OnCraftFuelSourceChanged(object sender, EventArgs e)
        {
            RefreshFuelSource();
            Debug.LogWarningFormat("从OnCraftFuelSourceChanged调用RefreshFuelSource();");
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
                else
                    return true;
            }
            return true;
        }

        /// <summary>
        /// 根据飞行场景数据更新当前行星名称。
        /// Updates the current planet name based on the flight scene data.
        /// </summary>
        public void UpdateCurrentPlanet()
        {
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
                        $"he/she has {Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1) * DamageScale))} left",
                        false, 2f);
                }
            }
            
            
        }

        private void DamageDroodWaste(IFuelSource _fuelSource, FlightFrameData frame, float DamageScale)
        {
            if (_fuelSource == null || _evaScript == null || PartScript == null || 
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
                Debug.LogError("DamageDroodWaste: null object found: - " +
                               $"_fuelSource={_fuelSource != null}, _evaScript={_evaScript != null}, " +
                               $"PartScript={PartScript != null}, Game.Instance={Game.Instance != null}, Settings={Game.Instance?.Settings != null}");
                return;
            }

            if (frame.DeltaTimeWorld <= 0)
            {
                return;
            } 
            float damage=(isRunning ? 1.75f : 1f) * (isTourist ? 1.05f : 1f) * DamageScale * (float)frame.DeltaTimeWorld;
            
            if (_fuelSource.TotalCapacity- _fuelSource.TotalFuel < 0.00001)
            {
                
                if ( _fuelSource.FuelType != null && 
                     (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
                {
                    this.PartScript.TakeDamage(damage * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                        $"<color=red>Crew Member {_evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because {_fuelSource.FuelType.Name} level is too high" +
                        $"he/she has {Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1) * DamageScale))} left",
                        false, 2f);
                }
            }
        }
        /// <summary>
        /// 为零件生成inspector model，添加生命支持信息。
        /// Generates the inspector model for the part, adding life support information.
        /// </summary>
        public override void OnGenerateInspectorModel(PartInspectorModel model) 
        {
            base.OnGenerateInspectorModel(model);
            GroupModel groupModel = new GroupModel("<color=green><size=115%>Life Support Info");
            model.AddGroup(groupModel);
            groupModel.Add<TextModel>(new TextModel("Remain Oxygen", (Func<string>) (() =>
            {
                if (UsingInternalOxygen() && _oxygenSource != null && _oxygenSource.TotalCapacity > 0)
                {
                    float percentage = (float)(_oxygenSource.TotalFuel / _oxygenSource.TotalCapacity);
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
                if (UsingInternalOxygen() && _oxygenSource != null && _evaScript != null)
                {
                    float percentage = (float)(_oxygenSource.TotalFuel / _oxygenSource.TotalCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>"+Units.GetStopwatchTimeString(_oxygenSource.TotalFuel / (Data.OxygenComsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                else if (!UsingInternalOxygen())
                {
                    return "<color=green>Infinity</color>";
                }
                return "N/A";
            })));
            
            groupModel.Add<TextModel>(new TextModel("Remain Water", (Func<string>) (() =>
            {
                if (_waterSource != null && _waterSource.TotalCapacity > 0)
                {
                    float waterPercentage = (float)(_waterSource.TotalFuel / _waterSource.TotalCapacity);
                    string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={waterTextColor}>{Units.GetPercentageString(waterPercentage)}</color>";
                }
                return "<color=purple>N/A</color>";
            })));
            
            groupModel.Add<TextModel>(new TextModel("Water Supply Time", (Func<string>) (() =>
            {
                if (_waterSource != null && _evaScript != null)
                {
                    float waterPercentage = (float)(_waterSource.TotalFuel / _waterSource.TotalCapacity);
                    string waterTextColor = waterPercentage > 0.5 ? "green" : waterPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={waterTextColor}>"+Units.GetStopwatchTimeString(_waterSource.TotalFuel / (Data.WaterComsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                return "N/A";
            })));
            
            groupModel.Add<TextModel>(new TextModel("Remain Food", (Func<string>) (() =>
            {
                if (_foodSource != null && _foodSource.TotalCapacity > 0)
                {
                    float foodPercentage = (float)(_foodSource.TotalFuel / _foodSource.TotalCapacity);
                    string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>{Units.GetPercentageString(foodPercentage)}</color>";
                }
                return "<color=purple>N/A</color>";
            })));
        
            groupModel.Add<TextModel>(new TextModel("Food Supply Time", (Func<string>) (() =>
            {
                if (_foodSource != null && _evaScript != null)
                {
                    float foodPercentage = (float)(_foodSource.TotalFuel / _foodSource.TotalCapacity);
                    string foodTextColor = foodPercentage > 0.5 ? "green" : foodPercentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>"+Units.GetStopwatchTimeString(_foodSource.TotalFuel / (Data.FoodComsumeRate * (isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1)));
                }
                return "N/A";
            })));
            AddGM("CO2 Level",_co2Source);
            AddGM("Wasted Water Level",_wastedWaterSource);
            AddGM("Solid Waste Level",_solidWasteSource);
            
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
        }   
    }
}