using System;
using System.Collections.Generic;
using Assets.Scripts;
using ModApi.Flight;
using ModApi.Flight.MapView;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Planet;
using ModApi.Scenes.Events;
using UnityEngine;
using UnityEngine.Serialization;

namespace Droodism.RadiationBelt
{
    
    public class RadiationBeltManager : MonoBehaviourBase
    {
        public static RadiationBeltManager Instance { get; private set; }
        public RadiationBeltConfig CurrentConfig;

        private GameObject currentRadiationBeltObject;
        public ProceduralRadiationBelt BeltInstance;

       public RadiationBeltCameraRenderer  FlightCameraRenderer,MapCameraRenderer;
        
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
            Game.Instance.SceneManager.SceneTransitionCompleted += OnSceneTransitionCompleted;
        }

        

        public void OnSceneTransitionCompleted(object sender, SceneTransitionEventArgs e)
        {
            if (e.TransitionToScene!="Flight")
            {
                return;
            }
            this.BeltList.Clear();
            this.planetRadiusMetersByName.Clear();
            this.planetRadiusScaledByName.Clear();
            CurrentFocusPlanet = Game.Instance.FlightScene.CraftNode.Parent.Name;
            foreach (IPlanetData planetData in Game.Instance.FlightScene.CraftNode.Parent.PlanetData.SolarSystemData.Planets)
            {
                planetRadiusMetersByName[planetData.Name] = planetData.Radius;
                planetRadiusScaledByName[planetData.Name] = planetData.RadiusScaledSpace;
                AddPlanetRadiationBelt(planetData.Name);
            }

            this.CurrentConfig = RadiationBeltConfig.LoadFromFile(CurrentFocusPlanet);
            var currentRadiationBelt = GetCurrentRadiationBelt(CurrentFocusPlanet);
            this.currentRadiationBeltObject = currentRadiationBelt.gameObject;
            this.FlightCameraRenderer =
                Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject
                    .GetComponent<RadiationBeltCameraRenderer>() == null
                    ? Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject
                        .AddComponent<RadiationBeltCameraRenderer>()
                    : Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.gameObject
                        .GetComponent<RadiationBeltCameraRenderer>();
            FlightCameraRenderer.beltRenderer = currentRadiationBelt;
            this.BeltInstance = GetCurrentRadiationBelt(CurrentFocusPlanet);
            
        }
        #endregion

       
        
        public string CurrentFocusPlanet { get; private set; }
        //这个其实很蠢,我手动写了一个切换时更新的
        void Update()
        {
            if (Game.InFlightScene)
            {

                if (!Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible)
                {
                    return ;
                }

                var currentName = GetCurrentFocusPlanet();

                if (this.CurrentConfig == null)
                {
                    Mod.Log("currentConfig is null");
                    CurrentConfig = RadiationBeltConfig.LoadFromFile(currentName);
                    return;
                }



                if (currentRadiationBeltObject == null)
                {
                    Mod.Log("CurrentRadiationBeltObject is null.");
                    return;
                }

                try
                {
                    // Keep belt scale neutral so visual boundary matches physics query.
                    this.currentRadiationBeltObject.transform.localScale = Vector3.one;
                    if (CurrentFocusPlanet != currentName)
                    {
                        OnFocusPlanetChanged(currentName);
                    }

                    CurrentFocusPlanet = currentName;
                }


                catch (Exception e)
                {
                    Mod.LogError("fucked1111 " + e.StackTrace);
                }
            }

            if (Game.InMenuScene)
            {
                //TODO 
                /*
                if (map in menu stuff active)
                {
                    
                }*/
            }
        }
       
        private void OnFocusPlanetChanged(string currentName)
        {
            
            Mod.Log($"OnFocusPlanet changed,current is {currentName}");
            CurrentConfig = RadiationBeltConfig.LoadFromFile(currentName);
            BeltInstance = GetCurrentRadiationBelt(currentName);
            currentRadiationBeltObject = BeltInstance.gameObject;
            this.FlightCameraRenderer.beltRenderer = BeltInstance;
            // Ensure meshes exist for the newly focused planet if enabled.
            if (CurrentConfig.Enabled)
            {
                BeltInstance.LoadDataFromConfig(CurrentConfig);
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
            currentRadiationBeltObject.transform.localScale = Vector3.one;
            
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
            CurrentConfig = RadiationBeltConfig.LoadFromFile(GetCurrentFocusPlanet());
        }

        public void ReGenerateMeshes()
        {
            BeltInstance.LoadDataFromConfig(CurrentConfig);
            BeltInstance.RegenerateMeshesAsync();
        }

        private ProceduralRadiationBelt GetCurrentRadiationBelt(string nAme)
        {

            try
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
               
            }
            catch (Exception e)
            {
                
            }
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
        
        private double GetCurrentPlanetRadiusMeters()
        {
            if (string.IsNullOrEmpty(CurrentFocusPlanet)) return 1.0;
            EnsurePlanetRadiusCached(CurrentFocusPlanet);
            return planetRadiusMetersByName.TryGetValue(CurrentFocusPlanet, out double radiusMeters)
                ? Math.Max(1e-6, radiusMeters)
                : 1.0;
        }

        private double GetPlanetRadiusMeters(string planetName)
        {
            if (string.IsNullOrEmpty(planetName)) return 1.0;
            EnsurePlanetRadiusCached(planetName);
            return planetRadiusMetersByName.TryGetValue(planetName, out double radiusMeters)
                ? Math.Max(1e-6, radiusMeters)
                : 1.0;
        }
        private void EnsurePlanetRadiusCached(string planetName)
        {
            if (planetName == null)
            {
                return;
            }
            if (planetRadiusMetersByName.ContainsKey(planetName) && planetRadiusScaledByName.ContainsKey(planetName)) {return;}
            if (!Game.InFlightScene || Game.Instance?.FlightScene?.CraftNode?.Parent?.PlanetData?.SolarSystemData?.Planets == null) return;

            foreach (IPlanetData planet in Game.Instance.FlightScene.CraftNode.Parent.PlanetData.SolarSystemData.Planets)
            {
                if (!string.Equals(planet.Name, planetName, StringComparison.Ordinal)) continue;
                planetRadiusMetersByName[planetName] = planet.Radius;
                planetRadiusScaledByName[planetName] = planet.RadiusScaledSpace;
                return;
            }
        }

        /// <summary>
        /// Map render unit helper. In current map view, empirical scale is close to 1 unit = 1000 km.
        /// </summary>
        public float GetCurrentPlanetRenderRadiusUnits()
        {
            //你游这个b scaledSpace害人不浅,所以只能这么用了
            double meters = GetCurrentPlanetRadiusMeters();
            float metersPerUnit = (CurrentConfig != null && CurrentConfig.renderMetersPerUnit > 0f)
                ? CurrentConfig.renderMetersPerUnit
                : 1e6f;
            return Mathf.Max(1e-6f, (float)(meters / metersPerUnit));
        }

        /// <summary>
        /// 判断是否在辐射带内的函数,有点重要说是
        /// </summary>
        /// <param name="planetName"></param>
        /// <param name="pciPositionMeters"></param>
        /// <param name="planetCenterPciMeters"></param>
        /// <param name="inInnerBelt"></param>
        /// <param name="signedDistance"></param>
        /// <returns></returns>
        public bool TryGetBeltSignedDistancePciMeters(RadiationBeltConfig cfg,
            string planetName,
            Vector3 pciPositionMeters,
            bool inInnerBelt,
            out float signedDistance)
        {
            signedDistance = float.PositiveInfinity;
            if (cfg == null || !cfg.Enabled)
            {
                return false;
            }
            

            double radiusMeters = GetPlanetRadiusMeters(planetName);
            if (radiusMeters <= 1.0)
            {
                return false;
            }

            float invRadius = 1f / Mathf.Max(1e-6f, (float)radiusMeters);
            
            var belt = GetCurrentRadiationBelt(planetName);
            if (belt != null)
            {
                pciPositionMeters = Quaternion.Inverse(belt.transform.rotation) * pciPositionMeters;
            }

            Vector3 pNorm = pciPositionMeters * invRadius;

            signedDistance = inInnerBelt
                ? EvaluateInnerSignedDistance(cfg, pNorm)
                : EvaluateOuterSignedDistance(cfg, pNorm);
            return true;
        }

        public bool TryGetBeltIntensityPciMeters(RadiationBeltConfig cfg,
            string planetName,
            Vector3 pciPositionMeters,
            bool inInnerBelt,
            out float intensity,
            out float signedDistance)
        {
            intensity = 0f;
            if (!TryGetBeltSignedDistancePciMeters(cfg, planetName, pciPositionMeters, inInnerBelt, out signedDistance))
            {
                return false;
            }

            if (signedDistance >= 0f)
            {
                intensity = 0f;
                return true;
            }

            float edgeWidth = inInnerBelt
                ? Mathf.Max(1e-4f, cfg.innerIntensityEdgeWidth)
                : Mathf.Max(1e-4f, cfg.outerIntensityEdgeWidth);
            float exponent = inInnerBelt
                ? Mathf.Max(1e-4f, cfg.innerIntensityExponent)
                : Mathf.Max(1e-4f, cfg.outerIntensityExponent);
            float baseIntensity = inInnerBelt
                ? Mathf.Max(0f, cfg.innerBaseIntensity)
                : Mathf.Max(0f, cfg.outerBaseIntensity);

            // signedDistance is normalized in planetary radii; deeper inside => higher intensity.
            float depth01 = Mathf.Clamp01((-signedDistance) / edgeWidth);
            float shaped = Mathf.Pow(depth01, exponent);
            intensity = baseIntensity * shaped;
            return true;
        }

        public bool TryGetTotalBeltIntensityPciMeters(RadiationBeltConfig cfg,
            string planetName,
            Vector3 pciPositionMeters,
            out float totalIntensity,
            out float innerIntensity,
            out float outerIntensity)
        {
            totalIntensity = 0f;
            innerIntensity = 0f;
            outerIntensity = 0f;

            bool innerOk = TryGetBeltIntensityPciMeters(cfg, planetName, pciPositionMeters, true, out innerIntensity, out _);
            bool outerOk = TryGetBeltIntensityPciMeters(cfg, planetName, pciPositionMeters, false, out outerIntensity, out _);
            if (!innerOk && !outerOk)
            {
                return false;
            }

            totalIntensity = innerIntensity + outerIntensity;
            return true;
        }

        public bool TryGetDoseRateRadPerHour(RadiationBeltConfig cfg,
            string planetName,
            Vector3 pciPositionMeters,
            out float totalDoseRateRadPerHour,
            out float innerDoseRateRadPerHour,
            out float outerDoseRateRadPerHour)
        {
            totalDoseRateRadPerHour = 0f;
            innerDoseRateRadPerHour = 0f;
            outerDoseRateRadPerHour = 0f;

            if (!TryGetTotalBeltIntensityPciMeters(cfg, planetName, pciPositionMeters, out _, out float innerIntensity, out float outerIntensity))
            {
                return false;
            }

            float innerBase = Mathf.Max(1e-6f, cfg.innerBaseIntensity);
            float outerBase = Mathf.Max(1e-6f, cfg.outerBaseIntensity);
            float innerPeak = Mathf.Max(0f, cfg.innerPeakDoseRateRadPerHour);
            float outerPeak = Mathf.Max(0f, cfg.outerPeakDoseRateRadPerHour);

            innerDoseRateRadPerHour = innerPeak * Mathf.Clamp01(innerIntensity / innerBase);
            outerDoseRateRadPerHour = outerPeak * Mathf.Clamp01(outerIntensity / outerBase);
            totalDoseRateRadPerHour = innerDoseRateRadPerHour + outerDoseRateRadPerHour;
            return true;
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
        public RadiationBeltConfig GetRuntimeConfigForPlanet(string planetName)
        {
            
            //这个b玩意也蠢,要是你不在当前星球每帧都给你load
            if (CurrentConfig != null &&CurrentFocusPlanet== planetName)
            {
                return CurrentConfig;
            }
            //要是没有那就手动load一下
            return RadiationBeltConfig.LoadFromFile(planetName);
        }
    }
    
   
    
}