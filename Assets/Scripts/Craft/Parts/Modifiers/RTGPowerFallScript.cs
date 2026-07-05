using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi;
using ModApi.Craft;
using ModApi.GameLoop;
using ModApi.Math;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class RTGPowerFallScript : PartModifierScript<RTGPowerFallData>,IFlightUpdate,IFlightStart ,IAnalyzePerformance
    {
        private GeneratorScript generatorScript;
        private IFuelSource _battery;
        /// <summary>
        /// 满功率时每秒向电池添加的量（GeneratorScript 的 _battery.AddFuel(num3 * 0.001) 全油门值）
        /// </summary>
        private float _fullBatteryRate;
        /// <summary>
        /// Designer 性能分析面板中用于预览衰减的模拟年数。
        /// </summary>
        private float _designerPreviewYears;
        /// <summary>
        /// 每年功率衰减比例。
        /// 真实 RTG（Pu-238，半衰期 87.7年）放射性衰变约 0.79%/年，
        /// 加上热电偶退化，总计约 4%/年，故取 0.04。
        /// 10年后 ~66% 功率，20年后 ~44%，87年后 ~2.7%（接近燃料耗尽）。
        /// </summary>
        private const float AnnualDecayFraction = 0.04f;
        /// <summary>
        /// 每年剩余功率比例 = 1 - AnnualDecayFraction
        /// </summary>
        private const float AnnualRemainFraction = 1f - AnnualDecayFraction; // 0.96
        public long MissionDurationTime{get;private set;}

        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            generatorScript=this.PartScript.GetModifier<GeneratorScript>();
        }
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            this._battery = this.PartScript.BatteryFuelSource;
        }

        public override void OnInitialLaunch()
        { 
            Data.MissionStartTime = (long)Game.Instance.FlightScene.FlightState.Time;
        }

        #region 周期

        public void FlightStart(in FlightFrameData frame)
        {
            // GeneratorScript.FlightUpdate 中的逻辑：
            //   num2 = _powerScale * FuelFlow * DeltaTimeWorld   (消耗的燃料量，升)
            //   num3 = MaxPowerGenerated(num2)                   (产生的能量，焦耳)
            //   _battery.AddFuel(num3 * 0.001)                   (转成电池内部单位)
            //
            // 满功率(_powerScale=1)时，每秒产生的能量 = MaxPowerGenerated(FuelFlow * 1)
            // 由于 MaxPowerGenerated 是线性的：MaxPowerGenerated(k * x) = k * MaxPowerGenerated(x)
            // 所以每秒能量 = MaxPowerGenerated(FuelFlow)
            // 每秒电池增量 = MaxPowerGenerated(FuelFlow) * 0.001
            float fuelFlow = generatorScript.Data.FuelFlow;
            float energyPerSecond = generatorScript.Data.MaxPowerGenerated(fuelFlow);
            _fullBatteryRate = energyPerSecond * 0.001f;
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            MissionDurationTime = (long)Game.Instance.FlightScene.FlightState.Time - Data.MissionStartTime;
            WorkingLogic(frame);
        }
        
        #endregion

        private void WorkingLogic(in FlightFrameData frame)
        {
            if (_fullBatteryRate <= 0f || _battery == null)
                return;

            float dtWorld = (float)frame.DeltaTimeWorld;
            if (dtWorld <= 0f)
                return;

            // 指数衰减：P(t) = P0 * (annualRemainFraction)^(years)
            // annualRemainFraction = 1 - 0.04 = 0.96
            float years = MissionDurationTime / 31536000f;
            float decayFactor = Mathf.Pow(AnnualRemainFraction, years);

            // GeneratorScript 每帧向电池添加 _fullBatteryRate * dt
            // 我们移除衰减掉的部分 = (1 - decayFactor) * 满功率电池添加量 * dt
            float removalAmount = _fullBatteryRate * (1f - decayFactor) * dtWorld;

            if (removalAmount <= 0f)
                return;

            if (removalAmount > _battery.TotalFuel)
            {
                removalAmount = (float)_battery.TotalFuel;
            }

            _battery.RemoveFuel(removalAmount);
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            
            GroupModel decayGroup = new GroupModel(Locale.GetString("Droodism.RTGPowerFallScript.RTGGroupTitle"));
            
            decayGroup.Add(new TextModel(Locale.GetString("Droodism.RTGPowerFallScript.MissionTime"), () =>
            {
                float years = MissionDurationTime / 31536000f;
                return Units.GetStopwatchTimeString(MissionDurationTime);
            }));
            
            
            decayGroup.Add(new TextModel(Locale.GetString("Droodism.RTGPowerFallScript.CurrentPower"), () =>
            {
                float fuelFlow = generatorScript.Data.FuelFlow;
                float originalPower = generatorScript.Data.MaxPowerGenerated(fuelFlow);
                float years = MissionDurationTime / 31536000f;
                float decayFactor = Mathf.Pow(AnnualRemainFraction, years);
                return Units.GetPowerString(originalPower * decayFactor);
            }));
            
            decayGroup.Add(new TextModel(Locale.GetString("Droodism.RTGPowerFallScript.PowerRemaining"), () =>
            {
                float years = MissionDurationTime / 31536000f;
                float decayFactor = Mathf.Pow(AnnualRemainFraction, years);
                return $"{decayFactor * 100f:F1}%";
            }));
            
            model.AddGroup(decayGroup);
        }

        public void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            float fuelFlow = generatorScript.Data.FuelFlow;
            float originalPower = generatorScript.Data.MaxPowerGenerated(fuelFlow);
            
            
            groupModel.Add(new SliderModel(
                Locale.GetString("Droodism.RTGPowerFallScript.PreviewYears"),
                () => _designerPreviewYears,
                v => _designerPreviewYears = Mathf.Max(0f, v),
                0f, 100f, true, true
            )).ValueFormatter = (Func<float, string>)(x => $"{x:F1} Year");
            
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RTGPowerFallScript.CurrentPower"), () =>
            {
                float decayFactor = Mathf.Pow(AnnualRemainFraction, _designerPreviewYears);
                return Units.GetPowerString(originalPower * decayFactor);
            }));
            
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RTGPowerFallScript.PowerRemaining"), () =>
            {
                float decayFactor = Mathf.Pow(AnnualRemainFraction, _designerPreviewYears);
                return $"{decayFactor * 100f:F1}%";
            }));
        }

        public bool UsesMachNumber { get; }
    }
}
