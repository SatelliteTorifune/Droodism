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
    [DesignerPartModifier("MethaloxGenerator")]
    [PartModifierTypeId("MethaloxGenerator")]
    public class MethaloxGeneratorData : PartModifierData<MethaloxGeneratorScript>
    {
        [FormerlySerializedAs("batteryComsumption")] [SerializeField] [PartModifierProperty(true, false)]
        private float batteryConsumption=1f;

        [FormerlySerializedAs("hpco2Comsumption")] [SerializeField] [PartModifierProperty(true, false)]
        private float hpco2Consumption=1f;

        [SerializeField] [PartModifierProperty(true, false)]
        private float methaneloxGeneration=1f;

        [FormerlySerializedAs("waterComsumption")] [SerializeField] [PartModifierProperty(true, false)]
        private float waterConsumption=1f;

        [FormerlySerializedAs("hpoxygenComsumption")] [SerializeField] [PartModifierProperty(true, false)]
        private float hpoxygenConsumption=1f;

        public float BatteryConsumption
        {
            get => this.batteryConsumption*100f;
        }

        public float Hpco2Consumption
        {
            get => this.hpco2Consumption*0.08f;
        }

        public float MethaneloxGeneration
        {
            get => this.methaneloxGeneration*0.01f;
        }
        
        public float WaterConsumption
        {
            get => this.waterConsumption*0.004f;
        }

        public float HpoxygenConsumption
        {
            get => this.hpoxygenConsumption*0.015f;
        }
        

    }
}