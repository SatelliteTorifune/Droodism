using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Flight;
using Assets.Scripts.Tools.ObjectTransform;
using ModApi.Craft.Propulsion;
using ModApi.Planet;
using UnityEngine;
using ModApi.Flight.Sim;
using ModApi.Settings.Core;


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
    
    //private IFuelSource _fuelSourceFood;
    //private FuelTankScript _fuelTankFood;
    
    private IInputController _inputThrottle;
    private float _fuelRemoved;
    private string currentPlanetName;
    private IPlanetData planetData;
    private float _oxygenConsumeRate;
    
    
    private CraftFuelSource craftFuelSource;
    
    
    public override void OnModifiersCreated()
    {
      base.OnModifiersCreated();
      if(Game.InFlightScene)
      {
        RefreshFuelSource();
      }
        
    }
    
    
    private FuelTankScript FuelTank
    {
      get => this._fuelTank;
      set
      {
        if (this._fuelTank == value) 
          return;
        if (Game.InFlightScene && this._fuelTank != null)
          this._fuelTank.CraftFuelSourceChanged -= new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
        this._fuelTank = value;
        if (!Game.InFlightScene || this._fuelTank == null)
          return;
        this._fuelTank.CraftFuelSourceChanged += new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
      }
    }
    /*private FuelTankScript FuelTankFood
    {
      get => this._fuelTankFood;
      set
      {
        if (this._fuelTankFood == value) 
          return;
        if (Game.InFlightScene && this._fuelTankFood != null)
          this._fuelTankFood.CraftFuelSourceChanged -= new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
        this._fuelTankFood = value;
        if (!Game.InFlightScene || this._fuelTankFood == null)
          return;
        this._fuelTankFood.CraftFuelSourceChanged += new EventHandler<EventArgs>(this.OnCraftFuelSourceChanged);
      }
    }*/
    void IDesignerStart.DesignerStart(in DesignerFrameData frame)
    {
      base.OnInitialized();
    }
    
    private void AddTank(String fuelType,float FuelCapacity)
    {
      XElement element = new XElement("FuelTank");
      element.SetAttributeValue("capacity", FuelCapacity);
      element.SetAttributeValue("fuel", FuelCapacity);
      element.SetAttributeValue("fuelType", fuelType);
      element.SetAttributeValue("utilization", -1);
      element.SetAttributeValue("autoFuelType", false);
      element.SetAttributeValue("subPriority",-1);
      element.SetAttributeValue("partPropertiesEnabled", false);
      element.SetAttributeValue("staticPriceAndMass", false);
      var tankData= PartModifierData.CreateFromStateXml(element, Data.Part, 15) as FuelTankData;
      try
      {
        tankData.InspectorEnabled = true;
        tankData.SubPriority = -1;
        tankData.CreateScript();

      }
      catch (Exception e)
      {
        Debug.LogErrorFormat("{0}",e);
      }
    }
    
    void IFlightStart.FlightStart(in FlightFrameData frame)
    {
      bool inFlightScene = Game.InFlightScene;
      UpdateCurrentPlanet();
      //当SOI变更时进行更新当前Planet的planetData
      Game.Instance.FlightScene.CraftNode.ChangedSoI += OnSoiChanged;
      PartData partData = this.Data.Part;
      if (partData.Modifiers.Count<=6)
      {
        AddTank("Oxygen",100);
        Debug.LogFormat("创建Oxygen");
        //AddTank("Food",100);
        Debug.LogFormat("创建Food");
      }
      this.RefreshFuelSource();
    }

    
    void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
    {
      if (frame.DeltaTimeWorld == 0.0)
        return;
      //DamageDrood(_fuelSource,frame,0.1f);
      //DamageDrood(_fuelSourceFood,frame,0.1f);
      //_fuelSourceFood.RemoveFuel(frame.DeltaTimeWorld * Data.FoodComsumeRate);
      if (UsingInternalOxygen())
      {
        double num1 = frame.DeltaTimeWorld;
        double num2 =(double)Data.OxygenComsumeRate * frame.DeltaTimeWorld;
        _fuelSource.RemoveFuel(num2*num1);
        Debug.LogFormat($"Removed{num2*num1}");
        
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
    
    
    private void OnCraftFuelSourceChanged(object sender, EventArgs e)
    {
      this.RefreshFuelSource();
    }
    private void RefreshFuelSource()
    {
      Debug.LogFormat("调用RefreshFuelSource");
      foreach (var _modifier in this.PartScript.Modifiers)
      {
       
        if (_modifier.GetData().Name.Contains("FuelTank"))
        {
          
          this._fuelTank = _modifier as FuelTankScript;
          //this._fuelTankFood=_modifier as FuelTankScript;
          Debug.LogFormat("1");
          if (_fuelTank?.FuelType.Name=="Oxygen")
          {
            this._fuelSource = this.FuelTank?.CraftFuelSource;
            Debug.LogFormat("Oxygen");
          }
          
          //if (_fuelTankFood.FuelType.Name=="Food")
          //{
          //  this._fuelSourceFood = this.FuelTankFood?.CraftFuelSource;
          //}
        }
      }
      //this._fuelTank = GetComponent<FuelTankScript>();
      
      
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

    private void DamageDrood(IFuelSource _fuelSource,FlightFrameData frame,float DamageScale)
    {
      if (_fuelSource.IsEmpty&&(double) (float) (Setting<float>)Game.Instance.Settings.Game.Flight.ImpactDamageScale > 0.0)
      {
        this.PartScript.TakeDamage(DamageScale * Game.Instance.Settings.Game.Flight.ImpactDamageScale * frame.DeltaTime,PartDamageType.Basic);
        Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Drood(id:{this.PartScript.Data.Id}) is taking damage because running out of {_fuelSource.FuelType.Name}",false,2f);
      }
    }
  }
  
  
  
}