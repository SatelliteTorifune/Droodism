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
        IFlightFixedUpdate,
        IFlightUpdatePaused
    {
        private CrewCompartmentScript? _crewCompartment;
        private FullBodyBipedIK? _pilotIK;
        private EvaScript? _evaScript;
        
        private bool _isRagdollActive = false;
        private bool _ragdollPhysicsCreated = false; // Track if ragdoll physics has been fully created
        private bool _wasPaused = false; // Track previous pause state
        private bool _justUnpaused = false; // Flag to detect unpause transition
        
        // Store bone transforms to restore after unpause
        private class BoneTransformState
        {
            public Transform bone;
            public Vector3 position;
            public Quaternion rotation;
        }
        private List<BoneTransformState> _savedBoneStates = new();
        
        private IKSavedWeights _savedWeights = new();
        private Transform? _ragdollRoot;
        private HashSet<string> _skipBones = new(StringComparer.OrdinalIgnoreCase);
        
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
            
            Mod.Log($"OnCraftStructureChanged: Called, _isRagdollActive={_isRagdollActive}");
            
            if (_crewCompartment != null)
            {
                _crewCompartment.CrewEnter -= OnCrewEnter;
                _crewCompartment.CrewExit -= OnCrewExit;
                _crewCompartment.CrewEnter += OnCrewEnter;
                _crewCompartment.CrewExit += OnCrewExit;
            }
            
            // Only refresh pilot references if ragdoll is not active
            // Otherwise, we might accidentally recreate the ragdoll
            if (!_isRagdollActive)
            {
                RefreshPilotReferences();
            }
            else
            {
                Mod.Log("OnCraftStructureChanged: Skipping RefreshPilotReferences because ragdoll is active");
            }
        }

        private void OnCrewEnter(EvaScript crew)
        {
            Mod.Log($"OnCrewEnter: crew={crew?.gameObject?.name}, _isRagdollActive={_isRagdollActive}");
            RefreshPilotReferencesFromCrew(crew);
        }

        private void OnCrewExit(EvaScript crew)
        {
            Mod.Log($"OnCrewExit: crew={crew?.gameObject?.name}, _isRagdollActive={_isRagdollActive}");
            // Don't clear pilot references if ragdoll is active - we still need them
            if (!_isRagdollActive)
            {
                ClearPilotReferences();
            }
            else
            {
                Mod.Log("OnCrewExit: Keeping pilot references because ragdoll is active");
            }
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
            Mod.Log($"RefreshPilotReferencesFromCrew: crew={crew?.gameObject?.name}, _isRagdollActive={_isRagdollActive}");
            
            _evaScript = crew;
            _pilotIK = crew.GetComponentInChildren<FullBodyBipedIK>();
            
            if (_pilotIK == null)
            {
                Mod.Log("RefreshPilotReferencesFromCrew: _pilotIK is null, returning");
                return;
            }

            if (_isRagdollActive)
            {
                Mod.Log("RefreshPilotReferencesFromCrew: Calling ApplyRagdollPhysics");
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
                Mod.LogWarning("ApplyRagdollPhysics: _evaScript is null, returning");
                return;
            }
            
            var transform = _evaScript.transform;
            Mod.Log($"ApplyRagdollPhysics: _evaScript transform path = {GetTransformPath(transform)}");
            
            // Check if ragdoll already exists - prevent duplicate creation
            var existingRBs = _evaScript.GetComponentsInChildren<Rigidbody>();
            if (existingRBs.Length > 0)
            {
                Mod.Log($"ApplyRagdollPhysics: Ragdoll already exists ({existingRBs.Length} Rigidbodies found), skipping creation");
                
                // Just ensure they are non-kinematic
                foreach (var rb in existingRBs)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
                return;
            }
            
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

            // Bones to skip - these are decorative meshes, not physics bones
            _skipBones.Clear();
            _skipBones.Add("EVAChestPlate");
            _skipBones.Add("EVAChestPlateVariant");
            _skipBones.Add("JetPackNozzleBottomLeft");
            _skipBones.Add("JetPackNozzleBottomRight");
            _skipBones.Add("JetPackNozzleTopLeft");
            _skipBones.Add("JetPackNozzleTopRight");
            _skipBones.Add("ParticleSystem");
            _skipBones.Add("ClickyCollider");
            
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
            
            // Store reference for later
            _ragdollRoot = boneRoot;
            
            // Now add Rigidbody + CharacterJoint
            // The Hips is the root of the ragdoll - no parent joint needed
            if (hips != null)
            {
                var rb = hips.gameObject.AddComponent<Rigidbody>();
                rb.mass = 25f; // Heavier root for stability
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Mod.Log($"CreateRagdollDynamically: Added Rigidbody to {hips.name}, mass={rb.mass}");
            }
            
            // Spine -> Hips - very restricted
            if (spine != null && hips != null)
            {
                AddRigidbodyAndJoint(spine.gameObject, hips.gameObject, 15f, 
                    swingLimit: 3f, twistLow: -5f, twistHigh: 5f, limbLength: 0.3f);
            }
            
            // Chest -> Spine - restricted
            if (chest != null && spine != null)
            {
                AddRigidbodyAndJoint(chest.gameObject, spine.gameObject, 10f,
                    swingLimit: 5f, twistLow: -10f, twistHigh: 10f, limbLength: 0.25f);
            }
            
            // UpperChest -> Chest
            if (upperChest != null && chest != null)
            {
                AddRigidbodyAndJoint(upperChest.gameObject, chest.gameObject, 8f,
                    swingLimit: 8f, twistLow: -15f, twistHigh: 15f, limbLength: 0.2f);
            }
            
            // Neck -> UpperChest - very tight (MUST be before Head -> Neck!)
            Mod.Log($"CreateRagdollDynamically: Neck setup - neck={neck?.name ?? "NULL"}, upperChest={upperChest?.name ?? "NULL"}");
            if (neck != null && upperChest != null)
            {
                Mod.Log($"CreateRagdollDynamically: Calling AddRigidbodyAndJoint for Neck");
                AddRigidbodyAndJoint(neck.gameObject, upperChest.gameObject, 2f,
                    swingLimit: 3f, twistLow: -5f, twistHigh: 5f, limbLength: 0.1f);
            }

            // Head -> Neck - tighter limit (MUST be after Neck -> UpperChest!)
            Mod.Log($"CreateRagdollDynamically: Head setup - head={head?.name ?? "NULL"}, neck={neck?.name ?? "NULL"}");
            if (head != null && neck != null)
            {
                Mod.Log($"CreateRagdollDynamically: Calling AddRigidbodyAndJoint for Head");
                AddRigidbodyAndJoint(head.gameObject, neck.gameObject, 5f,
                    swingLimit: 15f, twistLow: -20f, twistHigh: 20f, limbLength: 0.15f);
                Mod.Log($"CreateRagdollDynamically: After AddRigidbodyAndJoint for Head");
            }
            else if (head != null && upperChest != null)
            {
                Mod.Log($"CreateRagdollDynamically: Falling back to Head -> UpperChest");
                AddRigidbodyAndJoint(head.gameObject, upperChest.gameObject, 5f,
                    swingLimit: 15f, twistLow: -20f, twistHigh: 20f, limbLength: 0.15f);
            }
            else
            {
                Mod.Log($"CreateRagdollDynamically: Head NOT connected - head={head?.name ?? "NULL"}, neck={neck?.name ?? "NULL"}, upperChest={upperChest?.name ?? "NULL"}");
            }
            
            // Left arm
            if (leftClavicle != null && upperChest != null)
            {
                AddRigidbodyAndJoint(leftClavicle.gameObject, upperChest.gameObject, 3f,
                    swingLimit: 10f, twistLow: -10f, twistHigh: 10f, limbLength: 0.1f);
            }
            if (leftShoulder != null && leftClavicle != null)
            {
                AddRigidbodyAndJoint(leftShoulder.gameObject, leftClavicle.gameObject, 3f,
                    swingLimit: 30f, twistLow: -30f, twistHigh: 30f, limbLength: 0.25f);
            }
            if (leftElbow != null && leftShoulder != null)
            {
                AddRigidbodyAndJoint(leftElbow.gameObject, leftShoulder.gameObject, 2f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 90f, limbLength: 0.25f, isHinge: true);
            }
            if (leftHand != null && leftElbow != null)
            {
                AddRigidbodyAndJoint(leftHand.gameObject, leftElbow.gameObject, 0.5f,
                    swingLimit: 0f, twistLow: -10f, twistHigh: 10f, limbLength: 0.08f);
            }
            
            // Right arm
            if (rightClavicle != null && upperChest != null)
            {
                AddRigidbodyAndJoint(rightClavicle.gameObject, upperChest.gameObject, 3f,
                    swingLimit: 10f, twistLow: -10f, twistHigh: 10f, limbLength: 0.1f);
            }
            if (rightShoulder != null && rightClavicle != null)
            {
                AddRigidbodyAndJoint(rightShoulder.gameObject, rightClavicle.gameObject, 3f,
                    swingLimit: 30f, twistLow: -30f, twistHigh: 30f, limbLength: 0.25f);
            }
            if (rightElbow != null && rightShoulder != null)
            {
                AddRigidbodyAndJoint(rightElbow.gameObject, rightShoulder.gameObject, 2f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 90f, limbLength: 0.25f, isHinge: true);
            }
            if (rightHand != null && rightElbow != null)
            {
                AddRigidbodyAndJoint(rightHand.gameObject, rightElbow.gameObject, 0.5f,
                    swingLimit: 0f, twistLow: -10f, twistHigh: 10f, limbLength: 0.08f);
            }
            
            // Left leg - CRITICAL: legs need strict limits to prevent crossing
            if (leftHip != null && hips != null)
            {
                AddRigidbodyAndJoint(leftHip.gameObject, hips.gameObject, 5f,
                    swingLimit: 10f, twistLow: -5f, twistHigh: 5f, limbLength: 0.12f);
            }
            if (leftKnee != null && leftHip != null)
            {
                // Knee should only bend backward (hinge)
                AddRigidbodyAndJoint(leftKnee.gameObject, leftHip.gameObject, 3f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 5f, limbLength: 0.35f, isHinge: true);
            }
            if (leftAnkle != null && leftKnee != null)
            {
                AddRigidbodyAndJoint(leftAnkle.gameObject, leftKnee.gameObject, 2f,
                    swingLimit: 5f, twistLow: -10f, twistHigh: 10f, limbLength: 0.25f);
            }
            if (leftToes != null && leftAnkle != null)
            {
                AddRigidbodyAndJoint(leftToes.gameObject, leftAnkle.gameObject, 0.2f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 0f, limbLength: 0.04f);
            }
            
            // Right leg - symmetric and strict
            if (rightHip != null && hips != null)
            {
                AddRigidbodyAndJoint(rightHip.gameObject, hips.gameObject, 5f,
                    swingLimit: 10f, twistLow: -5f, twistHigh: 5f, limbLength: 0.12f);
            }
            if (rightKnee != null && rightHip != null)
            {
                AddRigidbodyAndJoint(rightKnee.gameObject, rightHip.gameObject, 3f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 5f, limbLength: 0.35f, isHinge: true);
            }
            if (rightAnkle != null && rightKnee != null)
            {
                AddRigidbodyAndJoint(rightAnkle.gameObject, rightKnee.gameObject, 2f,
                    swingLimit: 5f, twistLow: -10f, twistHigh: 10f, limbLength: 0.25f);
            }
            if (rightToes != null && rightAnkle != null)
            {
                AddRigidbodyAndJoint(rightToes.gameObject, rightAnkle.gameObject, 0.2f,
                    swingLimit: 0f, twistLow: 0f, twistHigh: 0f, limbLength: 0.04f);
            }
            
            // Verify
            var allRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            Mod.Log($"CreateRagdollDynamically: Created {allRigidbodies.Length} Rigidbody components");
            foreach (var rb in allRigidbodies)
            {
                var joint = rb.GetComponent<CharacterJoint>();
                Mod.Log($"  - {rb.gameObject.name} has Rigidbody, mass={rb.mass}, connectedTo={joint?.connectedBody?.gameObject?.name ?? "NONE (root)"}");
            }
            
            // Disable collisions between ragdoll bones themselves to prevent jittering
            DisableRagdollSelfCollisions();
            
            // Mark ragdoll as fully created
            _ragdollPhysicsCreated = true;
            Mod.Log("CreateRagdollDynamically: Ragdoll physics fully created and initialized");
        }
        
        private void DisableRagdollSelfCollisions()
        {
            if (_evaScript == null) return;
            
            var allColliders = _evaScript.GetComponentsInChildren<Collider>();
            int ignoredPairs = 0;
            
            // Ignore all collisions between ragdoll bones (they should only collide with external objects)
            for (int i = 0; i < allColliders.Length; i++)
            {
                for (int j = i + 1; j < allColliders.Length; j++)
                {
                    if (allColliders[i] != null && allColliders[j] != null)
                    {
                        Physics.IgnoreCollision(allColliders[i], allColliders[j], true);
                        ignoredPairs++;
                    }
                }
            }
            
            Mod.Log($"DisableRagdollSelfCollisions: Ignored {ignoredPairs} collision pairs between {allColliders.Length} ragdoll colliders");
        }

        private void AddRigidbodyAndJoint(GameObject bone, GameObject parentBone, float mass,
            float swingLimit = 45f, float twistLow = -50f, float twistHigh = 50f,
            float limbLength = 0.2f, bool isHinge = false)
        {
            Mod.Log($"AddRigidbodyAndJoint ENTER: bone={bone?.name ?? "NULL"}, parentBone={parentBone?.name ?? "NULL"}, mass={mass}");
            if (bone == null || parentBone == null)
            {
                Mod.LogWarning("AddRigidbodyAndJoint: bone or parentBone is null, returning early");
                return;
            }
            
            var rb = bone.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Add collider - capsule with proper size
            AddBoneCollider(bone, parentBone, limbLength);
            
            var joint = bone.AddComponent<CharacterJoint>();
            joint.connectedBody = parentBone.GetComponent<Rigidbody>();
            
            // Configure joint limits based on joint type
            float actualSwingLimit = isHinge ? 0f : swingLimit;
            float actualTwistLow = twistLow;
            float actualTwistHigh = twistHigh;
            
            var swing1 = new SoftJointLimit { limit = actualSwingLimit, bounciness = 0f };
            var swing2 = new SoftJointLimit { limit = actualSwingLimit, bounciness = 0f };
            var twist = new SoftJointLimit { limit = (actualTwistHigh - actualTwistLow) * 0.5f, bounciness = 0f };
            var lowTwist = new SoftJointLimit { limit = actualTwistLow, bounciness = 0f };
            var highTwist = new SoftJointLimit { limit = actualTwistHigh, bounciness = 0f };
            joint.swing1Limit = swing1;
            joint.swing2Limit = swing2;
            joint.lowTwistLimit = lowTwist;
            joint.highTwistLimit = highTwist;
            
            // Joint stability settings
            joint.enablePreprocessing = true;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
            
            Mod.Log($"AddRigidbodyAndJoint: {bone.name} -> {parentBone.name}, mass={mass}, swing={actualSwingLimit}");
            Mod.Log($"AddRigidbodyAndJoint EXIT: {bone.name} completed successfully");
        }
        
        private void AddBoneCollider(GameObject bone, GameObject parentBone, float limbLength)
        {
            if (bone == null) return;
            
            // Skip decorative bones that should not have physics
            if (_skipBones.Contains(bone.name))
            {
                Mod.Log($"AddBoneCollider: Skipping decorative bone {bone.name}");
                return;
            }
            
            // Remove any existing colliders on this bone
            var existingCollider = bone.GetComponent<Collider>();
            if (existingCollider != null)
            {
                GameObject.Destroy(existingCollider);
            }
            
            var col = bone.AddComponent<CapsuleCollider>();
            
            // Use the provided limb length for proper sizing
            // Add some padding for better collision
            float height = limbLength * 2.2f;
            float radius = limbLength * 0.25f;
            
            // Minimum sizes for stability
            radius = Mathf.Max(0.04f, radius);
            height = Mathf.Max(0.08f, height);
            
            col.radius = radius;
            col.height = height;
            
            // Determine capsule direction based on bone direction to parent
            if (parentBone != null)
            {
                Vector3 dirToParent = (parentBone.transform.position - bone.transform.position).normalized;
                
                float yAlign = Mathf.Abs(Vector3.Dot(dirToParent, Vector3.up));
                float xAlign = Mathf.Abs(Vector3.Dot(dirToParent, Vector3.right));
                float zAlign = Mathf.Abs(Vector3.Dot(dirToParent, Vector3.forward));
                
                if (yAlign > xAlign && yAlign > zAlign)
                {
                    col.direction = 1; // Y axis
                }
                else if (xAlign > zAlign)
                {
                    col.direction = 0; // X axis
                }
                else
                {
                    col.direction = 2; // Z axis
                }
            }
            else
            {
                col.direction = 1; // Default to Y axis
            }
            
            // Center the capsule
            col.center = Vector3.zero;
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

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            // Auto-enable ragdoll when in flight scene and ragdoll is enabled in data
            if (Game.InFlightScene && Data.EnableRagdoll && !_isRagdollActive)
            {
                Mod.Log("IFlightUpdate: Auto-enabling ragdoll");
                EnableRagdollMode();
                return;
            }
            
            if (!_isRagdollActive || _evaScript == null) return;
            
            // Detect pause state changes
            bool isPaused = Game.Instance.FlightScene.TimeManager.Paused;
            if (_wasPaused && !isPaused)
            {
                // Just unpaused - set flag and log
                _justUnpaused = true;
                Mod.Log("IFlightUpdate: Game just unpaused, will reinforce ragdoll");
            }
            _wasPaused = isPaused;
            
            // When unpaused, immediately reinforce ragdoll state
            if (_justUnpaused)
            {
                Mod.Log("IFlightUpdate: Restoring bone states and reinforcing ragdoll after unpause");
                RestoreBoneStates();
                ReinforceRagdollState();
                _justUnpaused = false;
            }
            
            // Check if pause state changed to PAUSED
            if (!_wasPaused && isPaused)
            {
                Mod.Log("IFlightUpdate: Game just paused, freezing ragdoll");
                FreezeRagdollForPause();
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

        /// <summary>
        /// Reinforce ragdoll state - call this after unpause to ensure physics is working correctly
        /// </summary>
        private void ReinforceRagdollState()
        {
            if (_evaScript == null) return;
            
            Mod.Log("ReinforceRagdollState: Starting");
            
            // First, restore bone transforms to where they should be
            RestoreBoneStates();
            
            // Ensure all rigidbodies are non-kinematic and have gravity
            var rigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.WakeUp();
                Mod.Log($"ReinforceRagdollState: Woke up {rb.gameObject.name}");
            }
            
            // Ensure Animator is disabled
            var animator = _evaScript.GetComponent<Animator>();
            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }
            
            // Ensure IK is disabled
            if (_pilotIK != null)
            {
                _pilotIK.enabled = false;
            }
        }
        
        /// <summary>
        /// Save current bone transforms before pause
        /// </summary>
        private void SaveBoneStates()
        {
            if (_evaScript == null) return;
            
            _savedBoneStates.Clear();
            var rigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                _savedBoneStates.Add(new BoneTransformState
                {
                    bone = rb.transform,
                    position = rb.transform.localPosition,
                    rotation = rb.transform.localRotation
                });
            }
            Mod.Log($"SaveBoneStates: Saved {_savedBoneStates.Count} bone states");
        }
        
        /// <summary>
        /// Restore bone transforms after unpause
        /// </summary>
        private void RestoreBoneStates()
        {
            if (_evaScript == null || _savedBoneStates.Count == 0) return;
            
            Mod.Log($"RestoreBoneStates: Restoring {_savedBoneStates.Count} bone states");
            foreach (var state in _savedBoneStates)
            {
                if (state.bone != null)
                {
                    state.bone.localPosition = state.position;
                    state.bone.localRotation = state.rotation;
                }
            }
        }
        
        /// <summary>
        /// Freeze ragdoll during pause - set all rigidbodies to kinematic to prevent position drift
        /// </summary>
        private void FreezeRagdollForPause()
        {
            if (_evaScript == null) return;
            
            Mod.Log("FreezeRagdollForPause: Freezing all ragdoll rigidbodies");
            
            // Save current transform states first
            SaveBoneStates();
            
            var rigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                Mod.Log($"FreezeRagdollForPause: Set {rb.gameObject.name} to kinematic");
            }
        }

        /// <summary>
        /// Enable ragdoll mode - called externally or from FlightUpdate
        /// </summary>
        public void EnableRagdollMode()
        {
            if (_isRagdollActive) return;
            
            Mod.Log("EnableRagdollMode called");
            
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
                Mod.LogWarning("EnableRagdollMode: _evaScript or _pilotIK is null, ragdoll physics not applied");
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
            _ragdollPhysicsCreated = false;
            Data.EnableRagdoll = false;
            
            // Clean up dynamically created ragdoll components
            CleanupDynamicRagdoll();
            
            RestoreEvaScriptIK();
        }
        
        private void CleanupDynamicRagdoll()
        {
            if (_evaScript == null) return;
            
            Mod.Log("CleanupDynamicRagdoll: Starting cleanup");
            
            // Remove dynamically added components
            var rigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            int removedCount = 0;
            foreach (var rb in rigidbodies)
            {
                // Only remove if it was added by us (check mass values we use)
                // Include 25f for Hips root bone
                if (rb.mass > 0 && rb.mass <= 25f)
                {
                    Mod.Log($"CleanupDynamicRagdoll: Removing components from {rb.gameObject.name}, mass={rb.mass}");
                    
                    // Remove the joint first
                    var joint = rb.GetComponent<CharacterJoint>();
                    if (joint != null)
                    {
                        GameObject.Destroy(joint);
                    }
                    
                    // Remove the collider if it's a capsule we added
                    var capsule = rb.GetComponent<CapsuleCollider>();
                    if (capsule != null)
                    {
                        GameObject.Destroy(capsule);
                    }
                    
                    // Remove the rigidbody itself
                    GameObject.Destroy(rb);
                    removedCount++;
                }
                else
                {
                    Mod.Log($"CleanupDynamicRagdoll: SKIPPING {rb.gameObject.name}, mass={rb.mass} (not our component)");
                }
            }
            Mod.Log($"CleanupDynamicRagdoll: Removed {removedCount} rigidbody components");
            
            // Clear saved bone states since ragdoll is being destroyed
            _savedBoneStates.Clear();
            
            _ragdollRoot = null;
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
            
            Mod.LogWarning($"FindCrew: Could not find crew for part {PartScript.Data.Name}");
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

        /// <summary>
        /// Called every frame while the game is paused in flight scene.
        /// Use this to monitor pause state changes.
        /// </summary>
        void IFlightUpdatePaused.FlightUpdatePaused(in FlightFrameData frame)
        {
            if (!_isRagdollActive || _evaScript == null) return;
            
            // Detect transition to pause
            if (!_wasPaused)
            {
                Mod.Log("IFlightUpdatePaused: Game just paused, saving bone states");
                SaveBoneStates();
            }
            
            _wasPaused = true;
        }
    }
}
