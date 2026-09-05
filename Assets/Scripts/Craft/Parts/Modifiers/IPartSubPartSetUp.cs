using System;
using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    /// <summary>
    /// Contract + shared implementation for part modifier scripts that resolve and attach a sub-part —
    /// a child transform of the part such as a fan, solar panel, particle-system emitter or rotating
    /// base. Parts that define a non-zero position offset get an extra hidden "SubPartRotatorOffset"
    /// pivot dummy so the sub-part can rotate around an arbitrary point.
    /// </summary>
    public interface IPartSubPartSetUp
    {
        /// <summary>The currently attached sub-part transform, or null if it could not be resolved.</summary>
        Transform SubPart { get; }

        /// <summary>
        /// Attaches the given sub-part, rebuilding the offset pivot dummy transform when the part
        /// data specifies a non-zero position offset. Implementations should delegate the pivot
        /// bookkeeping to <see cref="ApplySubPart"/>.
        /// </summary>
        /// <param name="subPart">The sub-part transform to attach (may be null).</param>
        void SetSubPart(Transform subPart);

        /// <summary>Name of the dummy transform used to pivot a sub-part around an arbitrary point.</summary>
        public const string OffsetName = "SubPartRotatorOffset";

        /// <summary>
        /// Resolves a sub-part by its slash-separated child path (e.g. "DeviceBase/DeviceFanA").
        /// Walks the hierarchy directly first, then falls back to a deep search of the whole part
        /// when the direct path does not match. Trailing slashes in the path are ignored.
        /// </summary>
        /// <param name="self">The part modifier component to resolve relative to.</param>
        /// <param name="path">The slash-separated child path, e.g. "DeviceBase/DeviceFanA".</param>
        /// <returns>The resolved sub-part transform, or null when it could not be found.</returns>
        public static Transform FindSubPart(Component self, string path)
        {
            if (self == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            string normalized = path.TrimEnd('/');
            string[] strArray = normalized.Split('/', StringSplitOptions.None);
            Transform subPart = self.transform;
            foreach (string name in strArray)
            {
                subPart = subPart.Find(name) ?? subPart;
            }

            return subPart.name == strArray[strArray.Length - 1]
                ? subPart
                : ModApi.Utilities.FindFirstGameObjectMyselfOrChildren(normalized, self.gameObject)?.transform;
        }

        /// <summary>
        /// Attaches the sub-part to an optional offset pivot. Destroys the previous pivot (if any),
        /// then creates a fresh <see cref="OffsetName"/> dummy transform positioned at the requested
        /// offset when the sub-part exists and the offset is non-zero. Returns the new pivot transform
        /// (or null) and outputs the inverse delta used to keep the sub-part on the pivot while it rotates.
        /// </summary>
        /// <param name="previousOffset">The pivot created by a previous call (will be destroyed).</param>
        /// <param name="subPart">The sub-part to pivot (may be null).</param>
        /// <param name="positionOffset">The position offset from the sub-part pivot; zero disables the pivot.</param>
        /// <param name="offsetPositionInverse">The offset-to-sub-part delta in pivot-local space.</param>
        /// <returns>The new pivot transform, or null when none is needed.</returns>
        public static Transform ApplySubPart(Transform previousOffset, Transform subPart, Vector3 positionOffset, out Vector3 offsetPositionInverse)
        {
            if (previousOffset != null)
            {
                UnityEngine.Object.Destroy(previousOffset.gameObject);
            }

            offsetPositionInverse = Vector3.zero;
            if (subPart == null || positionOffset.magnitude <= 0.0f)
            {
                return null;
            }

            Transform offset = new GameObject(OffsetName).transform;
            offset.SetParent(subPart.parent, false);
            offset.position = subPart.TransformPoint(positionOffset);
            offsetPositionInverse = offset.InverseTransformPoint(subPart.position);
            return offset;
        }
    }
}
