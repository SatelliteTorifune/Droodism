using ModApi.Craft.Propulsion;
using ModApi.Design.PartProperties;

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
    [DesignerPartModifier("SupportLife",PanelOrder = 2000)]
    [PartModifierTypeId("SupportLife")]
    public class SupportLifeData : PartModifierData<SupportLifeScript>
    {
        [SerializeField] [PartModifierProperty(true, false)]
        private float _oxygenComsumeRate=1f;
        [SerializeField][PartModifierProperty(true, false)]
        private float _foodComsumeRate=1f;
        [SerializeField][PartModifierProperty(true, false)]
        private float _waterComsumeRate=1f;
        
        [SerializeField] [PartModifierProperty(true, false)]
        private float _oxygenDamageScale=1f;
        [SerializeField][PartModifierProperty(true, false)]
        private float foodDamageScale=1f;
        [SerializeField][PartModifierProperty(true, false)]
        private float waterDamageScale=1f;
        
        [SerializeField] [PartModifierProperty]
        private float desireOxygenCapacity = 0.3f;
        [SerializeField] [PartModifierProperty]
        private float desireFoodCapacity = 1f;
        [SerializeField] [PartModifierProperty]
        private float desireWaterCapacity = 0.5f;
        //[SerializeField] [PartModifierProperty(true, false)]
        private int _fuelSourceAttachPoint = 0;
        public int FuelSourceAttachPoint
        {
            get=>_fuelSourceAttachPoint;
            set=>_fuelSourceAttachPoint=value;

        }
        private FuelTankScript _fuelTank;
        private FuelType _fuelType;
        
        public FuelType FuelType
        {
            get => this._fuelType;
            private set => this._fuelType = value;
        }
        
        public float OxygenComsumeRate
        {
            get =>this._oxygenComsumeRate*0.007f; 
            private set=>this._oxygenComsumeRate = value;
        }
        public float FoodComsumeRate
        {
            get
                =>this._foodComsumeRate*0.00058f;
            set=>this._foodComsumeRate = value;
        }

        public float WaterComsumeRate
        {
            get => _waterComsumeRate*0.0000347f;
            set => this._waterComsumeRate = value;
        }
        public float OxygenDamageScale
        {
            get=>this._oxygenDamageScale*0.35f;
            set=>this._oxygenDamageScale = value;
        }
        public float FoodDamageScale
        {
            get=>this.foodDamageScale*0.0002f;
            set=>this.foodDamageScale = value;
        }
        
        public float WaterDamageScale
        {
            get=>this.waterDamageScale*0.001f;
            set=>this.waterDamageScale = value;
        }
        
        public float DesireOxygenCapacity
        {
            get=>this.desireOxygenCapacity*600;
            private set=>this.desireOxygenCapacity = value;
        }
        
        public float DesireFoodCapacity
        {
            get=>this.desireFoodCapacity*50f;
            set=>this.desireFoodCapacity = value;
        }
        
        public float DesireWaterCapacity
        {
            get=>this.desireWaterCapacity*3;
            set=>this.desireWaterCapacity = value;
        }
        
        
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            
            
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
        
        
    }
    
    
}