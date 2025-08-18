namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("PhotoBioReactor")]
    [PartModifierTypeId("PhotoBioReactor")]
    public class PhotoBioReactorData : PartModifierData<PhotoBioReactorScript>
    {
        [SerializeField] 
        [PartModifierProperty(true, false)]
        private float _foodGeneratedScale =1f;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _efficiency = 0.46f;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _growProgressTotal = 100f;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _growSpeed = 1f;

        [SerializeField] [PartModifierProperty(true, false)]
        private float _decaySpeed = 1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _co2ConsumptionRate = 1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _oxygenGenerationRate = 1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _waterConsumptionRate = 1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _powerConsumptionRate = 1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _solidWasteConsumptionRate = 1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _wastedWaterGenerationRate = 1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _boosteScale = 1.5f;
        
        private Vector3 _positionOffset1 = new Vector3(0f, 0f, 0.65f);
        public SubPartRotatorData.AngleLerpType AngleLerp => SubPartRotatorData.AngleLerpType.Euler;
        public SubPartRotatorData.AngleLerpType AngleLerp2 => SubPartRotatorData.AngleLerpType.Quaternion;
        public Vector3 PositionOffset1
        {
            get => this._positionOffset1;
            set => this._positionOffset1 = value;
        }
        private Vector3 _disabledRotation = Vector3.zero;
        public Vector3 DisabledRotation
        {
            get => this._disabledRotation;
            set => this._disabledRotation = value;
        }
     
        private Vector3 _enabledRotation = Vector3.zero;
        public Vector3 EnabledRotation
        {
            get => this._enabledRotation;
            set => this._enabledRotation = value;
        }
        
        private float _currentEnabledPercent = 0.0f;

        public float CurrentEnabledPercent
        {
            get => this._currentEnabledPercent*1f;
            set => this._currentEnabledPercent = value;
        }
        
        private float _rotationSpeed = 1f;
       
        private float _rotationRate = 0.1f;
        public float RotationRate => this._rotationRate * this._rotationSpeed;
        public float FoodGeneratedScale
        {
            get=>this._foodGeneratedScale*10f;
        }

        public float Efficiency
        {
            get=>this._efficiency*1f;
        }

        public float GrowProgressTotal
        {
            get=>this._growProgressTotal*4000f;
        }

        public float GrowSpeed
        {
            get=>this._growSpeed*1f;
        }

        public float DecaySpeed
        {
            get=>this._decaySpeed*1f;
        }

        public float Co2ConsumptionRate
        {
            get=>this._co2ConsumptionRate*0.0048f;
        }

        public float OxygenGenerationRate
        {
            get=>this._oxygenGenerationRate*0.008f;
        }
        public float WaterConsumptionRate
        {
            get=>this._waterConsumptionRate*0.0011f;
        }

        public float PowerConsumptionRate
        {
            get=>this._powerConsumptionRate*0.5f;
        }

        public float WastedWaterGenerationRate
        {
            get=>this._wastedWaterGenerationRate*0.0002f;
        }
        public float SolidWasteConsumptionRate
        {
            get=>this._solidWasteConsumptionRate*2E-05f;
        }
        public float BoosteScale
        {
            get=>this._boosteScale*1f;
        }
        
        public string SubPartPath = "DeviceBase/MainPipe";
    }
}