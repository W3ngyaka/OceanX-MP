# OceanX MP — Handoff Document
_Last updated: 2026-05-26 (revised after full technical audit — see bottom section)_

---

## Project Goal

An interactive Unity ocean ecosystem simulation built as an **educational tool**.

**Problem it solves:** Lack of experiential learning tools limits ocean literacy and systems thinking.

**What the user does:**
- Opens a Food Chain view (icon → overlay) and clicks animals to read species info
- Adds or removes marine species using UI buttons
- Adjusts biodiversity levels
- Watches cascading effects unfold in real time

**What they learn:**
- Marine ecosystems are interconnected systems
- Sharks (apex predators) are critical to maintaining balance
- Removing one species causes a chain reaction across the food chain

**Core demo moment:** Remove all sharks → medium fish overpopulate → small fish collapse from over-predation → medium fish starve and collapse too.

---

## Sprint Plan Status

| Week | Sprint | Status |
|------|--------|--------|
| 1 | Research and concept development | ✅ Done |
| 2 | Planning, system design, task allocation | ✅ Done |
| 3 | Core simulation manager + species data system | ✅ Done |
| 4 | Spawning, removal, population tracking | ✅ Done |
| 5 | Food chain relationships + predator-prey logic | ✅ Done |
| 6 | Population growth/decline + ecosystem health system | 🟢 Partial — logistic growth, natural death, starvation live on **both** CPU and GPU ticks; health score and state machine not yet built |
| 7 | Cascading effects + ecosystem state machine | 🔶 Partial — cascade is emergent from the population dynamics; state machine not started |
| 8 | Movement systems — flocking + predator behaviour | ✅ Done (completed Week 5) |
| 9 | Event system + integration hooks for UI | ❌ Not started — no C# events declared anywhere; tablet UI polls `EcosystemNetworkManagerGPU.Instance` every frame |
| 10 | Preset scenarios + complete core system | ❌ Not started |
| 11 | Debugging, testing, system balancing | ❌ Not started |
| 12 | Final optimisation, bug fixing, project completion | ❌ Not started |

---

## Codebase Structure

The project has **two separate systems** — the Ecosystem (active product) and the Simulation (older research/prototype). Do not confuse them.

```
Assets/Junheng/
├── Ecosystem/          ← THE ACTIVE PRODUCT
│   └── Scripts/
│       ├── Boids CPU/
│       │   ├── EcosystemSimulation.cs   Main manager (CPU) — PopulationTick coroutine IS active
│       │   ├── Boid.cs                  Individual fish
│       │   ├── BoidInfo.cs              Per-boid state struct
│       │   ├── BoidSwimmingUtility.cs   Physics integration
│       │   ├── BoidAffecter.cs          Target / obstacle affecters
│       │   ├── BoidSimulation.cs        Legacy single-species test controller (dead — test scene only)
│       │   └── SpatialPartition3D.cs    Spatial grid for neighbour queries
│       ├── ScriptableObjects/
│       │   ├── EcosystemDefinition.cs   Top-level asset — species list + bounds
│       │   ├── SpeciesDefinition.cs     Per-species data asset (population dynamics fields ARE active)
│       │   ├── BoidSchoolProperties.cs  Flocking weights + ranges
│       │   ├── BoidMovementProperties.cs Speed, turn rate, acceleration
│       │   └── SpeciesBehaviorProperties.cs  Predator/prey AI settings
│       ├── Simple Flocking/             ← DEAD — delete this folder + Fish.prefab + Simple Flock Test.unity
│       ├── UI/
│       │   ├── EcosystemUI.cs           Auto-builds species cards at runtime
│       │   ├── SpeciesCardUI.cs         Per-species card — +/− buttons, pop count
│       │   └── Editor/
│       │       └── EcosystemUIBuilder.cs  One-click scene hierarchy builder (Editor only)
│       └── Networking/
│           ├── NetworkBootstrap.cs          Host/Client role setup, starts NGO
│           ├── EcosystemNetworkManager.cs   CPU — syncs population via NetworkList, RPCs
│           ├── EcosystemNetworkManagerGPU.cs GPU — syncs GPU school counts via NetworkList, RPCs
│           ├── TabletEcosystemUI.cs         CPU tablet UI — spawns species cards
│           ├── TabletEcosystemUIGPU.cs      GPU tablet UI — reads EcosystemDefinitionGPU
│           ├── TabletSpeciesCardUI.cs       CPU card — add/remove RPC buttons
│           ├── TabletSpeciesCardUIGPU.cs    GPU card — add/remove RPC buttons
│           ├── ConnectionScreenUI.cs        Client IP input + connect button
│           └── HostSpawner.cs              Spawns network manager prefab on server start
│
└── Simulation/         ← RESEARCH SYSTEM — GPU boid simulation (also used as active GPU layer)
    └── Scripts/
        ├── Boids_GPU/
        │   ├── Ecosystem/                      ← GPU ECOSYSTEM LAYER (active — Boids_Demo scene)
        │   │   ├── SpeciesDataGPU.cs           Single source of truth per species (all 4 SOs + pop dynamics)
        │   │   ├── EcosystemDefinitionGPU.cs   Species list + simulation bounds asset
        │   │   ├── EcosystemSimulationGPU.cs   Autonomous tick cascade + add/remove API
        │   │   ├── SpeciesBehaviorPropertiesGPU.cs  Flee/hunt/hunger settings SO
        │   │   ├── WanderingAffecterGPU.cs     Random-wandering target for Apex species
        │   │   └── EcosystemUIAdapterGPU.cs    Thin wrapper — zero callers currently; may be deleted
        │   ├── BoidSpawnerGPUMultiTargets.cs   Reads SpeciesDataGPU for all spawn properties
        │   ├── BoidSimulationTargetAnimatorsSpawner.cs  GlobalScale field added
        │   ├── BoidSimulationBaseGPU.cs        + CleanupBaseGPUBuffers(), BoidsCount getter
        │   ├── BoidSpawnerGPU.cs               + CleanupSpawnData()
        │   ├── 01_Brute_Force_Normal_Rendering/     ← DEAD — educational comparison only
        │   ├── 02_Brute_Force_Instanced_Rendering/  ← DEAD — educational comparison only
        │   └── 03_Spatial_Partition_Instanced_Rendering/
        │       └── BoidSimulationGPU.cs        + ReinitializeBuffers()
        ├── Boids_CPU/           CPU boid simulation (dead — test scenes only, safe to delete)
        ├── Fish_Swimming_CPU/   Keyboard-controlled single fish (dead — test scene only)
        ├── Automatic_Fish_Swimming_CPU/  Target-following fish (dead — test scene only)
        ├── Other/
        │   └── TransformAnimator.cs     Animates targets along line/circle/rectangle paths
        ├── Packages/
        │   └── Shaders_Extensions/
        │       └── ComputeShaderExtensions.cs  ← DEAD DUPLICATE — delete (namespace GameDevBuddies)
        └── Shared/
            ├── BoidSpawnerBase.cs              + SetInitialGroupsCount(), SetBoidsCount(), RemoveTarget()
            ├── FishSwimmingUtility.cs
            ├── FishSwimmingMaterialUpdate.cs   ← IMPORTANT: drives shader animation from speed
            ├── FishMotionRenderProperties.cs   Min/max shader param ranges
            ├── FishMovementProperties.cs       Speed, turn, acceleration SO
            └── FishSchoolProperties.cs         Flocking weights SO
```

---

## What Is Currently Working

### CPU Ecosystem (EcosystemSimulation.cs)
- Initialises spatial partition grid, spawns all species via `AddSpecies`
- Per-frame: updates grid cells, runs each boid, cleans up dead boids
- **`PopulationTick` coroutine is active** — runs every `PopulationTickInterval` seconds applying logistic growth, natural death, and starvation. Reads all seven population-dynamics fields on `SpeciesDefinition`: `ReproductionRate`, `NaturalDeathRate`, `CarryingCapacity`, `StarvationDeathRate`, `StarvationThreshold`, `HealthyPreyRatio`, `RatioPressureStrength`
- **`AddSpecies(species, count)`** — spawns fish just outside a random boundary face
- **`RemoveSpecies(species, count)`** — sets fish to `Exiting` state, destroyed at boundary
- **`CountLiving(species)`** — public, used by UI cards

### GPU Ecosystem Layer (EcosystemSimulationGPU.cs)
- **`SpeciesDataGPU`** — one asset per species holds all simulation SOs (FishSchoolProperties, FishMovementProperties, FishMotionRenderProperties, SpeciesBehaviorPropertiesGPU) plus population dynamics fields (ReproductionRate, NaturalDeathRate, StarvationDeathRate, StarvationThreshold)
- **Autonomous population cascade** — coroutine ticks every 5s (configurable):
  - Births: logistic growth `reproRate × (1 - current/cap)`, slows near carrying capacity
  - Natural deaths: random chance per tick
  - Starvation: extra death chance when any prey species drops below `StarvationThreshold` fraction of its capacity
- **Fixed-target model** — targets never change at runtime; add/remove a school by scaling `BoidsCount` by `_boidsPerGroup` (derived from initial `BoidsCount / InitialGroupsCount` on Awake). Carrying capacity auto-derives from initial boid count — do not set it manually.
- **`AddSpecies` / `RemoveSpecies`** — public API for UI buttons and netcode RPCs
- **`CountGroups`** — returns `BoidsCount / boidsPerGroup` for UI display
- `BoidSpawnerGPUMultiTargets` reads all spawn properties from `SpeciesDataGPU`; `FishSchoolProperties` in Inspector can be left empty when `SpeciesData` is assigned

### GPU Netcode
- **Host/Client architecture** using Unity Netcode for GameObjects (NGO) over WiFi
- `NetworkBootstrap` — sets role (Host/Client), starts NGO, spawns `EcosystemNetworkManagerGPU`
- `EcosystemNetworkManagerGPU` — auto-finds `EcosystemSimulationGPU` on server; syncs school counts every 1s via `NetworkList<int>`; exposes `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc` (Server RPCs)
- `TabletEcosystemUIGPU` + `TabletSpeciesCardUIGPU` — client tablet UI; cards auto-built from `EcosystemDefinitionGPU`; buttons send RPCs; population label polls synced NetworkList
- Host verified working. Client scene in progress — NetworkConfig mismatch being resolved (ensure both scenes have identical Network Prefabs List)

### Boid.cs (CPU Ecosystem)
- **States:** `Schooling`, `Fleeing`, `Hunting`, `Dead`, `Entering`, `Exiting` (`Idle` is declared but never used)
- Same-species flocking: separation, alignment, cohesion
- Predator hunts when hunger above `HuntThreshold`, kills prey within `AttackRange`
- Prey flees when predator within `FleeRange`, panic timer keeps fleeing after losing sight
- `IsSolitary = true` disables flocking (used for sharks)

### TransformAnimator
- Animates target transforms along Line / Circle / Rectangle paths
- `GlobalScale` on `BoidSimulationTargetAnimatorsSpawner` uniformly scales all spawned path dimensions — adjust before hitting Create Targets

---

## What Has Been Tried and Removed

### Initial Null Reference Bug (fixed)
`EcosystemSimulation.Start()` originally called `SpawnAllSpecies()` before `BuildSpatialPartition()`. Fixed by swapping order.

---

## What Needs Building Next (Priority Order)

Recommended build order from the technical audit — events before health, clean-up before anything:

### 0. Clean-up sweep (half day — unblocks everything)
Delete confirmed-dead code before adding features. Key targets: `Ecosystem/Scripts/Simple Flocking/` folder + `Simple Flock Test.unity` + `Fish.prefab`, `Simulation/Scripts/Boids_GPU/01_Brute_Force_*/` and `02_Brute_Force_*/`, `Simulation/Packages/Shaders_Extensions/ComputeShaderExtensions.cs`. Full list in §3 and §6 of the audit section below.

### 1. Finish client netcode setup
Resolve NetworkConfig mismatch — both host and client NetworkManagers must have the **exact same Network Prefabs List**. Register `EcosystemNetworkManagerGPU` prefab on the client's NetworkManager. Test add/remove RPC round-trip between tablet and display.

### 2. Create remaining SpeciesDataGPU assets
Only `SpeciesData_Clownfish` exists. Need assets for:
- Golden Trevally (Mesopredator)
- Yellowtail Snapper (Prey)
- Giant Trevally (Apex)

Wire `PreySpecies` / `PredatorSpecies` lists on each. Add all four to `EcosystemDefinitionGPU`.

### 3. Event System — build this BEFORE the health system
```csharp
public static event Action<SpeciesDataGPU, int> OnPopulationChanged;
public static event Action<float>               OnHealthChanged;
public static event Action<EcosystemState>      OnStateChanged;
```
Fire from simulation on change. Lets the health system and UI team subscribe instead of polling every frame. File: `EcosystemEvents.cs` in `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/`.

### 4. Ecosystem Health Score + State Machine
Create `EcosystemHealth.cs` alongside `EcosystemSimulationGPU`:

**Health score (0–100) factors:**
- Biodiversity: fraction of species with living members
- Balance: each species within a healthy population range
- Apex predator presence: sharks weighted heavily
- Stability: rate of population change

**States:** Healthy → Unstable → Critical → Collapsing → Recovering

Files: `EcosystemHealth.cs` + `EcosystemState.cs` in `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/`.

### 5. Preset Scenarios
- **Balanced Ocean**, **Shark Removed**, **Overpopulation**, **Collapse**, **Recovery**
- Each is a method calling `AddSpecies` / `RemoveSpecies` against `EcosystemSimulationGPU` to reach a starting state
- File: `PresetScenarios.cs` in `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/`

### 6. Food Chain Overlay + Species Info Panel
- `SpeciesDataGPU` needs: `Sprite Icon`, `string Description`, `string DietDescription`
- Food chain overlay auto-generates from `PreySpecies` / `PredatorSpecies` lists
- File: `FoodChainOverlay.cs` → `Assets/Junheng/Ecosystem/Scripts/UI/`

### 7. FishAnimator — procedural animation for CPU boids
The GPU simulation has working procedural animation via `FishSwimmingMaterialUpdate.cs`. CPU boids currently move as rigid bodies with no animation.
- Need to create from scratch: `FishAnimationProperties.cs` (ScriptableObject), a field referencing it on `SpeciesDefinition`, and `FishAnimator.cs` driving CPU boid renderers using the same shader params as the GPU reference

---

## Scene Setup Reference

### Boids_Demo (host/display scene)
- `Boids_Simulation_GPU` GameObject: `BoidSimulationGPU` + `EcosystemSimulationGPU`
- 4 `BoidSpawnerGPUMultiTargets`: Clownfish, Yellowtail Snapper, Golden Trevally, Giant Trevally
- Giant Trevally: 1200 boids, 60 groups, 60 animated targets from `BoidSimulationTargetAnimatorsSpawner`
- `NetworkManager` GameObject: `NetworkManager` + `UnityTransport` + `NetworkBootstrap` (Role: Host)
- `EcosystemNetworkManagerGPU` prefab registered in NetworkManager's Network Prefabs List

### Tablet scene (client)
- `NetworkManager` GameObject: same components, Role: **Client**
- Same `EcosystemNetworkManagerGPU` prefab registered (must match host exactly)
- `ConnectionScreenUI` canvas for IP entry
- `TabletEcosystemUIGPU` with `EcosystemDefinitionGPU` asset, card prefab, card container

---

## Recommended Species Values

| Species | ReproRate | NaturalDeath | StarveRate | StarveThreshold |
|---------|-----------|--------------|------------|-----------------|
| Giant Trevally (Apex) | 0.02 | 0.01 | 0.30 | 0.20 |
| Golden Trevally (Mesopredator) | 0.12 | 0.03 | 0.25 | 0.15 |
| Yellowtail Snapper (Prey) | 0.20 | 0.05 | 0.00 | 0.00 |
| Clownfish (Prey) | 0.20 | 0.05 | 0.00 | 0.00 |

Carrying capacity is auto-derived from the spawner's initial `BoidsCount` at runtime — do not set it manually.

---

## Known Issues / Watchpoints

- **NetworkConfig mismatch** — client and host must have identical Network Prefabs Lists in their NetworkManager components
- **Only Clownfish SpeciesDataGPU exists** — other three species need assets created and added to `EcosystemDefinitionGPU`
- **Duplicate AudioListener** — multiple cameras in the scene. Keep exactly one AudioListener active
- **Synchronous GPU readback in `ReinitializeBuffers()`** — `readBuffer.GetData(_boidsInfos)` at [BoidSimulationGPU.cs:76](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/03_Spatial_Partition_Instanced_Rendering/BoidSimulationGPU.cs) blocks the CPU for 3–8 ms on every Add/Remove click. Convert to `AsyncGPUReadback.Request` before the demo
- **`SpatialPartitionGPU._visualizeOccupancy` defaults to `true`** — triggers a per-frame GPU readback. Safe in the two ship scenes (serialized to `0`), but will silently kill performance on any new `SpatialPartitionGPU` component. Flip default to `false`
- **Double wanderer initialisation** — `SetupApexTargets` calls `Initialize` twice on each wanderer; the second call randomises its position. Fix: remove the second `Initialize(bounds)` call in [EcosystemSimulationGPU.cs:231](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/EcosystemSimulationGPU.cs)
- **Per-frame `new MaterialPropertyBlock` in `BoidSpawnerGPU`** — ~480 GC allocs/sec with 4 spawners. Cache one per spawner at init
- **`ConnectionScreenUI` listener leak** — callbacks only unsubscribed inside themselves; if screen is destroyed mid-connect, listeners hold a stale reference. Add `OnDestroy` unsubscribe
- **`_grid.GetNearby()` allocates a new List each call** — acceptable now, optimise in Week 11/12

---

## Team Structure

| Role | Person |
|------|--------|
| Simulation / backend | JunHeng |
| UI and rendering | Separate teammates |

Each person has their own Claude session. Share context via this file and `CLAUDE.md` (project root), both committed to git.

---

## Technical Audit — 2026-05-26

Multi-lens audit across all 85 .cs files in `Assets/Junheng/`, using game-developer, monitoring-expert, code-reviewer, and architecture-designer perspectives. File:line references are clickable.

### 1. Critical Issues — Fix Before the Demo

1. **CPU population tick is still running despite HANDOFF claiming it was removed.** [EcosystemSimulation.cs:64](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Boids%20CPU/EcosystemSimulation.cs) starts `PopulationTick()` and the coroutine at [EcosystemSimulation.cs:278-347](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Boids%20CPU/EcosystemSimulation.cs) actively reads `ReproductionRate`, `NaturalDeathRate`, `CarryingCapacity`, `StarvationDeathRate`, `StarvationThreshold`, `HealthyPreyRatio`, `RatioPressureStrength`. If a designer ever runs the CPU Ecosystem scene, populations will autonomously cascade — exactly the behavior HANDOFF says was removed. Either delete the coroutine + the seven dormant fields on `SpeciesDefinition`, or update HANDOFF to reflect the current truth.
2. **Synchronous GPU readback inside `ReinitializeBuffers()`.** [BoidSimulationGPU.cs:76](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/03_Spatial_Partition_Instanced_Rendering/BoidSimulationGPU.cs) calls `readBuffer.GetData(_boidsInfos)` — a blocking CPU↔GPU stall. With ~5000 boids this is 3–8 ms per Add/Remove click and per population-tick rebuild. On a tablet, repeated taps will cause visible hitches. Convert to `AsyncGPUReadback.Request(...)` and defer the rebuild one frame.
3. **`SpatialPartitionGPU._visualizeOccupancy` defaults to `true`.** [SpatialPartitionGPU.cs:40](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/GPU_Spatial_Partition/SpatialPartitionGPU.cs) and the per-frame `GetData` at [SpatialPartitionGPU.cs:287](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/GPU_Spatial_Partition/SpatialPartitionGPU.cs). The two ship scenes (`Boids_Demo`, `Swirl_Demo`) correctly set it to `0`, but the default means anyone adding a new SpatialPartitionGPU component will silently introduce a per-frame GPU readback. Either flip the default to `false` or guard the entire block with `#if UNITY_EDITOR`.
4. **HANDOFF references `FishAnimationProperties` that does not exist anywhere.** Grep across `Assets/Junheng/` returns zero hits. The "Known Issues" entry "FishAnimationProperties type referenced in SpeciesDefinition but class does not exist — will throw compile error" is stale: [SpeciesDefinition.cs](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/ScriptableObjects/SpeciesDefinition.cs) has no such field today. No bug, but the note is misleading. Same for the "Sprint 8: FishAnimator" claim in "What needs building next" §5 — that work isn't blocked on anything; it just isn't started.
5. **Per-frame `new MaterialPropertyBlock` + `new RenderParams` per spawner.** [BoidSpawnerGPU.cs:105-108](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/BoidSpawnerGPU.cs). With 4 active spawners this is ~480 GC allocations/sec just to set the same buffer references each frame. Cache one `RenderParams` and one `MaterialPropertyBlock` per spawner at init, mutate fields in place.
6. **`EcosystemSimulationGPU.SetupApexTargets` double-initialises wanderers.** [EcosystemSimulationGPU.cs:230-231](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/EcosystemSimulationGPU.cs) calls `CreateAndInitWanderer(g)` (which already runs `Initialize` at line 249) then runs `wanderer.Initialize(bounds)` again on the returned instance. Each wanderer jumps to a fresh random position on the second call — wasted work and a visible spawn-position jitter for Apex schools.
7. **`ConnectionScreenUI` leaks netcode callbacks if destroyed mid-connect.** [ConnectionScreenUI.cs:65-66](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Networking/ConnectionScreenUI.cs) subscribes to `OnClientConnectedCallback` / `OnClientDisconnectCallback` and only unsubscribes inside the callbacks themselves. If the connect screen is destroyed (scene change, app pause) before either fires, the listeners hold a reference to the destroyed object. Add a matching `OnDestroy` unsubscribe.
8. **Two `ComputeShaderExtensions` classes coexist.** [Simulation/Scripts/Other/ComputeShaderExtensions.cs](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Other/ComputeShaderExtensions.cs) (namespace `OceanX`) and [Simulation/Packages/Shaders_Extensions/ComputeShaderExtensions.cs](OceanX%20MP/Assets/Junheng/Simulation/Packages/Shaders_Extensions/ComputeShaderExtensions.cs) (namespace `GameDevBuddies`) have identical method bodies. Compiles only because they live in different namespaces — confusing to readers, fragile under refactor. Delete the `GameDevBuddies` copy; nothing references it.

### 2. Optimisation Priorities — Ranked by Impact

| # | Item | Where | Estimated impact |
|---|------|-------|------------------|
| 1 | Convert `_boidsComputeBuffer.GetData` in `ReinitializeBuffers()` to `AsyncGPUReadback` | [BoidSimulationGPU.cs:68-89](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/03_Spatial_Partition_Instanced_Rendering/BoidSimulationGPU.cs) | Removes 3–8 ms stall per add/remove and per population tick. Most visible win on the tablet demo. |
| 2 | Cache `RenderParams` + `MaterialPropertyBlock` per spawner | [BoidSpawnerGPU.cs:103-119](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/BoidSpawnerGPU.cs) | Eliminates ~500 GC allocs/sec, smooths frame time tail. |
| 3 | Make `SpatialPartition3D<T>.GetNearby` reuse a single buffer list | [SpatialPartition3D.cs:51-66](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Boids%20CPU/SpatialPartition3D.cs) | At 500 boids × 60 fps removes ~30 k list allocs/sec. Crucial before scaling the CPU Ecosystem. |
| 4 | Cache `BoidSpawnerBase.Targets`/`Obstacles` arrays — getters currently `new SimulationAffecter[len]` every call | [BoidSpawnerBase.cs:26-31](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Shared/BoidSpawnerBase.cs), called every frame by [BoidSimulationBaseGPU.cs:209-238](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/BoidSimulationBaseGPU.cs) | ~8 array allocs per frame × array length 60 for Giant Trevally — measurable GC. |
| 5 | Replace per-frame `pop.ToString()` and `$"{n}%"` interpolations in card UIs with "only update if changed" | [SpeciesCardUI.cs:65-72](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/UI/SpeciesCardUI.cs), [TabletSpeciesCardUI.cs:34-45](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Networking/TabletSpeciesCardUI.cs), [TabletSpeciesCardUIGPU.cs:30-41](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Networking/TabletSpeciesCardUIGPU.cs), [EcosystemUI.cs:107-127](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/UI/EcosystemUI.cs) | ~240 string allocs/sec on the tablet. Naturally falls out of the event system in §5 of the sprint plan. |
| 6 | Population sync timer should use `WaitForSeconds` coroutine instead of every-Update accumulator | [EcosystemNetworkManagerGPU.cs:47-61](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Networking/EcosystemNetworkManagerGPU.cs), [EcosystemNetworkManager.cs:49-63](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Networking/EcosystemNetworkManager.cs) | Removes work from the per-frame budget. Minor. |
| 7 | `TransformAnimatorSpeedCorrection.Update` rewrites every animator's speed every frame | [TransformAnimatorSpeedCorrection.cs:16-36](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Other/TransformAnimatorSpeedCorrection.cs) | Switch to an event fired only when `SimulationSpeed` actually changes. |
| 8 | `Boid.UpdateBoid` in Sim/Boids_CPU allocates two `List<SimulationAffecter>` per boid per frame | [Boid.cs:76-87](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_CPU/Boid.cs) | Catastrophic if this path is ever revived (60 k allocs/sec at 500 boids). Either fix or delete the file — see §3. |

### 3. Dead / Redundant Code — Safe to Delete

These files exist but are not referenced by any scene or script on the active demo path (`Boids_Demo.unity` → `EcosystemSimulationGPU` → `BoidSimulationGPU` (03_Spatial_Partition) → its spawners). Each was confirmed dead by ripgrep across `Assets/Junheng/`.

- **`Ecosystem/Scripts/Simple Flocking/` — entire folder**: `Fish.cs`, `FishSchool.cs`, `FishTools.cs`, `MathTools.cs`, `Drawer.cs`, `Trace.cs`, `WayPoint.cs`. Independent Craig-Reynolds boid implementation using `Physics.OverlapSphere` (very slow). Only referenced by `Assets/Junheng/Ecosystem/Scenes/Simple Flock Test.unity` and `Assets/Junheng/Ecosystem/Prefabs/Fish.prefab`. Delete the folder, the test scene, and the prefab together.
- **`Ecosystem/Scripts/Boids CPU/BoidSimulation.cs`**: legacy single-species CPU controller using `Boid.Initialize()`. Only referenced by `CPU Flocking Test.unity`. `EcosystemSimulation` does not use it.
- **`Simulation/Scripts/Boids_CPU/`** — `Boid.cs`, `BoidInformation.cs`, `BoidSimulationCPU.cs`, `BoidSpawnData.cs`, `BoidSpawner.cs`, `BoidSpawnerRandom.cs`, `SpatialPartition3D.cs`. Used only by test scenes (`CPU Flocking Test.unity` and similar). The active product uses the GPU path. Keep only if the team still actively runs the test scenes.
- **`Simulation/Scripts/Boids_GPU/01_Brute_Force_Normal_Rendering/BoidSimulationGPU.cs`** and **`02_Brute_Force_Instanced_Rendering/BoidSimulationGPU.cs`**: older comparison versions. Not referenced from the active scene; only self-referencing. Educational value only.
- **`Simulation/Scripts/Boids_GPU/GPU_Spatial_Partition/SpatialPartitionGPUTester.cs`**: standalone test harness. No active scene uses it.
- **`Simulation/Scripts/Automatic_Fish_Swimming_CPU/`** + **`Simulation/Scripts/Fish_Swimming_CPU/`**: single-fish keyboard/target demos. Not on the active path.
- **`Simulation/Packages/Shaders_Extensions/ComputeShaderExtensions.cs`** (namespace `GameDevBuddies`): duplicate of `Simulation/Scripts/Other/ComputeShaderExtensions.cs`. Nothing imports `GameDevBuddies`.
- **`Ecosystem/Scripts/Boids CPU/SpatialPartition3D.cs`** vs **`Simulation/Scripts/Boids_CPU/SpatialPartition3D.cs`**: near-identical generic grid. After deleting the Sim/Boids_CPU folder, the duplicate naturally goes away.
- **Dead public hooks** (commented "ECOSYSTEM HOOK — do not remove" but actually unused): `BoidSimulationBaseGPU.BoidsCount` getter at [BoidSimulationBaseGPU.cs:119](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/BoidSimulationBaseGPU.cs), `BoidSpawnerBase.SetInitialGroupsCount` at [BoidSpawnerBase.cs:114](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Shared/BoidSpawnerBase.cs), `BoidSpawnerBase.RemoveTarget` at [BoidSpawnerBase.cs:122](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Shared/BoidSpawnerBase.cs). `EcosystemSimulationGPU` reads `spawner.SpawnData.BoidsCount` directly instead. Remove the misleading "do not remove" comments at minimum, ideally the methods too.
- **Dead enum value**: `BoidBehaviorState.Idle` at [BoidInfo.cs:9](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Boids%20CPU/BoidInfo.cs). Never written or read.
- **Dead SO fields**: `SpeciesBehaviorProperties.HuntWeight` / `FleeWeight` (CPU and GPU variants). Set in asset files but never read by `Boid.cs` or any compute shader extraction.
- **Dead UI field**: `TabletSpeciesCardUI.SpeciesIcon` and `SpeciesCardUI.SpeciesIcon` — exposed in the prefab but `Initialize` doesn't assign a sprite (see commented-out lines at [SpeciesCardUI.cs:51-52](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/UI/SpeciesCardUI.cs)).
- **`EcosystemUIAdapterGPU.cs`**: only references itself. The two tablet card UIs talk to `EcosystemNetworkManagerGPU.Instance` directly, bypassing the adapter. Either wire it through, or delete it.

### 4. Architecture Recommendations

1. **Pick one ecosystem implementation and retire the other.** The CPU `EcosystemSimulation` and the GPU `EcosystemSimulationGPU` solve the same problem with different data models, parallel SO trees (`SpeciesDefinition` vs `SpeciesDataGPU`), parallel network managers (`EcosystemNetworkManager` vs `EcosystemNetworkManagerGPU`), and parallel UI stacks (`TabletEcosystemUI` vs `TabletEcosystemUIGPU`). Every new feature has to be built twice. Recommendation: declare the GPU pipeline the product, delete the CPU `EcosystemSimulation`, `Boid.cs`, `BoidInfo.cs`, `BoidSimulation.cs`, `BoidAffecter.cs`, `BoidSwimmingUtility.cs`, the CPU `EcosystemNetworkManager`, `TabletEcosystemUI`, `TabletSpeciesCardUI`, `SpeciesCardUI`, `EcosystemUI`, and the CPU `Species*` SOs after the next demo. The CPU path appears unused in the active scene.
2. **Drop the `EcosystemUIAdapterGPU` indirection.** It currently adds a layer with zero callers — the tablet cards talk to `EcosystemNetworkManagerGPU.Instance` directly. If the team wants this kind of bridge for a future local-only UI, keep it; otherwise remove it to flatten the call graph.
3. **Extract a shared base for the two network managers and two tablet UIs.** `EcosystemNetworkManager(GPU)` are ~95% identical; same for `TabletEcosystemUI(GPU)` and `TabletSpeciesCardUI(GPU)`. Once recommendation #1 above is acted on this disappears naturally — but if both paths must persist for now, lift the common code into an abstract base parameterised by the species-data type.
4. **Introduce the event system before the health system.** The sprint plan lists health (Week 6) before events (Week 9). In practice the health calculation is read by UI every frame today ([EcosystemUI.cs:72-127](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/UI/EcosystemUI.cs)); building events first means the health value can fire once per change and be cached by every subscriber. Order: events → health → state machine → presets.
5. **Network architecture is correctly scoped for the demo.** Server-authoritative `EcosystemSimulationGPU`, `NetworkList<int>` for population fan-out at 1 Hz, two RPCs for add/remove. `RpcInvokePermission.Everyone` is acceptable on a closed-WiFi tablet-to-display demo with one client. Do not invest in matchmaking, lobbies, or anti-cheat — out of scope.

### 5. Revised Sprint Plan

Updated from the audit. ✅ = done, 🔶 = partial, ❌ = not started, 🟢 = more complete than previously recorded, 🔴 = less complete than previously recorded.

| Week | Sprint | Previous status | Revised status | Reason |
|------|--------|-----------------|----------------|--------|
| 1 | Research and concept development | ✅ | ✅ | — |
| 2 | Planning, system design, task allocation | ✅ | ✅ | — |
| 3 | Core simulation manager + species data system | ✅ | ✅ | Both CPU and GPU pipelines have a manager + species SO. |
| 4 | Spawning, removal, population tracking | ✅ | ✅ | `AddSpecies` / `RemoveSpecies` work on both pipelines; `CountLiving` / `CountGroups` wired. |
| 5 | Food chain relationships + predator-prey logic | ✅ | 🟢 ✅ | Confirmed in [Boid.cs:330-338](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Boids%20CPU/Boid.cs) (CPU hunt/flee state machine works); GPU side is data-only — relationships present on SO but no GPU kernel reads them yet. CPU path is the source of truth here. |
| 6 | Population growth/decline + ecosystem health system | 🔶 (health not built) | 🟢 partial (logistic growth + starvation + ratio pressure live on CPU; logistic + natural-death + starvation live on GPU. Health score still absent) | CPU `PopulationTick` is fully wired contrary to HANDOFF claim; GPU `PopulationTickRoutine` is also wired. Only the health score / state machine remain. |
| 7 | Cascading effects + ecosystem state machine | 🔶 | 🟢 partial (cascade is implicit in both ticks; state machine still not started) | Cascade is emergent from the dynamics; state machine TBD. |
| 8 | Movement systems — flocking + predator behaviour | ✅ | ✅ | — |
| 9 | Event system + integration hooks for UI | 🔶 | 🔴 ❌ not started | No C# events declared anywhere (`OnPopulationChanged`, `OnHealthChanged`, `OnStateChanged` don't exist). Tablet UI polls `Instance.GetPopulation` every frame. Promote ahead of health work. |
| 10 | Preset scenarios | ❌ | ❌ | Unchanged. Trivial once events are in place. |
| 11 | Debugging, testing, system balancing | ❌ | ❌ | Should fold the audit's clean-up checklist in here. |
| 12 | Final optimisation, bug fixing, project completion | ❌ | ❌ | Targets the optimisation list in §2. |

**Recommended order for the remaining work**:
1. Clean-up sweep (delete dead code from §3) — half a day, unblocks every other touch.
2. Event system (Sprint 9) — small, makes Sprint 6/7/10 trivial.
3. `EcosystemHealth.cs` + state machine (Sprint 6 / 7) — subscribes to the new events.
4. Preset scenarios (Sprint 10) — call `AddSpecies` / `RemoveSpecies` against `EcosystemSimulationGPU`.
5. Optimisation pass (Sprint 11/12) — items #1–#4 from §2.

**File locations for new work**:
- `EcosystemHealth.cs` → `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/EcosystemHealth.cs` (namespace `OceanX.BoidsGPU.Ecosystem`, alongside `EcosystemSimulationGPU`).
- `EcosystemEvents.cs` (static event hub) → same folder.
- `EcosystemState.cs` (enum + state machine) → same folder.
- `PresetScenarios.cs` → `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/PresetScenarios.cs`.
- `FoodChainOverlay.cs` + species-info panel → `Assets/Junheng/Ecosystem/Scripts/UI/` (UI is shared across both pipelines).

### 6. Clean-up Checklist

Tick these off as future sessions resolve them.

**Critical (block the demo):**
- [ ] Decide CPU `PopulationTick` fate — delete it or update HANDOFF so it's documented as the current behavior. ([EcosystemSimulation.cs:64,278-347](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Boids%20CPU/EcosystemSimulation.cs))
- [ ] Convert `ReinitializeBuffers()` to `AsyncGPUReadback`. ([BoidSimulationGPU.cs:68-89](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/03_Spatial_Partition_Instanced_Rendering/BoidSimulationGPU.cs))
- [ ] Flip `_visualizeOccupancy` default to `false` and gate the readback with `#if UNITY_EDITOR`. ([SpatialPartitionGPU.cs:40,281-288](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/GPU_Spatial_Partition/SpatialPartitionGPU.cs))
- [ ] Remove the double `Initialize(bounds)` on wanderers in `SetupApexTargets`. ([EcosystemSimulationGPU.cs:230-231,249](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/EcosystemSimulationGPU.cs))
- [ ] Cache `RenderParams` + `MaterialPropertyBlock` per spawner. ([BoidSpawnerGPU.cs:103-119](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Boids_GPU/BoidSpawnerGPU.cs))
- [ ] Unsubscribe `OnClientConnectedCallback` / `OnClientDisconnectCallback` in `ConnectionScreenUI.OnDestroy`. ([ConnectionScreenUI.cs:65-66](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Networking/ConnectionScreenUI.cs))
- [ ] Delete duplicate `Simulation/Packages/Shaders_Extensions/ComputeShaderExtensions.cs`.

**Stale documentation:**
- [x] Remove the "FishAnimationProperties referenced but missing" entry from HANDOFF "Known Issues" — the field does not exist in `SpeciesDefinition`.
- [x] Update HANDOFF "What Has Been Tried and Removed" — CPU `PopulationTick` is not removed.
- [x] Mark CPU `SpeciesDefinition` population-dynamics fields as *active on the CPU path*, not "dormant".

**Dead code to delete (safe — no active references):**
- [ ] `Ecosystem/Scripts/Simple Flocking/` folder + `Simple Flock Test.unity` scene + `Fish.prefab`.
- [ ] `Ecosystem/Scripts/Boids CPU/BoidSimulation.cs`.
- [ ] `Simulation/Scripts/Boids_GPU/01_Brute_Force_Normal_Rendering/`.
- [ ] `Simulation/Scripts/Boids_GPU/02_Brute_Force_Instanced_Rendering/`.
- [ ] `Simulation/Scripts/Boids_GPU/GPU_Spatial_Partition/SpatialPartitionGPUTester.cs`.
- [ ] `Simulation/Scripts/Boids_CPU/` folder (if test scenes are not actively used).
- [ ] `Simulation/Scripts/Automatic_Fish_Swimming_CPU/` and `Simulation/Scripts/Fish_Swimming_CPU/`.
- [ ] `EcosystemUIAdapterGPU.cs` (no callers).
- [ ] `BoidSimulationBaseGPU.BoidsCount` getter (no callers despite "do not remove" comment).
- [ ] `BoidSpawnerBase.SetInitialGroupsCount` and `BoidSpawnerBase.RemoveTarget` (no callers).
- [ ] `BoidBehaviorState.Idle` enum value.
- [ ] `SpeciesBehaviorProperties.HuntWeight` and `FleeWeight` (CPU and GPU). Read by no kernel.
- [ ] `SpeciesDefinition.SpawnRadius` (never read — `EcosystemSimulation` uses its own `SpawnOffsetDistance`).
- [ ] `SpeciesCardUI.SpeciesIcon` and `TabletSpeciesCardUI(GPU).SpeciesIcon` exposed-but-unassigned fields.

**Optimisation backlog (Sprint 11/12):**
- [ ] Reuse one `List<T>` inside `SpatialPartition3D.GetNearby` instead of allocating per call. ([SpatialPartition3D.cs:51-66](OceanX%20MP/Assets/Junheng/Ecosystem/Scripts/Boids%20CPU/SpatialPartition3D.cs))
- [ ] Cache `Targets`/`Obstacles` arrays on `BoidSpawnerBase` instead of allocating on every getter call. ([BoidSpawnerBase.cs:26-31,136-149](OceanX%20MP/Assets/Junheng/Simulation/Scripts/Shared/BoidSpawnerBase.cs))
- [ ] Replace per-frame `pop.ToString()` on tablet cards with cached "last value" comparison.
- [ ] Replace per-frame `Update` polling in card UIs with event subscriptions (after Sprint 9 lands).
- [ ] Replace `EcosystemNetworkManager(GPU)` per-frame timer with a `WaitForSeconds` coroutine.
- [ ] Make `TransformAnimatorSpeedCorrection` event-driven on simulation-speed change.

**Architecture decisions to make:**
- [ ] Decide: deprecate CPU `EcosystemSimulation` pipeline entirely, or maintain both? If deprecating, fold into the clean-up sweep.
- [ ] Decide: keep `EcosystemUIAdapterGPU` and wire the tablet UI through it, or delete?
- [ ] Decide: extract a shared `EcosystemNetworkManagerBase<TSim, TData>` if both pipelines persist.

**New work (file locations confirmed):**
- [ ] `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/EcosystemEvents.cs` — static C# events.
- [ ] `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/EcosystemHealth.cs` — health score.
- [ ] `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/EcosystemState.cs` — state machine enum + transitions.
- [ ] `Assets/Junheng/Simulation/Scripts/Boids_GPU/Ecosystem/PresetScenarios.cs` — Balanced / Shark Removed / Overpopulation / Collapse / Recovery.
- [ ] `Assets/Junheng/Ecosystem/Scripts/UI/FoodChainOverlay.cs` — overlay UI driven by `SpeciesDataGPU.PreySpecies` / `PredatorSpecies`.
