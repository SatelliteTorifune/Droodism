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
    [DesignerPartModifier("GravityRing")]
    [PartModifierTypeId("GravityRing")]
    public class GravityRingData : PartModifierData<GravityRingScript>
    {
        [SerializeField] [DesignerPropertyToggleButton(Label = "Reverse the Rotation Direction")]
        private bool isReverse = true;

        public bool IsReverse
        {
            get => isReverse;
        }
    }
}