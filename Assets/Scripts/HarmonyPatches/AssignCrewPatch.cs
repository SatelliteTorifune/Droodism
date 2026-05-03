using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using HarmonyLib;

namespace Assets.Scripts
{
    [HarmonyPatch(typeof(EvaData), nameof(EvaData.AssignCrewMember))]
    public static class AssignCrewPatch
    {
        [HarmonyPostfix]
        public static void Postfix(EvaData __instance)
        {
            if (__instance?.Script?.PartScript == null)
                return;
            var support = __instance.Script.PartScript.GetModifier<SupportLifeScript>();
            support?.Data?.SetDroodismCrewData();
        }
    }
    
}
    
    