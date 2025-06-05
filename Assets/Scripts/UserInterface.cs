using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Microsoft.CSharp.RuntimeBinder;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Flight;
using ModApi.Mods;
using ModApi.Ui.Inspector;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
    {
        ICraftScript _craftScript;
        public int DroodCount = 0;
        public int AstronautCount = 0;
        public int TouristCount = 0;
        
        public IFuelSource CraftTotalOxygenFuelSource;
        
        private void OnInitialized(IFlightScene flightScene)
        {
            UpdateDroodCount();
        }
        private void OnCraftChanged(ICraftNode craftNode)
        {
            UpdateDroodCount();
        }

        private void OnCraftStructureChanged()
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
           
          

        }

        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            Debug.Log("OnBuildFlightViewInspectorPanel called");
            var LS = new GroupModel("<color=green><size=105%>Life Support");
            var fs = Game.Instance.Settings.Game.Flight;
            var ui = Game.Instance.FlightScene.FlightSceneUI;
            request.Model.AddGroup(LS);
            
            var DroodCountTextModel = new TextModel("Drood Count",()=>(DroodCount==0?"raft Has No Crew":this.DroodCount.ToString()));
            LS.Add(DroodCountTextModel);
            
            var AstronautCountTextModel = new TextModel("Astronaut Count",()=>(AstronautCount==0?"N/A":this.AstronautCount.ToString()));
            LS.Add(AstronautCountTextModel);
            
            var TouristCountTextModel = new TextModel("Tourist Count",()=>(TouristCount==0?"N/A":this.TouristCount.ToString()));
            LS.Add(TouristCountTextModel);
            
            var textButtonModel = new TextButtonModel(
                "手动更新", b =>
                {
                    UpdateDroodCount();
                    smjb();
                });
            LS.Add(textButtonModel);
            Debug.Log("4");

            
        }

        private void smjb()
        {
            var craftNode = Game.Instance.FlightScene.CraftNode;
            SupportLifeScript sls;
            if (true)
            {
                foreach (var pd in craftNode.CraftScript.Data.Assembly.Parts)
                {
                    foreach (var pmd in pd.Modifiers)
                    {
                        if (pmd.Name.Contains("Support"))
                        {
                            sls = (SupportLifeScript)pmd.GetScript();
                            sls.RefreshFuelSource();
                            Debug.LogFormat("燃料已更新");
                            Game.Instance.FlightScene.FlightSceneUI.ShowMessage("燃料已更新",false,5f);
                        }
                    }
                    
                }
            }
        }

        

    }

    
}