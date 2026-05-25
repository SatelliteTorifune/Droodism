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
    public class RagdollModifierScript : PartModifierScript<RagdollModifierData>,
        IFlightUpdate,
        IFlightFixedUpdate,
        IFlightUpdatePaused,
        IFlightStart
    {
        #region Fields

        private CrewCompartmentScript _crewCompartment;
        private FullBodyBipedIK _pilotIK;
        private EvaScript _evaScript;

        private bool _isRagdollActive = false;
        private bool _wasPaused = false;
        private bool _ragdollPhysicsCreated = false;

        private IKSavedWeights _savedWeights = new();
        private Transform _ragdollRoot;
        private TransformInfoScript _transformInfoScript;

        private readonly HashSet<string> _skipBones = new(StringComparer.OrdinalIgnoreCase)
        {
            "EVAChestPlate",
            "EVAChestPlateVariant",
            "JetPackNozzleBottomLeft",
            "JetPackNozzleBottomRight",
            "JetPackNozzleTopLeft",
            "JetPackNozzleTopRight",
            "ParticleSystem",
            "ClickyCollider"
        };

        private readonly List<RigidbodyState> _originalRigidbodyStates = new();
        private readonly List<BoneTransform> _pausedBoneStates = new();
        private readonly HashSet<(string, string)> _loggedCollisions = new();

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
            public float RightHandPosWeight, RightHandRotWeight;
            public float LeftHandPosWeight, LeftHandRotWeight;
            public float RightFootPosWeight, RightFootRotWeight;
            public float LeftFootPosWeight, LeftFootRotWeight;
        }

        private class BoneTransform
        {
            public string Name;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;

            public BoneTransform(string name, Vector3 localPos, Quaternion localRot)
            {
                Name = name;
                LocalPosition = localPos;
                LocalRotation = localRot;
            }
        }

        #endregion

        #region Inspector

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);

            var group = new GroupModel("Ragdoll");
            model.AddGroup(group);

            group.Add(new TextButtonModel("启用", b => EnableRagdollMode()));
            group.Add(new TextButtonModel("禁用", b => DisableRagdollMode()));
            group.Add(new TextModel("Status", () => _isRagdollActive ? "Active" : "Inactive"));
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

            if (!Game.InFlightScene) return;

            if (_crewCompartment != null)
            {
                _crewCompartment.CrewEnter -= OnCrewEnter;
                _crewCompartment.CrewExit -= OnCrewExit;
                _crewCompartment.CrewEnter += OnCrewEnter;
                _crewCompartment.CrewExit += OnCrewExit;
            }

            if (!_isRagdollActive)
                RefreshPilotReferences();
        }

        #endregion

        #region Crew Management

        private void OnCrewEnter(EvaScript crew)
        {
            RefreshPilotReferencesFromCrew(crew);
        }

        private void OnCrewExit(EvaScript crew)
        {
            if (!_isRagdollActive)
                ClearPilotReferences();
        }

        private void RefreshPilotReferences()
        {
            if (_crewCompartment == null) return;
            foreach (var crew in _crewCompartment.Crew)
                RefreshPilotReferencesFromCrew(crew);
        }

        private void RefreshPilotReferencesFromCrew(EvaScript crew)
        {
            _evaScript = crew;
            _pilotIK = crew.GetComponentInChildren<FullBodyBipedIK>();
            _transformInfoScript = crew.GetComponent<TransformInfoScript>();

            if (_isRagdollActive)
                ApplyRagdollPhysics();
        }

        private void ClearPilotReferences()
        {
            _evaScript = null;
            _pilotIK = null;
            _transformInfoScript = null;
        }

        private void FindCrew()
        {
            if (_crewCompartment != null && _crewCompartment.Crew.Count > 0)
            {
                SetCrew(_crewCompartment.Crew[0]);
                return;
            }

            var cc = PartScript.GetModifier<CrewCompartmentScript>();
            if (cc != null && cc.Crew.Count > 0)
            {
                _crewCompartment = cc;
                SetCrew(cc.Crew[0]);
                return;
            }

            var eva = PartScript.GetModifier<EvaScript>();
            if (eva != null)
                SetCrew(eva);
        }

        private void SetCrew(EvaScript crew)
        {
            _evaScript = crew;
            _pilotIK ??= crew.GetComponent<FullBodyBipedIK>() ?? crew.GetComponentInChildren<FullBodyBipedIK>();
            _transformInfoScript ??= crew.GetComponent<TransformInfoScript>();
        }

        #endregion

        #region Public API

        public void SetRagdollMode(bool enable)
        {
            if (enable) EnableRagdollMode();
            else DisableRagdollMode();
        }

        public void EnableRagdollMode()
        {
            if (_isRagdollActive) return;

            _wasPaused = false;

            if (_evaScript == null || _pilotIK == null)
                FindCrew();

            _isRagdollActive = true;
            Data.EnableRagdoll = true;

            if (_evaScript != null && _pilotIK != null)
                ApplyRagdollPhysics();
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
            if (_evaScript == null) return;

            var animator = _evaScript.GetComponent<Animator>();
            if (animator != null)
                animator.enabled = false;

            if (_pilotIK != null)
            {
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
            }

            // Disable TransformInfoScript so it doesn't fight the ragdoll
            if (_transformInfoScript != null)
                _transformInfoScript.enabled = false;

            _originalRigidbodyStates.Clear();

            var existingRBs = _evaScript.GetComponentsInChildren<Rigidbody>();
            if (existingRBs.Length > 0)
            {
                foreach (var rb in existingRBs)
                {
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
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                var joints = _evaScript.GetComponentsInChildren<CharacterJoint>();
                if (joints.Length == 0)
                    CreateRagdollDynamically(_evaScript.transform);
            }
            else
            {
                CreateRagdollDynamically(_evaScript.transform);
            }

            ZeroRagdollVelocities();
            DisableRagdollSelfCollisions();
            AttachCollisionLogger();
            _ragdollPhysicsCreated = true;
        }

        private void CreateRagdollDynamically(Transform root)
        {
            Transform boneRoot = root.Find("Root/Offset") ?? root.Find("Offset") ?? root;
            _ragdollRoot = boneRoot;

            Transform hips = boneRoot.Find("Hips");
            Transform spine = hips?.Find("Spine");
            Transform chest = spine?.Find("Chest");
            Transform upperChest = chest?.Find("UpperChest");
            Transform neck = upperChest?.Find("Neck");
            Transform head = neck?.Find("Head");

            Transform lClav = upperChest?.Find("LeftClavicle");
            Transform lShoulder = lClav?.Find("LeftShoulder");
            Transform lElbow = lShoulder?.Find("LeftElbow");
            Transform lHand = lElbow?.Find("LeftHand");

            Transform rClav = upperChest?.Find("RightClavicle");
            Transform rShoulder = rClav?.Find("RightShoulder");
            Transform rElbow = rShoulder?.Find("RightElbow");
            Transform rHand = rElbow?.Find("RightHand");

            Transform lHip = hips?.Find("LeftHip");
            Transform lKnee = lHip?.Find("LeftKnee");
            Transform lAnkle = lKnee?.Find("LeftAnkle");
            Transform lToes = lAnkle?.Find("LeftToes");

            Transform rHip = hips?.Find("RightHip");
            Transform rKnee = rHip?.Find("RightKnee");
            Transform rAnkle = rKnee?.Find("RightAnkle");
            Transform rToes = rAnkle?.Find("RightToes");

            if (hips != null)
            {
                var rb = hips.gameObject.AddComponent<Rigidbody>();
                rb.mass = 25f;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            void AddRBJoint(Transform child, Transform parent, float mass,
                float swingLimit = 20f, float twistLow = -30f, float twistHigh = 30f,
                float limbLen = 0.2f, bool isHinge = false)
            {
                if (child == null || parent == null) return;

                var rb = child.gameObject.AddComponent<Rigidbody>();
                rb.mass = mass;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                AddBoneCollider(child.gameObject, parent.gameObject, limbLen);

                var joint = child.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parent.GetComponent<Rigidbody>();

                float sw = isHinge ? 0f : swingLimit;
                joint.swing1Limit = new SoftJointLimit { limit = sw };
                joint.swing2Limit = new SoftJointLimit { limit = sw };
                joint.lowTwistLimit = new SoftJointLimit { limit = twistLow };
                joint.highTwistLimit = new SoftJointLimit { limit = twistHigh };
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;
                joint.enablePreprocessing = true;
            }

            // Spine chain
            AddRBJoint(spine, hips, 15f, 10f, -10f, 10f, 0.3f);
            AddRBJoint(chest, spine, 10f, 10f, -15f, 15f, 0.25f);
            AddRBJoint(upperChest, chest, 8f, 15f, -20f, 20f, 0.2f);

            // Neck and head
            if (neck != null)
            {
                AddRBJoint(neck, upperChest, 2f, 5f, -10f, 10f, 0.1f);
                if (head != null)
                    AddRBJoint(head, neck, 5f, 20f, -30f, 30f, 0.15f);
            }
            else if (head != null && upperChest != null)
                AddRBJoint(head, upperChest, 5f, 20f, -30f, 30f, 0.15f);

            // Left arm
            AddRBJoint(lClav, upperChest, 3f, 15f, -15f, 15f, 0.1f);
            AddRBJoint(lShoulder, lClav, 3f, 40f, -40f, 40f, 0.25f);
            AddRBJoint(lElbow, lShoulder, 2f, 0f, 0f, 90f, 0.25f, isHinge: true);
            AddRBJoint(lHand, lElbow, 0.5f, 0f, -10f, 10f, 0.08f);

            // Right arm
            AddRBJoint(rClav, upperChest, 3f, 15f, -15f, 15f, 0.1f);
            AddRBJoint(rShoulder, rClav, 3f, 40f, -40f, 40f, 0.25f);
            AddRBJoint(rElbow, rShoulder, 2f, 0f, 0f, 90f, 0.25f, isHinge: true);
            AddRBJoint(rHand, rElbow, 0.5f, 0f, -10f, 10f, 0.08f);

            // Left leg
            AddRBJoint(lHip, hips, 5f, 15f, -10f, 10f, 0.12f);
            AddRBJoint(lKnee, lHip, 3f, 0f, 0f, 5f, 0.35f, isHinge: true);
            AddRBJoint(lAnkle, lKnee, 2f, 10f, -15f, 15f, 0.25f);
            AddRBJoint(lToes, lAnkle, 0.2f, 0f, 0f, 0f, 0.04f);

            // Right leg
            AddRBJoint(rHip, hips, 5f, 15f, -10f, 10f, 0.12f);
            AddRBJoint(rKnee, rHip, 3f, 0f, 0f, 5f, 0.35f, isHinge: true);
            AddRBJoint(rAnkle, rKnee, 2f, 10f, -15f, 15f, 0.25f);
            AddRBJoint(rToes, rAnkle, 0.2f, 0f, 0f, 0f, 0.04f);
        }

        private void AddBoneCollider(GameObject bone, GameObject parentBone, float limbLength)
        {
            if (bone == null || _skipBones.Contains(bone.name)) return;

            var existing = bone.GetComponent<Collider>();
            if (existing != null) UnityEngine.Object.Destroy(existing);

            var col = bone.AddComponent<CapsuleCollider>();
            col.radius = Mathf.Max(0.04f, limbLength * 0.25f);
            col.height = Mathf.Max(0.08f, limbLength * 2.2f);
            col.center = Vector3.zero;

            if (parentBone != null)
            {
                Vector3 dir = (parentBone.transform.position - bone.transform.position).normalized;
                float yA = Mathf.Abs(Vector3.Dot(dir, Vector3.up));
                float xA = Mathf.Abs(Vector3.Dot(dir, Vector3.right));
                col.direction = (yA > xA && yA > Mathf.Abs(Vector3.Dot(dir, Vector3.forward))) ? 1 : 0;
            }
            else
            {
                col.direction = 1;
            }
        }

        private void DisableRagdollSelfCollisions()
        {
            if (_evaScript == null) return;

            var ragdollCols = _evaScript.GetComponentsInChildren<Collider>();

            // Ignore self-collisions among ragdoll bones
            for (int i = 0; i < ragdollCols.Length; i++)
            {
                for (int j = i + 1; j < ragdollCols.Length; j++)
                {
                    if (ragdollCols[i] != null && ragdollCols[j] != null)
                        Physics.IgnoreCollision(ragdollCols[i], ragdollCols[j], true);
                }
            }

            // Keep CharacterCollider enabled (so craft doesn't fall through ground),
            // but make it ignore all ragdoll CapsuleColliders
            var characterCollider = _evaScript.transform.Find("CharacterCollider");
            if (characterCollider != null)
            {
                var ccCols = characterCollider.GetComponentsInChildren<Collider>();
                foreach (var cc in ccCols)
                {
                    if (cc == null) continue;
                    foreach (var rc in ragdollCols)
                    {
                        if (rc != null)
                            Physics.IgnoreCollision(cc, rc, true);
                    }
                }
            }
        }

        private void ZeroRagdollVelocities()
        {
            if (_evaScript == null) return;
            foreach (var rb in _evaScript.GetComponentsInChildren<Rigidbody>())
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void AttachCollisionLogger()
        {
            if (_evaScript == null) return;
            _loggedCollisions.Clear();

            var ragdollColliders = _evaScript.GetComponentsInChildren<Collider>();
            foreach (var col in ragdollColliders)
            {
                var go = col.gameObject;
                if (go.GetComponent<RagdollCollisionLogger>() != null) continue;

                var logger = go.AddComponent<RagdollCollisionLogger>();
                logger.Initialize(_loggedCollisions, _isRagdollActive);
            }
        }

        #endregion

        #region Bone Sync

        private void SyncBoneTransforms()
        {
            // Rigidbody and transform positions are always in sync in Unity.
            // No manual sync needed - SkinnedMeshRenderer reads bone transform
            // matrices directly. Force-setting positions per-frame can conflict
            // with RigidbodyInterpolation and cause jitter.
        }

        #endregion

        #region IK Management

        private void SaveIKWeights()
        {
            if (_pilotIK == null) return;

            var s = _pilotIK.solver;
            _savedWeights = new IKSavedWeights
            {
                RightHandPosWeight = s.rightHandEffector.positionWeight,
                RightHandRotWeight = s.rightHandEffector.rotationWeight,
                LeftHandPosWeight = s.leftHandEffector.positionWeight,
                LeftHandRotWeight = s.leftHandEffector.rotationWeight,
                RightFootPosWeight = s.rightFootEffector.positionWeight,
                RightFootRotWeight = s.rightFootEffector.rotationWeight,
                LeftFootPosWeight = s.leftFootEffector.positionWeight,
                LeftFootRotWeight = s.leftFootEffector.rotationWeight
            };
        }

        private void RestoreEvaScriptIK()
        {
            if (_pilotIK == null) return;

            _pilotIK.enabled = true;

            var s = _pilotIK.solver;
            s.rightHandEffector.positionWeight = _savedWeights.RightHandPosWeight;
            s.rightHandEffector.rotationWeight = _savedWeights.RightHandRotWeight;
            s.leftHandEffector.positionWeight = _savedWeights.LeftHandPosWeight;
            s.leftHandEffector.rotationWeight = _savedWeights.LeftHandRotWeight;
            s.rightFootEffector.positionWeight = _savedWeights.RightFootPosWeight;
            s.rightFootEffector.rotationWeight = _savedWeights.RightFootRotWeight;
            s.leftFootEffector.positionWeight = _savedWeights.LeftFootPosWeight;
            s.leftFootEffector.rotationWeight = _savedWeights.LeftFootRotWeight;

            if (_evaScript != null)
            {
                var animator = _evaScript.GetComponent<Animator>();
                if (animator != null) animator.enabled = true;

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

            // Restore TransformInfoScript
            if (_transformInfoScript != null)
                _transformInfoScript.enabled = true;
        }

        #endregion

        #region Flight Loop

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _evaScript = PartScript.GetModifier<EvaScript>();
            _pilotIK = _evaScript?.GetComponentInChildren<FullBodyBipedIK>();
            _transformInfoScript = _evaScript?.GetComponent<TransformInfoScript>();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            // Ragdoll bones are children of the craft in local space.
            // Physics runs in world space; bones move with craft automatically.
            // Do NOT move the craft to follow ragdoll - it creates a feedback loop
            // where craft teleportation fights rigidbody velocity.
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (_evaScript == null) return;

            if (_wasPaused && _isRagdollActive)
                OnUnpaused();

            if (!_isRagdollActive) return;

            // Keep ragdoll physics active
            var rbs = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                if (rb.isKinematic)
                    rb.isKinematic = false;
            }

            ForceDisableAnimatorAndIK();
        }

        void IFlightUpdatePaused.FlightUpdatePaused(in FlightFrameData frame)
        {
            if (_evaScript == null || !_isRagdollActive) return;
            OnPaused();
        }

        #endregion

        #region Pause Management

        private void OnPaused()
        {
            if (_wasPaused) return;

            ForceDisableAnimatorAndIK();
            SaveCurrentBonePositions();
            _wasPaused = true;
        }

        private void OnUnpaused()
        {
            if (!_wasPaused) return;

            ForceDisableAnimatorAndIK();
            RestoreBonePositions();
            _wasPaused = false;
        }

        private void SaveCurrentBonePositions()
        {
            if (_evaScript == null) return;

            _pausedBoneStates.Clear();
            var rbs = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                _pausedBoneStates.Add(new BoneTransform(rb.transform.name, rb.transform.localPosition, rb.transform.localRotation));
            }
        }

        private void RestoreBonePositions()
        {
            if (_evaScript == null || _pausedBoneStates.Count == 0) return;

            var rbs = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                var saved = _pausedBoneStates.FirstOrDefault(s => s.Name == rb.transform.name);
                if (saved != null)
                {
                    rb.transform.localPosition = saved.LocalPosition;
                    rb.transform.localRotation = saved.LocalRotation;
                    rb.position = rb.transform.position;
                    rb.rotation = rb.transform.rotation;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void ForceDisableAnimatorAndIK()
        {
            if (_evaScript == null) return;

            var animator = _evaScript.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            foreach (var a in _evaScript.GetComponentsInChildren<Animator>())
                if (a.enabled) a.enabled = false;

            if (_pilotIK != null && _pilotIK.enabled)
                _pilotIK.enabled = false;
        }

        #endregion
    }
}
