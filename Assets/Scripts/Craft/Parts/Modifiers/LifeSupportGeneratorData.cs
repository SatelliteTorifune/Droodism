namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("LifeSupportGenerator")]
    [PartModifierTypeId("LifeSupportGenerator")]
    public class LifeSupportGeneratorData : PartModifierData<LifeSupportGeneratorScript>
    {
        [SerializeField] [PartModifierProperty(true, false)]
        private float _waterConvertEfficiency=1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _oxygenConvertEfficiency=1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _fossilFuelConvertEfficiency=1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private bool isPollution=false;

        public float WaterConvertEfficiency
        {
            get => this._waterConvertEfficiency*0.08f;
        }
        
        public float OxygenConvertEfficiency
        {
            get => this._oxygenConvertEfficiency*4;
        }

        public float FossilFuelConvertEfficiency
        {
            get => this._fossilFuelConvertEfficiency * 0.1f;
        }
        
        public bool IsPollution { get=> this.isPollution; set=>(this.isPollution)=value; }
    }
}