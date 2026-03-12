using System;
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

       

        void Awake()
        {
            Instance = this;
        }

        public GameObject CurrentRadiationBeltObject;

        private void Start()
        {
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnForegroundStateChanged;
            Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanging += OnForegroundStateChanging;

        }

        void OnForegroundStateChanged(bool b)
        {
            Mod.LOG("OnForegroundStateChanged"+b);
        }

        void OnForegroundStateChanging(bool b)
        {
          Mod.LOG("OnForegroundStateChanging"+b);
        }

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

            
        }

        public void OnSceneLoaded(object sender, SceneEventArgs e)
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
                Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Initialized += OnMapViewInitialized;
            }

            void UnSubscribe()
            {
                Game.Instance.FlightScene.FlightEnded -= OnFlightEnded;
                Game.Instance.FlightScene.Initialized -= OnFlightSceneInitialized;
                Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Initialized -= OnMapViewInitialized;
            }

        }
        

        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            CurrentRadiationBeltObject = null;
        }

        private void OnFlightSceneInitialized(IFlightScene flightScene)
        {
            
        }

        private void OnMapViewInitialized(IMapView mapView)
        {
            currentConfig = RadiationBeltConfig.LoadFromFile(Game.Instance.FlightScene.CraftNode.Parent.Name);
            if (currentConfig==null)
            {
                return;
            }
            AddPlanetRadiationBelt(ref this.CurrentRadiationBeltObject, Game.Instance.FlightScene.CraftNode.Parent.Name);
        }

        private void AddPlanetRadiationBelt(ref GameObject CurrentRadiationBeltObject,string PlanetName)
        {
            var parentGameObject=GetMapPlanet(PlanetName);
            if (parentGameObject == null)
            {
                Mod.LOGError("Parent GameObject is null.");
                return;
            }
            

            
            CurrentRadiationBeltObject = Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/PlanetBelt.prefab"));
            if (CurrentRadiationBeltObject == null)
            {
                Mod.LOG("Belt Object not found");
                return;
            }
            var Belt=CurrentRadiationBeltObject.GetComponent<ProceduralRadiationBelt>();
            Belt.Parent = parentGameObject;
            CurrentRadiationBeltObject.transform.SetParent(parentGameObject.transform);
            var renderer = Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>()==null?Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.AddComponent<RadiationBeltCameraRenderer>():Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>();
            renderer.beltRenderer=Belt;
            
            Belt.LoadDataFromConfig(currentConfig);
            Belt.RegenerateMeshes();
            
            
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