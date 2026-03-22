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

        public double GetPlanetRadiusMeters(string planetName)
        {
            if (string.IsNullOrEmpty(planetName)) return 1.0;
            return planetRadiusMetersByName.TryGetValue(planetName, out double radiusMeters)
                ? Math.Max(1e-6, radiusMeters)
                : 1.0;
        }

        public double NormalizedToMeters(double normalizedDistance)
        {
            return normalizedDistance * GetCurrentPlanetRadiusMeters();
        }
        

        public bool TryGetBeltSignedDistance(
            string planetName,
            Vector3 worldPosition,
            bool innerBelt,
            out float signedDistance)
        {
            signedDistance = float.PositiveInfinity;
            var belt = GetCurrentRadiationBelt(planetName);
            if (belt == null)
            {
                return false;
            }

            var cfg = GetRuntimeConfigForPlanet(planetName);
            if (cfg == null || !cfg.Enabled)
            {
                return false;
            }

            belt.LoadDataFromConfig(cfg);

            // Convert world position to belt local coordinates.
            // This keeps the computation space identical to the mesh generation space.
            Vector3 local = belt.transform.InverseTransformPoint(worldPosition);

            signedDistance = innerBelt ? belt.Inner_func(local) : belt.Outer_func(local);
            return true;
        }

        /// <summary>
        /// PCI (planet-centered inertial) meters-space query.
        /// Input position is expected to be centered on the target planet, in meters.
        /// </summary>
        public bool IsInInnerBeltPciMeters(string planetName, Vector3 pciPositionMeters)
        {
            return TryGetBeltSignedDistancePciMeters(planetName, pciPositionMeters, Vector3.zero, true, out float signedDistance) &&
                   signedDistance < 0f;
        }

        /// <summary>
        /// PCI (planet-centered inertial) meters-space query.
        /// Input position is expected to be centered on the target planet, in meters.
        /// </summary>
        public bool IsInOuterBeltPciMeters(string planetName, Vector3 pciPositionMeters)
        {
            return TryGetBeltSignedDistancePciMeters(planetName, pciPositionMeters, Vector3.zero, false, out float signedDistance) &&
                   signedDistance < 0f;
        }

        public bool TryGetBeltSignedDistancePciMeters(
            string planetName,
            Vector3 pciPositionMeters,
            bool innerBelt,
            out float signedDistance)
        {
            return TryGetBeltSignedDistancePciMeters(
                planetName,
                pciPositionMeters,
                Vector3.zero,
                innerBelt,
                out signedDistance);
        }

        public bool TryGetBeltSignedDistancePciMeters(
            string planetName,
            Vector3 pciPositionMeters,
            Vector3 planetCenterPciMeters,
            bool innerBelt,
            out float signedDistance)
        {
            signedDistance = float.PositiveInfinity;
            
            var cfg = GetRuntimeConfigForPlanet(planetName);
            if (cfg == null || !cfg.Enabled)
            {
                return false;
            }

            if (!planetRadiusMetersByName.TryGetValue(planetName, out double radiusMeters))
            {
                return false;
            }

            float invRadius = 1f / Mathf.Max(1e-6f, (float)radiusMeters);
            Vector3 planetRelativeMeters = pciPositionMeters - planetCenterPciMeters;

            // Keep physics query in the same orientation used by rendered belts.
            // Without this, tilted/rotated planets can produce visible mismatch.
            var belt = GetCurrentRadiationBelt(planetName);
            if (belt != null)
            {
                planetRelativeMeters = Quaternion.Inverse(belt.transform.rotation) * planetRelativeMeters;
            }

            Vector3 pNorm = planetRelativeMeters * invRadius;

            signedDistance = innerBelt
                ? EvaluateInnerSignedDistance(cfg, pNorm)
                : EvaluateOuterSignedDistance(cfg, pNorm);
            return true;
        }

        private RadiationBeltConfig GetRuntimeConfigForPlanet(string planetName)
        {
            if (currentConfig != null &&CurrentFocusPlanet== planetName)
            {
                return currentConfig;
            }

            // Fallback for non-focus bodies (should be rare with current gameplay logic).
            return RadiationBeltConfig.LoadFromFile(planetName);
        }

        private static float EvaluateInnerSignedDistance(RadiationBeltConfig cfg, Vector3 p)
        {
            float innerCompression = Mathf.Max(0.01f, cfg.innerCompression);
            float innerExtension = Mathf.Max(0.01f, cfg.innerExtension);
            p.x *= p.x < 0.0f ? innerExtension : innerCompression;

            float innerDeformXY = Mathf.Max(0.01f, cfg.innerDeformXY);
            float innerBorderDeformXY = Mathf.Max(0.01f, cfg.innerBorderDeformXY);
            float q1 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * innerDeformXY) - cfg.innerDist;
            float d1 = Mathf.Sqrt(q1 * q1 + p.y * p.y) - cfg.innerRadius;
            float q2 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * innerBorderDeformXY) - cfg.innerBorderDist;
            float d2 = Mathf.Sqrt(q2 * q2 + p.y * p.y) - cfg.innerBorderRadius;
            return Mathf.Max(d1, -d2) + (cfg.innerDeform > 0.001f
                ? (Mathf.Sin(p.x * 5.0f) * Mathf.Sin(p.y * 7.0f) * Mathf.Sin(p.z * 6.0f)) * cfg.innerDeform
                : 0.0f);
        }

        private static float EvaluateOuterSignedDistance(RadiationBeltConfig cfg, Vector3 p)
        {
            float outerCompression = Mathf.Max(0.01f, cfg.outerCompression);
            float outerExtension = Mathf.Max(0.01f, cfg.outerExtension);
            p.x *= p.x < 0.0f ? outerExtension : outerCompression;

            float outerDeformXY = Mathf.Max(0.01f, cfg.outerDeformXY);
            float outerBorderDeformXY = Mathf.Max(0.01f, cfg.outerBorderDeformXY);
            float q1 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * outerDeformXY) - cfg.outerDist;
            float d1 = Mathf.Sqrt(q1 * q1 + p.y * p.y) - cfg.outerRadius;
            float q2 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * outerBorderDeformXY) - cfg.outerBorderDist;
            float d2 = Mathf.Sqrt(q2 * q2 + p.y * p.y) - cfg.outerBorderRadius;
            return Mathf.Max(d1, -d2) + (cfg.outerDeform > 0.001f
                ? (Mathf.Sin(p.x * 5.0f) * Mathf.Sin(p.y * 7.0f) * Mathf.Sin(p.z * 6.0f)) * cfg.outerDeform
                : 0.0f);
        }
    }
    
   
    
}