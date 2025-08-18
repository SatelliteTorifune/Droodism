using System.Windows.Forms;

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
    [DesignerPartModifier("GasDealer")]
    [PartModifierTypeId("GasDealer")]
    public class GasDealerData : PartModifierData<GasDealerScript>
    {
        [SerializeField] [DesignerPropertySlider( 0.1f, 1f, 10,Label="Gas Flow Rate",Tooltip="Determines the rate which High Pressure gas is released/compressed by the part.")]
        private float gasFlowRate = 1f;

        public float GasFlowRate
        {
            get => this.gasFlowRate*2.5f;
        }

        [SerializeField] [PartModifierProperty]
        private float batteryConsumption = 1;

        public float BatteryConsumption
        {
            get => this.batteryConsumption*11.45141919810f;
        }
    }
}