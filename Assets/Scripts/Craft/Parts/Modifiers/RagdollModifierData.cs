using System;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using ModApi.Design.PartProperties;
using UnityEngine;

[Serializable]
[DesignerPartModifier("Ragdoll",PanelOrder = 2000)]
[PartModifierTypeId("Ragdoll")]
public class RagdollModifierData : PartModifierData<RagdollModifierScript>
{
    [SerializeField]
    [PartModifierProperty]
    [DesignerPropertyToggleButton(Label = "Enable Ragdoll", Order = 0, Tooltip = "Whether to enable ragdoll physics on this part.")]
    private bool _enableRagdoll = false;
    public bool EnableRagdoll
    {
        get => _enableRagdoll;
        set => _enableRagdoll = value;
    }
}