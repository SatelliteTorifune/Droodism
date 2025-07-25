namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
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

        private string _subPartPath = "Device/DeviceFan";
        private Vector3 _positionOffset = Vector3.zero;
        public Vector3 PositionOffset
        {
            get => this._positionOffset;
            set => this._positionOffset = value;
        }

        public double WaterComsuptionRate
        {
            get => this.waterComsuptionRate*0.00025;
        }
        
        public double OxygenGenerationRate
        {
            get => this.oxygenGenerationRate*0.145;
        }
        
        public double HydrogenGenerationRate
        {
            get => this.hyrogenGenerationRate*3E-04;
        }
        
        public double PowerConsumptionRate
        {
            get => this.powerConsumptionRate*4.2;
        }
        
        public string SubPartPath => this._subPartPath;
        
    }
}