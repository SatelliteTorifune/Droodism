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
    [DesignerPartModifier("GravityRing")]
    [PartModifierTypeId("GravityRing")]
    public class GravityRingData : PartModifierData<GravityRingScript>
    {
        [SerializeField] [DesignerPropertyToggleButton(Label = "Reverse the Rotation Direction")]
        public bool IsReverse = false;
        private Vector3 positionOffset1 = Vector3.zero;
        [SerializeField]
        [PartModifierProperty]
        private float currentExtentPercent = 0;
        [SerializeField]
        [PartModifierProperty]
        public float CurrentRotation = 90f;
        [SerializeField]
        [DesignerPropertySlider(0.1f, 1f,10, Label = "Extend Speed")]
        public float ExtendSpeed = 0.2f;
        [SerializeField] [DesignerPropertySlider(1f, 30f,30, Label = "Deploy Rotation Speed")]
        private float deployRotationSpeed = 15f;
        [SerializeField] [DesignerPropertySlider(0.1f, 2f,20, Label = "Main Rotation Speed")]
        private float rotationSpeed = 1f;
        
        
        public float DeployRotationSpeed
        {
            get => deployRotationSpeed;
        }

        public float CurrentExtentPercent
        {
            get => currentExtentPercent;
            set => currentExtentPercent = value;
        }
        

        public Vector3 PositionOffset1
        {
            get => positionOffset1;
        }

        public float RotationSpeed
        {
            get => rotationSpeed*0.5f;
            set => rotationSpeed = value;
        }
        
    }
}