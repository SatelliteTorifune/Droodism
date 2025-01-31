using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Math;
using ModApi.Scripts.State.Validation;
using ModApi.Ui.Inspector;
using System;
using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    

    public class LifeSupportScript : 
    PartModifierScript<LifeSupportData>,
    IFlightUpdate,
    IFlightStart
    {
       private IFuelSource _battery;
       private FuelTankScript _fuelTank;

       private double _fuelRemoved;

       void IFlightStart.FlightStart(in FlightFrameData frame)
       {
        
       }

       void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
       {
        if (frame.DeltaTimeWorld==0)
            return;

        if(this.PartScript.Data.Activated || !this.PartScript.Data.Config.SupportsActivation)
        {
            double num1 = 1.0 / frame.DeltaTimeWorld;
            double num2 = 0.1d;
            if(this._battery != null)
            {
                this._battery.RemoveFuel(num1 * num2);
                Debug.LogFormat("this isn't null,and the stuff is executing");
            }
            else if(this._battery == null)
            {
                Debug.LogFormat("this is null");
            }
            
        }
       }
    }
}