using System;
using UnityEngine;

namespace OceanX.BoidsCPU
{
    /// <summary>
    /// Structure containing the properties that define one boid instance in the simulation.
    /// </summary>
    [Serializable]
    public struct BoidInformation
    {
        public Vector3 Position;
        public Vector3 MovementDirection;
        public float Acceleration;
        public float Speed;
        public float AngularAcceleration;
        public float AngularVelocity;
    }
}