using System;
using System.Collections.Generic;
using Assets.Scripts;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Flight.MapView;
using ModApi.GameLoop;
using ModApi.Planet;
using ModApi.Scenes.Events;
using UnityEngine;

namespace Droodism.RadiationBelt
{
    
    public class RadiationBeltManager : MonoBehaviourBase
    {
        public static RadiationBeltManager Instance { get; private set; }
        public RadiationBeltConfig currentConfig;

        private GameObject CurrentRadiationBeltObject;
        public ProceduralRadiationBelt BeltInstance;

        public RadiationBeltCameraRenderer  CameraRenderer;
        
        public List<ProceduralRadiationBelt> BeltList=new List<ProceduralRadiationBelt>();
        private readonly Dictionary<string, double> planetRadiusMetersByName = new Dictionary<string, double>();
        private readonly Dictionary<string, double> planetRadiusScaledByName = new Dictionary<string, double>();
            

        #region NBCS
        void Awake()
        {
            Instance = this;
        }

       

        private void Start()
        { 
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;

        }

        void OnForegroundStateChanged(bool b)
        {
            Mod.Log("OnForegroundStateChanged" + b);
            if (!b)
            {
                return;
            }
            
        }

        void OnForegroundStateChanging(bool b)
        {
          Mod.Log("OnForegroundStateChanging"+b);
        }
        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
         return;
            try
            {
                BeltList = null;
                CurrentRadiationBeltObject = null;
                BeltInstance = null;
                currentConfig = null;
            }
            catch (Exception exception)
            {
            }
            
        }

        private void OnFlightSceneInitialized(IFlightScene flightScene)
        {
          
        }
        #endregion

        public string CurrentFocusPlanet { get; private set; }
        void Update()
        {
            if (!Game.InFlightScene)
            {  
                return;
            }
            if (!Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible)
            {
                return;
            }
            var currentName = GetCurrentFocusPlanet();

            if (this.currentConfig==null)
            {
                Mod.Log("currentConfig is null");
                currentConfig = RadiationBeltConfig.LoadFromFile(currentName);
                return;
            }
            
            
         
            if (CurrentRadiationBeltObject==null)
            {
                Mod.Log("CurrentRadiationBeltObject is null.");
                return;
            }
            try
            {
               //未来加旋转,别急
                //this.CurrentRadiationBeltObject.transform.eulerAngles = Vector3.one;
                if (CurrentFocusPlanet != currentName)
                {
                    OnFocusPlanetChanged(currentName);
                }
                CurrentFocusPlanet =currentName;
            }

          
            catch (Exception e)
            {
               Mod.LogError("fucked1111 "+e.StackTrace);
            }
        }
       
        private void OnFocusPlanetChanged(string currentName)
        {
            
            Mod.Log($"OnFocusPlanet changed,current is {currentName}");
            currentConfig = RadiationBeltConfig.LoadFromFile(currentName);
            BeltInstance = GetCurrentRadiationBelt(currentName);
            CurrentRadiationBeltObject = BeltInstance.gameObject;
            this.CameraRenderer.beltRenderer = BeltInstance;
            // Ensure meshes exist for the newly focused planet if enabled.
            if (currentConfig.Enabled)
            {
                BeltInstance.LoadDataFromConfig(currentConfig);
                BeltInstance.RegenerateMeshesAsync();
            }
            Mod.Log("OnFocusPlanet finished");

        }
        private string lastRemembered;
        
        private string GetCurrentFocusPlanet()
        {
            if (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem == null)
            {
              
                if (Game.Instance.FlightScene.CraftNode==null)
                {
                    return lastRemembered;
                }

                lastRemembered = Game.Instance.FlightScene.CraftNode.Parent.Name;
                return Game.Instance.FlightScene.CraftNode.Parent.Name;
            }

            lastRemembered = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem
                .AssociatedPlanet.Name;
            return Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem.AssociatedPlanet.Name;
        }
        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene != "Flight")
            {
                try
                {
                    UnSubscribe();
                }
                catch (Exception exception)
                {
                }

                return;
            }

            Subscribe();

            //换一个Invoke?
            void Subscribe()
            {
                Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
                Game.Instance.FlightScene.Initialized += OnFlightSceneInitialized;
                Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnForegroundStateChanged;
                Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanging += OnForegroundStateChanging;
                Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Initialized += OnMapViewInitialized;
               
            }

            void UnSubscribe()
            {
                Game.Instance.FlightScene.FlightEnded -= OnFlightEnded;
                Game.Instance.FlightScene.Initialized -= OnFlightSceneInitialized;
                Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged -= OnForegroundStateChanged;
                Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanging -= OnForegroundStateChanging;
                Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Initialized -= OnMapViewInitialized;

            }

        }
        
        private void OnMapViewInitialized(IMapView view)
        {

            this.BeltList.Clear();
            this.planetRadiusMetersByName.Clear();
            this.planetRadiusScaledByName.Clear();
            CurrentFocusPlanet = GetCurrentFocusPlanet();

            try
            {
                foreach (IPlanetData planetData in Game.Instance.FlightScene.CraftNode.Parent.PlanetData.SolarSystemData.Planets)
                {
                    planetRadiusMetersByName[planetData.Name] = planetData.Radius;
                    planetRadiusScaledByName[planetData.Name] = planetData.RadiusScaledSpace;
                    AddPlanetRadiationBelt(planetData.Name);
                }
                
                ReFreshCurrentConfig();
                
                var currentRadiationBelt = GetCurrentRadiationBelt(CurrentFocusPlanet);
                this.CurrentRadiationBeltObject = currentRadiationBelt.gameObject;
                this.CameraRenderer =
                    Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject
                        .GetComponent<RadiationBeltCameraRenderer>() == null
                        ? Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject
                            .AddComponent<RadiationBeltCameraRenderer>()
                        : Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject
                            .GetComponent<RadiationBeltCameraRenderer>();
                CameraRenderer.beltRenderer = currentRadiationBelt;
                this.BeltInstance = GetCurrentRadiationBelt(CurrentFocusPlanet);
            }
            catch (Exception e)
            {
                Mod.Log("OnMapViewInitialized Fucked");
            }

        }
        
        
        private void AddPlanetRadiationBelt(string PlanetName)
        { 
            var parentGameObject=GetMapPlanet(PlanetName);
            if (parentGameObject == null)
            {
                Mod.LogError("Parent GameObject is null.");
                return;
            }
            
            var currentRadiationBeltObject = Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/PlanetBelt.prefab"));
            if (currentRadiationBeltObject == null)
            {
                return;
            }
            var beltInstance=currentRadiationBeltObject.GetComponent<ProceduralRadiationBelt>();
            beltInstance.Parent = parentGameObject;
            currentRadiationBeltObject.transform.SetParent(parentGameObject.transform);
            
            var config = RadiationBeltConfig.LoadFromFile(PlanetName);
            if (config.Enabled)
            {
                beltInstance.LoadDataFromConfig(config);
                beltInstance.RegenerateMeshesAsync();
            }
            this.BeltList.Add(beltInstance);
        }

        

        public void ReFreshCurrentConfig()
        {
            Mod.Log("ReFreshCurrentConfig called");
            currentConfig = RadiationBeltConfig.LoadFromFile(GetCurrentFocusPlanet());
            Mod.Log("ReFreshCurrentConfig end");
        }

        public void ReGenerateMeshes()
        {
            BeltInstance.LoadDataFromConfig(currentConfig);
            BeltInstance.RegenerateMeshesAsync();
            //BeltInstance.RegenerateMeshes();
        }

        private ProceduralRadiationBelt GetCurrentRadiationBelt(string nAme)
        {

            if (nAme==null)
            {
                Mod.Log("ProceduralRadiationBelt name is null");
                return  null;
            }

            if (BeltList==null)
            {
                Mod.Log("List is null");
                return null;
            }
            foreach (var prb in BeltList)
            {
                if (prb.Parent.name==nAme)
                {
                    return prb;
                }
            }
            Mod.Log("NOT FOUND ON list,there are {0} on list",BeltList.Count);
            return null;
        }

        private static GameObject GetMapPlanet(string PlanetName)
        {
            foreach (Transform t in GameObject.FindObjectsOfType<Transform>(true))
            {
                if (t.name == PlanetName)
                {
                    Transform parent = t.parent;
                    if (parent != null && parent.name == "Planets")
                    {
                        Transform mapView = parent.parent;
                        if (mapView != null && mapView.name.Contains("MapView"))
                        {
                            return t.gameObject;
                        }
                    }
                }
            }
            return null;
        }

        public float GetCurrentPlanetRadiusScaledSpace()
        {
            if (string.IsNullOrEmpty(CurrentFocusPlanet))
            {
                return 1f;
            }
            return planetRadiusScaledByName.TryGetValue(CurrentFocusPlanet, out double radiusScaled)
                ? Mathf.Max(1e-6f, (float)radiusScaled)
                : 1f;
        }

        public double GetCurrentPlanetRadiusMeters()
        {
            if (string.IsNullOrEmpty(CurrentFocusPlanet)) return 1.0;
            return planetRadiusMetersByName.TryGetValue(CurrentFocusPlanet, out double radiusMeters)
                ? Math.Max(1e-6, radiusMeters)
                : 1.0;
        }

        public double NormalizedToMeters(double normalizedDistance)
        {
            return normalizedDistance * GetCurrentPlanetRadiusMeters();
        }

        public double MetersToNormalized(double metersDistance)
        {
            return metersDistance / GetCurrentPlanetRadiusMeters();
        }
    }
    
   
    
}