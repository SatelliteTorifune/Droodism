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
    [DesignerPartModifier("WaterDesalination")]
    [PartModifierTypeId("WaterDesalination")]
    public class Water_DesalinationData : PartModifierData<Water_DesalinationScript>
    {
        [SerializeField][PartModifierProperty]
        private float powerConsumption = 1f;
        [SerializeField][PartModifierProperty]
        private float waterGenerationScale = 1f;
        
        public float PowerConsumption => this.powerConsumption*231;
        public float WaterGenerationScale => this.waterGenerationScale*0.05f;
    }
}