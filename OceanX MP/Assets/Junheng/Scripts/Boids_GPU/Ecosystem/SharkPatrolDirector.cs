using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Waypoint patrol AI for the blacktip reef shark. Sits beside the sim (like
    /// <see cref="MorayCaveDirector"/>) and reads its hooks — it does NOT touch the compute shader,
    /// and it never writes a fish's position or speed. The shark keeps boiding exactly as before.
    ///
    /// How it works: every fish steers toward its school's <see cref="EcosystemTargetGPU"/>, normally
    /// dragged around a random Circle / Rectangle / Line route by a paired TransformAnimator. This
    /// director DISABLES that animator and moves the target along an authored waypoint loop instead —
    /// the same mechanism the removal path uses via <see cref="EcosystemTargetGPU.ParkAt"/>. So the ONLY
    /// thing that changes is the route the target takes; the shark still chases it with its normal
    /// TargetWeight, separation, obstacle avoidance, reef SDF backstop and turn-rate limits. It still
    /// cuts corners, swings wide and detours around rock — it just does so along your path instead of a
    /// random one.
    ///
    /// Waypoints are the ordered children of <see cref="_waypointsRoot"/> (plain empty GameObjects),
    /// the same convention <see cref="MorayCave"/> uses for its cave paths. They are sampled into a
    /// Catmull-Rom spline: the shark's MaxAngularVelocity gives it a wide minimum turning radius, so a
    /// raw polyline with a sharp corner would just get overshot every lap.
    ///
    /// THE LEASH (see <see cref="_useLeash"/>). A boid only accelerates above its cruising speed when
    /// fleeing a predator or sprinting in/out of the sim — ordinary target-chasing does not, so a
    /// patrolling shark swims at EXACTLY its cruising speed and can never sprint to catch up. Its target
    /// meanwhile has to move slightly FASTER than cruising, or the shark overtakes it and mills around it
    /// in a scattered loop instead of streaming behind it. So the target permanently outruns the shark,
    /// and the only thing bounding the gap is the shark cutting corners (a shorter route than the
    /// spline's arclength). If that isn't enough, the gap grows lap after lap until the target laps the
    /// shark from behind — at which point the shark turns around and cuts back across the reef to chase
    /// it, and the patrol falls apart. The leash prevents that: the target may never get more than
    /// <see cref="_maxLeadDistance"/> ahead of the shark ALONG THE PATH. Up to that limit it runs at
    /// full speed; at the limit it advances only as fast as the shark itself is progressing along the
    /// route. Only the target's speed along the spline changes — the shark is never repositioned, and
    /// the target never leaves the path.
    ///
    /// Two details that are easy to get wrong, both found by testing this in play mode:
    ///
    /// • The lead is measured ALONG THE PATH (the shark's centroid is projected onto the spline), not as
    ///   a straight line to the target. A shark faithfully tracing the route sits far from a target that
    ///   is half a loop ahead of it, and a straight-line measure cannot tell that apart from a shark that
    ///   has wandered off sideways. Only the first of those needs the leash.
    ///
    /// • The target is slowed, never stopped (<see cref="_leashMinSpeedFactor"/> is a floor, not a
    ///   target). A stopped target is a fixed point, and a fish that cannot swim below cruising speed
    ///   cannot hover at a fixed point — it flies a circle around it no tighter than its turning radius
    ///   (cruisingSpeed / MaxAngularVelocity, about 7m for this shark). A stop-and-resume leash keyed on
    ///   any threshold under that radius therefore deadlocks: the target waits forever while the shark
    ///   loops around it. A target that always keeps creeping forward is chased, not orbited.
    /// </summary>
    public class SharkPatrolDirector : MonoBehaviour
    {
        [Header("Wiring (auto-found if left empty)")]
        [Tooltip("The ecosystem sim. Auto-found on Awake if null.")]
        [SerializeField] private EcosystemSimulationGPU _sim;

        [Tooltip("The Blacktip reef shark species asset. Auto-found by name on Awake if null.")]
        [SerializeField] private SpeciesDataGPU _sharkSpecies;

        [Tooltip("Parent of the waypoint empties. Its CHILDREN, in sibling order, are the path. " +
                 "Defaults to this object's own transform, so dropping this component straight onto " +
                 "the 'Waypoints' object needs no wiring at all.")]
        [SerializeField] private Transform _waypointsRoot;

        [Header("Path shape")]
        [Tooltip("ON = the last waypoint joins back to the first, so the shark laps forever. " +
                 "OFF = the path runs end-to-end and then retraces back along itself.")]
        [SerializeField] private bool _closedLoop = true;

        [Tooltip("Spline samples per waypoint segment. Higher = smoother corners, slightly more memory. " +
                 "The path is resampled only on Awake / RebuildPath().")]
        [Min(2)] [SerializeField] private int _samplesPerSegment = 20;

        [Header("Target speed along the path")]
        [Tooltip("Metres/second the TARGET travels along the spline. Leave at 0 to derive it from the " +
                 "species' cruising speed via Speed Factor below (recommended — it keeps working if the " +
                 "shark's movement asset is re-tuned).")]
        [Min(0f)] [SerializeField] private float _patrolSpeed = 0f;

        [Tooltip("Used when Patrol Speed is 0: target speed = the shark's CruisingSpeed x this. Keep it " +
                 "slightly ABOVE 1. Fish are hard-clamped to never swim slower than cruising, so a SLOWER " +
                 "target gets overtaken and the shark mills around it; a touch faster and it streams " +
                 "along behind. (Same reasoning as the sim's own Target Speed Fraction Range.)")]
        [Min(0.1f)] [SerializeField] private float _speedFactor = 1.15f;

        [Tooltip("Metres of path each ADDITIONAL concurrent shark starts further along the loop, so " +
                 "several sharks spread out instead of stacking on one point. 0 = every shark starts at " +
                 "the first waypoint (the shark's MaxSchools is 6, so consider ~1/6 of the loop length " +
                 "if you routinely run more than one).")]
        [Min(0f)] [SerializeField] private float _spawnStaggerMetres = 0f;

        [Header("Leash (keeps the shark from falling off the path)")]
        [Tooltip("ON = the target may not get further than Max Lead Distance ahead of the shark along " +
                 "the path. See the class summary for why a shark can never sprint to catch up. OFF = " +
                 "the target runs at a constant speed and the shark lags however it lags — eventually " +
                 "getting lapped, after which it turns around to chase and the patrol breaks down.")]
        [SerializeField] private bool _useLeash = true;

        [Tooltip("How far (m) the target may get ahead of the shark ALONG THE PATH before it stops " +
                 "gaining ground and just paces the shark. Below this the target runs at full speed, so " +
                 "this is effectively the standoff the shark settles into behind its target. Too small " +
                 "and the target is dragged back onto the shark's nose; too large and the shark can " +
                 "drift most of a lap behind before anything reins it in.")]
        [Min(1f)] [SerializeField] private float _maxLeadDistance = 18f;

        [Tooltip("Speed floor while leashed, as a fraction of the base speed — the target always creeps " +
                 "forward at least this fast even if the shark stops making progress entirely (snagged " +
                 "on reef, say). Never set 0: a stationary target gets orbited rather than chased, and " +
                 "the leash deadlocks (see the class summary). Keep it well under 1 so the shark can " +
                 "actually gain on it.")]
        [Range(0.05f, 1f)] [SerializeField] private float _leashMinSpeedFactor = 0.25f;

        [Tooltip("Seconds between GPU read-backs of the shark's position for the leash. Each read is a " +
                 "SYNCHRONOUS GPU read-back, so keep it low-frequency — this is not a per-frame check.")]
        [Min(0.05f)] [SerializeField] private float _centroidPollInterval = 0.4f;

        [Header("Debug")]
        [Tooltip("Draw the sampled spline and each shark's live target position as gizmos.")]
        [SerializeField] private bool _drawGizmos = true;

        // Per-shark runtime state, keyed by the school's target (stable for the school's lifetime).
        private class PatrolState
        {
            public float   cursor;       // arclength (m) the TARGET has advanced along the path
            public float   nextPoll;     // Time.time at which the leash may next read the centroid
            public float   speedScale;   // 1 = free running, down to _leashMinSpeedFactor when leashed
            public bool    hasCentroid;  // false until the first successful read-back
            public Vector3 centroid;     // last known school centre, from the GPU
        }

        private readonly Dictionary<EcosystemTargetGPU, PatrolState> _agents
            = new Dictionary<EcosystemTargetGPU, PatrolState>();

        // Scratch reused each frame to find agents whose school has gone (removed / now exiting).
        private readonly List<EcosystemTargetGPU> _seen  = new List<EcosystemTargetGPU>();
        private readonly List<EcosystemTargetGPU> _stale = new List<EcosystemTargetGPU>();

        // Sampled spline + arclength table. _cumulative[i] is the distance from the path start to
        // _points[i], so _cumulative[last] is the total path length.
        private readonly List<Vector3> _points     = new List<Vector3>();
        private readonly List<float>   _cumulative = new List<float>();
        private float _pathLength;

        /// <summary>Total length (m) of the sampled path. 0 when no valid path is built.</summary>
        public float PathLength => _pathLength;

        private void Awake()
        {
            if (_sim == null) _sim = FindFirstObjectByType<EcosystemSimulationGPU>();
            if (_sharkSpecies == null) _sharkSpecies = FindSharkSpecies();
            if (_waypointsRoot == null) _waypointsRoot = transform;

            RebuildPath();

            if (_pathLength <= 0f)
                Debug.LogWarning($"{nameof(SharkPatrolDirector)}: no usable path — needs at least 2 " +
                                 $"waypoint children under '{(_waypointsRoot != null ? _waypointsRoot.name : "null")}'. " +
                                 "Sharks will keep their default random roaming.", this);

            if (_sharkSpecies == null)
                Debug.LogWarning($"{nameof(SharkPatrolDirector)}: could not find the blacktip reef shark " +
                                 "species asset — assign it manually.", this);
        }

        // Locates the Blacktip reef shark SpeciesDataGPU from the scene's spawners so the director works
        // without manual wiring. Matches on the species name containing "blacktip" (case-insensitive).
        private static SpeciesDataGPU FindSharkSpecies()
        {
            var spawners = FindObjectsByType<BoidSpawnerGPUMultiTargets>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sp in spawners)
            {
                SpeciesDataGPU s = sp != null ? sp.SpeciesData : null;
                if (s != null && !string.IsNullOrEmpty(s.SpeciesName) &&
                    s.SpeciesName.ToLowerInvariant().Contains("blacktip"))
                    return s;
            }
            return null;
        }

        private void Update()
        {
            if (_sim == null || _sharkSpecies == null || _pathLength <= 0f) return;

            int committed = _sim.CountCommittedGroups(_sharkSpecies); // schools NOT swimming out
            float dt = Time.deltaTime;
            _seen.Clear();

            for (int i = 0; i < committed; i++)
            {
                EcosystemTargetGPU target = _sim.GetSchoolTarget(_sharkSpecies, i);
                if (target == null) continue;
                _seen.Add(target);

                if (!_agents.TryGetValue(target, out PatrolState state))
                    state = Claim(target);

                Drive(state, target, i, dt);
            }

            // Release any agent whose school is gone: removed, or now swimming out (index >= committed,
            // so it never appeared in _seen this frame). We simply stop driving — the sim's ParkAt already
            // owns the target's exit, and re-enabling the animator here would drag the fish back in-bounds
            // and break the swim-out.
            _stale.Clear();
            foreach (var kv in _agents)
                if (!_seen.Contains(kv.Key)) _stale.Add(kv.Key);
            for (int i = 0; i < _stale.Count; i++)
                _agents.Remove(_stale[i]);
        }

        // Turning the director off mid-play hands every shark still in the tank back to its own random
        // path animator, rather than leaving its target frozen wherever it happened to be. Makes it easy
        // to A/B the patrol against stock roaming in play mode.
        private void OnDisable()
        {
            foreach (var kv in _agents)
            {
                EcosystemTargetGPU target = kv.Key;
                if (target != null && target.Animator != null) target.Animator.enabled = true;
            }
            _agents.Clear();
        }

        // Takes over a newly seen shark school: stops its random path animator (the same move ParkAt makes
        // on removal) so nothing fights us for the target, and drops its cursor at the first waypoint.
        private PatrolState Claim(EcosystemTargetGPU target)
        {
            if (target.Animator != null) target.Animator.enabled = false;

            // Every shark starts at the first waypoint, so a school spawning at its off-screen entry
            // marker beelines for waypoint 0 and then rides the loop. The stagger fans additional
            // concurrent sharks further along so they don't all chase the same point.
            float cursor = _agents.Count * _spawnStaggerMetres;

            var state = new PatrolState
            {
                cursor      = WrapDistance(cursor),
                nextPoll    = Time.time, // poll on the first frame so the leash has data immediately
                speedScale  = 1f,
                hasCentroid = false,
            };
            _agents[target] = state;
            return state;
        }

        private void Drive(PatrolState state, EcosystemTargetGPU target, int schoolIndex, float dt)
        {
            float freeStep = ResolvePatrolSpeed() * dt; // how far the target would advance unleashed
            float step     = freeStep;

            if (_useLeash)
            {
                if (Time.time >= state.nextPoll)
                {
                    state.nextPoll = Time.time + _centroidPollInterval;
                    if (_sim.TryGetSchoolCentroid(_sharkSpecies, schoolIndex, out Vector3 centroid))
                    {
                        state.centroid    = centroid;
                        state.hasCentroid = true;
                    }
                }

                if (state.hasCentroid)
                {
                    // Project the shark onto the spline and work in arclength, so "18m ahead on the
                    // route" is not confused with "18m off to one side" (see the class summary).
                    float sharkDistance = NearestDistanceAlongPath(state.centroid);
                    float lead = SignedDelta(state.cursor - sharkDistance); // + = target is ahead
                    float room = _maxLeadDistance - lead;                   // slack left this frame

                    // At the limit the target advances only as fast as the shark itself is progressing,
                    // but never slower than the floor — a target that stops moving gets orbited.
                    step = Mathf.Max(freeStep * _leashMinSpeedFactor, Mathf.Min(freeStep, room));
                }
            }

            state.speedScale = freeStep > 0f ? step / freeStep : 1f; // gizmo only
            state.cursor     = WrapDistance(state.cursor + step);

            // The one and only write: move the point the shark is steering at. Setting AffecterPosition
            // updates both the transform and the struct pushed to the GPU.
            target.AffecterPosition = SampleAtDistance(state.cursor);
        }

        // Target speed along the spline: the explicit override if set, otherwise derived from the species'
        // cruising speed so it keeps tracking any re-tune of the movement asset.
        private float ResolvePatrolSpeed()
        {
            if (_patrolSpeed > 0f) return _patrolSpeed;

            var movement = _sharkSpecies != null ? _sharkSpecies.MovementProperties : null;
            float cruising = movement != null ? movement.CruisingSpeed : 0f;
            return cruising > 0f ? cruising * _speedFactor : 3f;
        }

        // -------------------------------------------------------------------------
        // Path sampling — Catmull-Rom through the waypoints, flattened to an arclength
        // table so the target advances at a constant metres/second regardless of how
        // unevenly the waypoints are spaced.
        // -------------------------------------------------------------------------

        /// <summary>
        /// Re-samples the spline from the current waypoint children. Call after moving, adding or
        /// removing waypoints at runtime. Cursors are kept but wrapped into the new path length, so
        /// sharks already patrolling stay on the path rather than jumping back to the start.
        /// </summary>
        public void RebuildPath()
        {
            _points.Clear();
            _cumulative.Clear();
            _pathLength = 0f;

            if (_waypointsRoot == null) return;

            List<Vector3> waypoints = new List<Vector3>(_waypointsRoot.childCount);
            for (int i = 0; i < _waypointsRoot.childCount; i++)
            {
                Transform child = _waypointsRoot.GetChild(i);
                if (child != null) waypoints.Add(child.position);
            }
            if (waypoints.Count < 2) return;

            SampleSpline(waypoints, _samplesPerSegment, _closedLoop, _points);

            // Arclength table over the sampled points.
            _cumulative.Add(0f);
            for (int i = 1; i < _points.Count; i++)
            {
                _pathLength += Vector3.Distance(_points[i - 1], _points[i]);
                _cumulative.Add(_pathLength);
            }

            foreach (var kv in _agents) kv.Value.cursor = WrapDistance(kv.Value.cursor);
        }

        // Samples a Catmull-Rom spline through the waypoints into `into`. For a closed loop the first
        // point is repeated at the end so the arclength table covers the closing segment with no special
        // case; for an open path the samples are retraced in reverse so the shark runs to the far end and
        // swims home again instead of teleporting back to the start.
        private static void SampleSpline(List<Vector3> waypoints, int samplesPerSegment, bool closedLoop, List<Vector3> into)
        {
            int count = waypoints.Count;
            int segments = closedLoop ? count : count - 1;
            int samples = Mathf.Max(2, samplesPerSegment);

            for (int seg = 0; seg < segments; seg++)
            {
                Vector3 p0 = waypoints[WrapIndex(seg - 1, count, closedLoop)];
                Vector3 p1 = waypoints[WrapIndex(seg,     count, closedLoop)];
                Vector3 p2 = waypoints[WrapIndex(seg + 1, count, closedLoop)];
                Vector3 p3 = waypoints[WrapIndex(seg + 2, count, closedLoop)];

                for (int i = 0; i < samples; i++)
                    into.Add(CatmullRom(p0, p1, p2, p3, i / (float)samples));
            }

            if (closedLoop)
            {
                into.Add(into[0]); // close the ring
            }
            else
            {
                into.Add(waypoints[count - 1]);
                for (int i = into.Count - 2; i >= 0; i--) into.Add(into[i]);
            }
        }

        private static int WrapIndex(int i, int count, bool closedLoop)
        {
            if (closedLoop) return ((i % count) + count) % count;
            return Mathf.Clamp(i, 0, count - 1);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        // Keeps an arclength cursor inside [0, _pathLength), handling negatives.
        private float WrapDistance(float distance)
        {
            if (_pathLength <= 0f) return 0f;
            return ((distance % _pathLength) + _pathLength) % _pathLength;
        }

        // The shortest SIGNED arclength difference on the loop, in [-half, +half). Without this a target
        // 2m ahead of a shark sitting just before the wrap point would read as most of a lap behind it.
        private float SignedDelta(float difference)
        {
            if (_pathLength <= 0f) return 0f;
            float wrapped = WrapDistance(difference);
            return wrapped > _pathLength * 0.5f ? wrapped - _pathLength : wrapped;
        }

        // Arclength of the point on the path closest to `worldPosition` — i.e. how far along the route
        // the shark has actually got, independent of how far it has strayed off it. Linear scan; the
        // table is a couple of hundred points and only the leash poll calls this.
        private float NearestDistanceAlongPath(Vector3 worldPosition)
        {
            if (_points.Count == 0) return 0f;

            int   best         = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _points.Count; i++)
            {
                float d = (_points[i] - worldPosition).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return _cumulative[best];
        }

        // World position at `distance` metres along the path. Binary search over the arclength table,
        // then a linear interpolation inside the located sample segment.
        private Vector3 SampleAtDistance(float distance)
        {
            if (_points.Count == 0) return transform.position;
            if (_pathLength <= 0f)  return _points[0];

            distance = WrapDistance(distance);

            int low = 0;
            int high = _cumulative.Count - 1;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (_cumulative[mid] < distance) low = mid + 1;
                else high = mid;
            }
            if (low == 0) return _points[0];

            float segmentStart  = _cumulative[low - 1];
            float segmentLength = _cumulative[low] - segmentStart;
            float t = segmentLength > 0f ? (distance - segmentStart) / segmentLength : 0f;
            return Vector3.Lerp(_points[low - 1], _points[low], t);
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            Transform root = _waypointsRoot != null ? _waypointsRoot : transform;
            if (root.childCount < 2) return;

            // Sampled fresh each draw rather than from _points, so the route previews correctly in the
            // editor while you drag waypoints around and before Awake has ever run.
            List<Vector3> waypoints = new List<Vector3>(root.childCount);
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i) != null) waypoints.Add(root.GetChild(i).position);
            if (waypoints.Count < 2) return;

            List<Vector3> preview = new List<Vector3>();
            SampleSpline(waypoints, _samplesPerSegment, _closedLoop, preview);

            Gizmos.color = new Color(0.2f, 0.9f, 1f);
            for (int i = 1; i < preview.Count; i++) Gizmos.DrawLine(preview[i - 1], preview[i]);

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Count; i++) Gizmos.DrawWireSphere(waypoints[i], 0.5f);

            // Live cursors: where each shark's target currently sits. Green = running free, reddening
            // as the leash eases the target off to let the shark close the gap.
            foreach (var kv in _agents)
            {
                if (kv.Key == null) continue;
                Gizmos.color = Color.Lerp(Color.red, Color.green, kv.Value.speedScale);
                Gizmos.DrawWireSphere(kv.Key.AffecterPosition, 1f);
            }
        }
    }
}
