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
    // Currently not used, but added to fill to the 16-byte memory slot.
    float emptyFiller;
};

// Structure defining information about the simulation affecter that affects the 
// movement of the boids throughout the simulation. Affecters can be either considered
// as targets or as obstacles.
struct Affecter {
    // World position of the affecter.
    float3 position;
    // Size of the affecter, determining the distance from affecter where its impact is non-existent.
    float radius;

    // Type of the affecter (0 --> Target, 1 --> Obstacle).
    float affecterType;
    // Which boid group does this affecter affect.
    float boidGroupId;
    // Sub-group inside fish species that this boid belongs to.
    float boidSubGroupId;
    // Currently not used, but added to fill to the 16-byte memory slot.
    float emptyFiller;
};

#endif // BOID_SIMULATION_DATA