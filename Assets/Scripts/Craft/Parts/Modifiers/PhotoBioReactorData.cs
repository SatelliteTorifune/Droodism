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
    [PartModifierTypeId("Droodism.PhotoBioReactor")]
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
    }
}