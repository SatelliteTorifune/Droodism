using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Ui.Inspector;
using System;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Flight;
using ModApi.Craft.Propulsion;
using ModApi.Math;
using ModApi.Planet;
using UnityEngine;
using Assets.Scripts.Flight.Sim;
using ModApi.Craft.Parts.Modifiers.Propulsion;
using ModApi.Flight.Sim;
using static Assets.Scripts.Flight.Sim.PlanetNode;

#nullable disable
namespace Assets.Scripts.Craft.Parts.Modifiers
{
  public class SupportLifeScript : 
    PartModifierScript<SupportLifeData>,
    IDesignerStart,
    IGameLoopItem,
    IReactionEngine,
    IFlightStart,
    IFlightUpdate
  {
    private IFuelSource _fuelSource;
    private FuelTankScript _fuelTank;
    private IInputController _inputThrottle;
    private float _fuelRemoved;
    private float _powerScale = 1f;
    private string currentPlanetName;
    private IPlanetData planetData;
    private float _oxygenConsumeRate;


    public bool IsActive => true;
    public float CurrentMassFlowRate { get=>_oxygenConsumeRate; }
    public float CurrentThrust { get=>0; }
    public IFuelSource FuelSource { get=>(IFuelSource)this._fuelTank; }
    public float MaximumMassFlowRate { get=>0; }
    public float MaximumThrust { get=>0; }
    public float ThrottleResponse { get=>0; }
    PartData IReactionEngine.Part => this.PartScript.Data;
    public float RemainingFuel { get => (float) this._fuelTank.TotalFuel * this._fuelTank.FuelType.Density; }
    public bool SupportsWarpBurn { get; }

    private CraftFuelSource craftFuelSource;

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
      bool inFlightScene = Game.InFlightScene;
      UpdateCurrentPlanet();
      //当SOI变更时进行更新当前Planet的planetData
      Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
      

    }

    private void OnSoiChanged(IOrbitNode source)
    {
      UpdateCurrentPlanet();
      
    }
    void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
    {
      if (frame.DeltaTimeWorld == 0.0)
        return;
      
      if (UsingInternalOxygen())
      {
        double num1 = 1.0 / frame.DeltaTimeWorld;
        double num2 =_powerScale * (double)Data.OxygenComsumeRate * frame.DeltaTimeWorld;
        // 访问同一 part 的 FuelTankScript modifier 来消耗燃料
        try
        {
          Debug.LogFormat($"{craftFuelSource.FuelType.Name},{craftFuelSource.FuelType.Density}");
        }
        catch (Exception e)
        {
          Debug.LogFormat($"出事儿拉{e}");
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
    
    private void CreateInspectorModel(GroupModel model, bool flight)
    {
    }

    private void OnCraftFuelSourceChanged(object sender, EventArgs e)
    {
      
    }

    
    /// <summary>
    /// 检测当前环境是否可呼吸,可呼吸则返回false不消耗氧气;不可呼吸则返回true消耗内部氧气
    /// check if current environment is OK to breath,if so return false,drood does not consume oxygen brought;if not,then return trun,consuming oxygen brought
    /// </summary>
    /// <returns></returns>
    private bool UsingInternalOxygen()
    {
      float airDensity = this.PartScript.CraftScript.AtmosphereSample.AirDensity;
      if (airDensity!=0)
      {
        if (currentPlanetName.Contains("Droo")||currentPlanetName.Contains("Kerbin")||currentPlanetName.Contains("Earth")||currentPlanetName.Contains("Nebra")||currentPlanetName.Contains("Laythe")||currentPlanetName.Contains("Oord"))
        {
          return false;
        }
      }
      return true;
      
    }
    /// <summary>
    /// 更新当前星球的planetData
    /// </summary>
    private void UpdateCurrentPlanet()
    {
      bool inFlightScene = Game.InFlightScene;
      IPlanetData planetData = inFlightScene ? FlightSceneScript.Instance?.CraftNode?.Parent.PlanetData : (IPlanetData) null;//从IPlanetData接口实例化当前场景              
      currentPlanetName=planetData.Name;
    }

    
  }
  
  
}