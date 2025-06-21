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
    [DesignerPartModifier("ElectrolyticDevice")]
    [PartModifierTypeId("ElectrolyticDevice")]
    public class ElectrolyticDeviceData : PartModifierData<ElectrolyticDeviceScript>
    {
     
        [SerializeField]
        [PartModifierProperty(true, false)]
        private double _baseMass = 100f;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private double waterComsuptionRate = 1;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private double oxygenGenerationRate = 1;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private double hyrogenGenerationRate = 1;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private double powerConsumptionRate = 1;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private string _subPartPath = string.Empty;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _positionOffset = Vector3.zero;
        public Vector3 PositionOffset
        {
            get => this._positionOffset;
            set => this._positionOffset = value;
        }

        public double WaterComsuptionRate
        {
            get => this.waterComsuptionRate;
        }
        
        public double OxygenGenerationRate
        {
            get => this.oxygenGenerationRate;
        }
        
        public double HydrogenGenerationRate
        {
            get => this.hyrogenGenerationRate;
        }
        
        public double PowerConsumptionRate
        {
            get => this.powerConsumptionRate;
        }
        
        public string SubPartPath => this._subPartPath;
        
    }
}