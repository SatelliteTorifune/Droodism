using ModApi.Craft.Propulsion;
using ModApi.Design.PartProperties;
//去你妈的我要躺在床上对着梅莉的蕾丝边小白袜撸管子,谁他妈想写这东西
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
        private static bool isTourist;
        
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
        
        [SerializeField] 
        [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=green>Oxygen</color> Carry Amount(days)", Tooltip = "Define How much <color=green>Oxygen</color> Drood himself/herself will carry when Eva.")]
        private float desireOxygenCapacity = 0.1f;
        [SerializeField] [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=yellow>Food</color> Carry Amount(days)", Tooltip = "Define How much <color=yellow>Food</color> Drood himself/herself will carry when Eva.")]
        private float desireFoodCapacity = 0.1f;
        [SerializeField] [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=red>Water</color> Carry Amount(days)", Tooltip = "Define How much<color=red> Drink Water</color> Drood himself/herself will carry when Eva.")]
        private float desireWaterCapacity = 0.1f;
        
        public double _oxygenAmountBuffer=1f;
        public double _foodAmountBuffer=1f;
        public double _waterAmountBuffer = 1f;
        public double _co2AmountBuffer=1f;
        public double _wastedWaterAmountBuffer=1f;
        public double _solidWasteAmountBuffer=1f;
        [SerializeField][PartModifierProperty(true, false)]
        public double evaConsumeEfficiency=0.3f;
        
        private int _fuelSourceAttachPoint;
        public int FuelSourceAttachPoint
        {
            get=>_fuelSourceAttachPoint;
            set=>_fuelSourceAttachPoint=value;

        }
        private FuelTankScript _fuelTank;
        
        
        public float OxygenComsumeRate
        {
            get =>this._oxygenComsumeRate*0.007f; 
            private set=>this._oxygenComsumeRate = value;
        }
        public float FoodComsumeRate
        {
            get
                =>this._foodComsumeRate*0.0000058f;
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
            get=>this.desireFoodCapacity*0.5f;
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