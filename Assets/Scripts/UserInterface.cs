using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Craft.Parts;
using UnityEngine.SceneManagement;
using ModApi.Design.Events;
using ModApi.Scenes.Events;
using ModApi.Craft.Parts.Events;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Flight;
using ModApi.Mods;
using ModApi.Ui.Inspector;
using UnityEngine;
using static ModApi.Common.Game;
using static ModApi.Craft.Parts.PartData;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
    {
        ICraftScript _craftScript;
        public int DroodCount = 0;
        public int AstronautCount = 0;
        public int TouristCount = 0;
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
            
            var DroodCountTextModel = new TextModel("Drood Count (Total) ",()=>(DroodCount==0?"Current Craft Has No Crew":this.DroodCount.ToString()));
            LS.Add(DroodCountTextModel);
            
            var AstronautCountTextModel = new TextModel("Astronaut Count",()=>(AstronautCount==0?"Current Craft Has No Astronaut":this.AstronautCount.ToString()));
            LS.Add(AstronautCountTextModel);
            
            var TouristCountTextModel = new TextModel("Tourist Count",()=>(TouristCount==0?"Current Craft Has No Tourist":this.TouristCount.ToString()));
            LS.Add(TouristCountTextModel);

            void jbhs()
            {
                if (DroodCount == 1)
                {
                    DroodCountTextModel.Visible = false;
                    if (TouristCount == 0)
                        TouristCountTextModel.Visible = false;
                    if (AstronautCount == 0)
                        AstronautCountTextModel.Visible = false;
                }
            }
            

            var textButtonModel = new TextButtonModel(
                "Update Drood Count", b =>
                {
                    UpdateDroodCount();
                    jbhs();
                    ui.ShowMessage("Updated",false,5f);
                });
            LS.Add(textButtonModel);
            Debug.Log("4");

            var currentOxygen = new ProgressBarModel("Current Oxygen", () => 1f / 10f);
            LS.Add(currentOxygen);
            
        }


    }

    
}