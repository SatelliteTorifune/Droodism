using System;
using System.Collections.Generic;
using Assets.Scripts;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Flight.MapView;
using ModApi.GameLoop;
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
            foreach (var belt in BeltList)
            {
                try
                {
                    CurrentRadiationBeltObject = null;
                }
                catch (Exception exception)
                {
                    
                }
            }
            
        }

        private void OnFlightSceneInitialized(IFlightScene flightScene)
        {
          
        }
        #endregion

        public string CurrentFocusPlanet { get; private set; }
        void Update()
        {
            if (!Game.InFlightScene||this.currentConfig == null)
            {
                return;
            }
            
            
            if (!Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible||!currentConfig.Enabled)
            {
                return;
            }
         
            if (CurrentRadiationBeltObject==null)
            {
                Mod.Log("CurrentRadiationBeltObject is null.");
                return;
            }
            try
            {
                var currentName = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector
                    .SelectedItem==null?Game.Instance.FlightScene.CraftNode.Parent.Name:Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector
                    .SelectedItem.AssociatedPlanet.Name;
                this.CurrentRadiationBeltObject.transform.localScale = this.currentConfig.Scale;
                if (CurrentFocusPlanet != currentName)
                {
                    OnFocusPlanet();
                }
                CurrentFocusPlanet =currentName;
            }

          
            catch (Exception e)
            {
               Mod.LogError("fucked1111 "+e.StackTrace);
            }
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

       
        private void OnFocusPlanet()
        {
            
            var currentName = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem
                .AssociatedPlanet.Name;
            Mod.Log($"OnFocusPlanet changed,current is {currentName}");
            //ChangeBeltParent(currentName);
            Mod.Log("OnFocusPlanet finished");

        }
        

        

        private void OnMapViewInitialized(IMapView view)
        {

            CurrentFocusPlanet = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector
                .SelectedItem == null
                ? Game.Instance.FlightScene.CraftNode.Parent.Name
                : Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector
                    .SelectedItem.AssociatedPlanet.Name;

            try
            {
                foreach (var planetData in Game.Instance.FlightScene.CraftNode.Parent.PlanetData.SolarSystemData.Planets)
                {
                    if (planetData.Parent != null)
                    {  
                        AddPlanetRadiationBelt(planetData.Name);
                    }
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
                Mod.Log("OnMap Fucked");
            }

        }

        
        private void UpdateRender(ProceduralRadiationBelt beltInstance)
        {
            if (beltInstance == null)
            {
                Mod.Log("RadiationBeltManager.UpdateRender:beltInstance is null.");
                return;
            }
            var renderer = Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>()==null?Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.AddComponent<RadiationBeltCameraRenderer>():Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>();
            renderer.beltRenderer=beltInstance;
        }
        
        

        private void ChangeBeltParent(string PlanetName)
        {
            this.currentConfig = RadiationBeltConfig.LoadFromFile(PlanetName);
            var currentRadiationBelt = GetCurrentRadiationBelt(PlanetName);
            this.CurrentRadiationBeltObject =currentRadiationBelt.gameObject;
            //UpdateRender(currentRadiationBelt);
            
            if (currentConfig.Enabled)
            {
                ReGenerateMeshes();
            }
            
            return;
            var parentGameObject=GetMapPlanet(PlanetName);
            if (parentGameObject == null)
            {
                Mod.LogError("Parent GameObject is null.");
                return;
            }
            BeltInstance.Parent = parentGameObject;
            CurrentRadiationBeltObject.transform.SetParent(parentGameObject.transform);
            BeltInstance=CurrentRadiationBeltObject.GetComponent<ProceduralRadiationBelt>();
            var renderer = Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>()==null?Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.AddComponent<RadiationBeltCameraRenderer>():Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>();
            renderer.beltRenderer=BeltInstance;
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
                beltInstance.RegenerateMeshes();
            }
            this.BeltList.Add(beltInstance);
        }

        

        public void ReFreshCurrentConfig()
        {
            Mod.Log("ReFreshCurrentConfig called");
            currentConfig = RadiationBeltConfig.LoadFromFile(Game.Instance.FlightScene.CraftNode.Parent.Name);
            Mod.Log("ReFreshCurrentConfig end");
        }

        public void ReGenerateMeshes()
        {
            BeltInstance.LoadDataFromConfig(currentConfig);
            BeltInstance.RegenerateMeshes();
        }

        private ProceduralRadiationBelt GetCurrentRadiationBelt(string name)
        {

            if (name==null)
            {
                Mod.Log("ProceduralRadiationBelt name is null");
                return  null;
            }
            foreach (var prb in BeltList)
            {
                if (prb.Parent.name==name)
                {
                    return prb;
                }
            }
            return null;
        }

        public static GameObject GetMapPlanet(string PlanetName)
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
    }
    
   
    
}