using System;
using Assets.Scripts;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.GameLoop;
using ModApi.Scenes.Events;
using UnityEngine;

namespace Droodism.RadiationBelt
{
    public class DroodismRadiationBeltManager : MonoBehaviourBase
    {
        public static DroodismRadiationBeltManager Instance { get; private set; }

       

        void Awake()
        {
            Instance = this;
        }

        public GameObject CurrentRadiationBeltObject;

        private void Start()
        {
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;

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

            //事件触发的时候GO还没加载好
            void Subscribe()
            {
                Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
                Game.Instance.FlightScene.Initialized += OnInitialized;
            }

            void UnSubscribe()
            {
                Game.Instance.FlightScene.FlightEnded -= OnFlightEnded;
                Game.Instance.FlightScene.Initialized -= OnInitialized;
            }

        }

        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {

        }

        private void OnInitialized(IFlightScene flightScene)
        {
            AddPlanetRadiationBelt(ref this.CurrentRadiationBeltObject,
                GetMapPlanet(Game.Instance.FlightScene.CraftNode.Parent.Name));
        }

        private void AddPlanetRadiationBelt(ref GameObject CurrentRadiationBeltObject,GameObject parentGameObject)
        {
            if (parentGameObject == null)
            {
                Debug.LogWarning("Parent GameObject is null.");
                return;
            }

            
            CurrentRadiationBeltObject = Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/PlanetBelt.prefab"));
            if (CurrentRadiationBeltObject == null)
            {
                Debug.Log("Belt Object not found");
                return;
            }
            CurrentRadiationBeltObject.GetComponent<ProceduralRadiationBelt>().Parent = parentGameObject;
            CurrentRadiationBeltObject.transform.SetParent(parentGameObject.transform);
            var renderer = Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>()==null?Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.AddComponent<RadiationBeltCameraRenderer>():Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject.GetComponent<RadiationBeltCameraRenderer>();
            renderer.beltRenderer=CurrentRadiationBeltObject.GetComponent<ProceduralRadiationBelt>();
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