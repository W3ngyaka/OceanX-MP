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
| 6 | ✅ | Population growth/decline + eco-health score (ratio-driven dynamics; 100% reachable, gradual ramp) |
| 7 | 🔶 | Cascading effects done; formal state-machine enum not built (health-band Alucia reactions cover it); CPU layer removed |
| 8 | ✅ | Flocking + predator movement (done ahead of schedule) |
| 9 | ✅ | Start-at-zero/extinction, netcode + tablet add/remove, and C# events (species / unlock / health-band) all wired |
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
| `EcosystemTargetGPU.cs` | Per-school swim target (REPLACED `WanderingAffecterGPU`); `ParkAt` drives the swim-out on removal |
| `EcosystemUnlockManagerGPU.cs` | Eco-health/prey-gated species unlock system (singleton) |
| `EcosystemDebugHarnessGPU.cs` | In-editor OnGUI add/remove panel (no netcode) — dev-only |
| `FishEntryPointGPU.cs` | Off-screen entry/exit markers — schools swim in / out via these |
| `EcosystemUIAdapterGPU.cs` | UI→GPU bridge (⚠ DEAD — zero external references, confirmed 2026-07-08) |
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

⚠ **Natural births/deaths (Week 8) AND the per-species starvation fields (Week 9) are gone.** Population now changes only from:
- **Global ratio-driven dynamics** — each species feels a prey:predator school-count ratio against a shared dead-band (`RatioBandLow`/`RatioBandHigh`, default 1–3). Out-of-band species grow/shrink at `GrowRate`/`ShrinkRate` per tick; a predator with no prey is a hard shrink (starves). Can remove the last school (extinction).
- **Manual add/remove** via UI buttons / netcode RPCs.

`ReproductionRate` / `NaturalDeathRate` (Week 8) and `StarvationDeathRate` / `StarvationThreshold` (Week 9) were all deleted from `SpeciesDataGPU`. Balance is now **global**; per-species behaviour comes from `FishPerSchool` / `MaxSchools` / prey-predator lists. Carrying capacity = `MaxSchools × FishPerSchool`. Eco-health (`EcoHealth01`) derives from the same ratios. The cascade is emergent — no hardcoded chain-reaction logic.

## What needs building next
1. **Preset scenarios** (Balanced Ocean, Shark Removed, Overpopulation, Collapse, Recovery) — not started
2. **Converge the host scenes** — sim, health bar, and baked environment live in separate `Boids_Demo` / `SCENE_MainScene` copies; merge into one before the final build (see HANDOFF scene-divergence notes)
3. **Re-point Build Settings** — it still references the renamed `Junheng/Scenes/SCENE_MainScene 1.unity`; point it at `SCENE_MainScene.unity` and prune dead scene entries
4. **Strip debug logging** before the final build (e.g. `AluciaEcologyEvents.debugLog`)
5. Final optimisation, balancing, and build (Weeks 11–12)

### Done since this list was last written
- ✅ 12 canonical species created + wired (Giant moray added, Great barracuda removed)
- ✅ Runtime add/remove API + start-at-zero / extinction model (`e13e26b`)
- ✅ Eco-health score — 100% reachable, gradual ramp (`d17fdea`); drives `Health.cs` (tablet) + `HealthBarBinder` (large screen)
- ✅ C# events — species (starving / overpredated / overpopulated), unlock (`OnSpeciesUnlocked` / `OnUnlockStateChanged`), health-band Alucia lines

## Team structure
- Simulation/backend: JunHeng
- UI and rendering: separate teammates
- Each person has their own Claude session — share context via this file and git commits
