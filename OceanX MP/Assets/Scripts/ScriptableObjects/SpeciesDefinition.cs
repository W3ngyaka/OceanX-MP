using System.Collections.Generic;
using UnityEngine;

public enum SpeciesRole { Predator, Prey, Neutral }

// Master data asset for a single species.
// Create one asset per species via Assets > Create > OceanX > Species Definition.
// Assign it to an EcosystemDefinition to include it in the simulation.
[CreateAssetMenu(fileName = "SpeciesDefinition", menuName = "OceanX/Species Definition")]
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

    [Header("Predator-Prey Relationships")]
    [Tooltip("Species this animal actively hunts. Only relevant if Role = Predator.")]
    public List<SpeciesDefinition> PreySpecies     = new List<SpeciesDefinition>();

    [Tooltip("Species this animal flees from. Only relevant if Role = Prey or Neutral.")]
    public List<SpeciesDefinition> PredatorSpecies = new List<SpeciesDefinition>();

    // Assigned at runtime by EcosystemSimulation — do not set manually.
    [HideInInspector] public int RuntimeId = -1;
}
