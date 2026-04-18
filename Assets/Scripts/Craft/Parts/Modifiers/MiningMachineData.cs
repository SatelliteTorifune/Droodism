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
        [SerializeField] [PartModifierProperty]
        private Vector3 positionOffset1 = Vector3.zero;
        
        [FormerlySerializedAs("deployRotationSpeed")] [SerializeField] [PartModifierProperty]
        private float deploySpeed =1f;
        
        public Vector3 PositionOffset1
        {
            get => positionOffset1;
        }
        public float DeploySpeed
        {
            get => deploySpeed*0.3f;
        }
      

        [SerializeField] [PartModifierProperty]
        private float _currentEnabledPercent;
        public float CurrentEnabledPercent
        {
            get => this._currentEnabledPercent;
            set => this._currentEnabledPercent = value;
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
    }
}