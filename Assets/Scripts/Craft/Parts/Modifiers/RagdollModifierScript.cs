using System;
using System.Collections.Generic;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using ModApi.Ui.Inspector;
using RootMotion.FinalIK;
using System.Linq;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class RagdollModifierScript : PartModifierScript<RagdollModifierData>, 
        IFlightUpdate,
        IFlightFixedUpdate
    {
        private CrewCompartmentScript? _crewCompartment;
        private FullBodyBipedIK? _pilotIK;
        private EvaScript? _evaScript;
        
        private bool _isRagdollActive = false;
        private float _ragdollBlendWeight = 0f;
        
        private IKSavedWeights _savedWeights = new();
        
        // Store original collision modes for restore
        private class RigidbodyState
        {
            public Rigidbody rb;
            public bool isKinematic;
            public bool useGravity;
            public CollisionDetectionMode collisionDetectionMode;
        }
        private List<RigidbodyState> _originalRigidbodyStates = new();

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            
            GroupModel groupModel = new GroupModel("Ragdoll");
            model.AddGroup(groupModel);
            
            groupModel.Add<TextModel>(new TextModel("Status", () => _isRagdollActive ? "Active" : "Inactive"));
            
            // Debug button to toggle ragdoll
            TextButtonModel ragdollButton = new TextButtonModel(
                _isRagdollActive ? "Disable Ragdoll" : "Enable Ragdoll",
                b => 
                {
                    Mod.Log($"Inspector button clicked: current={_isRagdollActive}");
                    SetRagdollMode(!_isRagdollActive);
                }
            );
            groupModel.Add<TextButtonModel>(ragdollButton);
            
            ToggleModel toggleModel = new ToggleModel(
                "Enable Ragdoll (Data)",
                () => Data.EnableRagdoll,
                x => 
                {
                    Data.EnableRagdoll = x;
                    Mod.Log($"Data.EnableRagdoll set to: {x}");
                }
            );
            groupModel.Add<ToggleModel>(toggleModel);
        }

        public bool IsRagdollActive => _isRagdollActive;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _ragdollBlendWeight = Data.EnableRagdoll ? 1f : 0f;
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

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            
            if (_crewCompartment != null)
            {
                _crewCompartment.CrewEnter -= OnCrewEnter;
                _crewCompartment.CrewExit -= OnCrewExit;
                _crewCompartment.CrewEnter += OnCrewEnter;
                _crewCompartment.CrewExit += OnCrewExit;
            }
            
            RefreshPilotReferences();
        }

        private void OnCrewEnter(EvaScript crew)
        {
            RefreshPilotReferencesFromCrew(crew);
        }

        private void OnCrewExit(EvaScript crew)
        {
            ClearPilotReferences();
        }

        private void RefreshPilotReferences()
        {
            if (_crewCompartment == null) return;
            
            foreach (var crew in _crewCompartment.Crew)
            {
                RefreshPilotReferencesFromCrew(crew);
            }
        }

        private void RefreshPilotReferencesFromCrew(EvaScript crew)
        {
            _evaScript = crew;
            _pilotIK = crew.GetComponentInChildren<FullBodyBipedIK>();
            
            if (_pilotIK == null) return;

            if (_isRagdollActive)
            {
                ApplyRagdollPhysics();
            }
        }

        private void ClearPilotReferences()
        {
            _evaScript = null;
            _pilotIK = null;
        }

        public void SetRagdollMode(bool enable)
        {
            Mod.Log($"SetRagdollMode called: enable={enable}, current={_isRagdollActive}");
            
            if (_isRagdollActive == enable) 
            {
                Mod.Log("SetRagdollMode: early exit (same state)");
                return;
            }
            
            _isRagdollActive = enable;
            Data.EnableRagdoll = enable;
            
            Mod.Log($"SetRagdollMode: _pilotIK={_pilotIK != null}, _evaScript={_evaScript != null}");
            
            if (enable)
            {
                SaveIKWeights();
                ApplyRagdollPhysics();
            }
            else
            {
                RestoreEvaScriptIK();
            }
        }

        private void ApplyRagdollPhysics()
        {
            if (_pilotIK == null) return;

            SaveIKWeights();
            
            var solver = _pilotIK.solver;
            
            solver.rightHandEffector.positionWeight = 0f;
            solver.rightHandEffector.rotationWeight = 0f;
            solver.leftHandEffector.positionWeight = 0f;
            solver.leftHandEffector.rotationWeight = 0f;
            solver.rightFootEffector.positionWeight = 0f;
            solver.rightFootEffector.rotationWeight = 0f;
            solver.leftFootEffector.positionWeight = 0f;
            solver.leftFootEffector.rotationWeight = 0f;
            
            _pilotIK.enabled = false;
            
            if (_evaScript != null)
            {
                var animator = _evaScript.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }
                
                // Store original states and configure for ragdoll
                _originalRigidbodyStates.Clear();
                
                var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in evaRigidbodies)
                {
                    // Store original state
                    _originalRigidbodyStates.Add(new RigidbodyState
                    {
                        rb = rb,
                        isKinematic = rb.isKinematic,
                        useGravity = rb.useGravity,
                        collisionDetectionMode = rb.collisionDetectionMode
                    });
                    
                    // Configure for ragdoll physics
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
            }
        }

        private void RestoreEvaScriptIK()
        {
            if (_pilotIK == null) return;

            _pilotIK.enabled = true;
            
            var solver = _pilotIK.solver;
            
            solver.rightHandEffector.positionWeight = _savedWeights.RightHandPositionWeight;
            solver.rightHandEffector.rotationWeight = _savedWeights.RightHandRotationWeight;
            solver.leftHandEffector.positionWeight = _savedWeights.LeftHandPositionWeight;
            solver.leftHandEffector.rotationWeight = _savedWeights.LeftHandRotationWeight;
            solver.rightFootEffector.positionWeight = _savedWeights.RightFootPositionWeight;
            solver.rightFootEffector.rotationWeight = _savedWeights.RightFootRotationWeight;
            solver.leftFootEffector.positionWeight = _savedWeights.LeftFootPositionWeight;
            solver.leftFootEffector.rotationWeight = _savedWeights.LeftFootRotationWeight;
            
            if (_evaScript != null)
            {
                var animator = _evaScript.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = true;
                }
                
                // Restore original states
                foreach (var state in _originalRigidbodyStates)
                {
                    if (state.rb != null)
                    {
                        state.rb.isKinematic = state.isKinematic;
                        state.rb.useGravity = state.useGravity;
                        state.rb.collisionDetectionMode = state.collisionDetectionMode;
                    }
                }
                _originalRigidbodyStates.Clear();
            }
        }

        private void SaveIKWeights()
        {
            if (_pilotIK == null) return;

            var solver = _pilotIK.solver;
            _savedWeights = new IKSavedWeights
            {
                RightHandPositionWeight = solver.rightHandEffector.positionWeight,
                RightHandRotationWeight = solver.rightHandEffector.rotationWeight,
                LeftHandPositionWeight = solver.leftHandEffector.positionWeight,
                LeftHandRotationWeight = solver.leftHandEffector.rotationWeight,
                RightFootPositionWeight = solver.rightFootEffector.positionWeight,
                RightFootRotationWeight = solver.rightFootEffector.rotationWeight,
                LeftFootPositionWeight = solver.leftFootEffector.positionWeight,
                LeftFootRotationWeight = solver.leftFootEffector.rotationWeight
            };
        }

       public void FlightUpdate(in FlightFrameData frame)
        {
            // Auto-enable ragdoll when in flight scene and ragdoll is enabled in data
            // but not yet active
            if (Game.InFlightScene && Data.EnableRagdoll && !_isRagdollActive)
            {
                Mod.Log("Auto-enabling ragdoll from FlightUpdate");
                SetRagdollMode(true);
            }
        }
        

        public void FlightFixedUpdate(in FlightFrameData frame)
        {
            // Maintain ragdoll physics state during fixed update
            // Only needed if EvaScript's patch didn't work
            if (_isRagdollActive && _evaScript != null)
            {
                var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in evaRigidbodies)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
        }

        private struct IKSavedWeights
        {
            public float RightHandPositionWeight;
            public float RightHandRotationWeight;
            public float LeftHandPositionWeight;
            public float LeftHandRotationWeight;
            public float RightFootPositionWeight;
            public float RightFootRotationWeight;
            public float LeftFootPositionWeight;
            public float LeftFootRotationWeight;
        }
    }
}
