using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Cave-dwelling AI for the giant moray. Sits beside the sim (like IntroductionCameraDirectorGPU)
    /// and reads its hooks — it does NOT touch the compute shader or change how any other fish behaves.
    ///
    /// How it works: every fish steers toward its school's <see cref="EcosystemTargetGPU"/>, normally
    /// dragged around a random path by a paired TransformAnimator. For a moray that claims a cave, the
    /// director DISABLES that animator and drives the target itself — exactly the mechanism the removal
    /// path uses via <see cref="EcosystemTargetGPU.ParkAt"/> — parking the target at the cave mouth so
    /// the eel swims home and loiters there, then routing it to the other cave on a timer. Because the
    /// eel still follows a target, the reef SDF steers it around rock while travelling, so it looks like
    /// it finds its own way.
    ///
    /// Cave model: one moray per cave. With N caves, the first N morays to appear claim them; any extra
    /// morays keep their normal random roaming (their animator is left untouched) until a cave frees up.
    /// Since the sim removes the TOP school first, roaming extras are culled before caved morays, so
    /// claims stay stable.
    ///
    /// FUTURE (with Akil's spine shader): a genuinely still "resting / curled in the cave" pose can't be
    /// done through boid steering — a single fish can't swim slower than its cruising speed, so a static
    /// target makes it slowly loop the cave mouth (a believable idle, but not motionless). The real
    /// resting pose freezes the head at the cave and shapes the Moray_Lit_Instanced head-path trail into
    /// a coil while <see cref="MorayState.Phase.InCave"/>. <see cref="_onEnterCave"/> / this state is the
    /// hook to trigger that pose + the mouth animation later.
    /// </summary>
    public class MorayCaveDirector : MonoBehaviour
    {
        [Header("Wiring (auto-found if left empty)")]
        [Tooltip("The ecosystem sim. Auto-found on Awake if null.")]
        [SerializeField] private EcosystemSimulationGPU _sim;

        [Tooltip("The Giant moray species asset. Auto-found by name on Awake if null.")]
        [SerializeField] private SpeciesDataGPU _moraySpecies;

        [Tooltip("The GPU sim/renderer. Auto-found on Awake if null. Receives the cave anchors so the moray " +
                 "shader can lay a resting eel's body into the rock and gape its jaw.")]
        [SerializeField] private OceanX.BoidsGPU.SpatialPartitionInstancedRendering.BoidSimulationGPU _boidSim;

        [Header("Cave dwell timing")]
        [Tooltip("Seconds a moray settles in a cave before it can even consider leaving.")]
        [Min(0f)] [SerializeField] private float _caveDwellSeconds = 100f;

        [Tooltip("After the dwell time, how often (seconds) it rolls to decide whether to leave.")]
        [Min(1f)] [SerializeField] private float _leaveRollInterval = 30f;

        [Tooltip("Chance per roll that a settled moray leaves for another (free) cave.")]
        [Range(0f, 1f)] [SerializeField] private float _leaveChance = 0.375f;

        [Tooltip("Master switch for the curled resting POSE. ON = once settled, the boid is frozen at the cave " +
                 "(compute-shader freeze) and the body curls into the rest pose. OFF = the eel just loiters at " +
                 "the cave with the normal swim animation (fallback if the pose misbehaves).")]
        [SerializeField] private bool _enableRestPose = true;

        [Tooltip("Seconds for the resting curl pose to ease in once settled (and ease out when leaving). The " +
                 "eel swims 100% normally until it arrives; only then does its head reorient into the pose.")]
        [Min(0.05f)] [SerializeField] private float _restEaseSeconds = 1.5f;

        [Header("Placement / travel")]
        [Tooltip("How far in front of the cave marker (along its head-out forward) the swim-target sits, " +
                 "so the eel noses out of the mouth with its body trailing into the cave. Metres.")]
        [Min(0f)] [SerializeField] private float _headLead = 2f;

        [Tooltip("Distance (m) from the cave mouth that counts as 'arrived' and starts the rest ease. The pose " +
                 "anchors to the eel's own head, so this no longer drags it across the water — it just gates " +
                 "when the body starts to settle. Must exceed the eel's loiter/orbit radius so it triggers.")]
        [Min(0.5f)] [SerializeField] private float _arriveRadius = 4.5f;

        [Tooltip("How close (m) the eel's head must get to a cave PATH point before the target advances to the " +
                 "next one. Larger = it corners the path more loosely; smaller = it hugs each point.")]
        [Min(0.5f)] [SerializeField] private float _waypointRadius = 3f;

        [Tooltip("Max seconds the eel may spend heading to one path point before the target advances anyway " +
                 "(so a snag on the reef can't stall it forever).")]
        [Min(1f)] [SerializeField] private float _waypointTimeout = 8f;

        [Tooltip("Safety cap (seconds): if a travelling moray hasn't reached its cave by now (snagged on " +
                 "reef, etc.) it is treated as arrived so it can never get stuck travelling.")]
        [Min(1f)] [SerializeField] private float _travelTimeout = 30f;

        [Tooltip("Seconds between GPU centroid read-backs used for arrival detection. Kept low-frequency " +
                 "on purpose (each read is a synchronous GPU read-back).")]
        [Min(0.1f)] [SerializeField] private float _centroidPollInterval = 0.5f;

        [Header("Idle motion in cave (optional polish)")]
        [Tooltip("Small sideways bob of the loiter point so the eel isn't pinned to one spot. 0 = off.")]
        [Min(0f)] [SerializeField] private float _idleBobAmplitude = 0f;

        [Tooltip("Speed of the idle bob (radians/sec).")]
        [Min(0f)] [SerializeField] private float _idleBobSpeed = 0.6f;

        [Header("Path following (kinematic head drive)")]
        [Tooltip("Once the eel reaches the first path point, its HEAD is driven directly along the path (not " +
                 "steered), so it follows the points strictly and the body/tail trails behind and curves. " +
                 "This is how fast (m/s) the head travels along the path — set near the moray's swim speed.")]
        [Min(0.1f)] [SerializeField] private float _pathSpeed = 4f;

        [Tooltip("How close (m) the eel must steer to the FIRST path point before the head locks onto the path " +
                 "and starts being driven kinematically. Keep it modest so it doesn't orbit the first point.")]
        [Min(0.5f)] [SerializeField] private float _pathStartRadius = 3.5f;

        [Tooltip("Look-ahead (m) along the path for the eel's facing target, so its head points forward down " +
                 "the route while the head position is locked to the path.")]
        [Min(0f)] [SerializeField] private float _pathLookAhead = 2.5f;

        // Per-moray runtime state, keyed by the school's target (stable for the school's lifetime).
        private class MorayState
        {
            public enum Phase { Travelling, InCave }
            public Phase   phase;
            public MorayCave cave;      // the cave this moray owns / is heading to (occupancy = any agent.cave == cave)
            public float   dwell;       // seconds settled in the current cave
            public float   rollClock;   // seconds since the last leave-roll
            public float   nextPoll;    // Time.time at which we may next read the centroid
            public float   restWeight;  // 0..1: drives the compute PIN (head lock) + render undulation fade
            public bool    onPath;      // false = steering toward the first point; true = head locked to the path
            public float   cursorDist;  // arclength (m) the head has advanced along the path
            public Vector3 pinTarget;   // where the head is pinned this frame (published as this cave's anchor)
        }

        private readonly Dictionary<EcosystemTargetGPU, MorayState> _agents
            = new Dictionary<EcosystemTargetGPU, MorayState>();

        // Scratch reused each frame to find agents whose school has gone (removed / now exiting).
        private readonly List<EcosystemTargetGPU> _seen  = new List<EcosystemTargetGPU>();
        private readonly List<EcosystemTargetGPU> _stale = new List<EcosystemTargetGPU>();

        // Fixed-length anchor arrays reused each frame (must match BoidSimulationGPU.MorayMaxRestAnchors).
        private const int MaxRestAnchors = 8;
        private readonly Vector4[] _anchorPos = new Vector4[MaxRestAnchors];
        private readonly Vector4[] _anchorDir = new Vector4[MaxRestAnchors];

        private void Awake()
        {
            if (_sim == null) _sim = FindFirstObjectByType<EcosystemSimulationGPU>();
            if (_moraySpecies == null) _moraySpecies = FindMoraySpecies();
            if (_boidSim == null) _boidSim = FindFirstObjectByType<OceanX.BoidsGPU.SpatialPartitionInstancedRendering.BoidSimulationGPU>();
        }

        // Locates the Giant moray SpeciesDataGPU from the scene's spawners so the director works without
        // manual wiring. Matches on the species name containing "moray" (case-insensitive).
        private static SpeciesDataGPU FindMoraySpecies()
        {
            var spawners = FindObjectsByType<BoidSpawnerGPUMultiTargets>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sp in spawners)
            {
                SpeciesDataGPU s = sp != null ? sp.SpeciesData : null;
                if (s != null && !string.IsNullOrEmpty(s.SpeciesName) &&
                    s.SpeciesName.ToLowerInvariant().Contains("moray"))
                    return s;
            }
            return null;
        }

        private void Update()
        {
            if (_sim == null || _moraySpecies == null) return;

            int committed = _sim.CountCommittedGroups(_moraySpecies); // schools NOT swimming out

            // Obstacle-avoidance override: suppress the moray's reef avoidance (0) ONLY when EVERY moray is
            // cave-managed (path-following / resting), so it hugs the authored path and clips slightly into
            // the reef. If any moray is roaming (an extra with no free cave), keep the species' normal
            // avoidance so it still steers around the reef. Uses last frame's agent set (1-frame lag, fine).
            if (_boidSim != null)
            {
                bool allManaged = committed > 0 && _agents.Count >= committed;
                _boidSim.SetMorayAvoidanceOverride(allManaged ? 0f : -1f);
            }

            if (MorayCave.All.Count == 0) return; // no caves placed -> nothing to manage or publish

            float dt = Time.deltaTime;
            _seen.Clear();

            // Drive / claim every settled (non-exiting) moray school.
            for (int i = 0; i < committed; i++)
            {
                EcosystemTargetGPU target = _sim.GetSchoolTarget(_moraySpecies, i);
                if (target == null) continue;
                _seen.Add(target);

                if (_agents.TryGetValue(target, out MorayState state))
                {
                    DriveAgent(state, target, i, dt);
                }
                else
                {
                    TryClaimCave(target); // new moray (or a roaming extra) grabs a cave if one is free
                }
            }

            // Release any agent whose school is gone: removed, or now swimming out (index >= committed,
            // so it never appeared in _seen this frame). We simply stop driving and free its cave — the
            // sim's ParkAt already owns the target's exit; re-enabling the animator here would drag the
            // fish back in-bounds and break the swim-out.
            _stale.Clear();
            foreach (var kv in _agents)
                if (!_seen.Contains(kv.Key)) _stale.Add(kv.Key);
            for (int i = 0; i < _stale.Count; i++)
                _agents.Remove(_stale[i]);

            PublishRestAnchors();
        }

        // Publishes each cave as a head PIN anchor for the compute shader: xyz = where the head is pinned
        // this frame (the moving cursor while the eel is driven along the path, or the rest spot once
        // settled), w = the pin/rest weight of the eel assigned to that cave (0 while it is still steering to
        // the first point / none assigned). The compute shader locks a moray head to its nearest anchor by
        // that weight, so the head is driven strictly along the path and held at the rest spot.
        private void PublishRestAnchors()
        {
            if (_boidSim == null) return;
            var caves = MorayCave.All;
            int n = Mathf.Min(caves.Count, MaxRestAnchors);
            for (int i = 0; i < n; i++)
            {
                MorayCave c = caves[i];
                Vector3 pos = c.RestPoint; float w = 0f;
                foreach (var kv in _agents)
                    if (kv.Value.cave == c) { pos = kv.Value.pinTarget; w = kv.Value.restWeight; break; }
                Vector3 dir = c.HeadOutDirection;
                _anchorPos[i] = new Vector4(pos.x, pos.y, pos.z, w);
                _anchorDir[i] = new Vector4(dir.x, dir.y, dir.z, 0f);
            }
            _boidSim.SetMorayRestAnchors(_anchorPos, _anchorDir, n);
        }

        // The rest/pin weight of the eel currently assigned to this cave (0 if none).
        private float RestWeightForCave(MorayCave cave)
        {
            foreach (var kv in _agents)
                if (kv.Value.cave == cave) return kv.Value.restWeight;
            return 0f;
        }

        // Assigns a free cave to a moray school and takes over its target. If every cave is occupied the
        // school is left roaming (its animator untouched) and re-checked next frame — it grabs a cave the
        // moment one frees.
        private void TryClaimCave(EcosystemTargetGPU target)
        {
            MorayCave cave = FirstFreeCave();
            if (cave == null) return; // all caves taken — keep roaming for now

            // Stop the path animator so it can't drag the target back onto its roaming route (same move
            // ParkAt makes on removal).
            if (target.Animator != null) target.Animator.enabled = false;

            var state = new MorayState
            {
                phase      = MorayState.Phase.Travelling,
                cave       = cave,
                dwell      = 0f,
                rollClock  = 0f,
                nextPoll   = Time.time + _centroidPollInterval,
                onPath     = false,
                cursorDist = 0f,
            };
            _agents[target] = state;
        }

        private void DriveAgent(MorayState state, EcosystemTargetGPU target, int schoolIndex, float dt)
        {
            // A cave marker destroyed at runtime leaves a Unity-null reference — try to re-home.
            if (state.cave == null)
            {
                MorayCave replacement = FirstFreeCave();
                if (replacement == null) return; // nowhere to go; leave the target where it is
                state.cave       = replacement;
                state.phase      = MorayState.Phase.Travelling;
                state.onPath     = false;
                state.cursorDist = 0f;
            }

            MorayCave cave = state.cave;

            // restWeight drives BOTH the compute head-PIN (so the head is locked to pinTarget and driven
            // directly, not steered) and the render undulation fade. It is on while on the path or resting,
            // off while still steering toward the first point.
            float weightTarget = (_enableRestPose && (state.onPath || state.phase == MorayState.Phase.InCave)) ? 1f : 0f;
            state.restWeight = Mathf.MoveTowards(state.restWeight, weightTarget, dt / Mathf.Max(0.05f, _restEaseSeconds));

            switch (state.phase)
            {
                case MorayState.Phase.Travelling:
                    if (!state.onPath)
                    {
                        // Approach: STEER toward the first path point (pin off) until the head is close, then
                        // lock onto the path so it stops orbiting and starts being driven along it.
                        Vector3 startPt = FirstPathPoint(cave);
                        state.pinTarget = startPt;
                        target.AffecterPosition = startPt;
                        if (Time.time >= state.nextPoll)
                        {
                            state.nextPoll = Time.time + _centroidPollInterval;
                            if (_sim.TryGetSchoolCentroid(_moraySpecies, schoolIndex, out Vector3 centroid) &&
                                Vector3.Distance(centroid, startPt) <= _pathStartRadius)
                            {
                                state.onPath     = true;
                                state.cursorDist = 0f;
                            }
                        }
                    }
                    else
                    {
                        // On the path: advance a head cursor along the route at a fixed speed. The head is
                        // PINNED to it (compute), so it follows the points strictly and the body trails and
                        // curves. Face a little further ahead so the head points forward down the route.
                        float total = PathTotalLength(cave);
                        state.cursorDist += _pathSpeed * dt;
                        if (state.cursorDist >= total)
                        {
                            state.cursorDist = total;
                            state.pinTarget  = PathPositionAtDistance(cave, total);
                            target.AffecterPosition = state.pinTarget;
                            EnterCave(state); // reached the rest spot -> settle
                        }
                        else
                        {
                            state.pinTarget = PathPositionAtDistance(cave, state.cursorDist);
                            target.AffecterPosition = PathPositionAtDistance(cave, state.cursorDist + _pathLookAhead);
                        }
                    }
                    break;

                case MorayState.Phase.InCave:
                    state.pinTarget = cave.RestPoint;
                    target.AffecterPosition = state.pinTarget;
                    state.dwell += dt;
                    if (state.dwell >= _caveDwellSeconds)
                    {
                        state.rollClock += dt;
                        if (state.rollClock >= _leaveRollInterval)
                        {
                            state.rollClock = 0f;
                            if (Random.value < _leaveChance)
                            {
                                MorayCave dest = PickFreeCaveOtherThan(state.cave);
                                if (dest != null) // stay put if the other cave is occupied
                                {
                                    state.cave       = dest; // reserve dest, frees the old cave (occupancy is by agent.cave)
                                    state.phase      = MorayState.Phase.Travelling;
                                    state.onPath     = false; // steer out to the new cave's first point, then lock on
                                    state.cursorDist = 0f;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void EnterCave(MorayState state)
        {
            state.phase     = MorayState.Phase.InCave;
            state.dwell     = 0f;
            state.rollClock = 0f;
            // FUTURE hook: trigger the resting/curl pose + mouth animation here.
        }

        // The first path point (where the eel locks onto the path). Falls back to the marker if no path.
        private static Vector3 FirstPathPoint(MorayCave cave)
        {
            return cave.PathPointCount > 0 ? cave.GetPathPoint(0) : cave.Position;
        }

        // Total arclength (m) of the cave's path (sum of segment lengths). 0 for < 2 points.
        private static float PathTotalLength(MorayCave cave)
        {
            int n = cave.PathPointCount;
            float len = 0f;
            for (int i = 1; i < n; i++) len += Vector3.Distance(cave.GetPathPoint(i - 1), cave.GetPathPoint(i));
            return len;
        }

        // World position at arclength `dist` along the cave's path, clamped to the ends. This is what the
        // head is driven along, so the route the points trace IS the path the head follows.
        private static Vector3 PathPositionAtDistance(MorayCave cave, float dist)
        {
            int n = cave.PathPointCount;
            if (n == 0) return cave.Position;
            if (n == 1 || dist <= 0f) return cave.GetPathPoint(0);
            float remaining = dist;
            for (int i = 1; i < n; i++)
            {
                Vector3 a = cave.GetPathPoint(i - 1);
                Vector3 b = cave.GetPathPoint(i);
                float seg = Vector3.Distance(a, b);
                if (remaining <= seg || i == n - 1)
                    return Vector3.Lerp(a, b, seg > 1e-4f ? Mathf.Clamp01(remaining / seg) : 1f);
                remaining -= seg;
            }
            return cave.GetPathPoint(n - 1);
        }

        // A cave no agent currently owns / is heading to, or null if all are occupied.
        private MorayCave FirstFreeCave()
        {
            var caves = MorayCave.All;
            for (int i = 0; i < caves.Count; i++)
                if (caves[i] != null && !IsOccupied(caves[i])) return caves[i];
            return null;
        }

        // A random free cave that is NOT the given one (for the "leave for another cave" move).
        private MorayCave PickFreeCaveOtherThan(MorayCave current)
        {
            var caves = MorayCave.All;
            int count = 0;
            MorayCave pick = null;
            for (int i = 0; i < caves.Count; i++)
            {
                MorayCave c = caves[i];
                if (c == null || c == current || IsOccupied(c)) continue;
                count++;
                if (Random.Range(0, count) == 0) pick = c; // reservoir pick — uniform without allocating
            }
            return pick;
        }

        private bool IsOccupied(MorayCave cave)
        {
            foreach (var kv in _agents)
                if (kv.Value.cave == cave) return true;
            return false;
        }
    }
}
