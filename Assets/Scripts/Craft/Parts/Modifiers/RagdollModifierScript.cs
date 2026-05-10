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
            
            // Debug button to enable ragdoll
            TextButtonModel enableButton = new TextButtonModel(
                "Enable Ragdoll",
                b => 
                {
                    Mod.Log("Inspector Enable button clicked");
                    EnableRagdollMode();
                }
            );
            groupModel.Add<TextButtonModel>(enableButton);
            
            // Debug button to disable ragdoll
            TextButtonModel disableButton = new TextButtonModel(
                "Disable Ragdoll",
                b => 
                {
                    Mod.Log("Inspector Disable button clicked");
                    DisableRagdollMode();
                }
            );
            groupModel.Add<TextButtonModel>(disableButton);
            
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
            if (enable)
                EnableRagdollMode();
            else
                DisableRagdollMode();
        }

        private void ApplyRagdollPhysics()
        {
            Mod.Log($"=== ApplyRagdollPhysics START ===");
            Mod.Log($"_pilotIK = {_pilotIK}");
            Mod.Log($"_evaScript = {_evaScript}");
            
            if (_evaScript == null)
            {
                Mod.Log("ApplyRagdollPhysics: _evaScript is null, returning");
                return;
            }
            
            var transform = _evaScript.transform;
            Mod.Log($"ApplyRagdollPhysics: _evaScript transform path = {GetTransformPath(transform)}");
            
            // Log the ENTIRE bone hierarchy
            Mod.Log("=== BONE HIERARCHY ===");
            LogBoneHierarchy(transform, 0);
            Mod.Log("=== END BONE HIERARCHY ===");
            
            // Disable animator if exists
            var animator = _evaScript.GetComponent<Animator>();
            Mod.Log($"ApplyRagdollPhysics: Animator = {animator}");
            if (animator != null)
            {
                animator.enabled = false;
                Mod.Log("ApplyRagdollPhysics: Disabled Animator");
            }
            else
            {
                Mod.Log("ApplyRagdollPhysics: Animator is null - fine for ragdoll");
            }
            
            // Disable FullBodyBipedIK if exists
            if (_pilotIK != null)
            {
                Mod.Log($"ApplyRagdollPhysics: Disabling _pilotIK on {_pilotIK.gameObject.name}");
                SaveIKWeights();
                _pilotIK.enabled = false;
                
                var solver = _pilotIK.solver;
                solver.rightHandEffector.positionWeight = 0f;
                solver.rightHandEffector.rotationWeight = 0f;
                solver.leftHandEffector.positionWeight = 0f;
                solver.leftHandEffector.rotationWeight = 0f;
                solver.rightFootEffector.positionWeight = 0f;
                solver.rightFootEffector.rotationWeight = 0f;
                solver.leftFootEffector.positionWeight = 0f;
                solver.leftFootEffector.rotationWeight = 0f;
                Mod.Log("ApplyRagdollPhysics: Set IK weights to 0");
            }
            else
            {
                Mod.Log("ApplyRagdollPhysics: _pilotIK is null - skipping IK disable");
            }
            
            // Store original states and configure for ragdoll
            _originalRigidbodyStates.Clear();
            
            var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            Mod.Log($"ApplyRagdollPhysics: Found {evaRigidbodies.Length} Rigidbody components");
            
            if (evaRigidbodies.Length == 0)
            {
                Mod.Log("ApplyRagdollPhysics: No Rigidbody found! Creating ragdoll dynamically...");
                CreateRagdollDynamically(transform);
            }
            else
            {
                int i = 0;
                foreach (var rb in evaRigidbodies)
                {
                    Mod.Log($"ApplyRagdollPhysics: Rigidbody[{i}] name={rb.gameObject.name}, isKinematic={rb.isKinematic}, mass={rb.mass}");
                    _originalRigidbodyStates.Add(new RigidbodyState
                    {
                        rb = rb,
                        isKinematic = rb.isKinematic,
                        useGravity = rb.useGravity,
                        collisionDetectionMode = rb.collisionDetectionMode
                    });
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    i++;
                }
                
                // Check for CharacterJoints
                var joints = _evaScript.GetComponentsInChildren<CharacterJoint>();
                Mod.Log($"ApplyRagdollPhysics: Found {joints.Length} CharacterJoint components");
                foreach (var joint in joints)
                {
                    Mod.Log($"  Joint: {joint.gameObject.name} -> {joint.connectedBody?.gameObject?.name ?? "null"}");
                }
                
                // If no joints, we need to add them
                if (joints.Length == 0 && evaRigidbodies.Length > 1)
                {
                    Mod.Log("ApplyRagdollPhysics: No CharacterJoints found! Adding dynamically...");
                    CreateRagdollDynamically(transform);
                }
            }
            
            Mod.Log($"ApplyRagdollPhysics: Stored {_originalRigidbodyStates.Count} original rigidbody states");
            Mod.Log("=== ApplyRagdollPhysics END ===");
        }

        private string GetTransformPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private void LogBoneHierarchy(Transform t, int depth)
        {
            string indent = new string(' ', depth * 2);
            string path = GetTransformPath(t);
            var rb = t.GetComponent<Rigidbody>();
            var collider = t.GetComponent<Collider>();
            Mod.Log($"{indent}{t.name} (path={path}) hasRigidbody={rb != null}, hasCollider={collider != null}");
            
            for (int i = 0; i < t.childCount; i++)
            {
                LogBoneHierarchy(t.GetChild(i), depth + 1);
            }
        }

        private void CreateRagdollDynamically(Transform root)
        {
            Mod.Log($"CreateRagdollDynamically: Starting from {root.name}");
            
            // The actual bone root is likely Root/Offset under the EvaScript
            Transform boneRoot = root.Find("Root/Offset");
            if (boneRoot == null) boneRoot = root.Find("Offset");
            if (boneRoot == null) boneRoot = root;
            
            Mod.Log($"CreateRagdollDynamically: boneRoot = {boneRoot.name}");
            
            // Navigate to Hips: <boneRoot>/Hips
            Transform hips = boneRoot.Find("Hips");
            if (hips == null) hips = boneRoot.Find("../Hips");
            Mod.Log($"CreateRagdollDynamically: hips = {hips?.name}");
            
            // Navigate to Spine: <boneRoot>/Hips/Spine
            Transform spine = hips?.Find("Spine");
            Mod.Log($"CreateRagdollDynamically: spine = {spine?.name}");
            
            // Navigate to Chest: <boneRoot>/Hips/Spine/Chest
            Transform chest = spine?.Find("Chest");
            Mod.Log($"CreateRagdollDynamically: chest = {chest?.name}");
            
            // Navigate to UpperChest: <boneRoot>/Hips/Spine/Chest/UpperChest
            Transform upperChest = chest?.Find("UpperChest");
            Mod.Log($"CreateRagdollDynamically: upperChest = {upperChest?.name}");
            
            // Navigate to Neck/Head: <boneRoot>/Hips/Spine/Chest/UpperChest/Neck/Head
            Transform neck = upperChest?.Find("Neck");
            Transform head = neck?.Find("Head");
            Mod.Log($"CreateRagdollDynamically: neck = {neck?.name}, head = {head?.name}");
            
            // Navigate to LeftClavicle/LeftShoulder/LeftElbow: 
            // <boneRoot>/Hips/Spine/Chest/UpperChest/LeftClavicle/LeftShoulder/LeftElbow
            Transform leftClavicle = upperChest?.Find("LeftClavicle");
            Transform leftShoulder = leftClavicle?.Find("LeftShoulder");
            Transform leftElbow = leftShoulder?.Find("LeftElbow");
            Transform leftHand = leftElbow?.Find("LeftHand");
            Mod.Log($"CreateRagdollDynamically: leftClavicle={leftClavicle?.name}, leftShoulder={leftShoulder?.name}, leftElbow={leftElbow?.name}, leftHand={leftHand?.name}");
            
            // Navigate to RightClavicle/RightShoulder/RightElbow
            Transform rightClavicle = upperChest?.Find("RightClavicle");
            Transform rightShoulder = rightClavicle?.Find("RightShoulder");
            Transform rightElbow = rightShoulder?.Find("RightElbow");
            Transform rightHand = rightElbow?.Find("RightHand");
            Mod.Log($"CreateRagdollDynamically: rightClavicle={rightClavicle?.name}, rightShoulder={rightShoulder?.name}, rightElbow={rightElbow?.name}, rightHand={rightHand?.name}");
            
            // Navigate to LeftHip/LeftKnee/LeftAnkle
            Transform leftHip = hips?.Find("LeftHip");
            Transform leftKnee = leftHip?.Find("LeftKnee");
            Transform leftAnkle = leftKnee?.Find("LeftAnkle");
            Transform leftToes = leftAnkle?.Find("LeftToes");
            Mod.Log($"CreateRagdollDynamically: leftHip={leftHip?.name}, leftKnee={leftKnee?.name}, leftAnkle={leftAnkle?.name}, leftToes={leftToes?.name}");
            
            // Navigate to RightHip/RightKnee/RightAnkle
            Transform rightHip = hips?.Find("RightHip");
            Transform rightKnee = rightHip?.Find("RightKnee");
            Transform rightAnkle = rightKnee?.Find("RightAnkle");
            Transform rightToes = rightAnkle?.Find("RightToes");
            Mod.Log($"CreateRagdollDynamically: rightHip={rightHip?.name}, rightKnee={rightKnee?.name}, rightAnkle={rightAnkle?.name}, rightToes={rightToes?.name}");
            
            // Now add Rigidbody + CharacterJoint
            // The Hips is the root of the ragdoll - no parent joint needed
            if (hips != null)
            {
                var rb = hips.gameObject.AddComponent<Rigidbody>();
                rb.mass = 20f;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                Mod.Log($"CreateRagdollDynamically: Added Rigidbody to {hips.name}, mass={rb.mass}");
            }
            
            // Spine -> Hips
            if (spine != null && hips != null)
            {
                AddRigidbodyAndJoint(spine.gameObject, hips.gameObject, 15f, 
                    swingLimit: 10f, twistLow: -20f, twistHigh: 20f);
            }
            
            // Chest -> Spine
            if (chest != null && spine != null)
            {
                AddRigidbodyAndJoint(chest.gameObject, spine.gameObject, 10f,
                    swingLimit: 15f, twistLow: -30f, twistHigh: 30f);
            }
            
            // UpperChest -> Chest (more mobile)
            if (upperChest != null && chest != null)
            {
                AddRigidbodyAndJoint(upperChest.gameObject, chest.gameObject, 8f,
                    swingLimit: 20f, twistLow: -40f, twistHigh: 40f);
            }
            
            // Head -> Neck (UpperChest)
            if (head != null && neck != null)
            {
                AddRigidbodyAndJoint(head.gameObject, neck.gameObject, 5f,
                    swingLimit: 30f, twistLow: -50f, twistHigh: 50f);
            }
            else if (head != null && upperChest != null)
            {
                AddRigidbodyAndJoint(head.gameObject, upperChest.gameObject, 5f,
                    swingLimit: 30f, twistLow: -50f, twistHigh: 50f);
            }
            
            // Left arm: Clavicle -> UpperChest, Shoulder -> Clavicle, Elbow -> Shoulder, Hand -> Elbow
            if (leftClavicle != null && upperChest != null)
            {
                AddRigidbodyAndJoint(leftClavicle.gameObject, upperChest.gameObject, 3f,
                    swingLimit: 45f, twistLow: -45f, twistHigh: 45f);
            }
            if (leftShoulder != null && leftClavicle != null)
            {
                AddRigidbodyAndJoint(leftShoulder.gameObject, leftClavicle.gameObject, 3f,
                    swingLimit: 90f, twistLow: -90f, twistHigh: 90f);
            }
            if (leftElbow != null && leftShoulder != null)
            {
                AddRigidbodyAndJoint(leftElbow.gameObject, leftShoulder.gameObject, 2f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 120f); // hinge joint
            }
            if (leftHand != null && leftElbow != null)
            {
                AddRigidbodyAndJoint(leftHand.gameObject, leftElbow.gameObject, 1f,
                    swingLimit: 0f, twistLow: -30f, twistHigh: 30f);
            }
            
            // Right arm
            if (rightClavicle != null && upperChest != null)
            {
                AddRigidbodyAndJoint(rightClavicle.gameObject, upperChest.gameObject, 3f,
                    swingLimit: 45f, twistLow: -45f, twistHigh: 45f);
            }
            if (rightShoulder != null && rightClavicle != null)
            {
                AddRigidbodyAndJoint(rightShoulder.gameObject, rightClavicle.gameObject, 3f,
                    swingLimit: 90f, twistLow: -90f, twistHigh: 90f);
            }
            if (rightElbow != null && rightShoulder != null)
            {
                AddRigidbodyAndJoint(rightElbow.gameObject, rightShoulder.gameObject, 2f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 120f);
            }
            if (rightHand != null && rightElbow != null)
            {
                AddRigidbodyAndJoint(rightHand.gameObject, rightElbow.gameObject, 1f,
                    swingLimit: 0f, twistLow: -30f, twistHigh: 30f);
            }
            
            // Left leg: Hip -> Hips, Knee -> Hip, Ankle -> Knee, Toes -> Ankle
            if (leftHip != null && hips != null)
            {
                AddRigidbodyAndJoint(leftHip.gameObject, hips.gameObject, 5f,
                    swingLimit: 30f, twistLow: -20f, twistHigh: 20f);
            }
            if (leftKnee != null && leftHip != null)
            {
                AddRigidbodyAndJoint(leftKnee.gameObject, leftHip.gameObject, 3f,
                    swingLimit: 0f, twistLow: -90f, twistHigh: 0f); // hinge
            }
            if (leftAnkle != null && leftKnee != null)
            {
                AddRigidbodyAndJoint(leftAnkle.gameObject, leftKnee.gameObject, 2f,
                    swingLimit: 20f, twistLow: -30f, twistHigh: 30f);
            }
            if (leftToes != null && leftAnkle != null)
            {
                AddRigidbodyAndJoint(leftToes.gameObject, leftAnkle.gameObject, 0.5f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 0f);
            }
            
            // Right leg
            if (rightHip != null && hips != null)
            {
                AddRigidbodyAndJoint(rightHip.gameObject, hips.gameObject, 5f,
                    swingLimit: 30f, twistLow: -20f, twistHigh: 20f);
            }
            if (rightKnee != null && rightHip != null)
            {
                AddRigidbodyAndJoint(rightKnee.gameObject, rightHip.gameObject, 3f,
                    swingLimit: 0f, twistLow: -90f, twistHigh: 0f);
            }
            if (rightAnkle != null && rightKnee != null)
            {
                AddRigidbodyAndJoint(rightAnkle.gameObject, rightKnee.gameObject, 2f,
                    swingLimit: 20f, twistLow: -30f, twistHigh: 30f);
            }
            if (rightToes != null && rightAnkle != null)
            {
                AddRigidbodyAndJoint(rightToes.gameObject, rightAnkle.gameObject, 0.5f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 0f);
            }
            
            // Verify
            var allRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            Mod.Log($"CreateRagdollDynamically: Created {allRigidbodies.Length} Rigidbody components");
            foreach (var rb in allRigidbodies)
            {
                Mod.Log($"  - {rb.gameObject.name} has Rigidbody, mass={rb.mass}");
            }
        }

        private void AddRigidbodyAndJoint(GameObject bone, GameObject parentBone, float mass,
            float swingLimit = 45f, float twistLow = -50f, float twistHigh = 50f)
        {
            if (bone == null || parentBone == null) return;
            
            var rb = bone.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            var joint = bone.AddComponent<CharacterJoint>();
            joint.connectedBody = parentBone.GetComponent<Rigidbody>();
            
            var swing1 = new SoftJointLimit { limit = swingLimit, bounciness = 0f };
            var swing2 = new SoftJointLimit { limit = swingLimit, bounciness = 0f };
            var twist = new SoftJointLimit { limit = (twistHigh - twistLow) * 0.5f, bounciness = 0f };
            var lowTwist = new SoftJointLimit { limit = twistLow, bounciness = 0f };
            var highTwist = new SoftJointLimit { limit = twistHigh, bounciness = 0f };
            joint.swing1Limit = swing1;
            joint.swing2Limit = swing2;
          
            joint.lowTwistLimit = lowTwist;
            joint.highTwistLimit = highTwist;
            
            Mod.Log($"AddRigidbodyAndJoint: {bone.name} -> {parentBone.name}, mass={mass}, swing={swingLimit}");
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

        
        /// <summary>
        /// Enable ragdoll mode - called externally or from FlightUpdate
        /// </summary>
        public void EnableRagdollMode()
        {
            if (_isRagdollActive) return;
            
            Mod.Log("EnableRagdollMode called");
            
            // Only find crew if not already set
            if (_evaScript == null || _pilotIK == null)
            {
                Mod.Log("EnableRagdollMode: Searching for crew...");
                FindCrew();
            }
            else
            {
                Mod.Log("EnableRagdollMode: Crew already found, skipping search");
            }
            
            Mod.Log($"After FindCrew: _evaScript={_evaScript != null}, _pilotIK={_pilotIK != null}");
            
            _isRagdollActive = true;
            Data.EnableRagdoll = true;
            
            if (_evaScript != null && _pilotIK != null)
            {
                Mod.Log("EnableRagdollMode: Calling ApplyRagdollPhysics");
                ApplyRagdollPhysics();
            }
            else
            {
                Mod.Log("EnableRagdollMode: _evaScript or _pilotIK is null, ragdoll physics not applied");
            }
        }

        /// <summary>
        /// Disable ragdoll mode
        /// </summary>
        public void DisableRagdollMode()
        {
            if (!_isRagdollActive) return;
            
            Mod.Log("DisableRagdollMode called");
            _isRagdollActive = false;
            Data.EnableRagdoll = false;
            RestoreEvaScriptIK();
        }

        private void FindCrew()
        {
            Mod.Log("FindCrew: Starting search");
            
            // Method 1: Try our own crew compartment
            if (_crewCompartment != null && _crewCompartment.Crew.Count > 0)
            {
                Mod.Log($"FindCrew: Found {_crewCompartment.Crew.Count} crew in our compartment");
                SetCrew(_crewCompartment.Crew[0]);
                return;
            }
            
            // Method 2: Try to get crew compartment from this part
            var crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
            if (crewCompartment != null && crewCompartment.Crew.Count > 0)
            {
                _crewCompartment = crewCompartment;
                Mod.Log($"FindCrew: Found {crewCompartment.Crew.Count} crew in part compartment");
                SetCrew(crewCompartment.Crew[0]);
                return;
            }
            
            // Method 3: Direct EvaScript modifier
            var eva = PartScript.GetModifier<EvaScript>();
            if (eva != null)
            {
                Mod.Log("FindCrew: Found EvaScript via GetModifier");
                SetCrew(eva);
                return;
            }
            
            Mod.Log($"FindCrew: Could not find crew for part {PartScript.Data.Name}");
        }

        private void SetCrew(EvaScript crew)
        {
            Mod.Log($"SetCrew: crew = {crew}, crew.gameObject.name = {crew.gameObject.name}");
            _evaScript = crew;
            
            // Only set _pilotIK if not already set
            if (_pilotIK == null)
            {
                // Find FullBodyBipedIK on the SAME object as EvaScript
                _pilotIK = crew.GetComponent<FullBodyBipedIK>();
                if (_pilotIK == null)
                {
                    _pilotIK = crew.GetComponentInChildren<FullBodyBipedIK>();
                    Mod.Log($"SetCrew: GetComponentInChildren found _pilotIK = {_pilotIK?.gameObject?.name}");
                }
                else
                {
                    Mod.Log($"SetCrew: GetComponent found _pilotIK on same object");
                }
            }
            Mod.Log($"SetCrew: _pilotIK = {_pilotIK}");
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (!_isRagdollActive || _evaScript == null) return;
            
            // Force all bone rigidbodies to ragdoll state every physics step
            var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in evaRigidbodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }
        
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!_isRagdollActive || _evaScript == null) return;
            
            if (Game.InFlightScene && Data.EnableRagdoll && !_isRagdollActive)
            {
                Mod.Log("IFlightUpdate: Auto-enabling ragdoll");
                EnableRagdollMode();
            }
            // Keep Animator disabled - check every frame in case something re-enables it
            var animator = _evaScript.GetComponent<Animator>();
            if (animator != null && animator.enabled)
            {
                Mod.Log("FlightUpdate: Animator was re-enabled, disabling again");
                animator.enabled = false;
            }
            
            // Check for IK re-enable
            if (_pilotIK != null && _pilotIK.enabled)
            {
                Mod.Log("FlightUpdate: _pilotIK was re-enabled, disabling again");
                _pilotIK.enabled = false;
            }
            
            // Also disable any child Animators
            var childAnimators = _evaScript.GetComponentsInChildren<Animator>();
            foreach (var anim in childAnimators)
            {
                if (anim.enabled)
                {
                    anim.enabled = false;
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
