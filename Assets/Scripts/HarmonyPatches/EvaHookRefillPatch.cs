using System;
using System.Reflection;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Mods;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod
    {
        private static FieldInfo _grapplingHookField;

        private static FieldInfo GrapplingHookField
        {
            get
            {
                if (_grapplingHookField == null)
                    _grapplingHookField = AccessTools.Field(typeof(EvaScript), "_grapplingHook");
                return _grapplingHookField;
            }
        }

        
        private const float OxygenRefillRatePerSecond = 5f;

        /// <summary>
        /// Postfix 补丁：在原生 UpdateGrapplingHook() 的 Jetpack 燃料充能逻辑执行完毕后，
        /// 追加氧气补充逻辑。当钩爪连接 Eva ↔ 飞船时，从飞船氧气源抽取并补充 Eva 的个人氧气 buffer。
        /// </summary>
        [HarmonyPatch]
        public class UpdateGrapplingHookOxygenRefillPatch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateGrapplingHook");
            }

            public static void Postfix(EvaScript __instance)
            {
               
                if (!Game.InFlightScene)
                {
                    return;
                }

                if (!__instance.PartScript.CommandPod.IsPlayerControlled)
                {
                    return;
                }
                var hook = GrapplingHookField.GetValue(__instance) as GrapplingHookScript;
                if (hook == null)
                {
                    return;
                }
                

                // 3. 判断钩爪是否连接到了飞船（Eva ↔ Craft）
                bool hasEvaFrom = hook.EvaFrom != null;
                bool hasCraftTo = hook.CraftTo != null;
                bool hasEvaTo = hook.EvaTo != null;
                bool hasCraftFrom = hook.CraftFrom != null;

                if (hasEvaFrom && hasCraftTo)
                {
                    PatchWorkingLogic(hook.EvaFrom, hook.CraftTo);
                }
                else if (hasEvaTo && hasCraftFrom)
                {
                    PatchWorkingLogic(hook.EvaTo, hook.CraftFrom);
                }
                
            }

            private static void PatchWorkingLogic(EvaScript eva, ICraftScript targetCraft)
            {
                // 4. 获取当前 Eva 的 SupportLifeScript
                var supportLife = eva.PartScript.GetModifier<SupportLifeScript>();
                if (supportLife?.Data == null)
                {
                    return;
                }
                

                // 5. 获取目标飞船的氧气源
                var activePod = targetCraft.PrimaryCommandPod;
                if (activePod?.Part?.PartScript == null)
                {
                    return;
                }
                
                var cmdPodPatch = activePod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                if (cmdPodPatch == null)
                {
                    return;
                }
                
                var craftOxygen = cmdPodPatch.OxygenFuelSource;
                if (craftOxygen == null)
                {
                    return;
                }
                
                // 6. 计算本帧应补充的氧气量
                double refillAmount = OxygenRefillRatePerSecond * Time.deltaTime;
                Log(
                    $"[O2Refill] Step6: refillAmount={refillAmount:F6} (rate={OxygenRefillRatePerSecond}/s × dt={Time.deltaTime:F6})");
                if (refillAmount <= 0.0) return;

                // 检查飞船是否有足够的氧气
                double availableFromCraft = Math.Min(refillAmount, craftOxygen.TotalFuel);
                Log(
                    $"[O2Refill] Step6b: availableFromCraft={availableFromCraft:F6} (craft has {craftOxygen.TotalFuel:F6})");
                if (availableFromCraft <= 0.0) return;

                // 检查 Eva 个人 buffer 是否已满
                double currentBuffer = supportLife.Data._oxygenAmountBuffer;
                double capacity = supportLife.Data.DesireOxygenCapacity;
                double spaceLeft = capacity - currentBuffer;
                Log(
                    $"[O2Refill] Step6c: Eva O₂ buffer={currentBuffer:F3}/{capacity:F3}, spaceLeft={spaceLeft:F3}");
                if (spaceLeft <= 0.0) return;

                double actualRefill = Math.Min(availableFromCraft, spaceLeft);

                // 7. 执行转移：飞船 → Eva
                craftOxygen.RemoveFuel(actualRefill);
                supportLife.Data._oxygenAmountBuffer += actualRefill;

                Log($"[O2Refill] SUCCESS: refilled {actualRefill:F3} O₂ to {eva.Data.CrewName} " +
                        $"(buffer: {currentBuffer:F3} → {supportLife.Data._oxygenAmountBuffer:F3} / {capacity:F3})");
            }
        }
    }
}