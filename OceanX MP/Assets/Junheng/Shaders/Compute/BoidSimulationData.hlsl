#ifndef BOID_SIMULATION_DATA
#define BOID_SIMULATION_DATA

// NOTE: Ordering of the variables is very important for preserving a small memory footprint!
// NOTE: Variables are sorted so that they fill a 16-byte memory slots.
// NOTE: If you're adding more properties, make sure to round them to 16-byte memory slots by adding empty filler variables.

// Structure containing information about one boid instance in the simulation.
struct BoidInfo {
    // World position of the boid.
    float3 position;
    // Current acceleration of the boid [m/s^2].
    float acceleration;

    // Current movement direction of the boid.
    float3 direction;
    // Current movement speed of the boid [m/s].
    float speed;

    // Current angular velocity of the boid [degrees/s].
    float angularVelocity;
    // Current angular acceleration of the boid [degrees/s^2].
    float angularAcceleration;
    // ID of the boid that represents the index of the boid school info it belongs to
    // as well as the size of the boid. Boids with larger IDs represent larger fish species.
    // Also, it contains a sub-group ID packed into this one single value (bits 8-15), while
    // the boid group ID is packed in the lowest 8 bits (0-7).
    float boidID;
    // Percentage of the swim motion intensity that the boid is currently swimming at.
    // This intensity is updated based on the current acceleration of the boid (not velocity!).
    float swimMotionIntensity;

    // Current swim time for the movement animation in the material.
    float currentSwimTime;
    // Minimal playback speed for updating the current swim time on the material.
    float minPlaybackSpeed;
    // Maximum playback speed for updating the current swim time on the material.
    float maxPlaybackSpeed;
    // Original index of the boid in the global boids buffer. Used for re-arranging
    // boids back to their original position in the buffer after the simulation so that
    // the rendering system could render correct meshes for correct boids.
    float originalIndex;

    // Entry-sprint state, armed ONCE by the spawner and then owned by the simulation kernel:
    //   -1 : spawned at an off-screen entry point, has not crossed into the bounds yet => sprinting in.
    //   >0 : seconds of post-entry sprint still owed; counts down, then the fish settles to cruising.
    //    0 : entry finished, or the fish spawned inside the bounds => never sprints again.
    // Only a boid at -1 reacts to being outside the box, so a settled fish that drifts over the
    // boundary is not mistaken for a new arrival. (Adds a 17th float: the struct
    // is no longer a clean multiple of 16 bytes, which is fine for a StructuredBuffer stride — the
    // 16-byte-slot note above is a footprint nicety, not a correctness requirement.)
    float entryBoostTimeRemaining;
};

// Structure defining properties that affect the look of the swimming motion of a boid.
// Only required for the rendering part in the rendering shader.
struct BoidRenderInfo {
    float minSideToSideAmplitude;
    float maxSideToSideAmplitude;
    float minYawRotationAmplitude;
    float maxYawRotationAmplitude;

    float minRollRotationAmplitude;
    float maxRollRotationAmplitude;
    float minPanningYawAmplitude;
    float maxPanningYawAmplitude;
};

// Structure containing information about one boid school. Since we're simulating fish 
// behavior, this structure basically holds information about one fish species.
struct BoidSchoolInfo {
    // Sensing distance of other fish around this one, for moving in line with them.
    float visionRangeSquared;
    // Sensing distance for surrounding obstacles.
    float obstacleAvoidanceRangeSquared;
    // Sensing distance for other fish around this one, for moving away from them.
    float separationRangeSquared;
    // Multiplier representing how strongly we want to keep our distance from other fish.
    float separationWeight;

    // Multiplier representing how strongly we want to keep close to other fish.
    float cohesionWeight;
    // Multiplier representing how strongly we want to swim in the same direction as other fish.
    float alignmentWeight;
    // Multiplier representing how strongly we want to follow the closest target.
    float targetFollowWeight;
    // Normal swimming speed of this fish species.
    float cruisingSpeed;

    // Maximum swimming speed of this fish species.
    float maxSpeed;
    // Water friction expressed as a percentage of the current movement speed.
    float waterFriction;
    // How fast will the fish loose velocity when it starts slowing down.
    float deceleration;
    // Maximum possible acceleration that the fish can have.
    float maxAcceleration;

    // Basically determines how fast can acceleration increase.
    float movementJerk;
    // Max angular velocity that the fish can reach.
    float maxAngularVelocity;
    // Angular deceleration of the angular velocity when the fish is not trying to turn. Used for smooth movement after turning.
    float angularVelocityReduction;
    // Max angular acceleration that the fish can reach.
    float maxAngularAcceleration;

    // Angular deceleration of the acceleration when the fish is not trying to turn. Used for smooth movement after turning.
    float angularDeceleration;
    // Basically determines how fast can angular acceleration increase.
    float angularJerk;
    // How much of angular acceleration is transferred to movement acceleration.
    float rotationEffectOnSpeed;
    // 0 = avoid obstacles in full 3D (shortest way clear, usually over the top).
    // 1 = avoid strictly horizontally, so a bottom-dweller banks AROUND reef structure rather than
    // climbing it. Near the substrate every escape vector has an upward component, and going "over"
    // each rock accumulates until the fish is at the surface — which is what this exists to stop.
    // (Was emptyFiller; reusing it keeps the struct size unchanged.)
    float horizontalAvoidBias;
};

// Structure defining information about the simulation affecter that affects the 
// movement of the boids throughout the simulation. Affecters can be either considered
// as targets or as obstacles.
// MUST match the C# AffecterGPU struct (AffecterGPU.cs) field-for-field, in order.
// 16 floats (64 bytes): position(3) radius(1) halfExtents(3) shape(1)
// rotation(4) affecterType(1) boidGroupId(1) boidSubGroupId(1) emptyFiller(1).
struct Affecter {
    // World position of the affecter (box center for Box shape).
    float3 position;
    // Sphere radius — distance from affecter where its impact is non-existent (Sphere shape).
    float radius;

    // Box influence half-extents in local space (Box shape). Unused for Sphere.
    float3 halfExtents;
    // Shape flag: 0 --> Sphere, 1 --> Box.
    float shape;

    // Box orientation quaternion (x, y, z, w). Unused for Sphere.
    float4 rotation;

    // Type of the affecter (0 --> Target, 1 --> Obstacle, 2 --> Predator).
    float affecterType;
    // Which boid group does this affecter affect.
    float boidGroupId;
    // Sub-group inside fish species that this boid belongs to.
    float boidSubGroupId;
    // Currently not used, padding to a 16-float (64-byte) stride.
    float emptyFiller;
};

#endif // BOID_SIMULATION_DATA