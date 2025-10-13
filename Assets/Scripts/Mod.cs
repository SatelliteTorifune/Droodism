using System.Xml.Linq;
using Assets.Packages.DevConsole;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Flight;
using ModApi.Scenes.Events;
using HarmonyLib;
using ModApi.Craft;
using ModApi.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.State;
using static ModApi.Common.Game;
using static ModApi.Craft.Parts.PartData;
using Assets.Scripts.State;
using Assembly = System.Reflection.Assembly;

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

        public static Mod Inctance { get; } = GetModInstance<Mod>();
        private CraftScript Craft => Instance.Designer.CraftScript as CraftScript;

        public override void OnModLoaded()
        {
            base.OnModLoaded();
            GameObject DroodismUI=new GameObject("DroodismUI");
            DroodismUI.AddComponent<DroodismUIManager>();
            //DroodismUI.AddComponent<DroodismCrewMananger>();
            GameObject.DontDestroyOnLoad(DroodismUI);
            DroodismUI.SetActive(true);
        }

        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            subPlus();

            if (InDesignerScene)
            {
                Instance.Designer.CraftLoaded += OnCraftLoaded;
                Instance.Designer.CraftStructureChanged+=OnCraftStructureChanged;
                Created += OnPartAdded;
            }

            if (InFlightScene)
            {
                try
                {
                    UpdateDroodCount();
                    Debug.LogFormat("OnSceneLoaded更新Drood数量");
                    那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
                    Debug.LogFormat("OnSceneLoaded执行doShit");
                }
                catch (Exception e1)
                {
                    Debug.LogFormat("你要干啥{0}", e1);
                }
            }

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
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneTransitionCompleted+=OnSceneTransitionCompleted;
            //Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.FlightView,OnBuildFlightViewInspectorPanel);
            DevConsoleApi.RegisterCommand("RefreshFuelSource",那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花);
            DevConsoleApi.RegisterCommand("doIt",要鸡巴干啥);
            DevConsoleApi.RegisterCommand("CreateCrewDataFromGameStates",要鸡巴干啥2);
            DevConsoleApi.RegisterCommand("DroodismSave",要鸡巴干啥3);
            
        }

        void 要鸡巴干啥()
        {
           
            foreach (var crew in DroodismCrewMananger.Instance._crewDataList)
            {
                Debug.LogFormat($"_crewDataList 里面的名字{crew.Name},职位{crew.role},id{crew.id}");
            }
        }

        void 要鸡巴干啥2()
        {
            DroodismCrewMananger.Instance.CreateCrewDataFromGameStates();
        }
        void 要鸡巴干啥3()
        {
            DroodismCrewMananger.Instance.SaveCrewDataToDatabase();
            DroodismCrewMananger.Instance.SaveXml();
        }

        public void 那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花()
        {
            
            try
            {
                foreach (var pd in Game.Instance.FlightScene.CraftNode.CraftScript.Data.Assembly.Parts)
                {
                    if (pd.PartType.Name=="Eva"||pd.PartType.Name=="Eva-Tourist")
                    {
                        pd.PartScript.GetModifier<SupportLifeScript>().RefreshFuelSource();
                    }
                }
            }
            catch (Exception)
            {
              //TODO 爱鸡巴throw就丢
            }
            
        }

        private void OnSceneTransitionCompleted(object sender, SceneTransitionEventArgs e)
        {
            那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
        }
        private void subPlus()
        {
            try
            {
                Instance.FlightScene.Initialized += OnInitialized;
                Debug.LogFormat(" Initialized订阅OnInitialized");
                Instance.FlightScene.CraftChanged += OnCraftChanged;
                Debug.LogFormat(" CraftChanged订阅OnCraftChanged");
                Instance.FlightScene.CraftStructureChanged += OnCraftStructureChangedUI;
                Debug.LogFormat(" CraftStructureChanged订阅OnCraftStructureChangedUI");
                Instance.FlightScene.ActiveCommandPodChanged += OnCraftChanged;
                Debug.LogFormat(" ActiveCommandPodChanged订阅OnCraftChanged");
                Instance.FlightScene.ActiveCommandPodStateChanged += OnCraftChanged;
                Debug.LogFormat(" ActiveCommandPodStateChanged订阅OnCraftChanged");

            }
            catch (Exception e)
            {
                Debug.LogWarningFormat($"订阅有问题,我不知道哪里有问题,但是反正这玩意加个try-catch也能跑{e}");
            }

        }
        //这个函数懒得调用
        private void subMinus()
        {
            Instance.FlightScene.Initialized -= OnInitialized;
            Instance.FlightScene.CraftChanged -= OnCraftChanged;
            Instance.FlightScene.CraftStructureChanged -= OnCraftStructureChangedUI;
            Instance.FlightScene.ActiveCommandPodChanged -= OnCraftChanged;
            Instance.FlightScene.ActiveCommandPodStateChanged -= OnCraftChanged;
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

        
        
        public Vector3d ConvertPlanetPositionToLatLongAgl(Vector3d position)
        {
            if (double.IsNaN(position.x) || double.IsNaN(position.y) || double.IsNaN(position.z))
                return Vector3d.zero;
            IPlanetNode parent = Game.Instance.FlightScene.CraftNode.Parent;
            Vector3d surfaceVector = parent.PlanetVectorToSurfaceVector(position);
            double latitude;
            double longitude;
            parent.GetSurfaceCoordinates(surfaceVector, out latitude, out longitude);
            double num = parent.GetTerrainHeight(position);
            if (parent.PlanetData.HasWater && num < (double) parent.PlanetData.SeaLevel)
                num = (double) parent.PlanetData.SeaLevel;
            return new Vector3d(latitude * 57.29578, longitude * 57.29578, position.magnitude - (parent.PlanetData.Radius + num));
        }
        //傻逼jundroo害我还要帮他们擦屁股
        public static string GetStopwatchTimeString(double seconds)
        {
            if (!Units.IsFinite(seconds))
                return "N/A";
            string empty = string.Empty;
            if (seconds > 31536000.0)
            {
                long num = (long) (seconds / 31536000.0);
                seconds -= (double) (num * 31536000L);
                empty += string.Format("{0:n0}y ", (object) num);
            }
            if (seconds > 86400.0)
            {
                long num = (long) (seconds / 86400.0);
                seconds -= (double) (num * 86400L);
                empty += string.Format("{0:n0}d ", (object) num);
            }
            if (seconds > 3600.0)
            {
                long num = (long) (seconds / 3600.0);
                seconds -= (double) (num * 3600L);
                empty += string.Format("{0:n0}h ", (object) num);
            }
            if (seconds > 60.0)
            {
                long num = (long) (seconds / 60.0);
                seconds -= (double) (num * 60L);
                empty += string.Format("{0:n0}m ", (object) num);
            }
            return empty + string.Format("{0:n2}s", (object) seconds);
        }
        public string FormatFuel(double totalFuel, string[] format)
        {
            // Converts into lowest unit type
            //Code by Chaotic Graviton
            totalFuel *= 1e3;
            if (Math.Abs(totalFuel) > 1e9)
                return (totalFuel * 1e-9).ToString("0.00") + format[3];
            else if (Math.Abs(totalFuel) > 1e6)
                return (totalFuel * 1e-6).ToString("0.00") + format[2];
            else if (Math.Abs(totalFuel) > 1e3)
                return (totalFuel * 1e-3).ToString("0.00") + format[1];
            return totalFuel.ToString("0.00") + format[0];
        }    }
    
}