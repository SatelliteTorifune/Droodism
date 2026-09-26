using ModApi.Craft.Parts;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    /// <summary>
    /// Shared helpers for part modifier scripts. Consolidates the repeatedly copy-pasted
    /// "grab the STCommandPodPatch modifier" and "find a craft fuel source by id" logic.
    /// </summary>
    public static class PartScriptUtilities
    {
        /// <summary>
        /// Returns the STCommandPodPatch modifier of the craft's command pod, or null when the
        /// part has no command pod or the patch modifier is missing.
        /// </summary>
        /// <param name="partScript">The part to resolve the command pod patch for.</param>
        /// <returns>The patch modifier, or null.</returns>
        public static STCommandPodPatchScript GetCommandPodPatch(IPartScript partScript)
        {
            return partScript?.CommandPod?.Part?.PartScript?.GetModifier<STCommandPodPatchScript>();
        }

        /// <summary>
        /// Finds the craft fuel source with the given fuel type id, or null when not found.
        /// </summary>
        /// <param name="partScript">The part whose craft's fuel sources are searched.</param>
        /// <param name="fuelType">The fuel type id, e.g. "LH2".</param>
        /// <returns>The matching fuel source, or null.</returns>
        public static IFuelSource FindCraftFuelSource(IPartScript partScript, string fuelType)
        {
            if (partScript?.CraftScript?.FuelSources?.FuelSources == null)
            {
                return null;
            }

            foreach (var source in partScript.CraftScript.FuelSources.FuelSources)
            {
                if (source.FuelType.Id == fuelType)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
