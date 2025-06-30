using System;
using System.Collections.Generic;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers;
using UnityEngine;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Craft.Parts;


namespace Assets.Scripts

{
    public partial class SRManager : MonoBehaviour
    {

        public static SRManager Instance { get; private set; }
        private GameObject _flightsObject;
        
    }
    [HarmonyPatch(typeof(CraftFuelSources), "Rebuild")]
    class RebuildPatch
    {
        static bool Prefix(CraftFuelSources __instance, ref List<CrossFeedScript> ____crossFeeds, ref List<Tuple<IFuelSource, IFuelSource>> ____equalizeCrossFeeds, ref List<CraftFuelSource> ____fuelSources, IFuelTransferManager ____fuelTransferManager, ICraftScript craftScript)
        {
            SRCraftFuelSources sources =  new SRCraftFuelSources(____fuelTransferManager);
            sources.Rebuild(craftScript);
            ____crossFeeds = sources.CrossFeeds;
            ____equalizeCrossFeeds = sources.EqualizeCrossFeeds;
            ____fuelSources = sources.FuelSources;
            return false;
        }
    }
}
