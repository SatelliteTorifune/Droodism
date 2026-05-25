using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    /// <summary>
    /// Logs collisions to help debug ragdoll self-collision issues.
    /// Attach to each collider in the ragdoll chain during development.
    /// </summary>
    public class RagdollCollisionLogger : MonoBehaviour
    {
        private HashSet<(string, string)> _loggedCollisions;
        private bool _isActive;

        public void Initialize(HashSet<(string, string)> loggedCollisions, bool isActive)
        {
            _loggedCollisions = loggedCollisions;
            _isActive = isActive;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive || _loggedCollisions == null) return;

            string a = gameObject.name;
            string b = collision.gameObject.name;

            // Normalize pair order
            var pair = string.Compare(a, b, System.StringComparison.Ordinal) < 0
                ? (a, b) : (b, a);

            if (_loggedCollisions.Add(pair))
            {
                Vector3 pt = collision.GetContact(0).point;
                Debug.LogWarning($"[RagdollCollision] {a} <-> {b} at ({pt.x:F2},{pt.y:F2},{pt.z:F2})");
            }
        }
    }
}
