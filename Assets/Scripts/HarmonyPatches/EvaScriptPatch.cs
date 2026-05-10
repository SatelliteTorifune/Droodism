using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using HarmonyLib;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod : ModApi.Mods.GameMod
    {
        [HarmonyPatch(typeof(EvaScript), nameof(EvaScript.OnModifiersCreated))]
        public static class EvaScriptPatch
        {
            /// <summary>
            /// 在 OnModifiersCreated 方法执行后运行的后置补丁。
            /// Postfix patch to run after the OnModifiersCreated method.
            /// 老实说我不知道这玩意也没有啥用,因为我反编译看到的Eva Script好像自己定义好了东西
            /// </summary>
            /// <param name="__instance">EvaScript 实例。The EvaScript instance.</param>
            public static void Postfix(EvaScript __instance)
            {

                // Only execute the patch in the flight scene
                if (!Game.InFlightScene)
                {
                    return;
                }

                try
                {
                    var fuelTanks = ((Component)__instance).GetComponents<FuelTankScript>();
                    var jetPackFuelTank = Enumerable.FirstOrDefault(fuelTanks, tank => tank.FuelType?.Id == "Jetpack");


                    var fuelTankField = AccessTools.Field(typeof(EvaScript), "_fuelTank");
                    if (fuelTankField != null)
                    {
                        fuelTankField.SetValue(__instance, jetPackFuelTank);
                        if (jetPackFuelTank == null)
                        {
                            //Debug.LogWarning("[EvaScriptPatch] No FuelTankScript with fuel type 'JetPack' found. Setting _fuelTank to null.");
                        }
                        else
                        {
                            //Debug.Log($"[EvaScriptPatch] Successfully set _fuelTank to JetPack fuel tank.");
                        }
                    }
                    else
                    {
                        LogError("[EvaScriptPatch] Failed to find _fuelTank field via reflection.");
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"[EvaScriptPatch] Error in Postfix patch: {e.Message}");
                }
            }
        }
    }
}