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
    [DesignerPartModifier("Glider")]
    [PartModifierTypeId("Droodism.Glider")]
    public class GliderData : PartModifierData<GliderScript>
    {
        public float Area = 15f;

        // 气动中心相对于挂点的局部偏移（可在编辑器里微调）
        public Vector3 AerodynamicCenterLocal = new Vector3(0f, -0.5f, 0f);

        // 升力/阻力曲线（Angle vs Coefficient）
        public AnimationCurve LiftCurve;   // CL（单位：无）
        public AnimationCurve DragCurve;   // CD（单位：无）

        // 最大容许的破坏力（如果想在极端升力下让伞撕裂）
        public float MaxBreakForce = 3e5f;

        
    }
}