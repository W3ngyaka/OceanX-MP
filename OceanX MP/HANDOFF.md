# OceanX MP — Handoff Document
_Last updated: Week 9 of 12 (2026-06-12)_

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
| 9 | Event system + integration hooks for UI | 🔶 Partial — start-at-zero/extinction done; bounds derived from sim area; C# events not yet wired |
| 10 | Preset scenarios + complete core system | ❌ Not started |
| 11 | Debugging, testing, system balancing | ❌ Not started |
| 12 | Final optimisation, bug fixing, project completion | ❌ Not started |

---

## Species List & Food Chain

> ✅ **CANONICAL species list (confirmed by JunHeng, 2026-06-12).** This table is now the single source of truth. The data assets and prototype must be aligned to it.
>
> **Asset delta to apply:**
> - ➕ **Giant moray** (*Gymnothorax javanicus*) — **no `_Data.asset` yet, needs creating** (+ Behavior / MotionRender / Movement / School props, like the other species)
> - ➖ **Great barracuda** — has a `_Data.asset` but is **no longer in the list** → remove/deprecate it
> - The other 10 species already have data assets ✓
>
> **Superseded names** (do not use): Humphead wrasse, Crescent grunter, Lined surgeonfish (never had assets); Great barracuda (being removed). The **prototype** (`oceanx-prototype.html`) still uses placeholder names (Striped Mullet, Convict Surgeonfish, Reef Manta Ray, Malabar Grouper…) — align it to this list when wiring the unlock system.
>
> **Live sim still runs only the Clownfish placeholder** — `EcosystemDefinitionGPU.asset` has none of these 12 wired in yet (confirmed by Player.log "Found simulation with 1 species").
>
> _Prey/predator columns below follow the game's tier-based cascade design (apex eats all below; each tier eats the tiers beneath it). Confirm the exact per-species prey/predator lists when filling in each `SpeciesDataGPU` asset._

### Keystone Species
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Blacktip reef shark | *Carcharhinus melanopterus* | All species below | — |

### Tertiary Consumers
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Brown-marbled grouper | *Epinephelus fuscoguttatus* | Secondary + primary consumers | Blacktip reef shark |
| Giant moray | *Gymnothorax javanicus* | Secondary + primary consumers (nocturnal ambush) | Blacktip reef shark |

### Secondary Consumers
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Bluefin trevally | *Caranx melampygus* | Primary consumers, small fish | Blacktip reef shark, Brown-marbled grouper, Giant moray |
| Russell's snapper | *Lutjanus russellii* | Small fish, crustaceans, benthic invertebrates | Blacktip reef shark, Brown-marbled grouper, Giant moray |
| Yellowstripe scad | *Selaroides leptolepis* | Zooplankton, small invertebrates | Blacktip reef shark, Brown-marbled grouper, Giant moray, Bluefin trevally |
| Bluespotted ribbontail ray | *Taeniura lymma* | Benthic crustaceans, mollusks, worms | Blacktip reef shark |

### Primary Consumers
| Species | Scientific Name | Preys On | Preyed Upon By |
|---------|----------------|----------|----------------|
| Fringelip mullet | *Crenemugil crenilabis* | Algae and organic detritus | Blacktip reef shark, Brown-marbled grouper, Giant moray, Bluefin trevally |
| Bullethead parrotfish | *Chlorurus sordidus* | Algae scraped from coral substrate | Blacktip reef shark, Brown-marbled grouper, Giant moray, Bluefin trevally |
| Streaked spinefoot | *Siganus javus* | Algae and seagrass | Blacktip reef shark, Brown-marbled grouper, Giant moray, Bluefin trevally |
| Eyestripe surgeonfish | *Acanthurus dussumieri* | Algae, detritus | Blacktip reef shark, Brown-marbled grouper, Giant moray, Bluefin trevally |
| Reticulated damselfish | *Dascyllus reticulatus* | Algae, zooplankton, small invertebrates | Blacktip reef shark, Brown-marbled grouper, Giant moray, Bluefin trevally |

**Total: 12 species** — 1 Keystone, 2 Tertiary, 4 Secondary, 5 Primary

---

## Codebase Structure

The dead Ecosystem CPU layer has been removed. The active product runs entirely on the GPU simulation pipeline.

> ⚠ **Verified against the actual tree (2026-06-12).** All simulation scripts live directly under `Assets/Junheng/Scripts/` — the old `Junheng/Ecosystem/Scripts/` and `Junheng/Simulation/Scripts/` split no longer exists, and there is no `03_` prefix on the spatial-partition folder.

```
Assets/Junheng/
├── Scripts/
│   ├── DualMonitor.cs                Activates Display 2 (Spacedesk/iPad) on startup
│   ├── Boids_GPU/
│   │   ├── AffecterGPU.cs
│   │   ├── BoidInfoGPU.cs
│   │   ├── BoidRenderInfoGPU.cs
│   │   ├── BoidSchoolInfoGPU.cs
│   │   ├── BoidSimulationBaseGPU.cs
│   │   ├── BoidSimulationTargetAnimatorsSpawner.cs
│   │   ├── BoidSpawnerGPU.cs
│   │   ├── BoidSpawnerGPUMultiTargets.cs   Active spawner used in Boids_Demo
│   │   ├── BoidSwirlSpawnerGPU.cs
│   │   ├── Ecosystem/
│   │   │   ├── EcosystemDefinitionGPU.cs   Species list + simulation bounds asset
│   │   │   ├── EcosystemSimulationGPU.cs   Tick cascade + start-at-zero add/remove API
│   │   │   ├── EcosystemUIAdapterGPU.cs    UI→GPU bridge (⚠ only self-referenced — verify if still used)
│   │   │   ├── SpeciesBehaviorPropertiesGPU.cs  Flee/hunt/hunger settings SO
│   │   │   ├── SpeciesDataGPU.cs           Single source of truth per species
│   │   │   └── WanderingAffecterGPU.cs     Random-wander target (one per school, all roles)
│   │   ├── GPU_Spatial_Partition/
│   │   │   └── SpatialPartitionGPU.cs      GPU spatial grid compute shader wrapper
│   │   └── Spatial_Partition_Instanced_Rendering/
│   │       └── BoidSimulationGPU.cs        Active GPU simulation + ReinitializeBuffers()
│   ├── Boids_CPU/                   Only two files remain — used by GPU base classes
│   │   ├── BoidInformation.cs       Per-boid movement state struct (used by FishSwimmingUtility)
│   │   └── BoidSpawnData.cs         Spawn config struct (used by BoidSpawnerBase)
│   ├── Automatic_Fish_Swimming_CPU/
│   │   ├── AutomaticFishSwimSimulation.cs
│   │   └── AutomaticFishSwimming.cs
│   ├── Networking/
│   │   ├── ConnectionScreenUI.cs           Client IP input + connect button
│   │   ├── EcosystemNetworkManagerGPU.cs   Syncs school counts via NetworkList, RPCs
│   │   ├── HostSpawner.cs                  Spawns network manager prefab on server start
│   │   ├── LanDiscovery.cs                 UDP broadcast — tablet auto-finds host on WiFi
│   │   ├── NetworkBootstrap.cs             Host/Client role setup, starts NGO
│   │   └── TabletEcosystemUIGPU.cs         Pure species→index lookup service
│   ├── Shader_GUI/Editor/          Custom material inspectors for the Fish_Lit shaders
│   │   ├── FishLitBaseShaderGUI.cs / FishLitDetailGUI.cs / FishLitShaderGUI.cs
│   │   ├── FishSwimmingGUI.cs / MaterialAccess.cs / Property.cs / ShaderUtils.cs
│   ├── Other/
│   │   ├── BoundsComparer.cs
│   │   ├── ComputeShaderExtensions.cs
│   │   ├── TransformAnimator.cs
│   │   ├── TransformAnimatorSpeedCorrection.cs
│   │   └── TransformFollow.cs
│   └── Shared/
│       ├── BoidSimulationBase.cs
│       ├── BoidSpawnerBase.cs            SchoolCount/IsActive + SetSchoolConfiguration
│       ├── BoidSpawnUtility.cs
│       ├── FishMotionRenderProperties.cs
│       ├── FishMovementProperties.cs
│       ├── FishSchoolProperties.cs
│       ├── FishSwimmingMaterialUpdate.cs   drives shader animation from speed
│       ├── FishSwimmingUtility.cs
│       ├── GlobalAffectersInjector.cs
│       ├── GroupOfBoidsSpawnData.cs
│       ├── SimulationAffecter.cs
│       └── SimulationAffecterComponent.cs
├── Shaders/
│   ├── Compute/                     Brute-force + spatial-partition + grid compute shaders
│   └── Fish/                        Fish_Lit + Fish_Lit_Instanced shaders
├── Data/                            EcosystemDefinitionGPU.asset + Fish/ species assets
├── Scenes/                          Boids_Demo, Netcode Simulation Test, Swirl_Demo
├── Prefabs/ · Settings/ · Visual/   Prefabs, URP/build settings, materials/meshes/textures

Assets/Aloysius/                     UI team (see "Weeks 7–8 — UI Team" section)
└── Scripts/  Bob.cs · FoodWebLines.cs · Health.cs · ModalController.cs · SpeciesBubble.cs · SwipeToClose.cs
```

---

## What Is Currently Working

### GPU Ecosystem (EcosystemSimulationGPU.cs) — Active System
- **`SpeciesDataGPU`** — one asset per species holds all simulation SOs (FishSchoolProperties, FishMovementProperties, FishMotionRenderProperties, SpeciesBehaviorPropertiesGPU) plus population dynamics fields (`StarvationDeathRate`, `StarvationThreshold`, prey/predator lists)
- **Population tick** — coroutine ticks every 5s (configurable). **Natural births and natural deaths were removed** (see Week 8). Population now changes only from:
  - **Starvation cascade (ratio-based)** — a species loses a school when any prey species drops below `StarvationThreshold` fraction of its capacity, rolled at `StarvationDeathRate`
  - **Manual add/remove** via the UI / netcode RPCs
- **`AddSpecies` / `RemoveSpecies`** — public API for UI buttons and netcode RPCs. Species start at 0 schools; Add increments up to `MaxSchools`; Remove decrements to extinction (0). Starvation tick can also remove the last school.
- **`CountGroups`** — returns current school count for UI display
- **`FishPerSchool` / `MaxSchools`** — per-species fields on `SpeciesDataGPU`; Add/Remove scales boid count by `FishPerSchool`; cap synced to clients
- **Empty-ocean crash safety** — all GPU buffers sized `Mathf.Max(1, count)`; dispatch and render skipped when `_boidsCount == 0`
- **Simulation bounds derived** — `EcosystemSimulationGPU.SimulationBounds` reads from `BoidSimulationGPU.SimulationAreaBounds`; no more manual sync of two separate bounds assets
- `BoidSpawnerGPUMultiTargets` reads all spawn properties from `SpeciesDataGPU`

### GPU Netcode
- **Host/Client architecture** using Unity Netcode for GameObjects (NGO) over WiFi
- `NetworkBootstrap` — sets role (Host/Client), starts NGO
- `EcosystemNetworkManagerGPU` — auto-finds `EcosystemSimulationGPU` on server; syncs school counts via `NetworkList<int>` (periodic tick **+ immediate resync on add/remove**); exposes `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc`
- `TabletEcosystemUIGPU` — now a pure species→index lookup service (card UI stripped out entirely)
- New tablet UI: `SpeciesBubble` (tap → modal) + `ModalController` (in-card Add/Remove + per-species population)
- `ConnectionScreenUI` — tablet IP entry screen + LAN auto-discovery

### Tablet UI (built by Aloysius, integrated into JunHeng's main `Netcode Simulation Test` scene)
- **Food web graph** — 12 species bubbles (`SpeciesBubble.cs`) laid out in trophic tiers; `FoodWebLines.cs` edges exist but are hidden pending visual fix
- **`ModalController.cs`** — species info modal with Add/Remove buttons wired to `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc`; now also greys buttons at cap/0 (connected in `e13e26b`)
- **Eco-health bar** — `Health.cs` bar present in scene; **not yet reading from GPU simulation**
- **`SwipeToClose.cs`** — swipe to dismiss modal
- **`Bob.cs`** — bobbing animation on species bubbles
- **Fish image assets** — PNG sprites for most species in `Assets/Aloysius/Fishes/` and `Assets/Aloysius/iamge/`

### LAN Discovery
- `LanDiscovery.cs` — UDP broadcast on port 47777; tablet auto-discovers host on same WiFi network. Advertiser starts automatically when `NetworkBootstrap` starts the host.

### TransformAnimator
- Animates target transforms along Line / Circle / Rectangle paths
- `GlobalScale` on `BoidSimulationTargetAnimatorsSpawner` uniformly scales all spawned path dimensions

---

## Scene Architecture

| Scene | Owner | Role | Path |
|-------|-------|------|------|
| `Boids_Demo` | JunHeng | **Host** — GPU simulation + trifold display | `Assets/Junheng/Scenes/Boids_Demo.unity` |
| `Netcode Simulation Test` | JunHeng | **Client — MAIN tablet UI scene** | `Assets/Junheng/Scenes/Netcode Simulation Test.unity` |
| `Netcode Simulation Test 1` | Aloysius | UI prototyping only (handed off to JunHeng) | `Assets/Aloysius/Netcode Simulation Test 1.unity` |

`Boids_Demo` is the host (GPU simulation, netcode host role, only scene in the build).

**`Netcode Simulation Test` (JunHeng) is the canonical tablet client scene.** JunHeng is integrating Aloysius's UI into it — it already contains the food-web UI scripts (`SpeciesBubble`, `ModalController`, `FoodWebLines`, `Bob`, `SwipeToClose`) wired alongside the netcode layer (`NetworkBootstrap`, `ConnectionScreenUI`, `TabletEcosystemUIGPU`).

**`Netcode Simulation Test 1` (Aloysius) is NOT the product scene** — Aloysius builds/prototypes the UI there, then hands the prefabs/scripts to JunHeng to wire into the main scene above. Treat it as a reference, not the shipping client.

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

## What Was Done — Weeks 7–8 (UI Team / Aloysius)

All work lives in `Assets/Aloysius/` and the `Netcode Simulation Test 1` scene — this is a **prototyping scene**. Aloysius builds the UI here, then hands the scripts/prefabs to JunHeng, who integrates them into the main client scene (`Netcode Simulation Test`). The integration is already underway (the UI scripts are present in JunHeng's scene).

### Food Web Graph UI
- **`SpeciesBubble.cs`** — interactive species node bubbles on the food web panel; tapping opens a species info modal
- **`FoodWebLines.cs`** — `LineRenderer`-based edges between species nodes (predator arrows). Currently hidden by default (`LINE FOOD WEB HIDE` commit) — the lines exist but are toggled off. Marked "wonky, TO BE CHANGED."
- Food web nodes and layout working in the scene; full visual structure of 12 species bubbles present

### Species Info Cards
- Fish image assets added for multiple species (shark, grouper, mullet, scad, snapper, damselfish, eyestripe surgeonfish, moray, barracuda)
- Species info card UI with fish image + info text wired to each `SpeciesBubble`

### ModalController
- **`ModalController.cs`** — modal popup card triggered by tapping a species bubble; shows species info, Add/Remove buttons. Connected to netcode RPCs in `e13e26b`.

### Animations
- **`Bob.cs`** — bobbing animation for UI species bubbles
- Transition animation pass 1 committed (`1eae5f3`)
- `SwipeToClose.cs` — swipe-down gesture to dismiss the species infobox modal

### Eco-Health Bar
- **`Health.cs`** — health bar UI script (reads eco-health, drives fill bar)
- Bar/frame image assets: `bar.png`, `ecoheal.png`, `healthframe.png`
- Wired to the `Netcode Simulation Test 1` scene; **not yet connected to GPU simulation data**

### Mockup / Coral Assets (shared team)
- Imported temporary 3D coral assets (`Assets/_Assets/3D Models/Corals/`) — *Acropora hyacinthus* hard coral model + *Rainbow Haven Reef* coral preset
- `SCENE_MockupScene.unity` filled with coral assets for visual reference
- Stylized Water 3 material tweaked for the mockup scene

### Prototype (JunHeng + team)
- `prototype/oceanx-prototype.html` — interactive HTML prototype created and iterated (`2a65a8d`, `039197a`). Full spec is in the **Prototype Specification** section of this document.

### LanDiscovery + Connection UI (JunHeng)
- **`LanDiscovery.cs`** — UDP broadcast so tablet auto-discovers the host on the same WiFi network (no manual IP entry needed after initial setup). Works alongside `ConnectionScreenUI`.
- `NetworkBootstrap` updated to start the LAN advertiser when hosting.
- Android APK build pipeline confirmed working (`dc77683`, `680c1be`).

---

## What Was Done — Week 9 (JunHeng)

### ✅ `e13e26b` — Start-at-zero / school-scaling / extinction model

The GPU ecosystem now starts from a completely empty ocean and the player builds it up.

**`SpeciesDataGPU`:**
- Added `FishPerSchool` (int) — number of boids per school unit (constant density scaling)
- Added `MaxSchools` (int) — static per-species cap; synced to clients via netcode

**`BoidSpawnerBase`:**
- Added `SchoolCount` / `IsActive` properties + `SetSchoolConfiguration(schoolCount, fishPerSchool)`
- Inactive spawners (school count = 0) are excluded from the concat buffer, spatial grid, affecter targets, and rendering — no placeholder draw calls

**`EcosystemSimulationGPU`:**
- Owns `N` (school count) per species, initialised to 0
- `AddSpecies`: increments N up to `MaxSchools`, calls `ReinitializeBuffers`
- `RemoveSpecies`: decrements N down to 0 (extinction), calls `ReinitializeBuffers`
- Starvation tick can now remove the last school (species goes fully extinct)
- One `WanderingAffecterGPU` target per school for all roles — unifies the old apex-only wandering path, no memory leaks on rebuild

**Empty-ocean / extinction crash safety:**
- All GPU compute buffers and spatial grid sized `Mathf.Max(1, count)` — zero is never passed to `new ComputeBuffer`
- `SetData` / `GetData` guarded against empty arrays
- Per-frame dispatch and render skip when `_boidsCount == 0`
- Group IDs assigned densely over active spawners only

**Netcode:**
- `MaxSchools` synced to clients
- Tablet Add button greys out at cap; Remove button greys out at 0

**Bug fix:** NaN spawn positions for single-fish schools caused by a divide-by-zero in spawn grouping — fixed with a guard.

> ⚠ **Not yet play-tested in the Unity Editor at time of commit.** Needs a full in-editor run with add/remove cycles.

---

### ✅ `a47e2c7` — Remove redundant scripts / dead fields

- Deleted `TabletSpeciesCardUIGPU.cs` (old card UI, fully replaced by `ModalController`)
- Removed dead `_carryingCapacity` dict from `EcosystemSimulationGPU` (carrying capacity now derived from `MaxSchools × FishPerSchool`)
- Cleaned up `EcosystemNetworkManagerGPU` stale comment
- Trimmed `TabletEcosystemUIGPU` to pure lookup service

---

### ✅ `e9ae364` — Derive ecosystem bounds from the BoidSimulationGPU area

- `EcosystemSimulationGPU` now exposes a `SimulationBounds` property that reads directly from `BoidSimulationGPU.SimulationAreaBounds`
- `EcosystemDefinitionGPU` SimulationCenter/SimulationSize are now only a fallback (no longer need to be hand-matched to the sim area)
- Wandering affecter targets now spawn and roam inside the derived volume
- Bounds gizmo draws the derived volume so it overlaps the BoidSimulationGPU box
- Also added `SpeciesData_Clownfish.asset` (placeholder species data)

---

### ✅ `1887612` — Null-guard in UpdateSimulation

Added an early-return null-guard in `BoidSimulationGPU.UpdateSimulation` so the simulation gracefully skips a frame if called before GPU buffers are fully initialised (first-frame race condition).

---

## Prototype Specification (`prototype/oceanx-prototype.html`)

_Full interactive reference — open it in a browser. Everything below is derived from reading its source code._

---

### Layout — two panels

| Panel | Description |
|-------|-------------|
| **Left — Coral Reef** | Live reef visual: SVG terrain, layered coral/flora that grows with health, animated fish swimming across, god-rays, depth darkening, murk overlay that fades as health rises |
| **Right — Food Web tablet** | Bright light-blue rounded panel; two views toggled by chevron (⌄): **FOODWEB** (default) and **CURRENT ORGANISMS** |

**Top bar** (spans full width): OceanX logo + "BALANCE THE OCEAN" tagline → Eco-Health label → horizontal health bar (color: low=red, mid=orange, high=cyan) → numeric percentage.

Left reef panel also has a **vertical Eco-Health bar** on its left edge (same colour logic).

---

### Food Web view

- SVG canvas, nodes arranged by trophic tier (top = Keystone, bottom = Primary)
- Each node is a **glass bubble**: radial-gradient fill, white ring, highlight ellipse, emoji inside, name label below
- Ring colour = trophic tier (Keystone=cyan, Tertiary=orange, Secondary/Primary=teal/green)
- **Locked nodes**: emoji shown as dark silhouette, name shows "???"
- **Count badge** top-right corner of bubble: cyan normal, red = overpopulated, orange = underpopulated
- **Over/under glow**: ring turns red/orange + drop-shadow glow when imbalanced
- **Predator arrows** (edges): hidden by default. **Long-press** a node to reveal arrows TO its predators; all other nodes dim; predators get a cyan highlight ring. Releasing clears the overlay
- **Tap** (short press) → opens modal

---

### Modal — tap a node

**Unlocked species:**
- Left column: large emoji (animates bobbing on first-reveal), common name, **[+ ADD]** button (rounded, cyan glow)
- Right column: trophic tier label (tier colour), Scientific Name, Role description (what it does in the reef), "What's next" contextual hint, count currently in ecosystem + balance status (✓ balanced / ⚠ overpopulated / ⚠ underpopulated)

**Locked species:**
- Left: silhouette emoji + no name shown
- Right: "Species Missing" label, **progressive hint** (gets more specific each time the player taps — 3 levels: vague → clearer → almost there), requirements checklist:
  - Eco-health ≥ X% (shown if minHealth > 0)
  - Specific prey species count ≥ N (one row per requirement, green ✓ met / red ○ missing)

---

### Current Organisms view

- Trapezoid "tank" shape with subtle grid overlay
- Grid of bubbles: species emoji + count badge, over/under colour coding, tap → remove popup (−1 or All)
- Empty state: "No organisms yet — add species from the food web."

---

### Game flow

1. **Intro screen** (inside the food-web panel): "⚠ REEF STATUS: CRITICAL" badge + Alucia's opening message + "Begin →" button. Disappears when Begin is pressed.
2. **Player taps nodes to Add** species one school at a time
3. **Eco-health updates** every render frame (based on diversity + ratio scores)
4. **Unlock gate**: locked nodes auto-unlock when prerequisites are met → Alucia announces it
5. **First-time add**: "New Species Discovered" reveal card floats over the reef panel for 5.5 s (emoji, name, sci name, tier badge, description, hint, countdown bar)
6. **Win**: eco-health reaches 100% → Alucia celebrates, sticky win message
7. **Reset** (↺ button): SpongeBob-style bubble-flood animation wipes the screen, then intro re-appears

---

### Alucia — NPC guide

Translucent speech bubble, bottom-left of reef panel, mermaid avatar (🧜‍♀️). Three visual states:
- **Default** (light blue): tips on first add, unlock announcements
- **Warn** (orange): overpopulation / underpopulation alerts (fires whenever player taps an imbalanced node, and after adding a species that tips the balance)
- **Win** (green): ecosystem fully recovered

Auto-hides after 5.2 s. Sticky for win message.

---

### Eco-Health formula (from prototype JS)

```
h = (distinct species count) × 6          // up to 66 for 11 non-shark species
h += 8 if shark is present
for each prey species with count > 0:
  ratio = count[species] / sum(count[its predators])
  if no predators: penalty for count > 8
  elif ratio >= 8 (overpop): penalty
  elif ratio <= 0.5 (underpop): penalty
  else: bonus (peaks at ratio 2:1–5:1)
h = clamp(0, 100)
```

Visual warning ring fires at **ratio ≥ 4:1** (before the 8:1 penalty threshold). States: `< 35% = low`, `35–70% = mid`, `≥ 70% = high`.

---

### Unlock prerequisites (from prototype — note species names differ from HANDOFF list)

| Species | Requires | Min health |
|---------|----------|-----------|
| Striped Mullet | — (start unlocked) | 0% |
| Reticulated Damselfish | — (start unlocked) | 0% |
| Bullethead Parrotfish | — | 5% |
| Convict Surgeonfish | — | 9% |
| Brown Surgeonfish | — | 13% |
| Yellowtail Scad | damselfish ×2 | 0% |
| Humphead Wrasse | surgeonfish ×2 | 0% |
| Crescent Grunter | convict ×2 | 0% |
| Reef Manta Ray | damselfish ×3 | 22% |
| Malabar Grouper | mullet ×3, wrasse ×1 | 35% |
| Great Barracuda | scad ×2 | 45% |
| Blacktip Reef Shark | mullet ×2, surgeonfish ×2, grouper ×1 | 55% |

> ⚠ **Prototype uses placeholder species names** (Striped Mullet, Convict Surgeonfish, Reef Manta Ray, Malabar Grouper, Yellowtail Scad, Brown Surgeonfish) that do **not** match the now-canonical list (see **Species List & Food Chain**). The prototype's unlock prerequisites and node layout are still valid as *design reference* — but remap every species to the canonical 12 (and drop Manta Ray / Convict Surgeonfish, add Giant moray / the ray / snapper / spinefoot) when porting the unlock system to Unity.

---

### Reef visual — habitat growth

Ocean background colour interpolates across 8 keyframes from dark murky teal (health 0%) to vivid cyan (health 100%). Flora appears in layers (back/mid/front/tall) with each item having a `minHealth` threshold — items fade in gradually as health crosses their threshold. Murk overlay opacity = `max(0, (70 - health) / 70 × 0.5)`, fully gone above 70%.

---

### ⚠ Key design difference vs current build

The prototype uses a **locked progression** model: only 2 species are available at start; the rest unlock gate-by-gate as the player adds prey first. The current Unity build has **no lock system** — any species can be added at any time. **The unlock system and progressive hint logic need to be built.** The `SpeciesDataGPU` fields `StartUnlocked` and `UnlockRequirements` are already spec'd for this.

---

## What Needs Building Next (Priority Order)

### ~~1. Start-at-Zero / School-Scaling / Extinction model~~ ✅ Done (`e13e26b`)

Implemented in commit `e13e26b`. Player builds from empty ocean; Add/Remove scale schools; extinction at 0; `MaxSchools` cap; crash-safe empty-ocean state. **Needs an in-editor play-test with full add/remove cycles to confirm no regressions.**

### 1. Finalise the 12 canonical species + wire them into the sim
The species list is now locked (see **Species List & Food Chain**). Concrete steps:
- ➕ **Create the Giant moray** (*Gymnothorax javanicus*) data assets — `_Data`, `_Behavior`, `_MotionRenderProperties`, `_MovementProperties`, `_SchoolProperties` (mirror an existing Tertiary species like Brown-marbled grouper)
- ➖ **Remove/deprecate Great barracuda** — it has assets but is no longer in the list
- Wire all 12 `SpeciesDataGPU` assets into `EcosystemDefinitionGPU.asset` **in a fixed order** (currently only the Clownfish placeholder is wired) so host + tablet index-based RPCs line up
- Then add the UI/unlock fields below and fill `PreySpecies` / `PredatorSpecies` from the food-chain table

`SpeciesDataGPU` needs new fields before the food-web UI can be built:
- `string ScientificName`
- `string Description`
- `Sprite Icon`
- `TrophicTier` enum (Keystone / Tertiary / Secondary / Primary)
- `Vector2 FoodWebPosition` — node position in the food web graph UI
- `bool StartUnlocked` — false = silhouette until prerequisites met
- `List<SpeciesUnlockRequirement> UnlockRequirements` — prerequisites with min count

### 2. Build Tablet UI (Food Web Graph)
Full spec in **Prototype Specification** section above. Key pieces missing in Unity:

| Feature | Notes |
|---------|-------|
| Food web SVG canvas | Bubble nodes, trophic-tier colour rings, count badge, over/under glow |
| Predator arrow edges | Hidden by default; revealed on long-press (dim all, show connected edges + highlight predators) |
| Species lock/unlock system | Silhouette + "???" until prereqs met; progressive 3-level hint on repeated taps |
| Eco-health bar (GPU-wired) | Formula in Prototype Specification; currently not connected to GPU population |
| Over/underpopulation indicators | Ring colour + glow; Alucia warns when player taps an imbalanced node |
| Species info modal | Left: emoji + ADD button; Right: tier, sci name, role, "What's next" hint, count + balance status |
| Locked modal | Silhouette, progressive hint, requirements checklist (eco-health % + prey counts) |
| Current Organisms view | Toggle via chevron; trapezoid tank with grid; tap bubble → remove popup |
| Alucia NPC | Mermaid speech bubble, 3 states (default/warn/win), auto-hides 5.2 s |
| First-time reveal card | "New Species Discovered" overlay on reef panel, 5.5 s |
| Intro screen | Inside food-web panel; "REEF STATUS: CRITICAL" badge + Begin button |
| Reset | SpongeBob bubble-flood animation, state wipe, intro re-appears |
| Reef habitat visual | Layered flora growth, murk fade, ocean colour keyframes (GPU-side equivalent) |

### 3. Ecosystem Health Score + State Machine
**Health score (0–100) factors:**
- Biodiversity: fraction of species with living members
- Balance: prey:predator ratio per species
- Apex predator presence (shark weighted heavily)
- Stability: rate of population change

**States:** Healthy → Unstable → Critical → Collapsing → Recovering

### 4. Finish Netcode Client Setup
Resolve NetworkConfig mismatch — both host and client NetworkManagers must have the **exact same Network Prefabs List**. Register `EcosystemNetworkManagerGPU` prefab on the client's NetworkManager.

### 5. Preset Scenarios
- **Balanced Ocean**, **Shark Removed**, **Overpopulation**, **Collapse**, **Recovery**

---

## Recommended Population Dynamics Values

> ⚠ **`ReproRate` and `NaturalDeath` were deleted from `SpeciesDataGPU` in Week 8** — those two columns are historical reference only and no longer map to any field. Only **StarveRate** (`StarvationDeathRate`) and **StarveThreshold** (`StarvationThreshold`) still exist. Species below match the canonical list.

| Species | Tier | ~~ReproRate~~ | ~~NaturalDeath~~ | StarveRate | StarveThreshold |
|---------|------|-----------|--------------|------------|-----------------|
| Blacktip reef shark | Keystone | 0.02 | 0.01 | 0.30 | 0.20 |
| Brown-marbled grouper | Tertiary | 0.08 | 0.02 | 0.25 | 0.15 |
| Giant moray | Tertiary | 0.08 | 0.02 | 0.25 | 0.15 |
| Bluefin trevally | Secondary | 0.12 | 0.03 | 0.20 | 0.15 |
| Russell's snapper | Secondary | 0.12 | 0.03 | 0.20 | 0.15 |
| Yellowstripe scad | Secondary | 0.15 | 0.04 | 0.15 | 0.10 |
| Bluespotted ribbontail ray | Secondary | 0.10 | 0.03 | 0.20 | 0.15 |
| Fringelip mullet | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Bullethead parrotfish | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Streaked spinefoot | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Eyestripe surgeonfish | Primary | 0.20 | 0.05 | 0.00 | 0.00 |
| Reticulated damselfish | Primary | 0.22 | 0.05 | 0.00 | 0.00 |

Carrying capacity is now derived from `MaxSchools × FishPerSchool` (per-species fields on `SpeciesDataGPU`), not the old runtime `BoidsCount`.

---

## Scene Setup Reference

> The only scene enabled in the build is **`Assets/Junheng/Scenes/Boids_Demo.unity`** (verified in `EditorBuildSettings.asset`). Other scenes present: `Netcode Simulation Test` (Junheng), `Swirl_Demo` (Junheng), `Netcode Simulation Test 1` + `Health` (Aloysius), plus mockup/shader-test scenes under `Assets/_Assets/Scenes/`.

### Boids_Demo (host/trifold display scene — the build scene)
- GameObject with `BoidSimulationGPU` + `EcosystemSimulationGPU` (verified present)
- `BoidSpawnerGPUMultiTargets` per species + `BoidSimulationTargetAnimatorsSpawner` (verified present)
- `NetworkBootstrap` (Role: Host) present in scene
- ⚠ Currently only the **Clownfish placeholder** is wired into `EcosystemDefinitionGPU` — the 12 real species still need adding (same fixed order on host + tablet)
- `EcosystemNetworkManagerGPU` prefab registered in NetworkManager's Network Prefabs List

### Netcode Simulation Test (client/tablet scene — MAIN, JunHeng)
This is the canonical tablet client. `Netcode Simulation Test 1` (Aloysius) is only a UI prototyping scene fed into this one.
- `NetworkBootstrap` (Role: **Client**), same `EcosystemNetworkManagerGPU` prefab registered (must match host exactly)
- `ConnectionScreenUI` for IP entry / LAN auto-discovery (`LanDiscovery`)
- `TabletEcosystemUIGPU` — species→index lookup service
- Food-web UI (integrated from Aloysius): `SpeciesBubble`, `ModalController`, `FoodWebLines`, `Bob`, `SwipeToClose`
- ⚠ `Health.cs` eco-health bar exists in Aloysius's prototype scene; not yet confirmed wired into this main scene

---

## Known Issues / Watchpoints

- **🛑 CRASH — shark + water shader (suspected URP / Stylized Water opaque-texture interaction).**
  - **Repro:** when the **shark** enters the scene **together with the water shader**, the app crashes.
  - **Workarounds that run fine:** removing the **shader** alone, or the **shark** alone, runs without crashing.
  - **Oddity:** with the **shark + water + (some) other GameObject** all present, everything runs smoothly — so it appears to be a fragile state, not a clean reproduction.
  - **Suspected cause:** URP / Stylized Water shader not resolving correctly, likely tied to the **Opaque Texture** setting (camera/URP asset `_CameraOpaqueTexture`). The shark material rendering with the water shader's opaque-texture sampling may be the trigger.
  - **Status:** worked around (commit `b85296d` "fixing Crashing error" in `Boids_Demo.unity`), **not root-caused.** Next step: verify URP asset has Opaque Texture enabled and matches between desktop + mobile renderers, and test the shark material in isolation against the water shader.
- **Start-at-zero not yet play-tested** — `e13e26b` was committed without an in-editor run. First full add/remove cycle in the editor may surface buffer or affecter regressions.
- **Food web lines broken** — `FoodWebLines.cs` `LineRenderer` edges are present but hidden (`LINE FOOD WEB HIDE`). Marked "wonky, TO BE CHANGED." Predator arrows need a rework before they can be shown.
- **Eco-health bar not wired to GPU** — `Health.cs` exists in Aloysius's scene but is not reading live population data from the GPU simulation. Needs a bridge to `EcosystemSimulationGPU` or the netcode layer.
- **NetworkConfig mismatch** — client and host must have identical Network Prefabs Lists
- **`EcosystemDefinitionGPU.asset` species order** — all 12 species must be added in a fixed, shared order so index-based RPCs match between host and tablet
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
