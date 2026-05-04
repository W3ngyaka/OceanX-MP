using System.Collections.Generic;
using UnityEngine;

// One fish in the CPU boid simulation.
// BoidSimulation calls UpdateBoid every frame; this script owns no Update().
public class Boid : MonoBehaviour
{
    [Header("Debug (read-only)")]
    [SerializeField] private BoidInfo _boidInfo;
    [SerializeField] private int      _groupId = -1;

    private BoidSchoolProperties   _schoolProps;
    private BoidMovementProperties _moveProps;
    private Transform              _cachedTransform;

    public int     GroupId           => _groupId;
    public Vector3 Position          => _boidInfo.Position;
    public Vector3 MovementDirection => _boidInfo.MovementDirection;

    public Transform CachedTransform
    {
        get
        {
            if (_cachedTransform == null) _cachedTransform = transform;
            return _cachedTransform;
        }
    }

    public void Initialize(BoidSchoolProperties schoolProps, int groupId)
    {
        _schoolProps = schoolProps;
        _moveProps   = schoolProps.MovementProperties;
        _groupId     = groupId;
        _boidInfo    = BoidSwimmingUtility.InitializeBoid(CachedTransform, _moveProps);
    }

    // Called each frame by BoidSimulation with the list of nearby boids and scene affecters.
    public void UpdateBoid(List<Boid> nearbyBoids, float timeDelta,
        Bounds simulationBounds, BoidAffecter[] affecters)
    {
        Vector3 currentPos = _boidInfo.Position;

        // --- obstacle check (highest priority) ---
        BoidAffecter obstacle = GetClosestAffecter(currentPos, affecters, BoidAffecterType.Obstacle);
        bool avoidingObstacle = false;
        Vector3 obstacleEscape = Vector3.zero;

        if (obstacle != null)
        {
            float dist = (currentPos - obstacle.Position).magnitude;
            if (dist <= obstacle.Radius)
            {
                avoidingObstacle = true;
                obstacleEscape   = (currentPos - obstacle.Position).normalized;
            }
        }

        // --- desired direction ---
        Vector3 desiredDir;
        if (avoidingObstacle)
        {
            desiredDir = obstacleEscape;
        }
        else
        {
            desiredDir = CalculateFlockingDirection(currentPos, nearbyBoids, affecters);
        }

        // --- bounds check overrides flocking (but not obstacle escape) ---
        if (!avoidingObstacle &&
            BoidSwimmingUtility.WillLeaveSimulationArea(currentPos, desiredDir, _boidInfo.Speed, simulationBounds))
        {
            desiredDir = (simulationBounds.center - currentPos).normalized;
        }

        // --- physics update ---
        BoidSwimmingUtility.UpdateMovementDirection(desiredDir, timeDelta, _moveProps, ref _boidInfo);
        BoidSwimmingUtility.UpdateMovementSpeed(avoidingObstacle, timeDelta, _moveProps, ref _boidInfo);
        BoidSwimmingUtility.UpdatePositionAndRotation(timeDelta, CachedTransform, ref _boidInfo);
    }

    private Vector3 CalculateFlockingDirection(Vector3 currentPos,
        List<Boid> nearbyBoids, BoidAffecter[] affecters)
    {
        Vector3 currentDir = _boidInfo.MovementDirection;

        Vector3 separation       = Vector3.zero;
        Vector3 alignmentDir     = currentDir;
        Vector3 cohesionPos      = currentPos;
        int     flockNeighbors   = 0;
        int     separationNeighbors = 0;

        float visionSq      = _schoolProps.VisionRange * _schoolProps.VisionRange;
        float sepSq         = _schoolProps.SeparationRange * _schoolProps.SeparationRange;
        float invSepSq      = 1f / sepSq;

        for (int i = 0; i < nearbyBoids.Count; i++)
        {
            Boid other = nearbyBoids[i];
            if (other == this) continue;

            // Smaller species stay out of our way; we ignore them.
            if (other.GroupId < _groupId) continue;

            float distSq = (other.Position - currentPos).sqrMagnitude;
            if (distSq > visionSq) continue;

            // Separation applies to same and larger species.
            if (distSq < sepSq)
            {
                separation += SeparationVector(currentPos, other.Position, invSepSq);
                separationNeighbors++;
            }

            // Alignment and cohesion only within the same species.
            if (other.GroupId != _groupId) continue;

            alignmentDir += other.MovementDirection;
            cohesionPos  += other.Position;
            flockNeighbors++;
        }

        // Average alignment and cohesion over neighbors.
        float inv = flockNeighbors == 0 ? 1f : 1f / flockNeighbors;
        alignmentDir *= inv;
        cohesionPos  *= inv;

        if (separationNeighbors > 0)
            separation /= separationNeighbors;

        Vector3 cohesionDir    = flockNeighbors == 0
            ? currentDir
            : (cohesionPos - currentPos).normalized;
        Vector3 separationDir  = separationNeighbors > 0
            ? separation.normalized
            : currentDir;

        // Target attraction.
        BoidAffecter target    = GetClosestAffecter(currentPos, affecters, BoidAffecterType.Target);
        Vector3 targetDir      = currentDir;
        float   targetWeight   = 0f;
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
            cohesionDir   * _schoolProps.CohesionWeight +
            separationDir * _schoolProps.SeparationWeight +
            alignmentDir  * _schoolProps.AlignmentWeight +
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
            if (d < closestD)
            {
                closestD = d;
                closest  = a;
            }
        }

        return closest;
    }

    private static Vector3 SeparationVector(Vector3 self, Vector3 neighbor, float invSepSq)
    {
        Vector3 away   = self - neighbor;
        float   distSq = away.sqrMagnitude;
        return away * Mathf.Clamp01(1f - distSq * invSepSq);
    }
}
