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
    [DesignerPartModifier("SewageTreatDeivce")]
    [PartModifierTypeId("SewageTreatDeivce")]
    public class SewageTreatDeivceData : PartModifierData<SewageTreatDeivceScript>
    {
        [SerializeField] [PartModifierProperty(true, false)]
        private float _wastedWaterComsumeRate=1f;
    }
}