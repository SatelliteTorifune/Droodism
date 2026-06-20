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
using Droodism.RadiationBelt;
using ModApi.Ui.Inspector;
using System.Xml.Serialization;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.Droodism.Crew;
using Assets.Scripts.Droodism.ResourceWarning;
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

        } 

        public override void OnModLoaded()
        {
          
            try
            {
                base.OnModLoaded();
                var harmony = new Harmony("com.SatelliteTorifune.Droodism");
                CrewManagerSyncPatches.Apply(harmony);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception)
            {
                string s = $"Mod {Mod.ModInfo.Name} failed to Initialize. Verify all depencencies installed and enabled.<br><color=red><size=200%>你他妈加Juno Harmony了吗?";
                Game.Instance.UserInterface.CreateMessageDialog(s);
                throw new FileNotFoundException(s);
            }
            GameObject DroodismGO=new GameObject("DroodismUI");
            DroodismGO.AddComponent<DroodismUIManager>();
            DroodismGO.AddComponent<RadiationBeltManager>();
            DroodismGO.AddComponent<RadiationBeltDebugUI>();
            DroodismGO.AddComponent<DroodismCrewDataManager>();
            DroodismGO.AddComponent<ResourceWarningScript>();
            GameObject.DontDestroyOnLoad(DroodismGO);
            DroodismGO.SetActive(true);
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.MapView, OnBuildMapViewInspectorPanel);
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

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneTransitionCompleted+=OnSceneTransitionCompleted;
            RegisterCommands();
           
            
        }

        /// <summary>
        /// 注册Droodism的自定义指令
        /// </summary>
        private void RegisterCommands()
        {
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
            
            DevConsoleApi.RegisterCommand("RebuildFuelSource",()=>
            {
                var fs = ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript.FuelSources as CraftFuelSources;
                fs.Rebuild(ModApi.Common.Game.Instance.FlightScene.CraftNode.CraftScript);
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
            flag.AllowPlayerControl = true;
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Planted Flag at <color=green> {Game.Instance.FlightScene.CraftNode.Parent.Name} </color>'s surface,at {(ConvertPlanetPositionToLatLongAgl(position).x)}° , {(ConvertPlanetPositionToLatLongAgl(position).y)}° ",true,120f);
        }

        private void  CheckDefaultPlanetRadiationBeltConfig()
        {
            var folderPath = GetRadiationBeltConfigFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            SetUp("Cylero");
            SetUp("Droo");
            SetUp("Earth");
            SetUp("Miros");
            SetUp("Nebra");
            SetUp("Oord");
            SetUp("Orcus");
            SetUp("Sergeaa");
            SetUp("Taurus");
            SetUp("Tydos");
            SetUp("Urados");
            SetUp("Vulco");
            void SetUp(string planet)
            {
                var asset = Mod.ResourceLoader.LoadAsset<TextAsset>("Assets/Resources/DefaultRadiationBeltConfigs/"+planet+".xml");
                if (asset != null)
                {
                    var targetPath = Path.Combine(folderPath, planet+".xml");
                    if (!File.Exists(targetPath))
                    {
                        File.WriteAllText(targetPath, asset.text, Encoding.UTF8);
                    }
                }
            }
            
        }

        private void CheckDefaultBreathablePlanetConfig()
        {
            var folderPath = GetDefaultBreathablePlanetConfigFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            var asset = Mod.ResourceLoader.LoadAsset<TextAsset>("Assets/Resources/BreathablePlanets.xml");
            Log($"[Droodism] CheckDefaultBreathablePlanetConfig: asset={asset != null}, folderPath={folderPath}");
            if (asset != null)
            {
                var targetPath = Path.Combine(folderPath, "BreathablePlanets.xml");
                if (!File.Exists(targetPath))
                {
                    File.WriteAllText(targetPath, asset.text, Encoding.UTF8);
                     Log($"[Droodism] Copied BreathablePlanets.xml to: {targetPath}");
                }
                else
                {
                     Log($"[Droodism] BreathablePlanets.xml already exists at: {targetPath}");
                }
            }
            else
            {
                LogError("[Droodism] Failed to load BreathablePlanets.xml from Resources!");
            }
        }
        private  static string GetRadiationBeltConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + RadiationBeltConfig.CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
            
        }
        private  static string GetDefaultBreathablePlanetConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + BreathablePlanets.CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }

        private void CheckDefaultFlagImage()
        {
            const string defaultImageResourcePath = "Assets/Resources/DefaultFlag1.bytes";
            const string defaultImageFileName = "CustomImage.jpg";

            var folderPath = Path.Combine(Application.persistentDataPath, "UserData", "DroodismConfig", "FlagImage");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var targetPath = Path.Combine(folderPath, defaultImageFileName);
            if (File.Exists(targetPath))
            {
                return;
            }

            TextAsset defaultImageAsset = null;
            try
            {
                defaultImageAsset = Instance.ResourceLoader.LoadAsset<TextAsset>(defaultImageResourcePath);
            }
            catch (Exception e)
            {
                LogError($"[Droodism] Failed to load default flag image asset at '{defaultImageResourcePath}': {e}");
            }
            

            try
            {
                File.WriteAllBytes(targetPath, defaultImageAsset.bytes);
                Log($"[Droodism] Wrote default flag image to: {targetPath}");
            }
            catch (Exception e)
            {
                LogError($"[Droodism] Failed to write default flag image to '{targetPath}': {e}");
            }
        }
        
        
    }
    
}