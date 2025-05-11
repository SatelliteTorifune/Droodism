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
    public class SupportLifeScript : PartModifierScript<SupportLifeData>,IFlightUpdate
    {
        private IFuelSource _fuelSource;
        void IFlightUpdate.FlightUpdate(in FlightFrameData frameData)
        {
            
            if (frameData.DeltaTimeWorld==0.0)
            {
                return;
            }

            this._fuelSource.RemoveFuel(10);
        }
    }
}