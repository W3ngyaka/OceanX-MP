using System.Collections.Generic;
using UnityEngine;

public enum SpeciesRole
{
    Apex,           // Top of the chain — solitary, hunts freely, not hunted (e.g. Tiger Shark)
    Mesopredator,   // Middle tier — flocks, hunts smaller species, gets hunted by apex (e.g. Grouper, Barracuda)
    Prey,           // Bottom tier — flocks, does not hunt (e.g. Surgeonfish, Parrotfish)
    Neutral         // No predator/prey role — decorative or ambient species
}

// Master data asset for a single species.
// Create one asset per species via Assets > Create > OceanX > Species Definition.
// Assign it to an EcosystemDefinition to include it in the simulation.
[CreateAssetMenu(fileName = "Species_Definition", menuName = "OceanX/Species Definition")]
public class SpeciesDefinition : ScriptableObject
{
    [Header("Identity")]
    public string      SpeciesName = "Unknown Species";
    public SpeciesRole Role        = SpeciesRole.Prey;

    [Header("Visuals")]
    [Tooltip("Prefab must have a Boid component, MeshFilter, and MeshRenderer.")]
    public GameObject Prefab;

    [Header("Population")]
    [Tooltip("How many individuals to spawn at simulation start.")]
    public int   DefaultPopulation = 30;
    [Tooltip("Radius around the spawn point to scatter individuals.")]
    [Range(1f, 100f)] public float SpawnRadius = 10f;

    [Header("Behaviour Style")]
    [Tooltip("If true, this species ignores flocking and patrols alone. Use for apex predators like sharks.")]
    public bool IsSolitary = false;

    [Header("Behavior")]
    [Tooltip("Flocking ranges and weights for this species.")]
    public BoidSchoolProperties    SchoolProperties;

    [Tooltip("Predator/prey AI settings. Leave null for species with no hunting or fleeing.")]
    public SpeciesBehaviorProperties BehaviorProperties;

    [Header("Population Dynamics")]
    [Tooltip("Base birth rate per individual per population tick.")]
    [Range(0f, 1f)] public float ReproductionRate   = 0.10f;
    [Tooltip("Baseline mortality per individual per population tick, excluding predation kills.")]
    [Range(0f, 1f)] public float NaturalDeathRate    = 0.02f;
    [Tooltip("Maximum sustainable population for this species.")]
    public int CarryingCapacity                      = 100;
    [Tooltip("Extra death rate applied each tick when any prey species is below the starvation threshold.")]
    [Range(0f, 1f)] public float StarvationDeathRate = 0.20f;
    [Tooltip("Fraction of prey carrying capacity below which this species starts starving.")]
    [Range(0f, 1f)] public float StarvationThreshold = 0.15f;

    [Header("Predator-Prey Relationships")]
    [Tooltip("Species this animal actively hunts. Only relevant if Role = Predator.")]
    public List<SpeciesDefinition> PreySpecies     = new List<SpeciesDefinition>();

    [Tooltip("Species this animal flees from. Only relevant if Role = Prey or Neutral.")]
    public List<SpeciesDefinition> PredatorSpecies = new List<SpeciesDefinition>();

    [Header("Ratio Pressure (Predators Only)")]
    [Tooltip("Healthy predator-to-prey ratio. E.g. 0.1 = 1 predator per 10 prey. Leave at 0 for prey/neutral species.")]
    [Range(0f, 1f)] public float HealthyPreyRatio = 0.1f;

    [Tooltip("How hard the cascade hits when the ratio is off. Higher = faster, more dramatic population swings.")]
    [Range(0f, 5f)] public float RatioPressureStrength = 1.0f;

    // Assigned at runtime by EcosystemSimulation — do not set manually.
    [HideInInspector] public int RuntimeId = -1;
}
