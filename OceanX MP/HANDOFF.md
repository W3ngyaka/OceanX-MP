# OceanX MP — Handoff Document
_Last updated: Week 6 of 12_

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
| 7 | Cascading effects + ecosystem state machine | ❌ Not started |
| 8 | Movement systems — flocking + predator behaviour | ✅ Done (completed Week 5) |
| 9 | Event system + integration hooks for UI | ❌ Not started |
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
│       │   ├── EcosystemSimulation.cs   Main manager
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
│       └── UI/
│           ├── EcosystemUI.cs           Auto-builds species cards at runtime
│           ├── SpeciesCardUI.cs         Per-species card — +/− buttons, pop count
│           └── Editor/
│               └── EcosystemUIBuilder.cs  One-click scene hierarchy builder (Editor only)
│
└── Simulation/         ← OLDER RESEARCH SYSTEM — keep for reference, do not replace
    └── Scripts/
        ├── Boids_CPU/           CPU boid simulation with BoidSpawner
        ├── Boids_GPU/           GPU compute shader variant
        ├── Fish_Swimming_CPU/   Keyboard-controlled single fish (FishSwimmingControllerCPU)
        ├── Automatic_Fish_Swimming_CPU/  Target-following fish (AutomaticFishSwimming)
        └── Shared/
            ├── FishSwimmingUtility.cs        Same physics as Ecosystem BoidSwimmingUtility
            ├── FishSwimmingMaterialUpdate.cs ← IMPORTANT: drives shader animation from speed
            ├── FishMotionRenderProperties.cs  Min/max shader param ranges (ScriptableObject)
            ├── FishMovementProperties.cs      Same as BoidMovementProperties (OceanX namespace)
            └── FishSchoolProperties.cs        Same as BoidSchoolProperties (OceanX namespace)
```

---

## What Is Currently Working

### EcosystemSimulation.cs
- Initialises spatial partition grid, then spawns all species via `AddSpecies`
- Per-frame: updates grid cells, runs each boid, cleans up dead boids
- **`AddSpecies(species, count)`** — spawns fish just outside a random boundary face, fish swim inward via `Entering` state, join normal behaviour once inside
- **`RemoveSpecies(species, count)`** — picks random fish, sets them to `Exiting` state (swim outward), destroyed when they cross the boundary
- **`CountLiving(species)`** — public, used by UI cards

### Boid.cs (Ecosystem)
- **States:** `Schooling`, `Fleeing`, `Hunting`, `Idle`, `Dead`, `Entering`, `Exiting`
- Entering/Exiting fish ignore all food chain and flocking logic — they just swim to their target/direction
- Same-species flocking: separation, alignment, cohesion in a single neighbour pass
- Predator: grows hunger over time, hunts when above `HuntThreshold`, kills prey within `AttackRange` via `TryKill()`
- Prey: flees when predator within `FleeRange`, panic timer keeps fleeing after losing sight
- Solitary flag (`IsSolitary = true`) disables flocking — used for sharks

### SpeciesDefinition.cs
Per-species ScriptableObject with:
- Identity (name, role: Predator/Prey/Neutral)
- Prefab + `FishAnimationProperties` (field exists, **script not yet created**)
- Population (DefaultPopulation, SpawnRadius)
- BoidSchoolProperties + SpeciesBehaviorProperties
- Predator-Prey lists (PreySpecies, PredatorSpecies)
- **Population Dynamics fields** (added but not used — tick system was removed):
  `ReproductionRate`, `NaturalDeathRate`, `CarryingCapacity`, `StarvationDeathRate`, `StarvationThreshold`

### UI (EcosystemUI + SpeciesCardUI)
- `EcosystemUI` reads `EcosystemDefinition` on Start, instantiates one `SpeciesCardUI` per species
- Each card refreshes population count every frame, greys out Remove button at 0
- Eco health bar present — uses placeholder formula (average pop/cap ratio)
- `EcosystemUIBuilder` (Editor script, `OceanX > Build Ecosystem UI`) builds the full Canvas hierarchy in one click — **delete after use**

### Simulation System (Older — for reference)
- `FishSwimmingMaterialUpdate.cs` is a **complete, working procedural animation driver**
- It reads `acceleration` and `angularAcceleration` from the boid and pushes 5 shader properties:
  - `_SideToSideAmplitude` — body wave width
  - `_YawRotationAmplitude` — head yaw
  - `_TailRollAmplitude` — tail roll
  - `_TailYawAmplitude` — tail yaw/sweep
  - `_CurrentSwimTime` — drives the sine wave on the shader
- `FishMotionRenderProperties` is a ScriptableObject that stores min/max values for each param
- This system is **already proven** and needs to be wired into the Ecosystem boids

---

## What Has Been Tried and Removed

### Population Tick System (removed)
A coroutine running every `PopulationTickInterval` seconds that:
- Calculated births via logistic growth: `pop × reproRate × (1 - pop/carryingCapacity)`
- Applied natural death rate per tick
- Applied starvation death rate if prey species dropped below a threshold ratio
- Called `SpawnAdditional` / `KillRandom` to adjust live boid counts

**Why removed:** The team decided manual add/remove via UI buttons is the right interaction model for this educational tool. Autonomous population dynamics conflicted with user agency — the user should be the one causing the cascade, not a background timer.

The data fields (`ReproductionRate`, `CarryingCapacity`, etc.) remain on `SpeciesDefinition` as they may be re-used for the health score calculation or UI display.

### Initial Null Reference Bug (fixed)
`EcosystemSimulation.Start()` originally called `SpawnAllSpecies()` before `BuildSpatialPartition()`. Since `AddSpecies` (called by `SpawnAllSpecies`) immediately adds boids to `_grid`, and `_grid` was null, this caused a NullReferenceException on every run.

**Fix:** Swap order — `BuildSpatialPartition()` must be called before `SpawnAllSpecies()`. Also removed the redundant boid-add loop from `BuildSpatialPartition` since `AddSpecies` already handles it.

---

## What Needs Building Next (Priority Order)

### 1. FishAnimator — connect procedural animation to Ecosystem boids
**Why first:** Visual quality. The fish currently move as rigid bodies with no swimming animation.

The Simulation system already has a complete, working solution:
- `FishSwimmingMaterialUpdate.cs` drives shader params from acceleration
- `FishMotionRenderProperties.cs` is the tuning ScriptableObject

**What to do:**
- Create `FishAnimationProperties.cs` (ScriptableObject, OceanX/Ecosystem menu) — wraps `FishMotionRenderProperties`-style min/max params
- Create `FishAnimator.cs` — attach to boid prefab, add a `FishSwimmingMaterialUpdate`-style update each frame reading `_boidInfo.Acceleration` and `_boidInfo.AngularAcceleration` from the parent Boid
- `SpeciesDefinition.AnimationProperties` field is already wired and waiting — assign the asset

### 2. Ecosystem Health Score + State Machine
**Why second:** This is the feedback loop the learner needs to understand what their actions caused.

Create `EcosystemHealth.cs` as a component alongside `EcosystemSimulation`:

**Health score (0–100) factors:**
- Biodiversity: fraction of species with at least one living member
- Balance: each species in a healthy population range (not too low, not at cap)
- Apex predator presence: sharks weighted heavily (core learning point)
- Stability: how fast populations are changing

**States:**
| State | Condition |
|-------|-----------|
| Healthy | Score > 75, all species present |
| Unstable | Score 50–75 or any species below 20% of cap |
| Critical | Apex predator extinct or score < 50 |
| Collapsing | Two+ species in rapid decline |
| Recovering | Was Critical/Collapsing, score trending upward |

### 3. Event System (UI Integration Bridge)
**Why third:** Lets UI team subscribe to simulation changes without polling.

```csharp
public static event Action<SpeciesDefinition, int> OnPopulationChanged;
public static event Action<float>                  OnHealthChanged;
public static event Action<EcosystemState>         OnStateChanged;
```

Fire from `EcosystemSimulation` when population changes, from `EcosystemHealth` when score or state changes. UI cards and health bar switch from per-frame polling to event-driven updates.

### 4. Food Chain Overlay + Species Info Panel
**Why fourth:** Directly serves the educational goal — learner taps an animal and reads what it eats and what eats it.

- `SpeciesDefinition` needs: `Sprite Icon`, `string Description`, `string DietDescription`
- Food chain overlay: read `PreySpecies` and `PredatorSpecies` lists from each `SpeciesDefinition` to auto-generate the hierarchy — no hardcoding needed
- Species info panel: opens on card tap, shows icon, description, role in chain

### 5. Preset Scenarios
**Why fifth:** Required for Week 10 milestone demo.

Each preset is a method on `EcosystemSimulation` (or a separate `ScenarioLoader`) that calls `RemoveSpecies` / `AddSpecies` to reach a starting state:
- **Balanced Ocean** — all species at ~50% of carrying capacity
- **Shark Removed** — spawn with zero sharks, watch cascade
- **Overpopulation** — prey species near carrying capacity, sharks absent
- **Collapse** — most species at critical levels
- **Recovery** — collapse state, then add back apex predators and watch recovery

---

## Recommended Species Values (for Inspector setup)

| Species | ReproRate | NaturalDeath | CarryingCap | StarveDeath | StarveThreshold |
|---------|-----------|--------------|-------------|-------------|-----------------|
| Shark | 0.02 | 0.01 | 10 | 0.30 | 0.20 |
| Medium fish | 0.12 | 0.03 | 60 | 0.25 | 0.15 |
| Small fish | 0.20 | 0.05 | 150 | 0.00 | 0.00 |
| Plankton | 0.30 | 0.08 | 300 | 0.00 | 0.00 |

These fields exist on `SpeciesDefinition` and will be used once the health score system reads them.

---

## Known Issues / Watchpoints

- **Duplicate AudioListener** — two cameras in the scene both have `AudioListener`. Delete the one on the non-main camera.
- **Population dynamics fields are dormant** — `ReproductionRate`, `CarryingCapacity`, etc. exist on `SpeciesDefinition` but nothing reads them currently. They are ready for the health system.
- **`FishAnimationProperties` type is referenced in `SpeciesDefinition` but the class does not exist yet** — Unity will throw a compile error if a script tries to use it before it's created.
- **EcosystemUIBuilder should be deleted after use** — it's an Editor-only script. Leaving it in is harmless but unnecessary.
- **`_grid.GetNearby()` allocates a new List each call** — noted in a comment in `EcosystemSimulation.Update()`. Acceptable for now; optimise in Week 11/12 with a shared buffer.

---

## Team Structure

| Role | Person |
|------|--------|
| Simulation / backend | JunHeng |
| UI and rendering | Separate teammates |

Each person has their own Claude session. Share context via this file and `CLAUDE.md` (project root), both committed to git. Code changes from teammates appear via git — Claude sessions are not linked.
