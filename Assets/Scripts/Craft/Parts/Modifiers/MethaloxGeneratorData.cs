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
    [DesignerPartModifier("MethaloxGenerator")]
    [PartModifierTypeId("MethaloxGenerator")]
    public class MethaloxGeneratorData : PartModifierData<MethaloxGeneratorScript>
    {
        [SerializeField] [PartModifierProperty(true, false)]
        private float batteryComsumption=1f;

        [SerializeField] [PartModifierProperty(true, false)]
        private float hpco2Comsumption=1f;

        [SerializeField] [PartModifierProperty(true, false)]
        private float methaneloxGeneration=1f;

        [SerializeField] [PartModifierProperty(true, false)]
        private float waterComsumption=1f;

        [SerializeField] [PartModifierProperty(true, false)]
        private float hpoxygenComsumption=1f;

        public float BatteryComsumption
        {
            get => this.batteryComsumption*100f;
        }

        public float Hpco2Comsumption
        {
            get => this.hpco2Comsumption*0.08f;
        }

        public float MethaneloxGeneration
        {
            get => this.methaneloxGeneration*0.01f;
        }
        
        public float WaterComsumption
        {
            get => this.waterComsumption*0.004f;
        }

        public float HpoxygenComsumption
        {
            get => this.hpoxygenComsumption*0.015f;
        }
        

    }
}