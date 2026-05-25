# OceanX MP — Handoff Document
_Last updated: Week 7 of 12_

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
| 6 | Population growth/decline + ecosystem health system | 🔶 Partial — health score not yet built |
| 7 | Cascading effects + ecosystem state machine | 🔶 Partial — GPU cascade done, state machine not started |
| 8 | Movement systems — flocking + predator behaviour | ✅ Done (completed Week 5) |
| 9 | Event system + integration hooks for UI | 🔶 Partial — netcode sync working, C# events not yet wired |
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
│       │   ├── EcosystemSimulation.cs   Main manager (CPU)
│       │   ├── Boid.cs                  Individual fish
│       │   ├── BoidInfo.cs              Per-boid state struct
│       │   ├── BoidSwimmingUtility.cs   Physics integration
│       │   ├── BoidAffecter.cs          Target / obstacle affecters
│       │   ├── BoidSimulation.cs        Legacy single-species test controller
│       │   └── SpatialPartition3D.cs    Spatial grid for neighbour queries
│       ├── ScriptableObjects/
│       │   ├── EcosystemDefinition.cs   Top-level asset — species list + bounds
│       │   ├── SpeciesDefinition.cs     Per-species data asset
│       │   ├── BoidSchoolProperties.cs  Flocking weights + ranges
│       │   ├── BoidMovementProperties.cs Speed, turn rate, acceleration
│       │   └── SpeciesBehaviorProperties.cs  Predator/prey AI settings
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
        │   ├── Ecosystem/                      ← GPU ECOSYSTEM LAYER (new, Week 7)
        │   │   ├── SpeciesDataGPU.cs           Single source of truth per species (all 4 SOs + pop dynamics)
        │   │   ├── EcosystemDefinitionGPU.cs   Species list + simulation bounds asset
        │   │   ├── EcosystemSimulationGPU.cs   Autonomous tick cascade + add/remove API
        │   │   ├── SpeciesBehaviorPropertiesGPU.cs  Flee/hunt/hunger settings SO
        │   │   ├── WanderingAffecterGPU.cs     Random-wandering target for Apex species
        │   │   └── EcosystemUIAdapterGPU.cs    Thin wrapper for UI button wiring
        │   ├── BoidSpawnerGPUMultiTargets.cs   Reads SpeciesDataGPU for all spawn properties
        │   ├── BoidSimulationTargetAnimatorsSpawner.cs  GlobalScale field added
        │   ├── BoidSimulationBaseGPU.cs        + CleanupBaseGPUBuffers(), BoidsCount getter
        │   ├── BoidSpawnerGPU.cs               + CleanupSpawnData()
        │   └── 03_Spatial_Partition_Instanced_Rendering/
        │       └── BoidSimulationGPU.cs        + ReinitializeBuffers()
        ├── Boids_CPU/           CPU boid simulation with BoidSpawner
        ├── Fish_Swimming_CPU/   Keyboard-controlled single fish
        ├── Automatic_Fish_Swimming_CPU/  Target-following fish
        ├── Other/
        │   └── TransformAnimator.cs     Animates targets along line/circle/rectangle paths
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
- **`AddSpecies(species, count)`** — spawns fish just outside a random boundary face
- **`RemoveSpecies(species, count)`** — sets fish to `Exiting` state, destroyed at boundary
- **`CountLiving(species)`** — public, used by UI cards

### GPU Ecosystem Layer (EcosystemSimulationGPU.cs) ← NEW Week 7
- **`SpeciesDataGPU`** — one asset per species holds all simulation SOs (FishSchoolProperties, FishMovementProperties, FishMotionRenderProperties, SpeciesBehaviorPropertiesGPU) plus population dynamics fields (ReproductionRate, NaturalDeathRate, StarvationDeathRate, StarvationThreshold)
- **Autonomous population cascade** — coroutine ticks every 5s (configurable):
  - Births: logistic growth `reproRate × (1 - current/cap)`, slows near carrying capacity
  - Natural deaths: random chance per tick
  - Starvation: extra death chance when any prey species drops below `StarvationThreshold` fraction of its capacity
- **Fixed-target model** — targets never change at runtime; add/remove a school by scaling `BoidsCount` by `_boidsPerGroup` (derived from initial `BoidsCount / InitialGroupsCount` on Awake). Carrying capacity auto-derives from initial boid count — nothing to set manually.
- **`AddSpecies` / `RemoveSpecies`** — public API for UI buttons and netcode RPCs
- **`CountGroups`** — returns `BoidsCount / boidsPerGroup` for UI display
- `BoidSpawnerGPUMultiTargets` reads all spawn properties from `SpeciesDataGPU`; `FishSchoolProperties` in Inspector can be left empty when `SpeciesData` is assigned

### GPU Netcode ← NEW Week 7
- **Host/Client architecture** using Unity Netcode for GameObjects (NGO) over WiFi
- `NetworkBootstrap` — sets role (Host/Client), starts NGO, spawns `EcosystemNetworkManagerGPU`
- `EcosystemNetworkManagerGPU` — auto-finds `EcosystemSimulationGPU` on server; syncs school counts every 1s via `NetworkList<int>`; exposes `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc` (Server RPCs)
- `TabletEcosystemUIGPU` + `TabletSpeciesCardUIGPU` — client tablet UI; cards auto-built from `EcosystemDefinitionGPU`; buttons send RPCs; population label polls synced NetworkList
- Host verified working. Client scene in progress — NetworkConfig mismatch being resolved (ensure both scenes have identical Network Prefabs List)

### Boid.cs (CPU Ecosystem)
- **States:** `Schooling`, `Fleeing`, `Hunting`, `Idle`, `Dead`, `Entering`, `Exiting`
- Same-species flocking: separation, alignment, cohesion
- Predator hunts when hunger above `HuntThreshold`, kills prey within `AttackRange`
- Prey flees when predator within `FleeRange`, panic timer keeps fleeing after losing sight
- `IsSolitary = true` disables flocking (used for sharks)

### TransformAnimator
- Animates target transforms along Line / Circle / Rectangle paths
- `GlobalScale` on `BoidSimulationTargetAnimatorsSpawner` uniformly scales all spawned path dimensions — adjust before hitting Create Targets

---

## What Has Been Tried and Removed

### Population Tick System — CPU (removed)
A coroutine running every `PopulationTickInterval` seconds applying logistic growth, natural death, and starvation. Removed from the CPU system because manual add/remove via UI buttons is the right interaction model — autonomous dynamics conflicted with user agency.

The equivalent system **has been implemented on the GPU side** (`EcosystemSimulationGPU`) where it runs alongside user-driven add/remove rather than replacing it.

### Initial Null Reference Bug (fixed)
`EcosystemSimulation.Start()` originally called `SpawnAllSpecies()` before `BuildSpatialPartition()`. Fixed by swapping order.

---

## What Needs Building Next (Priority Order)

### 1. Finish client netcode setup
Resolve NetworkConfig mismatch — both host and client NetworkManagers must have the **exact same Network Prefabs List**. Register `EcosystemNetworkManagerGPU` prefab on the client's NetworkManager. Test add/remove RPC round-trip between tablet and display.

### 2. Create remaining SpeciesDataGPU assets
Only `SpeciesData_Clownfish` exists. Need assets for:
- Golden Trevally (Mesopredator)
- Yellowtail Snapper (Prey)
- Giant Trevally (Apex)

Wire `PreySpecies` / `PredatorSpecies` lists on each. Add all four to `EcosystemDefinitionGPU`.

### 3. Ecosystem Health Score + State Machine
Create `EcosystemHealth.cs` alongside `EcosystemSimulation`:

**Health score (0–100) factors:**
- Biodiversity: fraction of species with living members
- Balance: each species within a healthy population range
- Apex predator presence: sharks weighted heavily
- Stability: rate of population change

**States:** Healthy → Unstable → Critical → Collapsing → Recovering

### 4. Event System (UI Integration Bridge)
```csharp
public static event Action<SpeciesDefinition, int> OnPopulationChanged;
public static event Action<float>                  OnHealthChanged;
public static event Action<EcosystemState>         OnStateChanged;
```
Fire from simulation on change. Lets UI team subscribe instead of polling every frame.

### 5. FishAnimator — procedural animation for CPU boids
The GPU simulation already has working procedural animation via shader params. The CPU Ecosystem boids move as rigid bodies with no animation.
- `FishSwimmingMaterialUpdate.cs` is the working reference implementation
- `SpeciesDefinition.AnimationProperties` field exists and is waiting — assign a new `FishAnimationProperties` ScriptableObject

### 6. Food Chain Overlay + Species Info Panel
- `SpeciesDefinition` needs: `Sprite Icon`, `string Description`, `string DietDescription`
- Food chain overlay auto-generates from `PreySpecies` / `PredatorSpecies` lists

### 7. Preset Scenarios
- **Balanced Ocean**, **Shark Removed**, **Overpopulation**, **Collapse**, **Recovery**
- Each is a method calling `RemoveSpecies` / `AddSpecies` to reach a starting state

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
- **Population dynamics fields on CPU `SpeciesDefinition` are dormant** — `ReproductionRate`, `CarryingCapacity` etc. exist but nothing reads them. Ready for health system
- **`FishAnimationProperties` type referenced in `SpeciesDefinition` but class does not exist** — will throw compile error if a script tries to use it before creation
- **`_grid.GetNearby()` allocates a new List each call** — acceptable now, optimise in Week 11/12

---

## Team Structure

| Role | Person |
|------|--------|
| Simulation / backend | JunHeng |
| UI and rendering | Separate teammates |

Each person has their own Claude session. Share context via this file and `CLAUDE.md` (project root), both committed to git.
