using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.SpatialPartitionInstancedRendering;

namespace OceanX.BoidsGPU.Ecosystem
{
    public class EcosystemSimulationGPU : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EcosystemDefinitionGPU _ecosystem;

        [Tooltip("The BoidSimulationGPU (03_Spatial_Partition_Instanced_Rendering) in the scene.")]
        [SerializeField] private BoidSimulationGPU _simulation;

        public EcosystemDefinitionGPU Ecosystem => _ecosystem;

        /// <summary>
        /// The simulation volume, taken straight from the BoidSimulationGPU so the ecosystem (where the
        /// roaming targets live) is always the SAME box as the actual boid simulation area — no need to
        /// keep EcosystemDefinitionGPU's center/size in sync by hand. Falls back to the asset's bounds
        /// only if the BoidSimulationGPU reference is missing (e.g. drawing gizmos before it is wired).
        /// </summary>
        private Bounds SimulationBounds =>
            _simulation != null ? _simulation.SimulationAreaBounds : _ecosystem.SimulationBounds;

        [Header("Population Tick")]
        [Tooltip("Master switch for the automatic ratio-driven population dynamics (growth / shrink / " +
                 "starvation). When OFF, populations are frozen and change ONLY via manual Add/Remove " +
                 "(UI / netcode / debug harness) — nothing auto-grows or despawns. Handy while testing " +
                 "movement or shaders. Can be toggled live in Play mode.")]
        [SerializeField] private bool _enablePopulationDynamics = true;

        [Tooltip("Seconds between each population tick.")]
        [SerializeField] private float _tickInterval = 5f;

        /// <summary>Whether the automatic ratio-driven population tick is running. Manual Add/Remove always works.</summary>
        public bool PopulationDynamicsEnabled
        {
            get => _enablePopulationDynamics;
            set => _enablePopulationDynamics = value;
        }

        [Header("Removal animation (swim-out)")]
        [Tooltip("How close (metres) a swimming-out fish must get to its exit point to count as having " +
                 "reached it. A school is culled the instant ALL of its fish are within this distance of " +
                 "their exit point — no fixed timer. A few metres works; too small and a stray fish can " +
                 "stop the school being removed.")]
        [Min(0.5f)]
        [SerializeField] private float _exitArrivalRadius = 3f;

        [Tooltip("How often (seconds) to check whether a swimming-out school's fish have all reached their " +
                 "exit point. Each check reads those fish positions back from the GPU, so keep it modest.")]
        [Min(0.05f)]
        [SerializeField] private float _exitPollInterval = 0.2f;

        [Header("Swim Path Targets (demo-style Target + Target Animator per school)")]
        [Tooltip("Influence radius of each spawned target (sets the Target object's scale, like the " +
                 "demo spawner's Target Radius).")]
        [Min(0.1f)]
        [SerializeField] private float _targetRadius = 3f;

        [Tooltip("Each target's path speed is a random fraction (in this range) of ITS species' cruising " +
                 "speed. Keep it slightly ABOVE 1: fish are hard-clamped to never swim slower than their " +
                 "cruising speed, so a SLOWER target gets overtaken and the school mills around it in a " +
                 "loose scattered cloud. A target a touch faster stays just ahead and the school streams " +
                 "behind it in a tight line (the demo ratio was ~1.15).")]
        [SerializeField] private Vector2 _targetSpeedFractionRange = new Vector2(1.05f, 1.3f);

        [Tooltip("Metres from the simulation-bounds edge kept free of paths so a target can never leave " +
                 "the volume (a target outside the bounds reads as 'exiting' to the compute shader and " +
                 "would pull its whole school off-screen).")]
        [Min(0f)]
        [SerializeField] private float _pathBoundsSafeZone = 3f;

        [Tooltip("Vertical-only version of the safe zone: metres kept free at the FLOOR and CEILING of the " +
                 "simulation bounds. Separate from Path Bounds Safe Zone because that one reserves room for " +
                 "a path's horizontal extent, whereas a path is flat — its height is a single value, so it " +
                 "only needs enough clearance that the target is never AT the boundary (a target outside " +
                 "the bounds reads as 'exiting' and pulls its school off-screen). Keep this small, or it " +
                 "eats into the seabed and surface ends of the species depth bands: at the default 3m " +
                 "safe zone a bottom-dwelling species could not get near the sand at all.")]
        [Min(0.1f)]
        [SerializeField] private float _pathVerticalClearance = 0.5f;

        [Tooltip("Random range for each school's path size, as a fraction of the largest size that fits " +
                 "inside the bounds — so schools get small and large routes.")]
        [SerializeField] private Vector2 _pathSizeMultiplierRange = new Vector2(0.35f, 0.9f);

        [Header("Entry Spawn Spreading (anti-stacking)")]
        [Tooltip("Sideways jitter radius applied around a chosen entry marker. A marker is a single point, " +
                 "so schools added in quick succession (spam-Add) would otherwise spawn exactly on top of " +
                 "each other; this nudges each new school off the marker, perpendicular to its swim-in " +
                 "direction so it stays off-screen. 0 = spawn exactly on the marker (old behaviour).")]
        [Min(0f)]
        [SerializeField] private float _entrySpawnJitterRadius = 4f;

        [Tooltip("Preferred minimum distance between a new school's spawn origin and recently spawned " +
                 "schools. The jitter above is retried a few times to satisfy this; if it can't, the " +
                 "farthest-apart attempt is used. Roughly one school's cluster diameter is a good value.")]
        [Min(0f)]
        [SerializeField] private float _entrySpawnMinSeparation = 3f;

        [Tooltip("How much deeper/shallower than the BEST-matching entry marker another marker may be and " +
                 "still be considered for a species' spawn-in. Each species enters through the gates nearest " +
                 "its own Preferred Depth Band, so a ray slides in along the sand instead of dropping in from " +
                 "mid-water. 0 = always use the single closest-matching gate (least variety). Larger values " +
                 "let more gates share the traffic, at the cost of fish sometimes entering off-depth; make it " +
                 "big enough and depth stops mattering at all.")]
        [Min(0f)]
        [SerializeField] private float _entryMarkerDepthTolerance = 3f;

        [Header("Predator-Prey Balance (global)")]
        [Tooltip("Lower edge of the balanced prey:predator ratio (measured in SCHOOL counts). " +
                 "Below this, there are too many predators for the prey — prey shrinks, predators starve.")]
        [SerializeField] private float _ratioBandLow = 1f;
        [Tooltip("Upper edge of the balanced prey:predator ratio (in SCHOOL counts). " +
                 "Above this, there are too few predators — prey overpopulates, well-fed predators grow.")]
        [SerializeField] private float _ratioBandHigh = 3f;
        [Tooltip("Per-tick chance (0-1) that an under-predated / well-fed species gains one school.")]
        [Range(0f, 1f)] [SerializeField] private float _growRate = 0.3f;
        [Tooltip("Per-tick chance (0-1) that an over-predated / starving species loses one school.")]
        [Range(0f, 1f)] [SerializeField] private float _shrinkRate = 0.3f;

        [Header("Eco-Health weights (sum ~1)")]
        [Tooltip("How much species diversity (fraction of species alive) contributes to eco-health.")]
        [Range(0f, 1f)] [SerializeField] private float _healthDiversityWeight = 0.4f;
        [Tooltip("How much food-web balance contributes (fraction of food-web species that are stable or " +
                 "growing — i.e. not starving, over-hunted, or overpopulated).")]
        [Range(0f, 1f)] [SerializeField] private float _healthBalanceWeight = 0.4f;
        [Tooltip("How much apex-predator presence contributes (the shark is the keystone).")]
        [Range(0f, 1f)] [SerializeField] private float _healthApexWeight = 0.2f;

        /// <summary>Current ecosystem health in 0..1, derived live from school counts. Read by the netcode layer.</summary>
        public float EcoHealth01 => ComputeEcoHealth01();

        // -------------------------------------------------------------------------
        // Runtime state — every species starts at zero schools (empty ocean). The
        // player builds the ecosystem up from nothing; schools are added/removed one
        // at a time and a species is excluded from the simulation entirely at N == 0.
        // -------------------------------------------------------------------------

        // Current number of schools per species — the one piece of genuine runtime state.
        // ALL species start at 0. A species is in this dictionary iff it has a matching spawner.
        // FishPerSchool / MaxSchools / carrying capacity are constant authoring data and are read
        // straight off the SpeciesDataGPU asset (see FishPerSchool / MaxSchoolsOf helpers).
        private readonly Dictionary<SpeciesDataGPU, int> _schoolCount = new Dictionary<SpeciesDataGPU, int>();
        // Animated roaming targets created at runtime, one per school, owned by this manager so they
        // can be destroyed on removal/extinction with no leaks.
        private readonly Dictionary<SpeciesDataGPU, List<SimulationAffecterComponent>> _managedTargets
            = new Dictionary<SpeciesDataGPU, List<SimulationAffecterComponent>>();

        // Per-species count of schools currently swimming out (an exit animation in flight). Removals are
        // immediate and concurrent: each Remove parks one more of the species' TOP schools off-screen so
        // it beelines out right away, incrementing this count. The exiting schools are always the highest
        // K sub-groups — a contiguous block — so a single BatchExitRoutine can cull them together once
        // they have all reached their exit points, with no mid-flight buffer reindexing.
        private readonly Dictionary<SpeciesDataGPU, int> _exitingCount = new Dictionary<SpeciesDataGPU, int>();

        private bool IsExiting(SpeciesDataGPU species) => _exitingCount.TryGetValue(species, out int e) && e > 0;
        private int  ExitingCount(SpeciesDataGPU species) => _exitingCount.TryGetValue(species, out int e) ? e : 0;

        // Reusable scratch list for filtering entry/exit markers by mode (avoids per-call allocation).
        private readonly List<FishEntryPointGPU> _markerScratch = new List<FishEntryPointGPU>();

        // Anti-stacking spawn spreading: the last few entry origins used (most-recent last) plus the last
        // marker chosen, so back-to-back Adds fan out across different gates and don't pile up on one point.
        private readonly List<Vector3> _recentEntryOrigins = new List<Vector3>();
        private FishEntryPointGPU _lastEntryMarker = null;
        private const int RecentEntryOriginMemory = 8;

        // FIFO of pending +1 (add) operations per species. Removes execute immediately (see
        // StartRemoveExitImmediate); only Adds queue — and only while the species is busy swimming schools
        // out — so an Add pressed mid-exit lands after the batch commits instead of colliding with it.
        private readonly Dictionary<SpeciesDataGPU, Queue<int>> _opQueue
            = new Dictionary<SpeciesDataGPU, Queue<int>>();

        // Constant per-species authoring values, clamped to a sane minimum. Read directly from the
        // asset so there is a single source of truth (no cached copies to drift out of sync).
        private static int FishPerSchool(SpeciesDataGPU species) => Mathf.Max(1, species.FishPerSchool);
        private static int MaxSchoolsOf(SpeciesDataGPU species) => Mathf.Max(1, species.MaxSchools);

        // -------------------------------------------------------------------------
        // Unity lifecycle — Awake runs before any Start, so spawners are set inactive
        // (N = 0) before BoidSimulationGPU.Start initializes the GPU buffers. The sim
        // therefore boots into a valid "empty ocean" state.
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (_ecosystem == null)
            {
                Debug.LogError("[EcosystemSimulationGPU] EcosystemDefinitionGPU not assigned.", this);
                enabled = false;
                return;
            }

            if (_simulation == null)
            {
                Debug.LogError("[EcosystemSimulationGPU] BoidSimulationGPU not assigned.", this);
                enabled = false;
                return;
            }

            SetupAllSpecies();
        }

        private void Start()
        {
            WarnAboutMisplacedEntryPoints();
            StartCoroutine(PopulationTickRoutine());
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Queues adding one school to this species (fish swim in). Honoured in order with any
        /// other pending add/remove for the species; capped at MaxSchools. Called by UI / netcode.</summary>
        public void AddSpecies(SpeciesDataGPU species)
        {
            if (!ValidateSpecies(species, out BoidSpawnerGPUMultiTargets spawner)) return;
            EnqueueOp(species, +1);
            PumpQueue(species, spawner);
        }

        /// <summary>Removes one school from this species immediately: its fish start swimming out the
        /// moment this is called, concurrently with any other schools already leaving. Spamming Remove
        /// sends several schools out at once. Stops at extinction. Called by UI / netcode.</summary>
        public void RemoveSpecies(SpeciesDataGPU species)
        {
            if (!ValidateSpecies(species, out BoidSpawnerGPUMultiTargets spawner)) return;
            StartRemoveExitImmediate(species, spawner);
        }

        // -------------------------------------------------------------------------
        // Operation queue — serialises add/remove per species so rapid taps and mixed
        // add/remove are all honoured, animating one at a time.
        // -------------------------------------------------------------------------

        private void EnqueueOp(SpeciesDataGPU species, int delta)
        {
            if (!_opQueue.TryGetValue(species, out Queue<int> q))
            {
                q = new Queue<int>();
                _opQueue[species] = q;
            }
            q.Enqueue(delta);
        }

        // Drains queued ops. Adds apply instantly; a queued remove starts an immediate swim-out. The pump
        // is held off while the species is busy swimming schools out (BatchExitRoutine re-runs it once the
        // batch commits) so queued Adds land after the exiting block is gone, not tangled up with it.
        private void PumpQueue(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            if (IsExiting(species)) return; // busy swimming schools out; resumes after the batch commits
            if (!_opQueue.TryGetValue(species, out Queue<int> q)) return;

            while (q.Count > 0 && !IsExiting(species))
            {
                int op = q.Dequeue();
                if (op > 0)
                {
                    if (AddSchool(species, spawner))
                        _simulation.ReinitializeBuffers();
                }
                else
                {
                    StartRemoveExitImmediate(species, spawner);
                }
            }
        }

        /// <summary>
        /// Returns the current number of schools for this species (one unit = one school).
        /// 0 means the species is extinct / not yet added. This is the RAW count including
        /// schools currently swimming out — the internal swim-out logic depends on that, so
        /// UI should read CountCommittedGroups instead.
        /// </summary>
        public int CountGroups(SpeciesDataGPU species)
        {
            if (species == null) return 0;
            return _schoolCount.TryGetValue(species, out int n) ? n : 0;
        }

        /// <summary>
        /// School count the player has committed to, i.e. excluding schools currently swimming
        /// out. Drops the instant Remove is pressed (which parks a school for exit) while its
        /// fish still animate away on-screen. This is what the UI/netcode should display so the
        /// tablet number updates immediately on removal instead of waiting for the swim-out.
        /// </summary>
        public int CountCommittedGroups(SpeciesDataGPU species)
        {
            if (species == null) return 0;
            int raw = _schoolCount.TryGetValue(species, out int n) ? n : 0;
            return Mathf.Max(0, raw - ExitingCount(species));
        }

        /// <summary>Static per-species cap on the number of schools. 0 if the species is null.</summary>
        public int GetMaxSchools(SpeciesDataGPU species)
        {
            if (species == null) return 0;
            return MaxSchoolsOf(species);
        }

        /// <summary>
        /// Cause-aware population status for UI / Alucia messaging (this does NOT drive the tick — that
        /// still uses <see cref="PopulationPressure"/>). It distinguishes WHY a species is struggling so the
        /// guide can say the right thing, and stays quiet for trivial states (e.g. a lone freshly-added
        /// species with nothing established yet):
        ///   <b>Starving</b>     — it eats other species, its food WAS present but is now gone/scarce (a real collapse).
        ///   <b>OverPredated</b> — its predators are present and outnumber it past the band (being over-hunted).
        ///   <b>Overpopulated</b>— it normally has predators, they are ABSENT, and it has grown near its cap.
        ///   <b>Balanced</b>     — none of the above (includes a just-added species, or a lone predator whose
        ///                         prey has never been introduced — which must NOT read as "starving").
        /// </summary>
        public enum SpeciesStatus { Absent, Balanced, Starving, OverPredated, Overpopulated }

        [Header("Overpopulation thresholds (Alucia reactions + eco-health balance)")]
        [Tooltip("A species that outnumbers its combined predators by MORE than this ratio (in school counts) " +
                 "counts as overpopulated even while some predators remain — they can no longer keep it in " +
                 "check. Below this it is merely 'growing'. Set above RatioBandHigh (the growing threshold).")]
        [SerializeField] private float _overpopulatedRatio = 7f;
        [Tooltip("The OTHER overpopulation trigger: when a species' predators are ENTIRELY gone, it is " +
                 "overpopulated once it has MORE than this many schools (so a freshly-added school or two is " +
                 "never labelled overpopulated). A flat count, independent of the species' cap, so the warning " +
                 "appears promptly the moment its predators are wiped out.")]
        [SerializeField] private int _overpopulatedFreeCount = 3;

        // Per-species memory: has this species' food ever been present? Lets us tell a genuine collapse
        // ("prey ran out") apart from "prey were never introduced yet" (which must NOT read as starving).
        private readonly HashSet<SpeciesDataGPU> _foodWasPresent = new HashSet<SpeciesDataGPU>();

        /// <summary>Cause-aware status for a species (committed counts). See <see cref="SpeciesStatus"/>.</summary>
        public SpeciesStatus GetSpeciesStatus(SpeciesDataGPU species)
        {
            if (species == null) return SpeciesStatus.Absent;
            Dictionary<SpeciesDataGPU, int> counts = BuildCommittedCounts();
            int n = CountIn(counts, species);
            if (n <= 0) return SpeciesStatus.Absent;

            bool hasPrey      = HasAny(species.PreySpecies);
            bool hasPredators = HasAny(species.PredatorSpecies);
            int  preyN = hasPrey      ? SumCounts(counts, species.PreySpecies)     : 0;
            int  predN = hasPredators ? SumCounts(counts, species.PredatorSpecies) : 0;

            if (hasPrey && preyN > 0) _foodWasPresent.Add(species); // remember its food has existed at least once

            // Starving: it eats prey, that food WAS present before, and now it is gone or too scarce.
            // (A lone predator whose prey has never been added stays Balanced — no false alarm.)
            if (hasPrey && _foodWasPresent.Contains(species)
                && (preyN <= 0 || (float)preyN / n < _ratioBandLow))
                return SpeciesStatus.Starving;

            // Over-predated: predators are actually present and outnumber it past the low band.
            if (predN > 0 && (float)n / predN < _ratioBandLow)
                return SpeciesStatus.OverPredated;

            // Overpopulated: it SHOULD be kept in check by predators, they are absent, and it has grown near cap.
            if (IsOverpopulated(species, counts)) return SpeciesStatus.Overpopulated;

            return SpeciesStatus.Balanced;
        }

        /// <summary>
        /// Genuine runaway overpopulation: a species that SHOULD be regulated by predators, but all of
        /// them are gone, and it has grown to near its cap. An apex with no natural predators is never
        /// "overpopulated" (its growth toward its cap is normal). Shared by the status classifier and
        /// eco-health so both agree on what counts as "too many".
        /// </summary>
        private bool IsOverpopulated(SpeciesDataGPU species, Dictionary<SpeciesDataGPU, int> counts)
        {
            if (!HasAny(species.PredatorSpecies)) return false; // apex / no natural predators -> never overpopulated
            int n     = CountIn(counts, species);
            int predN = SumCounts(counts, species.PredatorSpecies);

            // Overpopulated if EITHER it has run away past the overpopulation ratio while some predators still
            // remain (they can no longer hold it back), OR its predators are entirely gone and it has grown
            // near its cap. (This is an OR: either condition alone flags it.)
            if (predN > 0)
                return (float)n / predN > _overpopulatedRatio;

            // Predators entirely gone: overpopulated once it has more than a few schools (nothing left to hold
            // it back). Flat count, not a fraction of cap, so the warning appears promptly.
            return n > _overpopulatedFreeCount;
        }

        // -------------------------------------------------------------------------
        // Population tick — symmetric, ratio-driven predator-prey dynamics.
        //
        // Each species feels two forces, both measured as a prey:predator ratio in
        // SCHOOL counts against a shared [low, high] dead-band:
        //   • Predation (top-down): few predators (ratio high) → grow; many (ratio low) → shrink.
        //   • Food       (bottom-up): prey abundant (ratio high) → grow; prey scarce (ratio low) → starve.
        // Inside the band nothing changes. Counts are read from a snapshot taken at the
        // start of the tick so every species is updated from the SAME state (order-independent).
        // Manual add/remove and these dynamics are the only things that change population.
        // -------------------------------------------------------------------------

        private IEnumerator PopulationTickRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_tickInterval);
                RunPopulationTick();
            }
        }

        private void RunPopulationTick()
        {
            // Master switch — when off, populations are frozen (manual Add/Remove still works).
            if (!_enablePopulationDynamics) return;

            // Snapshot counts so all decisions use the same pre-tick state.
            Dictionary<SpeciesDataGPU, int> snapshot = new Dictionary<SpeciesDataGPU, int>(_schoolCount);

            bool anyChanged = false;
            foreach (SpeciesDataGPU species in _ecosystem.Species)
            {
                if (species == null) continue;
                if (!snapshot.TryGetValue(species, out int n) || n <= 0) continue; // extinct / not present
                if (IsExiting(species)) continue;                                   // mid swim-out — leave it alone

                BoidSpawnerGPUMultiTargets spawner = FindSpawner(species);
                if (spawner == null) continue;

                int pressure = PopulationPressure(species, snapshot);
                if (pressure > 0)
                {
                    if (Random.value < _growRate && AddSchool(species, spawner)) anyChanged = true;
                }
                else if (pressure < 0)
                {
                    // Shrink animates a swim-out (async); no immediate rebuild needed here.
                    if (Random.value < _shrinkRate) StartRemoveExitImmediate(species, spawner);
                }
            }

            if (anyChanged) _simulation.ReinitializeBuffers();
        }

        /// <summary>
        /// Net population tendency for a species given a snapshot of school counts:
        /// +1 = should grow, -1 = should shrink, 0 = balanced (inside the dead-band).
        /// Combines predation pressure (from its predators) and food availability (from its prey).
        /// </summary>
        private int PopulationPressure(SpeciesDataGPU species, Dictionary<SpeciesDataGPU, int> counts)
        {
            int n = CountIn(counts, species);
            if (n <= 0) return 0;

            bool hasPrey      = HasAny(species.PreySpecies);
            bool hasPredators = HasAny(species.PredatorSpecies);

            // Starvation is a HARD override — a species can't grow without food, no matter how
            // few predators it has. This keeps the keystone-collapse cascade intact: when prey
            // runs out, the predator shrinks even if its own predators are gone.
            // (Primary consumers have no modelled prey — they graze algae — so they never starve.)
            if (hasPrey)
            {
                int preySchools = SumCounts(counts, species.PreySpecies);
                if (preySchools <= 0) return -1;                                  // food gone
                if ((float)preySchools / n < _ratioBandLow) return -1;           // food scarce
            }

            // Otherwise, weigh growth vs decline from predation pressure + food abundance.
            int growVotes = 0;
            int shrinkVotes = 0;

            if (hasPredators)
            {
                int predatorSchools = SumCounts(counts, species.PredatorSpecies);
                if (predatorSchools <= 0)
                {
                    growVotes++; // no predators present → prey overpopulates toward its cap
                }
                else
                {
                    float ratio = (float)n / predatorSchools;
                    if (ratio > _ratioBandHigh) growVotes++;       // too few predators
                    else if (ratio < _ratioBandLow) shrinkVotes++; // too many predators
                }
            }

            if (hasPrey)
            {
                // Scarcity already handled by the override above; here only abundance drives growth.
                if ((float)SumCounts(counts, species.PreySpecies) / n > _ratioBandHigh) growVotes++;
            }

            return growVotes.CompareTo(shrinkVotes); // sign of (grow - shrink): +1 / 0 / -1
        }

        // -------------------------------------------------------------------------
        // Eco-health — a read-only scoreboard derived from the SAME ratios the
        // dynamics use, so the bar, the warnings and the population all agree.
        // -------------------------------------------------------------------------

        private float ComputeEcoHealth01()
        {
            // Health reads the COMMITTED counts (schools mid swim-out already subtracted) so the
            // bar — on both the big screen and the tablet — reacts the instant Remove is pressed,
            // in lockstep with the population number, instead of waiting for the fish to leave.
            Dictionary<SpeciesDataGPU, int> committed = BuildCommittedCounts();

            int totalSpecies = 0;
            int aliveSpecies = 0;
            int considered = 0;   // alive species that participate in the food web
            int healthy = 0;      // ...of those, how many are stable or growing (not declining / runaway)
            bool apexAlive = false;

            foreach (SpeciesDataGPU species in _ecosystem.Species)
            {
                if (species == null) continue;
                totalSpecies++;

                int n = CountIn(committed, species);
                if (n <= 0) continue;
                aliveSpecies++;

                if (species.Role == SpeciesRoleGPU.Apex) apexAlive = true;

                if (HasAny(species.PredatorSpecies) || HasAny(species.PreySpecies))
                {
                    considered++;
                    // "Growing" counts as healthy just like "stable" — a well-fed apex filling out toward
                    // its cap is not a problem. Only a species that is actively DECLINING (starving or
                    // being over-hunted -> negative pressure) or has genuinely run away (its predators are
                    // gone and it has ballooned near its cap) is unhealthy.
                    bool declining = PopulationPressure(species, committed) < 0;
                    if (!declining && !IsOverpopulated(species, committed)) healthy++;
                }
            }

            if (totalSpecies == 0) return 0f;

            float diversity = (float)aliveSpecies / totalSpecies;
            // Balance is measured against the WHOLE ecosystem (all species), not just the ones currently
            // present — so a single healthy fish reads as ~1/N of the balance, not a full 100%. This makes
            // eco-health ramp up gradually as the reef is built, instead of jumping the instant one fish is
            // added. 100% still needs every species alive AND healthy.
            float balance   = totalSpecies > 0 ? (float)healthy / totalSpecies : 0f;
            float apex      = apexAlive ? 1f : 0f;

            float weightSum = Mathf.Max(0.0001f, _healthDiversityWeight + _healthBalanceWeight + _healthApexWeight);
            float health = (_healthDiversityWeight * diversity
                          + _healthBalanceWeight   * balance
                          + _healthApexWeight      * apex) / weightSum;
            return Mathf.Clamp01(health);
        }

        // Reused scratch dict for the committed-count snapshot (raw minus schools swimming out) so
        // eco-health can be recomputed every frame without allocating.
        private readonly Dictionary<SpeciesDataGPU, int> _committedScratch = new Dictionary<SpeciesDataGPU, int>();

        // Builds a per-species snapshot of COMMITTED school counts: the raw count minus any schools
        // currently swimming out. This is the count the player has effectively committed to, and what
        // the UI (population numbers + eco-health) should reflect the instant Remove is pressed.
        private Dictionary<SpeciesDataGPU, int> BuildCommittedCounts()
        {
            _committedScratch.Clear();
            foreach (KeyValuePair<SpeciesDataGPU, int> kv in _schoolCount)
                _committedScratch[kv.Key] = Mathf.Max(0, kv.Value - ExitingCount(kv.Key));
            return _committedScratch;
        }

        // Count/sum helpers over a snapshot or the live dictionary.
        private static int CountIn(Dictionary<SpeciesDataGPU, int> counts, SpeciesDataGPU species)
            => species != null && counts.TryGetValue(species, out int n) ? n : 0;

        private static int SumCounts(Dictionary<SpeciesDataGPU, int> counts, List<SpeciesDataGPU> species)
        {
            if (species == null) return 0;
            int sum = 0;
            for (int i = 0; i < species.Count; i++) sum += CountIn(counts, species[i]);
            return sum;
        }

        private static bool HasAny(List<SpeciesDataGPU> species)
        {
            if (species == null) return false;
            for (int i = 0; i < species.Count; i++) if (species[i] != null) return true;
            return false;
        }

        // -------------------------------------------------------------------------
        // Internal school mutation — no GPU rebuild here; the caller rebuilds once.
        // Returns true if the school count actually changed.
        // -------------------------------------------------------------------------

        private bool AddSchool(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            int n = _schoolCount.TryGetValue(species, out int current) ? current : 0;
            if (n >= MaxSchoolsOf(species)) return false; // at cap — no-op

            int fishPerSchool = FishPerSchool(species);

            // Create the target for the new school FIRST so the spawner has >= N targets before the
            // rebuild (BoidSpawnerGPUMultiTargets needs one target per sub-group). The new school's
            // sub-group index is the old count n (0-based).
            CreateTarget(species, spawner, n);

            int newN = n + 1;
            _schoolCount[species] = newN;
            spawner.SetSchoolConfiguration(newN, fishPerSchool);

            // Spawn the new school off-screen at a random entry point so it swims into the ecosystem
            // (instead of popping in). No-op fallback to the normal in-bounds spawn if no entry points
            // are placed in the scene. Applies to both manual add and the automatic population tick.
            ApplyEntrySpawnOrigin(species, spawner);
            return true;
        }

        // Starts ONE more school of this species swimming out, immediately and concurrently with any
        // others already leaving. Each call parks the highest not-yet-exiting school's target at an
        // off-screen exit point, so its fish beeline out the moment Remove is pressed — spamming Remove
        // sends several schools out at once instead of draining them one at a time. The exiting schools
        // are always the species' TOP K sub-groups (a contiguous block); a single BatchExitRoutine culls
        // the whole block together once they have all arrived. The count / GPU buffers do NOT change here.
        private void StartRemoveExitImmediate(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            int n = CountGroups(species);
            int alreadyExiting = ExitingCount(species);
            int settled = n - alreadyExiting;
            if (settled <= 0) return; // every remaining school is already swimming out — nothing to do

            // Park the highest settled school (sub-group index settled - 1) so it starts exiting now.
            EcosystemTargetGPU exitingTarget = GetTargetAt(species, settled - 1);
            if (exitingTarget == null)
            {
                // No target to animate (shouldn't happen) — cull one immediately so counts stay consistent.
                if (CommitRemoveSchools(species, spawner, 1))
                    _simulation.ReinitializeBuffers();
                return;
            }

            exitingTarget.ParkAt(PickExitPoint());
            _exitingCount[species] = alreadyExiting + 1;

            // Start the batch cull coroutine only on the 0 -> 1 transition. If one is already running it
            // picks up this newly parked school on its next poll (it reads the live exiting count).
            if (alreadyExiting == 0)
                StartCoroutine(BatchExitRoutine(species, spawner));
        }

        // Watches every swimming-out school of this species and culls the whole top block the instant ALL
        // of their fish have reached their exit point (each fish within _exitArrivalRadius of its own
        // school's parked exit point) — no timer. The exiting schools are the contiguous top block of
        // sub-groups, so committing them together keeps surviving school indices stable. More Removes
        // arriving mid-swim just grow the block, so the cull naturally waits for the latest-added schools too.
        private IEnumerator BatchExitRoutine(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            int fishPerSchool = FishPerSchool(species);
            WaitForSeconds wait = new WaitForSeconds(_exitPollInterval);

            while (true)
            {
                yield return wait;

                int e = ExitingCount(species);
                if (e <= 0) break; // nothing exiting (shouldn't happen)

                int n = CountGroups(species);
                if (n <= 0) { _exitingCount[species] = 0; break; } // nothing left

                e = Mathf.Min(e, n);
                int firstExiting = n - e; // exiting schools are the contiguous top block [firstExiting, n-1]

                // Each exiting school beelines to its OWN exit point, so count arrivals per school against
                // that school's parked target. When every fish of every exiting school is within the arrival
                // radius, they have all reached the exit point — cull the whole block right then.
                // RenderingOffset is re-read each poll in case a rebuild for another species shifted the slice.
                int reached = 0;
                bool readOk = true;
                for (int j = firstExiting; j < n; j++)
                {
                    EcosystemTargetGPU target = GetTargetAt(species, j);
                    Vector3 exitPoint = target != null ? target.AffecterPosition : SimulationBounds.center;

                    int startIndex = spawner.RenderingOffset + j * fishPerSchool;
                    if (_simulation.TryCountBoidsWithinRadius(startIndex, fishPerSchool, exitPoint, _exitArrivalRadius, out int within))
                        reached += within;
                    else { readOk = false; break; } // buffers not ready this poll — try again next tick
                }

                if (readOk && reached >= e * fishPerSchool)
                {
                    if (CommitRemoveSchools(species, spawner, e))
                        _simulation.ReinitializeBuffers();
                    _exitingCount[species] = 0;
                    break;
                }
            }

            // Resume any Adds that were queued while this species was busy swimming schools out.
            PumpQueue(species, spawner);
        }

        // Drops the top `count` schools at once: decrement N, destroy their (parked) targets, shrink the
        // spawner. No GPU rebuild here — the caller rebuilds once. Returns true if the count changed.
        // Because the exiting schools are the top-most sub-groups, removing them together leaves every
        // surviving school's index unchanged.
        private bool CommitRemoveSchools(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner, int count)
        {
            int n = _schoolCount.TryGetValue(species, out int current) ? current : 0;
            if (n <= 0 || count <= 0) return false;

            count = Mathf.Min(count, n);
            int fishPerSchool = FishPerSchool(species);
            int newN = n - count;
            _schoolCount[species] = newN;

            // Destroy the parked targets (the last `count` in the list) so targets and schools stay in
            // lockstep and no GameObjects leak.
            for (int i = 0; i < count; i++)
                DestroyLastTarget(species, spawner);

            // newN == 0 deactivates the spawner (excluded from the simulation entirely).
            spawner.SetSchoolConfiguration(newN, fishPerSchool);
            return true;
        }

        // The swim target for a specific sub-group (school) of a species, or null if out of range.
        private EcosystemTargetGPU GetTargetAt(SpeciesDataGPU species, int subGroupIndex)
        {
            if (!_managedTargets.TryGetValue(species, out List<SimulationAffecterComponent> targets))
                return null;
            if (subGroupIndex < 0 || subGroupIndex >= targets.Count)
                return null;
            return targets[subGroupIndex] as EcosystemTargetGPU;
        }

        // A random placed entry/exit marker, or (if none placed) a point just outside the bounds along
        // a random horizontal direction, so fish always have somewhere off-screen to swim out to.
        private Vector3 PickExitPoint()
        {
            Bounds b = SimulationBounds;

            // Use a placed exit marker only if it is actually OUTSIDE the volume. A marker mistakenly left
            // inside the bounds would keep the exiting fish on-screen forever and stall the (timer-less)
            // cull, so treat it as absent and fall back to a guaranteed-outside point.
            FishEntryPointGPU point = PickMarker(forEntry: false);
            if (point != null && !b.Contains(point.Position)) return point.Position;

            // Fallback: a point just outside the bounds along a random horizontal direction, so fish
            // always have somewhere off-screen to swim out to and reliably leave the volume.
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.right;
            Vector3 outward = new Vector3(dir.x, 0f, dir.y);
            return b.center + outward * (b.extents.magnitude + 5f);
        }

        // -------------------------------------------------------------------------
        // Runtime target lifecycle — one Target + Target Animator PAIR per school,
        // for ALL species, mirroring the original Boids_Demo setup exactly:
        //
        //   Boids_Spawner_<Species>
        //   ├── Targets
        //   │   └── Target (N)            EcosystemTargetGPU (the affecter fish follow)
        //   └── Target Animators
        //       └── Target Animator (N)   TransformAnimator (moves that target along its path)
        //
        // The animator is configured per school with a randomized shape / centre /
        // height / size / direction, its speed derived from the species' cruising
        // speed, and the whole path fitted inside the simulation bounds.
        // -------------------------------------------------------------------------

        private void CreateTarget(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner, int subGroupIndex)
        {
            EcosystemTargetGPU target = CreateTargetPair(species, spawner, subGroupIndex);
            spawner.AddTarget(target);

            if (!_managedTargets.TryGetValue(species, out List<SimulationAffecterComponent> targets))
            {
                targets = new List<SimulationAffecterComponent>();
                _managedTargets[species] = targets;
            }
            targets.Add(target);
        }

        private void DestroyLastTarget(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            if (!_managedTargets.TryGetValue(species, out List<SimulationAffecterComponent> targets) || targets.Count == 0)
                return;

            int lastIndex = targets.Count - 1;
            SimulationAffecterComponent target = targets[lastIndex];
            targets.RemoveAt(lastIndex);

            if (target != null)
            {
                spawner.RemoveTarget(target);

                // Destroy the paired Target Animator too — a leaked animator would keep running
                // with a destroyed TargetTransform and throw every frame.
                if (target is EcosystemTargetGPU ecosystemTarget && ecosystemTarget.Animator != null)
                {
                    Destroy(ecosystemTarget.Animator.gameObject);
                }
                Destroy(target.gameObject);
            }
        }

        // Creates the demo-style pair for one school: a Target (EcosystemTargetGPU, what the fish
        // follow) and a Target Animator (TransformAnimator, what moves it), both parented under the
        // species' spawner so the Hierarchy shows exactly what each school is doing.
        private EcosystemTargetGPU CreateTargetPair(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner, int subGroupIndex)
        {
            // --- Target (the simulation affecter the school follows) ---
            GameObject targetGO = new GameObject($"Target ({subGroupIndex})");
            targetGO.transform.SetParent(GetOrCreateContainer(spawner, "Targets"), false);

            EcosystemTargetGPU target = targetGO.AddComponent<EcosystemTargetGPU>();
            target.SetSubGroupID(subGroupIndex);
            target.SetAffecterType(SimulationAffecterType.Target);
            target.SetUpdatePositionEveryFrame(true);
            targetGO.transform.localScale = Vector3.one * _targetRadius;

            // --- Target Animator (moves the target along its path) ---
            GameObject animatorGO = new GameObject($"Target Animator ({subGroupIndex})");
            animatorGO.transform.SetParent(GetOrCreateContainer(spawner, "Target Animators"), false);

            TransformAnimator animator = animatorGO.AddComponent<TransformAnimator>();
            animator.TargetTransform = targetGO.transform;
            target.SetAnimator(animator);

            ConfigureAnimator(animator, species);

            // Place the target on its path right away so it never sits at the container origin for
            // a frame (a target outside the bounds would read as "exiting" to the compute shader).
            animator.SetupInitialTargetPosition();
            return target;
        }

        // Configures one school's Target Animator: shape from the species' PathStyle, randomized
        // centre / swim height / size / direction so no two schools trace the same route, speed
        // derived from the species' cruising speed so the school can always keep up, and the whole
        // path fitted inside the simulation bounds (minus a safe zone) so the target never leaves
        // the volume.
        private void ConfigureAnimator(TransformAnimator animator, SpeciesDataGPU species)
        {
            Bounds bounds = SimulationBounds;
            float  margin = _pathBoundsSafeZone;

            // Speed matched to the SPECIES — a target faster than the fish makes the school
            // perpetually max-rotate at it and jitter. Fallback for missing movement data.
            FishMovementProperties movement = species.MovementProperties;
            float cruisingSpeed = movement != null ? movement.CruisingSpeed : 0f;
            float speed = cruisingSpeed > 0f
                ? cruisingSpeed * Random.Range(_targetSpeedFractionRange.x, _targetSpeedFractionRange.y)
                : 2f;

            float sizeMultiplier = Random.Range(_pathSizeMultiplierRange.x, _pathSizeMultiplierRange.y);

            // Swim height comes from the SPECIES' preferred depth band rather than the whole water column,
            // so each species settles into its own slice of the reef (rays on the sand, scad up at the
            // pillar tops) instead of every school roaming the same mid-water layer.
            float bandBottomY, bandTopY;
            GetPreferredDepthRange(species, out bandBottomY, out bandTopY);

            // Clearance is vertical-only (see _pathVerticalClearance): a path is flat, so its height needs
            // no room for horizontal extent — only enough that the target is never AT the boundary, which
            // would read as "exiting" and drag the school off-screen. Clamped to at most half the bounds
            // height so a shallow simulation area can't invert the range.
            float clearance = Mathf.Min(_pathVerticalClearance, bounds.extents.y * 0.5f);
            float height    = Mathf.Clamp(
                Random.Range(bandBottomY, bandTopY),
                bounds.min.y + clearance,
                bounds.max.y - clearance);

            float anchorRangeX;
            float anchorRangeZ;
            float yaw;

            TransformAnimator.PositionAnimationShape shape = ResolvePathShape(species.PathStyle);
            switch (shape)
            {
                case TransformAnimator.PositionAnimationShape.Circle:
                {
                    float maxRadius = Mathf.Max(2f, Mathf.Min(bounds.extents.x, bounds.extents.z) - margin);
                    float radius    = Mathf.Clamp(maxRadius * sizeMultiplier, 2f, maxRadius);

                    // Respect what the fish can TURN: circling radius r at speed s demands s/r rad/s of
                    // sustained turning. GROW the circle until following it needs only half the species'
                    // max turn rate — never slow the target below cruising speed instead (fish can't swim
                    // slower than cruising, so a slower target makes the school mill around it and
                    // scatter). Only if even the biggest circle that fits is too tight, slow the target
                    // as a last resort.
                    if (movement != null && movement.MaxAngularVelocity > 0f)
                    {
                        float minRadiusForSpeed = speed / ((movement.MaxAngularVelocity * Mathf.Deg2Rad) * 0.5f);
                        radius = Mathf.Clamp(minRadiusForSpeed, radius, maxRadius);
                        if (radius < minRadiusForSpeed)
                        {
                            speed = radius * (movement.MaxAngularVelocity * Mathf.Deg2Rad) * 0.5f;
                        }
                    }
                    animator.CircleRadius = radius;

                    anchorRangeX = Mathf.Max(0f, bounds.extents.x - radius - margin);
                    anchorRangeZ = Mathf.Max(0f, bounds.extents.z - radius - margin);
                    yaw          = Random.Range(0f, 360f);
                    break;
                }
                case TransformAnimator.PositionAnimationShape.Rectangle:
                {
                    // Width and depth rolled independently for more variety. Yaw stays axis-aligned
                    // (0 or 180 like the demo) so the bounds-fit math below holds exactly.
                    float halfWidth  = Mathf.Clamp((bounds.extents.x - margin) * Random.Range(_pathSizeMultiplierRange.x, _pathSizeMultiplierRange.y), 2f, Mathf.Max(2f, bounds.extents.x - margin));
                    float halfLength = Mathf.Clamp((bounds.extents.z - margin) * Random.Range(_pathSizeMultiplierRange.x, _pathSizeMultiplierRange.y), 2f, Mathf.Max(2f, bounds.extents.z - margin));
                    animator.RectangleWidth  = halfWidth * 2f;
                    animator.RectangleLength = halfLength * 2f;

                    anchorRangeX = Mathf.Max(0f, bounds.extents.x - halfWidth - margin);
                    anchorRangeZ = Mathf.Max(0f, bounds.extents.z - halfLength - margin);
                    yaw          = Random.value < 0.5f ? 0f : 180f;
                    break;
                }
                default: // Line
                {
                    float maxHalfLength = Mathf.Max(2f, Mathf.Min(bounds.extents.x, bounds.extents.z) - margin);
                    float halfLength    = Mathf.Clamp(maxHalfLength * sizeMultiplier, 2f, maxHalfLength);
                    animator.LineLength = halfLength * 2f;

                    // The line can face any way, so reserve its half-length on BOTH axes.
                    anchorRangeX = Mathf.Max(0f, bounds.extents.x - halfLength - margin);
                    anchorRangeZ = Mathf.Max(0f, bounds.extents.z - halfLength - margin);
                    yaw          = Random.Range(0f, 180f);
                    break;
                }
            }

            animator.AnimationShape       = shape;
            animator.MovementSpeed        = speed;
            animator.MovingClockwise      = Random.value < 0.5f;
            // Scale the arrival threshold with speed so a fast target can't overshoot its next
            // waypoint every frame and orbit it.
            animator.EdgeReachingDistance = Mathf.Max(0.5f, speed * 0.05f);

            animator.transform.position = new Vector3(
                bounds.center.x + Random.Range(-anchorRangeX, anchorRangeX),
                height,
                bounds.center.z + Random.Range(-anchorRangeZ, anchorRangeZ));
            animator.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
        }

        // Finds or creates a named container child under a species' spawner, mirroring the
        // Targets / Target Animators grouping of the original Boids_Demo hierarchy.
        private static Transform GetOrCreateContainer(BoidSpawnerGPUMultiTargets spawner, string name)
        {
            Transform container = spawner.transform.Find(name);
            if (container == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(spawner.transform, false);
                container = go.transform;
            }
            return container;
        }

        // Maps a species' authored path style to a concrete TransformAnimator shape for one school.
        private static TransformAnimator.PositionAnimationShape ResolvePathShape(SpeciesPathStyleGPU style)
        {
            switch (style)
            {
                case SpeciesPathStyleGPU.Circle:    return TransformAnimator.PositionAnimationShape.Circle;
                case SpeciesPathStyleGPU.Rectangle: return TransformAnimator.PositionAnimationShape.Rectangle;
                case SpeciesPathStyleGPU.Line:      return TransformAnimator.PositionAnimationShape.Line;
                default: // RandomShape — roll one of the three per school so even one species varies
                    int roll = Random.Range(0, 3);
                    return roll == 0 ? TransformAnimator.PositionAnimationShape.Circle
                         : roll == 1 ? TransformAnimator.PositionAnimationShape.Rectangle
                                     : TransformAnimator.PositionAnimationShape.Line;
            }
        }

        // -------------------------------------------------------------------------
        // Off-screen entry points — fish swim in from a marker placed outside the bounds.
        // -------------------------------------------------------------------------

        // Picks an entry FishEntryPointGPU and tells the spawner to spawn its next school there, facing the
        // simulation center. To stop schools added in quick succession (spam-Add) piling up on one point,
        // it (a) avoids reusing the previous marker when more than one exists and (b) jitters the origin
        // sideways, keeping clear of recently used origins. If no entry points exist, the spawner keeps its
        // default in-bounds spawn behaviour (nothing breaks in scenes without markers).
        private void ApplyEntrySpawnOrigin(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            FishEntryPointGPU point = PickEntryMarker(species);
            if (point == null) return; // no entry-capable point placed — keep the default in-bounds spawn

            Vector3 origin = ChooseSpreadOrigin(point);
            Vector3 inward = SimulationBounds.center - origin;
            spawner.SetPendingSpawnOrigin(origin, inward);

            _recentEntryOrigins.Add(origin);
            if (_recentEntryOrigins.Count > RecentEntryOriginMemory) _recentEntryOrigins.RemoveAt(0);
            _lastEntryMarker = point;
        }

        // Picks an entry-capable marker for this species, preferring gates that sit at the species' own swim
        // depth so a school enters at the height it is going to live at — a ray slides in along the sand
        // instead of dropping in from mid-water and sinking, scad arrive up where the pillars are. Among
        // gates that are equally good depth-wise it still avoids the previous spawn's marker, so consecutive
        // schools fan out instead of clumping on one point.
        private FishEntryPointGPU PickEntryMarker(SpeciesDataGPU species)
        {
            IReadOnlyList<FishEntryPointGPU> all = FishEntryPointGPU.All;
            if (all == null || all.Count == 0) return null;

            _markerScratch.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                FishEntryPointGPU m = all[i];
                if (m != null && m.CanEntry) _markerScratch.Add(m);
            }
            if (_markerScratch.Count == 0) return null;
            if (_markerScratch.Count == 1) return _markerScratch[0];

            // Narrow to the gates closest to this species' depth band. Scored by how far each marker sits
            // OUTSIDE the band (0 = already at the right depth), keeping everything within
            // _entryMarkerDepthTolerance of the best score so several good gates still share the traffic.
            // Skipped when the species has no data, leaving the old depth-agnostic behaviour intact.
            if (species != null)
            {
                float bandBottomY, bandTopY;
                GetPreferredDepthRange(species, out bandBottomY, out bandTopY);

                float bestMismatch = float.PositiveInfinity;
                for (int i = 0; i < _markerScratch.Count; i++)
                    bestMismatch = Mathf.Min(bestMismatch, DepthMismatch(_markerScratch[i], bandBottomY, bandTopY));

                float cutoff = bestMismatch + _entryMarkerDepthTolerance;
                for (int i = _markerScratch.Count - 1; i >= 0; i--)
                    if (DepthMismatch(_markerScratch[i], bandBottomY, bandTopY) > cutoff) _markerScratch.RemoveAt(i);

                if (_markerScratch.Count == 1) return _markerScratch[0];
            }

            int idx = Random.Range(0, _markerScratch.Count);
            if (_markerScratch[idx] == _lastEntryMarker) idx = (idx + 1) % _markerScratch.Count;
            return _markerScratch[idx];
        }

        // How far a marker's height falls OUTSIDE [bandBottomY, bandTopY]; 0 when it is already in the band.
        private static float DepthMismatch(FishEntryPointGPU marker, float bandBottomY, float bandTopY)
        {
            float y = marker.Position.y;
            if (y < bandBottomY) return bandBottomY - y;
            if (y > bandTopY)    return y - bandTopY;
            return 0f;
        }

        // Resolves a species' PreferredDepthBand (a fraction of the simulation height) into world-space Y
        // limits. Single source of truth for the band, shared by the roaming-path height and the entry-gate
        // choice, so a school enters at the depth it will then roam at. Min/Max rather than .x/.y directly:
        // an inverted band authored in the Inspector should read as the range it looks like.
        private void GetPreferredDepthRange(SpeciesDataGPU species, out float bandBottomY, out float bandTopY)
        {
            Bounds b = SimulationBounds;
            float bandLow  = Mathf.Clamp01(Mathf.Min(species.PreferredDepthBand.x, species.PreferredDepthBand.y));
            float bandHigh = Mathf.Clamp01(Mathf.Max(species.PreferredDepthBand.x, species.PreferredDepthBand.y));
            bandBottomY = Mathf.Lerp(b.min.y, b.max.y, bandLow);
            bandTopY    = Mathf.Lerp(b.min.y, b.max.y, bandHigh);
        }

        // Nudges the spawn origin off the marker — sideways (perpendicular to the swim-in direction, so it
        // stays off-screen at the same depth) and away from recently used origins. Tries a handful of
        // jittered candidates and takes the first that clears _entrySpawnMinSeparation, else the farthest.
        private Vector3 ChooseSpreadOrigin(FishEntryPointGPU marker)
        {
            Vector3 basePos = marker.Position;
            if (_entrySpawnJitterRadius <= 0f) return basePos;

            Vector3 inward = (SimulationBounds.center - basePos).normalized;
            if (inward.sqrMagnitude < 1e-6f) inward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, inward);
            if (right.sqrMagnitude < 1e-6f) right = Vector3.Cross(Vector3.forward, inward);
            right.Normalize();
            Vector3 up = Vector3.Cross(inward, right).normalized;

            Vector3 best = basePos;
            float bestMinDist = -1f;
            const int tries = 6;
            for (int t = 0; t < tries; t++)
            {
                Vector2 disk = Random.insideUnitCircle * _entrySpawnJitterRadius;
                Vector3 candidate = basePos + right * disk.x + up * disk.y;

                float minDist = NearestRecentOriginDistance(candidate);
                if (minDist >= _entrySpawnMinSeparation) return candidate; // clears the gap — good enough
                if (minDist > bestMinDist) { bestMinDist = minDist; best = candidate; }
            }
            return best; // nothing cleared the gap — use the most-separated attempt
        }

        // Distance from p to the closest recently used spawn origin (+Infinity when there are none yet).
        private float NearestRecentOriginDistance(Vector3 p)
        {
            float min = float.PositiveInfinity;
            for (int i = 0; i < _recentEntryOrigins.Count; i++)
            {
                float d = Vector3.Distance(p, _recentEntryOrigins[i]);
                if (d < min) min = d;
            }
            return min;
        }

        // Picks a random enabled marker usable for entry (or exit). Returns null if none match.
        private FishEntryPointGPU PickMarker(bool forEntry)
        {
            IReadOnlyList<FishEntryPointGPU> all = FishEntryPointGPU.All;
            if (all == null || all.Count == 0) return null;

            _markerScratch.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                FishEntryPointGPU m = all[i];
                if (m == null) continue;
                if (forEntry ? m.CanEntry : m.CanExit) _markerScratch.Add(m);
            }
            if (_markerScratch.Count == 0) return null;
            return _markerScratch[Random.Range(0, _markerScratch.Count)];
        }

        // Entry points are meant to sit OUTSIDE the bounds (off-screen). One placed inside would make its
        // fish spawn already in view and not swim in — warn so it is easy to catch in the editor.
        private void WarnAboutMisplacedEntryPoints()
        {
            IReadOnlyList<FishEntryPointGPU> points = FishEntryPointGPU.All;
            if (points == null) return;
            Bounds bounds = SimulationBounds;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && bounds.Contains(points[i].Position))
                {
                    Debug.LogWarning($"[EcosystemSimulationGPU] Entry point '{points[i].name}' is INSIDE the " +
                                     "simulation bounds — its fish will spawn on-screen instead of swimming in. " +
                                     "Move it outside the bounds volume.", points[i]);
                }
            }
        }

        // -------------------------------------------------------------------------
        // Setup helpers
        // -------------------------------------------------------------------------

        private void SetupAllSpecies()
        {
            for (int i = 0; i < _ecosystem.Species.Count; i++)
            {
                SpeciesDataGPU species = _ecosystem.Species[i];
                if (species == null) continue;

                BoidSpawnerGPUMultiTargets spawner = FindSpawner(species);
                if (spawner == null)
                {
                    Debug.LogWarning($"[EcosystemSimulationGPU] '{species.SpeciesName}' has no matching spawner in the simulation — skipping.");
                    continue;
                }

                species.RuntimeId = i;

                _schoolCount[species]    = 0;
                _managedTargets[species] = new List<SimulationAffecterComponent>();

                // Start every species at zero schools: clear any inspector-assigned targets and mark the
                // spawner inactive so it is excluded from the GPU simulation until the player adds a school.
                spawner.ClearTargets();
                spawner.SetSchoolConfiguration(0, FishPerSchool(species));
            }
        }

        private BoidSpawnerGPUMultiTargets FindSpawner(SpeciesDataGPU species)
        {
            if (species == null) return null;
            BoidSpawnerBase[] spawners = _simulation.GetBoidSpawners();
            if (spawners == null) return null;
            foreach (BoidSpawnerBase s in spawners)
            {
                if (s is BoidSpawnerGPUMultiTargets mt && mt.SpeciesData == species)
                    return mt;
            }
            return null;
        }

        private bool ValidateSpecies(SpeciesDataGPU species, out BoidSpawnerGPUMultiTargets spawner)
        {
            spawner = null;
            if (species == null)
            {
                Debug.LogWarning("[EcosystemSimulationGPU] Null species passed.");
                return false;
            }
            spawner = FindSpawner(species);
            if (spawner == null)
            {
                Debug.LogWarning($"[EcosystemSimulationGPU] No spawner in the simulation has SpeciesData '{species.SpeciesName}' assigned.");
                return false;
            }
            return true;
        }

        // -------------------------------------------------------------------------
        // Editor visualisation
        // -------------------------------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            // Draw the volume actually used (from BoidSimulationGPU when available), so this box always
            // overlaps the BoidSimulationGPU's own bounds gizmo instead of drifting from it.
            if (_simulation == null && _ecosystem == null) return;
            Bounds bounds = SimulationBounds;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.12f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
