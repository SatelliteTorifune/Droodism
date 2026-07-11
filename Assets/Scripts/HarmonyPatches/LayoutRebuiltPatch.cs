using System;
using Assets.Scripts.Design;
using Assets.Scripts.Droodism.UserInterface;
using Assets.Scripts.Flight.UI;
using HarmonyLib;
using ModApi.Mods;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts.HarmonyPatches
{
    //byd jundroo给的教程有问题,本来是用另一个函数的,但是只能用harmony
    [HarmonyPatch(typeof(NavPanelController), "LayoutRebuilt")]
    class LayoutRebuiltPatch
    {
        static bool Prefix(NavPanelController __instance)
        {
            try
            {
                __instance.xmlLayout.GetElementById(DroodismUIManager.droodismBottomId)
                    .AddOnClickEvent(DroodismUIManager.Instance.OnToggleDroodismInspectorPanelState, true);
            }
            catch (Exception e)
            {
                Mod.LogError("Error while adding click event to{0}" + e);
            }

            return true;
        }
    }
}