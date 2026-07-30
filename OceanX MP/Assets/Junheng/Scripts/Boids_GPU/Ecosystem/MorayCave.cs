using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Marker you drop on an empty GameObject to define a cave the giant moray lives in. Placed
    /// INSIDE the reef, at the cave mouth. <see cref="MorayCaveDirector"/> reads the static list and
    /// routes morays between caves.
    ///
    /// Placement convention (matches how the caves were authored):
    ///   • The marker's POSITION is the "neck" point — where the first 5-10% of the eel's body sits,
    ///     i.e. just behind the head. The head noses a little further out than this.
    ///   • The marker's FORWARD (blue +Z axis) points OUT of the cave mouth — the direction the head
    ///     should face. The body trails back along -forward into the cave.
    /// The director places the moray's swim-target a short lead-distance along +forward so the eel
    /// settles head-out with its body trailing into the hole.
    ///
    /// No wiring needed — each instance registers itself into <see cref="All"/> on enable and removes
    /// itself on disable, exactly like <see cref="FishEntryPointGPU"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class MorayCave : MonoBehaviour
    {
        private static readonly List<MorayCave> _all = new List<MorayCave>();

        /// <summary>Read-only view of every enabled cave in the scene.</summary>
        public static IReadOnlyList<MorayCave> All => _all;

        /// <summary>Neck point — where the body sits just behind the head.</summary>
        public Vector3 Position => transform.position;

        /// <summary>Direction the head faces / pokes out of the cave (marker's forward).</summary>
        public Vector3 HeadOutDirection => transform.forward;

        /// <summary>
        /// Ordered swim-in PATH: the eel follows these points into the cave, then freezes at the LAST one.
        /// Authored as child GameObjects of this cave, in sibling order — first = an approach point outside
        /// the cave, last = the rest spot deep in the hole. Shape the last body-length of the path so it
        /// coils and ends facing OUT; the eel's body lays itself along the route it swam, so that curl is
        /// what you see it hold once frozen. No children = no path; the director falls back to a single
        /// hold point at the marker (loiter + freeze there).
        /// </summary>
        public int PathPointCount => transform.childCount;

        /// <summary>World position of path point <paramref name="i"/> (0 = approach, Count-1 = rest spot).</summary>
        public Vector3 GetPathPoint(int i) => transform.GetChild(i).position;

        /// <summary>The rest spot the eel freezes at: the last path point, or the marker itself if no path.</summary>
        public Vector3 RestPoint => transform.childCount > 0
            ? transform.GetChild(transform.childCount - 1).position
            : transform.position;

        private void OnEnable()
        {
            if (!_all.Contains(this)) _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
        }

        // Purple cave marker: a sphere at the neck, an arrow showing head-out, and a faint line for the
        // body trailing back into the cave.
        private void OnDrawGizmos()
        {
            Vector3 p   = transform.position;
            Vector3 fwd = transform.forward;

            Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.9f);
            Gizmos.DrawWireSphere(p, 1.2f);

            // Head-out arrow.
            Vector3 headTip = p + fwd * 2.5f;
            Gizmos.DrawLine(p, headTip);
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Gizmos.DrawLine(headTip, headTip - fwd * 0.6f + right * 0.4f);
            Gizmos.DrawLine(headTip, headTip - fwd * 0.6f - right * 0.4f);

            // Body trailing into the cave.
            Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.35f);
            Gizmos.DrawLine(p, p - fwd * 4f);

            // Swim-in path: connect the child points in order (green), mark the rest spot (yellow).
            int n = transform.childCount;
            if (n > 0)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
                Vector3 prev = p;
                for (int i = 0; i < n; i++)
                {
                    Vector3 pt = transform.GetChild(i).position;
                    Gizmos.DrawLine(prev, pt);
                    Gizmos.DrawWireSphere(pt, 0.6f);
                    prev = pt;
                }
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
                Gizmos.DrawWireSphere(transform.GetChild(n - 1).position, 1.0f);
            }
        }
    }
}
