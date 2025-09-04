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
    [DesignerPartModifier("Sacrifice")]
    [PartModifierTypeId("Sacrifice")]
    public class SacrificeData : PartModifierData<SacrificeScript>
    {
        [SerializeField][PartModifierProperty]
        private float _foodGenerationScale = 1.0f;

        public float FoodGenerationScale
        {
            get => this._foodGenerationScale;
        }
    }
}