# OceanX MP — Handoff Document
_Last updated: 2026-07-02_

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
| 6 | Population growth/decline + ecosystem health system | ✅ Done — ratio-driven predator-prey dynamics + eco-health score (GPU side) |
| 7 | Cascading effects + ecosystem state machine + codebase cleanup | 🔶 Partial — GPU cascade done, state machine not started, dead code removed |
| 8 | Movement systems — flocking + predator behaviour | ✅ Done (completed Week 5) |
| 9 | Event system + integration hooks for UI | 🔶 Partial — start-at-zero/extinction done; bounds derived from sim area; unlock C# events wired (`OnSpeciesUnlocked` / `OnUnlockStateChanged`); population/health/state events still partial |
| 10 | Preset scenarios + complete core system | ❌ Not started |
| 11 | Debugging, testing, system balancing | ❌ Not started |
| 12 | Final optimisation, bug fixing, project completion | ❌ Not started |

---

## Species List & Food Chain

> ✅ **CANONICAL species list (confirmed by JunHeng, 2026-06-12).** This table is now the single source of truth. The data assets and prototype must be aligned to it.
>
> **Asset delta — ✅ applied (2026-06-18):**
> - ➕ **Giant moray** (*Gymnothorax javanicus*) — data assets created and wired ✓
> - ➖ **Great barracuda** — dropped from the roster (excluded from the wired set) ✓
> - All 12 species now have data assets ✓
>
> **Superseded names** (do not use): Humphead wrasse, Crescent grunter, Lined surgeonfish (never had assets); Great barracuda (removed). The **prototype** (`oceanx-prototype.html`) still uses placeholder names (Striped Mullet, Convict Surgeonfish, Reef Manta Ray, Malabar Grouper…) — these were **remapped to this canonical list when the unlock system was wired (2026-06-18)**.
>
> **✅ Live sim now runs all 12 species** — `EcosystemDefinitionGPU.asset` has the full roster wired in fixed order, each with a matching `BoidSpawnerGPUMultiTargets` in `BoidSimulationGPU._gpuBoidSpawners`. (The old "1 species / Clownfish only" state is resolved.)
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
│   │   ├── BubbleSelectHook.cs              Per-bubble tap → selects species in TabletAddRemoveUIGPU (no SpeciesBubble edits)
│   │   ├── ConnectionScreenUI.cs           Client IP input + connect button
│   │   ├── EcosystemNetworkManagerGPU.cs   Syncs school counts + eco-health via NetworkList/NetworkVariable, RPCs
│   │   ├── HostSpawner.cs                  Spawns network manager prefab on server start
│   │   ├── LanDiscovery.cs                 UDP broadcast — tablet auto-finds host on WiFi
│   │   ├── NetworkBootstrap.cs             Host/Client role setup, starts NGO
│   │   ├── TabletAddRemoveUIGPU.cs          Singleton Add/Remove controller — fires RPCs for the selected species, greys at cap/0
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
└── Scripts/  (grown well past the original 6 — verified 2026-07-01)
    Core UI:    SpeciesBubble.cs · ModalController.cs · SpeciesInfoPanel.cs · TabController.cs · SwipeToClose.cs · DimFader.cs
    Food web:   FoodWebLines.cs · FoodWebDragReveal.cs · CurrentOrganismsGrid.cs · OrganismCardData.cs
    Health:     Health.cs (client/netcode bar) · HealthBarBinder.cs (host/large-screen bar, reads EcosystemSimulationGPU.EcoHealth01 direct) · EcoHealthDashboard.cs · EcoHealthChassis.cs
    Unlock:     GameState.cs + UnlockTester.cs (Aloysius placeholders) · LockedHintPanel.cs · SpeciesUnlockReveal.cs · NotificationManager.cs (used by JunHeng's EcosystemUnlockManagerGPU)
    NPC/FX:     AluciaController.cs · GodRays.cs · MarineSnow.cs · SonarPulse.cs · Bob.cs
    Data link:  SpeciesData.cs (UI asset — carries the gpuSpecies → SpeciesDataGPU link)
```

---

## What Is Currently Working

### GPU Ecosystem (EcosystemSimulationGPU.cs) — Active System
- **`SpeciesDataGPU`** — one asset per species holds all simulation SOs (FishSchoolProperties, FishMovementProperties, FishMotionRenderProperties, SpeciesBehaviorPropertiesGPU) plus population dynamics fields (`StarvationDeathRate`, `StarvationThreshold`, prey/predator lists)
- **Population tick** — coroutine ticks every 5s (configurable). **Natural births and natural deaths were removed** (Week 8). Population now changes from:
  - **Symmetric ratio-driven predator-prey dynamics** (Week 9) — each species feels a prey:predator balance ratio (in **school counts**) against a shared dead-band `[RatioBandLow, RatioBandHigh]` (default **1–3**):
    - few predators (ratio high) → prey grows; many predators (ratio low) → prey shrinks
    - prey abundant → predator grows (well-fed); prey scarce/gone → predator **starves** (hard override — can't grow without food)
    - inside the band → stable. Rolled at `GrowRate` / `ShrinkRate` (default 0.3/tick). Counts snapshotted each tick so updates are order-independent.
  - **Manual add/remove** via the UI / netcode RPCs
- **Eco-health score** (Week 9) — `EcoHealth01` (0–1) derived live from the **same ratios**: `diversity` (fraction of species alive) + `balance` (fraction within the band) + `apex present`, weighted (0.4/0.4/0.2). Synced to the tablet and drives `Health.cs`.
- **`AddSpecies` / `RemoveSpecies`** — public API for UI buttons and netcode RPCs. Species start at 0 schools; Add increments up to `MaxSchools`; Remove decrements to extinction (0). The ratio tick can also drive a species to extinction.
- **`CountGroups`** — returns current school count for UI display
- **`FishPerSchool` / `MaxSchools`** — per-species fields on `SpeciesDataGPU`; Add/Remove scales boid count by `FishPerSchool`; cap synced to clients
- **Prey/predator lists are now load-bearing** — `PreySpecies` / `PredatorSpecies` on `SpeciesDataGPU` drive BOTH the dynamics and eco-health. A species with empty lists won't participate (no growth/decline, ignored by balance).
- **Empty-ocean crash safety** — all GPU buffers sized `Mathf.Max(1, count)`; dispatch and render skipped when `_boidsCount == 0`
- **Simulation bounds derived** — `EcosystemSimulationGPU.SimulationBounds` reads from `BoidSimulationGPU.SimulationAreaBounds`; no more manual sync of two separate bounds assets
- `BoidSpawnerGPUMultiTargets` reads all spawn properties from `SpeciesDataGPU`

### GPU Netcode
- **Host/Client architecture** using Unity Netcode for GameObjects (NGO) over WiFi
- `NetworkBootstrap` — sets role (Host/Client), starts NGO
- `EcosystemNetworkManagerGPU` — auto-finds `EcosystemSimulationGPU` on server; syncs school counts via `NetworkList<int>` (periodic tick **+ immediate resync on add/remove**); exposes `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc`
- `TabletEcosystemUIGPU` — now a pure species→index lookup service (card UI stripped out entirely)
- **Decoupled tablet Add/Remove input layer (2026-06-29, `43fca49`)** — Add/Remove was extracted out of `ModalController` (which no longer touches netcode at all):
  - `TabletAddRemoveUIGPU` (singleton) holds the +/− buttons and optional population label; `Select(species)` resolves the netcode index via `TabletEcosystemUIGPU` and the buttons fire `RequestAddSpeciesRpc`/`RequestRemoveSpeciesRpc`; greys Add at `MaxSchools`, Remove at 0
  - `BubbleSelectHook` (one per species bubble) routes a bubble tap to `TabletAddRemoveUIGPU.Select(bubble.data.gpuSpecies)` **without editing the UI-team's `SpeciesBubble`** — add-component on each bubble, no per-bubble wiring
- `ConnectionScreenUI` — tablet IP entry screen + LAN auto-discovery

### Tablet UI (built by Aloysius, integrated into JunHeng's main `Netcode Simulation Test` scene)
- **Food web graph** — 12 species bubbles (`SpeciesBubble.cs`) laid out in trophic tiers; `FoodWebLines.cs` edges exist but are hidden pending visual fix
- **`ModalController.cs`** — species info modal with Add/Remove buttons wired to `RequestAddSpeciesRpc` / `RequestRemoveSpeciesRpc`; now also greys buttons at cap/0 (connected in `e13e26b`)
- **Eco-health bar — two drivers now:**
  - `Health.cs` (tablet client) reads the **networked** value `EcosystemNetworkManagerGPU.GetEcoHealth()` when `readFromSimulation` is on (needs host running + `fillImage` assigned)
  - `HealthBarBinder.cs` (host large screen, added by Aloysius `8d600f2`) reads **`EcosystemSimulationGPU.EcoHealth01` directly** — no netcode, auto-finds the sim. ⚠ Depends on JunHeng's `EcoHealth01` staying `public`.
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
- ✅ `Health.cs` now reads live eco-health from `EcosystemNetworkManagerGPU.GetEcoHealth()` (Week 9 code); in the standalone `Netcode Simulation Test 1` prototype scene it falls back to the manual value when no host is running

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

### ✅ Ratio-driven predator-prey dynamics + eco-health

Replaced the old per-species starvation cascade with a **symmetric, global ratio model**, and wired up the eco-health bar.

**`EcosystemSimulationGPU`:**
- `PopulationPressure(species, counts)` → `+1 / 0 / -1` from the prey:predator balance ratio (school counts) vs a global dead-band. Combines predation pressure (top-down) and food availability (bottom-up). **Starvation is a hard override** — no/low food always shrinks, so the keystone-collapse cascade can't be cancelled by "no predators."
- `RunPopulationTick` snapshots counts, then grows/shrinks each species via `AddSchool`/`RemoveSchool` rolled at `GrowRate`/`ShrinkRate`, one `ReinitializeBuffers` per tick.
- `ComputeEcoHealth01` / `EcoHealth01` (0–1) = diversity + balance + apex presence, weighted; reads the same ratios.
- New global tunables (Inspector): `RatioBandLow` (1), `RatioBandHigh` (3), `GrowRate` (0.3), `ShrinkRate` (0.3), health weights (0.4 / 0.4 / 0.2).

**`SpeciesDataGPU`:** removed the now-unused `StarvationDeathRate` / `StarvationThreshold`. Balance is global; per-species behaviour comes from `FishPerSchool` / `MaxSchools` / `PreySpecies` / `PredatorSpecies`.

**`EcosystemNetworkManagerGPU`:** added `NetworkVariable<float> _ecoHealth`, pushed each sync; `GetEcoHealth()` for clients.

**`Health.cs`:** now pulls live health from `EcosystemNetworkManagerGPU.Instance.GetEcoHealth()` (toggle `readFromSimulation`); null-guarded.

**Behaviour to expect:** prey with no predators climbs to its `MaxSchools` cap; a predator added before its prey starves out (gate this with the unlock UI); removing the shark makes mid fish overpopulate → over-predate their prey → then starve → oscillate, with eco-health dropping in step.

> ⚠ **Not yet play-tested in the Unity Editor.** Needs an in-editor run; tune the band/rates if it swings too hard or flatlines.

---

## What Was Done — 2026-06-18

> Work split by contributor. See `git log` for commit-level detail.

### JunHeng (simulation / backend + integration)

**Eco-health-gated species unlock system — new `EcosystemUnlockManagerGPU`**
(`Assets/Junheng/Scripts/Boids_GPU/Ecosystem/EcosystemUnlockManagerGPU.cs`)
- Ports the prototype's gate model: a locked species unlocks when **all** its
  `requires` (prey/support **school counts**) are met **AND** live eco-health %
  ≥ its `minHealth`. Latching / one-way, like the prototype.
- **Dual data source:** reads population + eco-health straight from
  `EcosystemSimulationGPU` when present (host / standalone — testable with
  `EcosystemDebugHarnessGPU`, no netcode needed), and falls back to the synced
  netcode layer (`EcosystemNetworkManagerGPU.GetPopulation` / `GetEcoHealth`) on
  the tablet client (where `_simulation` is left empty).
- **Drop-in replacement for Aloysius's placeholder `GameState`:** exposes
  `IsUnlocked`, `RegisterLockedTap` (progressive hints), `RefreshAllBubbles`, and
  fires `NotificationManager.ShowUnlocked` on each unlock; plus `OnSpeciesUnlocked`
  / `OnUnlockStateChanged` events and `GetLockInfo()` for the locked-modal
  requirement checklist. Optional `[Unlock]` console logging for headless testing.

**Species data model unified**
- `SpeciesData` (the UI/unlock asset) is now the single config per fish and links
  to its simulation species via a new **`gpuSpecies`** field (→ `SpeciesDataGPU`).
  Removed the unused `description` / `photo` fields. `SpeciesDataGPU` stays pure
  simulation data. Net: **one UI asset + one sim asset + one link** per fish — no
  more parallel species systems.

**Tablet UI wired to the real simulation**
- `SpeciesBubble.OnTap` resolves the species' netcode index via `gpuSpecies`
  (`TabletEcosystemUIGPU.GetSpeciesIndex`), so the modal's Add/Remove now drive the
  real sim and the population number shows — previously cosmetic-only (index -1),
  which is why Add was greyed and the count blank.
- `SpeciesBubble` reads lock state + hint progression from the unlock manager when
  present, falling back to `GameState` when it isn't — so **both** JunHeng's and
  Aloysius's setups run. **`GameState.cs` left untouched** (still Aloysius's
  placeholder).

**12 canonical species wired into the sim**
- All 12 `SpeciesDataGPU` added to `EcosystemDefinitionGPU` in fixed order, each
  with a per-species `BoidSpawnerGPUMultiTargets` registered in the
  `BoidSimulationGPU` **`_gpuBoidSpawners`** array — the list the sim actually
  reads. ⚠ Child spawner objects are **not** auto-scanned; a species in the
  definition list but missing from this array throws
  *"no spawner has SpeciesData 'X' assigned"* and won't spawn.
- Configured the 12 `SpeciesData` assets (gpuSpecies links, startUnlocked,
  minHealth, requires) using the **canonical** names — the prototype's placeholder
  names (Striped Mullet, Convict Surgeonfish, Reef Manta Ray, …) were remapped.

**Netcode + build**
- Verified host↔client over a `Boids_Demo` host + `Netcode Simulation Test` tablet
  client: add/remove RPCs, synced population, and the eco-health bar all working.
- Built Host + Tablet players under `Builds/Netcode Simulation Test/`.

> ⚠ **`minHealth` thresholds need tuning** against the real eco-health curve — the
> formula sits low early (diversity term), so a threshold like 70 can be
> effectively unreachable. Set them against what the bar actually reaches in play.

**Flocking / school tuning**
- Unified all 12 species' `FishSchoolProperties` (`Assets/Junheng/Data/Fish/**/*_SchoolProperties.asset`)
  to the Clownfish values: `VisionRange 15`, `ObstacleAvoidanceRange 3`,
  **`SeparationRange 0.35`**, `CohesionWeight 0.4`, `AlignmentWeight 0.35`,
  `SeparationWeight 1`, `TargetWeight 0.85`. Fixes fish spawning **too spread out** — the
  old `SeparationRange: 5` made them hold ~5 m gaps. `MovementProperties` /
  `MotionRenderProperties` left null on these assets (runtime-injected per species from
  `SpeciesDataGPU`). Apex/solitary species (shark, moray) now school tightly too — re-tune
  individually later if that looks unnatural.
- **Flocking model confirmed** (compute `BoidsGPU_Spatial_Partition.compute`): a boid's
  `BoidID` packs group ID (species, bits 0–7) + sub-group ID (school, bits 8–15).
  **Cohesion + alignment apply only within the same species AND the same school** — different
  species (and even different schools of one species) never merge. **Separation** applies to
  same-species + **larger** species (smaller species are ignored; species are size-ranked by
  group ID). So copying one species' school settings to all is safe — it doesn't blend species.

### Aloysius (UI / UX)

- **Shark info box** — new species info card / infobox for the shark (`0fe8695`).
- **Tap animation** — `TapPunch()` scale-punch on species-bubble tap in
  `SpeciesBubble` (`bff35e0`). Merged with JunHeng's netcode-index change in
  `OnTap` — both kept (animation fires, then the modal opens with a real target).
- **Lock/unlock placeholder** — `GameState` + `UnlockTester` remain his
  keyboard-driven placeholders for testing the unlock UI in isolation; kept as-is
  at his request. JunHeng's `EcosystemUnlockManagerGPU` is the production
  replacement, and `SpeciesBubble` works with either.
- Greyed-out food-web line styling for locked nodes.

### Akil (akeel-h) — scene environment / art
- Active contributor since ~2026-06-24. Owns the **3D scene environment**: coral
  placement, rockwork, mockup-scene redo, shader/rock-colour passes.
- Imported/added meshes & assets: fish + stingray assets, **parrotfish** mesh,
  **damselfish**; added the **shark** to the scene and adjusted rock colour.

---

## What Was Done — 2026-06-29 → 06-30

### JunHeng (simulation / backend + integration)

**Decoupled tablet Add/Remove input layer (`43fca49`)**
- New `BubbleSelectHook.cs` + `TabletAddRemoveUIGPU.cs` (under `Scripts/Networking/`).
  Add/Remove is now driven by a singleton controller fed by a per-bubble tap hook,
  instead of living inside `ModalController`. `ModalController` was slimmed by ~50
  lines and **no longer references netcode** (verified: no RPC/AddSpecies calls).
- Wiring contract: drop `BubbleSelectHook` on every species bubble (auto-reads
  `SpeciesBubble.data.gpuSpecies`), put one `TabletAddRemoveUIGPU` on an always-active
  object (e.g. the **Ecosystem Panel**, which also holds `TabletEcosystemUIGPU`), and
  assign its `addButton` / `removeButton` / `populationLabel`.

**Prey/predator relationships populated for all 12 species (`dac6700`)**
- `PreySpecies` / `PredatorSpecies` filled in on every `SpeciesDataGPU` asset from the
  food-chain table — the ratio dynamics + eco-health now have real edges to work with.
  (Was flagged "still worth a balance pass" in the 06-18 handoff; the lists themselves
  are now populated — values may still need tuning in play.)

**Clownfish placeholder dropped from the live sim (`e46bbd2`)**
- `EcosystemDefinitionGPU.asset` now wires exactly the **canonical 12** (Clownfish entry
  removed). The Clownfish data assets/mesh still exist on disk under
  `Data/Fish/Placeholder FIsh/Clownfish/` but are no longer referenced by the definition.

**Build scene switched to the tablet client**
- `EditorBuildSettings`: **`Assets/Junheng/Scenes/Netcode Simulation Test.unity` is now
  the enabled build scene** (Boids_Demo + Aloysius's client scene are present but
  disabled). ⚠ This supersedes the older "only Boids_Demo is in the build" note below.
- Multiple Android/host rebuilds across the team (`1001c91`, `eb0e945`, `8dd91c6`,
  `2a1d7d4`).

**New WIP client scenes (local, under `Assets/Junheng/Scenes/`)**
- `ALOYLOU VEFR @.unity` — JunHeng's current working tablet-client scene (newer/bigger
  than `Netcode Simulation Test`; 14 species bubbles). The decoupled Add/Remove layer
  had to be re-linked into it (11 plain bubbles hooked + controller added; the 3
  prefab-instanced bubbles — Shark, grouper, moray — and the controller's button refs
  are assigned in-editor).
- `Aloysius lololol.unity` — a 1-flag fork of `Netcode Simulation Test` with the
  `ConnectionScreen` GameObject disabled (UI-only inspection variant).
  > ⚠ Both of these are **not committed to git** at time of writing — treat as local
  > scratch/WIP until they land in a commit.

### Aloysius (UI / UX)
- New sprite for **locked organisms** (`114ac9e`); **seagrass** bubble (`243b9f3`);
  scene/build iterations (`502204d`, `6486453`, `4342b30`).

### Akil (akeel-h)
- See the Team section above — scene environment, coral/rockwork, fish/stingray/parrotfish/damselfish assets, shark added to scene.

---

## What Was Done — 2026-06-30 → 07-01

> Everything here post-dates the 06-30 handoff commit (`d57fbad`). Mostly teammate art/UI, but **two items land directly in JunHeng's host scene** — read the divergence note first.

### ⚠ Scene divergence — THREE "large screen" host scenes now exist (verified on disk 2026-07-01)

| Scene | Owner | Has the sim (`EcosystemSimulationGPU`)? | Health bar? | Notes |
|-------|-------|:---:|:---:|-------|
| `Assets/Junheng/Scenes/Boids_Demo.unity` | JunHeng | ✅ | ❌ | Canonical sim host — 12 spawners + netcode host role |
| `Assets/Aloysius/Boids_Demo.unity` | Aloysius | ✅ | ✅ | **A fork of JunHeng's Boids_Demo** with the large-screen eco-health bar + `HealthBarBinder` added (`8d600f2`) |
| `Assets/Akil/Scenes/SCENE_MainScene.unity` | Akil | ❌ | ❌ | Environment/art only — baked lighting, sky, reflection probes, coral, bubble particles; **no simulation** |

The sim lives in JunHeng's copy, the health bar in Aloysius's copy, the baked environment in Akil's copy. **These must converge into ONE host scene before the final build**, and until then editing the wrong `Boids_Demo` is a live trap (two of them both contain the sim and will drift apart).

### Aloysius (UI / UX)
- **Eco-health bar on the large screen (`8d600f2`)** — new script **`Assets/Aloysius/Scripts/HealthBarBinder.cs`**: drives a Filled `Image` + `%` TMP label straight from **`EcosystemSimulationGPU.EcoHealth01`** (auto-finds the sim, exponential smoothing, **no netcode** — host-side). Complements `Health.cs` (networked, tablet-side). Added into `Assets/Aloysius/Boids_Demo.unity` with new bar art (`Assets/Aloysius/New/heaklhtbarr.png`, `hhealthtth.png`) + `Assets/Aloysius/Prefabs/LinearProgress002Blue.prefab`. ⚠ Hard dependency on JunHeng's `EcoHealth01` staying `public`.
- **White infobox (`dcfa466`)** — new infobox art (`Assets/Aloysius/Info/WHITE KLAY.png`, `lighterbg.png`; removed `FRINGEEEEE.png`, `fish strroke.png`); renamed his client scene `help me burh.unity` → **`Assets/Aloysius/new netcode.unity`**.
- **`ALOYSIUS_UI_HANDOFF.md` fully rewritten (`99285d7`, `39e361c`)** — Aloysius maintains his own handoff doc at repo root; read it for the UI-side detail (his ~28-script suite, food-web/Alucia/notifications, etc.).

### Akil (akeel-h) — scene environment / lighting
- **Baked lighting into `Assets/Akil/Scenes/SCENE_MainScene.unity` (`e874fd3`, `90aa18a`, `1199aa0`)** — Lightmaps + `LightingData.asset` + reflection probes + new `Sky.mat`, adjusted scene colours. This is his **own** environment scene (no sim in it).
- **Bubble particles** added to the scene.
- ⚠ **Changed the shark material** — `Assets/.../Blacktip reef shark/Materials/defaultMat.mat` (`e874fd3`). Because the shark+water crash below is material/shader-sensitive, **re-test that crash after pulling this**.

---

## What Was Done — 2026-07-02

### JunHeng — Fish model asset prep for the swim shader (Blender)

Prepped the marine-creature meshes so they animate correctly under the `Fish_Lit` / `Fish_Swimming_Motion` shader. Done in **Blender 5.1** (driven over the Blender MCP). Source assets live **outside the repo** at `C:\Users\Admin\OneDrive\Documents\TP\year 3 sem 1\MP\assest\<species>\` — one folder per species (`.obj` + body/eye PNG textures, some with a `.mtl`).

**Why:** `Assets/Junheng/Shaders/Fish/Shared/Fish_Swimming_Motion.hlsl` reads its tail mask from **TEXCOORD1** (`float2 tailMaskUV : TEXCOORD1`), as `tailMask = saturate(pow(1.0 - tailMaskUV.x, _TailMaskFalloff))`. Each mesh therefore needs a **second UV channel (UV1)** whose **`.x` is a head→tail gradient (tail = 0.0 → head = 1.0)** — mask ≈1 at the tail (full wave), ≈0 at the head (rigid). UV0 (`UVMap`) stays the texture unwrap.

**⚠ Key point — UV1 is a MATH channel, NOT a texture unwrap.** It must not be seam-cut/unwrapped into islands: an island unwrap restarts U near 0 on every island, so the head of every segment reads as "tail" and wobbles. It's baked per-vertex as `UV1.x = (vert.z − z_min)/(z_max − z_min)` (models are oriented length-on-**Z**, head at **+Z**; head end confirmed via the eye-mesh centroid).

**Per-model pipeline applied:** import `.obj` → wire body/eye textures (MTL auto-wires to Base Color; no-MTL wired manually to the Principled BSDF) → bake UV1 on body + eyes (eye verts normalized in **body-local Z space** so eyes stay rigid at the head) → **join eyes into body** (one mesh, two material slots: body + eye) → clear all seams → save `.blend` next to the `.obj` → export **mesh-only FBX** (`use_selection`, `object_types={'MESH'}`, `mesh_smooth_type='FACE'`, `path_mode='COPY'`).

**⚠ Export must be FBX, not OBJ** — OBJ only stores one UV set and silently drops UV1. Blender writes `UVMap`→TEXCOORD0 and `UV1`→TEXCOORD1.

**Processed (FBX written next to each `.obj`):**

| Species | Source folder | Notes |
|---------|--------------|-------|
| Blacktip reef shark | `assest/Blacktip reef shark/` | `sharkv2_lowpoly.fbx` — folder renamed from `shark/`; ⚠ its `.blend` didn't survive the rename (only FBX + `.fbm` texture folder are there) |
| Bluespotted ribbontail ray | `assest/Bluespotted ribbontail ray/` | `stingray 1.fbx` — ⚠ tail-sway only (see caveat) |
| Reticulated damselfish | `assest/Reticulated damselfish/` | `damselfish.fbx` — no MTL, textures wired manually |
| Yellowstripe scad | `assest/Yellowstripe scad/` | `YellowstripeScad.fbx` — eye texture not referenced by MTL, wired manually |

**⚠ Ray caveat:** the ray got the same head→tail gradient, so under the fish tail-shader its **tail sways but the pectoral wings don't flap** (a tail-swimmer shader can't undulate ray wings). If a ray-specific wing shader is added later, its UV1 convention differs — re-bake that one.

**⚠ These FBXs are NOT in the Unity project yet.** They sit in the OneDrive `assest/` folder. To use them: copy each `.fbx` (+ its `.fbm` texture folder) into `Assets/` (Akil owns scene-art import), assign the `Fish_Lit` material, and confirm `UV1` imports as UV1/TEXCOORD1. With `UV1.x` = tail 0 / head 1 against the shader's `1.0 - tailMaskUV.x`, the tail waves while head + eyes stay rigid out of the box. (If the head wobbles instead, the channel got flipped.)

> **Shark rebaked 2026-07-02 (scale fix).** The Blacktip shark FBX was re-exported to fix the unit-scale bug in **Gotcha B** below. New file (UnitScaleFactor 100, UV1 baked) is in `assest/Blacktip reef shark/sharkv2_lowpoly.fbx` + `.blend`. Still needs copying into `Assets/` over the old one.

### ⚠⚠ Fish asset gotchas & checklist — learned the hard way, don't repeat

A whole session was lost to the three traps below. Read this before importing/prepping any new fish.

**A. Why UV1 is *baked*, not unwrapped (and how).**
The swim shader (`Fish_Swimming_Motion.hlsl`) reads a per-vertex tail-mask from **TEXCOORD1** — `tailMask = saturate(pow(1.0 - tailMaskUV.x, _TailMaskFalloff))`. It needs **one smooth head→tail gradient**, `UV1.x` = **tail 0.0 → head 1.0**. This is a *math* channel, so:
- **Do NOT seam-cut / Unwrap / Reset / Project-From-View it.** An island unwrap restarts U at ~0 on every island, so the front of every piece reads as "tail" and the whole fish wobbles.
- **Bake it numerically:** for every vertex `UV1.x = (vert.z − z_min)/(z_max − z_min)` (models are length-on-**Z**, head at **+Z** — confirm head via the eye-mesh centroid). Eyes = a separate mesh, so normalize their verts **in the body's local-Z range** (then join eyes into body) so they read ~1 and stay rigid at the head.
- Keep `UVMap` (UV0) as the texture unwrap + active-render channel. UV0 = texturing, UV1 = swim math. Never swap them.

**B. FBX unit-scale bug — the "big in scene, tiny when instanced" trap.** ← the one that cost the most time
- **Symptom:** dragging the FBX into a scene shows it at a normal size, but its Transform reads **scale 100**; when the GPU sim renders it (instanced), it's **tiny**. Other fish are 1-to-1.
- **Cause:** GPU instancing (`RenderMeshIndirect`) draws the **raw mesh** with only per-boid position+rotation — it **ignores the Transform scale**. If the mesh imported tiny with a 100× root, instancing draws the tiny mesh. The tiny+100root happens when the FBX is exported in **metres** (FBX `UnitScaleFactor = 1`) → Unity applies fileScale **0.01** and compensates with root **100**. Working fish are exported in **centimetres** (`UnitScaleFactor = 100`) → fileScale 1, root 1.
- **Check it:** in the `.fbx.meta`, a **working** fish has `bakeAxisConversion: 1` and the file-scale (`humanDescription.globalScale`) ≈ **1**; a **broken** one has `bakeAxisConversion: 0` and file-scale **0.01**.
- **Fix (Blender export):** export with `apply_unit_scale=True` **and** `apply_scale_options='FBX_SCALE_ALL'`, `global_scale=1.0`. Verify the exported FBX's `UnitScaleFactor == 100` (must match the working fish) — parse it from the binary if unsure. Do **not** hand-scale the mesh to "fix" it; that doesn't remove the root-100 and it breaks the swim (see D).

**C. Instanced material gotcha — the `_Boids` D3D12 error.**
- Error: *`Fish_Lit_Instanced requires a buffer (SRV) _Boids ... none provided`*. It means a `Fish_Lit_Instanced` material is being drawn **outside** the sim's indirect-draw path (the sim binds `_Boids` via a `MaterialPropertyBlock` in `BoidSpawnerGPU.RenderBoids`; a plain scene MeshRenderer, or a mis-set-up material, has no buffer).
- **Rules:** (1) `Fish_Lit_Instanced` materials belong **only** on a spawner's `BoidMaterial`, never on a scene MeshRenderer (use the non-instanced `Fish_Lit` for scene/hero objects). (2) The material **must have "Enable GPU Instancing" ON** (`m_EnableInstancingVariants: 1`). (3) **Do NOT hand-build the instanced material** (swapping the shader on a URP-Lit base leaves it missing passes/props); **duplicate a known-good one** (`Clownfish_Instanced.mat`) and just change its textures.

**D. Mesh scale ↔ swim tuning are coupled.**
The swim uses `position.z / _TailWaveLength` (native mesh units) and `sideToSide * 0.01`. If the mesh's native size changes by ×N, the tail wave gets N× tighter and side-to-side gets N× weaker. So **after any mesh-scale change, scale `_TailWaveLength` (and side-to-side amplitude) by the same factor.** Rotation amplitudes (roll/yaw/panning) and `_TailMaskFalloff` are angle/UV-based → scale-invariant, leave them.

**E. Where the swim values live.** Material floats `_TailWaveLength` / `_TailMaskFalloff` stay on the **instanced material** (the sim's `BoidMaterial`). The five *animated* amplitudes are runtime-injected from each species' **`FishMotionRenderProperties`** asset (referenced by `SpeciesDataGPU`) — material values there are overwritten at runtime. Map material→SO: `_AutomaticSwimSpeed`→SwimPlaybackSpeed, `_SideToSideAmplitude`→SideToSide, `_YawRotationAmplitude`→Yaw, `_TailRollAmplitude`→Roll, `_TailYawAmplitude`→PanningYaw (each Min=cruise, Max=full-accel).

**New-fish checklist:** import OBJ → wire body/eye textures → **bake** UV1 (A) → join eyes → clear seams → export FBX with `FBX_SCALE_ALL`, verify `UnitScaleFactor==100` (B) → in Unity, duplicate a working `*_Instanced.mat` + swap textures (C) → point the spawner's `BoidMesh`/`BoidMaterial` at it → set swim `_TailWaveLength` for the mesh's true size (D) → convert tuned amplitudes into the species' `FishMotionRenderProperties` (E).

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

The prototype uses a **locked progression** model: only 2 species are available at start; the rest unlock gate-by-gate as the player adds prey first. **✅ Implemented in Unity (2026-06-18)** via `EcosystemUnlockManagerGPU`: unlock config lives on the `SpeciesData` assets (`startUnlocked`, `minHealth`, `requires`) and the manager gates on live eco-health % + school counts (latching). Progressive 3-level hints run through `SpeciesBubble.ShowLockedHint`. **Remaining:** the locked-modal requirement checklist UI (the data is already exposed via `EcosystemUnlockManagerGPU.GetLockInfo`).

---

## What Needs Building Next (Priority Order)

### ~~1. Start-at-Zero / School-Scaling / Extinction model~~ ✅ Done (`e13e26b`)

Implemented in commit `e13e26b`. Player builds from empty ocean; Add/Remove scale schools; extinction at 0; `MaxSchools` cap; crash-safe empty-ocean state. **Needs an in-editor play-test with full add/remove cycles to confirm no regressions.**

### ~~1. Finalise the 12 canonical species + wire them into the sim~~ ✅ Done (2026-06-18)
The 12 canonical species are created and wired into `EcosystemDefinitionGPU.asset` in fixed order,
each with a matching `BoidSpawnerGPUMultiTargets` in `BoidSimulationGPU._gpuBoidSpawners`. Giant
moray added; Great barracuda dropped. `PreySpecies` / `PredatorSpecies` populated from the
food-chain table. **Still worth a balance pass** on the prey/predator lists + `MaxSchools` values.

> **Where the UI/unlock fields ended up:** these did **not** go on `SpeciesDataGPU` as originally
> planned. Unlock config lives on the UI asset **`SpeciesData`** (`startUnlocked`, `minHealth`,
> `requires`, hints), linked to its sim species via a `gpuSpecies` field. `SpeciesDataGPU` stays
> pure simulation data. Remaining UI-only fields (Icon, TrophicTier, FoodWebPosition) can be added
> to `SpeciesData` as the food-web graph UI is built.

### 1b. Reconcile the three host scenes (NEW — 2026-07-01)
There are now three "large screen" scenes and only JunHeng's has the live sim. Decide the canonical host scene and merge into it: the **simulation** (from `Assets/Junheng/Scenes/Boids_Demo.unity`), the **large-screen eco-health bar + `HealthBarBinder`** (from `Assets/Aloysius/Boids_Demo.unity`), and the **baked environment/lighting** (from `Assets/Akil/Scenes/SCENE_MainScene.unity`). Do this before the final build — two of the three both contain `EcosystemSimulationGPU` and will keep drifting until merged.

### 2. Build Tablet UI (Food Web Graph)
Full spec in **Prototype Specification** section above. Key pieces missing in Unity:

| Feature | Notes |
|---------|-------|
| Food web SVG canvas | Bubble nodes, trophic-tier colour rings, count badge, over/under glow |
| Predator arrow edges | Hidden by default; revealed on long-press (dim all, show connected edges + highlight predators) |
| Species lock/unlock system | ✅ Logic done (`EcosystemUnlockManagerGPU` + `SpeciesBubble`): lock state, eco-health/requirements gating, progressive 3-level hints. Remaining: silhouette/"???" visuals + locked-modal checklist |
| Eco-health bar (GPU-wired) | ✅ `Health.cs` reads `GetEcoHealth()` from the sim/netcode. Remaining: the prototype's low/mid/high colour states |
| Over/underpopulation indicators | Ring colour + glow; Alucia warns when player taps an imbalanced node |
| Species info modal | Left: emoji + ADD button; Right: tier, sci name, role, "What's next" hint, count + balance status |
| Locked modal | Silhouette, progressive hint, requirements checklist (eco-health % + prey counts) |
| Current Organisms view | Toggle via chevron; trapezoid tank with grid; tap bubble → remove popup |
| Alucia NPC | Mermaid speech bubble, 3 states (default/warn/win), auto-hides 5.2 s |
| First-time reveal card | "New Species Discovered" overlay on reef panel, 5.5 s |
| Intro screen | Inside food-web panel; "REEF STATUS: CRITICAL" badge + Begin button |
| Reset | SpongeBob bubble-flood animation, state wipe, intro re-appears |
| Reef habitat visual | Layered flora growth, murk fade, ocean colour keyframes (GPU-side equivalent) |

### 3. Ecosystem State Machine (health score ✅ done)
**Health score ✅** — `EcosystemSimulationGPU.EcoHealth01` (diversity + balance + apex, weighted) is built and synced; the bar moves. Remaining:

**State machine (not built):** Healthy → Unstable → Critical → Collapsing → Recovering — derive from the `EcoHealth01` value (and/or its rate of change) and expose it for the UI / Alucia warnings.

### 4. Finish Netcode Client Setup
Resolve NetworkConfig mismatch — both host and client NetworkManagers must have the **exact same Network Prefabs List**. Register `EcosystemNetworkManagerGPU` prefab on the client's NetworkManager.

### 5. Preset Scenarios
- **Balanced Ocean**, **Shark Removed**, **Overpopulation**, **Collapse**, **Recovery**

---

## Population Dynamics Values

> ⚠ **The per-species rate fields are gone.** `ReproRate` / `NaturalDeath` were deleted in Week 8; `StarvationDeathRate` / `StarvationThreshold` were deleted in Week 9 when the model became a **global, ratio-driven** system. Population behaviour is now tuned by **global constants** on `EcosystemSimulationGPU` plus **per-species** `FishPerSchool` / `MaxSchools` / prey-predator lists.

### Global tuning (on the `EcosystemSimulationGPU` component)
| Constant | Default | Meaning |
|----------|---------|---------|
| `RatioBandLow` | 1 | below this prey:predator ratio (schools) → prey shrinks / predators starve |
| `RatioBandHigh` | 3 | above this → prey overpopulates / well-fed predators grow |
| `GrowRate` | 0.3 | per-tick chance an out-of-band species gains a school |
| `ShrinkRate` | 0.3 | per-tick chance an out-of-band species loses a school |
| `_tickInterval` | 5 s | seconds between population ticks |
| Eco-health weights | 0.4 / 0.4 / 0.2 | diversity / balance / apex |

### Per-species (on each `SpeciesDataGPU` asset)
- `FishPerSchool` — fish per school (constant density)
- `MaxSchools` — hard cap; carrying capacity = `MaxSchools × FishPerSchool`
- `PreySpecies` / `PredatorSpecies` — **required**: they drive both the dynamics and eco-health (empty lists = species doesn't participate)

Tune the global band/rates live in Play mode by watching whether the reef settles or collapses.

---

## Scene Setup Reference

> ⚠ **Updated 2026-06-30:** the enabled build scene is now **`Assets/Junheng/Scenes/Netcode Simulation Test.unity`** (the tablet client), verified in `EditorBuildSettings.asset`. `Boids_Demo` and Aloysius's `Netcode Simulation Test` are listed but **disabled**. (Previously Boids_Demo was the only enabled scene.) Other scenes present: `Swirl_Demo`, `ALOYLOU VEFR @`, `Aloysius lololol` (Junheng, local WIP), `Netcode Simulation Test 1` + `Health` (Aloysius), plus mockup/shader-test scenes.

### Boids_Demo (host/trifold display scene — the build scene)
- GameObject with `BoidSimulationGPU` + `EcosystemSimulationGPU` (verified present)
- `BoidSpawnerGPUMultiTargets` per species + `BoidSimulationTargetAnimatorsSpawner` (verified present)
- `NetworkBootstrap` (Role: Host) present in scene
- ✅ All 12 species wired into `EcosystemDefinitionGPU` in fixed order, each with a matching `BoidSpawnerGPUMultiTargets` registered in `BoidSimulationGPU._gpuBoidSpawners` (the tablet's `TabletEcosystemUIGPU.Ecosystem` must point at the same definition asset/order)
- `EcosystemNetworkManagerGPU` prefab registered in NetworkManager's Network Prefabs List

### Netcode Simulation Test (client/tablet scene — MAIN, JunHeng)
This is the canonical tablet client. `Netcode Simulation Test 1` (Aloysius) is only a UI prototyping scene fed into this one.
- `NetworkBootstrap` (Role: **Client**), same `EcosystemNetworkManagerGPU` prefab registered (must match host exactly)
- `ConnectionScreenUI` for IP entry / LAN auto-discovery (`LanDiscovery`)
- `TabletEcosystemUIGPU` — species→index lookup service
- Food-web UI (integrated from Aloysius): `SpeciesBubble`, `ModalController`, `FoodWebLines`, `Bob`, `SwipeToClose`
- ✅ `Health.cs` eco-health bar reads `EcosystemNetworkManagerGPU.GetEcoHealth()` (assign its `fillImage`); `EcosystemUnlockManagerGPU` also lives here (client) with `_simulation` left empty so it reads via netcode

---

## Known Issues / Watchpoints

- **🛑 CRASH — shark + water shader (suspected URP / Stylized Water opaque-texture interaction).**
  - **Repro:** when the **shark** enters the scene **together with the water shader**, the app crashes.
  - **Workarounds that run fine:** removing the **shader** alone, or the **shark** alone, runs without crashing.
  - **Oddity:** with the **shark + water + (some) other GameObject** all present, everything runs smoothly — so it appears to be a fragile state, not a clean reproduction.
  - **Suspected cause:** URP / Stylized Water shader not resolving correctly, likely tied to the **Opaque Texture** setting (camera/URP asset `_CameraOpaqueTexture`). The shark material rendering with the water shader's opaque-texture sampling may be the trigger.
  - **Status:** worked around (commit `b85296d` "fixing Crashing error" in `Boids_Demo.unity`), **not root-caused.** Next step: verify URP asset has Opaque Texture enabled and matches between desktop + mobile renderers, and test the shark material in isolation against the water shader.
  - ⚠ **Update 2026-07-01:** akeel-h changed the shark material (`Blacktip reef shark/Materials/defaultMat.mat`, `e874fd3`). Since this crash is material/shader-sensitive, re-verify it after pulling that commit.
- **🔀 Scene divergence — three host/large-screen scenes (2026-07-01).** `Assets/Junheng/Scenes/Boids_Demo.unity` (sim, no bar), `Assets/Aloysius/Boids_Demo.unity` (fork with sim + health bar), `Assets/Akil/Scenes/SCENE_MainScene.unity` (environment/lighting, no sim). Two of the three both contain `EcosystemSimulationGPU` and will drift. Pick the canonical host scene and merge the health bar + baked environment into it before the final build. See the "2026-06-30 → 07-01" section for the table.
- **Start-at-zero not yet play-tested** — `e13e26b` was committed without an in-editor run. First full add/remove cycle in the editor may surface buffer or affecter regressions.
- **Food web lines broken** — `FoodWebLines.cs` `LineRenderer` edges are present but hidden (`LINE FOOD WEB HIDE`). Marked "wonky, TO BE CHANGED." Predator arrows need a rework before they can be shown.
- **Eco-health bar — now wired** (Week 9). `Health.cs` reads `EcosystemNetworkManagerGPU.Instance.GetEcoHealth()` when `readFromSimulation` is on. To work it needs: the network manager spawned (host), `Health.fillImage` assigned, and prey/predator lists filled so the score is meaningful. The **state machine** (Healthy/Unstable/Critical/…) is still not built — only the 0–1 score.
- **NetworkConfig mismatch** — client and host must have identical Network Prefabs Lists
- **`EcosystemDefinitionGPU.asset` species order** — all 12 species must be added in a fixed, shared order so index-based RPCs match between host and tablet
- **Species UI fields split across two assets** — unlock config (`startUnlocked`, `minHealth`, `requires`, hints) lives on **`SpeciesData`** (linked to the sim via `gpuSpecies`); `SpeciesDataGPU` stays pure sim data. Remaining UI-only fields (Icon, TrophicTier, FoodWebPosition) still to add to `SpeciesData` for the food-web graph
- **🐟 Fish FBXs prepped but not imported (2026-07-02).** Blacktip reef shark, Bluespotted ribbontail ray, Reticulated damselfish, Yellowstripe scad each have a `UV1` tail-mask gradient baked and a mesh-only FBX exported — but the FBXs live in the OneDrive `assest/` folder, **not in `Assets/`**. Copy them in (+ `.fbm`), assign `Fish_Lit`, and verify `UV1`→TEXCOORD1 imports. The ray only tail-sways (no wing flap). See "What Was Done — 2026-07-02" for the full pipeline.
- **Duplicate AudioListener** — multiple cameras in scene, keep exactly one active
- **`Boids_Simulation_CPU` GameObject in Boids_Demo** — disabled, holds missing script refs to deleted CPU scripts. Safe to delete from scene
- **Add/Remove wiring is a re-link trap when duplicating client scenes** — the decoupled input layer (`BubbleSelectHook` on every bubble + a `TabletAddRemoveUIGPU` with its buttons assigned) lives in the scene, not on a prefab, so a copied/new client scene (e.g. `ALOYLOU VEFR @`) loses it and taps do nothing until it's re-added. If population shows but Add/Remove are dead, check: hooks present on bubbles, controller present + buttons assigned, and `TabletEcosystemUIGPU.Ecosystem` points at the **same** `EcosystemDefinitionGPU` (same order) as the host.

---

## Team Structure

| Role | Person |
|------|--------|
| Simulation / backend + integration | JunHeng |
| UI / UX (tablet food-web, modals, Alucia) | Aloysius |
| Scene environment / 3D art (coral, rockwork, meshes) | Akil (akeel-h) |

Each person has their own Claude session. Share context via this file and `CLAUDE.md` (project root), both committed to git.
