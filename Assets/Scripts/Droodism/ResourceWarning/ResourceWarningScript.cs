using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Craft.Parts;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Math;
using ModApi.Scenes.Events;
using UnityEngine;

namespace Assets.Scripts.Droodism.ResourceWarning
{
    public class ResourceWarningScript : MonoBehaviourBase, IFlightUpdate, IFlightStart
    {
        public static ResourceWarningScript Instance { get; private set; }

        private Dictionary<string, DroodResourceStatus> _statusMap = new Dictionary<string, DroodResourceStatus>();
        private double _lastSlowdownTime = -999.0;
        private bool _hasTriggeredPauseThisSession = false;

        public enum WarningLevel { None, Warning, Critical }

        private class DroodResourceStatus
        {
            public string DroodName;
            public int DroodId;
            public WarningLevel Level;
            public double LastWarningGameTime;
            public bool WasEverCritical;
        }
        #region 循环注册

        private void OnEnable()
        {
            TryRegisterWithFlightGameLoop();
        }

        private void OnDisable()
        {
            TryUnregisterFromFlightGameLoop();
        }

        private void TryRegisterWithFlightGameLoop()
        {
            if (!Game.Instance.SceneManager.InFlightScene)
                return;
            if (Game.Instance.FlightScene == null)
                return;
            Game.Instance.FlightScene.GameLoop.Register(this);
        }

        private void TryUnregisterFromFlightGameLoop()
        {
            if (Game.Instance?.FlightScene == null)
                return;
            Game.Instance.FlightScene.GameLoop.Unregister(this);
        }

        private void Awake()
        {
            Instance = this;
            _statusMap.Clear();
            Game.Instance.SceneManager.SceneTransitionCompleted += OnSceneTransitionCompleted;
        }

        private void OnDestroy()
        {
            Game.Instance.SceneManager.SceneTransitionCompleted -= OnSceneTransitionCompleted;
            TryUnregisterFromFlightGameLoop();
            if (Game.Instance?.FlightScene != null)
                Game.Instance.FlightScene.FlightEnded -= OnFlightEnded;
        }
        #endregion

        #region 游戏内事件和函数
        
        private void OnSceneTransitionCompleted(object sender, SceneTransitionEventArgs e)
        {
            if (e.TransitionToScene == "Flight")
            {
                if (Game.Instance?.FlightScene != null)
                {
                    TryRegisterWithFlightGameLoop();
                    Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
                }
                _statusMap.Clear();
                ResetSessionFlags();
            }
            else
            {
                if (Game.Instance?.FlightScene != null)
                {
                    TryUnregisterFromFlightGameLoop();
                    Game.Instance.FlightScene.FlightEnded -= OnFlightEnded;
                }
            }
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
        }

        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            _statusMap.Clear();
            ResetSessionFlags();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!ModSettings.Instance.EnableResourceWarning)
                return;

            if (frame.DeltaTimeWorld <= 0.0)
                return;

            var tm = Game.Instance.FlightScene.TimeManager;
            if (tm.CurrentMode.TimeMultiplier <= 1.0f)
            {
                _lastSlowdownTime = -999.0;
                _hasTriggeredPauseThisSession = false;
                return;
            }

            CheckAllDroodResources(frame);
            HandleAutoSlowdown(frame);
        }
        #endregion
        #region 真干活的

        private void CheckAllDroodResources(in FlightFrameData frame)
        {
            if (Game.Instance.FlightScene.CraftNode?.CraftScript == null)
                return;

            var craft = Game.Instance.FlightScene.CraftNode.CraftScript;
            double gameTime = Game.Instance.FlightScene.FlightState.Time;

            foreach (var pd in craft.Data.Assembly.Parts)
            {
                var supportLifeData = pd.GetModifier<SupportLifeData>();
                if (supportLifeData == null)
                    continue;

                var supportLifeScript = supportLifeData.Script;
                string droodName = pd.GetModifier<EvaData>()?.CrewName ?? "Unknown";
                int droodId = pd.Id;
                bool usingInternalOxygen = supportLifeScript.UsingInternalOxygen();

                CheckResource(droodName, droodId, "Oxygen",
                    supportLifeData._oxygenAmountBuffer, supportLifeData.DesireOxygenCapacity, false,
                    usingInternalOxygen, gameTime);

                CheckResource(droodName, droodId, "H2O",
                    supportLifeData._waterAmountBuffer, supportLifeData.DesireWaterCapacity, false,
                    true, gameTime);

                CheckResource(droodName, droodId, "Food",
                    supportLifeData._foodAmountBuffer, supportLifeData.DesireFoodCapacity, false,
                    true, gameTime);

                CheckResource(droodName, droodId, "CO2",
                    supportLifeData._co2AmountBuffer, supportLifeData.DesireCO2Capacity, true,
                    usingInternalOxygen, gameTime);

                CheckResource(droodName, droodId, "Wasted Water",
                    supportLifeData._wastedWaterAmountBuffer, supportLifeData.DesireWastedWaterCapacity, true,
                    true, gameTime);

                CheckResource(droodName, droodId, "Solid Waste",
                    supportLifeData._solidWasteAmountBuffer, supportLifeData.DesireSolidWasteCapacity, true,
                    true, gameTime);
            }
        }

        private void CheckResource(
            string droodName, int droodId,
            string resourceId, double currentAmount, float capacity,
            bool isWaste,
            bool resourceRelevant,
            double gameTime)
        {
            string key = $"{droodId}_{resourceId}";
            if (!_statusMap.TryGetValue(key, out var status))
            {
                status = new DroodResourceStatus
                {
                    DroodName = droodName,
                    DroodId = droodId,
                    Level = WarningLevel.None,
                    LastWarningGameTime = -999.0,
                    WasEverCritical = false
                };
                _statusMap[key] = status;
            }

            if (!resourceRelevant)
            {
                status.Level = WarningLevel.None;
                status.WasEverCritical = false;
                return;
            }

            if (capacity <= 0.0)
            {
                status.Level = WarningLevel.None;
                status.WasEverCritical = false;
                return;
            }

            float percentage = (float)(currentAmount / capacity);

            float warningThreshold = ModSettings.Instance.ResourceWarningThreshold;
            float criticalThreshold = ModSettings.Instance.ResourceCriticalThreshold;

            WarningLevel newLevel;
            if (isWaste)
            {
                float fillPercent = percentage;
                newLevel = fillPercent >= (1.0f - criticalThreshold) ? WarningLevel.Critical
                    : fillPercent >= (1.0f - warningThreshold) ? WarningLevel.Warning
                    : WarningLevel.None;
            }
            else
            {
                newLevel = percentage <= criticalThreshold ? WarningLevel.Critical
                    : percentage <= warningThreshold ? WarningLevel.Warning
                    : WarningLevel.None;
            }

            if (newLevel == WarningLevel.Critical)
                status.WasEverCritical = true;

            if (newLevel != status.Level)
            {
                status.Level = newLevel;
                OnLevelChanged(status, resourceId, percentage, isWaste, gameTime);
            }
        }

        private void OnLevelChanged(DroodResourceStatus status, string resourceId, float percentage, bool isWaste, double gameTime)
        {
            if (status.Level == WarningLevel.None)
                return;

            double cooldown = 30.0;
            if (gameTime - status.LastWarningGameTime < cooldown)
                return;

            status.LastWarningGameTime = gameTime;

            var ui = Game.Instance.FlightScene.FlightSceneUI;
            string color = status.Level == WarningLevel.Critical ? "red" : "yellow";
            string levelStr = status.Level == WarningLevel.Critical ? "CRITICAL" : "WARNING";

            if (isWaste)
            {
                float fillPercent = 1.0f - percentage;
                ui.ShowMessage(
                    $"<size=150%><color={color}>[{levelStr}] {status.DroodName}: {resourceId} at {Units.GetPercentageString(fillPercent)} capacity!",
                    true, 5f);
            }
            else
            {
                ui.ShowMessage(
                    $"<size=150%><color={color}>[{levelStr}] {status.DroodName}: {resourceId} at {Units.GetPercentageString(percentage)} remaining!",
                    true, 5f);
            }

            if (status.Level == WarningLevel.Critical)
                _hasTriggeredPauseThisSession = true;
        }

        private void HandleAutoSlowdown(in FlightFrameData frame)
        {
            var tm = Game.Instance.FlightScene.TimeManager;
            double gameTime = Game.Instance.FlightScene.FlightState.Time;
            double slowdownInterval = 10.0;

            bool hasCritical = _statusMap.Values.Any(s => s.Level == WarningLevel.Critical);

            if (!hasCritical)
            {
                _lastSlowdownTime = -999.0;
                _hasTriggeredPauseThisSession = false;
                return;
            }

            if (gameTime - _lastSlowdownTime < slowdownInterval)
                return;

            _lastSlowdownTime = gameTime;

            float currentMultiplier = (float)tm.CurrentMode.TimeMultiplier;
            float minMultiplier = (float)tm.Modes.First().TimeMultiplier;

            
            if (currentMultiplier > minMultiplier)
            {
                tm.DecreaseTimeMultiplier();
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                    "<size=150%><color=orange>Resource critical! Warp speed reduced.</color>", true, 3f);

                if (tm.CurrentMode.TimeMultiplier <= minMultiplier * 1.5f)
                {
                    tm.RequestPauseChange(true, false);
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                        "<size=150%><color=red>Resource critical! Time paused.</color>", true, 5f);
                }
            }
        }

        public WarningLevel GetOverallWarningLevel()
        {
            WarningLevel worst = WarningLevel.None;
            foreach (var kvp in _statusMap)
            {
                if (kvp.Value.Level > worst)
                    worst = kvp.Value.Level;
            }
            return worst;
        }

        public int GetCriticalCount() => _statusMap.Values.Count(s => s.Level == WarningLevel.Critical);
        public int GetWarningCount() => _statusMap.Values.Count(s => s.Level == WarningLevel.Warning);

        public void ResetSessionFlags()
        {
            _lastSlowdownTime = -999.0;
            _hasTriggeredPauseThisSession = false;
        }
        #endregion
    }
}
