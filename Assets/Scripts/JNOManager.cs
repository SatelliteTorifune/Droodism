using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModApi.Flight;
using System.Xml.Linq;

using ModApi;
//using ModApi.Common;
using ModApi.Mods;
using ModApi.Flight.UI;
using Assets.Scripts.Flight.Sim;
using ModApi.Flight.Sim;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.UI;   
using Assets.Scripts.Design;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.State; 
using ModApi.Craft;
using ModApi.Ui;
using ModApi.Math;
using ModApi.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.Design.PartProperties;
using Assets.Scripts.Craft.Parts;
using Jundroo.ModTools;   
using UnityEngine;
using Assets.Scripts.Career;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using HarmonyLib;
namespace Assets.Scripts.JNOManager
{
    public partial class JNOManager:MonoBehaviour
    {
    [HarmonyPatch(typeof(CraftFuelSources), "Rebuild")]
    class Rebuild_Patch
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
}
