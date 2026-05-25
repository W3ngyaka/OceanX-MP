# OceanX MP — Claude Context

## What this project is
An interactive Unity ocean ecosystem simulation built as an educational tool.
Users add/remove marine species and watch cascading effects in real time, learning that marine ecosystems are interconnected and that apex predators (sharks) are critical to balance.

## Sprint plan (Weeks 3–12)
| Week | Status | Task |
|------|--------|------|
| 3 | ✅ | EcosystemManager + species ScriptableObjects |
| 4 | ✅ | Spawning, removal, population tracking |
| 5 | ✅ | Food chain relationships + predator-prey logic |
| 6 | 🔶 | Population growth/decline (tick system done, health score pending) |
| 7 | ❌ | Cascading effects + ecosystem state machine |
| 8 | ✅ | Flocking + predator movement (done ahead of schedule) |
| 9 | ❌ | Event system + UI integration hooks |
| 10 | ❌ | Preset scenarios |
| 11–12 | ❌ | Debugging, optimisation, final build |

## Key scripts
| File | Purpose |
|------|---------|
| `Assets/Scripts/Boids CPU/EcosystemSimulation.cs` | Main manager — tick loop, spawning, population dynamics |
| `Assets/Scripts/Boids CPU/Boid.cs` | Individual fish — flocking, hunting, fleeing, TryKill |
| `Assets/Scripts/Boids CPU/BoidSwimmingUtility.cs` | Physics integration |
| `Assets/Scripts/Boids CPU/SpatialPartition3D.cs` | Spatial grid for neighbour queries |
| `Assets/Scripts/ScriptableObjects/EcosystemDefinition.cs` | Top-level asset — species list + simulation bounds |
| `Assets/Scripts/ScriptableObjects/SpeciesDefinition.cs` | Per-species data (role, population dynamics, predator/prey lists) |
| `Assets/Scripts/ScriptableObjects/BoidSchoolProperties.cs` | Flocking weights and ranges |
| `Assets/Scripts/ScriptableObjects/SpeciesBehaviorProperties.cs` | Predator/prey AI settings |

## GPU Ecosystem Layer (active simulation — Boids_Demo scene)
All files: `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/`
Namespace: `OceanX.BoidsGPU.Ecosystem`

| File | Purpose |
|------|---------|
| `EcosystemSimulationGPU.cs` | Runtime add/remove/tick — runs in Awake before BoidSimulationGPU.Start |
| `EcosystemDefinitionGPU.cs` | Top-level asset — species list + simulation bounds |
| `SpeciesDataGPU.cs` | Per-species data (Role, SchoolProperties, prey/predator lists, pop dynamics) |
| `SpeciesBehaviorPropertiesGPU.cs` | Flee/hunt/hunger settings |
| `WanderingAffecterGPU.cs` | Randomly wandering target used for Apex species sub-groups |
| `EcosystemUIAdapterGPU.cs` | Bridges UI (CPU SpeciesDefinition) → GPU layer; same API surface as CPU version |
| `BoidSimulationGPU.cs` (03_Spatial_Partition) | GPU simulation + `ReinitializeBuffers()` |
| `BoidSpawnerGPU.cs` | GPU spawner — now holds position-preservation logic for buffer rebuilds |

### Runtime add/remove flow
`EcosystemSimulationGPU.AddSpecies / RemoveSpecies` →
`spawner.SetBoidsCount(newCount)` → `_simulation.ReinitializeBuffers()`

`ReinitializeBuffers()` sequence:
1. Read live GPU positions back to CPU (from correct ping-pong buffer)
2. Slice per spawner using `spawner.Boids.Length` (old count, not new)
3. Call `spawner.StorePreservedBoids(slice)` on each spawner
4. Tear down all GPU buffers (derived → base → spatial partition → spawners)
5. Re-run full init chain — `SpawnBoids` restores old positions, only new fish get fresh spawn positions

## Population dynamics (how the cascade works)
`EcosystemSimulation` runs a coroutine every `PopulationTickInterval` seconds (default 5s).

Per tick per species:
- **births** = `pop × reproRate × (1 - pop/carryingCapacity)` — logistic growth
- **naturalDeaths** = `pop × naturalDeathRate`
- **starvationDeaths** = `pop × starvationDeathRate` if any prey species is below `starvationThreshold × their carryingCapacity`

The cascade is emergent — no hardcoded chain reaction logic.

## Recommended species values
| Species     | ReproRate | NaturalDeath | CarryingCap | StarveDeath | StarveThreshold |
|-------------|-----------|--------------|-------------|-------------|-----------------|
| Shark        | 0.02     | 0.01         | 10          | 0.30        | 0.20            |
| Medium fish  | 0.12     | 0.03         | 60          | 0.25        | 0.15            |
| Small fish   | 0.20     | 0.05         | 150         | 0.00        | 0.00            |
| Plankton     | 0.30     | 0.08         | 300         | 0.00        | 0.00            |

## What needs building next
1. `FishAnimationProperties.cs` + `FishAnimator.cs` — procedural swimming animation (field already added to SpeciesDefinition)
2. Ecosystem health score + state machine (`Healthy / Unstable / Critical / Collapsing / Recovering`)
3. C# event system — fires on population change, health change, state change (bridge to UI team)
4. ~~Runtime add/remove species API~~ ✅ Done — `EcosystemSimulationGPU.AddSpecies/RemoveSpecies`, position preservation on rebuild fixed
5. Preset scenarios (Balanced Ocean, Shark Removed, Overpopulation, Collapse, Recovery)

## Team structure
- Simulation/backend: JunHeng
- UI and rendering: separate teammates
- Each person has their own Claude session — share context via this file and git commits
