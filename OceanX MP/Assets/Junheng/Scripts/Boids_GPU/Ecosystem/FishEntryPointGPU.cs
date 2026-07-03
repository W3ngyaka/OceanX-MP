using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Marker you drop on an empty GameObject to define a place where fish enter (and later exit) the
    /// simulation. Place these OUTSIDE the simulation bounds and OFF-CAMERA: when a school is added,
    /// <see cref="EcosystemSimulationGPU"/> picks a random registered entry point, spawns the new
    /// school's fish there and they swim quickly into the ecosystem.
    ///
    /// No wiring needed — each instance registers itself into <see cref="All"/> on enable and removes
    /// itself on disable, so EcosystemSimulationGPU just reads the static list.
    /// </summary>
    [DisallowMultipleComponent]
    public class FishEntryPointGPU : MonoBehaviour
    {
        /// <summary>Whether this marker can be used as a spawn-in point, a swim-out point, or both.</summary>
        public enum PointMode { Entry, Exit, Both }

        [Tooltip("Entry = fish spawn in here. Exit = removed fish swim out to here. Both = usable for either.")]
        [SerializeField] private PointMode _mode = PointMode.Both;

        // All currently-enabled points in the scene. EcosystemSimulationGPU reads this to pick a random
        // spawn-in / swim-out location. Static so no manual list wiring is required.
        private static readonly List<FishEntryPointGPU> _all = new List<FishEntryPointGPU>();

        /// <summary>Read-only view of every enabled entry/exit point in the scene.</summary>
        public static IReadOnlyList<FishEntryPointGPU> All => _all;

        /// <summary>World position fish spawn at / swim out to when this point is chosen.</summary>
        public Vector3 Position => transform.position;

        /// <summary>True if fish may spawn in at this point.</summary>
        public bool CanEntry => _mode == PointMode.Entry || _mode == PointMode.Both;

        /// <summary>True if removed fish may swim out to this point.</summary>
        public bool CanExit => _mode == PointMode.Exit || _mode == PointMode.Both;

        private void OnEnable()
        {
            if (!_all.Contains(this)) _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        // Draws a small marker so the (invisible) points are easy to place in the editor.
        // Cyan = Entry, orange = Exit, green = Both.
        private void OnDrawGizmos()
        {
            switch (_mode)
            {
                case PointMode.Entry: Gizmos.color = new Color(0f, 0.9f, 1f, 0.9f); break;
                case PointMode.Exit:  Gizmos.color = new Color(1f, 0.55f, 0f, 0.9f); break;
                default:              Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f); break;
            }
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            Gizmos.DrawLine(transform.position - Vector3.up * 1.5f, transform.position + Vector3.up * 1.5f);
        }
    }
}
