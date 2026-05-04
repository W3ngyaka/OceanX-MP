using System;
using UnityEngine;

[Serializable]
public struct BoidInfo
{
    public Vector3 Position;
    public Vector3 MovementDirection;
    public float Acceleration;
    public float Speed;
    public float AngularAcceleration;
    public float AngularVelocity;
}
