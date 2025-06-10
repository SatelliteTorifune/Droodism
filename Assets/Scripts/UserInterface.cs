using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Flight.Sim;
using Microsoft.CSharp.RuntimeBinder;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Flight;
using ModApi.Math;
using ModApi.Mods;
using ModApi.Ui.Inspector;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
    {
        ICraftScript _craftScript;
        public int DroodCount = 0;
        public int AstronautCount = 0;
        public int TouristCount = 0;

        private IFuelSource _oxygenSource;
        private IFuelSource _waterSource;
        private IFuelSource _foodSource;
        
        private SupportLifeScript _support;
        private bool isEva;

        private double oxygenConsumeRateTotal;
        private double foodConsumeRateTotal;
        private double waterConsumeRateTotal;
        
        public IFuelSource CraftTotalOxygenFuelSource;
        
        private void OnInitialized(IFlightScene flightScene)
        {
            UpdateDroodCount();
        }
        private void OnCraftChanged(ICraftNode craftNode)
        {
            UpdateDroodCount();
        }

        private void OnCraftStructureChangedUI()
        {
            UpdateDroodCount();
        }

       
        private void UpdateDroodCount()
        {
            
           var craftNode = Game.Instance.FlightScene.CraftNode;
           
            DroodCount = 0;
            AstronautCount = 0;
            TouristCount = 0;
            
           foreach (var partData in craftNode.CraftScript.Data.Assembly.Parts)
           {
               if (partData.PartType.Name.Contains("Eva"))
               {
                   DroodCount++;
                   if (partData.PartType.Name=="Eva")
                   {
                       AstronautCount++;
                   }
                   else
                   {
                       TouristCount++;
                   }
               }
           }
           UpdateFuelCalcul();
           GetComsubtionData();


        }
        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            var craftSources = Game.Instance.FlightScene.CraftNode.CraftScript.FuelSources.FuelSources;
                foreach (var source in craftSources)
                {
                    if (source.FuelType.Name.Contains(fuelType))
                    {
                        return source;
                    }
                } 
            return null;
        }
        private IFuelSource GetLocalFuelSource(string fuelType)
        {
            var craftSources = Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Modifiers;
            
            foreach (var source in craftSources)
            {
                if ( source.GetData().Name.Contains("Tank"))
                {
                    source.GetData().InspectorEnabled = false;
                    FuelTankScript fts=source as FuelTankScript;
                    if (fts.FuelType.Name.Contains(fuelType))
                    {
                        return fts;
                    }
                }
                
            }
            return null;
        }
        private void UpdateFuelCalcul()
        {
            if (DroodCount == 1&&Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts.Count==1&&Game.Instance.FlightScene.CraftNode.CraftScript.RootPart.Data.PartType.Name.Contains("Eva"))
            {
                isEva = true;
                
            }
            else
            {
                isEva = false;
                _oxygenSource = GetCraftFuelSource("Oxygen");
                _foodSource = GetCraftFuelSource("Food");
                _waterSource = GetCraftFuelSource("Water");
                if (_oxygenSource==null||_oxygenSource.IsEmpty)
                {
                    _oxygenSource=GetLocalFuelSource("Oxygen");
                }
                if (_foodSource==null||_foodSource.IsEmpty)
                {
                    _foodSource=GetLocalFuelSource("Food");
                }
                if (_waterSource==null||_waterSource.IsEmpty)
                {
                    _waterSource=GetLocalFuelSource("Water");
                }
                
            }
        }
        private void GetComsubtionData()
        {
            oxygenConsumeRateTotal = 0;
            foodConsumeRateTotal= 0;
            waterConsumeRateTotal= 0;
            foreach (var pd in Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts)
            {
                if (pd.PartType.Name.Contains("Eva"))
                {
                    bool flag;
                    if (pd.PartType.Name.Contains("Tourist"))
                    {
                        flag = true;
                    }
                    else
                        flag = false;
                    foreach (var pmd in pd.Modifiers)
                    {
                        if (pmd.Name.Contains("Life"))
                        {
                            var lss = pmd.GetScript() as SupportLifeScript;
                            oxygenConsumeRateTotal += lss.Data.OxygenComsumeRate*(flag ? 1.05 : 1);
                            foodConsumeRateTotal += lss.Data.FoodComsumeRate*(flag ? 1.05 : 1);
                            waterConsumeRateTotal += lss.Data.WaterComsumeRate*(flag ? 1.05 : 1);
                        }
                    }
                }
            }
        }
        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            Debug.Log("OnBuildFlightViewInspectorPanel called");
            var LS = new GroupModel("<color=green><size=105%>Life Support");
            var fs = Game.Instance.Settings.Game.Flight;
            var ui = Game.Instance.FlightScene.FlightSceneUI;
            request.Model.AddGroup(LS);

            var DroodCountTextModel = new TextModel("Drood Count",
                () => (DroodCount == 0 ? "Craft Has No Crew" : this.DroodCount.ToString()));
            LS.Add(DroodCountTextModel);

            var AstronautCountTextModel = new TextModel("Astronaut Count",
                () => (AstronautCount == 0 ? "N/A" : this.AstronautCount.ToString()));
            LS.Add(AstronautCountTextModel);

            var TouristCountTextModel = new TextModel("Tourist Count",
                () => (TouristCount == 0 ? "N/A" : this.TouristCount.ToString()));
            LS.Add(TouristCountTextModel);
            /*
            var OxygenProgressBarModel = new ProgressBarModel("Oxygen:Percentage",()=>
            (float)(_oxygenSource.TotalFuel/_oxygenSource.TotalCapacity));
            LS.Add(OxygenProgressBarModel);
            
            var OxygenTime = new TextModel("Oxygen Supply Time",
                () => ($"{Units.GetStopwatchTimeString(_oxygenSource.TotalFuel/(isEva?_support.Data.OxygenComsumeRate:oxygenConsumeRateTotal))}"));
            LS.Add(OxygenTime);
            
            var WaterProgressBarModel = new ProgressBarModel("Water Percentage",()=>
                (float)(_waterSource.TotalFuel/_waterSource.TotalCapacity));
            LS.Add(WaterProgressBarModel);
            var WaterTime = new TextModel("Water Supply Time",
                () => ($"{Units.GetStopwatchTimeString(_waterSource.TotalFuel/(isEva?_support.Data.WaterComsumeRate:waterConsumeRateTotal))}"));
            LS.Add(WaterTime);
            
            var FoodProgressBarModel = new ProgressBarModel("Food Percentage",()=>
                (float)(_foodSource.TotalFuel/_foodSource.TotalCapacity));
            LS.Add(FoodProgressBarModel);
            var FoodTime = new TextModel("Food Supply Time",
                () => ($"{Units.GetStopwatchTimeString(_foodSource.TotalFuel/(isEva?_support.Data.FoodComsumeRate:foodConsumeRateTotal))}"));
            LS.Add(FoodTime);*/
        
        }

        

    }

    
}