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
        private float _oxygenComsumeRate=0.01f;
        [SerializeField][PartModifierProperty(true, false)]
        private float _foodComsumeRate=0.01f;

        [SerializeField] [PartModifierProperty(true, false)]
        private float _oxygenDamageScale=0.5f;
        [SerializeField][PartModifierProperty(true, false)]
        private float foodDamageScale=0.01f;

        [SerializeField] [PartModifierProperty]
        private float desireOxygenCapacity = 300;
        [SerializeField] [PartModifierProperty]
        private float desireFoodCapacity = 250;
        [SerializeField] [PartModifierProperty(true, false)]
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
            get
            =>this._oxygenComsumeRate; 
            set=>this._oxygenComsumeRate = value;
        }
        public float FoodComsumeRate
        {
            get
                =>this._foodComsumeRate;
            set=>this._foodComsumeRate = value;
        }
        public float OxygenDamageScale
        {
            get=>this._oxygenDamageScale;
            set=>this._oxygenDamageScale = value;
        }
        public float FoodDamageScale
        {
            get=>this.foodDamageScale;
            set=>this.foodDamageScale = value;
        }
        
        public float DesireOxygenCapacity
        {
            get=>this.desireOxygenCapacity;
            set=>this.desireOxygenCapacity = value;
        }
        
        public float DesireFoodCapacity
        {
            get=>this.desireFoodCapacity;
            set=>this.desireFoodCapacity = value;
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