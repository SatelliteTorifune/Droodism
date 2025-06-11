using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Flight;
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
        private IFuelSource _oxygenSource, _foodSource, _waterSource;

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

        /// <summary>
        /// 指示小蓝人是否在奔或是否为游客。
        /// Flags indicating if the crew member is running or if they are a tourist.
        /// </summary>
        public bool isRunning, isTourist;
        
        /// <summary>
        /// 在创建modifiers时调用，启用零件属性。
        /// Called when modifiers are created, enables part properties.
        /// </summary>
        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            this.Data.PartPropertiesEnabled = true;
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
        /// <param name="fuelType">燃料类型（例如“氧气”、“食物”、“H2O”）。Type of fuel (e.g., "Oxygen", "Food", "H2O").</param>
        /// <param name="FuelCapacity">燃料罐的容量。Capacity of the fuel tank.</param>
        private void AddTank(String fuelType, float FuelCapacity)
        {
            XElement element = new XElement("FuelTank");
            element.SetAttributeValue("capacity", FuelCapacity);
            element.SetAttributeValue("fuel", FuelCapacity);
            element.SetAttributeValue("fuelType", fuelType);
            element.SetAttributeValue("utilization", -1);
            element.SetAttributeValue("autoFuelType", false);
            element.SetAttributeValue("subPriority", -1);
            element.SetAttributeValue("inspectorEnabled", false);
            element.SetAttributeValue("partPropertiesEnabled", false);
            element.SetAttributeValue("staticPriceAndMass", false);
            var tankData = PartModifierData.CreateFromStateXml(element, Data.Part, 15) as FuelTankData;
            try
            {
                tankData.InspectorEnabled = true;
                tankData.SubPriority = -1;
                
                
                var fuelTankScript = tankData.CreateScript() as FuelTankScript;
                
                if (fuelTankScript != null)
                {
                    try
                    {
                        fuelTankScript.FuelTransferMode = FuelTransferMode.None;
                    }
                    catch (Exception e)
                    {
                       Debug.LogFormat("Error while setting FuelTransferMode to None: {0}", e);
                    }
                    if (fuelTankScript.FuelType == null || fuelTankScript.FuelType.Id != fuelType)
                    {
                        return;
                    }

                    PartScript.Modifiers.Add(fuelTankScript);

                    if (!fuelTankScript.SupportsFuelTransfer)
                    { 
                        return;
                    }
                
                    
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("添加 {0} 类型的 FuelTank 失败: {1}", fuelType, e);
            }
        }
        
        /// <summary>
        /// 实现IFlightStart接口，在飞行场景开始时调用。
        /// Implements the IFlightStart interface, called at the start of the flight scene.
        /// </summary>
        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
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
            if (partData.Modifiers.Count <= 6)
            {
                AddTank("Oxygen", this.Data.DesireOxygenCapacity);
                AddTank("Food", this.Data.DesireFoodCapacity);
                AddTank("H2O", this.Data.DesireWaterCapacity);
            }
            
            try
            {
                CraftRefeshFuelSource();
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("2lazy{0}", e);
            }
            //_crewCompartmentScript.CrewEnter+=OnCrewEnter;
            //_crewCompartmentScript.CrewExit+=OnCrewExit;
        }

        private void OnCrewEnter(EvaScript evaScript)
        {
            RefreshFuelSource();
            Debug.Log("OnCrewEnter");
        }
        
        private void OnCrewExit(EvaScript evaScript)
        {
            RefreshFuelSource();
            Debug.Log("OnCrewExit");
        }
        /// <summary>
        /// 实现IFlightUpdate接口，在飞行期间每帧调用。
        /// Implements the IFlightUpdate interface, called every frame during flight.
        /// </summary>
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0) 
                return;
            if (_evaScript.EvaActive && _evaScript.IsPlayerCraft && !_evaScript.IsWalking && _evaScript.IsGrounded && (this.PartScript.CraftScript.SurfaceVelocity.magnitude >= 0.8))
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }
            ConsumptionLogic(frame);
            AutoRefillLogic(frame);
        }

        /// <summary>
        /// 从飞船中检索指定燃料类型的燃料源。
        /// Retrieves the fuel source for the specified fuel type from the craft.
        /// </summary>
        /// <param name="fuelType">要查找的燃料类型。Type of fuel to find.</param>
        /// <returns>如果找到则返回燃料源，否则返回null。The fuel source if found, otherwise null.</returns>
        private IFuelSource GetCraftFuelSource(string fuelType)
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

        /// <summary>
        /// 从零件的modifiers中检索指定燃料类型的本地燃料源。
        /// Retrieves the local fuel source for the specified fuel type from the part's modifiers.
        /// </summary>
        /// <param name="fuelType">要查找的燃料类型。Type of fuel to find.</param>
        /// <returns>如果找到则返回燃料源，否则返回null。The fuel source if found, otherwise null.</returns>
        private IFuelSource GetLocalFuelSource(string fuelType)
        {
            var craftSources = PartScript.Modifiers;
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

        /// <summary>
        /// 处理氧气、食物和水的消耗逻辑。
        /// Handles the consumption logic for oxygen, food, and water.
        /// </summary>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        private void ConsumptionLogic(in FlightFrameData frame)
        {
            if (_oxygenSource != null)
            {
                if (UsingInternalOxygen())
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
            }
            else
            {
                Debug.LogWarning("_oxygenSource is null");
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
        }

        /// <summary>
        /// 处理不使用内部氧气时的氧气自动补充逻辑。
        /// Handles the auto-refill logic for oxygen when not using internal oxygen.
        /// </summary>
        /// <param name="frame">飞行帧数据。Flight frame data.</param>
        private void AutoRefillLogic(in FlightFrameData frame)
        {
            if (_oxygenSource != null)
            {
                if (!UsingInternalOxygen())
                {
                    if (_oxygenSource.TotalCapacity - _oxygenSource.TotalFuel >= 0.001)
                    {
                        _oxygenSource.AddFuel(_oxygenSource.TotalCapacity * frame.DeltaTimeWorld * 0.10000000149011612);
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
            if (!Game.InFlightScene) 
                return;
        
            if (PartScript == null || PartScript.Modifiers == null)
            {
                return;
            }
            
            if (Game.InFlightScene)
            {
                try
                {
                    if (_evaScript.EvaActive)
                        CraftRefeshFuelSource();
                    else
                        CraftRefeshFuelSource();
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("2lazy{0}", e);
                }
                
                if (this._oxygenSource == null)
                {
                    Debug.LogWarning("未找到 Oxygen 类型的 FuelSource，可能影响 DamageDrood 逻辑");
                }
                if (this._foodSource == null)
                {
                    Debug.LogWarning("未找到 Food 类型的 FuelSource，可能影响 DamageDrood 逻辑");
                }
                if (this._waterSource == null)
                {
                    Debug.LogWarning("未找到 H2O 类型的 FuelSource，可能影响 DamageDrood 逻辑");
                }
            }
        }
        
        /// <summary>
        /// 在非EVA模式下刷新燃料源。
        /// Refreshes fuel sources when not in EVA mode.
        /// </summary>
        private void CraftRefeshFuelSource()
        {
            Debug.LogFormat("调用CraftRefeshFuelSource");
            _oxygenSource = GetCraftFuelSource("Oxygen");
            _foodSource = GetCraftFuelSource("Food");
            _waterSource = GetCraftFuelSource("H2O");
            if (_oxygenSource == null || _oxygenSource.IsEmpty)
            {
                _oxygenSource = GetLocalFuelSource("Oxygen");
            }
            else
            {
                ReFill(_oxygenSource, GetLocalFuelSource("Oxygen"));
            }
            if (_foodSource == null || _foodSource.IsEmpty)
            {
                _foodSource = GetLocalFuelSource("Food");
            }
            else
            {
                ReFill(_foodSource, GetLocalFuelSource("Food"));
            }
            if (_waterSource == null || _waterSource.IsEmpty)
            {
                _waterSource = GetLocalFuelSource("H2O");
            }
            else
            {
                ReFill(_waterSource, GetLocalFuelSource("H2O"));
            }
        }

        #region 无所弔谓
        /// <summary>
        /// 从源燃料源补充目标燃料源。
        /// Refills the target fuel source from the source fuel source.
        /// </summary>
        /// <param name="from">源燃料源。Source fuel source.</param>
        /// <param name="to">目标燃料源。Target fuel source.</param>
        private void ReFill(IFuelSource from, IFuelSource to)
        {
            if (from.TotalFuel >= to.TotalCapacity - to.TotalFuel)
            {
                from.RemoveFuel(to.TotalCapacity - to.TotalFuel);
                to.AddFuel(to.TotalCapacity - to.TotalFuel);
            }
            else
            {
                from.RemoveFuel(from.TotalFuel);
                to.AddFuel(from.TotalFuel);
            }
        }

        /// <summary>
        /// 在加载飞船时调用，触发飞船结构变化处理。
        /// Called when the craft is loaded, triggers craft structure change handling.
        /// </summary>
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            base.OnCraftLoaded(craftScript, movedToNewCraft);
            this.OnCraftStructureChanged(craftScript);
        }

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

            if (currentPlanetName.Contains("Droo") || currentPlanetName.Contains("Kerbin") ||
                currentPlanetName.Contains("Earth") || currentPlanetName.Contains("Nebra") ||
                currentPlanetName.Contains("Laythe") || currentPlanetName.Contains("Oord"))
            {
                return false;
            }
            
            bool isSubmerged = _evaScript.IsInWater && PartScript.CraftScript.FlightData.AltitudeAboveSeaLevel < 0.1;
            return isSubmerged;
        }

        /// <summary>
        /// 根据飞行场景数据更新当前行星名称。
        /// Updates the current planet name based on the flight scene data.
        /// </summary>
        private void UpdateCurrentPlanet()
        {
            IPlanetData planetData = Game.InFlightScene ? FlightSceneScript.Instance?.CraftNode?.Parent.PlanetData : null;
            currentPlanetName = planetData?.Name;
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

            if (_fuelSource.IsEmpty && _fuelSource.FuelType != null && 
                (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                this.PartScript.TakeDamage(num2 * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>Crew Member {_evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because running out of {_fuelSource.FuelType.Name}, " +
                    $"he/she has {Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / ((isRunning ? 1.75 : 1) * (isTourist ? 1.05 : 1) * DamageScale))} left",
                    false, 2f);
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
        }   
    }
}