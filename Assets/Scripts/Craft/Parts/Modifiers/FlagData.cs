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
    [DesignerPartModifier("Droodism.FlagData.Header")]
    [PartModifierTypeId("Flag")]
    public class FlagData : PartModifierData<FlagScript>
    {

        private Vector3 positionOffset = Vector3.zero;
        private float currentExtentPercent = 0;
        private float currentExtentPercent2 = 0.001f;
        private float currentRotationPercent = 0f;
        [SerializeField]
        [DesignerPropertySlider(0.1f, 1f,10, Label = "Droodism.FlagData.ExtendSpeed")]
        public float ExtendSpeed = 0.2f;
        [SerializeField] [DesignerPropertySlider(1f, 30f,30, Label = "Droodism.FlagData.DeployRotationSpeed")]
        private float deployRotationSpeed = 15f;

        [SerializeField] [PartModifierProperty]
        private bool isDeployed;

        [SerializeField] [PartModifierProperty]
        private bool stayDeployed;

        [SerializeField] [PartModifierProperty]
        private string flagContent;
       
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

        public Vector3 PositionOffset
        {
            get => positionOffset;
        }

        public bool StayDeployed
        {
            get => stayDeployed;
            set => stayDeployed = value;
        }
        
        public string FlagContent
        {
            get => flagContent;
        }

        public void SetFlagContent(string incomingFlagContent)
        {
            if (incomingFlagContent==null)
            {
                return;
            }
            this.flagContent = incomingFlagContent;
        }
    }
}