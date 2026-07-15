using System;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using ModApi.Design.PartProperties;
using UnityEngine;

[Serializable]
[DesignerPartModifier("Droodism.RagdollModifierData.Header", PanelOrder = 2000)]
[PartModifierTypeId("Ragdoll")]
public class RagdollModifierData : PartModifierData<RagdollModifierScript>
{
    [SerializeField]
    [PartModifierProperty]
    [DesignerPropertyToggleButton(Label = "Droodism.RagdollModifierData.EnableRagdoll", Order = 0, Tooltip = "Droodism.RagdollModifierData.EnableRagdollTooltip")]
    private bool _enableRagdoll = false;
    public bool EnableRagdoll
    {
        get => _enableRagdoll;
        set => _enableRagdoll = value;
    }
}