using System;
using System.Collections.Generic;
using Assets.Scripts.Flight.MapView;
using Assets.Scripts.Menu.MapView;
using ModApi;
using ModApi.Flight.MapView;
using ModApi.GameLoop;
using ModApi.Planet;
using ModApi.Scenes.Events;
using UnityEngine;

namespace Assets.Scripts.Droodism.RadiationBelt
{
    public class MenuMapRadiationBeltManager : MonoBehaviourBase
    {
        public static MenuMapRadiationBeltManager Instance { get; private set; }
        public List<ProceduralRadiationBelt> BeltList = new List<ProceduralRadiationBelt>();

        private RadiationBeltCameraRenderer _cameraRenderer;
        private bool _initialized;
        private string _currentFocusPlanet;
        private readonly Dictionary<string, double> _planetRadiusMetersByName = new Dictionary<string, double>();

        #region Unity Lifecycle

        void Awake() { Instance = this; }

        private void Start()
        {
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            Game.Instance.SceneManager.SceneLoaded -= OnSceneLoaded;
            UnsubscribeMenuMapViewEvents();
        }

        private void Update()
        {
            if (!Game.InMenuScene)
            {
                if (_initialized) 
                {
                    Cleanup(); 
                    return;
                    
                }
            }
            if (!_initialized) { TryInitialize(); return; }
            if (_cameraRenderer == null) return;

            var mapViewMgr = FindObjectOfType<MapViewManagerScript>();
            if (mapViewMgr?.MapView == null || !mapViewMgr.MapView.Visible) return;

            string currentPlanet = GetCurrentFocusedPlanet(mapViewMgr);
            if (currentPlanet != null && _currentFocusPlanet != currentPlanet)
                OnFocusPlanetChanged(currentPlanet);
            _currentFocusPlanet = currentPlanet;
        }

        #endregion

        #region Scene Events

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Menu")
            {
                _initialized = false;
                BeltList.Clear();
                _planetRadiusMetersByName.Clear();
                _currentFocusPlanet = null;
            }
        }

        private void OnMenuMapViewClosed()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void TryInitialize()
        {
            var menuMapView = MenuMapViewScript.Instance;
            if (menuMapView == null) return;

            var mapViewMgr = FindObjectOfType<MapViewManagerScript>();
            if (mapViewMgr?.MapView == null) return;

            try
            {
                CachePlanetRadii(menuMapView);
                CreateAllBelts(menuMapView, mapViewMgr);
                AttachCameraRenderer(mapViewMgr);
                MenuMapViewScript.Closed += OnMenuMapViewClosed;
                _currentFocusPlanet = GetCurrentFocusedPlanet(mapViewMgr) ?? "Droo";
                _initialized = true;
                Mod.Log("MenuMapRadiationBeltManager: Initialization complete.");
            }
            catch (Exception ex)
            {
                Mod.LogError($"MenuMapRadiationBeltManager: Initialization failed: {ex}");
                Cleanup();
            }
        }

        private void CachePlanetRadii(MenuMapViewScript menuMapView)
        {
            var solarSystem = menuMapView.GetComponentInChildren<SolarSystemDataScript>();
            if (solarSystem == null) { Mod.LogError("MenuMapRadiationBeltManager: SolarSystemDataScript not found!"); return; }

            foreach (PlanetDataScript planetData in solarSystem.Planets)
            {
                try { if (planetData != null) _planetRadiusMetersByName[planetData.Name] = planetData.Radius; }
                catch (Exception ex) { Mod.LogError($"Failed to get radius for {planetData.Name}: {ex.Message}"); }
            }
        }

        private void CreateAllBelts(MenuMapViewScript menuMapView, MapViewManagerScript mapViewMgr)
        {
            if (menuMapView.FlightStateData == null) return;
            foreach (var planetNode in menuMapView.FlightStateData.PlanetNodes)
            {
                try { AddPlanetRadiationBelt(planetNode.Name, mapViewMgr.MapView); }
                catch (Exception ex) { Mod.LogError($"Failed to create belt for {planetNode.Name}: {ex.Message}"); }
            }
        }

        private void AddPlanetRadiationBelt(string planetName, IMapView mapView)
        {
            GameObject planetParent = GetMapPlanet(planetName, mapView);
            if (planetParent == null) return;

            var beltObj = Instantiate(
                Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/PlanetBelt.prefab"));
            if (beltObj == null) { Mod.LogError("Failed to load PlanetBelt.prefab."); return; }

            var belt = beltObj.GetComponent<ProceduralRadiationBelt>();
            if (belt == null) { Destroy(beltObj); Mod.LogError("PlanetBelt.prefab missing ProceduralRadiationBelt."); return; }

            belt.Parent = planetParent;
            beltObj.transform.SetParent(planetParent.transform);
            beltObj.transform.localScale = Vector3.one;

            var config = RadiationBeltConfig.LoadFromFile(planetName);
            if (config.Enabled) { belt.LoadDataFromConfig(config); belt.RegenerateMeshesAsync(); }

            BeltList.Add(belt);
        }

        private void AttachCameraRenderer(MapViewManagerScript mapViewMgr)
        {
            var cam = mapViewMgr.MapViewCamera;
            if (cam == null) { Mod.LogError("MapViewCamera is null."); return; }

            var camGo = cam.gameObject;
            _cameraRenderer = camGo.GetComponent<RadiationBeltCameraRenderer>();
            if (_cameraRenderer == null) _cameraRenderer = camGo.AddComponent<RadiationBeltCameraRenderer>();

            if (BeltList.Count > 0) _cameraRenderer.beltRenderer = BeltList[0];
        }

        #endregion

        #region Focus Planet Handling

        private static string GetCurrentFocusedPlanet(MapViewManagerScript mapViewMgr)
        {
            try
            {
                var selected = mapViewMgr.MapView?.MapViewInspector?.SelectedItem;
                return selected?.AssociatedPlanet?.Name;
            }
            catch { return null; }
        }

        private void OnFocusPlanetChanged(string newPlanetName)
        {
            var belt = GetBeltForPlanet(newPlanetName);
            if (belt != null && _cameraRenderer != null)
                _cameraRenderer.beltRenderer = belt;

            var config = RadiationBeltConfig.LoadFromFile(newPlanetName);
            if (RadiationBeltManager.Instance != null)
            {
                RadiationBeltManager.Instance.CurrentConfig = config;
                RadiationBeltManager.Instance.CurrentFocusPlanet = newPlanetName;
            }

            if (belt != null) belt.LoadDataFromConfig(config);
        }

        public void ReGenerateCurrentMeshes()
        {
            if (string.IsNullOrEmpty(_currentFocusPlanet)) return;
            var belt = GetBeltForPlanet(_currentFocusPlanet);
            if (belt == null) return;
            var config = RadiationBeltManager.Instance?.CurrentConfig;
            if (config != null) belt.LoadDataFromConfig(config);
            belt.RegenerateMeshesAsync();
        }

        private ProceduralRadiationBelt GetBeltForPlanet(string planetName)
        {
            foreach (var belt in BeltList)
                if (belt.Parent != null && belt.Parent.name == planetName)
                    return belt;
            return null;
        }

        #endregion

        #region Cleanup

        public void Cleanup()
        {
            foreach (var belt in BeltList)
                if (belt != null && belt.gameObject != null)
                    Destroy(belt.gameObject);
            BeltList.Clear();

            if (_cameraRenderer != null) { Destroy(_cameraRenderer); _cameraRenderer = null; }

            _initialized = false;
            _planetRadiusMetersByName.Clear();
            _currentFocusPlanet = null;
            UnsubscribeMenuMapViewEvents();
        }

        private void UnsubscribeMenuMapViewEvents()
        {
            MenuMapViewScript.Closed -= OnMenuMapViewClosed;
        }

        #endregion

        #region Helpers

        private static GameObject GetMapPlanet(string planetName, IMapView mapView)
        {
            foreach (var planetNode in mapView.GetCelestialBodies())
                if (planetNode.Name == planetName)
                {
                    var transform = mapView.GetCelestialBodyTransform(planetNode);
                    return transform != null ? transform.gameObject : null;
                }
            return null;
        }

        public float GetCurrentPlanetRenderRadiusUnits()
        {
            if (string.IsNullOrEmpty(_currentFocusPlanet)) return 1f;

            float metersPerUnit = 1e6f;
            if (RadiationBeltManager.Instance?.CurrentConfig != null)
            {
                float configVal = RadiationBeltManager.Instance.CurrentConfig.renderMetersPerUnit;
                if (configVal > 0f) metersPerUnit = configVal;
            }

            if (_planetRadiusMetersByName.TryGetValue(_currentFocusPlanet, out double radiusMeters))
                return Mathf.Max(1e-6f, (float)(radiusMeters / metersPerUnit));
            return 1f;
        }

        #endregion
    }
}
