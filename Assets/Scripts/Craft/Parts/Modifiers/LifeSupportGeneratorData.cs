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
        private float _hydroloxConvertEfficiency=1f;

        public float HydroloxConvertEfficiency
        {
            get => this._hydroloxConvertEfficiency;
        }
    }
}