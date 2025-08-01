using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Design;
using Assets.Scripts.Flight.UI;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.State;
using ModApi.Ui.Inspector;
using Rewired.Utils.Attributes;
using UnityEngine;
using String = System.String;

namespace Assets.Scripts
{
    public partial class Mod : ModApi.Mods.GameMod
    {
        private DroodismUIManager droodismUIManagerInstance { get; set; }

        [HarmonyPatch(typeof(EvaScript), nameof(EvaScript.OnModifiersCreated))]
        public static class EvaScriptPatch
        {
            /// <summary>
            /// 在 OnModifiersCreated 方法执行后运行的后置补丁。
            /// Postfix patch to run after the OnModifiersCreated method.
            /// </summary>
            /// <param name="__instance">EvaScript 实例。The EvaScript instance.</param>
            public static void Postfix(EvaScript __instance)
            {
                // 仅在飞行场景中执行修补
                // Only execute the patch in the flight scene
                if (!Game.InFlightScene)
                {
                    return;
                }

                try
                {
                    // 获取零件上所有的 FuelTankScript 组件
                    // Get all FuelTankScript components on the part
                    var fuelTanks = ((Component)__instance).GetComponents<FuelTankScript>();

                    // 查找燃料类型为 "JetPack" 的 FuelTankScript
                    // Find the FuelTankScript with fuel type "JetPack"
                    var jetPackFuelTank = Enumerable.FirstOrDefault(fuelTanks, tank => tank.FuelType?.Id == "Jetpack");

                    // 使用反射设置 _fuelTank 字段的值
                    // Use reflection to set the value of the _fuelTank field
                    var fuelTankField = AccessTools.Field(typeof(EvaScript), "_fuelTank");
                    if (fuelTankField != null)
                    {
                        fuelTankField.SetValue(__instance, jetPackFuelTank);
                        if (jetPackFuelTank == null)
                        {
                            Debug.LogWarning(
                                "[EvaScriptPatch] No FuelTankScript with fuel type 'JetPack' found. Setting _fuelTank to null.");
                        }
                        else
                        {
                            Debug.Log($"[EvaScriptPatch] Successfully set _fuelTank to JetPack fuel tank.");
                        }
                    }
                    else
                    {
                        Debug.LogError("[EvaScriptPatch] Failed to find _fuelTank field via reflection.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EvaScriptPatch] Error in Postfix patch: {e.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(CraftFuelSources), "Rebuild")]
        class RebuildPatch
        {
            static bool Prefix(CraftFuelSources __instance,
                ref List<CrossFeedScript> ____crossFeeds,
                ref List<Tuple<IFuelSource, IFuelSource>> ____equalizeCrossFeeds,
                ref List<CraftFuelSource> ____fuelSources,
                IFuelTransferManager ____fuelTransferManager,
                ICraftScript craftScript)
            {
                SRCraftFuelSources sources = new SRCraftFuelSources(____fuelTransferManager);
                sources.Rebuild(craftScript);
                ____crossFeeds = sources.CrossFeeds;
                ____equalizeCrossFeeds = sources.EqualizeCrossFeeds;
                ____fuelSources = sources.FuelSources;
                return false;
            }
        }

        [HarmonyPatch(typeof(NavPanelController), "LayoutRebuilt")]
        class LayoutRebuiltPatch
        {
            static bool Prefix(NavPanelController __instance)
            {
                try
                {
                    __instance.xmlLayout.GetElementById(DroodismUIManager.droodismBottomId)
                        .AddOnClickEvent(DroodismUIManager.Instance.OnToggleDroodismInspectorPanelState, true);

                }
                catch (Exception e)
                {
                    Debug.LogFormat("Error while adding click event to{0}", e);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(CraftPerformanceAnalysis), "CreateCraftDetailsGroup")]
        class CreateCraftDetailsGroupPatch
        {
            static bool Prefix(CraftPerformanceAnalysis __instance)
            {
                Debug.Log("CreateCraftDetailsGroupPatch");
                return true;
            }
        }

        [HarmonyPatch(typeof(CraftPerformanceAnalysis), "RefreshInspectorPanel", new Type[] { typeof(bool) })]
        public class CraftPerformanceAnalysisPatch
        {
            // Postfix 补丁，在 RefreshInspectorPanel 方法执行后运行,添加一个新的info组,哈基j没给我留接口,那我只能harmony启动了
            [HarmonyPostfix]
            public static void Postfix(CraftPerformanceAnalysis __instance, bool immediate)
            {
                if (!immediate) return;
                var inspectorPanelField = AccessTools.Field(typeof(CraftPerformanceAnalysis), "_inspectorPanel");
                var inspectorPanel = inspectorPanelField.GetValue(__instance) as IInspectorPanel;

                if (inspectorPanel == null)
                {
                    Debug.LogError("InspectorPanel is null, cannot add TEXT group.");
                    return;
                }

                var inspectorModel = inspectorPanel.Model;
                if (inspectorModel == null)
                {
                    Debug.LogError("InspectorModel is null, cannot add TEXT group.");
                    return;
                }

                GroupModel textGroup = new GroupModel("<color=green>Life Support Resources Info");

                textGroup.Add<TextModel>(new TextModel("Drood Count",
                    () => Scripts.Mod.Inctance.GetDroodCountInDesigner()));
                foreach (var var in fuelTypes)
                {
                    AddStuff(var);
                }

                void AddStuff(String name)
                {
                    bool isWaste = name.Contains("Waste") || name == "CO2";
                    textGroup.Add<TextModel>(new TextModel(name + (isWaste ? " Capacity" : " Amount"),
                        () => GetFuelAmountInDesigner(name, isWaste)));
                }

                // 将新组添加到 InspectorModel
                inspectorModel.AddGroup(textGroup);
            }
        }

        // man what can i say
        //说正儿八经的,原意图是通过harmonyLib增加validation验证种类,但是你游屎山导致我懒得花力气了
        /*
        [HarmonyPatch]
        public class ValidatorPatch
        {
            static Type ValidatorType
            {
                get
                {
                    var type = Game.Instance.GameState.Validator.GetType();
                    Debug.Log($"Validator Type: {type.FullName}, Assembly: {type.Assembly.GetName().Name}");
                    return type;
                }
            }

            [HarmonyTargetMethod]
            static MethodInfo TargetMethod()
            {
                var method = AccessTools.Method(ValidatorType, "ValidateCraft", new[] { typeof(ICraftScript), typeof(LaunchLocation), typeof(bool) });
                if (method == null)
                {
                    Debug.LogError("ValidateCraft method not found. Available methods:");
                    var methods = AccessTools.GetDeclaredMethods(ValidatorType)
                        .Where(m => m.Name == "ValidateCraft");
                    foreach (var m in methods)
                    {
                        Debug.Log($"Method: {m}, Parameters: {string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))}");
                    }
                    return null;
                }
                Debug.Log($"Found ValidateCraft: {method}, Parameters: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}");
                return method;
            }

            [HarmonyPostfix]
            public static void Postfix(object __result, ICraftScript craftScript, LaunchLocation launchLocation, bool fix)
            {
                foreach (var fuelTypeid in fuelTypes)
                {
                    bool isWaste = fuelTypeid.Contains("Waste") || fuelTypeid == "CO2";
                    FuelValidation.ValidateFuelTotal(__result, craftScript, fuelTypeid,1,isWaste);
                }
                Debug.Log("Applied custom fuel total validation for LiquidFuel.");
            }

            */
    
        
        
    }
}