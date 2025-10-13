namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("CrewCabin")]
    [PartModifierTypeId("CrewCabin")]
    public class CrewCabinData : PartModifierData<CrewCabinScript>
    {
        
        [SerializeField]
        [PartModifierProperty(true, false)]
        private double waterComsuptionRate = 1;
        
        
    }
}