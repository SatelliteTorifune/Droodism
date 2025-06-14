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
    [DesignerPartModifier("LifeSupportGenerator")]
    [PartModifierTypeId("LifeSupportGenerator")]
    public class LifeSupportGeneratorData : PartModifierData<LifeSupportGeneratorScript>
    {
        [SerializeField] [PartModifierProperty(true, false)]
        private float _waterConvertEfficiency=1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _oxygenConvertEfficiency=1f;

        public float WaterConvertEfficiency
        {
            get => this._waterConvertEfficiency;
        }
        
        public float OxygenConvertEfficiency
        {
            get => this._oxygenConvertEfficiency;
        }
    }
}