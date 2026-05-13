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
        /// </summary>
        public const int Size = sizeof(float) * 16;

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
    }
}