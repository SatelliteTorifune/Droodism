using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Flight;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Scenes.Events;
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
        
        private FullBodyBipedIK _pilotIK;
        private EvaScript _evaScript;

        private bool _isRagdollActive = false;
        private bool _wasPaused = false;
        private bool _ragdollPhysicsCreated = false;
        private Vector3 _preservedVelocity = Vector3.zero;

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



        private readonly List<GameObject> _dynamicallyAddedBones = new();
        private readonly Dictionary<GameObject, Collider> _replacedColliders = new();

        // Cached transforms for spring constraint between CharacterCollider and Hips
        private Transform _characterColliderTransform;
        private Transform _hipsTransform;

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
            public Vector3 Velocity;
            public Vector3 AngularVelocity;

            public BoneTransform(string name, Vector3 localPos, Quaternion localRot,
                Vector3 velocity, Vector3 angularVelocity)
            {
                Name = name;
                LocalPosition = localPos;
                LocalRotation = localRot;
                Velocity = velocity;
                AngularVelocity = angularVelocity;
            }
        }

        #endregion

        #region Inspector

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);

            var group = new GroupModel(Locale.GetString("Droodism.RagdollModifier.Ragdoll"));
            model.AddGroup(group);

            group.Add(new TextButtonModel(Locale.GetString("Droodism.RagdollModifier.Enable"), b => EnableRagdollMode()));
            group.Add(new TextButtonModel(Locale.GetString("Droodism.RagdollModifier.Disable"), b => DisableRagdollMode()));
            group.Add(new TextModel(Locale.GetString("Droodism.RagdollModifier.Status"), () => Data.EnableRagdoll ? Locale.GetString("Droodism.RagdollModifier.Active") : Locale.GetString("Droodism.RagdollModifier.Inactive")));
            //group.Add(new ToggleModel("操",()=>cnm,(b)=>cnm=b));
            
        }

        #endregion

       
        
        #region Public API

        
        public void EnableRagdollMode()
        {
            if (_isRagdollActive) return;

            _wasPaused = false;
            _characterColliderTransform = _evaScript.transform.Find("CharacterCollider");
            _hipsTransform = _evaScript.transform.Find("Root").Find("Offset").Find("Hips");
            if (_characterColliderTransform == null || _hipsTransform == null) return;

            // Align Hips to CharacterCollider position before activating ragdoll physics
            _hipsTransform.position = _characterColliderTransform.position;

            if (_evaScript != null && _pilotIK != null)
            {
                CaptureCraftVelocity();
                ApplyRagdollPhysics();
            }

            _isRagdollActive = true;
            Data.EnableRagdoll = true;
            
        }

        public void DisableRagdollMode()
        {
            if (!_isRagdollActive) return;

            _isRagdollActive = false;
            _ragdollPhysicsCreated = false;
            _wasPaused = false;
            
            Data.EnableRagdoll = false;

            RestoreEvaScriptIK();
            DestroyRagdollPhysics();
            _originalRigidbodyStates.Clear();
            var tm = Game.Instance.FlightScene.TimeManager;
            tm.RequestPauseChange(true, false);
            tm.RequestPauseChange(false, false);
        }

        private void DestroyRagdollPhysics()
        {
            foreach (var go in _dynamicallyAddedBones)
            {
                if (go == null) continue;

                foreach (var joint in go.GetComponents<CharacterJoint>())
                  Destroy(joint);

                foreach (var col in go.GetComponents<CapsuleCollider>())
                  Destroy(col);
                foreach (var col in go.GetComponents<SphereCollider>())
                  Destroy(col);
                foreach (var col in go.GetComponents<BoxCollider>())
                  Destroy(col);

                foreach (var rb in go.GetComponents<Rigidbody>())
                  Destroy(rb);
            }

            foreach (var kvp in _replacedColliders)
            {
                if (kvp.Key == null || kvp.Value == null) continue;
              Destroy(kvp.Value);
                var newCol = kvp.Key.GetComponent<Collider>();
                if (newCol != null) UnityEngine.Object.Destroy(newCol);
            }
            _replacedColliders.Clear();
            _dynamicallyAddedBones.Clear();

        }

        #endregion

        #region Ragdoll Physics

        private void CaptureCraftVelocity()
        {
            if (PartScript?.CraftScript?.FlightData != null)
            {
                _preservedVelocity = PartScript.CraftScript.FlightData.SurfaceVelocity.ToVector3();
            }
        }

        private void ApplyPreservedVelocity()
        {
            if (_evaScript == null) return;
            if(Game.Instance.FlightScene.ViewManager.GameView.ReferenceFrame.IsSurfaceLocked&&PartScript.CraftScript.CraftNode.Parent.PlanetData.SurfaceGravity > 4.0)
            {
                return;
            }
            foreach (var rb in _evaScript.GetComponentsInChildren<Rigidbody>())
            {
                rb.velocity = _preservedVelocity;
                rb.angularVelocity = Vector3.zero;
            }
        }

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
            ApplyPreservedVelocity();
            DisableRagdollSelfCollisions();
            _ragdollPhysicsCreated = true;
        }

        private void CreateRagdollDynamically(Transform root)
        {
            // Get the part's design mass so ragdoll total mass matches the part.
            float partMass = PartScript?.Data?.Mass ?? 100f;
            // Base total of all hardcoded bone masses below
            const float baseTotalMass = 102.4f;
            float massScale = partMass / baseTotalMass;

            _dynamicallyAddedBones.Clear();
            var boneRoot = root.Find("Root/Offset") ?? root.Find("Offset") ?? root;

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
                rb.mass = 25f * massScale;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                // Add a CapsuleCollider to Hips so it doesn't clip through the ground.
                // Without this, Hips (as the root ragdoll bone) has no collision and falls freely.
                var hipsCol = hips.gameObject.AddComponent<CapsuleCollider>();
                hipsCol.radius = 0.15f;
                hipsCol.height = 0.4f;
                hipsCol.center = new Vector3(0f, 0.1f, 0f);
                hipsCol.direction = 1;

                _dynamicallyAddedBones.Add(hips.gameObject);

            }

            void AddRBJoint(Transform child, Transform parent, float baseMass,
                float swingLimit = 20f, float twistLow = -30f, float twistHigh = 30f,
                float limbLen = 0.2f, bool isHinge = false)
            {
                if (child == null || parent == null) return;

                var rb = child.gameObject.AddComponent<Rigidbody>();
                rb.mass = baseMass * massScale;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;


                _dynamicallyAddedBones.Add(child.gameObject);


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

            if (existing != null)
            {
                _replacedColliders[bone] = existing;
              Destroy(existing);
            }


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

            // Also make CharacterCollider (which is NOT under the bone hierarchy) ignore ragdoll colliders.
            // CharacterCollider stays at the craft/part level and should not double-collide with bones.
            if (_characterColliderTransform != null)
            {
                var ccCols = _characterColliderTransform.GetComponentsInChildren<Collider>();
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

        void Awake()
        {
            Game.Instance.SceneManager.SceneTransitionCompleted += OnSceneTransitionCompleted;
        }

        void OnSceneTransitionCompleted(object sender, SceneTransitionEventArgs e)
        {
            if (e.TransitionToScene!="Flight")
            {
                return;
            }
            
            if (Data.EnableRagdoll)
            {
                Mod.Log("OnSceneTransitionCompleted call Enabled");
                DisableRagdollMode();
                EnableRagdollMode();
            }
        }
        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _evaScript = PartScript.GetModifier<EvaScript>();
            _pilotIK = _evaScript?.GetComponentInChildren<FullBodyBipedIK>();
            _transformInfoScript = _evaScript?.GetComponent<TransformInfoScript>();
            _isRagdollActive = Data.EnableRagdoll;
            
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!_isRagdollActive || _hipsTransform == null || _characterColliderTransform == null)
                return;

            // 20m 边界检测：如果碰撞箱和视觉模型分离超过 20m，强制归中
            float dist = Vector3.Distance(_hipsTransform.position, _characterColliderTransform.position);
            if (dist > 20f)
            {
                Mod.Log($"[Ragdoll] Boundary exceeded ({dist:F1}m > 20m), hard-syncing Hips → CharacterCollider");
                _hipsTransform.position = _characterColliderTransform.position;

                // Also zero out velocities on all ragdoll Rigidbodies to prevent bounce-back
                foreach (var rb in _hipsTransform.GetComponentsInChildren<Rigidbody>())
                {
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (_evaScript == null||!_isRagdollActive) return;
            if (_wasPaused && _isRagdollActive)
                OnUnpaused();
            
            var rbs = _evaScript.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                if (rb.isKinematic)
                    rb.isKinematic = false;
            }

            // 弹性归中：用弹簧力把 Hips 拉向 CharacterCollider
            // 让布娃娃在 20m 范围内自由物理运动，同时防止无限分离
            if (_hipsTransform != null && _characterColliderTransform != null)
            {
                var hipsRb = _hipsTransform.GetComponent<Rigidbody>();
                if (hipsRb != null)
                {
                    Vector3 toTarget = _characterColliderTransform.position - _hipsTransform.position;
                    float dist = toTarget.magnitude;
                    if (dist > 0.5f && dist < 20f)
                    {
                        // 弹簧系数随距离增大，最大 50
                        float springK = Mathf.Min(dist * 2.5f, 50f);
                        hipsRb.AddForce(toTarget.normalized * springK, ForceMode.Acceleration);
                    }
                }
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
                _pausedBoneStates.Add(new BoneTransform(
                    rb.transform.name, rb.transform.localPosition, rb.transform.localRotation,
                    rb.velocity, rb.angularVelocity));
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
                    rb.velocity = saved.Velocity;
                    rb.angularVelocity = saved.AngularVelocity;
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