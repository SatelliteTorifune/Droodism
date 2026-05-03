using UnityEngine.Serialization;

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
        
        [FormerlySerializedAs("deployRotationSpeed")] [SerializeField] [PartModifierProperty]
        private float deploySpeed =1f;
        
        public float DeploySpeed
        {
            get => deploySpeed*0.3f;
        }
      

        [FormerlySerializedAs("_currentEnabledPercent")] [SerializeField] [PartModifierProperty]
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

        [SerializeField] [PartModifierProperty]
        private float workingSpeed=1;

        public float WorkingSpeed
        {
            get => this.workingSpeed;
            set => this.workingSpeed = value;
        }
    }
}