using System;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using RootMotion.FinalIK;
using System.Linq;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    public class RagdollModifierScript : PartModifierScript<RagdollModifierData>, 
        IFlightUpdate
    {
        private CrewCompartmentScript? _crewCompartment;
        private FullBodyBipedIK? _pilotIK;
        private EvaScript? _evaScript;
        
        private bool _isRagdollActive = false;
        private float _ragdollBlendWeight = 0f;
        private bool _isTransitioning = false;
        
        private IKSavedWeights _savedWeights = new();

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
            foreach (var crew in  _crewCompartment.Crew)
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
            if (_isRagdollActive == enable) return;
            
            _isRagdollActive = enable;
            Data.EnableRagdoll = enable;
            
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
                
                var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in evaRigidbodies)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
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
                
                var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in evaRigidbodies)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
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

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (_pilotIK == null) return;
            
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