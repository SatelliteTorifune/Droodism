using System;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft.Parts;
using ModApi.Flight.Sim;
using ModApi.Math;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod
    {
        public Vector3d ConvertPlanetPositionToLatLongAgl(Vector3d position)
        {
            if (double.IsNaN(position.x) || double.IsNaN(position.y) || double.IsNaN(position.z))
                return Vector3d.zero;
            IPlanetNode parent = Game.Instance.FlightScene.CraftNode.Parent;
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

        //傻逼jundroo害我还要帮他们擦屁股
        public static string GetStopwatchTimeString(double seconds)
        {
            if (!Units.IsFinite(seconds))
                return "N/A";
            string empty = string.Empty;
            if (seconds > 31536000.0)
            {
                long num = (long)(seconds / 31536000.0);
                seconds -= (double)(num * 31536000L);
                empty += string.Format("{0:n0}y ", (object)num);
            }

            if (seconds > 86400.0)
            {
                long num = (long)(seconds / 86400.0);
                seconds -= (double)(num * 86400L);
                empty += string.Format("{0:n0}d ", (object)num);
            }

            if (seconds > 3600.0)
            {
                long num = (long)(seconds / 3600.0);
                seconds -= (double)(num * 3600L);
                empty += string.Format("{0:n0}h ", (object)num);
            }

            if (seconds > 60.0)
            {
                long num = (long)(seconds / 60.0);
                seconds -= (double)(num * 60L);
                empty += string.Format("{0:n0}m ", (object)num);
            }

            return empty + string.Format("{0:n2}s", (object)seconds);
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