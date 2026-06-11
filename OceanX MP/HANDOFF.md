# OceanX MP — Handoff Document
_Last updated: Week 8 of 12 (2026-06-10)_

---

## Project Goal

An interactive Unity ocean ecosystem simulation built as an **educational tool**.

**Problem it solves:** Lack of experiential learning tools limits ocean literacy and systems thinking.

**What the user does:**
- Opens a Food Chain view (icon → overlay) and clicks animals to read species info
- Adds or removes marine species using UI buttons
- Watches cascading effects unfold in real time

**What they learn:**
- Marine ecosystems are interconnected systems
- Sharks (apex predators) are critical to maintaining balance
- Removing one species causes a chain reaction across the food chain

**Core demo moment:** Remove the blacktip reef shark → groupers and barracuda overpopulate → primary consumers collapse from over-predation → secondary consumers starve and collapse too.

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
| 7 | Cascading effects + ecosystem state machine + codebase cleanup | 🔶 Partial — GPU cascade done, state machine not started, dead code removed |
| 8 | Movement systems — flocking + predator behaviour | ✅ Done (completed Week 5) |
| 9 | Event system + integration hooks for UI | 🔶 Partial — netcode sync + tablet UI add/remove working, C# events not yet wired |
| 10 | Preset scenarios + complete core system | ❌ Not started |
| 11 | Debugging, testing, system balancing | ❌ Not started |
| 12 | Final optimisation, bug fixing, project completion | ❌ Not started |

---

## Species List & Food Chain

### Keystone Species
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Blacktip reef shark | *Carcharhinus melanopterus* | All species below | — |

### Tertiary Consumers
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Brown-marbled grouper | *Epinephelus fuscoguttatus* | All species below | Blacktip reef shark |
| Great barracuda | *Sphyraena barracuda* | All species below | Blacktip reef shark |

### Secondary Consumers
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Humphead wrasse | *Cheilinus undulatus* | Hard-shelled invertebrates (crown-of-thorns starfish, sea urchins) | Blacktip reef shark, Great barracuda |
| Bluefin trevally | *Caranx melampygus* | All primary consumers, juvenile bullethead parrotfish | Blacktip reef shark, Brown-marbled grouper, Great barracuda |
| Crescent grunter | *Terapon jarbua* | Yellowstripe scad, Reticulated damselfish, juvenile surgeonfish | Blacktip reef shark, Brown-marbled grouper, Great barracuda, Bluefin trevally |
| Yellowstripe scad | *Selaroides leptolepis* | Zooplankton | Blacktip reef shark, Brown-marbled grouper, Great barracuda, Bluefin trevally, Crescent grunter |

### Primary Consumers
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Fringelip mullet | *Crenemugil crenilabis* | Algae and organic detritus | Blacktip reef shark, Brown-marbled grouper, Great barracuda, Bluefin trevally, Crescent grunter |
| Bullethead parrotfish | *Chlorurus sordidus* | Algae and coral substrate | Blacktip reef shark, Brown-marbled grouper, Great barracuda, Bluefin trevally |
| Lined surgeonfish | *Acanthurus lineatus* | Filamentous algae | Blacktip reef shark, Brown-marbled grouper, Great barracuda, Bluefin trevally, Crescent grunter |
| Eyestripe surgeonfish | *Acanthurus dussumieri* | Algae, detritus | Blacktip reef shark, Brown-marbled grouper, Great barracuda, Bluefin trevally |
| Reticulated damselfish | *Dascyllus reticulatus* | Algae, zooplankton, small invertebrates | Blacktip reef shark, Brown-marbled grouper, Great barracuda, Bluefin trevally, Crescent grunter |

**Total: 12 species** — 1 Keystone, 2 Tertiary, 4 Secondary, 5 Primary

---

## Codebase Structure

The dead Ecosystem CPU layer has been removed. The active product runs entirely on the GPU simulation pipeline.

```
Assets/Junheng/
├── Ecosystem/
│   └── Scripts/
│       ├── DualMonitor.cs           Activates Display 2 (Spacedesk/iPad) on startup
│       ├── Networking/
│       │   ├── NetworkBootstrap.cs          Host/Client role setup, starts NGO
│       │   ├── EcosystemNetworkManagerGPU.cs GPU — syncs school counts via NetworkList, RPCs
│       │   ├── TabletEcosystemUIGPU.cs      GPU tablet UI — reads EcosystemDefinitionGPU
│       │   ├── TabletSpeciesCardUIGPU.cs    GPU card — add/remove RPC buttons
│       │   ├── ConnectionScreenUI.cs        Client IP input + connect button
│       │   └── HostSpawner.cs              Spawns network manager prefab on server start
│       └── UI/                      ← Empty, ready for new tablet UI build
│
├── Shaders/                         ← Reorganised (moved from Simulation/Shaders)
│   ├── Compute/                     Boid GPU compute shaders
│   └── Fish/                        Fish_Lit + Fish_Lit_Instanced shaders
│
└── Simulation/
    └── Scripts/
        ├── Boids_GPU/
        │   ├── Ecosystem/
        │   │   ├── SpeciesDataGPU.cs           Single source of truth per species
        │   │   ├── EcosystemDefinitionGPU.cs   Species list + simulation bounds asset
        │   │   ├── EcosystemSimulationGPU.cs   Autonomous tick cascade + add/remove API
        │   │   ├── SpeciesBehaviorPropertiesGPU.cs  Flee/hunt/hunger settings SO
        │   │   └── WanderingAffecterGPU.cs     Random-wandering target for Apex species
        │   ├── 03_Spatial_Partition_Instanced_Rendering/
        │   │   └── BoidSimulationGPU.cs        Active GPU simulation + ReinitializeBuffers()
        │   ├── GPU_Spatial_Partition/
        │   │   └── SpatialPartitionGPU.cs      GPU spatial grid compute shader wrapper
        │   ├── BoidSimulationBaseGPU.cs
        │   ├── BoidSimulationTargetAnimatorsSpawner.cs
        │   ├── BoidSpawnerGPU.cs
        │   ├── BoidSpawnerGPUMultiTargets.cs
        │   ├── BoidSwirlSpawnerGPU.cs
        │   ├── BoidInfoGPU.cs
        │   ├── BoidRenderInfoGPU.cs
        │   ├── BoidSchoolInfoGPU.cs
        │   └── AffecterGPU.cs
        ├── Boids_CPU/               Only two files remain — used by GPU base classes
        │   ├── BoidInformation.cs   Per-boid movement state struct (used by FishSwimmingUtility)
        │   ├── BoidSpawnData.cs     Spawn config struct (used by BoidSpawnerBase)
        │   ├── BoidSimulationCPU.cs (in Boids_Demo scene — disabled GameObject)
        │   └── BoidSpawner.cs      (in Boids_Demo scene — disabled GameObject)
        ├── Automatic_Fish_Swimming_CPU/  (in Boids_Demo scene)
        ├── Other/
        │   ├── TransformAnimator.cs
        │   ├── TransformFollow.cs
        │   ├── TransformAnimatorSpeedCorrection.cs
        │   ├── BoundsComparer.cs
        │   └── ComputeShaderExtensions.cs
        └── Shared/
            ├── BoidSimulationBase.cs
            ├── BoidSpawnerBase.cs
            ├── BoidSpawnUtility.cs
            ├── FishSwimmingUtility.cs
            ├── FishSwimmingMaterialUpdate.cs   ← drives shader animation from speed
            ├── FishMotionRenderProperties.cs
            ├── FishMovementProperties.cs
            ├── FishSchoolProperties.cs
            ├── GlobalAffectersInjector.cs
            ├── GroupOfBoidsSpawnData.cs
            ├── SimulationAffecter.cs
            └── SimulationAffecterComponent.cs
```

---

## What Is Currently Working

### GPU Ecosystem (EcosystemSimulationGPU.cs) — Active System
- **`SpeciesDataGPU`** — one asset per species holds all simulation SOs (FishSchoolProperties, FishMovementProperties, FishMotionRenderProperties, SpeciesBehaviorPropertiesGPU) plus population dynamics fields (`StarvationDeathRate`, `StarvationThreshold`, prey/predator lists)
- **Population tick** — coroutine ticks every 5s (configurable). **Natural births and natural deaths were removed** (see Week 8). Population now changes only from:
  - **Starvation cascade (ratio-based)** — a species loses a school when any prey species drops below `StarvationThreshold` fraction of its capacity, rolled at `StarvationDeathRate`
  - **Manual add/remove** via the UI / netcode RPCs
- **`AddSpecies` / `RemoveSpecies`** — public API for UI buttons and netcode RPCs (currently keeps ≥1 school per species — start-at-zero/extinction is the next planned change)
- **`CountGroups`** — returns current school count for UI display
- `BoidSpawnerGPUMultiTargets` reads all spawn properties from `SpeciesDataGPU`

### GPU Netcode
- **Host/Client architecture** using Unity Netcode for GameObjects (NGO) over WiFi
- `NetworkBootstrap` — sets role (Host/Client), starts NGO
- `EcosystemNetworkManagerGPU` — auto-finds `EcosystemSimulationGPU` on server; syncs school counts via `NetworkList<int>` (periodic tick **+ immediate resync on add/remove**); exposes `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc`
- `TabletEcosystemUIGPU` — now a pure species→index lookup service (card UI stripped out entirely)
- New tablet UI: `SpeciesBubble` (tap → modal) + `ModalController` (in-card Add/Remove + per-species population)
- `ConnectionScreenUI` — tablet IP entry screen + LAN auto-discovery

### TransformAnimator
- Animates target transforms along Line / Circle / Rectangle paths
- `GlobalScale` on `BoidSimulationTargetAnimatorsSpawner` uniformly scales all spawned path dimensions

---

## What Was Done — Week 7

- **Codebase cleanup** — removed ~40 dead scripts:
  - All CPU Ecosystem scripts (Boid, BoidSimulation, EcosystemSimulation, SpatialPartition3D, all ScriptableObjects)
  - Simple Flocking prototype folder
  - CPU networking scripts (EcosystemNetworkManager, TabletEcosystemUI, TabletSpeciesCardUI)
  - Old GPU variants (01 Brute Force Normal, 02 Brute Force Instanced)
  - Editor-only shader GUI scripts
  - Fish_Swimming_CPU, unused CPU boid variants
  - Duplicate ComputeShaderExtensions (GameDevBuddies namespace)
- **Shader paths fixed** — reorganised `Simulation/Shaders/` → `Shaders/`, updated all `#include` paths across compute and hlsl files
- **Species list finalised** — 12 species confirmed (see table above)

---

## What Was Done — Week 8 (JunHeng)

Focus: real species data, Android tablet build pipeline, shaders in-scene, and wiring the new tablet UI to the netcode layer.

### ✅ What Worked
- **Species data assets created** — built the per-species `SpeciesDataGPU` assets (Blacktip reef shark, Bluefin trevally, Bullethead parrotfish, Eyestripe surgeonfish, Fringelip mullet, Reticulated damselfish, Streaked spinefoot, Bluespotted ribbontail ray, Russell's snapper, Yellowstripe scad, Brown-marbled grouper, Great barracuda) + iterated on their data values. These no longer "don't exist."
- **Android APK builds** — got the tablet client building and deploying to Android; multiple successful test builds.
- **Mobile renderer** — switched the build to a mobile-appropriate URP renderer for the tablet.
- **Shaders in-scene** — implemented the `Fish_Lit` shader and a `Shader_GUI/Editor` property drawer into the simulation/UI scenes.
- **New tablet scene** — built out the `Netcode Simulation Test` tablet scene with the new bubble/modal UI.
- **Netcode UI integration (this session)** — wired the new UI to the existing netcode layer:
  - `SpeciesBubble` now carries a `SpeciesDataGPU` reference and resolves its species **index** via `TabletEcosystemUIGPU.GetSpeciesIndex()` (robust to reordering — no hand-numbered indices).
  - `TabletEcosystemUIGPU` became a lookup service (singleton + `GetSpeciesIndex`); legacy auto-card spawning is now optional.
  - Add/Remove moved **into the modal card** (`ModalController`) — buttons fire `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc` and show the synced population for the open species only.
  - **Instant population feedback** — `EcosystemNetworkManagerGPU` now resyncs the `NetworkList<int>` immediately after an add/remove RPC instead of only on the 1s tick, so the tablet count updates without lag.
  - Fixed stale-population bug — modal population is now per-species (cleared/hidden for cosmetic-only bubbles instead of showing the last card's number).
- **Population model simplified (this session)** — removed natural births and natural deaths from `EcosystemSimulationGPU.RunPopulationTick`. Population now changes only via the **starvation/prey-ratio cascade** + manual add/remove. The starvation cascade (the core "remove the food → predators starve" demo) is intact.
- **Removed dead rate fields (this session)** — deleted `ReproductionRate` and `NaturalDeathRate` from `SpeciesDataGPU` (gone from the Inspector; they were unused after the births/deaths removal). `StarvationDeathRate` / `StarvationThreshold` kept.
- **Card UI fully removed (this session)** — deleted `TabletSpeciesCardUIGPU.cs` (+ meta); stripped `TabletEcosystemUIGPU` down to a pure species→index lookup (no more `CardPrefab` / `CardContainer` / `BuildCards`). Updated the stale comment in `EcosystemNetworkManagerGPU`. The new bubble/modal UI is the only tablet UI now.
- **Planned next, spec'd this session** — start-at-zero / school-scaling / extinction model (player builds the ecosystem up from an empty ocean; species removable to 0; per-species `MaxSchools` cap). Full implementation prompt written (see "What Needs Building Next").

### ❌ What Didn't Work / Still Open
- **Hard crash with shark + water shader (URP).** See dedicated note in Known Issues below. This was the main blocker and is not fully resolved — only worked around.
- **Ecosystem definition not fully populated** — the `SpeciesDataGPU` assets exist, but `EcosystemDefinitionGPU.asset` still needs all species added in a fixed order (host + tablet must share the same list). Until then, bubbles for unlisted species resolve to index -1 (card goes dead).
- **Start-at-zero / extinction not yet built** — Add/Remove currently keeps ≥1 school per species (can't reach 0). The GPU pipeline assumes ≥1 boid per spawner, and an all-zero start would hit a zero-size `ComputeBuffer` crash. Spec'd and ready to implement.
- **C# event system** — population/health/state events for the UI team still not wired.
- **Ecosystem health bar** — GPU side still not connected.

---

## What Needs Building Next (Priority Order)

### 1. Start-at-Zero / School-Scaling / Extinction model (spec'd, ready to build)
Player builds the ecosystem up from an empty ocean. Each species starts at **N = 0 schools** (excluded from the GPU sim); Add/Remove scales **groups + targets + animators + boids together** (constant density, X fish per school); removable to **0 (extinction)**; per-species **static `MaxSchools` cap**.
- Key risk: all-zero start → zero-size `ComputeBuffer` crash. Chosen fix: min-1 placeholder buffers + skip dispatch/render when total boids = 0.
- Add fields `FishPerSchool` + `MaxSchools` to `SpeciesDataGPU`; spawner gets an active/school-count concept; `EcosystemSimulationGPU` owns `N`.
- A full implementation prompt for this was written this session (hand to `/game-developer`, then `/code-review`).

### 2. Create SpeciesDataGPU assets for all 12 species
`SpeciesDataGPU` needs new fields before assets can be created:
- `string ScientificName`
- `string Description`
- `Sprite Icon`
- `TrophicTier` enum (Keystone / Tertiary / Secondary / Primary)
- `Vector2 FoodWebPosition` — node position in the food web graph UI
- `bool StartUnlocked` — false = silhouette until prerequisites met
- `List<SpeciesUnlockRequirement> UnlockRequirements` — prerequisites with min count

Then create one asset per species and wire all `PreySpecies` / `PredatorSpecies` lists using the food chain table above.

### 3. Build Tablet UI (Food Web Graph)
See prototype at `prototype/oceanx-prototype.html` for reference design. Missing from Unity:
- **Food web graph panel** — SVG-style nodes (species bubbles) + edges (predator arrows)
- **Species lock/unlock system** — silhouette until prerequisites met
- **Eco-health bar** — GPU side not yet wired
- **Over/underpopulation indicators** — red/orange rings on nodes
- **Species info modal** — tap node → name, sci name, description, tier, count, Add button
- **Current Organisms view** — toggle to grid of active species bubbles
- **Intro screen** + Reset button

### 4. Ecosystem Health Score + State Machine
**Health score (0–100) factors:**
- Biodiversity: fraction of species with living members
- Balance: prey:predator ratio per species
- Apex predator presence (shark weighted heavily)
- Stability: rate of population change

**States:** Healthy → Unstable → Critical → Collapsing → Recovering

### 5. Finish Netcode Client Setup
Resolve NetworkConfig mismatch — both host and client NetworkManagers must have the **exact same Network Prefabs List**. Register `EcosystemNetworkManagerGPU` prefab on the client's NetworkManager.

### 6. Preset Scenarios
- **Balanced Ocean**, **Shark Removed**, **Overpopulation**, **Collapse**, **Recovery**

---

## Recommended Population Dynamics Values

| Species | Tier | ReproRate | NaturalDeath | StarveRate | StarveThreshold |
|---------|------|-----------|--------------|------------|-----------------|
| Blacktip reef shark | Keystone | 0.02 | 0.01 | 0.30 | 0.20 |
| Brown-marbled grouper | Tertiary | 0.08 | 0.02 | 0.25 | 0.15 |
| Great barracuda | Tertiary | 0.08 | 0.02 | 0.25 | 0.15 |
| Humphead wrasse | Secondary | 0.10 | 0.03 | 0.20 | 0.15 |
| Bluefin trevally | Secondary | 0.12 | 0.03 | 0.20 | 0.15 |
| Crescent grunter | Secondary | 0.12 | 0.03 | 0.20 | 0.15 |
| Yellowstripe scad | Secondary | 0.15 | 0.04 | 0.15 | 0.10 |
| Fringelip mullet | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Bullethead parrotfish | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Lined surgeonfish | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Eyestripe surgeonfish | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Reticulated damselfish | Primary | 0.22 | 0.05 | 0.00 | 0.00 |

Carrying capacity is auto-derived from the spawner's initial `BoidsCount` at runtime.

---

## Scene Setup Reference

### Boids_Demo (host/trifold display scene)
- `Boids_Simulation_GPU` GameObject: `BoidSimulationGPU` + `EcosystemSimulationGPU`
- One `BoidSpawnerGPUMultiTargets` per species (12 total needed)
- `NetworkManager` GameObject: `NetworkManager` + `UnityTransport` + `NetworkBootstrap` (Role: Host)
- `EcosystemNetworkManagerGPU` prefab registered in NetworkManager's Network Prefabs List

### Netcode Simulation Test (client/tablet scene)
- `NetworkManager` GameObject: same components, Role: **Client**
- Same `EcosystemNetworkManagerGPU` prefab registered (must match host exactly)
- `ConnectionScreenUI` canvas for IP entry
- `TabletEcosystemUIGPU` with `EcosystemDefinitionGPU` asset, card prefab, card container

---

## Known Issues / Watchpoints

- **🛑 CRASH — shark + water shader (suspected URP / Stylized Water opaque-texture interaction).**
  - **Repro:** when the **shark** enters the scene **together with the water shader**, the app crashes.
  - **Workarounds that run fine:** removing the **shader** alone, or the **shark** alone, runs without crashing.
  - **Oddity:** with the **shark + water + (some) other GameObject** all present, everything runs smoothly — so it appears to be a fragile state, not a clean reproduction.
  - **Suspected cause:** URP / Stylized Water shader not resolving correctly, likely tied to the **Opaque Texture** setting (camera/URP asset `_CameraOpaqueTexture`). The shark material rendering with the water shader's opaque-texture sampling may be the trigger.
  - **Status:** worked around (commit `b85296d` "fixing Crashing error" in `Boids_Demo.unity`), **not root-caused.** Next step: verify URP asset has Opaque Texture enabled and matches between desktop + mobile renderers, and test the shark material in isolation against the water shader.
- **NetworkConfig mismatch** — client and host must have identical Network Prefabs Lists
- **`SpeciesDataGPU` assets exist but ecosystem list incomplete** — the per-species assets are created; `EcosystemDefinitionGPU.asset` still needs every species added (same order on host + tablet) so the index-based RPCs line up
- **`SpeciesDataGPU` missing UI fields** — ScientificName, Description, Sprite, TrophicTier, FoodWebPosition, unlock requirements not yet added
- **Duplicate AudioListener** — multiple cameras in scene, keep exactly one active
- **`Boids_Simulation_CPU` GameObject in Boids_Demo** — disabled, holds missing script refs to deleted CPU scripts. Safe to delete from scene

---

## Team Structure

| Role | Person |
|------|--------|
| Simulation / backend | JunHeng |
| UI and rendering | Separate teammates |

Each person has their own Claude session. Share context via this file and `CLAUDE.md` (project root), both committed to git.
