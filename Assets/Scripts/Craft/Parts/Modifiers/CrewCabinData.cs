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
        
        [SerializeField] [PartModifierProperty(true, false)]
        private double radiationShieldDuration = 100d;
        [SerializeField] [PartModifierProperty(true, false)]
        private double radiationShieldDurationUpperLimit = 100d;

        public double RadiationShieldDuration
        {
            get=> radiationShieldDuration;
            set=> radiationShieldDuration = value;
        }

        public double RadiationShieldDurationUpperLimit
        {
            get => radiationShieldDurationUpperLimit;
        }
        
    }
}