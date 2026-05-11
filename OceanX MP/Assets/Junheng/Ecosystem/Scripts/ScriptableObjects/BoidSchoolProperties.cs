using UnityEngine;

[CreateAssetMenu(fileName = "Fish_School", menuName = "OceanX/Fish School Properties")]
public class BoidSchoolProperties : ScriptableObject
{
    [Header("Flocking Ranges")]
    [Range(0f, 100f)] public float VisionRange = 10f;
    [Range(0f, 20f)]  public float ObstacleAvoidanceRange = 5f;
    [Range(0f, 100f)] public float SeparationRange = 5f;

    [Header("Flocking Weights")]
    [Range(0f, 10f)] public float CohesionWeight = 0.5f;
    [Range(0f, 10f)] public float AlignmentWeight = 0.7f;
    [Range(0f, 10f)] public float SeparationWeight = 0.9f;
    [Range(0f, 10f)] public float TargetWeight = 1.75f;

    [Header("Movement")]
    public BoidMovementProperties MovementProperties;
}
