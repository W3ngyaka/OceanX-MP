using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Structure containing the properties that define one boid instance in the simulation.
    /// Since the simulation is executed on the GPU, the properties are combined into clusters
    /// of four-component vectors, or 16-byte memory slots. 
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoidInfoGPU
    {
        // NOTE: Visual separation of variables with empty space is just for easier reading.
        // NOTE: The actual 16-byte memory slots combination is a result of variable ordering and adding empty fillers.

        /// <summary>
        /// Total size of the struct, in bytes. Manually pre-calculated.
        /// 18 floats: the 16 original fields + <see cref="EntryBoostTimeRemaining"/> + <see cref="SignedTurnRate"/>.
        /// </summary>
        public const int Size = sizeof(float) * 18;

        /// <summary>
        /// World position of the boid.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Current acceleration of the boid [m/s^2].
        /// </summary>
        public float Acceleration;

        /// <summary>
        /// Current movement direction of the boid.
        /// </summary>
        public Vector3 Direction;
        /// <summary>
        /// Current movement speed of the boid [m/s].
        /// </summary>
        public float Speed;

        /// <summary>
        /// Current angular velocity of the boid [degrees/s].
        /// </summary>
        public float AngularVelocity;
        /// <summary>
        /// Current angular acceleration of the boid [degrees/s^2].
        /// </summary>
        public float AngularAcceleration;
        /// <summary>
        /// ID of the boid that represents the index of the boid school info it belongs to
        /// as well as the size of the boid. Boids with larger IDs represent larger fish species.
        /// Also, it contains a sub-group ID packed into this one single value (bits 8-15), while
        /// the boid group ID is packed in the lowest 8 bits (0-7).
        /// </summary>
        public float BoidID;
        /// <summary>
        /// Percentage of the swim motion intensity that the boid is currently swimming at.
        /// This intensity is updated based on the current acceleration of the boid (not velocity!).
        /// </summary>
        public float SwimMotionIntensity;

        /// <summary>
        /// Current swim time for the movement animation in the material.
        /// </summary>
        public float CurrentSwimTime;
        /// <summary>
        /// Minimal playback speed for updating the current swim time on the material.
        /// </summary>
        public float MinPlaybackSpeed;
        /// <summary>
        /// Maximum playback speed for updating the current swim time on the material.
        /// </summary>
        public float MaxPlaybackSpeed;
        /// <summary>
        /// Original index of this boid inside the global boids buffer. Used to re-arrange the
        /// boid back to its original position in the buffer, after the boid simulation has been completed
        /// so that the rendering shader could render the correct boid instance with the correct mesh.
        /// </summary>
        public float OriginalIndex;

        /// <summary>
        /// Entry-sprint state, armed once by <see cref="BoidSpawnerGPU.SpawnBoids"/> and then owned by
        /// the compute shader:
        /// <list type="bullet">
        /// <item>-1 — spawned at an off-screen entry point and has not crossed into the simulation
        /// bounds yet; the fish sprints until it enters.</item>
        /// <item>&gt; 0 — seconds of post-entry sprint still owed, counting down to 0.</item>
        /// <item>0 — entry finished, or the fish spawned inside the bounds; it never sprints again.</item>
        /// </list>
        /// The shader only reacts to a boid being outside the bounds while this is -1, so a settled fish
        /// that drifts out is not mistaken for a new arrival and re-launched at MaxSpeed.
        /// </summary>
        public float EntryBoostTimeRemaining;

        /// <summary>
        /// Signed turn rate for render-time deformation, written by the compute shader each frame.
        /// Range roughly [-1, 1]: the sign is the direction the boid is banking (which way it is
        /// yawing around the world-up axis) and the magnitude is how hard it is turning (its angular
        /// velocity as a fraction of the species' maximum). 0 when swimming straight.
        ///
        /// Consumed only by the ray wing shader (OceanX/Ray_Wing_Lit_Instanced) to sweep the tail
        /// toward the turn. The fish shader ignores it, so this field is behaviourally inert for
        /// every other boid — it exists purely to carry the sign the simulation would otherwise
        /// discard (AngularVelocity is stored unsigned).
        /// </summary>
        public float SignedTurnRate;
    }
}