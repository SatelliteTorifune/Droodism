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
    [DesignerPartModifier("Flag")]
    [PartModifierTypeId("Flag")]
    public class FlagData : PartModifierData<FlagScript>
    {

        private Vector3 positionOffset1 = Vector3.zero;
        private float currentExtentPercent = 0;
        private float currentExtentPercent2 = 0.001f;
        private float currentRotationPercent = 0f;
        [SerializeField]
        [DesignerPropertySlider(0.1f, 1f,10, Label = "Extend Speed")]
        public float ExtendSpeed = 0.2f;
        [SerializeField] [DesignerPropertySlider(1f, 30f,30, Label = "Deploy Rotation Speed")]
        private float deployRotationSpeed = 15f;

        [SerializeField] [PartModifierProperty]
        private bool isDeployed;

        [SerializeField] [PartModifierProperty]
        private bool stayDeployed;
       
        public float RotationSpeed = 0.75f;
        
        public float DeployRotationSpeed
        {
            get => deployRotationSpeed;
        }

        public float CurrentExtentPercent
        {
            get => currentExtentPercent;
            set => currentExtentPercent = value;
        }
        public float CurrentExtentPercent2
        {
            get => currentExtentPercent2;
            set => currentExtentPercent2 = value;
        }
        public float CurrentRotationPercent
        {
            get => currentRotationPercent;
            set => currentRotationPercent = value;
        }

        public bool IsDeployed
        {
            get => isDeployed;
            set => isDeployed = value;
        }

        public Vector3 PositionOffset1
        {
            get => positionOffset1;
        }

        public bool StayDeployed
        {
            get => stayDeployed;
            set => stayDeployed = value;
        }
    }
}