using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Flight;
using UnityEngine;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.Settings.Core;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class SupportLifeScript : PartModifierScript<SupportLifeData>, IDesignerStart, IFlightStart, IFlightUpdate
    {
        private static class FuelTypes
        {
            public const string Oxygen = "Oxygen";
            public const string Food = "Food";
            public const string Water = "Drinking Water";
        }

        private static readonly HashSet<string> BreathablePlanets = new() { "Droo", "Kerbin", "Earth", "Nebra", "Laythe", "Oord" };

        private readonly Dictionary<string, (Action<FuelTankScript> setTank, Action<IFuelSource> setSource)> _fuelTypeMap;

        private EvaScript _evaScript;
        private FuelTankScript _oxygenFuelTank;
        private IFuelSource _oxygenSource;
        private FuelTankScript _foodFuelTank;
        private IFuelSource _foodSource;
        private FuelTankScript _waterFuelTank;
        private IFuelSource _waterSource;
        private string _currentPlanetName;
        
        private IFlightScene FlightScene => Game.Instance.FlightScene;

        public SupportLifeScript()
        {
            _fuelTypeMap = new Dictionary<string, (Action<FuelTankScript>, Action<IFuelSource>)>
            {
                { FuelTypes.Oxygen, (tank => _oxygenFuelTank = tank, source => _oxygenSource = source) },
                { FuelTypes.Food, (tank => _foodFuelTank = tank, source => _foodSource = source) },
                { FuelTypes.Water, (tank => _waterFuelTank = tank, source => _waterSource = source) }
            };
        }

        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            if (Game.InFlightScene)
            {
                RefreshFuelSource();
            }
        }

        private void SetFuelTank(ref FuelTankScript field, FuelTankScript value)
        {
            if (field == value) return;
            if (Game.InFlightScene && field != null)
                field.CraftFuelSourceChanged -= OnCraftFuelSourceChanged;
            field = value;
            if (Game.InFlightScene && field != null)
                field.CraftFuelSourceChanged += OnCraftFuelSourceChanged;
        }

        private FuelTankScript OxygenFuelTank
        {
            get => _oxygenFuelTank;
            set => SetFuelTank(ref _oxygenFuelTank, value);
        }

        private FuelTankScript FuelTankFood
        {
            get => _foodFuelTank;
            set => SetFuelTank(ref _foodFuelTank, value);
        }

        private FuelTankScript FuelTankWater
        {
            get => _waterFuelTank;
            set => SetFuelTank(ref _waterFuelTank, value);
        }

        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
        }

        private XElement CreateFuelTankElement(string fuelType, float capacity)
        {
            return new XElement("FuelTank",
                new XAttribute("capacity", capacity),
                new XAttribute("fuel", capacity),
                new XAttribute("fuelType", fuelType),
                new XAttribute("utilization", -1),
                new XAttribute("autoFuelType", false),
                new XAttribute("subPriority", -1),
                new XAttribute("inspectorEnabled", false),
                new XAttribute("partPropertiesEnabled", false),
                new XAttribute("staticPriceAndMass", false));
        }

        private void AddTank(string fuelType, float fuelCapacity)
        {
            try
            {
                var element = CreateFuelTankElement(fuelType, fuelCapacity);
                var tankData = PartModifierData.CreateFromStateXml(element, Data.Part, 15) as FuelTankData;
                tankData.InspectorEnabled = false;
                tankData.SubPriority = -1;

                var fuelTankScript = tankData.CreateScript() as FuelTankScript;
                if (fuelTankScript == null || fuelTankScript.FuelType?.Name != fuelType)
                {
                    LogError($"Failed to create FuelTankScript for {fuelType}");
                    return;
                }

                PartScript.Modifiers.Add(fuelTankScript);
                Log($"Successfully added {fuelType} FuelTank to PartScript.Modifiers");

                if (!fuelTankScript.SupportsFuelTransfer)
                {
                    LogError($"{fuelType} FuelTank does not support fuel transfer (Disconnected: {fuelTankScript.PartScript.Disconnected})");
                    return;
                }

                fuelTankScript.FuelTransferMode = FuelTransferMode.Fill;
                Log($"Set {fuelType} FuelTank FuelTransferMode to Fill");

                if (fuelTankScript.CraftFuelSource == null)
                {
                    LogWarning($"{fuelType} FuelTank CraftFuelSource is null, using FuelTankScript as FuelSource");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to add {fuelType} FuelTank: {e}");
            }
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            Data.InspectorEnabled = true;

            try
            {
                _evaScript = GetComponent<EvaScript>();
            }
            catch (Exception e)
            {
                LogError($"Eva initialization failed: {e}");
            }

            UpdateCurrentPlanet();
            FlightScene.CraftNode.ChangedSoI += OnSoiChanged;

            if (Data.Part.Modifiers.Count <= 6)
            {
                AddTank(FuelTypes.Oxygen, Data.DesireOxygenCapacity);
                AddTank(FuelTypes.Food, Data.DesireFoodCapacity);
                AddTank(FuelTypes.Water, Data.DesireWaterCapacity);
            }

            OnCraftStructureChanged(PartScript.CraftScript);
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0.0 || !_evaScript.EvaActive)
                return;

            ConsumeResource(frame, _oxygenSource, Data.OxygenComsumeRate, Data.OxygenDamageScale, FuelTypes.Oxygen, () => UsingInternalOxygen());
            ConsumeResource(frame, _foodSource, Data.FoodComsumeRate, Data.FoodDamageScale, FuelTypes.Food, () => true);
            ConsumeResource(frame, _waterSource, Data.WaterComsumeRate, Data.WaterDamageScale, FuelTypes.Water, () => true);
        }

        private void ConsumeResource(in FlightFrameData frame, IFuelSource source, float consumeRate, float damageScale, string resourceName, Func<bool> isActive)
        {
            if (!isActive() || source == null)
            {
                if (source == null) LogWarning($"{resourceName} source is null");
                return;
            }

            if (source.IsEmpty)
            {
                DamageDrood(source, frame, damageScale);
            }
            else
            {
                double consumption = consumeRate * frame.DeltaTimeWorld * (_evaScript.IsWalking ? 1 : 1.8);
                source.RemoveFuel(consumption);
            }
        }

        private void RefreshFuelSource()
        {
            if (!Game.InFlightScene || PartScript?.Modifiers == null)
            {
                LogError("PartScript or PartScript.Modifiers is null, unable to get FuelSource");
                return;
            }

            foreach (var modifier in PartScript.Modifiers)
            {
                if (modifier.GetData().Name.Contains("FuelTank"))
                {
                    var fuelTank = modifier as FuelTankScript;
                    if (fuelTank?.FuelType == null)
                    {
                        LogWarning($"Invalid FuelTank or FuelType: {modifier.GetData().Name}");
                        continue;
                    }

                    if (_fuelTypeMap.TryGetValue(fuelTank.FuelType.Name, out var actions))
                    {
                        actions.setTank(fuelTank);
                        actions.setSource(fuelTank.CraftFuelSource != null ? fuelTank.CraftFuelSource : fuelTank as IFuelSource);
                        fuelTank.Data.InspectorEnabled = false;
                        Log($"Updated {fuelTank.FuelType.Name} FuelSource: {(actions.setSource != null ? "Success" : "Failed")}");
                    }
                    else if (fuelTank.FuelType.Name != "Jetpack")
                    {
                        LogWarning($"Unknown FuelType: {fuelTank.FuelType.Name}");
                    }
                }
            }

            foreach (var fuelType in _fuelTypeMap.Keys)
            {
                if (_fuelTypeMap[fuelType].setSource.Target == null)
                    LogWarning($"No {fuelType} FuelSource found, may affect DamageDrood logic");
            }
        }

        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            OnCraftStructureChanged(craftScript);
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            RefreshFuelSource();
        }

        private void OnCraftFuelSourceChanged(object sender, EventArgs e)
        {
            RefreshFuelSource();
        }

        private bool UsingInternalOxygen()
        {
            return PartScript.CraftScript.AtmosphereSample.AirDensity == 0 || !BreathablePlanets.Contains(_currentPlanetName);
        }

        private void UpdateCurrentPlanet()
        {
            _currentPlanetName = Game.InFlightScene ? FlightScene?.CraftNode?.Parent.PlanetData?.Name : null;
        }

        private void OnSoiChanged(IOrbitNode source)
        {
            UpdateCurrentPlanet();
        }

        private void DamageDrood(IFuelSource source, in FlightFrameData frame, float damageScale)
        {
            if (source == null || _evaScript == null || PartScript == null || Game.Instance?.Settings?.Game?.Flight == null)
            {
                LogError($"DamageDrood: Detected null - source={source != null}, _evaScript={_evaScript != null}, " +
                         $"PartScript={PartScript != null}, Game.Instance={Game.Instance != null}, Settings={Game.Instance?.Settings != null}");
                return;
            }

            if (frame.DeltaTimeWorld <= 0)
                return;

            float damage = (_evaScript.IsWalking ? 1f : 1.8f) * damageScale * (float)frame.DeltaTimeWorld;
            if (source.IsEmpty && source.FuelType != null && (float)(Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
            {
                PartScript.TakeDamage(damage * Game.Instance.Settings.Game.Flight.ImpactDamageScale, PartDamageType.Basic);
                FlightScene.FlightSceneUI.ShowMessage(
                    $"<color=red>Crew Member {_evaScript.Data.CrewName}(id:{PartScript.Data.Id}) is taking damage because running out of {source.FuelType.Name}, " +
                    $"he/she has {Units.GetStopwatchTimeString((100 - PartScript.Data.Damage) / damage)} seconds left",
                    false, 2f);
            }
            else if (source.FuelType == null)
            {
                LogWarning("DamageDrood: source.FuelType is null");
            }
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            var groupModel = new GroupModel("<color=green><size=115%>Life Support Info");
            model.AddGroup(groupModel);

            var resources = new[]
            {
                
                (FuelTypes.Oxygen, _oxygenSource, Data.OxygenComsumeRate, new Func<bool>(() => UsingInternalOxygen())),
                (FuelTypes.Food, _foodSource, Data.FoodComsumeRate, () => true),
                (FuelTypes.Water, _waterSource, Data.WaterComsumeRate, () => true)
            };

            foreach (var (name, source, consumeRate, isActive) in resources)
            {
                groupModel.Add<TextModel>(new TextModel($"Remain {name}", () =>
                {
                    if (!isActive())
                        return name == FuelTypes.Oxygen ? "<color=green>Using External Oxygen</color>" : "<color=purple>N/A</color>";
                    if (source?.TotalCapacity > 0)
                    {
                        float percentage = (float)(source.TotalFuel / source.TotalCapacity);
                        string color = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                        return $"<color={color}>{Units.GetPercentageString(percentage)}</color>";
                    }
                    return "<color=purple>N/A</color>";
                }));

                groupModel.Add<TextModel>(new TextModel($"{name} Supply Time", () =>
                {
                    if (!isActive())
                        return name == FuelTypes.Oxygen ? "<color=green>Infinity</color>" : "N/A";
                    if (source != null && _evaScript != null)
                    {
                        float percentage = (float)(source.TotalFuel / source.TotalCapacity);
                        string color = percentage > 0.5 ? "green" : percentage >= 0.25 ? "yellow" : "red";
                        return $"<color={color}>{Units.GetStopwatchTimeString(source.TotalFuel / (consumeRate * (_evaScript.IsWalking ? 1 : 1.8)))}</color>";
                    }
                    return "N/A";
                }));
            }
        }

        private void OnDestroy()
        {
            if (Game.InFlightScene)
            {
                if (_oxygenFuelTank != null) _oxygenFuelTank.CraftFuelSourceChanged -= OnCraftFuelSourceChanged;
                if (_foodFuelTank != null) _foodFuelTank.CraftFuelSourceChanged -= OnCraftFuelSourceChanged;
                if (_waterFuelTank != null) _waterFuelTank.CraftFuelSourceChanged -= OnCraftFuelSourceChanged;
                FlightScene.CraftNode.ChangedSoI -= OnSoiChanged;
            }
        }

        private void Log(string message) => Debug.Log($"[SupportLifeScript] {message}");
        private void LogWarning(string message) => Debug.LogWarning($"[SupportLifeScript] {message}");
        private void LogError(string message) => Debug.LogError($"[SupportLifeScript] {message}");
    }
}