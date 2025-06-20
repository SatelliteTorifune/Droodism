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
    [DesignerPartModifier("Sodium Peroxide")]
    [PartModifierTypeId("SodiumPeroxide")]
    public class Sodium_PeroxideData : PartModifierData<Sodium_PeroxideScript>
    {
    }
}