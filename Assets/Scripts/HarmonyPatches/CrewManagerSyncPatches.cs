using System;
using System.Reflection;
using Assets.Scripts.Droodism.Crew;
using HarmonyLib;
using UnityEngine;

namespace Assets.Scripts
{
    public static class CrewManagerSyncPatches
    {
      
        public static void Apply(Harmony harmony)
        {
            try
            {
                var crewManagerType = AccessTools.TypeByName("Assets.Scripts.State.CrewManager");
                if (crewManagerType == null)
                {
                    Mod.Log("[Droodism] CrewManager type not found, skip crew sync patch.");
                    return;
                }

                var createMethod = crewManagerType.GetMethod("CreateCrewMember", BindingFlags.Instance | BindingFlags.Public);
                if (createMethod == null)
                {
                    Mod.Log("[Droodism] CrewManager.CreateCrewMember() not found, skip crew sync patch.");
                    return;
                }

                var postfix = typeof(CrewManagerSyncPatches).GetMethod(
                    nameof(CreateCrewMember_Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (postfix == null)
                {
                    Mod.Log("[Droodism] CreateCrewMember_Postfix method not found.");
                    return;
                }

                harmony.Patch(createMethod,
                    postfix: new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                Mod.LogError($"[Droodism] Failed to apply CrewManager sync patch: {e}");
            }
        }

        private static void CreateCrewMember_Postfix(object __result)
        {
            try
            {
                // Only save — do NOT call EnsureSyncedWithGameCrewManager here,
                // as the full sync would overwrite CrewRole with GetRandomDroodPost().
                DroodismCrewDataManager.Instance?.Save();
            }
            catch (Exception e)
            {
                Mod.LogError($"[Droodism] CreateCrewMember_Postfix failed: {e}");
            }
        }
    }
}

