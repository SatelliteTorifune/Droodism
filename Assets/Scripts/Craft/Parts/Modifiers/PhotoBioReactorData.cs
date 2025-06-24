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

        public float FoodGeneratedScale
        {
            get=>this._foodGeneratedScale*1f;
        }
    }
}