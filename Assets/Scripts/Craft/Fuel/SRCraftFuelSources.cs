#region Assembly SimpleRockets2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using UnityEngine;

namespace Assets.Scripts.Craft.Fuel 
{
    //
    // Summary:
    //     Manages the the craft's fuel sources.
    public class SRCraftFuelSources:ICraftFuelSources, IDisposable
    {
       

        //
        // Summary:
        //     The cross feeds
        private List<CrossFeedScript> _crossFeeds = new List<CrossFeedScript>();

        public List<CrossFeedScript> CrossFeeds => _crossFeeds;

        //
        // Summary:
        //     The equalize cross feeds
        private List<Tuple<IFuelSource, IFuelSource>> _equalizeCrossFeeds;
        public List<Tuple<IFuelSource, IFuelSource>> EqualizeCrossFeeds => _equalizeCrossFeeds;

        //
        // Summary:
        //     The log of how much fuel has been added/removed for each fuel type this frame.
        //     This is only used when the FuelUsed event has at least one subscriber.
        private Dictionary<FuelType, double> _frameFuelLog = null;

        //
        // Summary:
        //     The fuel sources
        private List<CraftFuelSource> _fuelSources = new List<CraftFuelSource>();


        private static readonly IReadOnlyDictionary<string, IFuelSource> EmptyCraftWidePoolFuelSources =
            new Dictionary<string, IFuelSource>();

        private static readonly MethodInfo _setCraftWidePoolFuelSourcesMethod;
        private static readonly MethodInfo _getCraftWidePoolFuelSourcesMethod;

        static SRCraftFuelSources()
        {
            _setCraftWidePoolFuelSourcesMethod = typeof(PartScript).GetMethod(
                "SetCraftWidePoolFuelSources",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _getCraftWidePoolFuelSourcesMethod = typeof(PartScript).GetMethod(
                "GetCraftWidePoolFuelSources",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        //
        // Summary:
        //     The fuel transfer manager
        private IFuelTransferManager _fuelTransferManager;

        //
        // Summary:
        //     Gets the list of all fuel sources.
        //
        // Value:
        //     The fuel sources in the craft.
        public List<CraftFuelSource> FuelSources => _fuelSources;

        //
        // Summary:
        //     Occurs when fuel is used from any of the craft's fuel sources.
        IReadOnlyList<IFuelSource> ICraftFuelSources.FuelSources => FuelSources;

        public event FuelDelegate FuelUsed;

        //
        // Summary:
        //     Initializes a new instance of the Assets.Scripts.Craft.Fuel.CraftFuelSources
        //     class.
        //
        // Parameters:
        //   fuelTransferManager:
        //     The fuel transfer manager.
        public SRCraftFuelSources(IFuelTransferManager fuelTransferManager)
        {
            _fuelTransferManager = fuelTransferManager;
        }

        

       

        //
        // Summary:
        //     Absorbs the fuel source from another craft script into this craft script's fuel
        //     sources.
        //
        // Parameters:
        //   craftFuelSources:
        //     The craft fuel sources to absorb.
        public void AbsorbFuelSources(CraftFuelSources craftFuelSources)
        {
            int num = 0;
            foreach (CraftFuelSource fuelSource in _fuelSources)
            {
                num = Mathf.Max(fuelSource.Id, num);
            }

            num++;
            foreach (CraftFuelSource fuelSource2 in craftFuelSources.FuelSources)
            {
                fuelSource2.FuelTransferMode = FuelTransferMode.None;
                _fuelSources.Add(fuelSource2);
                fuelSource2.FuelTransferManager = _fuelTransferManager;
                fuelSource2.Id = num++;
            }
        }

        //
        // Summary:
        //     Creates the fuel source for a list of connected parts.
        //
        // Parameters:
        //   parts:
        //     The list of parts that are connected.
        //
        //   removeDisconnectedCrossFeeds:
        //     if set to true then remove disconnected cross feeds.
        //
        //   fuelSources:
        //     An optional list to add the new fuel sources to as they are created. If this
        //     is null then it will not be used.
        public void CreateFuelSourceForConnectedParts(IEnumerable<PartData> parts, bool removeDisconnectedCrossFeeds, List<CraftFuelSource> fuelSources)
        {
            List<FuelTankData> modifiers = new List<FuelTankData>();
            Dictionary<int, FuelTankScript> lookup = new Dictionary<int, FuelTankScript>();
            List<PartData> parts1 = new List<PartData>();
            HashSet<int> partIds = new HashSet<int>();
            HashSet<int> crossFeedPartIds = new HashSet<int>();

            foreach (PartData part in parts)
            {
                parts1.Add(part);
                partIds.Add(part.Id);
                part.GetModifiers<FuelTankData>(modifiers);
                CrossFeedData modifier = part.GetModifier<CrossFeedData>();
                if (modifier != null)
                {
                    if (!EngineUtilities.FuelPassesThrough(part))
                        crossFeedPartIds.Add(part.Id);
                    if (!removeDisconnectedCrossFeeds && modifier.Mode != 0)
                        this._crossFeeds.Add(modifier.Script);
                }
            }

            Dictionary<int, int> regions = ComputeRegions(parts1, partIds, crossFeedPartIds);
            Dictionary<int, Dictionary<string, IFuelSource>> regionPools = new Dictionary<int, Dictionary<string, IFuelSource>>();

            try
            {
                foreach (FuelTankData fuelTank in modifiers)
                {
                    if (fuelTank != null)
                    {
                        if (!fuelTank.Script.PartScript.Disconnected)
                        {
                            if (fuelTank.FuelType == FuelType.Battery)
                            {
                                if (fuelTank.Part.PartScript.CommandPod?.BatteryFuelSource is CraftFuelSource batteryFuelSource)
                                {
                                    batteryFuelSource.AddFuelTank(fuelTank.Script);
                                    continue;
                                }
                            }

                            if (fuelTank.FuelType?.Id == "Oxygen")
                            {
                                if (fuelTank?.Part?.PartScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>()?.OxygenFuelSource is CraftFuelSource craftFuelSource)
                                {
                                    craftFuelSource?.AddFuelTank(fuelTank?.Script);
                                    continue;
                                }
                            }
                            if (fuelTank.FuelType?.Id == "H2O")
                            {
                                if (fuelTank?.Part?.PartScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>()?.WaterFuelSource is CraftFuelSource craftFuelSource)
                                {
                                    craftFuelSource?.AddFuelTank(fuelTank?.Script);
                                    continue;
                                }
                            }
                            if (fuelTank.FuelType?.Id == "Food")
                            {
                                if (fuelTank?.Part?.PartScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>()?.FoodFuelSource is CraftFuelSource craftFuelSource)
                                {
                                    craftFuelSource?.AddFuelTank(fuelTank?.Script);
                                    continue;
                                }
                            }
                            if (fuelTank.FuelType?.Id == "CO2")
                            {
                                if (fuelTank?.Part?.PartScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>()?.CO2FuelSource is CraftFuelSource craftFuelSource)
                                {
                                    craftFuelSource?.AddFuelTank(fuelTank?.Script);
                                    continue;
                                }
                            }
                            if (fuelTank.FuelType?.Id == "Wasted Water")
                            {
                                if (fuelTank?.Part?.PartScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>()?.WastedWaterFuelSource is CraftFuelSource craftFuelSource)
                                {
                                    craftFuelSource?.AddFuelTank(fuelTank?.Script);
                                    continue;
                                }
                            }
                            if (fuelTank.FuelType?.Id == "Solid Waste")
                            {
                                if (fuelTank?.Part?.PartScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>()?.SolidWasteFuelSource is CraftFuelSource craftFuelSource)
                                {
                                    craftFuelSource?.AddFuelTank(fuelTank?.Script);
                                    continue;
                                }
                            }
                            else
                            {
                                int region;
                                if (fuelTank.FuelType != null && fuelTank.FuelType.CraftWidePool && regions.TryGetValue(fuelTank.Part.Id, out region))
                                {
                                    this.AddTankToRegionPool(regionPools, region, fuelTank, fuelSources);
                                    continue;
                                }
                            }
                        }
                        lookup[fuelTank.Part.Id] = fuelTank.Script;
                    }
                }

                foreach (int key in lookup.Keys.ToArray<int>())
                {
                    FuelTankScript fuelTankScript1 = lookup[key];
                    if (fuelTankScript1 != null)
                    {
                        FuelTankScript fuelTankScript2 = fuelTankScript1;
                        CraftFuelSource fuelSource = this.CreateFuelSource(fuelTankScript2.Data.FuelType);
                        FindConnectedTanks(fuelTankScript2.PartScript.Data, fuelTankScript2, fuelSource, lookup, new HashSet<int>());
                        fuelSources?.Add(fuelSource);
                    }
                }

                BorrowPoolsAcrossCrossFeeds(regions, regionPools);

                foreach (PartData partData in parts1)
                {
                    int key;
                    Dictionary<string, IFuelSource> dictionary;
                    IReadOnlyDictionary<string, IFuelSource> sources = !regions.TryGetValue(partData.Id, out key) || !regionPools.TryGetValue(key, out dictionary) ? EmptyCraftWidePoolFuelSources : (IReadOnlyDictionary<string, IFuelSource>) dictionary;
                    if (partData.PartScript is PartScript partScript)
                    {
                        //反射调用方法
                        _setCraftWidePoolFuelSourcesMethod?.Invoke(partScript, new object[] { sources });
                    }
                       
                }

                SetupCrossFeeds(removeDisconnectedCrossFeeds);
                SetupCraftWidePoolCrossFeeds();
            }
            catch (Exception e)
            {
                Mod.LogError($"Droodism:[SRCraftFuelSources] CreateFuelSourceForConnectedParts failed: {e}");
            }
        }
        private void SetupCraftWidePoolCrossFeeds()
        {
          foreach (CrossFeedScript crossFeed in this._crossFeeds)
          {
            PartData data = crossFeed.PartScript.Data;
            PartData adjacentPart1 = EngineUtilities.GetAdjacentPart(data, crossFeed.Data.AttachPointA);
            PartData adjacentPart2 = EngineUtilities.GetAdjacentPart(data, crossFeed.Data.AttachPointB);
            IReadOnlyDictionary<string, IFuelSource> widePoolFuelSources1 = adjacentPart1?.PartScript is PartScript partScript1
                ? (IReadOnlyDictionary<string, IFuelSource>) _getCraftWidePoolFuelSourcesMethod?.Invoke(partScript1, null)
                : null;
            IReadOnlyDictionary<string, IFuelSource> widePoolFuelSources2 = adjacentPart2?.PartScript is PartScript partScript2
                ? (IReadOnlyDictionary<string, IFuelSource>) _getCraftWidePoolFuelSourcesMethod?.Invoke(partScript2, null)
                : null;
            if (widePoolFuelSources1 != null && widePoolFuelSources2 != null)
            {
              foreach (KeyValuePair<string, IFuelSource> keyValuePair in widePoolFuelSources1)
              {
                if (widePoolFuelSources2.TryGetValue(keyValuePair.Key, out IFuelSource fuelSource))
                {
                  CraftFuelSource craftFuelSource1 = (CraftFuelSource) keyValuePair.Value;
                  CraftFuelSource craftFuelSource2 = (CraftFuelSource) fuelSource;
                  if (craftFuelSource1 != craftFuelSource2 && craftFuelSource1.FuelType.AllowFuelTransfer)
                  {
                    if (crossFeed.Data.Mode == CrossFeedData.CrossFeedMode.Equalize)
                    {
                      if (this._equalizeCrossFeeds == null)
                        this._equalizeCrossFeeds = new List<Tuple<IFuelSource, IFuelSource>>();
                      this._equalizeCrossFeeds.Add(new Tuple<IFuelSource, IFuelSource>((IFuelSource) craftFuelSource1, (IFuelSource) craftFuelSource2));
                    }
                    else if (crossFeed.Data.Mode == CrossFeedData.CrossFeedMode.Normal)
                      craftFuelSource2.AddCrossFeedPullSource((IFuelSource) craftFuelSource1);
                    else if (crossFeed.Data.Mode == CrossFeedData.CrossFeedMode.Reversed)
                      craftFuelSource1.AddCrossFeedPullSource((IFuelSource) craftFuelSource2);
                  }
                }
              }
            }
          }
        }

        //
        // Summary:
        //     Rebuilds the fuel sources.
        //
        // Parameters:
        //   craftScript:
        //     The craft script.
        public void Rebuild(ICraftScript craftScript)
        {
            this._fuelSources.Clear();
            this._crossFeeds.Clear();
            this._equalizeCrossFeeds?.Clear();

            CraftFuelSource fuelSource = this.CreateFuelSource(FuelType.Battery);
            foreach (ICommandPod commandPod in craftScript.CommandPods)
            {
                (commandPod as CommandPodScript).BatteryFuelSource = (IFuelSource) fuelSource;
                STCommandPodPatchScript patchScript = commandPod.Part.PartScript?.GetModifier<STCommandPodPatchScript>();
                if (patchScript != null)
                {
                    try
                    {
                        patchScript.OxygenFuelSource = CreateFuelSource(Game.Instance.PropulsionData.GetFuelType("Oxygen"), reverseSubPriority: true);
                        patchScript.WastedWaterFuelSource= CreateFuelSource(Game.Instance.PropulsionData.GetFuelType("Wasted Water"), reverseSubPriority: true);
                        patchScript.FoodFuelSource = CreateFuelSource(Game.Instance.PropulsionData.GetFuelType("Food"), reverseSubPriority: true);
                        patchScript.SolidWasteFuelSource= CreateFuelSource(Game.Instance.PropulsionData.GetFuelType("Solid Waste"), reverseSubPriority: true);
                        patchScript.CO2FuelSource = CreateFuelSource(Game.Instance.PropulsionData.GetFuelType("CO2"), reverseSubPriority: true);
                        patchScript.WaterFuelSource = CreateFuelSource(Game.Instance.PropulsionData.GetFuelType("H2O"), reverseSubPriority: true);
                    }
                    catch (Exception e)
                    {
                        Mod.Log($"SRCCraftFuelSources.Rebuild: Error creating fuel sources: {e}");
                    }
                }
            }
            

            this.CreateFuelSourceForConnectedParts((IEnumerable<PartData>) craftScript.Data.Assembly.Parts, false, (List<CraftFuelSource>) null);
            
        }
        
        //
        // Summary:
        //     Updates the craft's fuel sources.
        //
        // Parameters:
        //   deltaTime:
        //     The delta time.
        public void Update(float deltaTime)
        {
            foreach (CraftFuelSource fuelSource in _fuelSources)
            {
                fuelSource.UpdateCrossFeeds(deltaTime);
            }

            if (_equalizeCrossFeeds != null)
            {
                foreach (Tuple<IFuelSource, IFuelSource> equalizeCrossFeed in _equalizeCrossFeeds)
                {
                    EqualizeFuelSources(equalizeCrossFeed.Item1, equalizeCrossFeed.Item2, deltaTime);
                }
            }

            ClearFuelLog();
            List<CraftFuelSource> list = null;
            foreach (CraftFuelSource fuelSource2 in _fuelSources)
            {
                double fuelDelta = fuelSource2.UpdateFuel();
                LogFuelUsed(fuelSource2.FuelType, fuelDelta);
                if (fuelSource2.IsDead)
                {
                    if (list == null)
                    {
                        list = new List<CraftFuelSource>();
                    }

                    list.Add(fuelSource2);
                }
            }

            if (list != null)
            {
                foreach (CraftFuelSource item in list)
                {
                    _fuelSources.Remove(item);
                }
            }

            RaiseFuelUsedEvents();
        }

        //
        // Summary:
        //     Recursive method that finds the tanks connected to the specified fuel source
        //     and adds them to the fuel source. The fuel tanks found along the way are removed
        //     from the lookup dictionary by setting their value to null.
        //
        // Parameters:
        //   part:
        //     The part.
        //
        //   fuelTankScript:
        //     The fuel tank script.
        //
        //   fuelSource:
        //     The fuel source.
        //
        //   lookup:
        //     The lookup.
        private static void FindConnectedTanks(
            PartData part,
            FuelTankScript fuelTankScript,
            CraftFuelSource fuelSource,
            Dictionary<int, FuelTankScript> lookup,
            HashSet<int> visitedPassThrough)
        {
            if ((UnityEngine.Object) fuelTankScript != (UnityEngine.Object) null)
            {
                fuelSource.AddFuelTank(fuelTankScript);
                lookup[part.Id] = (FuelTankScript) null;
            }
            foreach (PartConnection partConnection in part.PartConnections)
            {
                PartData otherPart = partConnection.GetOtherPart(part);
                if (EngineUtilities.ConnectedWithFuelLine(partConnection, part, otherPart))
                {
                    FuelTankScript fuelTankScript1 = (FuelTankScript) null;
                    if (lookup.TryGetValue(otherPart.Id, out fuelTankScript1) && (UnityEngine.Object) fuelTankScript1 != (UnityEngine.Object) null && fuelTankScript1.Data.FuelType == fuelSource.FuelType)
                        FindConnectedTanks(otherPart, fuelTankScript1, fuelSource, lookup, visitedPassThrough);
                    else if (EngineUtilities.FuelPassesThrough(otherPart) && visitedPassThrough.Add(otherPart.Id))
                        FindConnectedTanks(otherPart, (FuelTankScript) null, fuelSource, lookup, visitedPassThrough);
                }
            }
        }

        //
        // Summary:
        //     Clears the frame's fuel log.
        private void ClearFuelLog()
        {
            if (_frameFuelLog != null)
            {
                _frameFuelLog.Clear();
            }
        }

        //
        // Summary:
        //     Creates the fuel source.
        //
        // Parameters:
        //   fuelType:
        //     Type of the fuel.
        //
        //   reverseSubPriority:
        //     if set to true then set the fuel source to reverse its sub priority ordering.
        //
        //
        // Returns:
        //     The fuel source.
        private CraftFuelSource CreateFuelSource(FuelType fuelType, bool reverseSubPriority = false)
        {
            CraftFuelSource craftFuelSource = new CraftFuelSource(_fuelTransferManager, _fuelSources.Count, fuelType);
            craftFuelSource.ReverseSubPriority = reverseSubPriority;
            _fuelSources.Add(craftFuelSource);
            return craftFuelSource;
        }

        private static Dictionary<int, int> ComputeRegions(List<PartData> parts, HashSet<int> partIds, HashSet<int> crossFeedPartIds)
        {
            Dictionary<int, int> regions = new Dictionary<int, int>(parts.Count);
            Queue<PartData> partDataQueue = new Queue<PartData>();
            int num1 = 0;

            foreach (PartData part1 in parts)
            {
                if (!regions.ContainsKey(part1.Id))
                {
                    if (crossFeedPartIds.Contains(part1.Id))
                    {
                        regions[part1.Id] = num1++;
                    }
                    else
                    {
                        int num2 = num1++;
                        regions[part1.Id] = num2;
                        partDataQueue.Enqueue(part1);

                        while (partDataQueue.Count > 0)
                        {
                            PartData part2 = partDataQueue.Dequeue();
                            foreach (PartConnection partConnection in part2.PartConnections)
                            {
                                PartData otherPart = partConnection.GetOtherPart(part2);
                                if (otherPart != null && partIds.Contains(otherPart.Id) && !regions.ContainsKey(otherPart.Id) && !crossFeedPartIds.Contains(otherPart.Id))
                                {
                                    regions[otherPart.Id] = num2;
                                    partDataQueue.Enqueue(otherPart);
                                }
                            }
                        }
                    }
                }
            }

            return regions;
        }

        private void AddTankToRegionPool(Dictionary<int, Dictionary<string, IFuelSource>> regionPools, int region, FuelTankData fuelTank, List<CraftFuelSource> fuelSources)
        {
            if (!regionPools.TryGetValue(region, out Dictionary<string, IFuelSource> dictionary))
            {
                dictionary = new Dictionary<string, IFuelSource>();
                regionPools[region] = dictionary;
            }

            if (dictionary.TryGetValue(fuelTank.FuelType.Id, out IFuelSource fuelSource1))
            {
                ((CraftFuelSource)fuelSource1).AddFuelTank(fuelTank.Script);
            }
            else
            {
                CraftFuelSource fuelSource2 = this.CreateFuelSource(fuelTank.FuelType, true);
                dictionary[fuelTank.FuelType.Id] = fuelSource2;
                fuelSource2.AddFuelTank(fuelTank.Script);
                fuelSources?.Add(fuelSource2);
            }
        }

        private static bool BorrowMissingPools(Dictionary<int, Dictionary<string, IFuelSource>> regionPools, int sourceRegion, int destRegion)
        {
            if (!regionPools.TryGetValue(sourceRegion, out Dictionary<string, IFuelSource> dictionary1))
                return false;

            if (!regionPools.TryGetValue(destRegion, out Dictionary<string, IFuelSource> dictionary2))
            {
                dictionary2 = new Dictionary<string, IFuelSource>();
                regionPools[destRegion] = dictionary2;
            }

            bool flag = false;
            foreach (KeyValuePair<string, IFuelSource> keyValuePair in dictionary1)
            {
                if (keyValuePair.Value.FuelType.AllowFuelTransfer && !dictionary2.ContainsKey(keyValuePair.Key))
                {
                    dictionary2[keyValuePair.Key] = keyValuePair.Value;
                    flag = true;
                }
            }

            return flag;
        }

        private void BorrowPoolsAcrossCrossFeeds(Dictionary<int, int> regionByPart, Dictionary<int, Dictionary<string, IFuelSource>> regionPools)
        {
            bool flag = true;
            while (flag)
            {
                flag = false;
                foreach (CrossFeedScript crossFeed in _crossFeeds)
                {
                    if (crossFeed.PartScript.Disconnected)
                        continue;

                    PartData data = crossFeed.PartScript.Data;
                    PartData adjacentPart1 = EngineUtilities.GetAdjacentPart(data, crossFeed.Data.AttachPointA);
                    PartData adjacentPart2 = EngineUtilities.GetAdjacentPart(data, crossFeed.Data.AttachPointB);

                    if (adjacentPart1 == null || adjacentPart2 == null)
                        continue;

                    if (!regionByPart.TryGetValue(adjacentPart1.Id, out int num1) || !regionByPart.TryGetValue(adjacentPart2.Id, out int num2))
                        continue;

                    if (num1 == num2)
                        continue;

                    CrossFeedData.CrossFeedMode mode = crossFeed.Data.Mode;
                    if (mode == CrossFeedData.CrossFeedMode.Normal || mode == CrossFeedData.CrossFeedMode.Equalize)
                        flag |= BorrowMissingPools(regionPools, num1, num2);
                    if (mode == CrossFeedData.CrossFeedMode.Reversed || mode == CrossFeedData.CrossFeedMode.Equalize)
                        flag |= BorrowMissingPools(regionPools, num2, num1);
                }
            }
        }

        

        //
        // Summary:
        //     Equalizes the fuel sources via cross feed simulation.
        //
        // Parameters:
        //   sourceA:
        //     The source a.
        //
        //   sourceB:
        //     The source b.
        //
        //   deltaTime:
        //     The delta time.
        private void EqualizeFuelSources(IFuelSource sourceA, IFuelSource sourceB, float deltaTime)
        {
            double totalFuel = sourceA.TotalFuel;
            double totalFuel2 = sourceB.TotalFuel;
            double totalCapacity = sourceA.TotalCapacity;
            double totalCapacity2 = sourceB.TotalCapacity;
            double num = totalCapacity + totalCapacity2;
            if (num > 0.0)
            {
                double num2 = (totalFuel + totalFuel2) / num;
                double num3 = num2 * totalCapacity - totalFuel;
                num3 *= 0.5;
                float num4 = sourceA.FuelType.FuelTransferRate * deltaTime;
                num3 = Mathd.Clamp(num3, 0f - num4, num4);
                if (num3 > 0.0)
                {
                    sourceA.AddFuel(sourceB.RemoveFuel(num3));
                }
                else if (num3 < 0.0)
                {
                    num3 = 0.0 - num3;
                    sourceB.AddFuel(sourceA.RemoveFuel(num3));
                }
            }
        }

        //
        // Summary:
        //     Adds the fuel delta for the specified fuel type to the fuel log for this frame.
        //
        //
        // Parameters:
        //   fuelType:
        //     The fuel type.
        //
        //   fuelDelta:
        //     The fuel delta.
        private void LogFuelUsed(FuelType fuelType, double fuelDelta)
        {
            if (this.FuelUsed != null)
            {
                if (_frameFuelLog == null)
                {
                    _frameFuelLog = new Dictionary<FuelType, double>();
                }

                double value = 0.0;
                _frameFuelLog.TryGetValue(fuelType, out value);
                _frameFuelLog[fuelType] = value + fuelDelta;
            }
        }

        //
        // Summary:
        //     Raises the fuel used events based on the amount of fuel that has been used by
        //     the craft this frame.
        private void RaiseFuelUsedEvents()
        {
            if (this.FuelUsed == null || _frameFuelLog == null)
            {
                return;
            }

            foreach (KeyValuePair<FuelType, double> item in _frameFuelLog)
            {
                double num = 0.0 - item.Value;
                if (num > 9.999999747378752E-05)
                {
                    this.FuelUsed(num, item.Key);
                }
            }
        }

        //
        // Summary:
        //     Sets up the cross feeds.
        //
        // Parameters:
        //   removeDisconnectedCrossFeeds:
        //     if set to true then remove disconnected cross feeds.
        private void SetupCrossFeeds(bool removeDisconnectedCrossFeeds)
        {
            if (removeDisconnectedCrossFeeds)
            {
                foreach (CraftFuelSource fuelSource in this._fuelSources)
                    fuelSource.ClearCrossFeeds();
                CrossFeedScript[] array = this._crossFeeds.ToArray();
                this._equalizeCrossFeeds?.Clear();
                foreach (CrossFeedScript crossFeedScript in array)
                {
                    if (crossFeedScript.PartScript.Disconnected)
                        this._crossFeeds.Remove(crossFeedScript);
                }
            }
            foreach (CrossFeedScript crossFeed in this._crossFeeds)
            {
                FuelTankScript source = (FuelTankScript) null;
                FuelTankScript target = (FuelTankScript) null;
                if (crossFeed.GetFuelTanks(out source, out target) && !source.FuelType.CraftWidePool)
                {
                    if (crossFeed.Data.Mode == CrossFeedData.CrossFeedMode.Equalize)
                    {
                        if (this._equalizeCrossFeeds == null)
                            this._equalizeCrossFeeds = new List<Tuple<IFuelSource, IFuelSource>>();
                        this._equalizeCrossFeeds.Add(new Tuple<IFuelSource, IFuelSource>((IFuelSource) source.CraftFuelSource, (IFuelSource) target.CraftFuelSource));
                    }
                    else
                        target.CraftFuelSource.AddCrossFeedPullSource((IFuelSource) source.CraftFuelSource);
                }
            }
        }

        public void Dispose()
        {
            
        }
    }
}
