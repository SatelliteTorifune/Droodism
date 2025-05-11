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
    [DesignerPartModifier("SupportLife")]
    [PartModifierTypeId("SupportLife")]
    public class SupportLifeData : PartModifierData<SupportLifeScript>
    {
        [SerializeField] [PartModifierProperty(true, false)]
        private float _oxygenComsumeRate;
        
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

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.UpdateFuelType();
        }
        
        private void UpdateFuelType()
        {
            this.FuelType = Assets.Scripts.Game.Instance.PropulsionData.GetFuelType("Oxygen");
        }
    }
    
    
}