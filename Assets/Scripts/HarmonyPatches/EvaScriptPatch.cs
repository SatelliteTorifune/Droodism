using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using HarmonyLib;
using ModApi;
using ModApi.GameLoop;
using System.Reflection;
using Assets.Scripts.Craft.Parts.Modifiers;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace Assets.Scripts.HarmonyPatches
{
    /// <summary>
    /// Patches for EvaScript to disable animation/movement when ragdoll is active.
    /// We patch internal private methods that would interfere with ragdoll physics,
    /// while keeping collision tracking and state management intact.
    /// </summary>
    public class EvaScriptPatch
    {
        // Helper method to check if ragdoll is active
        private static bool IsRagdollActive(EvaScript eva)
        {
            if (!Game.InFlightScene) return false;
            return eva?.PartScript?.GetModifier<RagdollModifierScript>().Data.EnableRagdoll ?? false;
        }

        /// <summary>
        /// Skip movement calculations when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class UpdateMovement_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateMovement");
            }

            public static bool Prefix(EvaScript __instance, out Vector3 totalForce, out Vector3 totalForceJetpack)
            {
                totalForce = Vector3.Zero;
                totalForceJetpack = Vector3.Zero;
                
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                
                return true;
            }
        }

        /// <summary>
        /// Skip animation controller updates when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class UpdateAnimationController_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateAnimationController");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                
                return true;
            }
        }

        /// <summary>
        /// Skip kinematic turning when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class UpdateKinematicTurning_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateKinematicTurning");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                
                return true;
            }
        }
/*
        /// <summary>
        /// Skip upright character when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class UprightCharacter_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UprightCharacter");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                return true;
            }
        }*/

        /// <summary>
        /// Skip SlowDownCharacter when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class SlowDownCharacter_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "SlowDownCharacter");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Skip UpdateControllerColiderParent when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class UpdateControllerColiderParent_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateControllerColiderParent");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Skip ProcessCompletedPhysicsCycle when ragdoll is active.
        /// This coroutine calls UprightCharacter after FixedUpdate.
        /// </summary>
        [HarmonyPatch]
        public class ProcessCompletedPhysicsCycle_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "ProcessCompletedPhysicsCycle");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Skip jump state updates when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class UpdateJumpState_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateJumpState");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                
                return true;
            }
        }

        /// <summary>
        /// Skip grappling hook updates when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class UpdateGrapplingHook_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateGrapplingHook");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    return false;
                }
                
                return true;
            }
        }

        /// <summary>
        /// Skip nozzle updates when ragdoll is active and stop any playing particles.
        /// </summary>
        [HarmonyPatch]
        public class UpdateNozzles_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateNozzles");
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (IsRagdollActive(__instance))
                {
                    foreach (var ps in __instance.GetComponentsInChildren<ParticleSystem>())
                    {
                        if (ps.isPlaying)
                            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                    return false;
                }

                return true;
            }
        }
    }
}