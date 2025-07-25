using System;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.Flight;
using ModApi.Math;
using ModApi.Mods;
using ModApi.Ui.Inspector;
using UnityEngine;
using Debug = UnityEngine.Debug;
using TextButtonModel = ModApi.Ui.Inspector.TextButtonModel;

namespace Assets.Scripts
{
    // Main class for the mod, inheriting from GameMod
 
    public partial class Mod : GameMod
    {
        // Reference to the craft script
        ICraftScript _craftScript;
        // Count of droods (crew members)
        public int DroodCount = 0;
        // Count of astronauts
        public int AstronautCount = 0;
        // Count of tourists
        public int TouristCount = 0;

        // Reference to the oxygen fuel source
        private IFuelSource _oxygenSource;
        // Reference to the water fuel source
        private IFuelSource _waterSource;
        // Reference to the food fuel source
        private IFuelSource _foodSource;
        
        private IFuelSource _co2Source,_wastedWaterSource,_solidWasteSource;
        
        // Reference to the support life script
        private SupportLifeScript _support;
        // Flag indicating if the craft is an Eva
        private bool isEva;

        // Total oxygen consume rate
        private double oxygenConsumeRateTotal;
        // Total food consume rate
        private double foodConsumeRateTotal;
        // Total water consume rate
        private double waterConsumeRateTotal;
        
        

        // Method called when the mod is initialized
        private void OnInitialized(IFlightScene flightScene)
        {
            doShit();
            // Update the drood count when the mod is initialized
            UpdateDroodCount();
            Debug.Log("OnInitialized called UpdateDroodCount");
            doShit();
        }

        // Method called when the craft changes
        private void OnCraftChanged(ICraftNode craftNode)
        {
            // Update the drood count when the craft changes
            UpdateDroodCount();
            Debug.Log("OnCraftChanged called UpdateDroodCount");
        }

        // Method called when the craft structure changes in the UI
        private void OnCraftStructureChangedUI()
        {
            // Update the drood count when the craft structure changes in the UI
            UpdateDroodCount();
            Debug.Log("OnCraftStructureChangedUI calledUpdateDroodCount");
        }

        // Method to update the count of droods, astronauts, and tourists on the craft
        public void UpdateDroodCount()
        {
            
            // Get the current craft node from the game instance
            var craftScript = Game.Instance.FlightScene.CraftNode.CraftScript;
            
            // Reset the counts of droods, astronauts, and tourists
            DroodCount = 0;
            AstronautCount = 0;
            TouristCount = 0;
            
            // Iterate through each part in the craft's assembly
            foreach (var partData in craftScript.Data.Assembly.Parts)
            {
                // Check if the part type name contains "Eva"
                if (partData.PartType.Name.Contains("Eva"))
                {
                    if (partData.PartType.Name.Contains("Chair"))
                    {
                        return;
                    }
                    // Increment the drood count
                    DroodCount++;
                    try
                    {
                        partData.PartScript.GetModifier<SupportLifeScript>().UpdateCurrentPlanet();
                        //partData.PartScript.GetModifier<SupportLifeScript>().RefreshFuelSource();
                    }
                    catch (Exception e)
                    {
                       Debug.LogFormat("完犊子了，{0}",e);
                    }
                    // Set the flag indicating that the craft is an Eva 
                    // Check if the part type name is exactly "Eva"
                    if (partData.PartType.Name == "Eva")
                    {
                        // Increment the astronaut count
                        AstronautCount++;
                    }
                    else
                    {
                        // Increment the tourist count
                        TouristCount++;
                    }
                }
            }
            // Update the total fuel sources
            UpdateTotalFuel();
            // Update the total consume rates
            UpdateTotalConsumeRate();
        }
        
        // Method to get the fuel source of a specific type from the entire craft
        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            // Retrieve all fuel sources from the craft's script
            var craftSources = Game.Instance.FlightScene.CraftNode.CraftScript.FuelSources.FuelSources;
            // Iterate through each fuel source
            foreach (var source in craftSources)
            {
                // Check if the fuel source's type name contains the specified fuel type
                if (source.FuelType.Id.Contains(fuelType))
                {
                    // Return the fuel source if it matches the specified type
                    return source;
                }
            }
            // Return null if no matching fuel source is found
            return null;
        }

        // Method to get the fuel source of a specific type from the craft's local sources (root part modifiers)
        private IFuelSource GetLocalFuelSource(string fuelType)
        {
            if (isEva)
            {
                // Retrieve all modifiers from the craft's root part
                var craftSources = Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Modifiers;
            
                // Iterate through each modifier
                foreach (var source in craftSources)
                {
                    // Check if the modifier's name contains "Tank"
                    if (source.GetData().Name.Contains("Tank"))
                    {
                        // Disable the inspector for this modifier
                        source.GetData().InspectorEnabled = false;
                        // Cast the modifier to FuelTankScript
                        FuelTankScript fts = source as FuelTankScript;
                        // Check if the fuel tank's type name contains the specified fuel type
                        if (fts.FuelType.Id.Contains(fuelType))
                        {
                            // Return the fuel tank if it matches the specified type
                            return fts;
                        }
                    }
                
                }
                
            }
            else
            {
                return null;
            }
            return null;
            
        } // Method to get the total fuel source for the craft    
        // Method to update the total fuel sources for oxygen, food, and water
        private void UpdateTotalFuel()
        {
            // Check if the craft is a single Eva part
            if (DroodCount == 1 && Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count == 1 && Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva"))
            {
                // Set the flag indicating that the craft is an Eva
                isEva = true;
                // Get local fuel sources for oxygen, food, and water
                _oxygenSource = GetLocalFuelSource("Oxygen");
                _foodSource = GetLocalFuelSource("Food");
                _waterSource = GetLocalFuelSource("H2O");
                _co2Source = GetLocalFuelSource("CO2");
                _wastedWaterSource = GetLocalFuelSource("Wasted Water");
                _solidWasteSource = GetLocalFuelSource("Solid Waste");
            }
            else
            {
                // Set the flag indicating that the craft is not a single Eva
                isEva = false;
                // Get craft fuel sources for oxygen, food, and water
                _oxygenSource = GetCraftFuelSource("Oxygen");
                _foodSource = GetCraftFuelSource("Food");
                _waterSource = GetCraftFuelSource("H2O");
                _co2Source = GetCraftFuelSource("CO2");
                _wastedWaterSource = GetCraftFuelSource("Wasted Water");
                _solidWasteSource = GetCraftFuelSource("Solid Waste");
                // If the craft fuel source is null or empty, try to get the local fuel source
                void UpdateTotalFuelHandler(string fuelType, ref IFuelSource source)
                {
                    if (fuelType=="Wasted Water"||fuelType=="CO2"||fuelType=="Solid Waste")
                    {
                        if (source==null)
                        {
                            source = new EmptyFuel();
                        }
                    }
                    else
                    {
                        if (source==null || source.IsEmpty)
                        {
                            source=GetLocalFuelSource(fuelType);
                            if (source == null)
                            {
                                source = new EmptyFuel();
                            }
                        } 
                    }
                    
                }
                UpdateTotalFuelHandler("Oxygen", ref _oxygenSource);
                UpdateTotalFuelHandler("Food", ref _foodSource);
                UpdateTotalFuelHandler("H2O", ref _waterSource);
                UpdateTotalFuelHandler("CO2", ref _co2Source);
                UpdateTotalFuelHandler("Wasted Water", ref _wastedWaterSource);
                UpdateTotalFuelHandler("Solid Waste", ref _solidWasteSource);
                
            }
        }

        // Method to update the total consume rate for oxygen, food, and water based on the number of crew members and their states
        private void UpdateTotalConsumeRate()
        {
            // Initialize total consume rates to zero
            oxygenConsumeRateTotal = 0;
            waterConsumeRateTotal = 0;
            foodConsumeRateTotal = 0;
            
            // Check if the craft is an Eva
            if (isEva)
            {
                // Get the SupportLifeScript modifier from the root part
                _support = Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.GetModifier<SupportLifeScript>();
                // Calculate the total consume rates for oxygen, water, and food, considering if the system is running and if the Eva is a tourist
                oxygenConsumeRateTotal = _support.Data.OxygenComsumeRate * (_support.isRunning ? 1.75 : 1) * (_support.isTourist ? 1.05 : 1);
                waterConsumeRateTotal = _support.Data.WaterComsumeRate * (_support.isRunning ? 1.75 : 1) * (_support.isTourist ? 1.05 : 1);
                foodConsumeRateTotal = _support.Data.FoodComsumeRate * (_support.isRunning ? 1.75 : 1) * (_support.isTourist ? 1.05 : 1);
            }
            else
            {
                // Iterate through each part in the craft's assembly
                foreach (var part in Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts)
                {
                    // Check if the part is an Eva
                    if (part.PartType.Name.Contains("Eva"))
                    {
                        if (part.PartType.Name.Contains("Chair"))
                        {
                            return;
                        }
                        // Get the SupportLifeScript modifier from the Eva part
                        var evaPart = part.PartScript;
                        _support = evaPart.GetModifier<SupportLifeScript>();
                        // Calculate the total consume rates based on the type of Eva (Astronaut or Tourist)
                        if (part.PartType.Name == "Eva")
                        {
                            oxygenConsumeRateTotal += _support.Data.OxygenComsumeRate;
                            waterConsumeRateTotal += _support.Data.WaterComsumeRate;
                            foodConsumeRateTotal += _support.Data.FoodComsumeRate;
                        }
                        if (part.PartType.Name == "Eva-Tourist")
                        {
                            oxygenConsumeRateTotal += _support.Data.OxygenComsumeRate * 1.05;
                            waterConsumeRateTotal += _support.Data.WaterComsumeRate * 1.05;
                            foodConsumeRateTotal += _support.Data.FoodComsumeRate * 1.05;
                        }
                    }
                }
            }
        }
        
        // Method to build the flight view inspector panel with life support information
        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            
            Debug.Log("OnBuildFlightViewInspectorPanel called");
            try
            {
                // Update the drood count (number of crew members)
                UpdateDroodCount();
                Debug.Log("OnBuildFlightViewInspectorPanel called UpdateDroodCount");
                
            }
            catch (Exception e)
            {
                // Log any exceptions that occur during the update
                Debug.Log(e.Message);
            }
            // Create a new group model for life support information
            var LS = new GroupModel("<color=green><size=105%>Life Support");
            // Retrieve game settings and UI for flight scene
            var fs = Game.Instance.Settings.Game.Flight;
            var ui = Game.Instance.FlightScene.FlightSceneUI;
            // Add the life support group to the request model
            request.Model.AddGroup(LS);
            // Create a text model for the drood count and add it to the life support group
            var DroodCountTextModel = new TextModel("Drood Count",
                () => (DroodCount == 0 ? "Craft Has No Crew" : this.DroodCount.ToString()));
            LS.Add(DroodCountTextModel);

            // Create a text model for the astronaut count and add it to the life support group
            var AstronautCountTextModel = new TextModel("Astronaut Count",
                () => (AstronautCount == 0 ? "N/A" : this.AstronautCount.ToString()));
            LS.Add(AstronautCountTextModel);

            // Create a text model for the tourist count and add it to the life support group
            var TouristCountTextModel = new TextModel("Tourist Count",
                () => (TouristCount == 0 ? "N/A" : this.TouristCount.ToString()));
            LS.Add(TouristCountTextModel);
            
            // Create a progress bar model for the oxygen percentage and add it to the life support group
            var OxygenProgressBarModel = new ProgressBarModel("Oxygen Percentage", () =>
                (float)(_oxygenSource.TotalFuel / _oxygenSource.TotalCapacity));
            LS.Add(OxygenProgressBarModel);
            LS.Add(new TextModel("Oxygen Percentage",
                () => Units.GetPercentageString((float)(_oxygenSource.TotalFuel / _oxygenSource.TotalCapacity))));
            // Create a text model for the oxygen supply time and add it to the life support group
            var OxygenTime = new TextModel("Oxygen Supply Time",
                () => ($"{GetStopwatchTimeString(_oxygenSource.TotalFuel / (oxygenConsumeRateTotal * (isEva ? (Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.GetModifier<SupportLifeScript>().isTourist ? 1.05 : 1) : 1) * (isEva ? (Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.GetModifier<SupportLifeScript>().isRunning ? 1.75 : 1) : 1)))}"));
            LS.Add(OxygenTime);
            
            // Create a progress bar model for the water percentage and add it to the life support group
            var WaterProgressBarModel = new ProgressBarModel("Water Percentage", () =>
                (float)(_waterSource.TotalFuel / _waterSource.TotalCapacity));
            LS.Add(WaterProgressBarModel);
            LS.Add(new TextModel("Water Percentage",
                () => Units.GetPercentageString((float)(_waterSource.TotalFuel / _waterSource.TotalCapacity))));
            // Create a text model for the water supply time and add it to the life support group
            var WaterTime = new TextModel("Water Supply Time",
                () => ($"{GetStopwatchTimeString(_waterSource.TotalFuel / (waterConsumeRateTotal * (isEva ? (Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.GetModifier<SupportLifeScript>().isTourist ? 1.05 : 1) : 1) * (isEva ? (Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.GetModifier<SupportLifeScript>().isRunning ? 1.75 : 1) : 1)))}"));
            LS.Add(WaterTime);
            
            // Create a progress bar model for the food percentage and add it to the life support group
            var FoodProgressBarModel = new ProgressBarModel("Food Percentage", () =>
                (float)(_foodSource.TotalFuel / _foodSource.TotalCapacity));
            LS.Add(FoodProgressBarModel);
            LS.Add(new TextModel("Food Percentage",
                () => Units.GetPercentageString((float)(_foodSource.TotalFuel / _foodSource.TotalCapacity))));
            // Create a text model for the food supply time and add it to the life support group
            var FoodTime = new TextModel("Food Supply Time",
                () => ($"{GetStopwatchTimeString(_foodSource.TotalFuel / (foodConsumeRateTotal * (isEva ? (Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.GetModifier<SupportLifeScript>().isTourist ? 1.05 : 1) : 1) * (isEva ? (Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.GetModifier<SupportLifeScript>().isRunning ? 1.75 : 1) : 1)))}"));
            LS.Add(FoodTime);
            
            var CO2ProgressBarModel = new ProgressBarModel("CO2 level", () =>
                (float)(_co2Source.TotalFuel / _co2Source.TotalCapacity));
            LS.Add(CO2ProgressBarModel);
            LS.Add(new TextModel("CO2 Percentage",
                () => Units.GetPercentageString((float)(_co2Source.TotalFuel / _co2Source.TotalCapacity))));
            var WastedWaterProgressBarModel = new ProgressBarModel("Wasted Water level", () =>
                (float)(_wastedWaterSource.TotalFuel / _wastedWaterSource.TotalCapacity));
            LS.Add(WastedWaterProgressBarModel);
            LS.Add(new TextModel("Wasted Water Percentage",
                () => Units.GetPercentageString((float)(_wastedWaterSource.TotalFuel / _wastedWaterSource.TotalCapacity))));
            var SolidWasteProgressBarModel = new ProgressBarModel("Solid Waste level", () =>
                (float)(_solidWasteSource.TotalFuel / _solidWasteSource.TotalCapacity));
            LS.Add(SolidWasteProgressBarModel);
            LS.Add(new TextModel("Solid Waste Percentage",
                () => Units.GetPercentageString((float)(_solidWasteSource.TotalFuel / _solidWasteSource.TotalCapacity))));
            
            LS.Add(new TextButtonModel(
                "Manual Update",
                b => UpdateDroodCount()));
            LS.Add(new TextButtonModel(
                "Plant Flag",
                b => SpawnFlag()));
             
        }
        
    }
    public class EmptyFuel: IFuelSource
    {
        public double AddFuel(double amount)
        {
            return 0;
        }

        public double RemoveFuel(double amount)
        {
            return 0;
        }

        public FuelTransferMode FuelTransferMode { get; set; }
        public FuelType FuelType { get; set; }
        public bool IsDestroyed { get; }
        public bool IsEmpty { get; }
        public Vector3 Position { get; }
        public int Priority { get; }
        public int SubPriority { get; }
        public bool SupportsFuelTransfer { get; }

        public double TotalCapacity
        {
            get { return 0; }

            set
            {
                this.TotalCapacity = value;
            }

        }

        public double TotalFuel
        {
            get
            {
                return 0;
            }
            set
            {
                this.TotalFuel = value;
            }

        }

    }
}
