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
        private float _growProgressTotal = 1f;
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
        private float _boosteScale = 1.5f;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private string _subPartPath = string.Empty;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _positionOffset1 = Vector3.zero;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private SubPartRotatorData.AngleLerpType _angleLerp = SubPartRotatorData.AngleLerpType.Euler;
        public SubPartRotatorData.AngleLerpType AngleLerp => this._angleLerp;
        public SubPartRotatorData.AngleLerpType AngleLerp2 => SubPartRotatorData.AngleLerpType.Quaternion;
        public Vector3 PositionOffset1
        {
            get => this._positionOffset1;
            set => this._positionOffset1 = value;
        }
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _disabledRotation = Vector3.zero;
        public Vector3 DisabledRotation
        {
            get => this._disabledRotation;
            set => this._disabledRotation = value;
        }
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _enabledRotation = Vector3.zero;
        public Vector3 EnabledRotation
        {
            get => this._enabledRotation;
            set => this._enabledRotation = value;
        }
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _enabledRotation2 = Vector3.zero;
        public Vector3 EnabledRotation2
        {
            get => this._enabledRotation2;
            set => this._enabledRotation2 = value;
        }
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _enabledRotation3 = Vector3.zero;
        public Vector3 EnabledRotation3
        {
            get => this._enabledRotation3;
            set => this._enabledRotation3 = value;
        }
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _enabledRotation4 = Vector3.zero;
        public Vector3 EnabledRotation4
        {
            get => this._enabledRotation4;
            set => this._enabledRotation4= value;
        }
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _currentEnabledPercent = 0.0f;

        public float CurrentEnabledPercent
        {
            get => this._currentEnabledPercent*1f;
            set => this._currentEnabledPercent = value;
        }
        [SerializeField]
        [DesignerPropertySlider(0.5f, 2.5f, 41, Label = "Rotation Speed")]
        private float _rotationSpeed = 1f;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _rotationRate = 0.1f;
        public float RotationRate => this._rotationRate * this._rotationSpeed;
        public float FoodGeneratedScale
        {
            get=>this._foodGeneratedScale*1f;
        }

        public float Efficiency
        {
            get=>this._efficiency*1f;
        }

        public float GrowProgressTotal
        {
            get=>this._growProgressTotal*1f;
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
            get=>this._co2ConsumptionRate*1f;
        }

        public float OxygenGenerationRate
        {
            get=>this._oxygenGenerationRate*1f;
        }
        public float WaterConsumptionRate
        {
            get=>this._waterConsumptionRate*1f;
        }

        public float PowerConsumptionRate
        {
            get=>this._powerConsumptionRate*1f;
        }

        public float SolidWasteConsumptionRate
        {
            get=>this._solidWasteConsumptionRate*1f;
        }
        public float BoosteScale
        {
            get=>this._boosteScale*1f;
        }
        
        public string SubPartPath=>this._subPartPath;
    }
}