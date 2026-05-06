using UnityEngine;

[CreateAssetMenu(fileName = "BoidMovementProperties", menuName = "OceanX/Boid Movement Properties")]
public class BoidMovementProperties : ScriptableObject
{
    [Header("Speed")]
    public float CruisingSpeed = 1.75f;
    public float MaxSpeed = 12f;
    [Range(0f, 1f)] public float WaterFriction = 0.45f;

    [Header("Acceleration")]
    public float Deceleration = 15f;
    public float MaxAcceleration = 20f;
    public float MovementJerk = 1000f;

    [Header("Rotation")]
    public float MaxAngularVelocity = 180f;
    public float AngularVelocityReduction = 200f;
    public float MaxAngularAcceleration = 85f;
    public float AngularDeceleration = 30f;
    public float AngularJerk = 1600f;
    [Range(0f, 0.5f)] public float RotationEffectOnSpeed = 0.05f;
}
