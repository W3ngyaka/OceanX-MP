using UnityEngine;

// Tuning data for how a species behaves in the ecosystem — fleeing from predators,
// hunting prey, and managing hunger. Attach to a SpeciesDefinition asset.
[CreateAssetMenu(fileName = "SpeciesBehaviorProperties", menuName = "OceanX/Species/Behavior Properties")]
public class SpeciesBehaviorProperties : ScriptableObject
{
    [Header("Prey Behavior — Fleeing")]
    [Tooltip("Distance at which this species detects a predator and starts fleeing.")]
    [Range(0f, 50f)] public float FleeRange = 12f;

    [Tooltip("Steering weight toward the escape direction. Higher = sharper panic turn.")]
    [Range(0f, 10f)] public float FleeWeight = 4.0f;

    [Tooltip("Seconds to stay in a panic state after losing sight of a predator.")]
    public float PanicDuration = 3.0f;

    [Header("Predator Behavior — Hunting")]
    [Tooltip("Distance at which this species detects prey and begins pursuit.")]
    [Range(0f, 50f)] public float DetectionRange = 20f;

    [Tooltip("Distance at which a kill is registered and prey is removed.")]
    [Range(0f, 5f)] public float AttackRange = 1.5f;

    [Tooltip("Steering weight toward prey during a chase.")]
    [Range(0f, 10f)] public float HuntWeight = 3.0f;

    [Header("Hunger")]
    [Tooltip("How much hunger increases per second. Set to 0 for prey/neutral species.")]
    public float HungerRate = 0.02f;

    [Tooltip("Hunger level [0-1] at which this species starts actively hunting.")]
    [Range(0f, 1f)] public float HuntThreshold = 0.3f;

    [Tooltip("Hunger level immediately after a successful kill.")]
    [Range(0f, 1f)] public float HungerAfterKill = 0.0f;
}
