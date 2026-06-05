using System;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Structure containing information about the spawn group of boids
    /// for the boid simulation. The group has its own rotation and individual
    /// spawn positions for each boid.
    /// </summary>
    [Serializable]
    public struct GroupOfBoidsSpawnData
    {
        public Quaternion RotationOfTheGroup;
        public Vector3[] SpawnPositions;
    }
}