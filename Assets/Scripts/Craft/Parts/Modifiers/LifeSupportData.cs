namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Assets.Scripts.Design;
    using ModApi.Design.PartProperties;
    using ModApi;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Propulsion;
    using System.Linq.Expressions;
    using UnityEngine;  

    [Serializable]
    [DesignerPartModifier("LifeSupport")]
    [PartModifierTypeId("LifeSupport")]
    public class LifeSupportData : PartModifierData<LifeSupportScript>
    {      
    [SerializeField]
    [PartModifierProperty(true, false)]
    private int _fuelSourceAttachPoint;
    /// <summary>The fuel type.</summary>
    [SerializeField]
    [DesignerPropertySpinner(Label = "Fuel Type", Order = 2, Tooltip = "")]
    private string _fuelType = "oxygen";
    /// <summary>The actual fuel type modifier</summary>
    private FuelType _fuelTypeModifier;
    public int FuelSourceAttachPoint => this._fuelSourceAttachPoint;

    private double _consumptionOverride = 1;
    private float _stretch = 1f;

    //public bool IsTourist => this.Part.PartType.Id == "Eva-Tourist";

    public float FuelFlow = 1f;

    private float _fuelConsumptionScale = 1f;




    public FuelType FuelType

    
    {
      get => (double) this._consumptionOverride != 0.0 ? this._fuelTypeModifier : (FuelType) null;
      private set => this._fuelTypeModifier = value;
    }

    protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
    {
      base.OnDesignerInitialization(d);
      d.OnVisibilityRequested<string>((Expression<Func<string>>) (() => this._fuelType), (Func<bool, bool>) (x => (double) this._consumptionOverride != 0.0));
      d.OnValueLabelRequested<float>((Expression<Func<float>>) (() => this._fuelConsumptionScale), (Func<float, string>) (x => Utilities.FormatPercentage(x)));
      d.OnValueLabelRequested<string>((Expression<Func<string>>) (() => this._fuelType), (Func<string, string>) (x => this.FuelType?.Name ?? string.Empty));
      d.OnSpinnerValuesRequested<string>((Expression<Func<string>>) (() => this._fuelType), new Action<List<string>>(this.GetSpinnerValues));
      d.OnPropertyChanged<float>((Expression<Func<float>>) (() => this._fuelConsumptionScale), (Action<float, float>) ((newVal, oldVal) => this.OnFuelConsumptionChanged()));
      d.OnPropertyChanged<string>((Expression<Func<string>>) (() => this._fuelType), (Action<string, string>) ((newVal, oldVal) => this.OnPropertyChangedInDesigner(true)));
      {
        d.Manager.RefreshUI();
        Symmetry.SynchronizePartModifiers(this.Script.PartScript);
        this.Script.PartScript.CraftScript.SetStructureChanged();
      };
      d.OnPropertyChanged<float>((Expression<Func<float>>) (() => this._stretch), (Action<float, float>) ((newVal, oldVal) =>
      {
        d.Manager.RefreshUI();
        
        Symmetry.SynchronizePartModifiers(this.Script.PartScript);
        this.Script.PartScript.CraftScript.SetStructureChanged();
      }));

    }
    protected override void OnInitialized()
    {
      base.OnInitialized();
      this.UpdateFuelType();
    }
    private void GetSpinnerValues(List<string> fuelTypes)
    {
      fuelTypes.Clear();
      foreach (FuelType fuel in (IEnumerable<FuelType>) Assets.Scripts.Game.Instance.PropulsionData.Fuels)
      {
        if (fuel.DisplayInDesigner && (double) fuel.ShockIntensity == -20)
          fuelTypes.Add(fuel.Id);
      }
    }

    private void OnPropertyChangedInDesigner(bool updateFuelType)
    {
      Symmetry.SynchronizePartModifiers(this.Part.PartScript);
      if (updateFuelType)
        this.UpdateFuelType();
      this.Part.PartScript.CraftScript.SetStructureChanged();
    }
    private void OnFuelConsumptionChanged()
    {
      Symmetry.SynchronizePartModifiers(this.Script.PartScript);
      this.Script.PartScript.CraftScript.SetStructureChanged();
    }
    private void UpdateFuelType()
    {
      this.FuelType = Game.Instance.PropulsionData.GetFuelType(this._fuelType);
    }
    

    }

    
    
}