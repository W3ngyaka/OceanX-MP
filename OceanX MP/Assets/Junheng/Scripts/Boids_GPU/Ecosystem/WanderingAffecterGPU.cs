using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// A SimulationAffecterComponent that wanders randomly within the simulation bounds
    /// each frame instead of sitting at a fixed world position.
    /// EcosystemSimulationGPU creates one of these per boid sub-group for each Apex species
    /// so that apex predator schools roam the full simulation volume rather than circling a
    /// static point.
    ///
    /// Mesopredator, Prey, and Neutral species are unaffected — they keep their normal fixed
    /// SimulationAffecterComponent targets set in the Inspector.
    ///
    /// Call Initialize(bounds) immediately after AddComponent before any Update runs.
    /// </summary>
    public class WanderingAffecterGPU : SimulationAffecterComponent
    {
        [Header("Wander Settings")]
        [Tooltip("Base movement speed of this wander target (m/s).")]
        [SerializeField] private float _wanderSpeed = 5f;

        [Tooltip("Maximum angular change per second (degrees). Controls how tightly the wander direction can turn.")]
        [SerializeField] private float _turnRate = 45f;

        [Tooltip("Distance inside the bounds boundary at which steering back toward center begins.")]
        [SerializeField] private float _boundaryMargin = 6f;

        [Tooltip("How aggressively the wanderer steers back to center when it hits the margin. 0 = no steering, 1 = instant snap.")]
        [Range(0f, 1f)]
        [SerializeField] private float _centerReturnStrength = 0.35f;

        // Runtime state
        private Bounds  _bounds;
        private Vector3 _wanderDirection;
        private bool    _initialized;
        // When true the wanderer stops roaming and holds a fixed position (used to park a removed
        // school's target outside the bounds so its fish swim out).
        private bool    _parked;

        // -------------------------------------------------------------------------
        // Public setup
        // -------------------------------------------------------------------------

        /// <summary>
        /// Configures this wanderer with the simulation bounds. Must be called once before
        /// the first Update tick, typically immediately after AddComponent.
        /// </summary>
        public void Initialize(Bounds bounds)
        {
            _bounds = bounds;

            // Start at a random position safely inside the bounds.
            Vector3 safeExtents  = bounds.extents - Vector3.one * (_boundaryMargin + 1f);
            safeExtents          = Vector3.Max(safeExtents, Vector3.one);
            transform.position   = bounds.center + new Vector3(
                Random.Range(-safeExtents.x, safeExtents.x),
                Random.Range(-safeExtents.y, safeExtents.y),
                Random.Range(-safeExtents.z, safeExtents.z));

            _wanderDirection = Random.onUnitSphere;
            AffecterPosition = transform.position;
            SetUpdatePositionEveryFrame(true);
            _initialized = true;
        }

        /// <summary>
        /// Stops wandering and pins this target at <paramref name="worldPosition"/> (typically an
        /// off-screen point outside the bounds). Fish following this target then swim straight to it,
        /// out of the simulation — the compute shader treats any school whose target sits outside the
        /// bounds as "exiting". Called by <see cref="EcosystemSimulationGPU"/> when removing a school.
        /// </summary>
        public void ParkAt(Vector3 worldPosition)
        {
            _parked = true;
            transform.position = worldPosition;
            AffecterPosition   = worldPosition;
        }

        // -------------------------------------------------------------------------
        // Per-frame wander update
        // -------------------------------------------------------------------------

        // NOTE: SimulationAffecterComponent also has a private Update() that reads
        // transform.position into the affecter struct. Both run in the same frame;
        // the order is non-deterministic but harmless — we sync AffecterPosition
        // here to guarantee it is correct regardless of call order.
        private void Update()
        {
            if (!_initialized) return;

            // Parked (a removed school swimming out): hold position, don't wander or clamp to bounds.
            if (_parked)
            {
                AffecterPosition = transform.position;
                return;
            }

            float dt = Time.deltaTime;

            // Accumulate a small random rotation into the current wander direction.
            // Scale the random offset by turn rate so it's frame-rate independent.
            Vector3 randomPerturbation = Random.insideUnitSphere * (_turnRate * Mathf.Deg2Rad * dt);
            _wanderDirection = Vector3.Slerp(_wanderDirection, (_wanderDirection + randomPerturbation).normalized, 0.15f).normalized;

            // When within the boundary margin, blend toward the center to prevent escape.
            Vector3 pos       = transform.position;
            Vector3 toCenter  = _bounds.center - pos;
            float   distToEdge = Mathf.Min(
                Mathf.Min(_bounds.max.x - pos.x, pos.x - _bounds.min.x),
                Mathf.Min(
                    Mathf.Min(_bounds.max.y - pos.y, pos.y - _bounds.min.y),
                    Mathf.Min(_bounds.max.z - pos.z, pos.z - _bounds.min.z)));

            if (distToEdge < _boundaryMargin)
            {
                float t = 1f - Mathf.Clamp01(distToEdge / _boundaryMargin);
                _wanderDirection = Vector3.Slerp(_wanderDirection, toCenter.normalized, t * _centerReturnStrength).normalized;
            }

            // Move the transform.
            transform.position += _wanderDirection * (_wanderSpeed * dt);

            // Hard-clamp strictly inside bounds so the affecter never teleports a school outside.
            Vector3 clamped = transform.position;
            float   m       = _boundaryMargin * 0.5f;
            clamped.x = Mathf.Clamp(clamped.x, _bounds.min.x + m, _bounds.max.x - m);
            clamped.y = Mathf.Clamp(clamped.y, _bounds.min.y + m, _bounds.max.y - m);
            clamped.z = Mathf.Clamp(clamped.z, _bounds.min.z + m, _bounds.max.z - m);
            transform.position = clamped;

            // Sync position to the underlying SimulationAffecter struct.
            AffecterPosition = transform.position;
        }
    }
}
