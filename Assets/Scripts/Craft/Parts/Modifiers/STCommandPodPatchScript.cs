using System.Windows.Forms;
using ModApi.GameLoop;
using Droodism.RadiationBelt;
using ModApi.Flight.Sim;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class STCommandPodPatchScript : PartModifierScript<STCommandPodPatchData>, IFlightUpdate, IFlightStart
    {
        
        public IFuelSource OxygenFuelSource { get; set; }
        public IFuelSource CO2FuelSource { get; set; }
        public IFuelSource FoodFuelSource { get; set; }
        public IFuelSource SolidWasteFuelSource { get; set; }
        public IFuelSource WaterFuelSource { get; set; }
        public IFuelSource WastedWaterFuelSource { get; set; }
        
        private RadiationBeltConfig Config { get; set; }

        public void FlightStart(in FlightFrameData frame)
        {
            this.PartScript.CraftScript.CraftNode.ChangedSoI += OnChangedSOI;
            this.Config = RadiationBeltConfig.LoadFromFile(this.PartScript.CraftScript.CraftNode.Parent.Name);
        }

        private void OnChangedSOI(IOrbitNode node)
        {
            this.Config = RadiationBeltConfig.LoadFromFile(this.PartScript.CraftScript.CraftNode.Parent.Name);
        }
        

        public void FlightUpdate(in FlightFrameData flightFrameData)
        {
            string planetName = this.PartScript.CraftScript.CraftNode.Parent.Name;
            
            //我不知道你是啥想法,总之这个东西开启了会每帧都调用从文件load对应的config,损失性能,但是对于debug来说很有用
            if (ModSettings.Instance.ActiveUpdateRadiationBeltConfig)
            {
                this.Config = GetRuntimeConfigForPlanet(planetName);
            }
            

            Vector3 vesselPci = this.PartScript.CraftScript.FlightData.Position.ToVector3();

            bool inner = RadiationBeltManager.Instance.TryGetBeltSignedDistancePciMeters(this.Config,
                planetName, vesselPci, true, out var dIn) && dIn < 0f;
            bool outer = RadiationBeltManager.Instance.TryGetBeltSignedDistancePciMeters(this.Config,
                planetName, vesselPci, false, out var dOut) && dOut < 0f;

            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($" inner {inner},outer {outer}");
            
        }
        /// <summary>
        /// 考虑到这么写每帧都在load会造成不可忽视的性能损失,config的更新现在交给了调用的modifier在Start和SOI变化时调用
        /// 缺点只有无法实施调用了
        /// </summary>
        /// <param name="planetName"></param>
        /// <returns></returns>
        /// RadiationBeltConfig GetRuntimeConfigForPlanet(string planetName)
        private RadiationBeltConfig GetRuntimeConfigForPlanet(string planetName)
        {
            //这个b玩意也蠢,要是你不在当前星球每帧都给你load
            if (RadiationBeltManager.Instance.CurrentConfig != null &&RadiationBeltManager.Instance.CurrentFocusPlanet== planetName)
            {
                return RadiationBeltManager.Instance.CurrentConfig;
            }
            //要是没有那就手动load一下
            return RadiationBeltConfig.LoadFromFile(planetName);
        }
    }

}
