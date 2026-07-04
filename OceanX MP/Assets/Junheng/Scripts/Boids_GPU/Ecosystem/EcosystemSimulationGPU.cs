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
        [Tooltip("How much food-web balance (fraction of species within the ratio band) contributes.")]
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
        /// 0 means the species is extinct / not yet added.
        /// </summary>
        public int CountGroups(SpeciesDataGPU species)
        {
            if (species == null) return 0;
            return _schoolCount.TryGetValue(species, out int n) ? n : 0;
        }

        /// <summary>Static per-species cap on the number of schools. 0 if the species is null.</summary>
        public int GetMaxSchools(SpeciesDataGPU species)
        {
            if (species == null) return 0;
            return MaxSchoolsOf(species);
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
            int totalSpecies = 0;
            int aliveSpecies = 0;
            int considered = 0;   // alive species that participate in the food web
            int balanced = 0;     // ...of those, how many sit inside the ratio band
            bool apexAlive = false;

            foreach (SpeciesDataGPU species in _ecosystem.Species)
            {
                if (species == null) continue;
                totalSpecies++;

                int n = CountGroups(species);
                if (n <= 0) continue;
                aliveSpecies++;

                if (species.Role == SpeciesRoleGPU.Apex) apexAlive = true;

                if (HasAny(species.PredatorSpecies) || HasAny(species.PreySpecies))
                {
                    considered++;
                    if (PopulationPressure(species, _schoolCount) == 0) balanced++;
                }
            }

            if (totalSpecies == 0) return 0f;

            float diversity = (float)aliveSpecies / totalSpecies;
            float balance   = considered > 0 ? (float)balanced / considered : 0f;
            float apex      = apexAlive ? 1f : 0f;

            float weightSum = Mathf.Max(0.0001f, _healthDiversityWeight + _healthBalanceWeight + _healthApexWeight);
            float health = (_healthDiversityWeight * diversity
                          + _healthBalanceWeight   * balance
                          + _healthApexWeight      * apex) / weightSum;
            return Mathf.Clamp01(health);
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
            ApplyEntrySpawnOrigin(spawner);
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
            WanderingAffecterGPU exitingTarget = GetWanderingTargetAt(species, settled - 1);
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
                    WanderingAffecterGPU target = GetWanderingTargetAt(species, j);
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

        // The roaming target for a specific sub-group (school) of a species, or null if out of range.
        private WanderingAffecterGPU GetWanderingTargetAt(SpeciesDataGPU species, int subGroupIndex)
        {
            if (!_managedTargets.TryGetValue(species, out List<SimulationAffecterComponent> targets))
                return null;
            if (subGroupIndex < 0 || subGroupIndex >= targets.Count)
                return null;
            return targets[subGroupIndex] as WanderingAffecterGPU;
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
        // Runtime target lifecycle — one animated roaming target per school, for ALL
        // species. This unifies the previously apex-only WanderingAffecterGPU path so
        // every role behaves the same: groups, targets, animators and N move together.
        // -------------------------------------------------------------------------

        private void CreateTarget(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner, int subGroupIndex)
        {
            WanderingAffecterGPU wanderer = CreateAndInitWanderer(species, subGroupIndex);
            spawner.AddTarget(wanderer);

            if (!_managedTargets.TryGetValue(species, out List<SimulationAffecterComponent> targets))
            {
                targets = new List<SimulationAffecterComponent>();
                _managedTargets[species] = targets;
            }
            targets.Add(wanderer);
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
                Destroy(target.gameObject);
            }
        }

        // Creates a WanderingAffecterGPU on a new child GameObject and configures its sub-group ID.
        private WanderingAffecterGPU CreateAndInitWanderer(SpeciesDataGPU species, int subGroupIndex)
        {
            GameObject go = new GameObject($"{species.SpeciesName}_Target_SubGroup{subGroupIndex}");
            go.transform.SetParent(transform);

            WanderingAffecterGPU wanderer = go.AddComponent<WanderingAffecterGPU>();
            wanderer.SetSubGroupID(subGroupIndex);
            wanderer.SetAffecterType(SimulationAffecterType.Target);
            wanderer.Initialize(SimulationBounds);
            return wanderer;
        }

        // -------------------------------------------------------------------------
        // Off-screen entry points — fish swim in from a marker placed outside the bounds.
        // -------------------------------------------------------------------------

        // Picks a random placed FishEntryPointGPU and tells the spawner to spawn its next school there,
        // facing the simulation center. If no entry points exist, the spawner keeps its default
        // in-bounds spawn behaviour (nothing breaks in scenes without markers).
        private void ApplyEntrySpawnOrigin(BoidSpawnerGPUMultiTargets spawner)
        {
            FishEntryPointGPU point = PickMarker(forEntry: true);
            if (point == null) return; // no entry-capable point placed — keep the default in-bounds spawn

            Vector3 origin = point.Position;
            Vector3 inward = SimulationBounds.center - origin;
            spawner.SetPendingSpawnOrigin(origin, inward);
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
