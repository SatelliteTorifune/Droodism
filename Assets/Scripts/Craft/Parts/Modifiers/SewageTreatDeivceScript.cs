using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using Assets.Scripts.Droodism.BackGround;
    using Assets.Scripts.Droodism.BackGround.Interface;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;
    public class SewageTreatDeivceScript : PartModifierScript<SewageTreatDeivceData>, IBackGroudCalculate, IFlightStart, IDesignerStart,IFlightUpdate
    {
        private IFuelSource waterSource, wastedWaterSource, _battery;
        private void ReCheck()
        {
            _battery = PartScript.BatteryFuelSource;
            try
            {
                var patchScript = PartScriptUtilities.GetCommandPodPatch(PartScript);
                if (patchScript == null)
                {
                    waterSource = wastedWaterSource = null;
                }
                if (patchScript!= null)
                {
                    waterSource = patchScript.WaterFuelSource;
                    wastedWaterSource=patchScript.WastedWaterFuelSource;
                }
            }
            catch (Exception)
            {
                waterSource = wastedWaterSource = null;
            }
        }
        public void FlightStart(in FlightFrameData frame)
        {
            ReCheck();
            this.UpdateScale();
        }
        public void FlightUpdate(in FlightFrameData frame)
        {
            if (!PartScript.Data.Activated)
                return;
            BackgroundLogic(frame.DeltaTimeWorld);
        }

        #region 后台结算

        /// <summary>
        /// 该零件激活时参与后台补算。
        /// </summary>
        public bool IsBackgroundUpdateEnabled => PartScript.Data.Activated;

        /// <summary>
        /// 后台补算入口（由 BackGroundCalculator 在飞船重新进入游戏视图时调用）。
        /// </summary>
        void IBackGroudCalculate.BackgroundUpdate(double elapsedSeconds)
        {
            ReCheck();
            BackgroundLogic(elapsedSeconds);
        }

        /// <summary>
        /// 按秒结算：废水 + 电 → 水。先按废水/电量可用量与水箱剩余容量限制本步秒数，防止大时间步长下账目不平。
        /// </summary>
        private void BackgroundLogic(double elapsedSeconds)
        {
            if (_battery == null || waterSource == null || wastedWaterSource == null)
            {
                return;
            }

            double scale = Data.Scale;
            double wastedWaterPerSecond = Data.WastedWaterComsumeRate * scale;
            double waterPerSecond = Data.ConvertEffiency * 0.3f * Data.WastedWaterComsumeRate * scale;
            double batteryPerSecond = Data.BatteryComsumeRate * Data.WastedWaterComsumeRate * scale;

            double seconds = elapsedSeconds;
            seconds = BackgroundSettlement.LimitSeconds(seconds, wastedWaterSource.TotalFuel, wastedWaterPerSecond);
            seconds = BackgroundSettlement.LimitSeconds(seconds, _battery.TotalFuel, batteryPerSecond);
            seconds = BackgroundSettlement.LimitSeconds(seconds, waterSource.TotalCapacity - waterSource.TotalFuel, waterPerSecond);

            if (seconds <= 0.0)
            {
                return;
            }

            wastedWaterSource.RemoveFuel(wastedWaterPerSecond * seconds);
            waterSource.AddFuel(waterPerSecond * seconds);
            _battery.RemoveFuel(batteryPerSecond * seconds);
        }

        #endregion
        #region 生命周期
        public void DesignerStart(in DesignerFrameData frame)
        {
            this.UpdateScale();
        }
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            ReCheck();
        }
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            
            this.UpdateScale();
           
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            this.UpdateScale();
            
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.ReCheck();
        
        #endregion

        /// <summary>Updates the scale of the generator.</summary>
        public void UpdateScale()
        {
            Transform transform = ((Component) this).transform.Find("Scalar");
            if (transform == null)
            {
                return;
            }

            foreach (AttachPointScript attachPointScript in this.PartScript.AttachPointScripts)
            {
                attachPointScript.AttachPoint.Scale = 0.8f * this.Data.Scale;
            }
            transform.localScale= Vector3.one*this.Data.Scale;
        }
    }
}
