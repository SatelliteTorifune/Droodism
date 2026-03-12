using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class DroodDropperScript : PartModifierScript<DroodDropperData>,IFlightStart,IFlightUpdate
    {
        private CrewCompartmentScript  _crewCompartment { get; set; }
       
        public void FlightStart(in FlightFrameData frame)
        {
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
        }
        public override void OnModifiersCreated()
        {
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
            this._crewCompartment.CrewEnter += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotEnter);
            this._crewCompartment.CrewExit += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotExit);
        }

        private void OnPilotEnter(EvaScript crew)
        {
            
        }

        private void OnPilotExit(EvaScript crew)
        {
            
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            try
            {
                if (this.PartScript.Data.Activated)
                {
                    this.PartScript.Data.Activated = false;
                    _crewCompartment.UnloadCrewMember(_crewCompartment.Crew[0],false);
                }
            }
            catch (Exception e)
            {
                Mod.LOGError("wocaonima");
            }
            
        }
    }
}