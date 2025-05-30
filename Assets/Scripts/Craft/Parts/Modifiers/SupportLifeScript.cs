using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Windows.Forms;
using System.Text;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Flight;
using ModApi.Craft.Propulsion;
using ModApi.Planet;
using UnityEngine;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.Settings.Core;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class SupportLifeScript : 
        PartModifierScript<SupportLifeData>,
        IDesignerStart,
        IFlightStart,
        IFlightUpdate
    {
        private EvaScript _evaScript;
        
        private IFuelSource _oxygenSource;
        private FuelTankScript _oxygenFuelTank;
        
        private IFuelSource _foodSource;
        private FuelTankScript _foodFuelTank;
        
        private IInputController _inputThrottle;
        private float _fuelRemoved;
        private string currentPlanetName;
        private IPlanetData planetData;
        private float _oxygenConsumeRate;
        
        private CraftFuelSource craftOxygenFuelSource;
        
        private GrapplingHookScript _grapplingHook;
        
        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            if (Game.InFlightScene)
            {
                RefreshFuelSource();
            }
        }
        
        private FuelTankScript OxygenFuelTank
        {
            get => this._oxygenFuelTank;
            set
            {
                if (this._oxygenFuelTank == value) 
                    return;
                if (Game.InFlightScene && this._oxygenFuelTank != null)
                    this._oxygenFuelTank.CraftFuelSourceChanged -= new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
                this._oxygenFuelTank = value;
                if (!Game.InFlightScene || this._oxygenFuelTank == null)
                    return;
                this._oxygenFuelTank.CraftFuelSourceChanged += new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
            }
        }

        private FuelTankScript FuelTankFood
        {
            get => this._foodFuelTank;
            set
            {
                if (this._foodFuelTank == value) 
                    return;
                if (Game.InFlightScene && this._foodFuelTank != null)
                    this._foodFuelTank.CraftFuelSourceChanged -= new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
                this._foodFuelTank = value;
                if (!Game.InFlightScene || this._foodFuelTank == null)
                    return;
                this._foodFuelTank.CraftFuelSourceChanged += new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
            }
        }

        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
        }
        
        private void AddTank(String fuelType, float FuelCapacity)
        {
            XElement element = new XElement("FuelTank");
            element.SetAttributeValue("capacity", FuelCapacity);
            element.SetAttributeValue("fuel", FuelCapacity);
            element.SetAttributeValue("fuelType", fuelType);
            element.SetAttributeValue("utilization", -1);
            element.SetAttributeValue("autoFuelType", false);
            element.SetAttributeValue("subPriority", -1);
            element.SetAttributeValue("inspectorEnabled",false);
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
                    Debug.LogFormat("创建 FuelTank，FuelType: {0}", fuelTankScript.FuelType?.Name ?? "null");
                    if (fuelTankScript.FuelType == null || fuelTankScript.FuelType.Name != fuelType)
                    {
                        Debug.LogErrorFormat("FuelType {0} 未正确设置！", fuelType);
                        return;
                    }

                    PartScript.Modifiers.Add(fuelTankScript);
                    Debug.LogFormat("成功添加 {0} 类型的 FuelTank 到 PartScript.Modifiers", fuelType);

                    if (!fuelTankScript.SupportsFuelTransfer)
                    {
                        Debug.LogErrorFormat("{0} 类型的 FuelTank 不支持燃料传输（PartScript.Disconnected = {1}）", 
                            fuelType, fuelTankScript.PartScript.Disconnected);
                        return;
                    }

                    fuelTankScript.FuelTransferMode = FuelTransferMode.Fill;
                    Debug.LogFormat("设置 {0} 类型的 FuelTank 的 FuelTransferMode 为 Fill", fuelType);

                    if (fuelTankScript.CraftFuelSource == null)
                    {
                        Debug.LogWarningFormat("{0} 类型的 FuelTank 的 CraftFuelSource 仍然为 null，将直接使用 FuelTankScript 作为 FuelSource", fuelType);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("创建 {0} 类型的 FuelTankScript 失败", fuelType);
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("添加 {0} 类型的 FuelTank 失败: {1}", fuelType, e);
            }
        }
        
        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            this.Data.InspectorEnabled = true;
            try
            {
                _evaScript = GetComponent<EvaScript>();
            }
            catch(Exception e)
            {
                Debug.LogErrorFormat("Eva初始化失败!{0}", e);
            } 
            
            bool inFlightScene = Game.InFlightScene;
            UpdateCurrentPlanet();
            Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
            PartData partData = this.Data.Part;
            if (partData.Modifiers.Count <= 6)
            {
                AddTank("Oxygen", this.Data.DesireOxygenCapacity);
                AddTank("Food", this.Data.DesireFoodCapacity);
            }
            OnCraftStructureChanged(this.PartScript.CraftScript);
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0 || !_evaScript.EvaActive) 
                return;

            if (_oxygenSource != null)
            {
               
                if (UsingInternalOxygen() && _oxygenSource != null )
                {
                    double num1 = (double)Data.OxygenComsumeRate * frame.DeltaTimeWorld * (_evaScript.IsWalking ? 1 : 1.5);
                    if (_oxygenSource.IsEmpty)
                    {
                        DamageDrood(_oxygenSource, frame, Data.OxygenDamageScale);
                        return;
                    }
                    _oxygenSource.RemoveFuel(num1);
                }
                
                else
                {
                    if (_oxygenSource == null)
                        Debug.LogWarning("_oxygenSource is Null");
                }
            }
            else
            {
                Debug.LogWarning("跳过 DamageDrood 调用：_oxygenSource 为 null");
            }

            if (_foodSource != null)
            {
                if (_foodSource.IsEmpty)
                {
                    DamageDrood(_foodSource,frame,Data.FoodDamageScale);
                    return;
                }
                _foodSource.RemoveFuel(frame.DeltaTimeWorld * Data.FoodComsumeRate);
                
            }
            else
            {
                Debug.LogWarning("_foodSource is Null");
            }

            
            
        }

        private void RefreshFuelSource()
        {
            if (!Game.InFlightScene) 
            {
                return;
            }
            Debug.LogFormat("调用RefreshFuelSource");
        
            if (PartScript == null || PartScript.Modifiers == null)
            {
                Debug.LogError("PartScript or PartScript.Modifiers is null，Unable to get FuelSource");
                return;
            }
            
        
            foreach (var modifier in this.PartScript.Modifiers)
            {
                if (modifier == null)
                {
                    continue;
                }
        
                var modifierData = modifier.GetData();
                if (modifierData == null || string.IsNullOrEmpty(modifierData.Name))
                {
                    continue;
                }
        
                if (modifierData.Name.Contains("FuelTank"))
                {
                    var fuelTank = modifier as FuelTankScript;
                    if (fuelTank != null && fuelTank.FuelType != null)
                    {
                        Debug.LogFormat("找到 FuelTank，FuelType: {0}, CraftFuelSource: {1}", 
                            fuelTank.FuelType.Name, fuelTank.CraftFuelSource != null ? "存在" : "null");
        
                        switch (fuelTank.FuelType.Name)
                        {
                            case "Oxygen":
                                this._oxygenFuelTank = fuelTank;
                                this._oxygenSource = fuelTank.CraftFuelSource != null ? (IFuelSource)fuelTank.CraftFuelSource : (IFuelSource)fuelTank;
                                Debug.LogFormat("已更新 Oxygen 的 FuelSource: {0}", this._oxygenSource != null ? "成功" : "失败");
                                break;
                            case "Food":
                                this._foodFuelTank = fuelTank;
                                this._foodSource = fuelTank.CraftFuelSource != null ? (IFuelSource)fuelTank.CraftFuelSource : (IFuelSource)fuelTank;
                                Debug.LogFormat("已更新 Food 的 FuelSource: {0}", this._foodSource != null ? "成功" : "失败");
                                break;
                            case "Jetpack":
                                Debug.LogFormat("跳过 Jetpack 的 FuelSource 更新");
                                break;
                            default:
                                Debug.LogWarningFormat("未知的 FuelType: {0}", fuelTank.FuelType.Name);
                                break;
                        }
                    }
                    else
                    {
                        Debug.LogWarningFormat("无效的 FuelTank 或 FuelType: {0}，FuelType={1}, CraftFuelSource={2}", 
                            modifierData.Name, fuelTank?.FuelType != null, fuelTank?.CraftFuelSource != null);
                    }
                }
                
            }
        
            if (this._oxygenSource == null)
            {
                Debug.LogWarning("未找到 Oxygen 类型的 FuelSource，可能影响 DamageDrood 逻辑");
            }
            if (this._foodSource == null)
            {
                Debug.LogWarning("未找到 Food 类型的 FuelSource，可能影响 DamageDrood 逻辑");
            }
        }

        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            RefreshFuelSource();
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e)
        {
            this.RefreshFuelSource();
        }
        
        private bool UsingInternalOxygen()
        {
            float airDensity = PartScript.CraftScript.AtmosphereSample.AirDensity;
            if (airDensity != 0)
            {
                if (currentPlanetName.Contains("Droo") || currentPlanetName.Contains("Kerbin") || 
                    currentPlanetName.Contains("Earth") || currentPlanetName.Contains("Nebra") || 
                    currentPlanetName.Contains("Laythe") || currentPlanetName.Contains("Oord"))
                {
                    return false;
                }
                return true;
            }
            return true;
        }

        private void UpdateCurrentPlanet()
        {
            bool inFlightScene = Game.InFlightScene;
            IPlanetData planetData = inFlightScene ? FlightSceneScript.Instance?.CraftNode?.Parent.PlanetData : null;
            currentPlanetName = planetData?.Name;
        }

        private void OnSoiChanged(IOrbitNode source)
        {
            UpdateCurrentPlanet();
        }

        private void DamageDrood(IFuelSource _fuelSource, FlightFrameData frame, float DamageScale)
        {
            if (_fuelSource == null || _evaScript == null || PartScript == null || 
                Game.Instance == null || Game.Instance.Settings?.Game?.Flight == null)
            {
                Debug.LogError("DamageDrood: 检测到 null 对象 - " +
                               $"_fuelSource={_fuelSource != null}, _evaScript={_evaScript != null}, " +
                               $"PartScript={PartScript != null}, Game.Instance={Game.Instance != null}, Settings={Game.Instance?.Settings != null}");
                return;
            }

            if (frame.DeltaTimeWorld <= 0)
            {
                return;
            }

            float num1 = 1 / (float)frame.DeltaTimeWorld;
            float num2 = (_evaScript.IsWalking ? 1f : 1.8f) * DamageScale * (float)frame.DeltaTimeWorld;

            if (_fuelSource.IsEmpty && _fuelSource.FuelType != null && 
                (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                this.PartScript.TakeDamage(num2 * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>Crew Member {_evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because running out of {_fuelSource.FuelType.Name}, " +
                    $"he/she has {Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) / DamageScale)} seconds left",
                    false, 2f);
            }
            else
            {
                if (_fuelSource.FuelType == null)
                    Debug.LogWarning("DamageDrood: _fuelSource.FuelType 为 null");
            }
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model) 
        {
        
            base.OnGenerateInspectorModel(model);
        
            
            GroupModel groupModel = new GroupModel("<color=green><size=115%>Life Support Info");
            model.AddGroup(groupModel);
            
            groupModel.Add<TextModel>(new TextModel("Remain Oxygen", (Func<string>) (() =>
            {
                if (UsingInternalOxygen() &&_oxygenSource != null && _oxygenSource.TotalCapacity > 0)
                {
                    float percentage = (float)(_oxygenSource.TotalFuel / _oxygenSource.TotalCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>{Units.GetPercentageString(percentage)}</color>";
                }
                else if(!UsingInternalOxygen())
                {
                    return "<color=green>Using External Oxygen</color>";
                }
                return "<color=purple>N/A</color>";
            })));
        
            groupModel.Add<TextModel>(new TextModel("Oxygen Supply Time", (Func<string>) (() =>
            {
                if (UsingInternalOxygen()&&_oxygenSource != null && _evaScript != null)
                {
                    float percentage = (float)(_oxygenSource.TotalFuel / _oxygenSource.TotalCapacity);
                    string oxygenTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={oxygenTextColor}>"+Units.GetStopwatchTimeString(_oxygenSource.TotalFuel / (Data.OxygenComsumeRate * (_evaScript.IsWalking ? 1 : 1.8)));
                }
                else if(!UsingInternalOxygen())
                {
                    return "<color=green>Infinity</color>";
                }
                return "N/A";
            })));
        
        
            groupModel.Add<TextModel>(new TextModel("Remain Food", (Func<string>) (() =>
            {
                if (_foodSource != null && _foodSource.TotalCapacity > 0)
                {
                    float percentage = (float)(_foodSource.TotalFuel / _foodSource.TotalCapacity);
                    string foodTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>{Units.GetPercentageString(percentage)}</color>";
                }
                return "<color=purple>N/A</color>";
            })));
        
            groupModel.Add<TextModel>(new TextModel("Food Supply Time", (Func<string>) (() =>
            {
                if (_foodSource != null && _evaScript != null)
                {
                    float percentage = (float)(_foodSource.TotalFuel / _foodSource.TotalCapacity);
                    string foodTextColor = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                    return $"<color={foodTextColor}>"+Units.GetStopwatchTimeString(_foodSource.TotalFuel / (Data.FoodComsumeRate * (_evaScript.IsWalking ? 1 : 1.8)));
                }
                return "N/A";
            })));
        }   
    }
}