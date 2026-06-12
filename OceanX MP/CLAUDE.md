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
| 7 | 🔶 | Cascading effects done; ecosystem state machine not started; CPU layer removed |
| 8 | ✅ | Flocking + predator movement (done ahead of schedule) |
| 9 | 🔶 | Start-at-zero/extinction done; netcode + tablet add/remove working; C# events not wired |
| 10 | ❌ | Preset scenarios |
| 11–12 | ❌ | Debugging, optimisation, final build |

> ⚠ **The CPU ecosystem layer was deleted in Week 7.** Scripts like `EcosystemSimulation.cs`, `Boid.cs`, `SpatialPartition3D.cs`, `EcosystemDefinition.cs`, `SpeciesDefinition.cs` no longer exist. The product runs entirely on the GPU layer below. See `HANDOFF.md` for the full, verified file tree.

## Key scripts (all under `Assets/Junheng/Scripts/`)
| File | Purpose |
|------|---------|
| `Boids_GPU/Spatial_Partition_Instanced_Rendering/BoidSimulationGPU.cs` | Active GPU simulation — dispatch, render, `ReinitializeBuffers()` |
| `Boids_GPU/BoidSimulationBaseGPU.cs` | Abstract GPU sim base — owns shared compute buffers |
| `Boids_GPU/BoidSpawnerGPU.cs` / `BoidSpawnerGPUMultiTargets.cs` | GPU spawners — position preservation + per-school targets |
| `Boids_GPU/GPU_Spatial_Partition/SpatialPartitionGPU.cs` | Spatial grid compute wrapper for neighbour queries |
| `Boids_GPU/Ecosystem/EcosystemSimulationGPU.cs` | Tick cascade + start-at-zero add/remove API |
| `Boids_GPU/Ecosystem/EcosystemDefinitionGPU.cs` | Top-level asset — species list + simulation bounds |
| `Boids_GPU/Ecosystem/SpeciesDataGPU.cs` | Per-species data (Role, school props, prey/predator lists, FishPerSchool, MaxSchools) |
| `Boids_GPU/Ecosystem/SpeciesBehaviorPropertiesGPU.cs` | Predator/prey AI (flee/hunt/hunger) |
| `Networking/EcosystemNetworkManagerGPU.cs` | Syncs school counts via NetworkList + add/remove RPCs |
| `Shared/BoidSimulationBase.cs` / `BoidSpawnerBase.cs` | Cross-CPU/GPU base classes (SchoolCount/IsActive live here) |

## GPU Ecosystem Layer (active simulation — Boids_Demo scene)
All files: `Assets/Junheng/Scripts/Boids_GPU/Ecosystem/`
Namespace: `OceanX.BoidsGPU.Ecosystem` (note: `BoidSimulationGPU` itself is in `OceanX.BoidsGPU.SpatialPartitionInstancedRendering`)

| File | Purpose |
|------|---------|
| `EcosystemSimulationGPU.cs` | Runtime add/remove/tick — runs in Awake before BoidSimulationGPU.Start |
| `EcosystemDefinitionGPU.cs` | Top-level asset — species list + simulation bounds |
| `SpeciesDataGPU.cs` | Per-species data (Role, SchoolProperties, prey/predator lists, pop dynamics) |
| `SpeciesBehaviorPropertiesGPU.cs` | Flee/hunt/hunger settings |
| `WanderingAffecterGPU.cs` | Randomly wandering target — one per school, all roles |
| `EcosystemUIAdapterGPU.cs` | UI→GPU bridge (⚠ only self-referenced in code — verify if still used) |
| `BoidSimulationGPU.cs` (Spatial_Partition_Instanced_Rendering) | GPU simulation + `ReinitializeBuffers()` |
| `BoidSpawnerGPU.cs` | GPU spawner — holds position-preservation logic for buffer rebuilds |

### Runtime add/remove flow (start-at-zero model, since `e13e26b`)
Species start at **0 schools** (excluded from the sim). Add = +1 school, Remove = −1 down to extinction (0); capped at per-species `MaxSchools`.

`EcosystemSimulationGPU.AddSpecies / RemoveSpecies` →
`spawner.SetSchoolConfiguration(schoolCount, fishPerSchool)` → `_simulation.ReinitializeBuffers()`

`ReinitializeBuffers()` sequence:
1. Read live GPU positions back to CPU (from correct ping-pong buffer; skipped when empty)
2. Slice per active spawner using `spawner.Boids.Length` (old count, not new)
3. Call `spawner.StorePreservedBoids(slice)` on each spawner
4. Tear down all GPU buffers (derived → base → spatial partition → spawners)
5. Re-run full init chain — `SpawnBoids` restores old positions, only new fish get fresh spawn positions

Empty-ocean / last-extinction is crash-safe: all buffers sized `Mathf.Max(1, count)`, dispatch + render skipped when `_boidsCount == 0`.

## Population dynamics (how the cascade works)
`EcosystemSimulationGPU` runs a coroutine every tick interval (default 5s).

⚠ **Natural births and natural deaths were removed in Week 8.** Population now changes only from:
- **Starvation cascade (ratio-based)** — a species loses a school when any prey species drops below `StarvationThreshold` fraction of its capacity, rolled at `StarvationDeathRate`. Can remove the last school (extinction).
- **Manual add/remove** via UI buttons / netcode RPCs.

`ReproductionRate` and `NaturalDeathRate` fields were deleted from `SpeciesDataGPU`. Carrying capacity is derived from `MaxSchools × FishPerSchool`. The cascade is emergent — no hardcoded chain reaction logic.

## What needs building next
1. Finalise the **12 canonical species** (list locked — see HANDOFF): create **Giant moray** assets, remove **Great barracuda**, then wire all 12 into `EcosystemDefinitionGPU` in fixed order (currently only the Clownfish placeholder is wired)
2. Play-test the start-at-zero model in-editor (committed in `e13e26b` without an editor run)
3. Ecosystem health score + state machine (`Healthy / Unstable / Critical / Collapsing / Recovering`); wire `Health.cs` bar to GPU data
4. C# event system — fires on population change, health change, state change (bridge to UI team)
5. ~~Runtime add/remove species API~~ ✅ Done · ~~Start-at-zero/extinction~~ ✅ Done (`e13e26b`)
6. Preset scenarios (Balanced Ocean, Shark Removed, Overpopulation, Collapse, Recovery)

## Team structure
- Simulation/backend: JunHeng
- UI and rendering: separate teammates
- Each person has their own Claude session — share context via this file and git commits
