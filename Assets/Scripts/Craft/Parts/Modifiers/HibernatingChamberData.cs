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
    [DesignerPartModifier("HibernatingChamber")]
    [PartModifierTypeId("HibernatingChamber")]
    public class HibernatingChamberData : PartModifierData<HibernatingChamberScript>
    {
        [SerializeField][PartModifierProperty]
        private float _hibernationPowerConsumption = 1f;

        public float HibernationPowerConsumption => _hibernationPowerConsumption*1e3f;
        
    }
}