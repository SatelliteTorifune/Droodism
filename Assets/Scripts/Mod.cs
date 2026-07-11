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
using System.IO;
using ModApi.State;
using static ModApi.Common.Game;
using static ModApi.Craft.Parts.PartData;
using Assembly = System.Reflection.Assembly;
using Assets.Scripts.Droodism.RadiationBelt;
using ModApi.Ui.Inspector;
using System.Xml.Serialization;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Droodism.BackGround;
using Assets.Scripts.Droodism.Crew;
using Assets.Scripts.Droodism.ResourceWarning;
using Assets.Scripts.HarmonyPatches;
using Assets.Scripts.Menu.MapView;
using ModApi.Craft.Parts;
using UnityEngine.UI;

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

        } // 反射调用 private 方法
       
        protected override void OnModInitialized()
        {
          
            base.OnModInitialized();
            try
            {
                var harmony = new Harmony("com.SatelliteTorifune.Droodism");
                CrewManagerSyncPatches.Apply(harmony);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception exception)
            {
                string s = $"Mod {Mod.ModInfo.Name} failed to Initialize. Verify all depencencies installed and enabled.<br><color=red><size=200%>你他妈加Juno Harmony了吗?";
                Game.Instance.UserInterface.CreateMessageDialog(s);
                Debug.LogErrorFormat($"Exception occurred while initializing Droodism: {{0}}", exception);
            }
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneTransitionCompleted+=OnSceneTransitionCompleted;
            
        }

        /// <summary>
        /// Called when the mod is fully loaded.
        /// This occurs after the mod is initialized and after mod data is loaded (like part and propulsion data, UI resources, etc.)
        /// </summary>
        public override void OnModLoaded()
        {
          
           
            GameObject DroodismGO=new GameObject("DroodismGameObject");
            DroodismGO.AddComponent<DroodismUIManager>();
            DroodismGO.AddComponent<RadiationBeltManager>();
            DroodismGO.AddComponent<MenuMapRadiationBeltManager>();
            DroodismGO.AddComponent<RadiationBeltDebugUI>();
            DroodismGO.AddComponent<DroodismCrewDataManager>();
            DroodismGO.AddComponent<ResourceWarningScript>();
            //DroodismGO.AddComponent<BackGroundCalulator>();
            GameObject.DontDestroyOnLoad(DroodismGO);
            DroodismGO.SetActive(true);
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.MapView, OnBuildMapViewInspectorPanel);
            RegisterCommands();
            CheckDefaultPlanetRadiationBeltConfig();
            CheckDefaultBreathablePlanetConfig();
            CheckDefaultFlagImage();
            
            

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

        

        /// <summary>
        /// 注册Droodism的自定义指令
        /// </summary>
        private void RegisterCommands()
        {
            DevConsoleApi.RegisterCommand("RefreshFuelSource",那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花);
            DevConsoleApi.RegisterCommand("ManualRefreshInstance",ManualRefreshInstance);
            DevConsoleApi.RegisterCommand("RadiationBeltDebugUI", () =>
            {
                if (Game.InFlightScene)
                {
                    if (!Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible)
                        return;
                }
                else if (!Game.InMenuScene)
                {
                    return;
                }
                if (Game.InMenuScene && MenuMapViewScript.Instance == null)
                    return;

                RadiationBeltDebugUI.Instance.OnToggleInspectorPanelState();
            });


            
            DevConsoleApi.RegisterCommand("RebuildFuelSource",()=>
            {
                var fs = ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.FuelSources as CraftFuelSources;
                fs.Rebuild(ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript);
            });

        }

        private void OnCraftChanged(ICraftNode craft) => PatchCraft(CurrentCraft());
        

        private void OnSceneTransitionCompleted(object sender, SceneTransitionEventArgs e)
        {
            那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
        }
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
        
    }
    
}