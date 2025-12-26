using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Craft;
using ModApi.GameLoop;
using RootMotion.FinalIK;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class LadderScript : PartModifierScript<LadderData>,IFlightStart,IFlightUpdate,IFlightFixedUpdate
    {
        private Transform LeftHandTransform,
            RightHandTransform,
            LeftElbowTransform,
            RightElbowTransform,
            BaseLKTransform;
        private FullBodyBipedIK _pilotIK;
        private EvaScript _pilot = (EvaScript) null;
        private CrewCompartmentScript _crewCompartment;
        private AttachPoint _seatAttachPoint;
        private CraftControls controls;
        
        
        public void FlightStart(in FlightFrameData frameData)
        {
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
        }

        public void FlightUpdate(in FlightFrameData frameData)
        {
            

        }
        public void FlightFixedUpdate(in FlightFrameData frameData)
        {
            LogData();
            if (_crewCompartment.Crew.Count == 0)
            {
                return;
            }

            MoveDrood();
        }

        private void MoveDrood()
        {
            controls = this._pilot.PartScript.CommandPod.Controls;
            BaseLKTransform.transform.localPosition=new Vector3(BaseLKTransform.transform.localPosition.x,BaseLKTransform.transform.localPosition.y+0.05f*controls.Pitch,BaseLKTransform.transform.localPosition.z);
            _crewCompartment.Data.CrewExitPosition=new Vector3(0, this.BaseLKTransform.transform.localPosition.y,_crewCompartment.Data.CrewExitPosition.z);
        }

        private void SetDroodPositionOnEnter(EvaScript crew)
        {
            var sb = crew.PartScript.CraftScript.FlightData.Position - this.PartScript.CraftScript.FlightData.Position;
            Mod.LOG("sb{0}",sb);
            var sb2 = Vector3d.Project(sb, this.PartScript.Transform.transform.localPosition);
            Mod.LOG("sb2{0}",sb2);
            BaseLKTransform.transform.localPosition = new Vector3(BaseLKTransform.transform.localPosition.x,(float)sb2.y,BaseLKTransform.transform.localPosition.z);
        }

        private void LogData()
        {
            Mod.LOG($"ladder BaseLKTransform.transform{this.BaseLKTransform.transform.localPosition}.出口{_crewCompartment.Data.CrewExitPosition}");
        }
        public override void OnModifiersCreated()
        {
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
            this._crewCompartment.SetCrewOrientation(this._seatAttachPoint.Position, this._seatAttachPoint.Rotation);
            this._crewCompartment.CrewEnter += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotEnter);
            this._crewCompartment.CrewExit += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotExit);
        }
        protected override void OnInitialized()
        {
            this._seatAttachPoint = this.PartScript.Data.GetAttachPoint("AttachPointSeat");
            UpdateComponents();
        }

        
        private void OnPilotEnter(EvaScript crew)
        {
            SetDroodPositionOnEnter(crew);
            this.SetPilot(crew);
            
        }

        /// <summary>Called when [pilot exit].</summary>
        /// <param name="crew">The crew.</param>
        private void OnPilotExit(EvaScript crew)
        {
            if (!((UnityEngine.Object) crew == (UnityEngine.Object) this._pilot))
                return;
            this.SetPilot((EvaScript) null);
            controls = null;
        }

        #region 可能是动画部分
        private void SetPilot(EvaScript pilot)
        {
            this._pilot = pilot;
            if ((UnityEngine.Object) pilot == (UnityEngine.Object) null)
            {
                this.SetIKEnabled(false);
                this._pilotIK = (FullBodyBipedIK) null;
            }
            else
            {
                
                this._pilotIK = this._pilot.GetComponentInChildren<FullBodyBipedIK>();
                this.SetIKEnabled(true);
                
            }
        }
    
        private void UpdateComponents()
        {
            BaseLKTransform = ModApi.Utilities.FindFirstGameObjectMyselfOrChildren("BodyBase", this.gameObject).transform;
            if (BaseLKTransform == null)
            {
                Debug.LogError("HibernatingChamberScript: Could not find BodyBase transform");
            }

            RightElbowTransform = BaseLKTransform.Find("RightElbow");
            RightHandTransform = RightElbowTransform.Find("RightHand");
            LeftElbowTransform = BaseLKTransform.Find("LeftElbow");
            LeftHandTransform = LeftElbowTransform.Find("LeftHand");
        }
        private void SetIKEnabled(bool enabled) {
      if (!((UnityEngine.Object) this._pilotIK != (UnityEngine.Object) null))
        return;
      if (enabled)
      {
          
          this._pilotIK.solver.bodyEffector.positionWeight = 1f;
          this._pilotIK.solver.bodyEffector.rotationWeight = 1f;
          this._pilotIK.solver.bodyEffector.target = BaseLKTransform;
          this._pilotIK.solver.bodyEffector.positionWeight = 1f;
          this._pilotIK.solver.bodyEffector.rotationWeight = 1f;
        this._pilotIK.solver.rightHandEffector.target = this.RightHandTransform;
        this._pilotIK.solver.rightHandEffector.positionWeight = 1f;
        this._pilotIK.solver.rightHandEffector.rotationWeight = 1f;
        this._pilotIK.solver.leftHandEffector.target = this.LeftHandTransform;
        this._pilotIK.solver.leftHandEffector.positionWeight = 1f;
        this._pilotIK.solver.leftHandEffector.rotationWeight = 1f;
        this._pilotIK.solver.leftArmChain.bendConstraint.bendGoal = this.LeftElbowTransform;
        this._pilotIK.solver.leftArmChain.bendConstraint.weight = 1f;
        this._pilotIK.solver.rightArmChain.bendConstraint.bendGoal = this.RightElbowTransform;
        this._pilotIK.solver.rightArmChain.bendConstraint.weight = 1f;
        this._pilotIK.solver.rightFootEffector.target = (Transform) null;
        this._pilotIK.solver.rightFootEffector.positionWeight = 0.0f;
        this._pilotIK.solver.rightFootEffector.rotationWeight = 0.0f;
        this._pilotIK.solver.leftFootEffector.target = (Transform) null;
        this._pilotIK.solver.leftFootEffector.positionWeight = 0.0f;
        this._pilotIK.solver.leftFootEffector.rotationWeight = 0.0f;
        
      }
      else
      {
        this._pilotIK.solver.rightHandEffector.target = (Transform) null;
        this._pilotIK.solver.rightHandEffector.positionWeight = 0.0f;
        this._pilotIK.solver.rightHandEffector.rotationWeight = 0.0f;
        this._pilotIK.solver.leftHandEffector.target = (Transform) null;
        this._pilotIK.solver.leftHandEffector.positionWeight = 0.0f;
        this._pilotIK.solver.leftHandEffector.rotationWeight = 0.0f;
        this._pilotIK.solver.leftArmChain.bendConstraint.bendGoal = (Transform) null;
        this._pilotIK.solver.leftArmChain.bendConstraint.weight = 0.0f;
        this._pilotIK.solver.rightArmChain.bendConstraint.bendGoal = (Transform) null;
        this._pilotIK.solver.rightArmChain.bendConstraint.weight = 0.0f;
        this._pilotIK.solver.rightFootEffector.target = (Transform) null;
        this._pilotIK.solver.rightFootEffector.positionWeight = 0.0f;
        this._pilotIK.solver.rightFootEffector.rotationWeight = 0.0f;
        this._pilotIK.solver.leftFootEffector.target = (Transform) null;
        this._pilotIK.solver.leftFootEffector.positionWeight = 0.0f;
        this._pilotIK.solver.leftFootEffector.rotationWeight = 0.0f;
        this._pilotIK.solver.bodyEffector.target = (Transform) null;
        this._pilotIK.solver.bodyEffector.positionWeight = 0.0f;
        this._pilotIK.solver.bodyEffector.rotationWeight = 0.0f;
        
      }
        }
        public override void OnPartDestroyed()
        {
            base.OnPartDestroyed();
            this.SetPilot((EvaScript) null);
        }
        #endregion
    }
    
}