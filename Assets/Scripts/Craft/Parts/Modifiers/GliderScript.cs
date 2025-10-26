using System.Linq.Expressions;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Craft.Parts.Modifiers.Input;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts.Input;
using ModApi.GameLoop;
using ModApi.Input;
using RootMotion.FinalIK;
using UnityEditor;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class GliderScript : PartModifierScript<GliderData>,IFlightUpdate,IFlightStart,IFlightFixedUpdate
    {
        private Transform 
            LeftHandTransform,
            RightHandTransform,
            LeftElbowTransform,
            RightElbowTransform,
            BaseLKTransform;
        private FullBodyBipedIK _pilotIK;
        private EvaScript currentEvaScript = (EvaScript) null;
        private CrewCompartmentScript _crewCompartment;
        private AttachPoint _seatAttachPoint;

        private CraftControls controls;
        private Rigidbody chuteRigidBody;
        
        float stallAngle = 45f;
        private bool isKill;

        public Vector3 _worldTorque;

        

        public void FlightStart(in FlightFrameData frame)
        {
            
        }
        private bool isGround()
        {
            return this.PartScript.CraftScript.FlightData.AltitudeAboveGroundLevel<1.5||PartScript.CraftScript.FlightData.Grounded&&this.PartScript.CraftScript.FlightData.SurfaceVelocityMagnitude < 0.5;
        }
        public void FlightUpdate(in FlightFrameData frame)
        {
            try
            {
                if (isGround())
                {
                    foreach (var eva in _crewCompartment.Crew)
                    {
                        if (_crewCompartment.Crew != null)
                        {
                            eva.CrewCompartment.UnloadCrewMember(eva, true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //since this shit is called every frame,so, when the part kills itself, it will throw an exception,so, i just ignore it.
            }
        }

        public void FlightFixedUpdate(in FlightFrameData frame)
        {
           
            if (!isGround())
            {
                WorkingLogic(frame);
            } 
        }

        private void WorkingLogic(in FlightFrameData frame)
        { 
            void UpdateIC()
            {
                foreach (var eva in _crewCompartment.Crew)
                {
                    controls = eva.PartScript.CommandPod.Controls;
                }
            }
           UpdateIC();
               Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{this.PartScript.CraftScript.FlightData.AngleOfAttack} bankAngle{this.PartScript.CraftScript.FlightData.BankAngle}");
           //UpdateFullyDeployedParachuteMovement(controls.Pitch, controls.Roll, this.PartScript.BodyScript.RigidBody);
           Vector3 direction = new Vector3(controls.Pitch, controls.Yaw,-controls.Roll);
           direction.Scale(Vector3.one);
           Vector3 b = this.PartScript.CraftScript.CenterOfMass.TransformDirection(direction);
           this._worldTorque = 0f <= 0.0 ? Vector3.Lerp(this._worldTorque, b, 2.5f * frame.DeltaTime) : b;

           
           this.PartScript.BodyScript.RigidBody.AddTorque(_worldTorque*20f,ForceMode.Force);
           
          this.PartScript.BodyScript.RigidBody.AddForce(GetDragMangitude(frame),ForceMode.Force);
           //this.PartScript.BodyScript.RigidBody.AddForceAtPosition(PartScript.CraftScript.FlightData.CurrentMass * PartScript.CraftScript.FlightData.GravityMagnitude *1.025f*PartScript.CraftScript.FlightData.SurfaceVelocity.normalized.ToVector3()*-1, PartScript.Transform.position);
           _worldTorque = Vector3.zero;

           Vector3 GetDragMangitude(in FlightFrameData frame)
           {
               float airDensity = this.PartScript.CraftScript.AtmosphereSample.AirDensity;
               float surfaceVelocityMagnitude = (float)this.PartScript.CraftScript.FlightData.SurfaceVelocityMagnitude;
               Vector3 dragDirection = PartScript.BodyScript.Transform.up;//-PartScript.CraftScript.FlightData.SurfaceVelocity.normalized.ToVector3();
               return dragDirection *Mathf.MoveTowards(0f,2f * airDensity * surfaceVelocityMagnitude,1e3f);
           }
           

        }
        private Quaternion GetFullyDeployedCanopyRotation()
        {
            return Quaternion.LookRotation(this.currentEvaScript.transform.up, -this.currentEvaScript.transform.forward);
        }
        
        #region 傻逼
        protected override void OnInitialized()
        {
            this._seatAttachPoint = this.PartScript.Data.GetAttachPoint("AttachPointSeat");
            UpdateComponents();
        }
        private void UpdateComponents()
        {
            
            BaseLKTransform = Utilities.FindFirstGameObjectMyselfOrChildren("BodyBase", this.gameObject).transform;
            if (BaseLKTransform == null)
            {
                Debug.LogError("GliderScript: Could not find BodyBase transform");
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
            this._crewCompartment.CrewEnter += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotEnter);
            this._crewCompartment.CrewExit += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotExit);
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
        }

        private void OnPilotEnter(EvaScript crew)
        {
            this.SetPilot(crew);
            //Deploy();
        }

        

        private void OnPilotExit(EvaScript crew)
        {
            if (!((UnityEngine.Object) crew == (UnityEngine.Object) this.currentEvaScript))
                return;
            var craftScript = this.PartScript.CraftScript as CraftScript;
            craftScript.Data.Assembly.RemovePart(this.PartScript.Data);
            this.SetPilot((EvaScript) null);
            this.PartScript.BodyScript.ExplodePart(this.PartScript, -1);
        }
        private void SetPilot(EvaScript pilot)
        {
            this.currentEvaScript = pilot;
            if ((UnityEngine.Object) pilot == (UnityEngine.Object) null)
            {
                this.SetIKEnabled(false);
                this._pilotIK = (FullBodyBipedIK) null;
            }
            else
            {
                this._pilotIK = this.currentEvaScript.GetComponentInChildren<FullBodyBipedIK>();
                this.SetIKEnabled(true);
                
            }
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