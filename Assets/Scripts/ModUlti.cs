using System;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Mfd;
using Assets.Scripts.Flight;
using Assets.Scripts.Vizzy.UI;
using ModApi;
using ModApi.Craft.Parts;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.State;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod
    {
       
        public void SpawnFlag() 
        {
            var templateText = Mod.ResourceLoader.LoadAsset<TextAsset>("Assets/Content/Resources/flag.xml");
            var craftData = Game.Instance.CraftLoader.LoadCraftImmediate(XDocument.Parse(templateText.text).Root);
            var xml = craftData.GenerateXml((Transform)null, false, true);
            Vector3d position = Game.Instance.FlightScene.CraftNode.Position;
            double latitude = ConvertPlanetPositionToLatLongAgl(position).x;
            double longitude=ConvertPlanetPositionToLatLongAgl(position).y;
            var location = new LaunchLocation(
                "location",
                LaunchLocationType.SurfaceLockedGround,
                Game.Instance.FlightScene.CraftNode.Parent.PlanetData.Name,
                latitude,
                longitude,
                new Vector3d(0.0, 0.0, 3000.0),
                0,
                0.5);
            var flag = ((FlightSceneScript)Game.Instance.FlightScene).SpawnCraft($"Flag at {Game.Instance.FlightScene.CraftNode.Parent.Name},{(ConvertPlanetPositionToLatLongAgl(position).x)} ,{(ConvertPlanetPositionToLatLongAgl(position).y)}", craftData, location, xml);
            flag.AllowPlayerControl = true;
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage(
                string.Format(Locale.GetString("Droodism.ModUlti.FlagPlanted"),
                    Game.Instance.FlightScene.CraftNode.Parent.Name,
                    ConvertPlanetPositionToLatLongAgl(position).x,
                    ConvertPlanetPositionToLatLongAgl(position).y),
                true, 120f);
        }
        public Vector3d ConvertPlanetPositionToLatLongAgl(Vector3d position)
        {
            if (double.IsNaN(position.x) || double.IsNaN(position.y) || double.IsNaN(position.z))
                return Vector3d.zero;
            IPlanetNode parent = Game.Instance.FlightScene?.CraftNode?.Parent;
            if (parent == null)
                return Vector3d.zero;
            Vector3d surfaceVector = parent.PlanetVectorToSurfaceVector(position);
            double latitude;
            double longitude;
            parent.GetSurfaceCoordinates(surfaceVector, out latitude, out longitude);
            double num = parent.GetTerrainHeight(position);
            if (parent.PlanetData.HasWater && num < (double)parent.PlanetData.SeaLevel)
                num = (double)parent.PlanetData.SeaLevel;
            return new Vector3d(latitude * 57.29578, longitude * 57.29578,
                position.magnitude - (parent.PlanetData.Radius + num));
        }
        

        public string FormatFuel(double totalFuel, string[] format)
        {
            // Converts into lowest unit type
            //Code by Chaotic Graviton
            totalFuel *= 1e3;
            if (Math.Abs(totalFuel) > 1e9)
                return (totalFuel * 1e-9).ToString("0.00") + format[3];
            else if (Math.Abs(totalFuel) > 1e6)
                return (totalFuel * 1e-6).ToString("0.00") + format[2];
            else if (Math.Abs(totalFuel) > 1e3)
                return (totalFuel * 1e-3).ToString("0.00") + format[1];
            return totalFuel.ToString("0.00") + format[0];
        }
        
        public static float GetDeltaTimeHours()
        {
            float deltaSeconds = 0f;
            if (Game.Instance?.FlightScene?.TimeManager != null)
            {
                deltaSeconds = Mathf.Max(0f, (float)Game.Instance.FlightScene.TimeManager.DeltaTime);
            }
            if (deltaSeconds <= 1e-6f)
            {
                deltaSeconds = Mathf.Max(0f, Time.deltaTime);
            }
            if (deltaSeconds <= 1e-6f)
            {
                deltaSeconds = Mathf.Max(0f, Time.unscaledDeltaTime);
            }
            return deltaSeconds / 3600f;
        }

        public static void Log(object message)
        {
            if (ModSettings.Instance.DebugMode)
            {
                Debug.unityLogger.Log(message);
            }
        }

        public static void Log(string format, params object[] args)
        {
            if (ModSettings.Instance.DebugMode)
            {
                Debug.unityLogger.LogFormat(LogType.Log, format, args);
            }
        }

        public static void LogWarning(string format, params object[] args)
        {
            if (ModSettings.Instance.DebugMode)
            {
                Debug.unityLogger.LogFormat(LogType.Log, format, args);
            }
        }

        public static void LogError(string format, params object[] args)
        {
            if (ModSettings.Instance.DebugMode)
            {
                Debug.unityLogger.LogFormat(LogType.Log, format, args);
                Debug.LogFormat(Environment.StackTrace);
            }
        }

        public static void Log(UnityEngine.Object context, string format, params object[] args)
        {
            if (ModSettings.Instance.DebugMode)
            {
                Debug.unityLogger.LogFormat(LogType.Log, context, format, args);
            }
        }
        

    }
}