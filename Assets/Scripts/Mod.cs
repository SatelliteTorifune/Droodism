using System.Xml.Linq;
using Assets.Packages.DevConsole;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Droodism;
using Assets.Scripts.Droodism.UserInterface;
using Assets.Scripts.Flight;
using ModApi.Scenes.Events;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.State;
using static ModApi.Common.Game;
using static ModApi.Craft.Parts.PartData;
using Assembly = System.Reflection.Assembly;
using Droodism.RadiationBelt;
using ModApi.Ui.Inspector;

namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi;
    using ModApi.Common;
    using ModApi.Mods;
    using UnityEngine;
    using UnityEngine.PlayerLoop;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>

    public partial class Mod : ModApi.Mods.GameMod
    {

        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {

        }

        public static Mod Instance { get; } = GetModInstance<Mod>();

        private CraftScript CurrentCraft()
        {
            return InFlightScene ?ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript as CraftScript:Game.Instance.Designer.CraftScript as CraftScript;

        } 

        public override void OnModLoaded()
        {
            base.OnModLoaded();
            GameObject DroodismGO=new GameObject("DroodismUI");
            DroodismGO.AddComponent<DroodismUIManager>();
            DroodismGO.AddComponent<RadiationBeltManager>();
            DroodismGO.AddComponent<RadiationBeltDebugUI>();
            DroodismGO.AddComponent<DroodismCrewDataManager>();
            GameObject.DontDestroyOnLoad(DroodismGO);
            DroodismGO.SetActive(true);
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.MapView, OnBuildMapViewInspectorPanel);
            
        }
        

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {

            if (InDesignerScene)
            {
                ModApi.Common.Game.Instance.Designer.CraftLoaded+=OnCraftLoaded;
                ModApi.Common.Game.Instance.Designer.CraftStructureChanged+=OnCraftStructureChanged;
                Created += OnPartAdded;
            }

            if (InFlightScene)
            {
                try
                {
                    ModApi.Common.Game.Instance.FlightScene.CraftChanged += OnCraftChanged;
                    PatchCraft(ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript as CraftScript);
                    Log("OnSceneLoaded更新Drood数量");
                    那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
                    Log("OnSceneLoaded执行doShit");
                }
                catch (Exception e1)
                {
                    Log("你要干啥{0}", e1);
                }
            }

        }

        private void OnCraftLoaded()
        {
            PatchCraft(CurrentCraft());
        }

        private void OnCraftStructureChanged()
        {
            GetDroodCountInDesigner();
        }
        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            var harmony = new Harmony("com.SatelliteTorifune.Droodism");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            CrewManagerSyncPatches.Apply(harmony);
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneTransitionCompleted+=OnSceneTransitionCompleted;
            //注册一下指令
            DevConsoleApi.RegisterCommand("RefreshFuelSource",那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花);
            DevConsoleApi.RegisterCommand("ManualRefreshInstance",ManualRefreshInstance);
            DevConsoleApi.RegisterCommand("RBUI", () =>
            {
                if (!Game.InFlightScene)
                {
                  return;   
                }
                if (!Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible)
                {
                    return;   
                }
                RadiationBeltDebugUI.Instance.OnToggleInspectorPanelState();
            });
           
            
        }

        private void OnCraftChanged(ICraftNode craft) => PatchCraft(CurrentCraft());
        public void 那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花()
        {
            
            try
            {
                foreach (var pd in Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts)
                {
                    if (pd.PartType.Name=="Eva"||pd.PartType.Name=="Eva-Tourist")
                    {
                        pd.PartScript.GetModifier<SupportLifeScript>().Refresh();
                    }
                }
            }
            catch (Exception)
            {
              
            }
            
        }

        private void OnSceneTransitionCompleted(object sender, SceneTransitionEventArgs e)
        {
            那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
        }
        
        public void SpawnFlag() 
        {
            var templateText = Mod.ResourceLoader.LoadAsset<TextAsset>("Assets/Content/Resources/flag.xml");
            var craftData = Game.Instance.CraftLoader.LoadCraftImmediate(XDocument.Parse(templateText.text).Root);
            var xml = craftData.GenerateXml((Transform)null, false, true);
            Vector3d position = Game.Instance.FlightScene.CraftNode.Position;
            double latitude = ConvertPlanetPositionToLatLongAgl(position).x;
            double longitude=ConvertPlanetPositionToLatLongAgl(position).y;
            var location = new LaunchLocation(
                "location",
                LaunchLocationType.SurfaceLockedGround,
                Game.Instance.FlightScene.CraftNode.Parent.PlanetData.Name,
                latitude,
                longitude,
                new Vector3d(0.0, 0.0, 3000.0),
                0,
                0.2);
            var flag = ((FlightSceneScript)Game.Instance.FlightScene).SpawnCraft($"Flag at {Game.Instance.FlightScene.CraftNode.Parent.Name},{(ConvertPlanetPositionToLatLongAgl(position).x)} ,{(ConvertPlanetPositionToLatLongAgl(position).y)}", craftData, location, xml);
            flag.AllowPlayerControl = false;
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Planted Flag at <color=green> {Game.Instance.FlightScene.CraftNode.Parent.Name} </color>'s surface,at {(ConvertPlanetPositionToLatLongAgl(position).x)}° , {(ConvertPlanetPositionToLatLongAgl(position).y)}° ",true,120f);
        }
        
        
        
        
    }
    
}