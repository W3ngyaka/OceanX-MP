using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Tuning data for how a GPU species behaves — fleeing from predators,
    /// hunting prey, and managing hunger. Mirrors SpeciesBehaviorProperties (CPU Ecosystem)
    /// but lives in the Simulation namespace and is designed for use with the GPU boid system.
    /// Assign to an Apex or Mesopredator SpeciesDataGPU. Leave null for Prey and Neutral.
    /// </summary>
    [CreateAssetMenu(fileName = "SpeciesBehavior", menuName = "OceanX/Species Behavior Properties")]
    public class SpeciesBehaviorPropertiesGPU : ScriptableObject
    {
        [Header("Prey Behavior — Fleeing")]
        [Tooltip("Distance at which this species detects a predator and starts fleeing.")]
        [Range(0f, 50f)] public float FleeRange = 12f;

        [Tooltip("Steering weight toward the escape direction. Higher = sharper panic turn.")]
        [Range(0f, 10f)] public float FleeWeight = 4f;

        [Tooltip("Seconds to stay in a panic state after losing sight of a predator.")]
        public float PanicDuration = 3f;

        [Header("Predator Behavior — Hunting")]
        [Tooltip("Distance at which this species detects prey and begins pursuit.")]
        [Range(0f, 50f)] public float DetectionRange = 20f;

        [Tooltip("Distance at which a kill is registered.")]
        [Range(0f, 5f)] public float AttackRange = 1.5f;

        [Tooltip("Steering weight toward prey during a chase.")]
        [Range(0f, 10f)] public float HuntWeight = 3f;

        [Header("Hunger")]
        [Tooltip("How much hunger increases per second. Set to 0 for prey/neutral species.")]
        public float HungerRate = 0.02f;

        [Tooltip("Hunger level [0-1] at which this species starts actively hunting.")]
        [Range(0f, 1f)] public float HuntThreshold = 0.3f;

        [Tooltip("Hunger level immediately after a successful kill.")]
        [Range(0f, 1f)] public float HungerAfterKill = 0f;
    }
}
