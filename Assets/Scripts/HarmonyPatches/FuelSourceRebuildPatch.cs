using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Mods;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
    {
        
        /// <summary>
        /// 重要!!核心组件之一,仿照Mono/jet/battery,使用添加的patch modifier(STCommandPodPatch)中的六个IFuelSource接口,然后用这个patch调用SRCraftFuelSources中的方设置FuelSource
        /// </summary>
        
        [HarmonyPatch(typeof(CraftFuelSources), "Rebuild")]
        class FuelSourceRebuildPatch
        {
            private static readonly Dictionary<CraftFuelSources, SRCraftFuelSources> _fuelSourcesMap = new Dictionary<CraftFuelSources, SRCraftFuelSources>();

            static bool Prefix(ref CraftFuelSources __instance, ICraftScript craftScript)
            {
                try
                {
                    if (!_fuelSourcesMap.TryGetValue(__instance, out var srcFuelSources))
                    {
                        var fuelTransferManager = (IFuelTransferManager)typeof(CraftFuelSources).GetField("_fuelTransferManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                        srcFuelSources = new SRCraftFuelSources(fuelTransferManager);
                        _fuelSourcesMap[__instance] = srcFuelSources;
                    }
                    srcFuelSources.Rebuild(craftScript);
                }
                catch (Exception ex)
                {
                    Log($"FuelSourceRebuildPatch.Prefix failed: {ex}");
                }
                return false;
            }
        }
    }
}