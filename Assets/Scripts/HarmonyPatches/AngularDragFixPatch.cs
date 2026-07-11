using Assets.Scripts.Craft;
using HarmonyLib;
using ModApi;
using UnityEngine;

namespace Assets.Scripts.HarmonyPatches
{
    /// <summary>
    /// 修复角速度阻力 (angularDrag) 在真空中不归零的问题。
    /// 
    /// PatchA: CreateBodyScript 初始值 0.05f → 0f
    /// PatchB2: LerpAngularDrag 在真空中 Prefix 跳过
    /// 
    /// 通过 ModSettings.Instance.FixAngularDragInVacuum 控制开关（默认开启）。
    /// </summary>
    public static class AngularDragFixPatch
    {
        private static bool IsEnabled
        {
            get
            {
                try
                {
                    return ModSettings.Instance.FixAngularDragInVacuum.Value;
                }
                catch
                {
                    return true;
                }
            }
        }

        // ================================================================
        // 补丁A：CraftBuilder.CreateBodyScript Postfix
        // 将初始 angularDrag 从 0.05f 重置为 0f
        // 目标：public static BodyScript CreateBodyScript(CraftScript, BodyData, Quaternion?)
        // ================================================================
        [HarmonyPatch(typeof(CraftBuilder), "CreateBodyScript")]
        [HarmonyPostfix]
        private static void ResetInitialAngularDrag(BodyScript __result)
        {
            if (!IsEnabled) return;
            if (__result?.RigidBody != null)
            {
                __result.RigidBody.angularDrag = 0f;
            }
        }

        // ================================================================
        // 补丁B2：BodyScript.LerpAngularDrag Prefix
        // 在真空中跳过 LerpAngularDrag 的执行，从源头阻止非零 angularDrag 被设置
        // 目标：private void LerpAngularDrag(float targetVal, float time)
        // ================================================================
        [HarmonyPatch(typeof(BodyScript), "LerpAngularDrag")]
        [HarmonyPrefix]
        private static bool SkipLerpAngularDragInVacuum(BodyScript __instance)
        {
            if (!IsEnabled) return true;
            if (__instance?.RigidBody == null) return true;
            if (__instance.RigidBody.isKinematic) return true;

            if (__instance.FluidDensity <= 0f && __instance.UpdateAngularDrag)
            {
                return false; // 跳过原方法，真空不设任何角阻力
            }

            return true; // 大气层内正常执行
        }
    }
}
