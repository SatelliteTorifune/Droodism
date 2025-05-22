using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Flight;
using ModApi.Craft.Propulsion;
using ModApi.Planet;
using UnityEngine;
using ModApi.Flight.Sim;

#nullable disable
namespace Assets.Scripts.Craft.Parts.Modifiers
{
  public class SupportLifeScript : 
    PartModifierScript<SupportLifeData>,
    IDesignerStart,
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
    
    
    private CraftFuelSource craftFuelSource;
    
    
    public override void OnModifiersCreated()
    {
      base.OnModifiersCreated();
      if(Game.InFlightScene)
      {
        this._fuelTank = ((Component) this).GetComponent<FuelTankScript>();
      }
        
    }
    
    private void AddTank(FuelType fuelType)
    {
      XElement element = new XElement("FuelTank");
      element.SetAttributeValue("capacity", 10);
      element.SetAttributeValue("fuel", 10);
      element.SetAttributeValue("fuelType", fuelType.Id);
      element.SetAttributeValue("utilization", 1);
      element.SetAttributeValue("autoFuelType", false);
      element.SetAttributeValue("partPropertiesEnabled", false);
      var tankData= PartModifierData.CreateFromStateXml(element, Data.Part, 15) as FuelTankData;
      try
      {
        tankData.CreateScript();
      }
      catch (Exception e)
      {
        Debug.LogErrorFormat("{0}",e);
      }
      
      
    }
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
      
      base.OnInitialized();
      PartData partData = this.Data.Part;
      if (partData.Modifiers.Count<=6)
      {
        AddTank(FuelType.Battery);
      }

    }

    void IFlightStart.FlightStart(in FlightFrameData frame)
    {
      this._powerScale = this.Data.OxygenComsumeRate;
      bool inFlightScene = Game.InFlightScene;
      UpdateCurrentPlanet();
      //当SOI变更时进行更新当前Planet的planetData
      Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
      

    }

    
    void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
    {
      if (frame.DeltaTimeWorld == 0.0)
        return;
      Debug.LogFormat("{0}",UsingInternalOxygen());
      if (UsingInternalOxygen())
      {
        double num1 = 1.0 / frame.DeltaTimeWorld;
        double num2 =_powerScale * (double)Data.OxygenComsumeRate * frame.DeltaTimeWorld;
        
        
      }
      
    }

    public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
    {
      this.OnCraftStructureChanged(craftScript);
      
    }

    public override void OnCraftStructureChanged(ICraftScript craftScript)
    {
      this.FuelTank = EngineUtilities.GetFuelTank(this.PartScript, this.Data.FuelSourceAttachPoint, this.Data.FuelType)?.Script;
      this.RefreshFuelSource();
    }
    

    public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
    {
      // 移除缩放、拉伸等逻辑
    }
    
    private void OnCraftFuelSourceChanged(object sender, EventArgs e)
    {
      
      this.RefreshFuelSource();
    }
    private void RefreshFuelSource()
    {
      if (this.Data.FuelType == this.FuelTank?.CraftFuelSource?.FuelType)
      {
        this._fuelSource = this.FuelTank?.CraftFuelSource;
      }
      else if (this.Data.FuelType != null)
      {
        this._fuelSource = EmptyFuelSource.GetOrCreate(this.Data.FuelType);
      }
    } 
    
    /// <summary>
    /// 检测当前环境是否可呼吸,可呼吸则返回false不消耗氧气;不可呼吸则返回true消耗内部氧气
    /// check if current environment is OK to breath,if so return false,drood does not consume oxygen brought;if not,then return trun,consuming oxygen brought
    /// </summary>
    /// <returns></returns>
    private bool UsingInternalOxygen()
    {
      //当前空气密度
      float airDensity = PartScript.CraftScript.AtmosphereSample.AirDensity;
      //若空气密度不为零(即当前环境存在大气),则检测当前星球是否存在可呼吸气体
      if (airDensity!=0)
      {
        if (currentPlanetName.Contains("Droo")||currentPlanetName.Contains("Kerbin")||currentPlanetName.Contains("Earth")||currentPlanetName.Contains("Nebra")||currentPlanetName.Contains("Laythe")||currentPlanetName.Contains("Oord"))
        {
          return false;
        }

        return true;
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
    /// <summary>
    /// SOi改变时调用UpdateCurrentPlanet方法更新当前星球的planetData
    /// </summary>
    private void OnSoiChanged(IOrbitNode source)
    {
      UpdateCurrentPlanet();
    }
    
  }
  
  
  
}