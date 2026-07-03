using System;
using System.Collections.Generic;
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
        /*
        /// <summary>
        /// 重要!!核心组件之一,仿照Mono/jet/battery,使用添加的patch modifier(STCommandPodPatch)中的六个IFuelSource接口,然后用这个patch调用SRCraftFuelSources中的方设置FuelSource
        /// </summary>
        [HarmonyPatch(typeof(CraftFuelSources), "Rebuild")]
        class FuelSourceRebuildPatch
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
        }*/
    }
}