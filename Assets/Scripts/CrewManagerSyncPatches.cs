using System;
using System.Reflection;
using Assets.Scripts.Droodism;
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
                    Debug.LogWarning("[Droodism] CrewManager type not found, skip crew sync patch.");
                    return;
                }

                var createMethod = crewManagerType.GetMethod("CreateCrewMember", BindingFlags.Instance | BindingFlags.Public);
                if (createMethod == null)
                {
                    Debug.LogWarning("[Droodism] CrewManager.CreateCrewMember() not found, skip crew sync patch.");
                    return;
                }

                var postfix = typeof(CrewManagerSyncPatches).GetMethod(
                    nameof(CreateCrewMember_Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (postfix == null)
                {
                    Debug.LogWarning("[Droodism] CreateCrewMember_Postfix method not found.");
                    return;
                }

                harmony.Patch(createMethod,
                    postfix: new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Droodism] Failed to apply CrewManager sync patch: {e}");
            }
        }

        private static void CreateCrewMember_Postfix(object __result)
        {
            // 需求：新 crew 创建后，立即同步 xml 条目（id/name）并把 radiation entry 补齐（没有就补 0）
            try
            {
                DroodismCrewDataManager.Instance?.EnsureSyncedWithGameCrewManager(saveNow: true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Droodism] CreateCrewMember_Postfix failed: {e}");
            }
        }
    }
}

