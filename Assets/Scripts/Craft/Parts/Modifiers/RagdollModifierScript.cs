using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Ui.Inspector;
using RootMotion.FinalIK;
using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    /// <summary>
    /// Ragdoll using IK to drive bones, with physics targets as IK effector goals.
    /// </summary>
    public class RagdollModifierScript : PartModifierScript<RagdollModifierData>, 
        IFlightStart,
        IFlightUpdate,
        IFlightUpdatePaused,
        IFlightFixedUpdate
    {
        #region Fields

        private CrewCompartmentScript _crewCompartment;
        private FullBodyBipedIK _pilotIK;
        private EvaScript _evaScript;
        private Transform _characterCollider;
        private Transform _hipsBone;
        
        private bool _isRagdollActive = false;
        private bool _wasPaused = false;
        private Transform _savedBodyTarget;

        #endregion

        #region Nested Types

        private struct IKSavedWeights
        {
            public float RightHandPos, RightHandRot;
            public float LeftHandPos, LeftHandRot;
            public float RightFootPos, RightFootRot;
            public float LeftFootPos, LeftFootRot;
            public float BodyPos;
        }

        private class PhysicsTarget
        {
            public GameObject Target;
            public Rigidbody Rb;
            public IKEffector Effector;
        }

        #endregion

        #region Physics Targets

        private PhysicsTarget _rightHand;
        private PhysicsTarget _leftHand;
        private PhysicsTarget _rightFoot;
        private PhysicsTarget _leftFoot;
        private PhysicsTarget _hips;
        private readonly List<PhysicsTarget> _allTargets = new();

        private IKSavedWeights _savedWeights;

        #endregion

        #region Inspector

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            
            var group = new GroupModel("Ragdoll");
            model.AddGroup(group);
            
            group.Add(new TextButtonModel("启用", b => EnableRagdollMode()));
            group.Add(new TextButtonModel("禁用", b => DisableRagdollMode()));
        }

        #endregion

        #region Properties

        public bool IsRagdollActive => _isRagdollActive;

        #endregion

        #region Part Modifier Events

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _isRagdollActive = Data.EnableRagdoll;
        }

        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
            if (_crewCompartment != null)
            {
                _crewCompartment.CrewEnter += OnCrewEnter;
                _crewCompartment.CrewExit += OnCrewExit;
            }
        }

        #endregion

        #region Crew Management

        private void OnCrewEnter(EvaScript crew)
        {
            _evaScript = crew;
            _pilotIK = crew.GetComponentInChildren<FullBodyBipedIK>();
            
            if (_isRagdollActive)
                EnableRagdollIK();
        }

        private void OnCrewExit(EvaScript crew)
        {
            if (!_isRagdollActive)
            {
                _evaScript = null;
                _pilotIK = null;
            }
        }

        #endregion

        #region Public API

        public void EnableRagdollMode()
        {
            if (_isRagdollActive) return;
            
            if (_evaScript == null || _pilotIK == null)
                FindCrew();
            
            _isRagdollActive = true;
            Data.EnableRagdoll = true;
            
            if (_evaScript != null && _pilotIK != null)
                EnableRagdollIK();
        }

        public void DisableRagdollMode()
        {
            if (!_isRagdollActive) return;
            
            _isRagdollActive = false;
            Data.EnableRagdoll = false;
            
            DisableRagdollIK();
        }

        public void SetRagdollMode(bool enable)
        {
            if (enable)
                EnableRagdollMode();
            else
                DisableRagdollMode();
        }

        private void FindCrew()
        {
            if (_crewCompartment?.Crew.Count > 0)
            {
                _evaScript = _crewCompartment.Crew[0];
            }
            else
            {
                var cc = PartScript.GetModifier<CrewCompartmentScript>();
                _evaScript = cc?.Crew[0] ?? PartScript.GetModifier<EvaScript>();
            }
            
            _pilotIK = _evaScript?.GetComponentInChildren<FullBodyBipedIK>();
        }

        #endregion

        #region Ragdoll IK

        private void EnableRagdollIK()
        {
            if (_pilotIK == null) return;
            
            SaveWeights();
            CreatePhysicsTargets();
            BindIK();
        }

        private void DisableRagdollIK()
        {
            RestoreWeights();
            DestroyTargets();
        }

        private void CreatePhysicsTargets()
        {
            _allTargets.Clear();
            
            var solver = _pilotIK.solver;
            
            // Find CharacterCollider (main collision box) and Hips bone
            _characterCollider = _evaScript.transform.Find("CharacterCollider");
            _hipsBone = _evaScript.transform.Find("Root/Offset/Hips");
            
            // Hips (Body) - IMPORTANT: drives the root/collision box
            _hips = CreateTarget("Phys_Hips", solver.bodyEffector.bone.position, 5f);
            _hips.Effector = solver.bodyEffector;
            _allTargets.Add(_hips);
            
            // Right Hand
            _rightHand = CreateTarget("Phys_RHand", solver.rightHandEffector.bone.position, 0.5f);
            _rightHand.Effector = solver.rightHandEffector;
            _allTargets.Add(_rightHand);
            
            // Left Hand
            _leftHand = CreateTarget("Phys_LHand", solver.leftHandEffector.bone.position, 0.5f);
            _leftHand.Effector = solver.leftHandEffector;
            _allTargets.Add(_leftHand);
            
            // Right Foot
            _rightFoot = CreateTarget("Phys_RFoot", solver.rightFootEffector.bone.position, 1f);
            _rightFoot.Effector = solver.rightFootEffector;
            _allTargets.Add(_rightFoot);
            
            // Left Foot
            _leftFoot = CreateTarget("Phys_LFoot", solver.leftFootEffector.bone.position, 1f);
            _leftFoot.Effector = solver.leftFootEffector;
            _allTargets.Add(_leftFoot);
        }

        private PhysicsTarget CreateTarget(string name, Vector3 worldPos, float mass)
        {
            var target = new PhysicsTarget
            {
                Target = new GameObject(name)
            };
            
            target.Target.transform.SetParent(_evaScript.transform);
            target.Target.transform.position = worldPos;  // Start at bone's world position
            target.Target.transform.rotation = Quaternion.identity;
            
            target.Rb = target.Target.AddComponent<Rigidbody>();
            target.Rb.mass = mass;
            target.Rb.useGravity = true;
            target.Rb.drag = 3f;
            target.Rb.angularDrag = 5f;
            target.Rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Add collider to prevent clipping
            var col = target.Target.AddComponent<SphereCollider>();
            col.radius = 0.15f;
            
            return target;
        }

        private void BindIK()
        {
            if (_pilotIK == null) return;
            
            // Disable animator and character controller
            var animators = _evaScript.GetComponentsInChildren<Animator>();
            foreach (var a in animators)
                a.enabled = false;
            
            // Disable character controller if exists
            var cc = _evaScript.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;
            
            // Bind physics targets to IK effectors
            foreach (var t in _allTargets)
            {
                if (t.Effector != null)
                {
                    t.Effector.target = t.Target.transform;
                    t.Effector.positionWeight = 1f;
                    t.Effector.rotationWeight = 0.5f;
                }
            }
        }

        private void SaveWeights()
        {
            if (_pilotIK == null) return;
            
            var s = _pilotIK.solver;
            _savedWeights = new IKSavedWeights
            {
                RightHandPos = s.rightHandEffector.positionWeight,
                RightHandRot = s.rightHandEffector.rotationWeight,
                LeftHandPos = s.leftHandEffector.positionWeight,
                LeftHandRot = s.leftHandEffector.rotationWeight,
                RightFootPos = s.rightFootEffector.positionWeight,
                RightFootRot = s.rightFootEffector.rotationWeight,
                LeftFootPos = s.leftFootEffector.positionWeight,
                LeftFootRot = s.leftFootEffector.rotationWeight,
                BodyPos = s.bodyEffector.positionWeight
            };
            
            // Also save effector target references
            _savedBodyTarget = s.bodyEffector.target;
        }

        private void RestoreWeights()
        {
            if (_pilotIK == null) return;
            
            var s = _pilotIK.solver;
            s.rightHandEffector.positionWeight = _savedWeights.RightHandPos;
            s.rightHandEffector.rotationWeight = _savedWeights.RightHandRot;
            s.leftHandEffector.positionWeight = _savedWeights.LeftHandPos;
            s.leftHandEffector.rotationWeight = _savedWeights.LeftHandRot;
            s.rightFootEffector.positionWeight = _savedWeights.RightFootPos;
            s.rightFootEffector.rotationWeight = _savedWeights.RightFootRot;
            s.leftFootEffector.positionWeight = _savedWeights.LeftFootPos;
            s.leftFootEffector.rotationWeight = _savedWeights.LeftFootRot;
            s.bodyEffector.positionWeight = _savedWeights.BodyPos;
            s.bodyEffector.target = _savedBodyTarget;
            
            // Restore animator
            var animators = _evaScript?.GetComponentsInChildren<Animator>();
            if (animators != null)
                foreach (var a in animators)
                    a.enabled = true;
            
            // Restore character controller
            var cc = _evaScript?.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = true;
        }

        private void DestroyTargets()
        {
            foreach (var t in _allTargets)
            {
                if (t?.Target != null)
                    UnityEngine.Object.Destroy(t.Target);
            }
            _allTargets.Clear();
            
            _hips = _rightHand = _leftHand = _rightFoot = _leftFoot = null;
        }

        #endregion

        #region Flight Loop

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _evaScript = PartScript.GetModifier<EvaScript>();
            _pilotIK = _evaScript?.GetComponentInChildren<FullBodyBipedIK>();
            
            if (_isRagdollActive && _evaScript != null)
                EnableRagdollIK();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!_isRagdollActive || _evaScript == null) return;
            
            var animators = _evaScript.GetComponentsInChildren<Animator>();
            foreach (var a in animators)
            { 
                a.enabled = !IsRagdollActive;
            }
            
            
            // Sync CharacterCollider and Hips to physics body
            if (_hips?.Target != null && _hipsBone != null&&IsRagdollActive)
            {
                _hipsBone.position = _hips.Target.transform.position;
                _hipsBone.rotation = _hips.Target.transform.rotation;
                
                if (_characterCollider != null)
                {
                    _characterCollider.position = _hips.Target.transform.position;
                    _characterCollider.rotation = _hips.Target.transform.rotation;
                }
            }
        }

        void IFlightUpdatePaused.FlightUpdatePaused(in FlightFrameData frame)
        {
            if (_evaScript == null || !_isRagdollActive) return;
            var animators = _evaScript.GetComponentsInChildren<Animator>();
            foreach (var a in animators)
            { 
                a.enabled = !IsRagdollActive;
            }
            if (!_wasPaused)
            {
                _wasPaused = true;
                // Stop all physics when paused
                foreach (var t in _allTargets)
                    t?.Rb?.Sleep();
            }
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            var animators = _evaScript.GetComponentsInChildren<Animator>();
            foreach (var a in animators)
            { 
                a.enabled = !IsRagdollActive;
            }
            if (_wasPaused && _isRagdollActive)
            {
                _wasPaused = false;
                // Wake up physics when unpaused
                foreach (var t in _allTargets)
                    t?.Rb?.WakeUp();
            }
        }

        #endregion
    }
}
