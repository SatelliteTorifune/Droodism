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
        [HarmonyPatch]
        public class UpdateGrapplingHookOxygenRefillPatch
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
            private const float OxygenRefillRatePerSecond = 1.5f;
            private const float Co2TransferRatePerSecond = 1.5f;
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(EvaScript), "UpdateGrapplingHook");
            }

            public static void Postfix(EvaScript __instance)
            {
                // 1. 前置检查
                if (!Game.InFlightScene) return;
                if (!__instance.PartScript.CommandPod.IsPlayerControlled) return;

                // 2. 通过反射获取钩爪实例
                var hook = GrapplingHookField.GetValue(__instance) as GrapplingHookScript;
                if (hook == null) return;

                // 3. 判断连接方向，提取 Eva 和飞船
                if (hook.EvaFrom != null && hook.CraftTo != null)
                    TransferResources(hook.EvaFrom, hook.CraftTo);
                else if (hook.EvaTo != null && hook.CraftFrom != null)
                    TransferResources(hook.EvaTo, hook.CraftFrom);
            }

            // ---- 核心传输逻辑 ----

            private static void TransferResources(EvaScript eva, ICraftScript targetCraft)
            {
                var supportLife = eva.PartScript.GetModifier<SupportLifeScript>();
                if (supportLife?.Data == null) return;

                var cmdPodPatch = targetCraft
                    ?.PrimaryCommandPod
                    ?.Part?.PartScript
                    ?.GetModifier<STCommandPodPatchScript>();
                if (cmdPodPatch == null) return;

                TransferOxygen(supportLife, cmdPodPatch);
                TransferCo2(supportLife, cmdPodPatch);
            }

            private static void TransferOxygen(
                SupportLifeScript life,
                STCommandPodPatchScript cmdPodPatch)
            {
                var src = cmdPodPatch.OxygenFuelSource;
                if (src == null) return;

                double amount = OxygenRefillRatePerSecond * Time.deltaTime;
                if (amount <= 0.0) return;

                double available = Math.Min(amount, src.TotalFuel);
                if (available <= 0.0) return;

                double buffer  = life.Data._oxygenAmountBuffer;
                double cap     = life.Data.DesireOxygenCapacity;
                double space   = cap - buffer;
                if (space <= 0.0) return;

                double actual = Math.Min(available, space);

                src.RemoveFuel(actual);
                life.Data._oxygenAmountBuffer += actual;
                
            }
            

            private static void TransferCo2(
                SupportLifeScript life,
                STCommandPodPatchScript cmdPodPatch)
            {
                var dst = cmdPodPatch.CO2FuelSource;
                if (dst == null) return;
                if (life.Data._co2AmountBuffer <= 0.0) return;

                double amount = Co2TransferRatePerSecond * Time.deltaTime;
                if (amount <= 0.0) return;

                double available = Math.Min(amount, life.Data._co2AmountBuffer);
                double space     = dst.TotalCapacity - dst.TotalFuel;
                if (space <= 0.0) return;

                double actual = Math.Min(available, space);

                dst.AddFuel(actual);
                life.Data._co2AmountBuffer -= actual;
                
            }
        }
}
