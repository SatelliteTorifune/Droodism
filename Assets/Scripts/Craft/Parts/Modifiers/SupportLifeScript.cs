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
using Assets.Scripts.Flight;
using ModApi.Planet;
using UnityEngine;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.Settings.Core;
using ModApi.Ui.Inspector;
///孩子们我所做的就只是拉屎而已,然后往屎上叠他妈的屎山
//鸡巴的我自己都看不懂我写的是什么鸡巴玩意了你还指望我给你写注释吗?
//顺便一提如果真有除了我以外的人在github上或者逆向出来了看到了这行字,那么我只能说一句牛逼,你简直是找屎大王,能闻着味道找到我编程以来拉的最大的一坨
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
        
        private IFuelSource _waterSource;
        private FuelTankScript _waterFuelTank;
        
        
        private float _fuelRemoved;
        private string currentPlanetName;
        private IPlanetData planetData;
        private float _oxygenConsumeRate;
        
        private CraftFuelSource craftOxygenFuelSource;
        
        private GrapplingHookScript _grapplingHook;

        private bool isRunning;
        private bool isTourist;
        
        private IInputController _evaPitch;
        private IInputController _evaRoll;

        
        
        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            this.Data.PartPropertiesEnabled = true;
            
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
            
            if (this.PartScript.Data.PartType.Name == "Eva-Tourist")
            {
                isTourist = true;
            }
            
            try
            {
                _evaScript = GetComponent<EvaScript>();
            }
            catch(Exception e)
            {
                Debug.LogErrorFormat("Eva初始化失败!{0}", e);
            } 
            
            UpdateCurrentPlanet();
            Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
            PartData partData = this.Data.Part;
            if (partData.Modifiers.Count <= 6)
            {
                AddTank("Oxygen", this.Data.DesireOxygenCapacity*(isTourist?0.95f:1f));
                AddTank("Food", this.Data.DesireFoodCapacity*(isTourist?0.95f:1f));
                AddTank("Drinking Water", this.Data.DesireWaterCapacity*(isTourist?0.95f:1f));
            }
            RefreshFuelSource();
            
        }
        
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0) 
                return;
            if (_evaScript.EvaActive&&_evaScript.IsPlayerCraft&& !_evaScript.IsWalking&&_evaScript.IsGrounded&&(this.PartScript.CraftScript.SurfaceVelocity.magnitude>=0.8))//&&_evaRoll.Value<=0.1&&_evaPitch.Value<=0.1)
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

        private void ConsumptionLogic(in FlightFrameData frame)
        {
            if (_oxygenSource != null)
            {
                if (UsingInternalOxygen())
                {
                    if (_oxygenSource.IsEmpty)
                    {
                        DamageDrood(_oxygenSource, frame, Data.OxygenDamageScale);
                        
                    }
                    else
                    {
                        double num1 = (double)Data.OxygenComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 :1)*(isTourist?1.05:1);
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
                Debug.LogWarning("_oxygenSource is  null");
            }

            if (_foodSource != null)
            {
                if (_foodSource.IsEmpty)
                {
                    DamageDrood(_foodSource,frame,Data.FoodDamageScale);
                }
                else
                {
                    double num1 = (double)Data.FoodComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 :1)*(isTourist?1.05:1);
                    _foodSource.RemoveFuel(num1);
                }
                
            }
            else
            {
                Debug.LogWarning("_foodSource is Null");
            }
            if (_waterSource != null)
            {
                if (_waterSource.IsEmpty)
                {
                    DamageDrood(_waterSource,frame,Data.WaterDamageScale);
                    
                }
                else
                {
                    double num1 = (double)Data.WaterComsumeRate * frame.DeltaTimeWorld * (isRunning ? 1.75 :1)*(isTourist?1.05:1);
                    _waterSource.RemoveFuel(num1);
                }
                
            }
            else
            {
                Debug.LogWarning("_waterSource is Null");
            }
        }

        private void AutoRefillLogic(in FlightFrameData frame)
        {
            //Auto Fill Oxygen When inside Ato
            if (_oxygenSource!=null)
            {
                if (!UsingInternalOxygen())
                {
                    if (_oxygenSource.TotalCapacity-_oxygenSource.TotalFuel>=0.001)
                    {
                        _oxygenSource.AddFuel(_oxygenSource.TotalCapacity * frame.DeltaTimeWorld*0.10000000149011612);
                    }
                }
            }
        }
        public void RefreshFuelSource()
        {
            if (!Game.InFlightScene) 
                return;
        
            if (PartScript == null || PartScript.Modifiers == null)
            {
                Debug.LogError("PartScript or PartScript.Modifiers is null，Unable to get FuelSource");
                return;
            }
            
            if (Game.InFlightScene)
            {
                EvaRefreshFuelSource();
                
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
                    Debug.LogWarning("未找到 Water 类型的 FuelSource，可能影响 DamageDrood 逻辑");
                }
                
            }
            
        }
        private void EvaRefreshFuelSource()
        {
            Debug.LogFormat("调用EvaRefreshFuelSource");
            
            try
            {
                if (_evaScript.EvaActive)
                {
                    Debug.LogFormat("执行真的EvaRefreshFuelSource的逻辑");
                    //如果drood在Eva状态,那就只在本地搜索Modifier
                    foreach (var modifier in this.PartScript.Modifiers)
                    {
                        var modifierData = modifier.GetData();
                        if (modifierData.Name.Contains("FuelTank"))
                        {
                            var fuelTank = modifier as FuelTankScript;
                            if (fuelTank != null && fuelTank.FuelType != null)
                            {
                                switch (fuelTank.FuelType.Name)
                                {
                                    case "Oxygen":
                                        this._oxygenFuelTank = fuelTank;
                                        this._oxygenSource = fuelTank.CraftFuelSource != null ? (IFuelSource)fuelTank.CraftFuelSource : (IFuelSource)fuelTank;
                                        this._oxygenFuelTank.Data.InspectorEnabled = false;
                                        Debug.LogFormat("已更新 Oxygen 的 FuelSource: {0}", this._oxygenSource != null ? "成功" : "失败");
                                        break;
                                    case "Food":
                                        this._foodFuelTank = fuelTank;
                                        this._foodSource = fuelTank.CraftFuelSource != null ? (IFuelSource)fuelTank.CraftFuelSource : (IFuelSource)fuelTank;
                                        this._foodFuelTank.Data.InspectorEnabled = false;
                                        Debug.LogFormat("已更新 Food 的 FuelSource: {0}", this._foodSource != null ? "成功" : "失败");
                                        break;
                                    case "Drinking Water":
                                        this._waterFuelTank = fuelTank;
                                        this._waterSource = fuelTank.CraftFuelSource != null ? (IFuelSource)fuelTank.CraftFuelSource : (IFuelSource)fuelTank;
                                        this._waterFuelTank.Data.InspectorEnabled = false;
                                        Debug.LogFormat("已更新 Water 的 FuelSource: {0}", this._foodSource != null ? "成功" : "失败");
                                        break;
                                    case "Jetpack":
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
                }
                if (!_evaScript.EvaActive)
                {
                    CraftRefeshFuelSource();
                    Debug.LogWarningFormat("从EvaRefreshFuelSource调用CraftRefeshFuelSource");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("{0}",e);
                //LMAO this try-catch only prevent game kicks your ass back to Designer when trying to load into the game,so yes.this catch simply does NOTHING 
                //是的我知道这样写简直傻逼的不行,你鸡巴需要先这样然后才能进入到CraftRefeshFuelSource
                //但是I dont give a fuck
            }
        }

        private void CraftRefeshFuelSource()
        {
            //是的我知道这很傻逼,你为什么要他妈的遍历craft中每一个part里面的modifier?就因为你他妈的不会用IFuelSource吗?
            //你妈的你是人啊我操
            
            Debug.LogWarningFormat("CraftRefeshFuelSource调用");
            List<FuelTankScript> CraftOxygenFuelSourceList = new List<FuelTankScript>();
            List<FuelTankScript> CraftFoodFuelSourceList= new List<FuelTankScript>();
            List<FuelTankScript> CraftWaterFuelSourceList= new List<FuelTankScript>();
            Debug.LogFormat("List创建");
            try
            {
                foreach (PartData partData in this.PartScript.CraftScript.Data.Assembly.Parts)
                {
                    foreach (var modifierData in partData.Modifiers)
                    {
                        if (modifierData.Name.Contains("Tank"))
                        {
                            FuelTankScript _fuelTankScript = modifierData.GetScript() as FuelTankScript;
                            if (_fuelTankScript.FuelType.Name.Contains("Oxygen"))
                            {
                                CraftOxygenFuelSourceList.Add(_fuelTankScript);
                                Debug.LogFormat($"添加{_fuelTankScript.Position}到列表");
                            }

                            if (_fuelTankScript.FuelType.Name.Contains("Drinking"))
                            {
                                CraftWaterFuelSourceList.Add(_fuelTankScript);
                                Debug.LogFormat($"添加{_fuelTankScript.Position}到列表");
                            }

                            if (_fuelTankScript.FuelType.Name.Contains("Food"))
                            {
                                CraftFoodFuelSourceList.Add(_fuelTankScript);
                                Debug.LogFormat($"添加{_fuelTankScript.Position}到列表");
                            }

                        }
                    }
                }

            }
            catch (Exception e)
            {
                Debug.LogFormat("下面出问题了{0}", e);
            }
        }
        #region 无所弔谓
        
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            if (Game.InFlightScene)
            {
                RefreshFuelSource();
                Debug.LogErrorFormat("从OnCraftStructureChanged调用RefreshFuelSource();");
            }
            
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e)
        {
            RefreshFuelSource();
            Debug.LogWarningFormat("从OnCraftFuelSourceChanged调用RefreshFuelSource();");
            
        } 
        private bool UsingInternalOxygen()
        {
            // 如果大气密度为 0，则需要使用内部氧气
            float airDensity = PartScript.CraftScript.AtmosphereSample.AirDensity;
            if (airDensity == 0)
            {
                return true;
            }

            // 检查当前行星是否含氧
            if (currentPlanetName.Contains("Droo") || currentPlanetName.Contains("Kerbin") ||
                currentPlanetName.Contains("Earth") || currentPlanetName.Contains("Nebra") ||
                currentPlanetName.Contains("Laythe") || currentPlanetName.Contains("Oord"))
            {
                return false;
            }
            
            // 如果在水下且接近海平面，需要使用内部氧气
            bool isSubmerged = _evaScript.IsInWater && PartScript.CraftScript.FlightData.AltitudeAboveSeaLevel < 0.1;
            return isSubmerged;
        }

        private void UpdateCurrentPlanet()
        {
            IPlanetData planetData = Game.InFlightScene ? FlightSceneScript.Instance?.CraftNode?.Parent.PlanetData : null;
            currentPlanetName = planetData?.Name;
        }

        private void OnSoiChanged(IOrbitNode source)
        {
            UpdateCurrentPlanet();
        }

        #endregion
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
            
            //我他妈这么知道这个num1干啥用的
            //float num1 = 1 / (float)frame.DeltaTimeWorld;
            float num2 = (isRunning?1.75f:1f)*(isTourist?1.05f:1f) * DamageScale * (float)frame.DeltaTimeWorld;

            if (_fuelSource.IsEmpty && _fuelSource.FuelType != null && 
                (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                this.PartScript.TakeDamage(num2 * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>Crew Member {_evaScript.Data.CrewName}(id:{this.PartScript.Data.Id}) is taking damage because running out of {_fuelSource.FuelType.Name}, " +
                    $"he/she has {Units.GetStopwatchTimeString((100 - this.PartScript.Data.Damage) /((isRunning?1.75:1)*(isTourist?1.05:1)*DamageScale))} left",
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
                    return $"<color={oxygenTextColor}>"+Units.GetStopwatchTimeString(_oxygenSource.TotalFuel / (Data.OxygenComsumeRate * (isRunning ? 1.75 :1)*(isTourist?1.05:1)));
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
                    return $"<color={foodTextColor}>"+Units.GetStopwatchTimeString(_foodSource.TotalFuel / (Data.FoodComsumeRate * (isRunning ? 1.75 :1)*(isTourist?1.05:1)));
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
                    return $"<color={waterTextColor}>"+Units.GetStopwatchTimeString(_waterSource.TotalFuel / (Data.WaterComsumeRate * (isRunning ? 1.75 :1)*(isTourist?1.05:1)));
                }
                return "N/A";
            })));
        }   
    }
    
}

