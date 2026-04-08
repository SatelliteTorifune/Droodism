using System.Linq.Expressions;
using Assets.Scripts.Design;
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
    [DesignerPartModifier("ChemicalReactor")]
    [PartModifierTypeId("ChemicalReactor")]
    public class ChemicalReactorData : PartModifierData<ChemicalReactorScript>
    {
        [SerializeField]  
        [DesignerPropertySpinner(Label = "Reaction Type", Order = 0, Tooltip = "The type of Chemical Reactor of This Part Applies")]
        private string reactorType = "LH2+LOX=Hydrolox";
        
        [SerializeField]  
        [DesignerPropertySlider(0.1f, 1f, 10, Label = "Generation Rate", Tooltip = "Determines the rate which the chemical reactor processes fuel.")]
        private float generationRate = 0.3f;
        [SerializeField]
        [DesignerPropertyLabel(Order = 3, PreserveState = false, NeverSerialize = true)]
        private string reactionDescription;

        public string ReactorType
        {
            get => reactorType;
        }
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested<string>(() => this.reactorType, x => x);
           
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
        private void GetSpinnerValues(List<string> reactorTypes)
        {
            reactorTypes.Clear();
            reactorTypes.Add("LH2+LOX=Hydrolox");
            reactorTypes.Add("N2+LH2=N2H4");
            reactorTypes.Add("N2");
        }

        internal string GetReactorTypeDescription()
        {
            switch (this.reactorType)
            {
                case "LH2+LOX=Hydrolox":
                    return "Hydrolox Generating:Use Liquid Hydrogen and HP Oxygen to generate Hydrolox";
                case "N2+LH2=N2H4":
                    return "Monopropellant Generating<br>Use Nitrogen and LH2 to generate Monopropellant";
                case "N2":
                    return "<color=green>Nitrogen</color>";
                default:
                    return this.reactorType;
            }
           
        }
    
    }
}