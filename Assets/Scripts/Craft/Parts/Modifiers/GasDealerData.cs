using System.Windows.Forms;
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
    [DesignerPartModifier("GasDealer")]
    [PartModifierTypeId("GasDealer")]
    public class GasDealerData : PartModifierData<GasDealerScript>
    {
        [SerializeField] [DesignerPropertySlider( 0.1f, 1f, 10,Label="Gas Flow Rate",Tooltip="Determines the rate which High Pressure gas is released/compressed by the part.")]
        private float gasFlowRate = 1f;
        [SerializeField] [PartModifierProperty]
        private float batteryConsumption = 1;
        [SerializeField]  [DesignerPropertySpinner(Label = "Gas Type", Order = 0, Tooltip = "The type of Gas of This Part Works")]
        private string gasType = "O2";

        public float GasFlowRate
        {
            get => this.gasFlowRate*2.5f;
        }

        public string GasType
        {
            get => this.gasType;
        }
       

        public float BatteryConsumption
        {
            get => this.batteryConsumption*11.45141919810f;
        }

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested<string>(() => this.gasType, x => GetSpinnerNames());
            d.OnSpinnerValuesRequested<string>(() => this.gasType, this.GetSpinnerValues);
        }
        private void GetSpinnerValues(List<string> shieldTypes)
        {
            shieldTypes.Clear();
            shieldTypes.Add("O2");
            shieldTypes.Add("CO2");
            shieldTypes.Add("N2");
        }

        internal string GetSpinnerNames()
        {
            switch (this.gasType)
             {
                 case "O2":
                     return "<color=green>Oxygen</color>";
                 case "CO2":
                     return "<color=green>Carbon Dioxide</color>";
                 case "N2":
                     return "<color=green>Nitrogen</color>";
                 default:
                     return this.gasType;
             }
           
        }
        
    }
}