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
                if (isGround())
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
            return this.PartScript.CraftScript.FlightData.AltitudeAboveGroundLevel<1.5||PartScript.CraftScript.FlightData.Grounded||this.PartScript.CraftScript.FlightData.SurfaceVelocityMagnitude < 0.5;
        }
        public void FlightFixedUpdate(in FlightFrameData frame)
        {
            if (!isGround())
            {
                //UpdateParameters();
                UpdateInput(frame);
                UpdateForces(frame);
                
            }
        }
        private void UpdateInput(in FlightFrameData frame)
        {
            foreach (var eva in _crewCompartment.Crew)
                controls = eva.PartScript.CommandPod.Controls;
            Vector3 direction = new Vector3(Mathf.Clamp(PitchPID(controls.Pitch * targetMaxPitch * -1, (float)PartScript.CraftScript.FlightData.Pitch, 0.01f), -3, 3), controls.Roll*0.04f,  Mathf.Clamp(RollPID(controls.Roll*targetRoll, (float)PartScript.CraftScript.FlightData.BankAngle, 0.01f),-1,1));
            Vector3 torque = this.PartScript.CraftScript.CenterOfMass.TransformDirection(direction);
            _worldTorque = Vector3.Lerp(_worldTorque, torque, 2.5f * frame.DeltaTime);
            PartScript.BodyScript.RigidBody.AddTorque(_worldTorque * 10f, ForceMode.Force);
        }

        private void UpdateParameters()
        {
            SupportLifeScript supportLifeScript = _pilot.PartScript.GetModifier<SupportLifeScript>();
            kForward = supportLifeScript.a;
            kDrag = supportLifeScript.b;
            maxLiftForce = supportLifeScript.c;
            liftBaseCoeff = supportLifeScript.d;
            sideSlipDamping = supportLifeScript.e;
        }
        public float kForward = 10f;
        public float kDrag = 0.15f;
        public float maxLiftForce = 3f;
        public float liftBaseCoeff = 20f;   
        public float sideSlipDamping = 1.75f;
        public float maxSideForce = 3f;
        private void UpdateForces(in FlightFrameData frame)
        {
            
            Rigidbody rb = PartScript.BodyScript.RigidBody;
             if (rb == null) return;
             Vector3 worldVel = this.PartScript.CraftScript.FlightData.SurfaceVelocity.ToVector3();
             Vector3 forwardDir = rb.transform.forward;   // 机体前向（本地 +Z）
             Vector3 rightDir   = rb.transform.right;     // 机体右向（本地 +X）
             Vector3 upDir      = rb.transform.up;        // 机体上向（本地 +Y）
            
             float speed = worldVel.magnitude; 
             
             float aoaDeg = (float)PartScript.CraftScript.FlightData.AngleOfAttack;  
             float sideslip = (float)-PartScript.CraftScript.FlightData.SideSlip; 
            
             // -------------------------------------------------
             // 前进推力（由下沉产生的水平加速）——保持原逻辑
             // -------------------------------------------------
             float verticalSpeed = (float)PartScript.CraftScript.FlightData.VerticalSurfaceVelocity;
             if (verticalSpeed < -0.1f)
             {
                 // 把下沉的垂直分量转化为前进推力
                 float forwardForceMag = kForward * -verticalSpeed;
                 Vector3 forwardForce = forwardForceMag * forwardDir;
                 rb.AddForceAtPosition(forwardForce,PartScript.CraftScript.CenterOfMass.position, ForceMode.Force);
             }
            
             // -------------------------------------------------
             // 4基于 AOA 的升力
             // -------------------------------------------------
             float cl = liftCurve.Evaluate(Mathf.Abs(aoaDeg));
             float liftForceMag = liftBaseCoeff * cl * Data.Area * speed * speed;
             liftForceMag = Mathf.Min(liftForceMag, maxLiftForce);
             Vector3 liftDir = Vector3.Cross(forwardDir, rightDir).normalized; 
             if (Vector3.Dot(liftDir, upDir) > 0)
             {
                 liftDir = -liftDir;
             }
             Vector3 liftForce = liftForceMag * liftDir;
             rb.AddForceAtPosition(liftForce, PartScript.CraftScript.CenterOfMass.position, ForceMode.Force);
             
             
             if (Mathf.Abs(sideslip) > 1f) // 小于 1° 时可以忽略
             {
                 float sideForceMag = sideSlipDamping *
                                      Mathf.Sin(Mathf.Deg2Rad * sideslip) *
                                      speed * speed;
                 sideForceMag = Mathf.Clamp(sideForceMag, -maxSideForce, maxSideForce);
                 
                 Vector3 sideForce = -Mathf.Sign(sideslip) * Mathf.Abs(sideForceMag) * rightDir;
                 rb.AddForceAtPosition(sideForce, PartScript.CraftScript.CenterOfMass.position,ForceMode.Force);
             }
             
             if (worldVel.sqrMagnitude > 0.0001f)
             {
                 Vector3 dragForce = -kDrag * worldVel * worldVel.magnitude;
                 rb.AddForceAtPosition(dragForce, PartScript.CraftScript.CenterOfMass.position,ForceMode.Force);
             }
            
        }
        
        public AnimationCurve liftCurve = CreateDefaultLiftCurve();

        private static AnimationCurve CreateDefaultLiftCurve()
        {
            Keyframe[] keys = new Keyframe[]
            {
                new Keyframe( 0f, 0.15f),
                new Keyframe( 2f, 0.30f),
                new Keyframe( 5f, 0.70f),
                new Keyframe( 8f, 1.10f),
                new Keyframe(12f, 1.55f),
                new Keyframe(15f, 1.60f),
                new Keyframe(18f, 1.40f),
                new Keyframe(22f, 1.00f),
                new Keyframe(26f, 0.55f),
                new Keyframe(30f, 0.20f)
            };
            
            AnimationCurve curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0.5f);
            }
            curve.preWrapMode  = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            return curve;
        }
        #region RollPID
        
        private float targetRoll = 45;
        private float kpRoll = 0.75f;
        private float kiRoll=0.2f;
        private float kdRoll=0.65f;
        private float prevErrorRoll;
        private float interalRoll;
        private float RollPID(float targetRoll, float currentRoll, float deltaTime)
        {
            //Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.AutoPilot.
            float error = deltaTime * (currentRoll - targetRoll);
            interalRoll+=error*deltaTime;
            float derivative = (error - prevErrorRoll) / deltaTime;
            float output = kpRoll * error + kiRoll * interalRoll + kdRoll * derivative;
            prevErrorRoll = error;
            return output;
        }
        #endregion
        #region PitchPID
        
        private float targetMaxPitch = 45;
        private float kpPitch= 0.8f;
        private float kiPitch=0.01f;
        private float kdPitch=1.2f;
        private float prevErrorPitch;
        private float interPitch;
        private float PitchPID(float targetPitch, float currentPitch, float deltaTime)
        {
            //Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod.AutoPilot.
            float error = deltaTime * (currentPitch - targetPitch);
            interPitch+=error*deltaTime;
            float derivative = (error - prevErrorPitch) / deltaTime;
            float output = kpPitch * error + kiPitch * interPitch + kdPitch * derivative;
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
            var craftScript = this.PartScript.CraftScript as CraftScript;
            craftScript?.Data.Assembly.RemovePart(this.PartScript.Data);
            craftScript?.Data.Assembly.RemoveBody(this.PartScript.BodyScript.Data);
            this.PartScript.BodyScript.ExplodePart(this.PartScript, -1);
            
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