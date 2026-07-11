using System.Reflection;
using Assets.Scripts.Flight;
using HarmonyLib;

namespace Assets.Scripts.HarmonyPatches
{
    [HarmonyPatch(typeof(TimeManager), "RequestPauseChange")]
    public class PauseResumePatch
    {
        static readonly FieldInfo _modeIndexField = typeof(TimeManager).GetField("_modeIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo _unPauseIndexField = typeof(TimeManager).GetField("_unPauseIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        
        static bool Prefix(TimeManager __instance, bool paused, bool userInitiated)
        {
            if (!ModSettings.Instance.LegacyPauseResume.Value)
            {
                return true;
            }
            if (paused)
            {
                if ((int)_modeIndexField.GetValue(__instance) > 0)
                {
                    _unPauseIndexField.SetValue(__instance, 2);
                }
                __instance.SetMode(0, false);
            }
            else
            {
                __instance.SetMode((int)_unPauseIndexField.GetValue(__instance), false);
            }
               
            return false;
        }
    }
}