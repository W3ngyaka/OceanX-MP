using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Static class that provides a helper method for spawning fish inside the simulation area.
    /// Based on the total number of fish, the size of the simulation area and the desired number
    /// of initial groups, the function will determine the individual positions of the fish so that
    /// they appear natural and not too uniform and unreal.
    /// </summary>
    public static class BoidSpawnUtility
    {
        /// <summary>
        /// Function generates the initial rotation and position of each boid that will be spawned in the simulation.
        /// The boids are split into individual groups, with each group having a different position inside the simulation area.
        /// Each group has its own initial rotation and individual positions of its boids. The positions and rotations are all contained
        /// inside the <see cref="GroupOfBoidsSpawnData"/> structure, which is returned as a result of the function.
        /// </summary>
        /// <param name="totalBoidsCount">Number of boids whose spawn position and rotation need to be calculated.</param>
        /// <param name="simulationAreaBounds"><see cref="Bounds"/> representing the center and size of the simulation area.</param>
        /// <param name="minSpawnDistanceBetweenBoids">Minimum spawn distance between two individual boid instances in the simulation.</param>
        /// <param name="totalGroupsCount">Total number of groups into which the boids should be split initially.</param>
        /// <returns>An instance of the <see cref="GroupOfBoidsSpawnData"/> structure containing initial rotation and position of each boid.</returns>
        public static GroupOfBoidsSpawnData[] CalculateBoidsSpawnData(int totalBoidsCount, Bounds simulationAreaBounds, float minSpawnDistanceBetweenBoids, int totalGroupsCount)
        {
            // Structures containing the spawn information for each boids spawn group.
            GroupOfBoidsSpawnData[] boidSpawnGroups = new GroupOfBoidsSpawnData[totalGroupsCount];

            // Number of boids that each group will have. Actually, the last group will have a bit less
            // than the other groups since the total number of boids isn't a direct multiplier of the group count.
            int boidsCountPerGroup = Mathf.CeilToInt(totalBoidsCount / (float)totalGroupsCount);
            int totalBoidsSpawned = 0;

            // For each boid group, place it somewhere inside the simulation area and initialize the positions of boids that
            // are a part of that group.
            for (int boidGroupIndex = 0; boidGroupIndex < totalGroupsCount; boidGroupIndex++)
            {
                // Calculate the dimension size of the spawn area bounds for this group based on the total number of
                // boids that will be spawned in this spawn area.
                int boidsToSpawnInThisGroup = Mathf.Min(boidsCountPerGroup, totalBoidsCount - totalBoidsSpawned);
                int boidsPerDimension = Mathf.CeilToInt(Mathf.Pow((float)boidsToSpawnInThisGroup, 0.33333f));
                float spawnAreaDimensionSize = (boidsPerDimension - 1) * minSpawnDistanceBetweenBoids;

                // Array containing spawn position for each boid instance in this group.
                Vector3[] positions = new Vector3[boidsToSpawnInThisGroup];

                // Reduce the simulation area bounds by the half of the spawn area to ensure that all boids are
                // correctly spawned inside the simulation area.
                Bounds reducedSimulationAreaBounds = new Bounds(simulationAreaBounds.center, simulationAreaBounds.size);
                reducedSimulationAreaBounds.size -= (Vector3.one * spawnAreaDimensionSize * 0.5f);

                // Calculate a random point inside the reduced simulation area bounds that will be the center of 
                // the spawn zone inside which all boids of this group will be spawned.
                Vector3 spawnAreaCenter = GetRandomPositionInsideBounds(reducedSimulationAreaBounds);

                // Determine the initial rotation for the whole group of boids.
                Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);

                // Spawn boids at equally distant points inside the spawn sphere.
                Vector3[] randomPointsInsideSphere = GenerateEquidistantPointsInsideSphere(boidsToSpawnInThisGroup, spawnAreaDimensionSize);
                for (int i = 0; i < boidsToSpawnInThisGroup; i++)
                {
                    // Spawn new boid and add it to the collection of spawned boids.
                    Vector3 boidPosition = randomPointsInsideSphere[i] + spawnAreaCenter;
                    positions[i] = boidPosition;

                    // Keep track of total spawned boids count.
                    totalBoidsSpawned++;
                }

                GroupOfBoidsSpawnData groupOfBoidsSpawnData = new GroupOfBoidsSpawnData
                {
                    RotationOfTheGroup = spawnRotation,
                    SpawnPositions = positions
                };
                boidSpawnGroups[boidGroupIndex] = groupOfBoidsSpawnData;
            }

            return boidSpawnGroups;
        }

        /// <summary>
        /// Function calculates the initial positions and rotations for each boid so that it's spawned on the surface of the sphere
        /// and oriented in the clockwise direction based on the tangent of the sphere at that point.
        /// </summary>
        /// <param name="totalBoidsCount">Total boids count to be spawned on the sphere.</param>
        /// <param name="swirlSphereCenter">Center of the sphere around which the boids will be spawned.</param>
        /// <param name="swirlSphereRadius">Radius of the spawning sphere.</param>
        /// <param name="boidsPositions">Reference to the array containing the output initial positions of the boids.</param>
        /// <param name="boidsRotations">Reference to the array containing the output initial rotations of the boids.</param>
        public static void CalculateBoidsSwirlSpawnData(int totalBoidsCount, Vector3 swirlSphereCenter, float swirlSphereRadius, ref Vector3[] boidsPositions, ref Quaternion[] boidsRotations)
        {
            // Calculate the golden angle increment.
            float goldenAngleIncrement = Mathf.PI * (3f - Mathf.Sqrt(5f));

            // Vertical spacing between two neighboring points on the sphere.
            float verticalSpacing = 2f / totalBoidsCount;

            for(int i = 0; i < totalBoidsCount; i++)
            {
                // i * verticalSpacing gives a linearly increasing value in range [0, 2]
                // Subtracting by 1 moves it in range [-1, 1]
                // Finally, offsetting by half vertical spacing positions the point in the center of the vertical segment.
                float y = ((i * verticalSpacing) - 1f) + (verticalSpacing / 2f);
                float radiusAtY = Mathf.Sqrt(1f - y * y);
                float phiAngle = i * goldenAngleIncrement;

                // Based on the golden ratio, calculate the X and Z coordinates at this height.
                float x = Mathf.Cos(phiAngle) * radiusAtY;
                float z = Mathf.Sin(phiAngle) * radiusAtY;

                // Final position is calculated by offsetting the point around the center of the sphere and making sure that its 
                // located at the surface of the sphere.
                Vector3 position = swirlSphereCenter + new Vector3(x, y, z).normalized * swirlSphereRadius;

                // Normal at position is the same as the vector from center to the position on the sphere.
                Vector3 normal = (position - swirlSphereCenter).normalized;

                // Computing tangent vector using the cross product of the normal and the up vector.
                Vector3 tangent = Vector3.Cross(Vector3.up, normal);
                if (tangent == Vector3.zero)
                {
                    tangent = Vector3.Cross(Vector3.right, normal);
                }
                tangent.Normalize();

                // Create rotation from local axes (tangent = forward, normal = up).
                Quaternion rotation = Quaternion.LookRotation(tangent, normal);

                // Caching the initial position and rotation for each boid.
                boidsPositions[i] = position;
                boidsRotations[i] = rotation;
            }
        }

        /// <summary>
        /// Function determines a random position and rotation for each boid inside the simulation area.
        /// </summary>
        /// <param name="totalBoidsCount">Total number of boids for which the position and rotation should be calculated.</param>
        /// <param name="simulationAreaBounds">Bounds of the simulation area.</param>
        /// <returns>Collections containing a random position and rotation for every boid.</returns>
        public static (Vector3[], Quaternion[]) CalculateRandomBoidsSpawnData(int totalBoidsCount, Bounds simulationAreaBounds)
        {
            // Setup collections that will hold the results.
            Vector3[] positions = new Vector3[totalBoidsCount];
            Quaternion[] rotations = new Quaternion[totalBoidsCount];

            for (int boidIndex = 0; boidIndex < totalBoidsCount; boidIndex++)
            {
                // Determine the initial random position and rotation for the boid.
                Vector3 spawnPosition = GetRandomPositionInsideBounds(simulationAreaBounds);
                Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
                positions[boidIndex] = spawnPosition;
                rotations[boidIndex] = spawnRotation;
            }

            return (positions, rotations);
        }

        private static Vector3 GetRandomPositionInsideBounds(Bounds simulationAreaBounds)
        {
            Vector3 simulationAreaCenter = simulationAreaBounds.center;
            Vector3 simulationAreaExtents = simulationAreaBounds.extents;

            float positionX = Random.Range(simulationAreaCenter.x - simulationAreaExtents.x, simulationAreaCenter.x + simulationAreaExtents.x);
            float positionY = Random.Range(simulationAreaCenter.y - simulationAreaExtents.y, simulationAreaCenter.y + simulationAreaExtents.y);
            float positionZ = Random.Range(simulationAreaCenter.z - simulationAreaExtents.z, simulationAreaCenter.z + simulationAreaExtents.z);

            return new Vector3(positionX, positionY, positionZ);
        }

        private static Vector3[] GenerateEquidistantPointsInsideSphere(int numberOfPoints, float radius)
        {
            Vector3[] points = new Vector3[numberOfPoints];

            // A single point would divide by (numberOfPoints - 1) == 0 below and produce a NaN
            // position, which corrupts the GPU boids/spatial-partition buffers. Place it at the centre.
            if (numberOfPoints == 1)
            {
                points[0] = Vector3.zero;
                return points;
            }

            // Calculate the golden angle increment.
            float phi = Mathf.PI * (3f - Mathf.Sqrt(5f));

            // Calculate the random position inside the sphere for every point.
            for (int i = 0; i < numberOfPoints; i++)
            {
                // Y-coordinate in range [-1, 1]
                float y = 1f - (i / (float)(numberOfPoints - 1f)) * 2f;
                float radiusAtHeightY = Mathf.Sqrt(1 - y * y);
                // Golden angle increment.
                float theta = phi * i;

                // Calculate X and Z coordinates.
                float x = Mathf.Cos(theta) * radiusAtHeightY;
                float z = Mathf.Sin(theta) * radiusAtHeightY;

                // Distribute radius inside the sphere (cube root for uniform density).
                float randomRadius = Mathf.Pow(Random.value, 1f / 3f) * radius;

                // Generate the final position.
                points[i] = new Vector3(x, y, z) * randomRadius;
            }

            return points;
        }
    }
}