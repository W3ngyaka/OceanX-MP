using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// The per-school swim target, mirroring the original Boids_Demo pattern: a plain target
    /// affecter whose position is animated by a separate <see cref="TransformAnimator"/>
    /// ("Target Animator") moving it along a Circle / Rectangle / Line path.
    /// <see cref="EcosystemSimulationGPU"/> creates one Target + Target Animator pair per school
    /// under the species' spawner, so the hierarchy reads exactly like the demo scene.
    ///
    /// This subclass only adds what the ecosystem needs on top of the base affecter:
    ///   • a reference to its paired animator (so removal can destroy both, no leaks), and
    ///   • <see cref="ParkAt"/> — pins the target at an off-screen exit point and DISABLES the
    ///     animator (otherwise it would keep dragging the target back onto its path and the
    ///     school being removed would never swim out).
    /// </summary>
    public class EcosystemTargetGPU : SimulationAffecterComponent
    {
        // The TransformAnimator that moves this target along its path. Owned by the ecosystem
        // manager; destroyed together with this target when the school is removed.
        private TransformAnimator _animator;

        /// <summary>The paired Target Animator driving this target, if any.</summary>
        public TransformAnimator Animator => _animator;

        /// <summary>Links this target to the animator that moves it.</summary>
        public void SetAnimator(TransformAnimator animator) => _animator = animator;

        /// <summary>
        /// Stops the path animation and pins this target at <paramref name="worldPosition"/>
        /// (typically an off-screen point outside the simulation bounds). Fish following this
        /// target then swim straight to it, out of the simulation — the compute shader treats any
        /// school whose target sits outside the bounds as "exiting". Called by
        /// <see cref="EcosystemSimulationGPU"/> when removing a school.
        /// </summary>
        public void ParkAt(Vector3 worldPosition)
        {
            if (_animator != null)
            {
                _animator.enabled = false; // stop the path from dragging the target back in-bounds
            }
            transform.position = worldPosition;
            AffecterPosition   = worldPosition;
        }
    }
}
