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
    [DesignerPartModifier("Sodium Peroxide")]
    [PartModifierTypeId("SodiumPeroxide")]
    public class Sodium_PeroxideData : PartModifierData<Sodium_PeroxideScript>
    {
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float maxOxygenGenerateAmount = 1;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float co2ComsumeRate = 1;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float wastedWaterComsumeRate = 1;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float waterComsumeRate = 1;
        [SerializeField]
        [PartModifierProperty(true, false)] 
        public float co2LevelInfluence = 5;
        [SerializeField]
        [PartModifierProperty(true, false)] 
        public float oxygenGeneratorScale = 0.6f;
        
        public float MaxOxygenGenerateAmount
        {
            get => this.maxOxygenGenerateAmount*3e3f;
        }
        
        public float Co2ComsumeRate
        {
            get => this.co2ComsumeRate*0.04f;
        }
        
        public float WastedWaterComsumeRate
        {
            get => this.wastedWaterComsumeRate*1;
        }
        
        public float WaterComsumeRate
        {
            get => this.waterComsumeRate*1;
        }   
    }
}