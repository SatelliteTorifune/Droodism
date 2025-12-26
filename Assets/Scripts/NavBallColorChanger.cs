using Assets.Scripts.Flight.UI.Navball;
using HarmonyLib;
using ModApi.Flight.UI;
using UnityEngine;

namespace Assets.Scripts
{
    //开个新坑,改改navball颜色
    [HarmonyPatch]
    public class NavballColorPatch
    {
        private static Color inertialTop = ColorUtility.TryParseHtmlString("#808080", out inertialTop)? inertialTop : new Color(0, 0, 0, 1);
        private static Color inertialBottom = ColorUtility.TryParseHtmlString("#000000", out inertialBottom)? inertialBottom : new Color(0, 0, 0, 1);
        
        private static Color surfaceTop = ColorUtility.TryParseHtmlString("#0059CC", out surfaceTop)? surfaceTop : new Color(0, 0, 0, 1);
        private static Color surfaceBottom = ColorUtility.TryParseHtmlString("#AA5500", out surfaceBottom)? surfaceBottom : new Color(0, 0, 0, 1);
        
        private static Color targetTop = ColorUtility.TryParseHtmlString("#E68B8B", out targetTop)? targetTop : new Color(0, 0, 0, 1);
        private static Color targetBottom = ColorUtility.TryParseHtmlString("#A21919", out targetBottom)? targetBottom : new Color(0, 0, 0, 1);
        
        // 缓存 Initialise 方法
        private static readonly System.Reflection.MethodInfo InitialiseMethod =
            typeof(NavballRendererControllerScript).GetMethod("Initialise",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        //懒狗,虽然但是还有拉点系我还没写
        private static Color GetColor(bool isTop)
        {
            var _navSphere = Game.Instance.FlightScene.FlightSceneUI.NavSphere;
            if (!isTop)
            {
                
                if (_navSphere.VelocityMode == NavSphereVelocityMode.Surface)
                    return surfaceBottom;
                if (_navSphere.VelocityMode == NavSphereVelocityMode.Target)
                {
                    if (_navSphere.Target != null)
                    {
                        return targetBottom;
                    }
                }
                return inertialBottom;
            }

            if (isTop)
            {
                if (_navSphere.VelocityMode == NavSphereVelocityMode.Surface)
                    return surfaceTop;
                if (_navSphere.VelocityMode == NavSphereVelocityMode.Target)
                {
                    if (_navSphere.Target != null)
                    {
                        return targetTop;
                    }
                }
                return inertialTop;

            }
            return Color.black;
            
        }
        
        // 拦截 BottomColor 的 setter
        [HarmonyPatch(typeof(NavballRendererControllerScript), "BottomColor", MethodType.Setter)]
        [HarmonyPrefix]
        static bool BottomColorSetterPrefix(NavballRendererControllerScript __instance, Color value, ref Material ____navballMaterial)
        {
            
            if (ModSettings.Instance.AltNavBallColor)
            {
                InitialiseMethod.Invoke(__instance, null);
                ____navballMaterial.SetColor("_BottomColour", GetColor(false));
                return false;  
            }

            return !ModSettings.Instance.AltNavBallColor;
        }

        // 拦截 TopColor 的 setter
        [HarmonyPatch(typeof(NavballRendererControllerScript), "TopColor", MethodType.Setter)]
        [HarmonyPrefix]
        static bool TopColorSetterPrefix(NavballRendererControllerScript __instance, Color value,
            ref Material ____navballMaterial)
        {
            if (ModSettings.Instance.AltNavBallColor)
            {
                InitialiseMethod.Invoke(__instance, null);
                ____navballMaterial.SetColor("_TopColour", GetColor(true));

                return false;
            }

            return !ModSettings.Instance.AltNavBallColor;
        }
    }
}