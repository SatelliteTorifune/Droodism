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
        #region Fields

        private CrewCompartmentScript _crewCompartment;
        private FullBodyBipedIK _pilotIK;
        private EvaScript _evaScript;
        
        private bool _isRagdollActive = false;
        private bool _ragdollPhysicsCreated = false;
        private bool _wasPaused = false;
    
        
        private IKSavedWeights _savedWeights = new();
        private Transform? _ragdollRoot;
        private HashSet<string> _skipBones = new(StringComparer.OrdinalIgnoreCase);
        
        private List<RigidbodyState> _originalRigidbodyStates = new();
        private List<BoneTransform> _pausedBoneStates = new();

        #endregion

        #region Nested Types

        private class RigidbodyState
        {
            public Rigidbody rb;
            public bool isKinematic;
            public bool useGravity;
            public CollisionDetectionMode collisionDetectionMode;
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

        private class BoneTransform
        {
            public string Name;
            public Vector3 Position;
            public Quaternion Rotation;

            public BoneTransform(string name, Vector3 position, Quaternion rotation)
            {
                Name = name;
                Position = position;
                Rotation = rotation;
            }
        }

        #endregion

        private bool animationEnabled;
        private bool animationEnabled2;
        private bool ikEnabled;
        #region Unity Inspector

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            
            GroupModel groupModel = new GroupModel("Ragdoll");
            model.AddGroup(groupModel);
            
            groupModel.Add<ToggleModel>(new ToggleModel(
                "动画",
                () => animationEnabled,
                x => 
                {
                    animationEnabled = x;
                }
            ));
            groupModel.Add<ToggleModel>(new ToggleModel(
                "动画2",
                () => animationEnabled2,
                x => 
                {
                    animationEnabled2 = x;
                }
            ));
            groupModel.Add<ToggleModel>(new ToggleModel(
                "IK",
                () => ikEnabled,
                x => 
                {
                    ikEnabled = x;
                }
            ));
            
            groupModel.Add<TextModel>(new TextModel("Status", () => _isRagdollActive ? "Active" : "Inactive"));
            
            TextButtonModel enableButton = new TextButtonModel(
                "Enable Ragdoll",
                b => 
                {
                    EnableRagdollMode();
                }
            );
            groupModel.Add<TextButtonModel>(enableButton);
            
            TextButtonModel disableButton = new TextButtonModel(
                "Disable Ragdoll",
                b => 
                {
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
            
            if (!_isRagdollActive)
            {
                RefreshPilotReferences();
            }
            else
            {
                Mod.Log("OnCraftStructureChanged: Skipping RefreshPilotReferences because ragdoll is active");
            }
        }

        #endregion

        #region Crew Management

        private void OnCrewEnter(EvaScript crew)
        {
            //Mod.Log($"OnCrewEnter: crew={crew?.gameObject?.name}, _isRagdollActive={_isRagdollActive}");
            RefreshPilotReferencesFromCrew(crew);
        }

        private void OnCrewExit(EvaScript crew)
        {
            //Mod.Log($"OnCrewExit: crew={crew?.gameObject?.name}, _isRagdollActive={_isRagdollActive}");
            if (!_isRagdollActive)
            {
                ClearPilotReferences();
            }
            else
            {
               // Mod.Log("OnCrewExit: Keeping pilot references because ragdoll is active");
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
            //Mod.Log($"RefreshPilotReferencesFromCrew: crew={crew?.gameObject?.name}, _isRagdollActive={_isRagdollActive}");
            
            _evaScript = crew;
            _pilotIK = crew.GetComponentInChildren<FullBodyBipedIK>();
            

            if (_isRagdollActive)
            {
               // Mod.Log("RefreshPilotReferencesFromCrew: Calling ApplyRagdollPhysics");
                ApplyRagdollPhysics();
            }
        }

        private void ClearPilotReferences()
        {
            _evaScript = null;
            _pilotIK = null;
        }

        private void FindCrew()
        {
            if (_crewCompartment != null && _crewCompartment.Crew.Count > 0)
            {
                SetCrew(_crewCompartment.Crew[0]);
                return;
            }
            
            var crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
            if (crewCompartment != null && crewCompartment.Crew.Count > 0)
            {
                _crewCompartment = crewCompartment;
                SetCrew(crewCompartment.Crew[0]);
                return;
            }
            
            var eva = PartScript.GetModifier<EvaScript>();
            if (eva != null)
            {
                SetCrew(eva);
                return;
            }
        }

        private void SetCrew(EvaScript crew)
        {
            _evaScript = crew;
            
            if (_pilotIK == null)
            {
                _pilotIK = crew.GetComponent<FullBodyBipedIK>();
                if (_pilotIK == null)
                {
                    _pilotIK = crew.GetComponentInChildren<FullBodyBipedIK>();
                }
            }
        }

        #endregion

        #region Public API

        public void SetRagdollMode(bool enable)
        {
            if (enable)
                EnableRagdollMode();
            else
                DisableRagdollMode();
        }

        public void EnableRagdollMode()
        {
            if (_isRagdollActive) return;
            
           // Mod.Log("EnableRagdollMode called");
            _wasPaused = false;
            
            if (_evaScript == null || _pilotIK == null)
            {
                FindCrew();
            }
            else
            {
            }
            
            //Mod.Log($"After FindCrew: _evaScript={_evaScript != null}, _pilotIK={_pilotIK != null}");
            
            _isRagdollActive = true;
            Data.EnableRagdoll = true;
            
            if (_evaScript != null && _pilotIK != null)
            {
               // Mod.Log("EnableRagdollMode: Calling ApplyRagdollPhysics");
                ApplyRagdollPhysics();
            }
            else
            {
                Mod.LogWarning("EnableRagdollMode: _evaScript or _pilotIK is null, ragdoll physics not applied");
            }
        }

        public void DisableRagdollMode()
        {
            if (!_isRagdollActive) return;
            
            _isRagdollActive = false;
            _ragdollPhysicsCreated = false;
            _wasPaused = false;
            Data.EnableRagdoll = false;
            
            RestoreEvaScriptIK();
        }

        #endregion

        #region Ragdoll Physics

        private void ApplyRagdollPhysics()
        {
            
            var transform = _evaScript.transform;
           // Mod.Log($"ApplyRagdollPhysics: _evaScript transform path = {GetTransformPath(transform)}");
            
            var existingRBs = _evaScript.GetComponentsInChildren<Rigidbody>();
            if (existingRBs.Length > 0)
            {
               // Mod.Log($"ApplyRagdollPhysics: Ragdoll already exists ({existingRBs.Length} Rigidbodies found), skipping creation");
                
                foreach (var rb in existingRBs)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
                return;
            }
            
          
            
            var animator = _evaScript.GetComponent<Animator>();
            //Mod.Log($"ApplyRagdollPhysics: Animator = {animator}");
            if (animator != null)
            {
                animator.enabled = false;
                //Mod.Log("ApplyRagdollPhysics: Disabled Animator");
            }
            else
            {
                //Mod.Log("ApplyRagdollPhysics: Animator is null - fine for ragdoll");
            }
            
            if (_pilotIK != null)
            {
                //Mod.Log($"ApplyRagdollPhysics: Disabling _pilotIK on {_pilotIK.gameObject.name}");
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
               // Mod.Log("ApplyRagdollPhysics: Set IK weights to 0");
            }
            else
            {
               // Mod.Log("ApplyRagdollPhysics: _pilotIK is null - skipping IK disable");
            }
            
            _originalRigidbodyStates.Clear();
            
            var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            //.Log($"ApplyRagdollPhysics: Found {evaRigidbodies.Length} Rigidbody components");
            
            if (evaRigidbodies.Length == 0)
            {
               // Mod.Log("ApplyRagdollPhysics: No Rigidbody found! Creating ragdoll dynamically...");
                CreateRagdollDynamically(transform);
            }
            else
            {
                int i = 0;
                foreach (var rb in evaRigidbodies)
                {
                    //Mod.Log($"ApplyRagdollPhysics: Rigidbody[{i}] name={rb.gameObject.name}, isKinematic={rb.isKinematic}, mass={rb.mass}");
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
                
                var joints = _evaScript.GetComponentsInChildren<CharacterJoint>();
                //Mod.Log($"ApplyRagdollPhysics: Found {joints.Length} CharacterJoint components");
                foreach (var joint in joints)
                {
                    //Mod.Log($"  Joint: {joint.gameObject.name} -> {joint.connectedBody?.gameObject?.name ?? "null"}");
                }
                
                if (joints.Length == 0 && evaRigidbodies.Length > 1)
                {
                   // Mod.Log("ApplyRagdollPhysics: No CharacterJoints found! Adding dynamically...");
                    CreateRagdollDynamically(transform);
                }
            }
            
            //Mod.Log($"ApplyRagdollPhysics: Stored {_originalRigidbodyStates.Count} original rigidbody states");
            //Mod.Log("=== ApplyRagdollPhysics END ===");
        }

        private void CreateRagdollDynamically(Transform root)
        {
            //Mod.Log($"CreateRagdollDynamically: Starting from {root.name}");

            _skipBones.Clear();
            _skipBones.Add("EVAChestPlate");
            _skipBones.Add("EVAChestPlateVariant");
            _skipBones.Add("JetPackNozzleBottomLeft");
            _skipBones.Add("JetPackNozzleBottomRight");
            _skipBones.Add("JetPackNozzleTopLeft");
            _skipBones.Add("JetPackNozzleTopRight");
            _skipBones.Add("ParticleSystem");
            _skipBones.Add("ClickyCollider");
            
            Transform boneRoot = root.Find("Root/Offset");
            if (boneRoot == null) boneRoot = root.Find("Offset");
            if (boneRoot == null) boneRoot = root;
            
            //Mod.Log($"CreateRagdollDynamically: boneRoot = {boneRoot.name}");
            
            Transform hips = boneRoot.Find("Hips");
            if (hips == null) hips = boneRoot.Find("../Hips");
            //Mod.Log($"CreateRagdollDynamically: hips = {hips?.name}");
            
            Transform spine = hips?.Find("Spine");
           // Mod.Log($"CreateRagdollDynamically: spine = {spine?.name}");
            
            Transform chest = spine?.Find("Chest");
            //Mod.Log($"CreateRagdollDynamically: chest = {chest?.name}");
            
            Transform upperChest = chest?.Find("UpperChest");
           // Mod.Log($"CreateRagdollDynamically: upperChest = {upperChest?.name}");
            
            Transform neck = upperChest?.Find("Neck");
            Transform head = neck?.Find("Head");
            //Mod.Log($"CreateRagdollDynamically: neck = {neck?.name}, head = {head?.name}");
            
            Transform leftClavicle = upperChest?.Find("LeftClavicle");
            Transform leftShoulder = leftClavicle?.Find("LeftShoulder");
            Transform leftElbow = leftShoulder?.Find("LeftElbow");
            Transform leftHand = leftElbow?.Find("LeftHand");
           // Mod.Log($"CreateRagdollDynamically: leftClavicle={leftClavicle?.name}, leftShoulder={leftShoulder?.name}, leftElbow={leftElbow?.name}, leftHand={leftHand?.name}");
            
            Transform rightClavicle = upperChest?.Find("RightClavicle");
            Transform rightShoulder = rightClavicle?.Find("RightShoulder");
            Transform rightElbow = rightShoulder?.Find("RightElbow");
            Transform rightHand = rightElbow?.Find("RightHand");
            //Mod.Log($"CreateRagdollDynamically: rightClavicle={rightClavicle?.name}, rightShoulder={rightShoulder?.name}, rightElbow={rightElbow?.name}, rightHand={rightHand?.name}");
            
            Transform leftHip = hips?.Find("LeftHip");
            Transform leftKnee = leftHip?.Find("LeftKnee");
            Transform leftAnkle = leftKnee?.Find("LeftAnkle");
            Transform leftToes = leftAnkle?.Find("LeftToes");
            //Mod.Log($"CreateRagdollDynamically: leftHip={leftHip?.name}, leftKnee={leftKnee?.name}, leftAnkle={leftAnkle?.name}, leftToes={leftToes?.name}");
            
            Transform rightHip = hips?.Find("RightHip");
            Transform rightKnee = rightHip?.Find("RightKnee");
            Transform rightAnkle = rightKnee?.Find("RightAnkle");
            Transform rightToes = rightAnkle?.Find("RightToes");
           // Mod.Log($"CreateRagdollDynamically: rightHip={rightHip?.name}, rightKnee={rightKnee?.name}, rightAnkle={rightAnkle?.name}, rightToes={rightToes?.name}");
            
            _ragdollRoot = boneRoot;
            
            if (hips != null)
            {
                var rb = hips.gameObject.AddComponent<Rigidbody>();
                rb.mass = 25f;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                //Mod.Log($"CreateRagdollDynamically: Added Rigidbody to {hips.name}, mass={rb.mass}");
            }
            
            if (spine != null && hips != null)
            {
                AddRigidbodyAndJoint(spine.gameObject, hips.gameObject, 15f, 
                    swingLimit: 3f, twistLow: -5f, twistHigh: 5f, limbLength: 0.3f);
            }
            
            if (chest != null && spine != null)
            {
                AddRigidbodyAndJoint(chest.gameObject, spine.gameObject, 10f,
                    swingLimit: 5f, twistLow: -10f, twistHigh: 10f, limbLength: 0.25f);
            }
            
            if (upperChest != null && chest != null)
            {
                AddRigidbodyAndJoint(upperChest.gameObject, chest.gameObject, 8f,
                    swingLimit: 8f, twistLow: -15f, twistHigh: 15f, limbLength: 0.2f);
            }
            
            //Mod.Log($"CreateRagdollDynamically: Neck setup - neck={neck?.name ?? "NULL"}, upperChest={upperChest?.name ?? "NULL"}");
            if (neck != null && upperChest != null)
            {
               // Mod.Log($"CreateRagdollDynamically: Calling AddRigidbodyAndJoint for Neck");
                AddRigidbodyAndJoint(neck.gameObject, upperChest.gameObject, 2f,
                    swingLimit: 3f, twistLow: -5f, twistHigh: 5f, limbLength: 0.1f);
            }

           // Mod.Log($"CreateRagdollDynamically: Head setup - head={head?.name ?? "NULL"}, neck={neck?.name ?? "NULL"}");
            if (head != null && neck != null)
            {
               // Mod.Log($"CreateRagdollDynamically: Calling AddRigidbodyAndJoint for Head");
                AddRigidbodyAndJoint(head.gameObject, neck.gameObject, 5f,
                    swingLimit: 15f, twistLow: -20f, twistHigh: 20f, limbLength: 0.15f);
               // Mod.Log($"CreateRagdollDynamically: After AddRigidbodyAndJoint for Head");
            }
            else if (head != null && upperChest != null)
            {
              //  Mod.Log($"CreateRagdollDynamically: Falling back to Head -> UpperChest");
                AddRigidbodyAndJoint(head.gameObject, upperChest.gameObject, 5f,
                    swingLimit: 15f, twistLow: -20f, twistHigh: 20f, limbLength: 0.15f);
            }
            else
            {
                //Mod.Log($"CreateRagdollDynamically: Head NOT connected - head={head?.name ?? "NULL"}, neck={neck?.name ?? "NULL"}, upperChest={upperChest?.name ?? "NULL"}");
            }
            
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
            
            if (leftHip != null && hips != null)
            {
                AddRigidbodyAndJoint(leftHip.gameObject, hips.gameObject, 5f,
                    swingLimit: 10f, twistLow: -5f, twistHigh: 5f, limbLength: 0.12f);
            }
            if (leftKnee != null && leftHip != null)
            {
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
            
            var allRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            //Mod.Log($"CreateRagdollDynamically: Created {allRigidbodies.Length} Rigidbody components");
            foreach (var rb in allRigidbodies)
            {
                var joint = rb.GetComponent<CharacterJoint>();
                //Mod.Log($"  - {rb.gameObject.name} has Rigidbody, mass={rb.mass}, connectedTo={joint?.connectedBody?.gameObject?.name ?? "NONE (root)"}");
            }
            
            DisableRagdollSelfCollisions();
            
            _ragdollPhysicsCreated = true;
        }
        
        private void DisableRagdollSelfCollisions()
        {
            if (_evaScript == null) return;
            
            var allColliders = _evaScript.GetComponentsInChildren<Collider>();
            int ignoredPairs = 0;
            
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
            
            //Mod.Log($"DisableRagdollSelfCollisions: Ignored {ignoredPairs} collision pairs between {allColliders.Length} ragdoll colliders");
        }

        private void AddRigidbodyAndJoint(GameObject bone, GameObject parentBone, float mass,
            float swingLimit = 45f, float twistLow = -50f, float twistHigh = 50f,
            float limbLength = 0.2f, bool isHinge = false)
        {
            //Mod.Log($"AddRigidbodyAndJoint ENTER: bone={bone?.name ?? "NULL"}, parentBone={parentBone?.name ?? "NULL"}, mass={mass}");
            if (bone == null || parentBone == null)
            {
                return;
            }
            
            var rb = bone.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            AddBoneCollider(bone, parentBone, limbLength);
            
            var joint = bone.AddComponent<CharacterJoint>();
            joint.connectedBody = parentBone.GetComponent<Rigidbody>();
            
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
            
            joint.enablePreprocessing = true;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
            
           // Mod.Log($"AddRigidbodyAndJoint: {bone.name} -> {parentBone.name}, mass={mass}, swing={actualSwingLimit}");
           // Mod.Log($"AddRigidbodyAndJoint EXIT: {bone.name} completed successfully");
        }
        
        private void AddBoneCollider(GameObject bone, GameObject parentBone, float limbLength)
        {
            if (bone == null) return;
            
            if (_skipBones.Contains(bone.name))
            {
                //Mod.Log($"AddBoneCollider: Skipping decorative bone {bone.name}");
                return;
            }
            
            var existingCollider = bone.GetComponent<Collider>();
            if (existingCollider != null)
            {
                GameObject.Destroy(existingCollider);
            }
            
            var col = bone.AddComponent<CapsuleCollider>();
            
            float height = limbLength * 2.2f;
            float radius = limbLength * 0.25f;
            
            radius = Mathf.Max(0.04f, radius);
            height = Mathf.Max(0.08f, height);
            
            col.radius = radius;
            col.height = height;
            
            if (parentBone != null)
            {
                Vector3 dirToParent = (parentBone.transform.position - bone.transform.position).normalized;
                
                float yAlign = Mathf.Abs(Vector3.Dot(dirToParent, Vector3.up));
                float xAlign = Mathf.Abs(Vector3.Dot(dirToParent, Vector3.right));
                float zAlign = Mathf.Abs(Vector3.Dot(dirToParent, Vector3.forward));
                
                if (yAlign > xAlign && yAlign > zAlign)
                {
                    col.direction = 1;
                }
                else if (xAlign > zAlign)
                {
                    col.direction = 0;
                }
                else
                {
                    col.direction = 2;
                }
            }
            else
            {
                col.direction = 1;
            }
            
            col.center = Vector3.zero;
        }

        #endregion

        #region IK Management

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

        #endregion

        #region Flight Loop

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
           
            var animator = _evaScript.GetComponent<Animator>();
            /*
            animator.enabled = animationEnabled;
            _pilotIK.enabled = ikEnabled;*/
            
            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }
            
            if (_pilotIK != null && _pilotIK.enabled)
            {
                // Mod.Log("FlightUpdate: _pilotIK was re-enabled, disabling again");
                _pilotIK.enabled = false;
            }
            
            var childAnimators = _evaScript.GetComponentsInChildren<Animator>();
            foreach (var anim in childAnimators)
            { 
                anim.enabled = animationEnabled2;

            }
            
        }
        
       


        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (_evaScript == null) return;
           
            if (_wasPaused && _isRagdollActive)
            {
                OnUnpaused();
            }
            
            if (!_isRagdollActive || _evaScript == null) return;
            
            EnsureRagdollPhysicsActive();
            
            /*
            var evaRigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in evaRigidbodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }*/
        }

        void IFlightUpdatePaused.FlightUpdatePaused(in FlightFrameData frame)
        {
            if (!_isRagdollActive || _evaScript == null) return;
            OnPaused();
        }

        #endregion

        #region Pause Management

        private void OnPaused()
        {
            if (!_wasPaused)
            {
               
                
                ForceDisableAnimatorAndIK();
                SaveCurrentBonePositions();
                
                _wasPaused = true;
            }
        }

        private void OnUnpaused()
        {
            if (_wasPaused)
            {
                
                ForceDisableAnimatorAndIK();
                RestoreBonePositions();
                
                _wasPaused = false;
            }
        }

        #endregion

        #region Bone State Management

        private void SaveCurrentBonePositions()
        {
            if (_evaScript == null) return;
            
            _pausedBoneStates.Clear();
            var rigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                _pausedBoneStates.Add(new BoneTransform(
                    rb.transform.name,
                    rb.transform.localPosition,
                    rb.transform.localRotation
                ));
            }
            
        }

        private void RestoreBonePositions()
        {
            if (_evaScript == null || _pausedBoneStates.Count == 0) return;
            
            var rigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            int restored = 0;
            
            foreach (var rb in rigidbodies)
            {
                var savedState = _pausedBoneStates.FirstOrDefault(s => s.Name == rb.transform.name);
                if (savedState != null)
                {
                    rb.transform.localPosition = savedState.Position;
                    rb.transform.localRotation = savedState.Rotation;
                    
                    rb.position = rb.transform.position;
                    rb.rotation = rb.transform.rotation;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    
                    restored++;
                }
            }
            
        }

        private void ForceDisableAnimatorAndIK()
        {
            if (_evaScript == null) return;
            
            var animator = _evaScript.GetComponent<Animator>();
            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }
            
            var childAnimators = _evaScript.GetComponentsInChildren<Animator>();
            foreach (var anim in childAnimators)
            {
                if (anim.enabled)
                {
                    anim.enabled = false;
                }
            }
            
            if (_pilotIK != null && _pilotIK.enabled)
            {
                _pilotIK.enabled = false;
            }
        }

        private void EnsureRagdollPhysicsActive()
        {
            if (_evaScript == null || !_isRagdollActive) return;
            
            var rigidbodies = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                if (rb.isKinematic)
                {
                    rb.isKinematic = false;
                }
            }
            
            ForceDisableAnimatorAndIK();
        }

        #endregion
        
        
    }
}
