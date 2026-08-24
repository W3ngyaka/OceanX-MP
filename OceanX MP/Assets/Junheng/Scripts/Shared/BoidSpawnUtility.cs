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
        /// <param name="minSpawnDistanceBetweenBoids">Spacing between neighbouring fish in a group. Actually
        /// enforced (see <see cref="GenerateSeparatedPoints"/>), and it also sets how wide a group's cluster
        /// is, since the cluster is just this pitch times however many fish have to fit in it. Callers should
        /// pass BoidSpawnerBase.ResolvedMinSpawnDistance, which floors the inspector value at the species'
        /// own SeparationRange so fish are never born tighter than their flocking wants them.</param>
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

                // Offsets of this group's fish around their own centre, guaranteed to respect the minimum
                // spawn distance (see GenerateSeparatedPoints).
                Vector3[] localOffsets = GenerateSeparatedPoints(boidsToSpawnInThisGroup, minSpawnDistanceBetweenBoids);

                // Full width of the cluster those offsets occupy, used to keep it inside the bounds below.
                int boidsPerDimension = LatticeSideFor(boidsToSpawnInThisGroup);
                float spawnAreaDimensionSize = (boidsPerDimension - 1) * minSpawnDistanceBetweenBoids
                                             + 2f * minSpawnDistanceBetweenBoids * LatticeJitterFraction;

                // Array containing spawn position for each boid instance in this group.
                Vector3[] positions = new Vector3[boidsToSpawnInThisGroup];

                // Shrink the simulation area by the cluster's FULL width so every fish lands inside it — the
                // centre is picked in here, and the offsets reach half a cluster width in each direction, so
                // reserving anything less lets the outer fish spawn through the boundary (where the compute
                // shader would read them as a school on its way out). Clamped at zero so a cluster wider than
                // the tank degrades to "spawn at the centre" instead of inverting the range.
                Bounds reducedSimulationAreaBounds = new Bounds(simulationAreaBounds.center, simulationAreaBounds.size);
                reducedSimulationAreaBounds.size = Vector3.Max(
                    simulationAreaBounds.size - Vector3.one * spawnAreaDimensionSize, Vector3.zero);

                // Calculate a random point inside the reduced simulation area bounds that will be the center of
                // the spawn zone inside which all boids of this group will be spawned.
                Vector3 spawnAreaCenter = GetRandomPositionInsideBounds(reducedSimulationAreaBounds);

                // Determine the initial rotation for the whole group of boids.
                Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);

                for (int i = 0; i < boidsToSpawnInThisGroup; i++)
                {
                    // Spawn new boid and add it to the collection of spawned boids.
                    positions[i] = localOffsets[i] + spawnAreaCenter;

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

        // How far a fish may be nudged off its lattice site, as a fraction of the spacing. The nudge is what
        // stops the cluster reading as a grid; keeping it below 0.5 is what keeps the separation guarantee
        // (two neighbouring fish can only ever close the gap by twice this).
        private const float LatticeJitterFraction = 0.2f;

        // Side length, in cells, of the smallest cubic lattice that can hold this many fish.
        // Computed by growing an integer rather than by ceil(pow(n, 1/3)), which the float pow makes
        // unreliable at exact cubes (8, 27, 64 can land a hair under and round up to a needlessly large
        // lattice, spreading the school wider than asked for).
        private static int LatticeSideFor(int numberOfPoints)
        {
            int side = 1;
            while (side * side * side < numberOfPoints) side++;
            return side;
        }

        /// <summary>
        /// Offsets for one school's fish around their own centre, spread so that no two are closer than
        /// (1 - 2 * <see cref="LatticeJitterFraction"/>) x <paramref name="minDistance"/> — 0.6 x it at the
        /// current setting, with typical neighbours a full <paramref name="minDistance"/> apart.
        ///
        /// The separation is guaranteed by CONSTRUCTION rather than hoped for: the fish are laid on a cubic
        /// lattice of pitch <paramref name="minDistance"/> — where the closest two sites are exactly that far
        /// apart — and each is then nudged off its site by less than half the pitch, so no nudge can close a
        /// gap to zero. Sites are taken nearest-the-centre-first so the school reads as a rounded shoal
        /// rather than a slab or a wireframe box, with ties between equidistant sites broken at random so the
        /// silhouette is not the same every spawn.
        ///
        /// This REPLACES a "generate equidistant points inside a sphere" routine that was not equidistant:
        /// it spread the point DIRECTIONS evenly (a Fibonacci sphere) but then gave each point an
        /// independently random radius, which threw the even spacing away. Nothing in it bounded how close
        /// two fish could land, so a school reliably spawned with several pairs interpenetrating — worst on
        /// the big species, whose bodies are metres long while the whole cluster was about two metres wide.
        /// </summary>
        private static Vector3[] GenerateSeparatedPoints(int numberOfPoints, float minDistance)
        {
            Vector3[] points = new Vector3[numberOfPoints];
            if (numberOfPoints <= 0) return points;

            // A lone fish sits at its school's centre; there is nothing to separate it from.
            if (numberOfPoints == 1)
            {
                points[0] = Vector3.zero;
                return points;
            }

            // A zero/negative spacing means "no spread asked for" — stack them and let the flocking sort it
            // out, rather than dividing the cluster geometry by nothing.
            if (minDistance <= 0f) return points;

            int side = LatticeSideFor(numberOfPoints);
            int siteCount = side * side * side;
            float centreOffset = (side - 1) * 0.5f;

            // Every lattice site, as an offset from the lattice centre.
            Vector3[] sites = new Vector3[siteCount];
            for (int i = 0; i < siteCount; i++)
            {
                int x = i % side;
                int y = (i / side) % side;
                int z = i / (side * side);
                sites[i] = new Vector3(x - centreOffset, y - centreOffset, z - centreOffset) * minDistance;
            }

            // Shuffle first, then sort by distance from the centre. The sort is what makes the cluster
            // rounded; shuffling first is what breaks ties between the many equidistant sites at random, so
            // a partially-filled lattice does not lean the same way (always +X, say) on every spawn.
            for (int i = siteCount - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (sites[i], sites[j]) = (sites[j], sites[i]);
            }
            System.Array.Sort(sites, (a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));

            // Take the innermost sites and nudge each one off-grid.
            float jitterRadius = minDistance * LatticeJitterFraction;
            for (int i = 0; i < numberOfPoints; i++)
            {
                points[i] = sites[i] + Random.insideUnitSphere * jitterRadius;
            }

            return points;
        }
    }
}