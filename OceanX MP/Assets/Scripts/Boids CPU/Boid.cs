using System.Collections.Generic;
using UnityEngine;

// One fish in the CPU boid simulation.
// BoidSimulation / EcosystemSimulation calls UpdateBoid every frame; this script owns no Update().
public class Boid : MonoBehaviour
{
    [Header("Debug (read-only)")]
    [SerializeField] private BoidInfo _boidInfo;
    [SerializeField] private int      _groupId = -1;

    // --- cached refs set on Initialize ---
    private BoidSchoolProperties     _schoolProps;
    private BoidMovementProperties   _moveProps;
    private SpeciesBehaviorProperties _behaviorProps;  // null for species with no AI rules
    private SpeciesDefinition         _speciesDef;     // null when using legacy Initialize()
    private Transform                 _cachedTransform;

    // Pre-cached int arrays so we don't allocate in the hot path
    private int[] _preyIds;
    private int[] _predatorIds;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public int     GroupId           => _groupId;
    public Vector3 Position          => _boidInfo.Position;
    public Vector3 MovementDirection => _boidInfo.MovementDirection;
    public bool    IsAlive           => _boidInfo.BehaviorState != BoidBehaviorState.Dead;

    public Transform CachedTransform
    {
        get
        {
            if (_cachedTransform == null) _cachedTransform = transform;
            return _cachedTransform;
        }
    }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    // Legacy path — used by the single-species BoidSimulation.
    public void Initialize(BoidSchoolProperties schoolProps, int groupId)
    {
        _schoolProps   = schoolProps;
        _moveProps     = schoolProps.MovementProperties;
        _behaviorProps = null;
        _speciesDef    = null;
        _groupId       = groupId;
        _preyIds       = new int[0];
        _predatorIds   = new int[0];
        _boidInfo      = BoidSwimmingUtility.InitializeBoid(CachedTransform, _moveProps);
    }

    // Ecosystem path — used by EcosystemSimulation.
    // runtimeId must already be assigned on speciesDef before calling this.
    public void InitializeAsSpecies(SpeciesDefinition speciesDef)
    {
        _speciesDef    = speciesDef;
        _schoolProps   = speciesDef.SchoolProperties;
        _moveProps     = speciesDef.SchoolProperties.MovementProperties;
        _behaviorProps = speciesDef.BehaviorProperties;
        _groupId       = speciesDef.RuntimeId;

        // Cache prey and predator IDs so the hot path never walks the lists.
        _preyIds     = BuildIdCache(speciesDef.PreySpecies);
        _predatorIds = BuildIdCache(speciesDef.PredatorSpecies);

        _boidInfo = BoidSwimmingUtility.InitializeBoid(CachedTransform, _moveProps);
    }

    // -------------------------------------------------------------------------
    // Kill / death
    // -------------------------------------------------------------------------

    // Called by a predator when this boid is within AttackRange.
    // Deactivates the GameObject; EcosystemSimulation removes it next cleanup pass.
    public bool TryKill()
    {
        if (!IsAlive) return false;
        _boidInfo.BehaviorState = BoidBehaviorState.Dead;
        gameObject.SetActive(false);
        return true;
    }

    // -------------------------------------------------------------------------
    // Per-frame update — called by the simulation manager
    // -------------------------------------------------------------------------

    public void UpdateBoid(List<Boid> nearbyBoids, float timeDelta,
        Bounds simulationBounds, BoidAffecter[] affecters)
    {
        Vector3 currentPos = _boidInfo.Position;

        // ------------------------------------------------------------------
        // 1. Grow hunger (predators only — prey species have HungerRate = 0)
        // ------------------------------------------------------------------
        if (_behaviorProps != null)
            _boidInfo.Hunger = Mathf.Clamp01(_boidInfo.Hunger + _behaviorProps.HungerRate * timeDelta);

        // ------------------------------------------------------------------
        // 2. Scan nearby boids — one pass collects flocking data AND
        //    the closest visible predator / prey simultaneously.
        // ------------------------------------------------------------------
        Boid  closestPredator     = null;
        Boid  closestPrey         = null;
        float closestPredatorDist = float.MaxValue;
        float closestPreyDist     = float.MaxValue;

        // Flocking accumulators (same-species only)
        Vector3 separation    = Vector3.zero;
        Vector3 alignmentSum  = _boidInfo.MovementDirection;
        Vector3 cohesionSum   = currentPos;
        int     flockCount    = 0;
        int     sepCount      = 0;

        float visionSq = _schoolProps.VisionRange * _schoolProps.VisionRange;
        float sepSq    = _schoolProps.SeparationRange * _schoolProps.SeparationRange;
        float invSepSq = sepSq > 0f ? 1f / sepSq : 0f;

        for (int i = 0; i < nearbyBoids.Count; i++)
        {
            Boid other = nearbyBoids[i];
            if (other == this || !other.IsAlive) continue;

            float distSq = (other.Position - currentPos).sqrMagnitude;

            // --- Predator / prey checks (ecosystem only) ---
            if (_behaviorProps != null)
            {
                float dist = Mathf.Sqrt(distSq);

                if (IsPredatorSpecies(other.GroupId) && dist < closestPredatorDist)
                {
                    closestPredatorDist = dist;
                    closestPredator     = other;
                }
                if (IsPreySpecies(other.GroupId) && dist < closestPreyDist)
                {
                    closestPreyDist = dist;
                    closestPrey     = other;
                }
            }

            // --- Flocking (same species only) ---
            if (other.GroupId != _groupId) continue;
            if (distSq > visionSq) continue;

            if (distSq < sepSq)
            {
                Vector3 away = currentPos - other.Position;
                separation  += away * Mathf.Clamp01(1f - distSq * invSepSq);
                sepCount++;
            }

            alignmentSum += other.MovementDirection;
            cohesionSum  += other.Position;
            flockCount++;
        }

        // ------------------------------------------------------------------
        // 3. Update behavior state
        // ------------------------------------------------------------------
        UpdateBehaviorState(closestPredator, closestPredatorDist,
                            closestPrey,     closestPreyDist,
                            timeDelta);

        // ------------------------------------------------------------------
        // 4. Obstacle avoidance (highest priority)
        // ------------------------------------------------------------------
        BoidAffecter obstacle    = GetClosestAffecter(currentPos, affecters, BoidAffecterType.Obstacle);
        bool         avoidingObstacle = false;
        Vector3      obstacleEscape   = Vector3.zero;

        if (obstacle != null)
        {
            float dist = (currentPos - obstacle.Position).magnitude;
            if (dist <= obstacle.Radius)
            {
                avoidingObstacle = true;
                obstacleEscape   = (currentPos - obstacle.Position).normalized;
            }
        }

        // ------------------------------------------------------------------
        // 5. Choose desired direction based on current behavior state
        // ------------------------------------------------------------------
        Vector3 desiredDir;
        bool    shouldAccelerate;

        if (avoidingObstacle)
        {
            desiredDir       = obstacleEscape;
            shouldAccelerate = true;
        }
        else if (_boidInfo.BehaviorState == BoidBehaviorState.Fleeing)
        {
            // Actively steer away from the predator; if it's out of sight just
            // keep heading in the current direction (panic momentum).
            desiredDir = closestPredator != null
                ? (currentPos - closestPredator.Position).normalized
                : _boidInfo.MovementDirection;
            shouldAccelerate = true;
        }
        else if (_boidInfo.BehaviorState == BoidBehaviorState.Hunting)
        {
            if (closestPrey != null)
            {
                // Kill if within attack range.
                if (closestPreyDist <= _behaviorProps.AttackRange)
                {
                    closestPrey.TryKill();
                    _boidInfo.Hunger = _behaviorProps.HungerAfterKill;
                    closestPrey      = null;
                }
            }

            desiredDir = closestPrey != null
                ? (closestPrey.Position - currentPos).normalized
                : _boidInfo.MovementDirection;
            shouldAccelerate = true;
        }
        else // Schooling
        {
            desiredDir       = CalculateFlockingDirection(currentPos, flockCount, sepCount,
                                   separation, alignmentSum, cohesionSum, affecters);
            shouldAccelerate = false;
        }

        // ------------------------------------------------------------------
        // 6. Bounds — steer toward center if about to leave simulation area
        // ------------------------------------------------------------------
        if (!avoidingObstacle &&
            BoidSwimmingUtility.WillLeaveSimulationArea(currentPos, desiredDir, _boidInfo.Speed, simulationBounds))
        {
            desiredDir = (simulationBounds.center - currentPos).normalized;
        }

        // ------------------------------------------------------------------
        // 7. Physics integration
        // ------------------------------------------------------------------
        BoidSwimmingUtility.UpdateMovementDirection(desiredDir, timeDelta, _moveProps, ref _boidInfo);
        BoidSwimmingUtility.UpdateMovementSpeed(shouldAccelerate, timeDelta, _moveProps, ref _boidInfo);
        BoidSwimmingUtility.UpdatePositionAndRotation(timeDelta, CachedTransform, ref _boidInfo);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void UpdateBehaviorState(Boid closestPredator, float predatorDist,
                                     Boid closestPrey,     float preyDist,
                                     float timeDelta)
    {
        if (_behaviorProps == null)
        {
            _boidInfo.BehaviorState = BoidBehaviorState.Schooling;
            return;
        }

        // Predator in flee range → immediate panic
        if (closestPredator != null && predatorDist <= _behaviorProps.FleeRange)
        {
            _boidInfo.BehaviorState = BoidBehaviorState.Fleeing;
            _boidInfo.PanicTimer    = _behaviorProps.PanicDuration;
            return;
        }

        // Panic timer still running even though predator left vision
        if (_boidInfo.PanicTimer > 0f)
        {
            _boidInfo.PanicTimer   -= timeDelta;
            _boidInfo.BehaviorState = BoidBehaviorState.Fleeing;
            return;
        }

        // Predator species that is hungry and can see prey
        if (_speciesDef != null &&
            _speciesDef.Role == SpeciesRole.Predator &&
            _boidInfo.Hunger >= _behaviorProps.HuntThreshold &&
            closestPrey != null &&
            preyDist <= _behaviorProps.DetectionRange)
        {
            _boidInfo.BehaviorState = BoidBehaviorState.Hunting;
            return;
        }

        _boidInfo.BehaviorState = BoidBehaviorState.Schooling;
    }

    // Computes the final schooling direction from pre-accumulated sums —
    // avoids iterating nearbyBoids a second time.
    private Vector3 CalculateFlockingDirection(Vector3 currentPos,
        int flockCount, int sepCount,
        Vector3 separation, Vector3 alignmentSum, Vector3 cohesionSum,
        BoidAffecter[] affecters)
    {
        Vector3 currentDir = _boidInfo.MovementDirection;

        float inv = flockCount == 0 ? 1f : 1f / flockCount;
        Vector3 avgAlignment = alignmentSum * inv;
        Vector3 avgCohesion  = cohesionSum  * inv;

        if (sepCount > 0) separation /= sepCount;

        Vector3 cohesionDir   = flockCount == 0
            ? currentDir
            : (avgCohesion - currentPos).normalized;
        Vector3 separationDir = sepCount > 0
            ? separation.normalized
            : currentDir;

        // Target attraction from affecters
        BoidAffecter target = GetClosestAffecter(currentPos, affecters, BoidAffecterType.Target);
        Vector3 targetDir   = currentDir;
        float   targetWeight = 0f;

        if (target != null)
        {
            Vector3 toTarget = target.Position - currentPos;
            if (toTarget.magnitude > target.Radius)
            {
                targetDir    = toTarget.normalized;
                targetWeight = _schoolProps.TargetWeight;
            }
        }

        Vector3 desired =
            cohesionDir   * _schoolProps.CohesionWeight   +
            separationDir * _schoolProps.SeparationWeight  +
            avgAlignment  * _schoolProps.AlignmentWeight   +
            targetDir     * targetWeight;

        return desired == Vector3.zero ? currentDir : desired.normalized;
    }

    private BoidAffecter GetClosestAffecter(Vector3 pos,
        BoidAffecter[] affecters, BoidAffecterType type)
    {
        if (affecters == null || affecters.Length == 0) return null;

        BoidAffecter closest  = null;
        float        closestD = float.MaxValue;

        foreach (BoidAffecter a in affecters)
        {
            if (a == null || a.Type != type || !a.AffectsGroup(_groupId)) continue;
            float d = Mathf.Max(0f, (pos - a.Position).magnitude - a.Radius);
            if (d < closestD) { closestD = d; closest = a; }
        }

        return closest;
    }

    private bool IsPredatorSpecies(int otherGroupId)
    {
        for (int i = 0; i < _predatorIds.Length; i++)
            if (_predatorIds[i] == otherGroupId) return true;
        return false;
    }

    private bool IsPreySpecies(int otherGroupId)
    {
        for (int i = 0; i < _preyIds.Length; i++)
            if (_preyIds[i] == otherGroupId) return true;
        return false;
    }

    private static int[] BuildIdCache(List<SpeciesDefinition> species)
    {
        if (species == null || species.Count == 0) return new int[0];
        int[] ids = new int[species.Count];
        for (int i = 0; i < species.Count; i++)
            ids[i] = species[i] != null ? species[i].RuntimeId : -1;
        return ids;
    }
}
