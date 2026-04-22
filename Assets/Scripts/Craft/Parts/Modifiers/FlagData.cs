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
        [SerializeField] [PartModifierProperty]
        private float currentEnabledPercent1;
        public float CurrentEnabledPercent1
        {
            get => this.currentEnabledPercent1;
            set => this.currentEnabledPercent1 = value;
        }
        
        [SerializeField] [PartModifierProperty]
        private float _currentEnabledPercent2;
        public float CurrentEnabledPercent2
        {
            get => this._currentEnabledPercent2;
            set => this._currentEnabledPercent2 = value;
        }
        
        [SerializeField] [PartModifierProperty]
        private float _currentEnabledPercent3;
        public float CurrentEnabledPercent3
        {
            get => this._currentEnabledPercent3;
            set => this._currentEnabledPercent3 = value;
        }
        public float DeploySpeed
        {
            get =>0.5f;
        }
    }
}