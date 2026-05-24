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
    [SerializeField]
    [PartModifierProperty]
    [DesignerPropertySlider(0f, 1f, 21, Label = "IK Blend Speed", Order = 1, 
        Tooltip = "How quickly IK control blends when entering ragdoll.")]
    private float _ikBlendSpeed = 0.1f;
    public bool EnableRagdoll
    {
        get => _enableRagdoll;
        set => _enableRagdoll = value;
    }

    [SerializeField] [PartModifierProperty]
    private bool animateEnabled2=true;
    [SerializeField] [PartModifierProperty]
    private bool ikEnabled=true;
    public float IkBlendSpeed => _ikBlendSpeed;
    protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
    {
        base.OnDesignerInitialization(d);
            
        d.OnPropertyChanged<bool>(
            () => _enableRagdoll,
            (newVal, oldVal) => Script?.SetRagdollMode(newVal)
        );
    }
    
    public bool AnimateEnabled2 { get; set; }
    public bool IKEnabled { get; set; }
}