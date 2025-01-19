    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Assets.Scripts.Flight;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Program.Craft;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using ModApi.Settings.Core;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    

    public class LifeSupportScript : PartModifierScript<lifesupportData>,
    IFlightUpdate,
    IGameLoop
    {
        private IFuelSource _batterySource;
        private IFuelSource _fuelSource;
        private FuelTankScript _fuelTank;
        private FlightSceneScript FS;
        private float _powerConsumption;
        private bool inFlightScene;

        void IFlightUpdate.FlightUpdate(in ModApi.GameLoop.FlightFrameData frame)
        {
            Debug.Log("start--------");
            IFuelSource battery = this._batterySource;
            Debug.Log("1");
            int num2;
            num2 = (battery != null ? (battery.IsEmpty ? 1 : 0) : 1) == 0 ? 1 : 0;

            if(num2!=0)
            {
                Debug.Log("2");
                this._batterySource.RemoveFuel((double) this._powerConsumption * frame.DeltaTimeWorld);
                Debug.Log("3");
            }

            else if(this._batterySource != null)
            {
                this._batterySource.RemoveFuel((double) this._powerConsumption * frame.DeltaTimeWorld);
                Debug.Log("NotNull!");
            }

            else if(this._batterySource == null)
            {
                Debug.Log("Null!");
            }
            else
            {
                Debug.Log("empty");
            }
            Debug.Log("end----------");
            
        }

         public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            
            if(Game.InFlightScene)
            {
              this._fuelTank = ((Component) this).GetComponent<FuelTankScript>();
            }
        
        }

        private FuelTankScript FuelTank//这一坨直接抄的
        {
          get => this._fuelTank;
          set
          {
            if (this._fuelTank != value)
            {
              if (Game.InFlightScene && this._fuelTank != null)
              {
                this._fuelTank.CraftFuelSourceChanged -= this.OnCraftFuelSourceChanged;
              }

              this._fuelTank = value;
              if (Game.InFlightScene && this._fuelTank != null)
              {
                this._fuelTank.CraftFuelSourceChanged += this.OnCraftFuelSourceChanged;
              }
            }
          }
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
          {
            this._batterySource = this.PartScript.BatteryFuelSource;
            this.FuelTank = EngineUtilities.GetFuelTank(this.PartScript, this.Data.FuelSourceAttachPoint, this.Data.FuelType)?.Script;
            this.RefreshFuelSource();
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

    }
}