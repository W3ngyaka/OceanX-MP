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

        /// <summary>
        /// Ecosystem targets are keyed to a globally unique FLOCK id (in BoidSubGroupId) and are
        /// deliberately species-agnostic (BoidGroupId = ALL_BOIDS_AFFECTER_ID), so that a mixed-species
        /// shoal — whose fish share a flock but not a species — can all follow the one target. Letting
        /// BoidSpawnerBase.SetId stamp its spawner's species ID over that on every rebuild would re-bind
        /// the target to one species and tear mixed shoals apart.
        /// </summary>
        public override bool KeepsOwnAffecterID => true;

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

        /// <summary>
        /// Undo of <see cref="ParkAt"/>: hands the target back to its path animator so it is dragged
        /// in-bounds again. The compute shader decides "this school is exiting" purely from whether its
        /// target sits outside the simulation bounds, so the school stops beelining for the exit on the
        /// very next frame it is back inside — nothing per-fish has to be told. Called by
        /// <see cref="EcosystemSimulationGPU"/> when an Add arrives while this school is still swimming
        /// out: rather than spawn a new school on top of one that is leaving, the leaving one is recalled.
        /// No-op on a target that was never parked.
        /// </summary>
        public void Unpark()
        {
            if (_animator == null) return; // nothing to hand the target back to — leave it where it is

            _animator.enabled = true;

            // Put the target back ON its path, don't just switch the animator on. The animator steps the
            // target from wherever it currently is toward its next waypoint — it never snaps to the path —
            // so an un-parked target would set off from the off-screen exit point and crawl home at
            // MovementSpeed, with its school following it around the exit gate the whole way. Resetting
            // the animation drops the target straight back onto the start of its path, in-bounds, which is
            // both what the fish should be swimming toward and what makes the shader stop reading the
            // school as "exiting" (that test is purely: is this target outside the simulation bounds).
            _animator.ResetAnimation();

            // The affecter caches its own copy of the position; resync it from the transform the animator
            // just moved, so the GPU sees the in-bounds target on the very next dispatch rather than one
            // frame later. AffecterPosition's setter writes the transform, so read-then-write is a no-op
            // on the transform itself and only refreshes the cached value.
            AffecterPosition = transform.position;
        }
    }
}
