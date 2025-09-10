using RootMotion.FinalIK;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi;
using ModApi.Craft;
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

    public class HibernatingChamberScript : PartModifierScript<HibernatingChamberData>,IFlightUpdate,IFlightStart
    {
        private Transform LeftHandTransform,
            RightHandTransform,
            LeftElbowTransform,
            RightElbowTransform,
            BaseLKTransform;
        private FullBodyBipedIK ik;
        private CrewCompartmentScript _crewCompartment;
        private AttachPoint _seatAttachPoint;
        private IFuelSource _batterySource;

        private void UpdateComponents()
        {
            BaseLKTransform = Utilities.FindFirstGameObjectMyselfOrChildren("BodyBase", this.gameObject).transform;
            if (BaseLKTransform == null)
            {
                Debug.LogError("HibernatingChamberScript: Could not find BodyBase transform");
            }

            RightElbowTransform = BaseLKTransform.Find("RightElbow");
            RightHandTransform = RightElbowTransform.Find("RightHand");
            LeftElbowTransform = BaseLKTransform.Find("LeftElbow");
            LeftHandTransform = LeftElbowTransform.Find("LeftHand");

        }

        public override void OnModifiersCreated()
        {
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
            this._crewCompartment.SetCrewOrientation(this._seatAttachPoint.Position, this._seatAttachPoint.Rotation);
        }

        protected override void OnInitialized()
        {
            this._seatAttachPoint = this.PartScript.Data.GetAttachPoint("AttachPointSeat");
            UpdateComponents();
        }

        public void FlightStart(in FlightFrameData frameData)
        {
            _batterySource = this.PartScript.BatteryFuelSource;
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
        }

        public void FlightUpdate(in FlightFrameData frameData)
        {
            if (_crewCompartment?.Crew.Count==0||_batterySource==null)
            {
                return;
            }
            if (_batterySource.IsEmpty&&_crewCompartment?.Crew.Count!=0)
            {
                foreach (var crew in _crewCompartment.Crew)
                {
                    crew.PartScript.GetModifier<SupportLifeScript>().SetHibernating(false,this.PartScript.Data.PartType);
                }
            }

            if (!_batterySource.IsEmpty)
            {
                _batterySource.RemoveFuel(frameData.DeltaTimeWorld * Data.HibernationPowerConsumption); 
                foreach (var crew in _crewCompartment.Crew)
                {
                    crew.PartScript.GetModifier<SupportLifeScript>().SetHibernating(true,this.PartScript.Data.PartType);
                }
            }


            
            
        }
        private void KickCrew()
        {
            try
            {
                /*
                foreach (var crew in _crewCompartment?.Crew)
                {
                    _crewCompartment.UnloadCrewMember(crew,false);
                }*/
                _crewCompartment.UnloadCrewMember(_crewCompartment.Crew[0],false);

            }
            catch (Exception e)
            {
                Debug.LogError("你爸我找到你了操你妈的Error while unloading crew members: " + e);
            }
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            _batterySource = this.PartScript.BatteryFuelSource;
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
        }
                
    }
}