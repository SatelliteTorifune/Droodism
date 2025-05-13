using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Ui.Inspector;
using System;
using ModApi.Craft.Propulsion;
using ModApi.Math;
using UnityEngine;

#nullable disable
namespace Assets.Scripts.Craft.Parts.Modifiers
{
  public class SupportLifeScript : 
    PartModifierScript<SupportLifeData>,
    IDesignerStart,
    IGameLoopItem,
    IFlightStart,
    IFlightUpdate
  {
    private IFuelSource _fuelSource;
    private FuelTankScript _fuelTank;
    private IInputController _inputThrottle;
    private double _fuelRemoved;
    private float _powerScale = 1f;

    public bool UsesMachNumber => false;

    private FuelTankScript FuelTank
    {
      get => this._fuelTank;
      set
      {
        if (this._fuelTank == value) // 替换为 == 比较
          return;
        if (Game.InFlightScene && this._fuelTank != null)
          this._fuelTank.CraftFuelSourceChanged -= new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
        this._fuelTank = value;
        if (!Game.InFlightScene || this._fuelTank == null)
          return;
        this._fuelTank.CraftFuelSourceChanged += new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
      }
    }

    void IDesignerStart.DesignerStart(in DesignerFrameData frame)
    {
      // 移除缩放、拉伸等逻辑，仅保留必要初始化
      
    }

    void IFlightStart.FlightStart(in FlightFrameData frame)
    {
      this._powerScale = this.Data.OxygenComsumeRate;
      
    }

    void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
    {
      if (frame.DeltaTimeWorld == 0.0)
        return;
      if (true)
      {
        double num1 = 1.0 / frame.DeltaTimeWorld;
        double num2 =_powerScale * (double)Data.OxygenComsumeRate * frame.DeltaTimeWorld;
        // 访问同一 part 的 FuelTankScript modifier 来消耗燃料
        var fuelTankModifier = this.PartScript.GetModifier<FuelTankScript>();
        Debug.LogFormat("0");
        if (fuelTankModifier != null)
        {
          
          Debug.LogFormat("1");
          fuelTankModifier.RemoveFuel(num2 * num1);
          Debug.LogFormat("{0}",fuelTankModifier.Position);
          this._fuelSource.RemoveFuel(num2 * num1);
          Debug.LogFormat("3");
          this._fuelRemoved = num2 * num1;
        }
        else
        {
          this._fuelRemoved = 0.0;
        }
      }
      
    }

    public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
    {
      this.OnCraftStructureChanged(craftScript);
    }

    public override void OnCraftStructureChanged(ICraftScript craftScript)
    {
      //this.FuelTank = EngineUtilities.GetFuelTank(this.PartScript, this.Data.FuelSourceAttachPoint, "Oxygen")?.Script;
      this.RefreshFuelSource();
    }

    public override void OnGenerateInspectorModel(PartInspectorModel model)
    {
      GroupModel groupModel = new GroupModel("Performance");
      model.AddGroup(groupModel);
      this.CreateInspectorModel(groupModel, false);
    }

    public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
    {
      // 移除缩放、拉伸等逻辑
    }
    
    protected override void OnInitialized()
    {
      base.OnInitialized();
    }

    private void CreateInspectorModel(GroupModel model, bool flight)
    {
      model.Add<TextModel>(new TextModel("Fuel Flow", (Func<string>) (() => Units.GetMassFlowRateString((flight ? (float) this._fuelRemoved : this.Data.OxygenComsumeRate) * 1.429f)), tooltip: "The kilograms of Oxygen being burnt per second."));
      model.Add<TextModel>(new TextModel("Fuel Type", (Func<string>) (() => "Oxygen"), tooltip: "The fuel type is fixed to Oxygen."));
      if (!flight)
        return;
    }

    private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.RefreshFuelSource();

    private void RefreshFuelSource()
    {
      var fuelTankModifier = this.PartScript.GetModifier<FuelTankScript>();
      if ("Oxygen" == fuelTankModifier?.CraftFuelSource?.FuelType.Name)
      {
        this._fuelSource = (IFuelSource) fuelTankModifier?.CraftFuelSource;
      }
      
    }
  }
}