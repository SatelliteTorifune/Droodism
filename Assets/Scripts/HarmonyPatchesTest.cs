

using System.Reflection;
using Assets.Scripts.Flight.GameView.Cameras;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public partial class Mod
    {
        
    }
    [HarmonyPatch(typeof(InteractiveCameraController))]
    [HarmonyPatch("OnDrag")]
    public static class InteractiveCameraController_OnDrag_Patch
    {
        private static readonly MethodInfo CameraRotationOffsetGetter = 
            AccessTools.PropertyGetter(typeof(InteractiveCameraController), "CameraRotationOffset");
    
        private static readonly MethodInfo CameraRotationOffsetSetter = 
            AccessTools.PropertySetter(typeof(InteractiveCameraController), "CameraRotationOffset");

        [HarmonyPrefix]
        public static bool Prefix(InteractiveCameraController __instance, PointerEventData eventData)
        {
            return true;
            //这他妈啥我操
            if (__instance.MouseLook)
                return true;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Vector2 delta = eventData.delta / Game.Instance.Device.Dpi;
                if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    delta *= 160f;
                }
                else
                {
                    delta *= 80f;
                }
                __instance.Rotate(new Vector2(-delta.y, delta.x), true);
            }
            else if (eventData.button == PointerEventData.InputButton.Right && 
                     Game.Instance.FlightScene.TimeManager.Paused)
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))
                {
                    // 通过属性 getter/setter 修改值
                    float currentValue = (float)CameraRotationOffsetGetter.Invoke(__instance, null);
                    CameraRotationOffsetSetter.Invoke(__instance, new object[] { currentValue + eventData.delta.x });
                }
                else
                {
                    __instance.Move(new Vector2(-eventData.delta.x, -eventData.delta.y));
                }
            }

            return false;
        }
    }



    
}
    
    