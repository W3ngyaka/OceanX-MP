using System;
using UnityEngine;

public enum BoidBehaviorState
{
    Schooling = 0,  // normal flocking with same species
    Fleeing   = 1,  // running from a predator
    Hunting   = 2,  // chasing prey
    Idle      = 3,  // slow drift, no active steering
    Dead      = 4   // removed from simulation next cleanup pass
}

[Serializable]
public struct BoidInfo
{
    public Vector3 Position;
    public Vector3 MovementDirection;
    public float   Acceleration;
    public float   Speed;
    public float   AngularAcceleration;
    public float   AngularVelocity;

    // Ecosystem state — visible in Inspector for debugging
    public BoidBehaviorState BehaviorState;
    [Range(0f, 1f)] public float Hunger;      // 0 = full, 1 = starving
    public float PanicTimer;                  // seconds remaining in panic after losing predator
}
