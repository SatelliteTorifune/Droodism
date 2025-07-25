#region Assembly SimpleRockets2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SRCraftFuelSources
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
            try
            {
                List<FuelTankData> list = new List<FuelTankData>();
            Dictionary<(int, FuelType), FuelTankScript> dictionary = new Dictionary<(int, FuelType), FuelTankScript>();
            foreach (PartData part in parts)
            {
                part.GetModifiers(list);
                if (!removeDisconnectedCrossFeeds)
                {
                    CrossFeedData modifier = part.GetModifier<CrossFeedData>();
                    if (modifier != null && modifier.Mode != 0)
                    {
                        _crossFeeds.Add(modifier.Script);
                    }
                }
            }
           
            
            foreach (FuelTankData item in list)
            {
                var patch = item?.Part.PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                CraftFuelSource craftFuelSource = null;
                if (item != null && !item.Script.PartScript.Disconnected)
                {
                    
                    if (item.FuelType == FuelType.Battery)
                    {
                        craftFuelSource = item.Part.PartScript.CommandPod?.BatteryFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType == FuelType.Monopropellant)
                    {
                        craftFuelSource = item.Part.PartScript.CommandPod?.MonoFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType == FuelType.Jet)
                    {
                        craftFuelSource = item.Part.PartScript.CommandPod?.JetFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType.Id =="Oxygen")
                    {
                        craftFuelSource=patch?.OxygenFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType.Id =="H2O")
                    {
                        craftFuelSource=patch?.WaterFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType.Id =="Wasted Water")
                    {
                        craftFuelSource=patch?.WastedWaterFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType.Id =="Food")
                    {
                        craftFuelSource=patch?.FoodFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType.Id =="Solid Waste")
                    {
                        craftFuelSource=patch?.SolidWasteFuelSource as CraftFuelSource;
                    }
                    else if (item.FuelType.Id =="CO2")
                    {
                        craftFuelSource=patch?.CO2FuelSource as CraftFuelSource;
                    }
                }

                if (craftFuelSource != null)
                {
                    craftFuelSource?.AddFuelTank(item.Script);
                }
                else
                {
                    dictionary[(item.Part.Id, item.FuelType)] = item.Script;
                }
            }

            (int, FuelType)[] array = dictionary.Keys.ToArray();
            (int, FuelType)[] array2 = array;
            foreach ((int, FuelType) key in array2)
            {
                FuelTankScript fuelTankScript = dictionary[key];
                if (fuelTankScript != null)
                {
                    FuelTankScript fuelTankScript2 = fuelTankScript;
                    CraftFuelSource craftFuelSource2 = CreateFuelSource(fuelTankScript2.Data.FuelType);
                    FindConnectedTanks(fuelTankScript2.PartScript.Data, fuelTankScript2, craftFuelSource2, dictionary);
                    fuelSources?.Add(craftFuelSource2);
                }
            }

            SetupCrossFeeds(removeDisconnectedCrossFeeds);
            }
            catch (Exception e)
            {
                Debug.LogFormat("CreateFuelSourceForConnectedParts歇逼了: {0}",e);
            }
            
            //Debug.Log("Modded CreateFuelSourceForConnectedParts called");
            
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
            //Debug.Log("Patched Rebuild firing");
            _fuelSources.Clear();
            _crossFeeds.Clear();
            foreach (ICommandPod commandPod in craftScript.CommandPods)
            {
                CommandPodScript commandPodScript = commandPod as CommandPodScript;
                STCommandPodPatchScript patchScript = commandPod.Part.PartScript?.GetModifier<STCommandPodPatchScript>();
                commandPodScript.BatteryFuelSource = CreateFuelSource(FuelType.Battery);
                commandPodScript.JetFuelSource = CreateFuelSource(FuelType.Jet, reverseSubPriority: true);
                commandPodScript.MonoFuelSource = CreateFuelSource(FuelType.Monopropellant, reverseSubPriority: true);
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
                        Debug.LogFormat($"SRCCraftFuelSources.Rebuild: Error creating fuel sources: {e}");
                    }
                }
            }

            IReadOnlyList<PartData> parts = craftScript.Data.Assembly.Parts;
            CreateFuelSourceForConnectedParts(parts, removeDisconnectedCrossFeeds: false, null);
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
        private static void FindConnectedTanks(PartData part, FuelTankScript fuelTankScript, CraftFuelSource fuelSource, Dictionary<(int, FuelType), FuelTankScript> lookup)
        {
            if (fuelTankScript != null)
            {
                fuelSource.AddFuelTank(fuelTankScript);
            }

            lookup[(part.Id, fuelSource.FuelType)] = null;
            foreach (PartConnection partConnection in part.PartConnections)
            {
                PartData otherPart = partConnection.GetOtherPart(part);
                FuelTankScript value = null;
                if (lookup.TryGetValue((otherPart.Id, fuelSource.FuelType), out value))
                {
                    if (value != null && value.Data.FuelType == fuelSource.FuelType && EngineUtilities.ConnectedWithFuelLine(partConnection, part, otherPart))
                    {
                        FindConnectedTanks(otherPart, value, fuelSource, lookup);
                    }
                }
                else if (otherPart.Config.FuelLineOverride)
                {
                    FindConnectedTanks(otherPart, null, fuelSource, lookup);
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
                foreach (CraftFuelSource fuelSource in _fuelSources)
                {
                    fuelSource.ClearCrossFeeds();
                }

                CrossFeedScript[] array = _crossFeeds.ToArray();
                _equalizeCrossFeeds?.Clear();
                CrossFeedScript[] array2 = array;
                foreach (CrossFeedScript crossFeedScript in array2)
                {
                    if (crossFeedScript.PartScript.Disconnected)
                    {
                        _crossFeeds.Remove(crossFeedScript);
                    }
                }
            }

            foreach (CrossFeedScript crossFeed in _crossFeeds)
            {
                FuelTankScript source = null;
                FuelTankScript target = null;
                if (!crossFeed.GetFuelTanks(out source, out target))
                {
                    continue;
                }

                if (crossFeed.Data.Mode == CrossFeedData.CrossFeedMode.Equalize)
                {
                    if (_equalizeCrossFeeds == null)
                    {
                        _equalizeCrossFeeds = new List<Tuple<IFuelSource, IFuelSource>>();
                    }

                    _equalizeCrossFeeds.Add(new Tuple<IFuelSource, IFuelSource>(source.CraftFuelSource, target.CraftFuelSource));
                }
                else
                {
                    target.CraftFuelSource.AddCrossFeedPullSource(source.CraftFuelSource);
                }
            }
        }
    }
}