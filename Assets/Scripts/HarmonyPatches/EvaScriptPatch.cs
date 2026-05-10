using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using HarmonyLib;
using ModApi;
using System;
using System.Reflection;
using Assets.Scripts.Craft.Parts.Modifiers;

namespace Assets.Scripts.HarmonyPatches
{
    /// <summary>
    /// Patches for EvaScript's flight loop methods to skip when ragdoll is active.
    /// </summary>
    public class EvaScriptPatch
    {
        /// <summary>
        /// Patches EvaScript.FlightUpdate to skip when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class FlightUpdate_Patch
        {
            static MethodBase TargetMethod()
            {
                // Try to find the explicit interface implementation method
                foreach (var m in typeof(EvaScript).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (m.Name.Contains("FlightUpdate") && m.GetParameters().Length == 1)
                    {
                        return m;
                    }
                }
                return null;
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (!Game.InFlightScene) return true;
                
                var ragdoll = __instance?.PartScript?.GetModifier<RagdollModifierScript>();
                if (ragdoll != null && ragdoll.IsRagdollActive)
                {
                    return false;
                }
                
                return true;
            }
        }

        /// <summary>
        /// Patches EvaScript.FlightFixedUpdate to skip when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class FlightFixedUpdate_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var m in typeof(EvaScript).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (m.Name.Contains("FlightFixedUpdate") && m.GetParameters().Length == 1)
                    {
                        return m;
                    }
                }
                return null;
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (!Game.InFlightScene) return true;
                
                var ragdoll = __instance?.PartScript?.GetModifier<RagdollModifierScript>();
                if (ragdoll != null && ragdoll.IsRagdollActive)
                {
                    return false;
                }
                
                return true;
            }
        }
        
        /// <summary>
        /// Patches EvaScript.FlightLateUpdate to skip when ragdoll is active.
        /// </summary>
        [HarmonyPatch]
        public class FlightLateUpdate_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var m in typeof(EvaScript).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (m.Name.Contains("FlightLateUpdate") && m.GetParameters().Length == 1)
                    {
                        return m;
                    }
                }
                return null;
            }

            public static bool Prefix(EvaScript __instance)
            {
                if (!Game.InFlightScene) return true;
                
                var ragdoll = __instance?.PartScript?.GetModifier<RagdollModifierScript>();
                if (ragdoll != null && ragdoll.IsRagdollActive)
                {
                    return false;
                }
                
                return true;
            }
        }
    }
}
