using System;
using Assets.Scripts;
using Assets.Scripts.GameLoop;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Flight.MapView;
using ModApi.GameLoop;
using ModApi.Scenes.Events;
using UI.Xml;
using UnityEngine;

namespace Droodism.RadiationBelt
{
    
    public class RadiationBeltManager : MonoBehaviourBase
    {
        public static RadiationBeltManager Instance { get; private set; }
        public RadiationBeltConfig currentConfig;

        private GameObject CurrentRadiationBeltObject;
        public ProceduralRadiationBelt BeltInstance;

        void Awake()
        {
            Instance = this;
        }

       

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
        
        private string lastPlanetName="";
        void Update()
        {
            if (!Game.InFlightScene)
            {
                return;
            }

            if (!Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible||!currentConfig.Enabled)
            {
                return;
            }


           

            try
            {
                this.CurrentRadiationBeltObject.transform.localScale = this.currentConfig.Scale;
                if (lastPlanetName != Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem.AssociatedPlanet.Name)
                {
                    OnFocusPlanet();
                }
                lastPlanetName = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem.AssociatedPlanet.Name;
            }

          
            catch (Exception e)
            {
               Mod.LOGError("fucked1111 "+e.StackTrace);
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

       
        private void OnFocusPlanet()
        {
            var currentName = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem
                .AssociatedPlanet.Name;
            Mod.LOG($"OnFocusPlanet changed,current is {currentName}");
            ChangeBeltParent(ref this.CurrentRadiationBeltObject, currentName);
            this.currentConfig = RadiationBeltConfig.LoadFromFile(currentName);
            if (currentConfig.Enabled)
            {
                ReGenerateMeshes();
            }
            
            Mod.LOG("OnFocusPlanet finished");

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
           
            
            try
            {
                Debug.Log($"{ Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem.AssociatedPlanet.Name}");
            }
            catch (Exception e)
            {
                Mod.LOGError("fucked"+e.StackTrace);
            }
            ReFreshCurrentConfig();
            if (currentConfig.Enabled)
            {
                AddPlanetRadiationBelt(ref this.CurrentRadiationBeltObject, Game.Instance.FlightScene.CraftNode.Parent.Name);
            }
            
        }

        private void ChangeBeltParent(ref GameObject CurrentRadiationBeltObject,string PlanetName)
        {
            var parentGameObject=GetMapPlanet(PlanetName);
            if (parentGameObject == null)
            {
                Mod.LOGError("Parent GameObject is null.");
                return;
            }
            BeltInstance.Parent = parentGameObject;
            CurrentRadiationBeltObject.transform.SetParent(parentGameObject.transform);
            BeltInstance=CurrentRadiationBeltObject.GetComponent<ProceduralRadiationBelt>();
            var renderer = Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>()==null?Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.AddComponent<RadiationBeltCameraRenderer>():Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>();
            renderer.beltRenderer=BeltInstance;
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
            BeltInstance=CurrentRadiationBeltObject.GetComponent<ProceduralRadiationBelt>();
            BeltInstance.Parent = parentGameObject;
            CurrentRadiationBeltObject.transform.SetParent(parentGameObject.transform);
            var renderer = Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>()==null?Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.AddComponent<RadiationBeltCameraRenderer>():Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>();
            renderer.beltRenderer=BeltInstance;

            ReGenerateMeshes();


        }

        public void ReFreshCurrentConfig()
        {
            currentConfig = RadiationBeltConfig.LoadFromFile(Game.Instance.FlightScene.CraftNode.Parent.Name);
        }

        public void ReGenerateMeshes()
        {
            BeltInstance.LoadDataFromConfig(currentConfig);
            BeltInstance.RegenerateMeshes();
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