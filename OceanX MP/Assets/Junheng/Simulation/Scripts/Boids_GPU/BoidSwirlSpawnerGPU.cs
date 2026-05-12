using UnityEngine;
using static OceanX.BoidSpawnUtility;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Special type of boid spawner that initializes boids in a spherical pattern around a point
    /// and orientates them all in clockwise direction. This type of spawner is implemented to
    /// quickly initialize boids for the swirling motion.
    /// </summary>
    public class BoidSwirlSpawnerGPU : BoidSpawnerGPU
    {
        [Header("Swirl Properties: ")]
        [SerializeField] private float _initialSwirlSphereRadius = 12f;

        /// <inheritdoc/>
        protected override void InitializeBoidsSpawnData(Bounds simulationAreaBounds)
        {
            int totalBoidsCount = _boidSpawnData.BoidsCount;
            Vector3 swirlSphereCenter = simulationAreaBounds.center;
            Vector3[] boidPositions = new Vector3[totalBoidsCount];
            Quaternion[] boidRotations = new Quaternion[totalBoidsCount];

            CalculateBoidsSwirlSpawnData(totalBoidsCount, swirlSphereCenter, _initialSwirlSphereRadius, ref boidPositions, ref boidRotations);

            int totalBoidsSpawned = 0;
            FishSchoolProperties schoolProperties = _boidSpawnData.FishSchoolProperties;
            _boids = new BoidInfoGPU[totalBoidsCount];
            for (int i = 0; i < totalBoidsCount; i++)
            {
                int boidIndex = totalBoidsSpawned;
                _boids[boidIndex] = new BoidInfoGPU
                {
                    Position = boidPositions[boidIndex],
                    Direction = boidRotations[boidIndex] * Vector3.forward,
                    Acceleration = 0f,
                    Speed = schoolProperties.MovementProperties.CruisingSpeed,
                    AngularAcceleration = 0f,
                    AngularVelocity = 0f,
                    BoidID = 0,
                    CurrentSwimTime = 0f,
                    MaxPlaybackSpeed = schoolProperties.MotionRenderProperties.MaxSwimPlaybackSpeed,
                    MinPlaybackSpeed = schoolProperties.MotionRenderProperties.MinSwimPlaybackSpeed,
                    SwimMotionIntensity = 0f,
                    OriginalIndex = boidIndex
                };
                totalBoidsSpawned++;
            }
        }
    }
}