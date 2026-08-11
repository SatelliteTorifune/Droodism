using ModApi;
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
    [DesignerPartModifier("Droodism.GasDealerData.Header")]
    [PartModifierTypeId("GasDealer")]
    public class GasDealerData : PartModifierData<GasDealerScript>
    {
        [SerializeField] [DesignerPropertySlider(0.1f, 1f, 10, Label = "Droodism.GasDealerData.GasFlowRate", Tooltip = "Droodism.GasDealerData.GasFlowRateTooltip")]
        private float gasFlowRate = 1f;
        [SerializeField] [PartModifierProperty]
        private float batteryConsumption = 1;
        [SerializeField] [DesignerPropertySpinner(Label = "Droodism.GasDealerData.GasType", Order = 0, Tooltip = "Droodism.GasDealerData.GasTypeTooltip")]
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
        private void GetSpinnerValues(List<string> gasType)
        {
            gasType.Clear();
            gasType.Add("O2");
            gasType.Add("CO2");
            gasType.Add("N2");
        }

        internal string GetSpinnerNames()
        {
            switch (this.gasType)
             {
                 case "O2":
                     return "<color=green>" + Locale.GetString("Droodism.GasDealerData.GasOxygen") + "</color>";
                 case "CO2":
                     return "<color=green>" + Locale.GetString("Droodism.GasDealerData.GasCarbonDioxide") + "</color>";
                 case "N2":
                     return "<color=green>" + Locale.GetString("Droodism.GasDealerData.GasNitrogen") + "</color>";
                 default:
                     return this.gasType;
             }
            
        }
        
    }
}