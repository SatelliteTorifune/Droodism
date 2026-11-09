using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class MethaloxGeneratorScript : ResourceProcessorPartScript<MethaloxGeneratorData>, IPartSubPartSetUp
    {
        private IFuelSource HPco2Source,waterSource,methaneloxSource;
        private ParticleSystem _particleSystem;
        private Transform _particleSystemTransform;

        public Transform SubPart => _particleSystemTransform;
        
        

        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            HPco2Source = GetRegularCraftFuelSource("HPCO2");
            waterSource = GetCommandPodPatch()
                .WaterFuelSource;
            methaneloxSource = GetRegularCraftFuelSource("LOX/CH4");
        }

        public override void FlightUpdate(in FlightFrameData frame)
        {
            if (_particleSystem==null)
            {
                Mod.Log("return");
                return;
            }
            if (!PartScript.Data.Activated)
            {
                _particleSystem.Stop();
               return; 
            }
            WorkingLogic(frame);
        }
        protected override void WorkingLogic(in FlightFrameData frame)
        {
            if (BatterySource==null||HPco2Source==null||waterSource==null||methaneloxSource==null)
            {
                _particleSystem?.Stop();
                return;
            }

            if (BatterySource.IsEmpty||HPco2Source.IsEmpty||waterSource.IsEmpty||methaneloxSource.TotalCapacity-methaneloxSource.TotalFuel<0.00001f)
            {
                _particleSystem?.Stop();
                return;
            }

            BackgroundLogic(frame.DeltaTimeWorld);

            if (_particleSystem!=null)
            {
                PlayEffects();
            }
        }

        #region 后台结算

        /// <summary>
        /// 激活时参与后台补算（离视图期间继续把高压 CO2 + 水 + 电转化为甲烷/液氧）。
        /// </summary>
        public override bool IsBackgroundUpdateEnabled => PartScript.Data.Activated;

        /// <summary>
        /// 按秒结算：电池 / 高压 CO2 / 水 → 甲烷液氧。先按各输入可用量与输出剩余容量限制本步秒数。
        /// </summary>
        protected override void BackgroundLogic(double elapsedSeconds)
        {
            if (BatterySource == null || HPco2Source == null || waterSource == null || methaneloxSource == null)
            {
                return;
            }

            double batteryPerSecond = Data.BatteryConsumption;
            double co2PerSecond = Data.Hpco2Consumption;
            double waterPerSecond = Data.WaterConsumption;
            double methaloxPerSecond = Data.MethaneloxGeneration;

            double seconds = elapsedSeconds;
            seconds = LimitBackgroundSeconds(seconds, BatterySource.TotalFuel, batteryPerSecond);
            seconds = LimitBackgroundSeconds(seconds, HPco2Source.TotalFuel, co2PerSecond);
            seconds = LimitBackgroundSeconds(seconds, waterSource.TotalFuel, waterPerSecond);
            seconds = LimitBackgroundSeconds(seconds, methaneloxSource.TotalCapacity - methaneloxSource.TotalFuel, methaloxPerSecond);

            if (seconds <= 0.0)
            {
                return;
            }

            BatterySource.RemoveFuel(batteryPerSecond * seconds);
            HPco2Source.RemoveFuel(co2PerSecond * seconds);
            waterSource.RemoveFuel(waterPerSecond * seconds);
            methaneloxSource.AddFuel(methaloxPerSecond * seconds);
        }

        #endregion


        private void PlayEffects()
        {
            if (!_particleSystem.isPlaying)
            {
               _particleSystem.Play(); 
            }
        }
        protected override void UpdateComponents()
        {
            SetSubPart(IPartSubPartSetUp.FindSubPart(this, "Device/ParticleSystem"));
            _particleSystem = _particleSystemTransform.GetComponent<ParticleSystem>();
            
        }

        public void SetSubPart( Transform subPart )
        {
            this._particleSystemTransform = subPart;
        }
    }
}