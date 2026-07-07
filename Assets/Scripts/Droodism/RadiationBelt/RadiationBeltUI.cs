using System.Collections.Generic;
using Assets.Scripts.Droodism.RadiationBelt;
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
            if (!Game.InFlightScene)
            {
                return;
            }
            GroupModel groupModel = new("<color=yellow>" + Locale.GetString("Droodism.RadiationBeltUI.RadiationBelt") + "</color>");
            request.Model.AddGroup(groupModel);
            groupModel.Collapsed = true;
            if (ModSettings.Instance.DebugMode)
            {
                groupModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltUI.DebugMenu"), (b) =>
                {
                    RadiationBeltDebugUI.Instance.OnToggleInspectorPanelState();
                }));

            }

            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.CurrentPlanet"), () => RadiationBeltManager.Instance.CurrentFocusPlanet));
            groupModel.Add(new ToggleModel(Locale.GetString("Droodism.RadiationBeltUI.ShowGeneral"), () => RadiationBeltDebugUI.Instance.ShowGeneral,b =>
            {
                RadiationBeltDebugUI.Instance.ShowGeneral = b;
            }));
            groupModel.Add(new ToggleModel(Locale.GetString("Droodism.RadiationBeltUI.ShowInner"), () => RadiationBeltDebugUI.Instance.ShowInner,b =>
            {
                RadiationBeltDebugUI.Instance.ShowInner = b;
            }));
            groupModel.Add(new ToggleModel(Locale.GetString("Droodism.RadiationBeltUI.ShowOuter"), () => RadiationBeltDebugUI.Instance.ShowOuter,b =>
            {
                RadiationBeltDebugUI.Instance.ShowOuter = b;
            }));
            
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.BeltTiltDeg"), () => ConfigText(c =>
            {
                if (c.Enabled)
                {
                    return FloatText(c.beltTiltDegrees, 2);
                }
                return Locale.GetString("Droodism.DroodismUIManager.NotAvailable");
              
            })));
            
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.InnerDist"), () => ConfigText(c => DistanceKmText(c, c.innerDist))));
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.InnerRadius"), () => ConfigText(c => DistanceKmText(c, c.innerRadius))));
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.InnerBorderRadius"), () => ConfigText(c => DistanceKmText(c, c.innerBorderRadius))));
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.InnerPeakDoseRateRadPerHour"), () => ConfigText(c => DoseRateText(c, c.innerPeakDoseRateRadPerHour))));
            
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.OuterDist"), () => ConfigText(c => DistanceKmText(c, c.outerDist))));
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.OuterRadius"), () => ConfigText(c => DistanceKmText(c, c.outerRadius))));
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.OuterBorderRadius"), () => ConfigText(c => DistanceKmText(c, c.outerBorderRadius))));
            groupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.OuterPeakDoseRateRadPerHour"), () => ConfigText(c => DoseRateText(c, c.outerPeakDoseRateRadPerHour))));
        }

        private static string ConfigText(System.Func<RadiationBeltConfig, string> selector)
        {
            var manager = RadiationBeltManager.Instance;
            if (manager == null || manager.CurrentConfig == null)
            {
                return Locale.GetString("Droodism.DroodismUIManager.NotAvailable");
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
                return Locale.GetString("Droodism.DroodismUIManager.NotAvailable");
            }

            string currentPlanet = RadiationBeltManager.Instance?.CurrentFocusPlanet;
            IPlanetData planet = FindPlanet(currentPlanet);
            if (planet == null)
            {
                return Locale.GetString("Droodism.DroodismUIManager.NotAvailable");
            }

            float km = normalizedDistance * (float)planet.Radius / 1000f;
            return $"{FloatText(km, 1)} km";
        }

        private string DoseRateText(RadiationBeltConfig config, float radPerHour)
        {
            if (config == null || !config.Enabled)
            {
                return Locale.GetString("Droodism.DroodismUIManager.NotAvailable");
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