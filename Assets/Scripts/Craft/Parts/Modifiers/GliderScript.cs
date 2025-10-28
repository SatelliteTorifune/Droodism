using System.Linq.Expressions;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Craft.Parts.Modifiers.Input;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts.Input;
using ModApi.GameLoop;
using RootMotion.FinalIK;
using ModApi.Flight.UI;

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
        private EvaScript _pilot = (EvaScript) null;
        private CrewCompartmentScript _crewCompartment;
        private AttachPoint _seatAttachPoint;

        private IInputController pitchInput;
        private CraftControls controls;
        public Vector3 _worldTorque;
        

        private bool isKill;
        private IFlightSceneUI ui;

        public void FlightStart(in FlightFrameData frame)
        {
            ui = Game.Instance.FlightScene.FlightSceneUI;
        }
        

        public void FlightUpdate(in FlightFrameData frame)
        {
            //Debug.LogFormat($"PartScript.CraftScript.ReferenceFrame.Center{PartScript.CraftScript.ReferenceFrame.Center},again{PartScript.CraftScript.Transform.position},pci{PartScript.CraftScript.FlightData.Position}");
            try
            {
                if (this.PartScript.CraftScript.FlightData.AltitudeAboveTerrain<5)
                {
                    foreach (var eva in _crewCompartment.Crew)
                    {
                        _crewCompartment.UnloadCrewMember(eva,true);
                    }
                }
                
                if (_crewCompartment.Crew.Count==0)
                {
                    this.PartScript.BodyScript.ExplodePart(this.PartScript, -1);
                }
            }
            catch (Exception e)
            {
                //since this shit is called every frame,so, when the part kills itself, it will throw an exception,so, i just ignore it.
            }
            
        }

        private bool isGround()
        {
            return this.PartScript.CraftScript.FlightData.AltitudeAboveGroundLevel<1.5||PartScript.CraftScript.FlightData.Grounded&&this.PartScript.CraftScript.FlightData.SurfaceVelocityMagnitude < 0.5;
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
            
            void UpdateIC(in FlightFrameData frame)
            {
                foreach (var eva in _crewCompartment.Crew)
                    controls = eva.PartScript.CommandPod.Controls;
                Vector3 direction =
                    new Vector3(
                        Mathf.Clamp(
                            PitchPID(controls.Pitch * targetMaxPitch * -1,
                                (float)PartScript.CraftScript.FlightData.Pitch, 0.01f), -1, 1), controls.Yaw, 0);// -Mathf.Clamp(MaxRollPID(controls.Roll*targetMaxRoll, (float)PartScript.CraftScript.FlightData.BankAngle, 0.01f),-1,1));
                Vector3 torque = this.PartScript.CraftScript.CenterOfMass.TransformDirection(direction);
                _worldTorque = Vector3.Lerp(_worldTorque, torque, 2.5f * frame.DeltaTime);
                PartScript.BodyScript.RigidBody.AddTorque(_worldTorque * 0.5f, ForceMode.Force);
            }
            SupportLifeScript supportLifeScript = this._pilot.PartScript.GetModifier<SupportLifeScript>();
            //kpPitch = supportLifeScript.kp;
            //kiPitch = supportLifeScript.ki;
            //kdPitch = supportLifeScript.kd;
            UpdateIC(frame);
            PartScript.BodyScript.RigidBody.AddForceAtPosition(this.PartScript.CraftScript.FlightData.CurrentMass*1.025f*PartScript.CraftScript.FlightData.GravityMagnitude*PartScript.CraftScript.FlightData.SurfaceVelocity.normalized.ToVector3()*-1,this.PartScript.CraftScript.CenterOfMass.position,ForceMode.Force);
            ui.ShowMessage($"FlightData.pitch{PartScript.CraftScript.FlightData.Pitch} ,输入:{PartScript.CraftScript.ActiveCommandPod.Controls.Pitch},输出:{ Mathf.Clamp(PitchPID(controls.Pitch * targetMaxPitch * -1, (float)PartScript.CraftScript.FlightData.Pitch, 0.01f), -1, 1)}");
            //ui.ShowMessage($"FlightData.roll{PartScript.CraftScript.FlightData.BankAngle} ,input:{PartScript.CraftScript.ActiveCommandPod.Controls.Roll} output:{MaxRollPID(controls.Roll*targetMaxRoll*-1, (float)PartScript.CraftScript.FlightData.BankAngle, 0.01f)}");
            
        }
        #region RollPID
        
        private float targetMaxRoll = 35;
        private float kpMaxRoll=0.8f;
        private float kiMaxRoll=0f;
        private float kdMaxRoll=0.4f;
        private float prevErrorMaxRoll;
        private float interalMaxRoll;
        private float MaxRollPID(float targetRoll, float currentRoll, float deltaTime)
        {
            //Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.AutoPilot.
            float error = deltaTime * (currentRoll - targetRoll);
            interalRoll+=error*deltaTime;
            float derivative = (error - prevErrorPitch) / deltaTime;
            float output = kpMaxRoll * error + kiMaxRoll * interalRoll + kdMaxRoll * derivative;
            prevErrorPitch = error;
            return output;
        }
        #endregion

        #region PitchPID
        
        private float targetMaxPitch = 30;
        private float kpPitch=0.8f;
        private float kiPitch=0f;
        private float kdPitch=0.4f;
        private float prevErrorPitch;
        private float interalRoll;
        private float PitchPID(float targetPitch, float currentPitch, float deltaTime)
        {
            //Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.AutoPilot.
            float error = deltaTime * (currentPitch - targetPitch);
            interalRoll+=error*deltaTime;
            float derivative = (error - prevErrorPitch) / deltaTime;
            float output = kpPitch * error + kiPitch * interalRoll + kdPitch * derivative;
            prevErrorPitch = error;
            return output;
        }
        #endregion
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
        private void OnPilotEnter(EvaScript crew) => this.SetPilot(crew);
        private void OnPilotExit(EvaScript crew)
        {
            if (!((UnityEngine.Object) crew == (UnityEngine.Object) this._pilot))
                return;
            this.SetPilot((EvaScript) null);
        }
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