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
    [DesignerPartModifier("FirstAidKit")]
    [PartModifierTypeId("FirstAidKit")]
    public class FirstAidKitData : PartModifierData<FirstAddKitScript>
    {
        [SerializeField] [PartModifierProperty]
        private float healHp=300f;
        
        [SerializeField] [PartModifierProperty]
        private float healRate=1f;
       
        public float HealHp
        {
            get => healHp;
            internal set => healHp = value;
        }

        public float HealRate
        {
            get => healRate;
        }
    }
}