using System.Linq.Expressions;
using Assets.Scripts.Design;
using ModApi;
using ModApi.Design.PartProperties;
using ModApi.Math;
using UnityEngine.UI;

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
    [DesignerPartModifier("Droodism.ChemicalReactorData.Header")]
    [PartModifierTypeId("ChemicalReactor")]
        public class ChemicalReactorData : PartModifierData<ChemicalReactorScript>
    {
        [SerializeField]  
        [DesignerPropertySpinner(Label = "Droodism.ChemicalReactorData.ReactionType", Order = 0, Tooltip = "Droodism.ChemicalReactorData.ReactionTypeTooltip")]
        private string reactorType = "LH2+LOX=Hydrolox";
        
        [SerializeField]  
        [DesignerPropertySlider(0.1f, 1f, 10, Label = "Droodism.ChemicalReactorData.GenerationRate", Tooltip = "Droodism.ChemicalReactorData.GenerationRateTooltip")]
        private float generationRate = 0.3f;
        
        [SerializeField]
        [DesignerPropertyLabel(Order = 3, PreserveState = false, NeverSerialize = true)]
        private string reactionDescription;

        [SerializeField] [PartModifierProperty]
        private float batteryConsumption = 0.4f;
        

        public string ReactorType
        {
            get => reactorType;
        }
        public float GenerationRate
        {
            get => generationRate;
        }
        public float BatteryConsumption
        {
            get => batteryConsumption;
        }
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested<string>(() => this.reactorType, x => this.GetReactorName());
            d.OnValueLabelRequested<string>((Expression<Func<string>>) (() => this.reactionDescription), (Func<string, string>) (x => GetReactorTypeDescription()));
            d.OnSpinnerValuesRequested<string>(() => this.reactorType, this.GetSpinnerValues);
            d.OnPropertyChanged<string>((Expression<Func<string>>) (() => this.reactorType), (Action<string, string>) ((newVal, oldVal) => this.OnPropertyChangedInDesigner()));
        }
        private void OnPropertyChangedInDesigner()
        {
            Symmetry.SynchronizePartModifiers(this.Part.PartScript);
            this.reactionDescription=GetReactorTypeDescription();
            this.Part.PartScript.CraftScript.SetStructureChanged();
        }

        private string GetReactorTypeDescription()
        {
            switch (this.reactorType)
            {
                case "LH2+LOX=Hydrolox":
                    return Locale.GetString("Droodism.ChemicalReactorData.DescHydrolox");
                case "N2+LH2=N2H4":
                    return Locale.GetString("Droodism.ChemicalReactorData.DescN2H4");
                case "H2O+CO2=Methanelox":
                    return Locale.GetString("Droodism.ChemicalReactorData.DescMethalox");
                default:
                    return this.reactorType;
            }
        }
        private void GetSpinnerValues(List<string> reactorTypes)
        {
            reactorTypes.Clear();
            reactorTypes.Add("LH2+LOX=Hydrolox");
            reactorTypes.Add("N2+LH2=N2H4");
            reactorTypes.Add("H2O+CO2=Methanelox");
        }
        
        

        internal string GetReactorName()
        {
            switch (this.reactorType)
            {
                case "LH2+LOX=Hydrolox":
                    return "<color=yellow>LH2</color>+<color=yellow>LOX</color>=<br><color=green>Hydrolox</color>";
                case "N2+LH2=N2H4":
                    return "<color=yellow>N2</color>+<color=yellow>LH2</color>=<br><color=green>N2H4</color>";
                case "H2O+CO2=Methanelox":
                    return "<color=yellow>H2O</color>+<color=yellow>CO2</color>=<br><color=green>Methalox</color>";
                default:
                    return this.reactorType;
            }
           
        }
    
    }
}