using System.Collections.Generic;
using Droodism.RadiationBelt;
using ModApi;
using ModApi.Craft.Parts.Attributes;
using ModApi.Flight.Sim;
using ModApi.Planet;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod
    {
        private void OnBuildMapViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            GroupModel groupModel = new("<color=yellow>Radiation Belt</color>");
            request.Model.AddGroup(groupModel);
            groupModel.Collapsed = true;
            if (ModSettings.Instance.DebugMode)
            {
                groupModel.Add(new TextButtonModel("Debug Menu", (b) =>
                {
                    RadiationBeltDebugUI.Instance.OnToggleInspectorPanelState();
                }));

            }

            groupModel.Add(new TextModel("Current Planet", () => RadiationBeltManager.Instance.CurrentFocusPlanet));
            groupModel.Add(new ToggleModel("Show General", () => RadiationBeltDebugUI.Instance.ShowGeneral,b =>
            {
                RadiationBeltDebugUI.Instance.ShowGeneral = b;
            }));
            groupModel.Add(new ToggleModel("Show Inner", () => RadiationBeltDebugUI.Instance.ShowInner,b =>
            {
                RadiationBeltDebugUI.Instance.ShowInner = b;
            }));
            groupModel.Add(new ToggleModel("Show Outer", () => RadiationBeltDebugUI.Instance.ShowOuter,b =>
            {
                RadiationBeltDebugUI.Instance.ShowOuter = b;
            }));
            
            groupModel.Add(new TextModel("Belt Tilt (Deg)", () => ConfigText(c =>
            {
                if (c.Enabled)
                {
                    return FloatText(c.beltTiltDegrees, 2);
                }
                return "N/A";
              
            })));
            
            groupModel.Add(new TextModel("Inner Dist", () => ConfigText(c => DistanceKmText(c, c.innerDist))));
            groupModel.Add(new TextModel("Inner Radius", () => ConfigText(c => DistanceKmText(c, c.innerRadius))));
            groupModel.Add(new TextModel("Inner Border Radius", () => ConfigText(c => DistanceKmText(c, c.innerBorderRadius))));
            groupModel.Add(new TextModel("Inner Peak Dose Rate (rad/h)", () => ConfigText(c => DoseRateText(c, c.innerPeakDoseRateRadPerHour))));
            
            groupModel.Add(new TextModel("Outer Dist", () => ConfigText(c => DistanceKmText(c, c.outerDist))));
            groupModel.Add(new TextModel("Outer Radius", () => ConfigText(c => DistanceKmText(c, c.outerRadius))));
            groupModel.Add(new TextModel("Outer Border Radius", () => ConfigText(c => DistanceKmText(c, c.outerBorderRadius))));
            groupModel.Add(new TextModel("Outer Peak Dose Rate (rad/h)", () => ConfigText(c => DoseRateText(c, c.outerPeakDoseRateRadPerHour))));
        }

        private static string ConfigText(System.Func<RadiationBeltConfig, string> selector)
        {
            var manager = RadiationBeltManager.Instance;
            if (manager == null || manager.CurrentConfig == null)
            {
                return "N/A";
            }

            return selector(manager.CurrentConfig);
        }

        private static string FloatText(float value, int decimals)
        {
            return value.ToString("n" + Mathf.Max(0, decimals));
        }

        private string DistanceKmText(RadiationBeltConfig config, float normalizedDistance)
        {
            if (config == null || !config.Enabled)
            {
                return "N/A";
            }

            string currentPlanet = RadiationBeltManager.Instance?.CurrentFocusPlanet;
            IPlanetData planet = FindPlanet(currentPlanet);
            if (planet == null)
            {
                return "N/A";
            }

            float km = normalizedDistance * (float)planet.Radius / 1000f;
            return $"{FloatText(km, 1)} km";
        }

        private string DoseRateText(RadiationBeltConfig config, float radPerHour)
        {
            if (config == null || !config.Enabled)
            {
                return "N/A";
            }

            return FloatText(radPerHour, 2);
        }

        private IPlanetData FindPlanet(string name)
        {
            foreach (var childPlanet in Game.Instance.FlightScene.FlightState.SolarSystemData.Planets)
            {
                if (childPlanet.Name == name)
                {
                    return childPlanet;
                }
            }
            return (IPlanetData) null;
        }
        
    }
}