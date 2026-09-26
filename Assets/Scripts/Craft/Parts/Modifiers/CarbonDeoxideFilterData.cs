

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("CarbonDioxideFilter")]
    [PartModifierTypeId("CarbonDioxideFilter")]
    public class CarbonDeoxideFilterData : PartModifierData<CarbonDeoxideFilterScript>
    {
        private Vector3 _positionOffset = Vector3.zero;
        public Vector3 PositionOffset
        {
            get => this._positionOffset;
            set => this._positionOffset = value;
        }
        [SerializeField][PartModifierProperty]
        private float co2ConsumptionRate = 1.0f;
        [SerializeField][PartModifierProperty]
        private float eletricityPowerConsumptionRatePerCo2 = 1.0f;
        [SerializeField][PartModifierProperty]
        private float _fanSpeed = 1.0f;
        public float FanSpeed
        {
            get => this._fanSpeed;
            set => this._fanSpeed = value;
        }

        public float Co2ConsumptionRate
        {
            get => this.co2ConsumptionRate*0.02f;
        }

        public float ElectricityPowerConsumptionRatePerCo2
        {
            get => this.eletricityPowerConsumptionRatePerCo2*400;
        }

        
    }
}