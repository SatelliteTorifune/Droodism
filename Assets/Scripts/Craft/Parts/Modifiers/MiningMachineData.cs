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
    [DesignerPartModifier("MiningMachine")]
    [PartModifierTypeId("MiningMachine")]
    public class MiningMachineData : PartModifierData<MiningMachineScript>
    {
    }
}