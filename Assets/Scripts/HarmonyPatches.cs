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
using AccessTools = HarmonyLib.AccessTools;
using String = System.String;

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
                        Debug.LogError("[EvaScriptPatch] Failed to find _fuelTank field via reflection.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EvaScriptPatch] Error in Postfix patch: {e.Message}");
                }
            }
        }

        
        /// <summary>
        /// 重要!!核心组件之一,仿照Mono/jet/battery,使用添加的patch modifier(STCommandPodPatch)中的六个IFuelSource接口,然后用这个patch调用SRCraftFuelSources中的方设置FuelSource
        /// </summary>
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

        //byd jundroo给的教程有问题,本来是用另一个函数的,但是只能用harmony
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
                    () => Scripts.Mod.Instance.GetDroodCountInDesigner()));
                foreach (var var in fuelTypes)
                {
                    AddStuff(var);
                }

                void AddStuff(String fuelType)
                {
                    bool isWaste = fuelType.Contains("Waste") || fuelType == "CO2"||fuelType=="HPCO2";
                    string name = "";
                    switch (fuelType)
                    {
                        case "H2O":
                            name="Water";
                            break;
                        case "CO2":
                            name = "Carbon Dioxide";
                            break;
                        case "HPCO2":
                            name = "High Pressure Carbon Dioxide";
                            break;
                        case "HPOxygen":
                            name = "High Pressure Oxygen";
                            break;
                        
                        default:
                            name = fuelType;
                            break;
                    
                    }
                    textGroup.Add<TextModel>(new TextModel(name + (isWaste ? " Capacity" : " Amount"),
                        () => GetFuelAmountInDesigner(fuelType, isWaste)));
                }

                // 将新组添加到 InspectorModel
                inspectorModel.AddGroup(textGroup);
            }
        }
        
        //写这个b玩意的意义何在啊我操
        //我下次加harmonyPatch一定要写注释
        [HarmonyPatch(typeof(EvaScript), "LoadIntoCrewCompartment")]
        class LoadCompartmentPatch
        {
            [HarmonyPrefix]
            static bool LoadIntoCrewCompartment(EvaScript __instance, CrewCompartmentScript crewCompartment)
            {
                //这东西我也不敢动啊
                return true;
            }
        }
    }
}