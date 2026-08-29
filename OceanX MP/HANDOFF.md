# Restore the Reef — Handoff Document

_Last updated: 2026-08-29 — sim/backend sections caught up through `23c2751c`. **Not yet documented:** `GuidedTutorial.cs` (new, `18478329`, Aloysius) and the 2026-08-28 moray cave changes (`d4beee0c`, Akil) — §7.18 and §7.24 are stale by their owners' work._

> This is the single source of truth for the project. Update the date above whenever you edit this file.
> Formerly named **OceanX MP** / **Balance the Ocean** — renamed to **Restore the Reef** 2026-07-26.

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [How to Use It](#2-how-to-use-it)
3. [Sprint Plan Status](#3-sprint-plan-status)
4. [Species List & Food Chain](#4-species-list--food-chain)
5. [Codebase Structure](#5-codebase-structure)
6. [Scene Architecture](#6-scene-architecture)
7. [What Has Been Implemented](#7-what-has-been-implemented)
8. [Prototype Specification](#8-prototype-specification)
9. [Things Tried That Didn't Work — Avoid](#9-things-tried-that-didnt-work--avoid)
10. [Known Issues / Watchpoints](#10-known-issues--watchpoints)
11. [What Needs Building Next / To Do](#11-what-needs-building-next--to-do)
12. [Reference — Population Dynamics Values](#12-reference--population-dynamics-values)
13. [Reference — Scene Setup](#13-reference--scene-setup)
14. [Team Structure](#14-team-structure)

---

# 1. Introduction

## What the project is

An interactive Unity ocean ecosystem simulation built as an **educational tool** for a museum/exhibit setting.

**Problem it solves:** Lack of experiential learning tools limits ocean literacy and systems thinking.

**What the user does:**
- Opens a Food Chain view (icon → overlay) and taps animals to read species info.
- Adds or removes marine species using UI buttons on a tablet.
- Watches cascading effects unfold in real time on a big trifold screen.

**What they learn:**
- Marine ecosystems are interconnected systems.
- Sharks (apex predators) are critical to maintaining balance.
- Removing one species causes a chain reaction across the food chain.

**Core demo moment:** Remove the blacktip reef shark → groupers and barracuda overpopulate → primary consumers collapse from over-predation → secondary consumers starve and collapse too. From a healthy 100% reef: **remove the shark → the bar drops straight to 73%.** One tap, and a quarter of the reef's health is gone. That's the whole point of the exhibit: the shark isn't a danger to the reef; it's what holds it together. Add it back → straight to 100%.

## Hardware setup

| Device | Role | Runs |
|--------|------|------|
| PC (Windows) | **Host** — GPU simulation authority | Big-screen (trifold) build; hosts the netcode session |
| Android tablet | **Client** — visitor UI | Tablet build; connects to host over LAN |
| Trifold display | Big-screen output | Attached to the host PC; shows the simulation, Alucia, reveal cards |

Both devices must be on the **same WiFi** network. If the venue WiFi doesn't cooperate, use a hotspot or personal WiFi.

---

# 2. How to Use It

## Connecting

1. Run the **Host** build on the PC.
2. Run the **Tablet** build on the Android tablet.
3. Both devices must be on the same WiFi.
4. The tablet **auto-discovers the PC** via UDP broadcast: it shows "Searching…" then "Connected!"
5. If it can't find it: use a hotspot or personal WiFi (venue WiFi sometimes blocks broadcast).

## Using the tablet

### Food Web tab

- **Tap** a fish → opens its info card: Name, Role, short Description, and **[+]** button.
- Click **"View Details"** on the info card to see more information.
- **Hold** a fish → reveals its food chain (who it eats + what eats it).
- Add fish with **[+]**.
- Everything is counted in **groups**, not individual fish. **One tap = one group.**

### Ecosystem tab

- Shows everything currently living in the reef.
- Remove fish with **[−]**.

### Hints panel

- Tells the player what the next locked fish is waiting for.
- Normal add/remove has **no cooldown** (removed 2026-07-29). Only a **first-time add** of a species (0→1) briefly locks out adding while the big-screen reveal card + intro camera play (~6s).

## Unlocking

- Player starts with the **five plant-eaters** — everything else is **locked**.
- A locked fish is waiting for two things: **its food already in the water** AND the reef **healthy enough**.
- Build the bottom of the food chain, and the hunters unlock on their own. Check the **Hints** tab if something is missing.
- Watch the **big screen**: when a fish unlocks, or when a species is added for the first time, a **reveal card** appears announcing it.

## Using the host (big screen)

### Start

- The ecosystem is **destroyed at the start** of the experience — no coral, no vegetation.
- Add fish and balance the food web to help fix the ecosystem.
- Alucia introduces herself and asks for the player's help to fix the ecosystem.

### Warnings

Alucia warns the player when something's off balance:
- **Overpopulated** — too many of one fish, not enough predators to keep it in check.
- **Starving / Underpopulated** — not enough food to go around.
- She also gives hints from time to time.

The health bar reacts the moment a player taps. Nothing changes on its own, so the player is always in control.

## Getting to 100%

Once everything is unlocked, this exact mix gives 100% eco-health:

**The hunters — exactly 1 group each:**

| Fish | Groups |
|------|:---:|
| Blacktip reef shark | 1 |
| Brown-marbled grouper | 1 |
| Giant moray | 1 |
| Bluefin trevally | 1 |
| Russell's snapper | 1 |
| Yellowstripe scad | 1 |

**The smaller fish (grazers) — range:**

| Fish | Groups |
|------|:---:|
| Reticulated damselfish | 6 |
| Fringelip mullet | 5–6 |
| Eyestripe surgeonfish | 5–7 |
| Bullethead parrotfish | 4–10 |
| Streaked spinefoot | 4–8 |
| Bluespotted ribbontail ray | 1–4 |

Each grazer can range from its "predator-count" floor up to its cap and stay at 100%; the 7 hunters are locked at 1 (each extra predator raises the grazers' floor).

## Operator controls (reset for next visitor)

Hold **F9** on the host for ~1.5 seconds → the reset chain runs, hidden behind a **SpongeBob-style bubble wipe** on the big screen.

- Ocean empties (all species → 0 schools).
- Species re-lock to the 5 starters.
- Eco-health drops to 0.
- Both screens return to the "Tap to Start" attract state.
- Intro is re-armed for the next visitor.

The tablet flips back to the title screen (no bubble wipe on the tablet — just the flip).

---

# 3. Sprint Plan Status

| Week | Sprint | Status |
|------|--------|--------|
| 1 | Research and concept development | ✅ Done |
| 2 | Planning, system design, task allocation | ✅ Done |
| 3 | Core simulation manager + species data system | ✅ Done |
| 4 | Spawning, removal, population tracking | ✅ Done |
| 5 | Food chain relationships + predator-prey logic | ✅ Done |
| 6 | Population growth/decline + ecosystem health system | ✅ Done — ratio-driven predator/prey dynamics + eco-health score |
| 7 | Cascading effects + ecosystem state machine + codebase cleanup | 🔶 Partial — GPU cascade done, formal state-machine enum not built (health-band Alucia reactions cover it), CPU layer removed |
| 8 | Movement systems — flocking + predator behaviour | ✅ Done (completed Week 5 in the schedule) |
| 9 | Event system + integration hooks for UI | ✅ Done — start-at-zero/extinction, netcode + tablet add/remove, C# events (species / unlock / health-band) all wired |
| 10 | Preset scenarios + complete core system | ❌ Not started |
| 11 | Debugging, testing, system balancing | 🔶 In progress — win condition + win screens, UI audio pass, moray cave navigation, tablet UI polish (Jul 30 – Aug 10) |
| 12 | Final optimisation, bug fixing, project completion | ❌ Not started |

---

# 4. Species List & Food Chain

**Canonical species list — confirmed by JunHeng, 2026-06-12.** This table is the single source of truth. Data assets and prototype must align to it.

**All 12 species have data assets.** `EcosystemDefinitionGPU.asset` has the full roster wired in fixed order, each with a matching `BoidSpawnerGPUMultiTargets` in `BoidSimulationGPU._gpuBoidSpawners`.

**Superseded names — do not use:** Humphead wrasse, Crescent grunter, Lined surgeonfish (never had assets); Great barracuda (removed 2026-06-18). The prototype (`oceanx-prototype.html`) still uses placeholder names (Striped Mullet, Convict Surgeonfish, Reef Manta Ray, Malabar Grouper…) — these were **remapped to this canonical list when the unlock system was wired**.

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

**Total: 12 species** — 1 Keystone, 2 Tertiary, 4 Secondary, 5 Primary.

> ⚠ **Streaked spinefoot = "rabbitfish"** — *Siganus javus* is commonly called the rabbitfish, so the mesh/folder churn naming it `rabbitfish.fbx` (and akeel-h's *"FBX changes for damselfish and rabbitfish"* commit) is the **same species**, not a new one.

---

# 5. Codebase Structure

The dead Ecosystem CPU layer has been removed. The active product runs entirely on the GPU simulation pipeline. All simulation scripts live directly under `Assets/Junheng/Scripts/` — the old `Junheng/Ecosystem/Scripts/` and `Junheng/Simulation/Scripts/` splits no longer exist, and there is no `03_` prefix on the spatial-partition folder.

```
Assets/Junheng/
├── Scripts/
│   ├── DualMonitor.cs                            Activates Display 2 (Spacedesk/iPad) on startup
│   ├── Boids_GPU/
│   │   ├── AffecterGPU.cs
│   │   ├── BoidInfoGPU.cs                        Per-boid struct — 18 floats incl. SignedTurnRate (for ray tail-sway)
│   │   ├── BoidRenderInfoGPU.cs
│   │   ├── BoidSchoolInfoGPU.cs
│   │   ├── BoidSimulationBaseGPU.cs              Abstract GPU sim base — owns shared compute buffers
│   │   ├── BoidSimulationTargetAnimatorsSpawner.cs
│   │   ├── BoidSpawnerGPU.cs                     GPU spawner — position preservation + moray spine render hooks
│   │   ├── BoidSpawnerGPUMultiTargets.cs         Active spawner used in production scenes
│   │   ├── BoidSwirlSpawnerGPU.cs
│   │   ├── Ecosystem/
│   │   │   ├── EcosystemDefinitionGPU.cs         Species list + simulation bounds asset
│   │   │   ├── EcosystemSimulationGPU.cs         Population tick + start-at-zero add/remove + entry/exit swim-in-out
│   │   │   ├── EcosystemTargetGPU.cs             Per-school swim target (ParkAt drives swim-out) — REPLACED WanderingAffecterGPU
│   │   │   ├── EcosystemUnlockManagerGPU.cs      Eco-health/prey-gated species unlock system (singleton)
│   │   │   ├── EcosystemDebugHarnessGPU.cs       In-editor OnGUI add/remove panel (no netcode) — dev-only
│   │   │   ├── FishEntryPointGPU.cs              Off-screen entry/exit markers; schools swim in/out via these (auto-registers)
│   │   │   ├── IntroductionCameraDirectorGPU.cs  Cinemachine — catches a species' first school at its gate & follows it in (0→1 only); host-only
│   │   │   ├── MorayCave.cs                      Cave marker (mouth + ordered child path); self-registers to MorayCave.All  [Akil, 2026-07-30]
│   │   │   ├── MorayCaveDirector.cs              Moray AI — claims a cave, drives the school target in, pins it head-out via GPU rest anchors  [Akil]
│   │   │   ├── SharkPatrolDirector.cs            Blacktip shark patrol — drives the school target along the Waypoints loop instead of a random path  [2026-08-18]
│   │   │   ├── EcosystemUIAdapterGPU.cs          UI→GPU bridge — ⚠ DEAD (zero external refs, confirmed 2026-07-08); safe to delete
│   │   │   ├── SpeciesBehaviorPropertiesGPU.cs   Flee/hunt/hunger SO (⚠ currently UNREAD by runtime — see flee-gap note in §9)
│   │   │   └── SpeciesDataGPU.cs                 Per-species SoT: Role, ScientificName, School/Movement/MotionRender/Behavior props, PathStyle, prey/predator lists, FishPerSchool, MaxSchools, UseSpineDeformation
│   │   ├── GPU_Spatial_Partition/
│   │   │   └── SpatialPartitionGPU.cs            GPU spatial grid compute shader wrapper
│   │   └── Spatial_Partition_Instanced_Rendering/
│   │       └── BoidSimulationGPU.cs              Active GPU simulation + ReinitializeBuffers() + moray trail buffer + reef backstop turn tunable
│   ├── Boids_CPU/                                Only two files remain — used by GPU base classes
│   │   ├── BoidInformation.cs                    Per-boid movement state struct (used by FishSwimmingUtility)
│   │   └── BoidSpawnData.cs                      Spawn config struct (used by BoidSpawnerBase)
│   ├── Automatic_Fish_Swimming_CPU/              ⚠ Legacy single-fish CPU demo — no C# refs, BUT GameObjects using these still sit in several scenes (incl. Boids_Demo, MainScene, SCENE_MainScene 1). Disabled leftovers — LOOK before deleting (script-GUID grep, not just C# grep)
│   │   ├── AutomaticFishSwimSimulation.cs
│   │   └── AutomaticFishSwimming.cs
│   ├── Networking/
│   │   ├── BubbleSelectHook.cs                   Per-bubble tap → selects species in TabletAddRemoveUIGPU (no SpeciesBubble edits)
│   │   ├── BubbleTransition.cs                   Procedural bubble-wipe overlay used by ExhibitReset (big-screen only)
│   │   ├── ConnectionScreenUI.cs                 Client IP input + connect button
│   │   ├── EcosystemNetworkManagerGPU.cs         Syncs school counts + max-schools + eco-health + species status via NetworkList/NetworkVariable, add/remove/reset RPCs
│   │   ├── ExhibitReset.cs                       Operator F9 trigger + host wipe sequence + DoLocalReset on both screens
│   │   ├── HostSpawner.cs                        ⚠ DEAD — zero refs anywhere (verified). Safe to delete
│   │   ├── LanDiscovery.cs                       UDP broadcast — tablet auto-finds host on WiFi
│   │   ├── NetworkBootstrap.cs                   Host/Client role setup, starts NGO, spawns net-manager
│   │   ├── OptimisticPopulationStore.cs          Client-side optimistic count overlay — pending taps over synced counts so the tablet number updates instantly (per-species-index; fixes remove-lag / snap-back)
│   │   ├── TabletAddRemoveUIGPU.cs               Singleton Add/Remove controller — fires RPCs, greys at cap/0, reads display counts via OptimisticPopulationStore, first-add reveal lockout (normal cooldown removed 2026-07-29), locked-species blocking
│   │   └── TabletEcosystemUIGPU.cs               Pure species→index lookup service
│   ├── Shader_GUI/Editor/                        Custom material inspectors for the Fish_Lit shaders (namespace: OceanX, formerly GameDevBuddies)
│   │   ├── FishLitBaseShaderGUI.cs / FishLitDetailGUI.cs / FishLitShaderGUI.cs
│   │   ├── FishSwimmingGUI.cs / MaterialAccess.cs / Property.cs / ShaderUtils.cs
│   ├── Content/
│   │   ├── ContentService.cs                     Downloads CSVs at launch → caches to persistentDataPath → falls back to StreamingAssets → hardcoded. Follows Google's 307 redirect (redirectLimit=32)
│   │   ├── CsvUtil.cs                            One robust RFC-4180 parser (quotes, commas, embedded newlines) shared by both loaders. Quote-only-at-field-start bugfix
│   │   └── AluciaEcologyEvents.cs                Polls sim, detects species starving/overpredated/overpopulated/extinct/added → speaks matching CSV line via AluciaController.Say
│   ├── Environment/
│   │   └── EnvironmentHealthReveal.cs            Reads EcosystemSimulationGPU.EcoHealth01 → drives per-item MaterialPropertyBlock/scale — corals grow in/retract with health
│   ├── Other/
│   │   ├── BoundsComparer.cs
│   │   ├── ComputeShaderExtensions.cs
│   │   ├── TransformAnimator.cs                  Animates target transforms along Line / Circle / Rectangle paths
│   │   ├── TransformAnimatorSpeedCorrection.cs
│   │   └── TransformFollow.cs
│   └── Shared/
│       ├── BoidSimulationBase.cs
│       ├── BoidSpawnerBase.cs                    SchoolCount/IsActive + SetSchoolConfiguration
│       ├── BoidSpawnUtility.cs
│       ├── FishMotionRenderProperties.cs
│       ├── FishMovementProperties.cs
│       ├── FishSchoolProperties.cs
│       ├── FishSwimmingMaterialUpdate.cs         Drives shader animation from speed
│       ├── FishSwimmingUtility.cs
│       ├── GlobalAffectersInjector.cs
│       ├── GroupOfBoidsSpawnData.cs
│       ├── SimulationAffecter.cs
│       └── SimulationAffecterComponent.cs
├── Shaders/
│   ├── Compute/                                  Brute-force + spatial-partition + grid compute shaders (incl. reef-SDF backstop, capped-turn helpers)
│   └── Fish/                                     Fish_Lit + Fish_Lit_Instanced shaders + moray-specific spine shader hooks
├── Data/
│   ├── Fish/                                     12 folders (canonical species) — each with SpeciesDataGPU + FishSchool/Movement/MotionRender/Behavior props
│   └── Models/                                   Real fish meshes (imported 2026-07-07 onwards)
├── Scenes/                                       SCENE_MainScene (canonical host), Boids_Demo (older host), Netcode Simulation Test (older tablet client), Swirl_Demo
├── Prefabs/ · Settings/ · Visual/                Prefabs, URP/build settings, materials/meshes/textures

Assets/Aloysius/                                  UI team
└── Scripts/
    Core UI:        SpeciesBubble.cs · ModalController.cs · SpeciesInfoPanel.cs · TabController.cs · SwipeToClose.cs · DimFader.cs
    Food web:       FoodWebLines.cs · CurrentOrganismsGrid.cs · OrganismCardData.cs
    Health:         Health.cs (client/netcode bar) · HealthBarBinder.cs (host/large-screen bar, reads EcosystemSimulationGPU.EcoHealth01 direct) · EcoHealthDashboard.cs
    Unlock:         GameState.cs + UnlockTester.cs (Aloysius placeholders) · HintsPanel.cs (live-requirement hints) · SpeciesUnlockReveal.cs · NotificationManager.cs (used by JunHeng's EcosystemUnlockManagerGPU)
    Reveal:         SpeciesAddedReveal.cs · RevealQueue.cs
    NPC/FX:         AluciaController.cs · AluciaLines.cs · GodRays.cs · MarineSnow.cs · SonarPulse.cs · Bob.cs · TextGlowPulse.cs (play-mode TMP glow on material instance) · Bubbles.cs (marine-snow UI particles behind the bubbles)
    ⚠ Deleted 2026-08-11 (`9768838d`, "Delete old scripts", 0 refs): EcoHealthChassis.cs · FoodWebDragReveal.cs · LockedHintPanel.cs
    Onboarding:     TutorialPanel.cs (now exposes IsOpenOrPending) · ContextNudge.cs · HideUntilStarted.cs · StartCrossfade.cs · ExperienceStartGate.cs
    Guidance/feedback: BalanceAdvisor.cs (bottom-left "HOW TO BALANCE" hint panel) · LookUpPrompt.cs ("look at the big screen" toast on Add) · ButtonCooldownOverlay.cs (radial Add-button cooldown sweep)   [all added 2026-07-26/27]
    Title/Win:      FishSwim.cs (title screen) · WinCondition.cs (health ≥0.99 held 2s) · WinScreen.cs (large screen) · TabletWinScreen.cs (tablet debrief + restart) · SplashSequence.cs
    Audio:          UISoundManager.cs · AdaptiveMusicSystem.cs · Editor/AdaptiveMusicSetup.cs (replaced deleted MusicDirector.cs)
    Data link:      SpeciesData.cs (UI asset — carries the gpuSpecies → SpeciesDataGPU link; unlock config lives here)
    Content DBs:    SpeciesContentDB.cs · RevealContentDB.cs · ViewedSpeciesReporter.cs

Assets/Akil/                                      Scene environment / 3D art
└── Assets/                                       Fish FBXs (shark, ray, damselfish, parrotfish, surgeonfish, spinefoot/rabbitfish, snapper, moray) + textures + materials
└── Scenes/                                       SCENE_MainScene 2.unity (Akil's environment) + SCENE_MainSceneBackup.unity
```

**Total scripts (Junheng):** all 58 read + verified 2026-07-08.

---

# 6. Scene Architecture

## Canonical (production) scenes

| Scene | Owner | Role | Path |
|-------|-------|------|------|
| `SCENE_MainScene.unity` | JunHeng | **Host — GPU simulation + trifold display** (main production scene) | `Assets/Junheng/Scenes/SCENE_MainScene.unity` |
| `new netcode 2.unity` | Aloysius | **Client — tablet UI scene (ACTIVE, in build)** | `Assets/Aloysius/Scenes/new netcode 2.unity` |

## Build Settings (as of 2026-07-28)

**One project ships two players by toggling which scene is enabled at index 1.** Index 0 is always `Assets/Aloysius/Scenes/Start scene.unity` (splash → auto-advances to buildIndex 1 via `SplashSequence`):

- **Host build (big-screen, Windows)** → enable `Assets/Junheng/Scenes/SCENE_MainScene.unity`.
- **Tablet build (client, Android)** → enable `Assets/Aloysius/Scenes/new netcode 2.unity`.

Only ONE of those two is enabled at a time. The `fdcfc4c3` **"android to window"** commit is exactly this flip. ⚠ **Flip the enabled scene AND the Player platform target together.** Working tree at last check = **tablet build** (`new netcode 2` enabled, Android). The `74deb98` `SCENE_MainScene 1.unity` reference is gone — that scene was renamed/removed; the earlier "still points at Aloysius's copy" warning no longer applies.

All other scenes (Boids_Demo, both `new netcode 1`, Akil's + Aloysius's `SCENE_MainScene`/`SCENE_MainScene 2`) are present but disabled.

## Prototyping / deprecated / archived scenes

| Scene | Owner | Status | Notes |
|-------|-------|--------|-------|
| `Assets/Junheng/Scenes/Boids_Demo.unity` | JunHeng | Deprecated | Old host scene; superseded by SCENE_MainScene |
| `Assets/Aloysius/Scenes/SCENE_MainScene 2.unity` | Aloysius | Prototyping | UI-team scene; **used for demo tuning** (JunHeng copied the converged main scene here for intro-camera/entry-point/card-hold tuning) |
| `Assets/Akil/Scenes/SCENE_MainScene 2.unity` | Akil | Environment / art | Renamed from `SCENE_MainScene 1.unity` 2026-07-24; older `SCENE_MainScene.unity` deleted (280k lines wiped) |
| `Assets/Akil/Scenes/SCENE_MainSceneBackup.unity` | Akil | Backup | — |
| `Assets/Aloysius/Scenes/new netcode 2.unity` | Aloysius | **ACTIVE tablet client** | The tablet UI scene enabled in Build Settings (index 1, Android build) |
| `Assets/Aloysius/Scenes/new netcode 3.unity` | Aloysius | Prototyping | Newer client copy added 2026-07-26; not in Build Settings — `new netcode 2` is the live one |
| `Assets/Junheng/Scenes/ALOYLOU VEFR @.unity` | JunHeng | WIP (local) | Newer tablet-client scene, 14 species bubbles — not committed to git at last check |
| `Assets/Junheng/Scenes/Aloysius lololol.unity` | JunHeng | WIP (local) | 1-flag fork of Netcode Simulation Test with ConnectionScreen disabled |
| `Assets/Junheng/Scenes/Swirl_Demo.unity` | JunHeng | Deprecated | Old swirl demo |
| `Assets/Aloysius/Netcode Simulation Test 1.unity` | Aloysius | Prototyping | UI-only prototyping scene (superseded) |
| `Assets/_Recovery/` | — | ⚠ **DELETE** | 9.9 MB of Unity crash-recovery dumps tracked in git; live script GUIDs pollute reference greps. `git rm -r --cached`, delete, add to `.gitignore` |

## Scene divergence history (context)

Historically had THREE `Boids_Demo` / `SCENE_MainScene` copies (JunHeng had the sim, Aloysius forked with health bar, Akil owned environment/lighting). Partial convergence happened as the health-bar (`HealthBarBinder`) and environment moved into JunHeng's `SCENE_MainScene`. **Full convergence into ONE host scene is still pending before final build** — see To Do §11.

---

# 7. What Has Been Implemented

## 7.1 GPU Ecosystem Simulation (core)

**Active runtime:** `EcosystemSimulationGPU` runs in Awake **before** `BoidSimulationGPU.Start`. Tick coroutine every `_tickInterval` seconds (default 5).

- **`SpeciesDataGPU`** — one asset per species holds all simulation SOs (FishSchoolProperties, FishMovementProperties, FishMotionRenderProperties, SpeciesBehaviorPropertiesGPU) plus per-species tuning (`FishPerSchool`, `MaxSchools`, `PreySpecies`, `PredatorSpecies`, `UseSpineDeformation`).
- **`EcosystemDefinitionGPU`** — top-level asset: species list + simulation bounds. The 12 canonical species are wired in **fixed order**, each with a matching `BoidSpawnerGPUMultiTargets` in `BoidSimulationGPU._gpuBoidSpawners`. ⚠ Child spawner objects are **not** auto-scanned; a species in the definition list but missing from this array throws *"no spawner has SpeciesData 'X' assigned"* and won't spawn.
- **`BoidSpawnerGPUMultiTargets`** reads all spawn properties from `SpeciesDataGPU`.
- **`BoidSpawnerBase`** — `SchoolCount` / `IsActive` properties + `SetSchoolConfiguration(schoolCount, fishPerSchool)`.
- **Simulation bounds derived** — `EcosystemSimulationGPU.SimulationBounds` reads from `BoidSimulationGPU.SimulationAreaBounds`. `EcosystemDefinitionGPU` SimulationCenter/SimulationSize are only a fallback; no more manual sync of two separate bounds assets; wandering affecter targets spawn and roam inside it. **`_simulation` now auto-wires** to a sibling `BoidSimulationGPU` via editor `Reset()` / `OnValidate()` (2026-07-26, `7f680929`) so the reference is never silently unassigned. The old `OnDrawGizmosSelected` bounds-box gizmo was **removed** in the same change (was drifting/duplicating BoidSimulationGPU's own gizmo).

## 7.2 Population Dynamics + Eco-Health (ratio-driven, global)

> ### ⚠ AS SHIPPED, THE AUTOMATIC TICK IS OFF (verified 2026-08-29)
> `_enablePopulationDynamics` is **`0` in all three production scenes** (`Host.unity`, `Trifold.unity`, `SCENE_MainScene.unity`) — the C# default is `true`, so the scenes override it. **Populations therefore only ever change from manual Add/Remove.** Nothing grows, shrinks or goes extinct on its own.
>
> Everything described in the rest of this section is implemented and correct, but **dormant** unless someone re-ticks that box. Read it as "how the model works if switched on", not "what the visitor sees".
>
> Two things that are easy to get wrong here:
> - **Eco-health still works.** `EcoHealth01` is a computed property (`ComputeEcoHealth01()`, evaluated on demand), not something the tick writes. It reads live school counts, so the health bar responds to manual Add/Remove exactly as before.
> - **Shoaling still runs.** `RunShoalingTick()` is called from `PopulationTickRoutine` *outside* the `_enablePopulationDynamics` gate, on purpose — merging reshuffles which schools swim together but never changes how many fish exist, so it is not the population master switch's business. The tick coroutine is therefore still running even when dynamics are off.

**⚠ Natural births/deaths (Week 8) AND per-species starvation fields (Week 9) are gone.** `ReproductionRate` / `NaturalDeathRate` / `StarvationDeathRate` / `StarvationThreshold` were all deleted from `SpeciesDataGPU`. Balance is now **global**; per-species behaviour comes from `FishPerSchool` / `MaxSchools` / prey/predator lists.

**Symmetric ratio-driven predator/prey dynamics:**
- Each species feels a prey:predator balance ratio (in **school counts**) against a shared dead-band `[RatioBandLow, RatioBandHigh]` (default **1–3**):
  - Few predators (ratio high) → prey grows; many predators (ratio low) → prey shrinks.
  - Prey abundant → predator grows (well-fed); prey scarce/gone → predator **starves** (hard override — can't grow without food).
  - Inside the band → stable.
- Rolled at `GrowRate` / `ShrinkRate` (default 0.3/tick).
- Counts snapshotted each tick so updates are order-independent.
- Can drive a species to extinction (0 schools).

**`PopulationPressure(species, counts)` → +1 / 0 / -1** — combines predation pressure (top-down) and food availability (bottom-up). **Starvation is a hard override** — no/low food always shrinks, so the keystone-collapse cascade can't be cancelled by "no predators."

**Eco-health (`EcoHealth01`, 0–1)** — diversity + balance + apex presence, weighted (0.4 / 0.4 / 0.2 default).
- **Diversity** — fraction of species alive out of the whole roster (see fix note below).
- **Balance** — fraction of present species that are **not declining** (`PopulationPressure >= 0`, i.e. stable or growing) AND not overpopulated. Well-fed apex filling toward its cap is fine; only actively starving / over-hunted / running-away species drag health down.
- **Apex presence** — extra credit when the shark is in the reef.

**Two key fixes to make 100% reachable:**
1. **Old behaviour required perfect stillness** (`PopulationPressure == 0`) — impossible for every species at once with the dense food web + `MaxSchools` caps → health topped out at ~87%. Now "not declining and not overpopulated" counts as healthy → **100% reachable.**
2. **Old balance divided by present species only** (`considered`), so the first fish added read as full balance → jumpy score. Now divides by `totalSpecies` (whole roster) → single fish contributes ~1/N; climbs smoothly as player builds the reef. **100% still requires every species alive AND healthy** — same ceiling, gentler ramp.

**Overpopulation model (ratio + count based, replaces old "predators gone + near cap"):**
- Shared helper **`IsOverpopulated(species, counts)`** used by BOTH `GetSpeciesStatus` and the eco-health balance term (bar + Alucia warnings always agree).
- **Predators present** → overpopulated if species outnumbers combined predators by > `_overpopulatedRatio` (default **7:1**).
- **Predators entirely gone** → overpopulated if > `_overpopulatedFreeCount` schools (default **3**).
- Inspector: `_overpopulatedRatio = 7`, `_overpopulatedFreeCount = 3` (replaced old `_overpopulatedAtFraction`).
- Apex species (no natural predators) are never overpopulated.

**Grazer MaxSchools raised for overpopulation headroom:** Parrotfish 6→10, Surgeonfish 6→10, Mullet 7→10, Damselfish 7→12, Spinefoot 6→10. Mullet kept effectively at 10 (18 fish/school). Predator/mid-fish caps unchanged.

**Cause-aware species status for Alucia (replaces `GetBalance`/`SpeciesBalance`):**
- `enum SpeciesStatus { Absent, Balanced, Starving, OverPredated, Overpopulated }`. Cause-aware, computed on committed counts, **messaging-only — does NOT drive the population tick** (that still uses `PopulationPressure`).
  - **Starving** — it eats prey, food *was* present at least once, and is now gone/below the low band (real collapse).
  - **OverPredated** — its predators are actually present and outnumber it past the low band.
  - **Overpopulated** — normally has predators, they're absent, and it has grown to overpopulated threshold.
  - **Balanced** — none of the above, including a just-added species or a lone predator whose prey was never introduced (no false alarm).
- Per-species `_foodWasPresent` memory distinguishes a genuine "prey ran out" from "prey were never added yet".

**⚠ `GetSpeciesStatus` and the eco-health formula do NOT use the same test — reconciled at the tablet layer (2026-07-29, `cc0cabe8`).** `GetSpeciesStatus` flags **OverPredated** on a pure predator:prey school-**ratio** (`n / predN < RatioBandLow`), but `ComputeEcoHealth01` counts a species as healthy on **`PopulationPressure` + overpopulation**. A well-fed species that is merely *outnumbered* by predators is therefore **healthy for the bar but OverPredated for the status** — which is why Russell's snapper's row asked for **+2** while the gauge read **100% THRIVING**.
- **Fix — one authority:** new **`EcosystemSimulationGPU.CountsTowardHealth(species[, committed])`** is literally the health formula's own test (`!declining && !overpopulated`, present + food-web-linked). **`GetSpeciesDelta` now returns `DeltaOk` the moment `CountsTowardHealth` is true**, regardless of the status enum — if the bar counts a species as healthy, its row is silent.
- **One value, no mixing:** the delta is sent as a single int with sentinel constants **`DeltaOk` / `DeltaNeedsPrey` / `DeltaCapped`** (public consts on `EcosystemSimulationGPU`). The tablet card switches on the number alone and never re-reads `GetSpeciesStatus`, so it can't re-introduce the disagreement.
- ⚠ **Known gaps (accepted for now, play-tested OK):** (1) `EcoHealthDashboard` still **rounds ≥99.5% → 100%**, so a genuinely-declining species can still show a number under a *displayed* "100%"; (2) the delta still guides "add more of yourself" for a real OverPredated case rather than pointing at the actual lever (more prey / fewer predators) — the signs are honest but not yet a full guide to a real 100%.

## 7.3 Start-at-Zero / School-Scaling / Extinction Model (`e13e26b`)

The GPU ecosystem starts from a completely empty ocean; the player builds it up.

- **`SpeciesDataGPU.FishPerSchool`** (int) — number of boids per school (constant density scaling).
- **`SpeciesDataGPU.MaxSchools`** (int) — static per-species cap; synced to clients via netcode.
- **`EcosystemSimulationGPU`** owns `N` (school count) per species, initialised to 0.
  - `AddSpecies` — increments N up to `MaxSchools`, calls `ReinitializeBuffers`.
  - `RemoveSpecies` — decrements N down to 0 (extinction), calls `ReinitializeBuffers`.
  - Population tick can now remove the last school (species goes fully extinct).
  - **Public API for UI/RPCs:** `AddSpecies` / `RemoveSpecies` / `CountGroups(species)` (returns current school count for UI display).
- **Inactive spawners** (school count = 0) are excluded from the concat buffer, spatial grid, affecter targets, and rendering — no placeholder draw calls.

**`ReinitializeBuffers()` sequence:**
1. Read live GPU positions back to CPU (from `_boidsComputeBuffer`, the original-order state buffer; skipped when empty).
2. Slice per active spawner using `spawner.Boids.Length` (old count, not new).
3. Call `spawner.StorePreservedBoids(slice)` on each spawner.
4. Tear down all GPU buffers (derived → base → spatial partition → spawners).
5. Re-run full init chain — `SpawnBoids` restores old positions, only new fish get fresh spawn positions.

**Empty-ocean / last-extinction is crash-safe:**
- All GPU compute buffers and spatial grid sized `Mathf.Max(1, count)` — zero never passed to `new ComputeBuffer`.
- `SetData` / `GetData` guarded against empty arrays.
- Per-frame dispatch and render skipped when `_boidsCount == 0`.
- Group IDs assigned densely over active spawners only.
- Null-guard in `UpdateSimulation` (`1887612`) — early-returns if called before GPU buffers are fully initialised (first-frame race).

**Bug fix:** NaN spawn positions for single-fish schools caused by divide-by-zero in spawn grouping — fixed with a guard.

**Carrying capacity** = `MaxSchools × FishPerSchool`. Dead `_carryingCapacity` dict removed from `EcosystemSimulationGPU`.

## 7.4 Unlock System (`EcosystemUnlockManagerGPU`)

Eco-health-gated species unlock system (singleton). Ports the prototype's gate model into Unity — a locked species unlocks when **all** its `requires` (prey/support **school counts**) are met **AND** live eco-health % ≥ its `minHealth`. Latching / one-way, like the prototype.

- **Dual data source:** reads population + eco-health straight from `EcosystemSimulationGPU` when present (host / standalone — testable with `EcosystemDebugHarnessGPU`, no netcode needed), and falls back to the synced netcode layer (`EcosystemNetworkManagerGPU.GetPopulation` / `GetEcoHealth`) on the tablet client (where `_simulation` is empty).
- **Drop-in replacement for Aloysius's placeholder `GameState`:** exposes `IsUnlocked`, `RegisterLockedTap` (progressive hints), `RefreshAllBubbles`; fires `NotificationManager.ShowUnlocked` on each unlock; plus `OnSpeciesUnlocked` / `OnUnlockStateChanged` events and `GetLockInfo()` for the locked-modal requirement checklist.
- **New overload `IsUnlocked(SpeciesDataGPU gpu)`** — resolves the gpu-species to its `SpeciesData`; a species the manager doesn't track is treated as unlocked, so it never blocks something the unlock system isn't meant to govern.
- **`ResetToStart()`** — re-locks all species to the initial "5 starters unlocked" state; resets hint counters. Called by `ExhibitReset.DoLocalReset`.
- **Locked species can no longer be added from the tablet** — `TabletAddRemoveUIGPU` remembers the selected species; Add is blocked and the Add button greys out while that species is still locked (not yet discovered). Remove and adds of unlocked species are unchanged.
- **Progressive locked hints** — `SpeciesBubble.ShowLockedHint` pulls `AluciaLines.GetVariants("hint.flavour", speciesName)` (ordered vague → clearer → almost there, matching the old `hint1/2/3`), falling back to the `SpeciesData` asset when the sheet has no rows for that fish. One source of truth with the host + Hints tab.
- **Optional `[Unlock]` console logging** for headless testing.

⚠ **`minHealth` thresholds need tuning** against the real eco-health curve — the formula can sit low early (diversity term), so a threshold like 70 can be effectively unreachable. Set them against what the bar actually reaches in play.

## 7.5 Reef-SDF Obstacle Avoidance

**`ReefSDFVolume` + `ReefSDFBaker`** — SDF-based obstacle avoidance for the reef mesh.
- **`_padding` field** (default **4 m**) on `ReefSDFVolume`; `Bounds = _size + 2·padding`. **Not optional slack** — the escape direction is a central-difference gradient sampling one voxel either side, so a fish within a voxel of the un-padded edge would difference against a data cliff and get a garbage direction. Padding pushes the edge beyond where fish can swim and catches reef straddling the boundary. `ReefSDFBaker` bakes the padded volume.

**Reef backstop "snap" fix (`BoidsGPU_Spatial_Partition.compute`):**
- **Symptom:** fish avoiding a rock would *instantly* flip their rotation — worst dodging obstacles directly above/below (snap up/down); side-to-side too but rarer.
- **Root cause:** the hard penetration backstop (`ResolveReefPenetration` + box-affecter fallback `ResolveObstaclePenetration`) rewrote `boidInfo.direction` in a **single frame**, bypassing the normal angular turn ramp. Vertical-dominant because flat rock tops / seabed give a vertical surface normal, and the "buried in a slab" escape hardcodes a +Y climb.
- **Fix:** keep the instant **position** push (prevents tunnelling), but turn the heading toward the corrected direction at a **capped rate**. New helpers `RotateDirectionTowards(from, to, maxRadians)` (capped slerp) + `ReefBackstopMaxTurn(schoolInfo)` (= species `maxAngularVelocity × multiplier × dt`). Applied to all four direction rewrites across both resolvers.
- **Live tunable:** `_reefBackstopTurnMultiplier` on `BoidSimulationGPU` (Inspector: "Reef Backstop Turn Multiplier", default **3**), pushed per-frame like `_TailSwayResponsiveness`. Lower = smoother; higher = snappier. Usable range ~2–4.

**Big-fish obstacle avoidance (stops body clipping + on-the-spot spin):**
- **Symptom:** big fish (grouper especially) had part of their body poking into coral/rock, sometimes appearing to rotate on the spot.
- **Cause:** reef collision is a **point** test on the fish's pivot — keeps pivot one voxel (0.5m) clear, but a big fish's mesh extends past that. A fish pinned against the reef (position held, forward motion cancelled) looks like it's spinning in place.
- **Quick fix (no code):** raised `ObstacleAvoidanceRange` so big fish bank away *earlier* (soft steer), before the hard backstop ever fires:
  - **Grouper:** 2.1 → 6 → **8** (bumped again after Akil's environment changes)
  - **Giant moray:** 0.75 → **5.5**
  - Bluefin trevally left at 4.5, shark 10.5, ray 5.7 unchanged.
- ⚠ **Moray value untested** at time of first bump (model wasn't in scene yet; now imported — re-verify).
- **Proper fix deferred:** per-species **collision radius** (used for both backstop clearance and avoidance margin) so big bodies are physically kept clear — real solution to point-collision clipping, especially the long-bodied moray.

## 7.6 Fish Rendering — Shaders + Species-Specific Deformations

**Standard fish shaders:**
- **`OceanX/Fish_Lit`** — non-instanced, for solitary/hero fish rendered as regular GameObjects (e.g. individual sharks placed in scene).
- **`OceanX/Fish_Lit_Instanced`** — GPU-instanced, for schooled fish rendered via `Graphics.RenderMeshIndirect` from `BoidSpawnerGPU.RenderBoids`. Reads per-boid position/rotation/state from a `StructuredBuffer<Boid> _Boids` SRV. See §9 for the "wrong path" crash.
- **`Fish_Swimming_Motion.hlsl`** shared library — procedural swim animation. Reads a per-vertex tail-mask from **TEXCOORD1**: `tailMask = saturate(pow(1.0 - tailMaskUV.x, _TailMaskFalloff))`. See §7.20 (Fish Asset Pipeline) for the UV1 baking rules.

**Custom material inspector — `Shader_GUI/Editor/`:**
- Custom UI for Fish_Lit materials (surface options, surface inputs, detail inputs, fish swimming properties, advanced options).
- Namespace originally `GameDevBuddies` (forked from an asset), **renamed to `OceanX`** 2026-07-26 to match project namespace and stop the *"Could not create a custom UI for shader 'OceanX/Fish_Lit'"* warning.
- Includes `FishLitBaseShaderGUI` / `FishLitDetailGUI` / `FishLitShaderGUI` / `FishSwimmingGUI` / `MaterialAccess` / `Property` / `ShaderUtils`.

**Ray tail-sway (signed turn-rate):**
- **`BoidInfoGPU` struct grew to 18 floats** (`Size = sizeof(float) * 18`) — new **`SignedTurnRate`** field (~[-1,1]: sign = bank/yaw direction, magnitude = turn hardness), written by the compute shader each frame. Carries the sign the sim otherwise discards (`AngularVelocity` is stored unsigned).
- **Consumed only by `OceanX/Ray_Wing_Lit_Instanced`** (Akil's ray-specific shader) to sweep the tail toward the turn — behaviourally inert for every other boid (the fish shader ignores it).
- **`BoidSimulationGPU` serialized `_tailSwayResponsiveness`** (default 4, frame-rate-independent ease), pushed to compute as `_TailSwayResponsiveness` — tune ray tail floatiness live.

**Moray serpentine spine deformation (2026-07-24, `d36ee95`):**
- **`SpeciesDataGPU.UseSpineDeformation`** (bool, in a "Rendering" header). **ONLY moray should tick this.** When ON, `BoidSimulationGPU` maintains a **per-instance head-path trail buffer** for the species, and the spawner's material MUST use `OceanX/Moray_Lit_Instanced` (a moray-only shader).
- ⚠ **Only ONE species may enable this** — buffers are sized for it.
- ⚠ **`OceanX/Moray_Lit_Instanced` shader not yet in the repo** (presumably still on Akil's side).
- **`BoidSpawnerGPU.SetSpineRenderData(...)`** — 13-arg API called by `BoidSimulationGPU` every frame for the moray spawner. Feeds the material's `MaterialPropertyBlock` a set of `_Moray*` props: `_MorayTrail` (SRV to head-path buffer), `_MorayTrailCursor` (SRV to per-instance cursor), plus `_MorayTrailCount`, `_MorayTrailSpacing`, `_MorayHeadLocalZ`, `_MorayBodyLength`, `_MorayUndulation{Amplitude,Waves,Speed,HeadHold}`, `_MorayDebugStraight`, `_MorayFlipNormals`, `_MoraySmoothingWindow`. Vertex shader uses these to lay each eel's body along its recorded head-path (proper eel-like undulation, not a rigid mesh).
- All values re-bound each frame → live-tunable in Play mode.
- Untouched (and unbound) for every other spawner — no impact on the standard rigid fish path.
- **Akil owns the moray tuning** — he authored the mesh + shader and drives the `_moraySpine*` values on `BoidSimulationGPU`. `SCENE_MainScene.unity` mirrors his `SCENE_MainScene 2.unity` values; sync from his scene when he changes them.

**Environment coral health reveal shader (Akil, 2026-07-16 → 07-18):**
- **`OceanX/CoralHealth`** — corals stay full-size and regain colour from a **bleached/dead** look as eco-health rises. `EnvironmentHealthReveal` drives `_Health` 0→1 via per-item `MaterialPropertyBlock` (cached so it doesn't clobber other overrides). `recoverStagger` spreads recovery across a group.
- New `ColorRecover` reveal style added alongside the placeholder `ScalePopIn`. Use `ColorRecover` for "all corals present but washed-out" + the dead-coral half of a hybrid scene; keep seagrass / "new" corals on `ScalePopIn`.

## 7.7 Flocking + Species Behavior Tuning (biology-driven)

Fish flocking + numbers were **re-tuned from the research doc** (Akil Hussain, `OceanX MP Research Document.docx`, per-species roaming + social behaviour). Supersedes an earlier 5-archetype grouping. **Fixes the "solitary fish still flock" bug** (previously several solitary/small-group species carried schooling values).

**Key lever:** cohesion + alignment only bind fish **within the same school**, so `FishPerSchool = 1` makes a species physically unable to flock (multiple individuals then only *repel* via separation → realistic for solitary/territorial fish).

**Behaviour tiers:**
- **Solitary — never school** (`FishPerSchool 1`, cohesion/align 0.05–0.15, wide separation): Blacktip shark, Brown-marbled grouper, Giant moray, Bluespotted ray, **Bullethead parrotfish**.
- **Pairs / small loose groups** (`FishPerSchool 2–3`, cohesion ~0.25–0.3): Bluefin trevally, Eyestripe surgeonfish.
- **Loose aggregation** (`FishPerSchool 7`, cohesion 0.4): Russell's snapper.
- **Tight / large schools** (high `FishPerSchool` + high cohesion/align, tight separation): Yellowstripe scad (25), Fringelip mullet (18), Streaked spinefoot (9), Reticulated damselfish (8).

**Final values** (`ObstacleAvoidanceRange 3` and `SeparationWeight 1` were the defaults; grouper/moray now different — see §7.5):

| Species | Vision | SepRange | Cohesion | Align | Target | FishPerSchool | MaxSchools |
|---|---|---|---|---|---|---|---|
| Blacktip reef shark | 22 | 3 | 0.15 | 0.15 | 0.7 | 1 | 3 |
| Brown-marbled grouper | 8 | 3 | 0.05 | 0.05 | 1.4 | 1 | 4 |
| Giant moray | 8 | 3 | 0.05 | 0.05 | 1.5 | 1 | 4 |
| Bluefin trevally | 18 | 1.5 | 0.25 | 0.3 | 0.85 | 2 | 5 |
| Russell's snapper | 12 | 1 | 0.4 | 0.3 | 0.85 | 7 | 5 |
| Yellowstripe scad | 14 | 0.3 | 1.3 | 1.5 | 0.6 | 25 | 6 |
| Bluespotted ribbontail ray | 8 | 2.5 | 0.1 | 0.1 | 1.2 | 1 | 5 |
| Fringelip mullet | 12 | 0.5 | 0.9 | 1 | 0.8 | 18 | 7 |
| Bullethead parrotfish | 12 | 3 | 0.1 | 0.1 | 1.2 | 1 | 6 |
| Streaked spinefoot | 10 | 0.5 | 0.75 | 0.8 | 0.85 | 9 | 6 |
| Eyestripe surgeonfish | 12 | 1.3 | 0.3 | 0.35 | 0.9 | 3 | 6 |
| Reticulated damselfish | 8 | 0.35 | 1 | 0.7 | 1.2 | 8 | 7 |

**⚠ Bullethead parrotfish = the MODELLED SEX drives behaviour.** Doc: "males highly solitary, females move in small groups." Our texture/model is the **terminal-phase male** (vivid blue-green), so it's set **solitary** (`FishPerSchool 1`, cohesion 0.1, wide separation, territorial). If a **female/initial-phase** parrotfish model is added later, give *that* asset the small-group values (`FishPerSchool ~3`, cohesion ~0.3) — don't overwrite the male's.

**`MaxSchools`** follows a trophic pyramid (apex 3 → tertiary 4 → secondary 5–6 → primary 6–7) so "remove the shark → prey overpopulate" reads more clearly. Later raised primary caps for overpopulation headroom — see §7.2.

**Speed untouched** — `FishMovementProperties` (CruisingSpeed/MaxSpeed) was NOT changed this pass; the doc has size/speed hints (moray "stealth not speed", scad "rapid directional changes") if a per-species speed pass is wanted next.

**`SpeciesBehaviorPropertiesGPU` (flee/hunt/hunger) is currently INERT** — see §9 (Things Tried).

**Flocking model confirmed** (`BoidsGPU_Spatial_Partition.compute`): a boid's `BoidID` packs group ID (species, bits 0–7) + sub-group ID (school, bits 8–15). **Cohesion + alignment apply only within the same species AND the same school** — different species (and even different schools of one species) never merge. **Separation** applies to same-species + **larger** species (smaller species ignored; species size-ranked by group ID). Copying one species' school settings to all is safe — doesn't blend species.

## 7.8 Introduction Camera (Cinemachine, host-only)

`IntroductionCameraDirectorGPU.cs` — cinematic shot that catches a species' **first** school at its off-screen entry gate and follows the real fish as they swim in, then releases so the `CinemachineBrain` blends back to the overview camera.

**Driven by two hooks on `EcosystemSimulationGPU`:**
- **`OnSpeciesFirstIntroduced`** (`event Action<SpeciesDataGPU>`) — fires inside `AddSchool` on a species' **debut only**: 0→1 **and** never having debuted before this session. The "never before" half is a latching `HashSet<SpeciesDataGPU> _everIntroduced`, tested and recorded in one call (`(n == 0) && _everIntroduced.Add(species)`), cleared only by `ResetToEmpty` so the next visitor gets every intro again. Not fired for subsequent adds.
  - **⚠ Corrected 2026-08-29.** This bullet used to read "*only on the 0→1 transition … not fired for subsequent adds or automatic population-tick growth*". Both halves were wrong. **0→1 alone is not a debut** — a species driven to extinction and added back hits 0→1 again, and replayed the whole reveal card + camera fly-in as though the visitor had never seen it (the reported bug, fixed in `23c2751c`). And the tick claim was never true at all: auto-growth calls the same `AddSchool`, so it fired the intro too. In practice that never surfaced because the population tick is switched off in every production scene (see §7.2) — but the guard now covers it either way, so re-enabling dynamics cannot resurrect the bug.
- **`TryGetSchoolCentroid(species, schoolIndex, out Vector3)`** — public wrapper over the private `TrySchoolCentroid`; synchronous GPU readback of one school's live centre-of-mass. ⚠ Throttle callers — do **not** poll every frame.

**Smooth follow:** readback is throttled + the centroid jitters, so the director `SmoothDamp`s a proxy transform toward the latest read every frame; the intro camera Follows/LookAts the proxy (continuous motion regardless of readback rate).

**`FramingMode` enum** — `FollowBehind` / `SideView` / `ThreeQuarter`, each with its own follow-offset, written onto the camera's `CinemachineFollow` at startup / on change.

**Framing rework (relative to the fish, no lurch):**
- **Was:** fixed WORLD-space Follow offset tuned for fish swimming +X, so "side view" was only a broadside when the school happened to move along that world axis — enter from another gate and the camera framed head-on / from behind.
- **Now:** at each shot the director reads the school's **entry heading** (averaged over a few frames) and which **side faces the camera**, bakes that into a world offset — framing is fish-relative (side view is a real broadside from any gate, on the near side so the camera moves *toward* the fish instead of crossing to the far side). Camera then **holds that spot and turns to keep the fish framed** (Cinemachine aim damping, pushed from code) rather than riding locked inside the fish's frame (which read as static).
- **Anti-lurch:** the proxy is now kept **glued to the fish during the pre-shot settle frames** (`_entrySettleFrames`, default 5) instead of frozen at the gate — so the shot cuts in already on the fish and doesn't jump to catch up.
- Framing offsets re-authored into the school's local frame and **renamed** (`…OffsetLocal`) so stale world-space values in existing scenes are orphaned and every scene picks up the new defaults, while staying inspector-tunable.
- **New tunables:** `_aimDamping` (turn smoothness), `_entrySettleFrames`, `_minHeadingSpeed` (milling fallback). Binding mode (World Space) + aim damping forced from code so all scenes match.
- ⚠ **Test in Play mode** — runtime-forced binding/damping don't show in the editor Solo preview.

### Tracking rework — lead the fish, don't chase them (`dd609a87`, 2026-08-24)

The shot used to feel like it was permanently *arriving* rather than settled. Root cause was a stack of small delays that all pointed the same way, so they added up instead of cancelling:

- **The proxy was damped toward a STALE target.** It `SmoothDamp`ed toward the last readback, so it lagged by the smoothing time *on top of* the readback age. Now the target is **extrapolated** — `_lastReadCentroid + _schoolVelocity * (timeSinceRead + _followSmoothTime)` — i.e. aimed at where the fish *will* be one smoothing time from now, so the damping's own delay lands it back on the present. Filtering costs no tracking accuracy any more.
- **`_leadStrength` (0–1, default 0.6) is THE framing knob.** Camera looking ahead of the fish → lower it; trailing behind → raise it. `1` fully cancels the delay, which is only exact at a *steady* speed — entering fish sprint then decelerate, so a full lead overshoots as they settle.
- **`_velocitySmoothTime`** steadies the speed reading only, never the position — the second jitter knob. Too high and it is slow to notice the post-sprint deceleration, which throws the camera ahead.
- **`_readbackEveryNFrames`** (default 4). The readback is a **synchronous GPU stall**; the extrapolation covers the gap exactly, so reading more often buys accuracy you already have at the cost of stalls. `_maxExtrapolationTime` caps how stale a reading may be counted as, so a run of failed readbacks can't fly the proxy off.
- **`_cameraPositionDamping`** (default 0.5) — pushed from code like the aim damping. This was the single biggest contributor to "forever arriving": at 1.5s the camera is still most of a tank-length short of its mark while the fish are already sprinting.
- **`_forceBrainLateUpdate`** — the Brain's default Smart Update picks its rate by *watching how the tracking target moves*, and a script-driven proxy that sits frozen between shots is exactly the case it reads wrong. A wrong pick evaluates the camera at the physics rate (50 Hz) while fish are drawn at frame rate — a small mismatch every frame that reads as relentless catch-up. Only overrides Smart Update; a Brain deliberately set to Fixed or Manual is left alone.
- **`_aimHeadingAtTarget`** — **this is why a SideView sometimes arrived as a 3/4.** New fish are spawned pointing at the simulation centre, then immediately steer onto their school's own randomised path, so their opening heading is *not* the direction they settle into — and the camera bakes one fixed world offset from that opening heading. Aiming at the school's swim *target* instead frames the direction they are actually going.
- The same commit fixed the sim advancing only every other frame — see §9.18. Entry jitter had two separate causes and both had to go.

### Per-species framing distance (`18eea1c3`, 2026-08-24)

`_speciesFramingOverrides` — one shared offset has to frame twelve species whose bodies are nothing like the same size, so a distance that suits a damselfish school crops a shark. The override is a **scale multiplier on the whole offset**, so the angle you authored is preserved exactly and the camera only sits further out along it. Species left out frame at 1× and are unchanged.

⚠ **Dial this in on the real display, not the editor Game view.** Cinemachine's FOV is *vertical*, so a narrow viewport (a trifold panel) shows less to the sides than a wide one at the same setting and crops a broadside sooner.

### Aim down the body, not the nose (`1e820fc1`, 2026-08-24)

`AimOffsetBehind` on the framing override — metres *behind* the school centre to aim at. Added for the giant moray: it is long and thin, so centring on the school centroid put most of the animal out of frame behind the focus point. Resolved once per shot (not per frame) and applied from the first proxy placement, so the shot never slides into place. `0` for every species not listed.

## 7.9 Big-Screen Reveal & Unlock Cards

Two sibling components sharing a common queue + CSV backend, fire on different events. Live in Aloysius's script folder.

### `SpeciesAddedReveal.cs` — "NEW ARRIVAL" card

- **Fires when:** a species goes 0 → 1+ population for the first time (per species). Listens to `EcosystemSimulationGPU.OnSpeciesFirstIntroduced` — same signal the intro-camera zoom uses, so card + camera never desync under rapid adds.
- **Content source:** `RevealContentDB` (`RevealContent.csv`) → `speciesName`, `role`, `firstAddedMessage`, plus `imageFile` (when `useCsvImage` is on). Every field falls back to the `SpeciesData` asset if the CSV row is missing.
- **Photo location:** `StreamingAssets/Trifold/` (big-screen images).
- **After the card:** kicks the hint via `hintSource.HintNextLocked()` after `hintDelayAfterAdd`.
- **`useCsvImage` toggle** — default OFF (text-only card, current design). Turn ON only after the `RevealImage` slot is laid out (sized + Preserve Aspect).

### `SpeciesUnlockReveal.cs` — "NEW SPECIES DISCOVERED" card

- **Fires when:** a locked species unlocks. Listens to `EcosystemUnlockManagerGPU.OnSpeciesUnlocked`.
- **Content source:** same `RevealContentDB` sheet, but uses **`unlockMessage`** column (not `firstAddedMessage`) so unlock copy can differ from arrival copy.
- **Photo source:** **`unlockImageFile`** column (falls back to `imageFile` if blank). Same `Trifold/` folder, different filename. The friend dropped 12 unlock-specific photos (`SharkUnlock.png`, `MorayUnlock.png`, `ParrotfishUnlock.png`, …) into `RevealImages/` (git-tracked).
- **`HintNextLocked()`** — finds the locked species with the fewest unmet requirements, builds a rotating hint via `BuildHint` (CSV templates → per-species flavour lines → fallback), pipes it through `AluciaController.Say()`.

### Shared plumbing

- **`RevealQueue.Get().Enqueue(...)`** — center-stage slot is serialized; an unlock card can't slam on top of an arrival card. Both cards enqueue with `key: species.speciesName`.
- **`RevealContentDB`** — one sheet feeds both cards, edited independently by fact-checkers. Header-driven parser; unknown columns ignored.
- **Both cards fade in → hold → fade out.** Hold times are **scene-serialised** (per-scene Inspector values override code defaults). Current live values in `SCENE_MainScene.unity`: **`holdSeconds` = 4s, `revealHoldSeconds` = 4s** (bumped from 2.5s → 4s on 2026-07-25 by Aloysius after testers said "too fast"). Total on-screen ≈ hold + 0.4s fade each end.
- **Reset:** `ResetShownHistory()` on `SpeciesAddedReveal` just hides the group; the actual "first added" gating lives in the sim, so `EcosystemSimulationGPU.ResetToEmpty()` re-arms both cards automatically.

### Arrival vs unlock messages are separate columns (2026-07-14)

- **`RevealContent.csv`** gained `sciName` and `unlockMessage` columns, old arrival column `blurb` renamed → `FirstAddedMessage`. Parser is header-driven, so column order doesn't matter.
- `unlockMessage` seeded from each species' `SpeciesData.addedMessage` so it starts sensible, but the two lines can now **diverge**.

### Historical fix — "NEW ARRIVAL" card showed placeholder text

- **Symptom:** the host card faded in but text stayed the design placeholder ("Species Name / ROLE / Description…") — never displayed real content.
- **Root cause (NOT the CSV):** `SpeciesAddedReveal` declared its text fields as `TMP_Text`, but the `AddedRevealCard` in the scene was built with legacy `UnityEngine.UI.Text`. Unity **cannot** assign a legacy `UI.Text` into a `TMP_Text` slot, so fields were stuck at `fileID: 0` (impossible to drag in) — every `if (nameText != null)` failed.
- **Fix:** changed the three fields in `SpeciesAddedReveal` from `TMP_Text` → **`Text`** (dropped `using TMPro;`), then wired them to the card's existing `NameText`/`TierText`/`MsgText` `UI.Text` objects. `FillCard` only ever calls `.text`, which both types have.

## 7.10 Tablet UI

**Built by Aloysius, integrated into JunHeng's main scene.**

### Food Web Graph
- **`SpeciesBubble.cs`** — 12 species bubbles laid out in trophic tiers; tapping opens species info modal. `TapPunch()` scale-punch on bubble tap (from Aloysius).
  - **Padlock stuck after a cross-tab unlock — fixed 2026-07-29 (`cc0cabe8`).** `EcosystemUnlockManagerGPU` pushes unlocks via `FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None)`, which **skips inactive objects**; switching tabs deactivates `FoodWebLayer`, so a species unlocked while the visitor was on another tab never reached its bubble and kept its padlock. `SpeciesBubble.OnEnable` now calls `Refresh()` (play mode only) so every bubble re-reads the current lock state whenever it becomes visible again.
- **`FoodWebLines.cs`** — `LineRenderer` edges between species nodes (predator arrows). Currently hidden by default (`LINE FOOD WEB HIDE` commit). Marked "wonky, TO BE CHANGED."
- Food web nodes and layout working in the scene; full visual structure of 12 species bubbles present.
- ~~**`FoodWebDragReveal.cs`** — drag/long-press reveal of predator arrows.~~ **Deleted 2026-08-11 (`9768838d`, 0 refs)** — never fully wired; the food-web arrows remain the open item (see `FoodWebLines` above).

### Species info modal
- **`ModalController.cs`** — species info modal triggered by tapping a species bubble; shows species info, Add/Remove buttons. Data-driven: pulls text + image from CSV.
- **`SpeciesInfoPanel.cs`** — panel opened via "View Details". Added `detailsHintFallbackSeconds` (default **8s**) fallback: if visitor selects a fish but never opens View Details, release the `details` gate after grace period so the food-web hint isn't gated indefinitely. Clock starts on the **first** selection only (not restarted by further bubble taps), otherwise browsing bubbles would postpone the hint indefinitely.

### Add/Remove input layer (decoupled)
- Add/Remove was extracted out of `ModalController` (which no longer touches netcode at all):
  - **`TabletAddRemoveUIGPU`** (singleton) holds the +/− buttons and optional population label; `Select(species)` resolves the netcode index via `TabletEcosystemUIGPU` and buttons fire `RequestAddSpeciesRpc`/`RequestRemoveSpeciesRpc`; greys Add at `MaxSchools`, Remove at 0.
  - **`BubbleSelectHook`** (one per species bubble) routes a bubble tap to `TabletAddRemoveUIGPU.Select(bubble.data.gpuSpecies)` **without editing the UI-team's `SpeciesBubble`** — add-component on each bubble, no per-bubble wiring.
- **Add cooldown — first-add only** (`unscaledTime`-based, exposed as `CooldownRemaining` / `CooldownDuration`; Add greys out while recovering). ⚠ **The general anti-spam `addCooldown` was removed 2026-07-29** (`fb99e6ad` "removed cooldown"): normal add/remove now has **NO cooldown** (`_currentCooldown = 0`), so rapid deliberate tapping is no longer swallowed.
  - **First-time add (count 0→1):** the only remaining lockout — **`firstAddCooldown`** (default **6s**) locks out adding **any** species until it elapses, so the big-screen reveal card + intro camera can fully show the new fish before the next add. Detected tablet-side via `OptimisticPopulationStore.Display(index) <= 0` before the add; `_currentCooldown` drives both the gate and the overlay sweep, so the radial sweep only appears on a first add. Set `firstAddCooldown = 0` to disable it entirely. ⚠ It's a **local heuristic** — a re-add after extinction (also 0→1) gets the lockout even though the *card* is first-ever-only; harmless. Exact sync would need a host "reveal in progress" NetworkVariable.
- **`ButtonCooldownOverlay.cs`** (Aloysius, `103401b3`) — FFXIV-style **radial recovery sweep** over the Add button icon that unwinds as the cooldown recovers, so a swallowed press reads as "not ready yet" instead of "broken". Purely presentational — reads `TabletAddRemoveUIGPU.CooldownRemaining/Duration`, never gates anything itself (`raycastTarget=false`), so it can't disagree with the real gate.
- **Locked-species blocking** — Add greys out when the selected species is still locked.

### Look-up prompt (Aloysius, `9ad7463a`)
- **`LookUpPrompt.cs`** — a transient "look up at the big screen" toast fired on **every Add** (`TabletAddRemoveUIGPU.OnAdd` → `LookUpPrompt.Trigger()`), because visitors stare at the tablet and miss the fish arriving on the trifold. Singleton + static `Trigger()`; repeated adds restart the hold window instead of re-fading (no flicker); suppressed while the tutorial is open; `ResetForNewSession()` clears it on exhibit reset (wired into `ExhibitReset.DoLocalReset`). **Not** a `ContextNudge` — it re-fires all session rather than dismissing once.

### Balance advisor (Aloysius, `c4d0a8b8`)
- **`BalanceAdvisor.cs`** — bottom-left **"HOW TO BALANCE"** panel. Every line is derived from the **sim's own** `GetSpeciesStatus` per species (so it can never disagree with the eco-health bar). Two anti-answer-key rules: **(1) grouped, not enumerated** — problems collapse by kind (eight missing species = one "sparse reef" line); **(2) escalating detail** — opens vague and only names the species, then the fix, after eco-health fails to reach a new high for `escalateAfter` (25s); any progress drops it back to vague. Hidden before start and while the tutorial is open (which also freezes the stuck-clock). Advisory-only (never blocks taps). Ranks active collapses (starving/over-predated) above mere absences. **2026-07-29 (`cc0cabe8`):** now shows `balancedMessage` whenever eco-health ≥ `balancedThreshold`, so it can't print an issue line under a THRIVING gauge (same status-vs-health disagreement fixed for the row numbers — see §7.2).

### Current Organisms view
- **`CurrentOrganismsGrid.cs`** — grid of currently-living species; card icons now come from `SpeciesContent.csv` `revealImageFile` (a tablet-folder copy of the big-screen arrival art), falling back to `imageFile` (info portrait), then the bubble's inspector `cardImage`, so a card never goes blank.
- **`OrganismCardData.cs`** — per-card data + overpopulation badge (reads from sim via `NetworkList`).
- **Per-species +/− numbers on each row** (`_speciesDelta` `NetworkList<int>` on `EcosystemNetworkManagerGPU`, host-computed via `GetSpeciesDelta`): shows how many to add (`+N`, blue) / remove (`−N`, red), or a word — **"hungry"** (`DeltaNeedsPrey`, fix is more prey) / **"hunted"** (`DeltaCapped`, over-hunted at its `MaxSchools` cap so the only lever is fewer predators) / **"ok"** (`DeltaOk`). Renders on the number/sentinel **alone** (see §7.2 consistency fix) so it can't contradict the gauge. Labels shortened 2026-07-29 to fit the Rajdhani row width.
- **Scrollbar** added to the Current Organisms panel (design pass).

### Health bar (two drivers)
- **`Health.cs`** (tablet client) — reads the **networked** value `EcosystemNetworkManagerGPU.GetEcoHealth()` when `readFromSimulation` is on (needs host running + `fillImage` assigned). In the standalone prototype scene, falls back to the manual value when no host is running. ⚠ Superseded by `HealthBarBinder` on the host side.
- **`HealthBarBinder.cs`** (host large screen) — reads **`EcosystemSimulationGPU.EcoHealth01` directly** — no netcode, auto-finds the sim. ⚠ Depends on JunHeng's `EcoHealth01` staying `public`. Auto-finds the sim; exponential smoothing.

### Hints panel
- **`HintsPanel.cs`** (`HintsPanel.BuildHint`) — computes **live** unmet requirements first ("get eco-health to X%", "add N more Y") so the Hints tab is always accurate against current populations, only falling back to `hint.flavour` → `SpeciesData.hint1` → generic line when nothing concrete is outstanding. **Priority was inverted** — previously a non-empty `hint1` short-circuited everything and hid the real requirements.

### Notifications
- **`NotificationManager.cs`** — tablet unlock/notify popups. Called by `EcosystemUnlockManagerGPU` on unlock.
- **Unlock popup redesign on tablet** (`d521c5f`).

### Overpopulated badge — sim rule not "at capacity"
- **Both tablet badges** (`SpeciesBubble.UpdateOverpopulation`, `OrganismCardData.UpdateOverpop`) previously fired on `pop >= MaxSchools` — a species sitting at its cap in a healthy ocean, which is *at capacity*, not overpopulated (why 6 damselfish falsely showed the badge at 100% health).
- **`EcosystemNetworkManagerGPU`** now syncs a per-species `NetworkList<int> _speciesStatus` (the sim's `SpeciesStatus` as int, written each `SyncPopulations`), exposed as `GetSpeciesStatus(i)` / `IsOverpopulated(i)`. Both badges now call `net.IsOverpopulated(index)`, so tablet and sim agree.

### Instant population feedback
- `EcosystemNetworkManagerGPU` resyncs the `NetworkList<int>` immediately after an add/remove RPC instead of only on the 1s tick, so tablet count updates without lag.
- **`OptimisticPopulationStore`** (static, client-side) — records each +/− tap and reports `synced + pending` so the tablet count updates **instantly** instead of lagging until removed fish finish swimming out; keyed per-species-index so it survives card/panel rebuilds. Kills the old snap-back bug.

### Modal / DimOverlay fix
- **"Coroutine couldn't be started because the game object 'DimOverlay' is inactive"** — dim-overlay reset moved out of `Start()` into `Awake()`. The panel is authored inactive, so `Start()` ran a frame **after** the first `Show()` had already activated the overlay, deactivating the dim right after it appeared (and breaking swipe-close). `Awake` runs synchronously inside `Show()`'s `SetActive(true)`, *before* the overlay is re-activated.
- **`DimFader.FadeTo`** — snaps to target and fires `onComplete` **synchronously** when `!isActiveAndEnabled`, instead of starting a coroutine on an inactive object.

### Recent polish (2026-07-30 → 08-10)
- **Current Organisms** reworked (`363a8331`, `213dbb90`) — card prefab + `OrganismCardData` layout fixes ("Fixed currentorganisms").
- **View-details / look-up arbitration** (`211dca1c`) — swapped the button art; added `LookUpPrompt.IsShowing` so `ContextNudge` yields the shared banner slot to the toast and pauses its own visible-time counter (bursts of adds no longer eat the hint window).
- **Prompt redesign** (`e920d3bf`) — scene-only prompt art/layout.
- **Help button gated** (`b39a52a8`) — hidden until after the start-gate canvas.
- **Bystander UI panel fix** (`0dec41e3`) — scene / font-asset only.
- **Balance UI restyle** (`0c363b9d`) — new `Balance.png`; asset renames (lollll→Hintfish, baa→hinttextbox, newbggggg→UIBG); deleted the stale `new netcode 3.unity` scene.
- **`ButtonCooldownOverlay` refactor** (`37ace428`) — split into `RecoveryFraction` / `ApplyStyle` / `SetSweep` helpers, behaviour unchanged (radial Add-button cooldown).
- **`TextGlowPulse.cs`** (`73dbed3a`) — play-mode-only soft TMP glow pulse on the **material instance** (not the shared material, so it doesn't throb all ~28 Rajdhani labels); optional subtle scale breath.

## 7.11 Alucia (voice / NPC guide)

- **`AluciaController.cs`** — translucent speech bubble, big-screen only. 3 visual states: **Default** (light blue, tips + unlock announcements) / **Warn** (orange, overpop/underpop alerts) / **Win** (green, ecosystem recovered). Auto-hides after 5.2s; sticky for win message.
- **Speech bubble auto-sizes to text** — `VerticalLayoutGroup + ContentSizeFitter (Vertical Fit = Preferred)` on `AluciaBubble` so it grows/shrinks with the line; `AluciaController.Say()` calls `LayoutRebuilder.ForceRebuildLayoutImmediate` so it resizes the same frame. ⚠ Bubble sprite should be Image Type = **Sliced** so it doesn't distort.

### Event system

- **Health-band reactions:** `EvaluateHealth(EcoHealth01)` → `health.critical` / `unstable.up` / `unstable.down` / `healthy` / `thriving`.
- **Intro:** `intro.1` / `intro.2` / `intro.3` (multi-line intro).
- **Unlock:** `NotificationManager` speaks unlock lines.
- **Cause-aware ecological events** (per-species): `species.starving` / `species.overpredated` / `species.overpopulated` / `species.extinct` / `species.added` (first-ever add only).
- **`AluciaEcologyEvents.cs`** polls the sim, detects each state, speaks the matching CSV line. Wired with a **settle window** after a count change and fire-once-per-entry to prevent spam.

### Timing gates (`AluciaEcologyEvents`)
- `startupGrace = 8s` — ignore first 8s.
- `settleSeconds = 5s` — waits 5s of NO count change; every tick that changes a count resets the settle. Growing species stays silent until it stabilises at its cap.
- `checkInterval = 2s`.
- Lower to ~**3 / 2 / 1** for snappier reactions.

### Hard-mute flag (reset safety)
- **`_muted`** flag on `AluciaController`: set true in `ResetForNewSession`, cleared in `HandleStarted`; `Say()` is a no-op while muted. She physically can't speak between a reset and the next real start.

### Per-species ecological hints (specific, kid-friendly, food-web-accurate)
- `alucia_lines.csv` (~67 rows) — for `species.overpopulated / overpredated / starving`, every one of the 12 fish has a **species-scoped** line that names its **real predators/prey** (pulled from the `SpeciesDataGPU` prey/predator lists) and tells the player what to do. Example: scad overpredated → *"remove a Brown Marbled Grouper or a Bluefin Trevally"*.
- **No dashes/hyphens in visible text** (kids/families).
- **Moods set by meaning**: real problems = `Warn`, reassuring "this fish is fine" lines (top predators can't be over-predated, grazers/ray can't starve) = `Calm`.
- ⚠ The `Species` column must equal `SpeciesDataGPU.SpeciesName` EXACTLY (case-insensitive) — keeps original spelling incl. the grouper's hyphen and **Russell's snapper's curly apostrophe (’)**.
- Em dashes → commas across Alucia's lines (kid-friendly punctuation pass).

## 7.12 Content Pipeline (Google Sheets → CSV → StreamingAssets)

**Fact-checkers (incl. overseas) edit game content in a spreadsheet; with a published-sheet URL, changes appear in the game on next launch with no rebuild.** Everything degrades gracefully offline.

### Three CSVs / three Google Sheet tabs

**Google Sheet: "OceanX Content"** — created in JunHeng's Google account 2026-07-11.
- **URL:** https://docs.google.com/spreadsheets/d/1yjne2lD4rmjwjPwED5OUm1_14MigDqRZOFVaaG7YjqU/edit
- Published-to-web endpoints (public read-only by design, **not** API keys); the game fetches these:
  - `alucia_lines.csv` → gid `1093782534`
  - `SpeciesContent.csv` → gid `196784187`
  - `RevealContent.csv` → gid `1248841811`

| CSV | Content | Read by | Shown on |
|-----|---------|---------|----------|
| `alucia_lines.csv` | Alucia's spoken lines — intro, health reactions, unlock, hints, ecological events | `AluciaController`, `NotificationManager`, `SpeciesUnlockReveal.BuildHint`, `AluciaEcologyEvents` | Big screen |
| `SpeciesContent.csv` | Per-species facts — long-form modal detail (name, sciName, iucnStatus, description, diet, habitat, imageFile, revealImageFile) | `SpeciesContentDB` → `ModalController`, `CurrentOrganismsGrid` | Tablet |
| `RevealContent.csv` | Big-screen card copy — short punchy blurbs (id, speciesName, sciName, role, firstAddedMessage, unlockMessage, imageFile, unlockImageFile) | `RevealContentDB` → `SpeciesAddedReveal`, `SpeciesUnlockReveal` | Big screen |

### Live-fetch service (new)

- **`ContentService.cs`** (`Assets/Junheng/Scripts/Content/`) — downloads each CSV from its published URL at launch → caches to `persistentDataPath` → falls back to the baked `StreamingAssets` copy → then the hardcoded lines. **Fixes tablet editing:** baked StreamingAssets is unreadable on Android, but the downloaded cache is.
- **`redirectLimit = 32`** — a Google published-CSV URL 307-redirects to `googleusercontent`; the fetch must follow it.
- Non-CSV downloads log the **HTTP code, final URL, and a 120-char body snippet** + a reminder the URL must end in `&output=csv`.
- **`ContentService.LocalPathFor(file)`** — what the loaders read; works with or without a `ContentService` in the scene (no service = baked copy, exactly like before).
- **`CsvUtil.cs`** — one robust RFC-4180 parser (quotes, commas, embedded newlines) shared by both loaders. **Quote-only-at-field-start bugfix** (was HIGH severity — a stray `"` in a cell put the parser in quoted mode for the rest of the file → silent row loss).

### Alucia event/variant model (`alucia_lines.csv` columns: `Event, Species, Mood, Weight, Text, Notes`)

- **Event** = stable ID the game matches on (`intro.1`, `health.critical`, `hint.withReq.1-4`, `species.starving`, `species.overpredated`, `species.overpopulated`, `species.extinct`, `species.added`, `hint.flavour`, …).
- **Multiple rows per Event = variants** — the game picks one (weighted, no immediate repeat) so she doesn't sound repetitive. Checkers add a variant by copying a row.
- **Species** (optional) scopes a line to one fish; blank = any fish. Species-specific rows win over generic.
- **Mood** (Calm/Warn/Win) drives the bubble tint.
- Rows whose Event starts with `#` are comments (there's a built-in instructions/legend block at the top).
- **`AluciaLines.GetVariants(event, species)`** returns every variant for an event scoped to a species — used for `hint.flavour` per-species hints.
- **`AluciaLines.GetLine(event, species)`** returns text + mood.

### `SpeciesContent.csv` — stable ids (2026-07-11)

- Added **`id`** column (e.g. `blacktip_reef_shark`) as the real match key, with `speciesName` as display text — so a name can be reworded/translated without breaking the card.
- Added `habitat` / `funFact` / `revealImageFile` columns.
- `SpeciesContentDB` indexes by **both** id and name, so either resolves.
- `SpeciesData.contentId` (new, optional) + one line in `ModalController` use it.

### Image folders — three types, cleanly separated

- **`StreamingAssets/SpeciesImages/` → renamed to `Tablet/`** (via `git mv` so history preserved).
- **`StreamingAssets/RevealImages/` → renamed to `Trifold/`** (via `git mv`).

| Image type | Shown | CSV → column | Folder |
|------|-------|--------------|--------|
| **Info** | tablet modal (fish description) | `SpeciesContent.csv` → `imageFile` | `Tablet/` |
| **Reveal** | big screen, first **added** + tablet organism cards | `RevealContent.csv` → `imageFile` **AND** `SpeciesContent.csv` → `revealImageFile` | `Trifold/` (big screen) + `Tablet/` (tablet copy) |
| **Unlock** | big screen, first **unlocked** | `RevealContent.csv` → `unlockImageFile` | `Trifold/` |

- **Matrix: `Trifold/` = reveal + unlock · `Tablet/` = info + reveal.**
- Reveal images renamed with `Reveal` suffix (`blacktip.png` → `blacktipReveal.png`, ×12) so inside `Tablet/` the reveal copy never collides with the same-named info photo.
- **`SpeciesContentDB.cs`** folder constant → `"Tablet"`. **`RevealContentDB.cs`** folder constant → `"Trifold"`.

### Editing tool — Google Sheets vs Excel

The live-fetch needs a URL that returns **CSV text**. **Google Sheets** does this cleanly (File → Share → Publish to web → CSV). **Excel does NOT** publish a CSV URL — an Excel Online / OneDrive link returns the web viewer (HTML) or the binary `.xlsx`, neither of which the loader reads. With Excel the options are (a) manual "Save As CSV → drop it in" (no live updates), or (b) keep a `.csv` (not `.xlsx`) on OneDrive/Dropbox/GitHub with a direct link (semi-live; must re-upload to change). **For live, no-rebuild editing, use Google Sheets.**

### Sheet workflow

- The **Google Sheet is the source of truth** once the team edits it directly.
- Sensible habit: pull the sheet → local CSV, and prefer pushing *new* columns / individual cells over blanket overwrites of existing data columns.
- Any change is recoverable via the sheet's **File → Version history**.
- Programmatic sheet edits are fine (e.g. the `gws` CLI is used as JunHeng for column deletes).

## 7.13 Netcode (Unity Netcode for GameObjects / NGO)

**Host/Client architecture over WiFi.**

- **`NetworkBootstrap.cs`** — sets role (Host/Client), starts NGO. Reads `boot.config` (`player-connection-mode=Listen`, `player-connection-debug=1` in dev builds).
- **`EcosystemNetworkManagerGPU.cs`** — auto-finds `EcosystemSimulationGPU` on server. Syncs:
  - `NetworkList<int>` school counts (periodic tick **+ immediate resync on add/remove**).
  - `NetworkList<int> _speciesStatus` — per-species `SpeciesStatus` as int (for tablet Overpopulated badge).
  - `NetworkVariable<float> _ecoHealth` — pushed each sync; `GetEcoHealth()` for clients.
  - `NetworkVariable<bool> _hasStarted` — the "tap to begin" gate.
- **RPCs exposed:** `RequestAddSpeciesRpc(index)`, `RequestRemoveSpeciesRpc(index)`, `RequestStartRpc()`, `RequestResetRpc()`.
- **Cap/floor enforcement is server-side** (not just greyed in the UI) — a rogue LAN client can only do what the tablet can do. Index validation uses an unsigned cast that rejects negatives and `int.MinValue`.
- `NetworkList`/`NetworkVariable` are server-write-only.
- Player counts as Client 0 in host mode.

### `TabletEcosystemUIGPU`
Pure species→index lookup service (card UI stripped out entirely).

### `HostSpawner.cs`
⚠ **DEAD** — zero refs anywhere (verified 2026-07-16). `NetworkBootstrap` spawns the net-manager on server start. Safe to delete.

## 7.14 LAN Discovery

**`LanDiscovery.cs`** — UDP broadcast on port 47777; tablet auto-discovers host on same WiFi network. Advertiser starts automatically when `NetworkBootstrap` starts the host. No manual IP entry needed after initial setup.

**`ConnectionScreenUI.cs`** — tablet IP entry screen + LAN auto-discovery. IP field pre-fills with the discovered IP when available.

## 7.15 Reset Flow (F9 → bubble wipe → attract state)

A full reset for the next visitor: empties the ocean, re-locks every species, zeroes eco-health, returns BOTH screens to "Tap to Start" attract state with the intro re-armed. The big screen plays a SpongeBob-style bubble wipe that hides the reset; the tablet flips back to the title.

### The reset chain (host-authoritative, hidden behind the wipe)

1. Operator holds **F9** on the host (a hold, not a tap — `ExhibitReset.holdSeconds`, default 1.5s).
2. Trigger: `RequestResetRpc()` → server fires `OnHostResetRequested` → the host plays the bubble wipe.
3. **At the wipe's covered PEAK** (screen hidden): `EcosystemNetworkManagerGPU.PerformResetCore()` empties the ocean (`EcosystemSimulationGPU.ResetToEmpty()` — instant hard-remove of every school via the tested cull path, one rebuild), drops `_hasStarted` to false, syncs 0 counts + 0 health; then `SignalResetApplied()` bumps `_resetGeneration` → `OnReset` on host **and** tablet.
4. **On `OnReset` (both devices)** `ExhibitReset.DoLocalReset()` runs:
   - `OptimisticPopulationStore.Clear()` — drop pending taps.
   - `EcosystemUnlockManagerGPU.ResetToStart()` — re-lock to the 5 starters + reset hints.
   - `FindObjectsByType` → per-component reset on: `ExperienceStartGate.ReturnToAttract`, `AluciaController.ResetForNewSession` (re-arms the intro), `HideUntilStarted`, `ContextNudge`, `SpeciesAddedReveal.ResetShownHistory`, `RevealQueue.ClearAll`, `NotificationManager.ClearAll`, `WinCondition.Reset`, `StartCrossfade.ResetForNewSession`, `TutorialPanel.ResetForNewSession`, `TabController.ResetForNewSession`, `LookUpPrompt.ResetForNewSession` (added 2026-07-27).

⚠ **Two-phase timing is deliberate**: the tablet re-locks only after the host has synced health→0, so its unlock check (which never re-locks) can't immediately re-unlock. That's why the tablet resets ~coverDuration after F9, not instantly.

### `BubbleTransition.cs` (big-screen only)
- Self-contained: builds its own full-screen overlay canvas + generates a soap-bubble sprite (or uses an assigned one).
- Stream of mixed-size bubbles rises **continuously bottom→top** (matches the prototype's `resetGame` flood — no stop/hold on the bubbles); each fades only when it reaches the TOP (by height, not a global timer).
- A water **veil** fades in→hold→out to hide/reveal the screen.
- `Play(onCovered, onComplete)`: `onCovered` fires at full veil cover (the reset runs there).
- **Tunables:** `coverDuration` / `holdDuration` / `revealDuration` (the veil), `bubbleRiseSeconds` (how long a bubble takes to rise before it fades at the top), `bubbleCount`, `bubbleSizeRange`, `waterColor` (**lower alpha = lighter, more see-through; alpha 0 = pure bubbles, no opaque hide**), `bubbleSprite`.
- Big-screen only because it's played inside the host's `OnHostResetRequested` path (`IsServer`); the tablet never calls `Play`.
- **Currently tuned:** transparent veil `waterColor a=0`, `bubbleCount 1000`, custom `bubble.png` sprite, cover 1.5s.

### Reset gaps fixed (one-shot latch → re-armable)
The following components latched `_played` / `_shownOnce` / `_initialized` and never fired again after reset. All got a `ResetForNewSession()` (or equivalent) so they re-fire on the next start:

- **`StartCrossfade`** — reveals the food-web UI on start; latched `_played`. Now re-arms on reset, restores the tap overlay, re-hides the pieces. Stays subscribed to `OnStarted` so next start replays it.
- **`ExperienceStartGate.ReturnToAttract`** — restores TapOverlay CanvasGroup alpha/interactable/blocksRaycasts (was coming back invisible: "empty water" bug).
- **`TutorialPanel`** — `_shownOnce` latched forever. Now hides + re-arms on reset.
- **`TabController`** — `_initialized` latched; next visitor inherited previous tab. Now snaps back to `defaultTab` on reset.
- **`AluciaController`** — added hard `_muted` flag (see §7.11).
- **`RevealQueue.ClearAll`** + **`SpeciesAddedReveal.ResetShownHistory`** + **`NotificationManager.ClearAll`** — fire at the F9 request instant (not just at covered peak) so they can't linger visible under the transparent veil.

## 7.16 Adaptive Music System

**`AdaptiveMusicSystem.cs`** (Aloysius) + `Editor/AdaptiveMusicSetup.cs`. Replaces deleted `MusicDirector.cs`.

- **Health-driven whole-song switcher** (horizontal re-sequencing): one mood track plays at a time, each owning a band of live eco-health (0–1); crossing a band **crossfades** to that band's song.
- Songs are standalone tracks of different lengths/tempo/key — switching whole songs avoids the phase-drift of permanent vertical layering.
- **Won't flicker** on the (deliberately oscillating) sim:
  - Health input is smoothed (`healthSmoothing`).
  - Band selection has **hysteresis**.
  - **Minimum dwell** (`minMoodSeconds`) blocks rapid re-switching.
- **Equal-power fades in dB** (sin/cos) driving each song's mixer-group volume — no mid-crossfade "hole."
- Individual soundtracks play one-at-a-time instead of all at once.
- **Per-species intro stings** (2026-07-29, `3217cd24`): `SpeciesData` gained `introSound`/`introVolume`; `AdaptiveMusicSystem.PlayIntro()` routes a species' sting through the swell source (falls back to generic swell if null). `SpeciesAddedReveal` now calls `PlayIntro` instead of `PlaySwell`.

### UI sound layer (rewritten 2026-08-03, `88522e05`)
- **`UISoundManager.cs` is now a full multi-voice SFX manager** (was a one-shot `PlayTap` wrapper). Scene singleton (`Instance`) with a round-robin pool of 2D `AudioSource` voices (default 4) so rapid sounds don't cut each other off. `Play(UISound, volumeScale)` where `UISound` is an enum: **Tap, Add, Remove, Locked, Disabled, TabSwitch, ModalOpen, ModalClose, Unlock, SpeciesAdded, Notification, Warning, Win**. Each entry supports random `variants`, per-sound `volume`, and `pitchJitter`. Clips wired in the Inspector (optional `Resources/UISounds/<Enum>` autoload fallback).
- **Call sites** fire directly, all null-guarded: `TabController.Select`→TabSwitch, locked bubble tap→Locked, `ModalController`→ModalOpen/Close (edge-guarded), `NotificationManager`→Unlock, `LookUpPrompt`→Notification, `BalanceAdvisor`→Warning (rising-edge only), `TabletAddRemoveUIGPU.Add`→ first-time SpeciesAdded / cap Disabled / else Add, `Remove`→Remove, `WinScreen.Show`→Win.
- **Clips** live in `Assets/Sounds/UI/OceanX/`. ⚠ The first Add/Remove batch (`73d18ddd`, "added remove audio (not working)") were placeholder duplicates (all identical 168014-byte files, so Remove didn't sound distinct); `6aaffd73` (2026-08-07) swapped in distinct real Add/Remove clips + loudness-matched volumes. **Verify in-scene that the Remove entry references the new clip** — final clip wiring lives in the `.unity` scene, and Remove is triggered from two sites (`OrganismCardData.RemoveSpecies` + `TabletAddRemoveUIGPU.Remove`).
- `841e85cd` imported an unused alt SFX set under `Sounds/UI/` labelled "prob wont be used".

## 7.17 Environment (Coral Growth + Reef Ambience)

**`EnvironmentHealthReveal.cs`** — the reef visually builds up as `EcoHealth01` rises: corals start hidden and pop in (one-by-one or in groups) as health climbs; retract as it declines. **Play-tested and working.**

- **Host-side driver** (`[ExecuteAlways]`) reads `EcosystemSimulationGPU.EcoHealth01` **directly** (mirrors `HealthBarBinder` — the sim + corals only exist on the big-screen host; no netcode). Smooths + clamps `0..1`.

### ⚠ Corals MUST have "Batching Static" UNCHECKED
- Static batching bakes transforms into a combined mesh, so runtime `localScale` changes are ignored — the effect silently does nothing in Play (works in edit mode only).
- Keep Contribute GI / Lightmap Static for the baked lighting; only *Batching* Static must be off.
- **Do not re-enable Batching Static on the corals** or this breaks.
- Suggestion: tick GPU Instancing on the coral materials to offset extra draw calls from un-batching.

### Preview toggles (turn OFF for the exhibit + before saving)
- `debugOverride` + `debugHealth` slider — fake a health value.
- `previewInEditor` — drive scale in edit mode (⚠ off before saving or corals save shrunk).
- `logDebug` — logs collected counts + live health/visible.

### Master toggle `effectEnabled`
- OFF = all corals forced fully visible (normal scene).
- ON = start-hidden reveal effect.

### Reveal groups
- A list; members collected from a **labelled parent Transform** (auto-collect child renderers).
- **Three per-group `appearMode`s:**
  - **Proportional** — visible count tracks health: `round(health01 * childCount)`. 100 corals → 1 per 1%; 300 → 3 per 1%; corals pop in/retract one-by-one as the bar moves — no manual grouping needed. Has `startHealth`/`endHealth` range.
  - **AllAtOnce** — whole group at one `threshold`.
  - **Staggered** — one-by-one via `staggerInterval` after the threshold.
- Plus `randomOrder` (shuffle the reveal sequence), `growDuration`, overshoot pop, hysteresis margin.
- ⚠ **All corals are flat under a single `Corals` parent** (with separate `Seagrass` / `Rockwork` parents) — intended setup is **one Proportional group on `Corals`**.

### Reveal technique = SCALE POP-IN (placeholder)
- Chosen as a placeholder — swapped from an earlier transparency attempt because the corals are opaque and dense.
- Corals grow from scale 0 → authored scale with an optional overshoot "pop."
- All visual writes go through `ApplyReveal` / `ForceVisible` so the technique stays swappable.
- ⚠ **Transparency-alpha and a dissolve/alpha-clip shader are BOTH still on the table** and may replace scale pop-in later.
- Akil added `ColorRecover` reveal style alongside `ScalePopIn` — corals stay full-size and regain colour from bleached (see §7.6).

### Scene environment (Akil)
- **Baked lighting** — Lightmaps + `LightingData.asset` + reflection probes + `Sky.mat`.
- **Bubble particles** added to the scene.
- **Reef obstacles rebaked** against the environment mesh.
- **Coral placement, rockwork, mockup-scene redo, shader/rock-colour passes.**
- Imported fish + stingray + parrotfish + damselfish + shark meshes.

## 7.18 Onboarding (Tutorial, Context Nudges)

**`TutorialPanel.cs`** — onboarding HOW TO PLAY panel. Auto-shows once on start. Now hides + re-arms on reset (see §7.15).

**`ContextNudge.cs`** — progressive hint bubbles ("tap a fish", "hold to see food web", etc.). Chainable via `showAfterId` and a public static **`ContextNudge.Advance(gateId)`** — call from anywhere to release nudges gated on `gateId`.

**Rejoin-race fix** — a tablet that joins a session **already started** receives `_hasStarted` inside the network spawn payload, which lands **before** `OnNetworkSpawn` wires up `OnValueChanged`, so `OnStarted` never fires. Root pattern in both `ContextNudge` and `TutorialPanel`:
- Now **ALWAYS subscribes** (not just in the "not started" branch) AND polls `HasStarted` every Update as a backstop.
- Added `_rearmPending` so a reset waits for `HasStarted` to go back to false before re-arming (avoids a stale-true re-show).

**Details-hint chain:**
- **`ModalController.Close`** calls `ContextNudge.Advance("details")` when a species panel closes — releases the "hold to see food web" hint at the moment a visitor has actually **read and dismissed** a panel (not just tapped a bubble).
- **`SpeciesInfoPanel.detailsHintFallbackSeconds`** (default **8s**) safety net — if visitor selects a fish but never opens View Details, release the gate after grace period so the hint isn't blocked forever.

## 7.19 Fish Entry / Exit + Swim-in-out Animation

**`FishEntryPointGPU.cs`** — drop-in marker (Entry / Exit / Both) placed OUTSIDE the bounds. New schools spawn at a random entry marker and swim in; removed schools swim out to a random exit marker. Auto-registers into a static list (no wiring).

**`EcosystemTargetGPU.cs`** — per-school swim target. `AddSchool` creates one (+ a `TransformAnimator` on a Line/Circle/Rectangle path) per school; `ParkAt(exitPoint)` drives the swim-out on Remove. **REPLACED the deleted `WanderingAffecterGPU`** (grep confirms the type no longer exists).

### Removal / swim-out model
- Remove is **immediate + concurrent** — each press parks one more of the species' TOP schools at an off-screen exit point so it beelines out now.
- A single `BatchExitRoutine` culls the whole exiting block once all its fish reach the exit (no fixed timer).
- Spamming Remove sends several out at once.
- **Tunables** in the "Removal animation (swim-out)" Inspector group on `EcosystemSimulationGPU`: `_exitArrivalRadius`, `_exitPollInterval`.

### Anti-stack spawning (2026-07-08)
- **Symptom:** spamming Add spawned several schools **stacked inside each other** at the same spot.
- **Cause:** new schools swim in from `FishEntryPointGPU` markers, which are **single points with no size**. `ApplyEntrySpawnOrigin` picked a marker **uniformly at random each Add** and placed the whole school exactly on `marker.Position`.
- **Fix (all in `EcosystemSimulationGPU`; exit logic untouched):**
  - **`PickEntryMarker()`** avoids reusing the previous Add's marker when more than one entry point exists → consecutive schools fan out across gates.
  - **`ChooseSpreadOrigin()`** jitters each new school **sideways** off the marker (perpendicular to its swim-in direction, so it stays off-screen) and retries a few times to stay clear of the **last 8 spawn origins** (shared across all species); falls back to the most-separated attempt.
- **Two new Inspector knobs** on `EcosystemSimulationGPU` → "Entry Spawn Spreading (anti-stacking)":
  - `_entrySpawnJitterRadius` (default `4`) — sideways nudge radius; `0` = old behaviour.
  - `_entrySpawnMinSeparation` (default `3`) — preferred gap between a new origin and recent ones.
- ⚠ **With only ONE entry marker under heavy spam** there's a hard space limit — add more entry markers (they round-robin) or raise the jitter radius.

### Fish spawning inside each other WITHIN one school (`989cfb77`, 2026-08-24)

Different problem from the anti-stacking above, and fixed separately. That one spread **schools** apart; this one is about fish **inside a single school** overlapping at birth. All in `BoidSpawnUtility`.

- **`GenerateEquidistantPointsInsideSphere` never actually guaranteed the spacing** it was named for. Replaced with **`GenerateSeparatedPoints`** — a jittered lattice whose pitch *is* the minimum spawn distance, so separation is enforced rather than hoped for.
- **`minSpawnDistanceBetweenBoids` now floors at the species' own `SeparationRange`** (`BoidSpawnerBase.ResolvedMinSpawnDistance`). Fish were being born tighter than their own flocking rules want them, so the school's first act was to shove itself apart.
- **The bounds reduction was half what it needed to be.** It reserved `spawnAreaDimensionSize * 0.5`, but the cluster centre is picked *inside* the reduced bounds and the offsets reach half a cluster width in *each* direction — so the outer fish spawned **through the boundary**, where the compute shader reads them as a school on its way out. Now reserves the cluster's **full** width, `Vector3.Max(..., zero)`-clamped so a cluster wider than the tank degrades to "spawn at the centre" rather than inverting the range.

### Add during a swim-out = RECALL, not a new school (`23c2751c`, 2026-08-29)

**Symptom:** spam Remove to extinction, then spam Add — the tablet number climbed to the cap over a visibly empty ocean, then the whole batch of schools arrived at once seconds later.

**Cause:** two separate things, both real.
- `PumpQueue` opened with `if (IsExiting(species)) return;`, so **every Add sat in the op queue for the entire swim-out** (up to `_exitTimeoutSeconds` = 25s). Meanwhile `OptimisticPopulationStore` on the tablet had already counted the taps, so the displayed number was a promise the sim had not accepted.
- `AddSchool`'s cap test used the **raw** count, which still includes schools mid-swim-out. At 8 of 8 leaving, `n` was still 8 and the tap was silently swallowed by fish that had already stopped counting for the visitor.

**Why the pump blocked in the first place** — this is the load-bearing constraint, do not "simplify" it away: `CommitRemoveSchools` requires that *exiting schools are the contiguous TOP block* `[n-e, n-1]`, so culling them leaves every survivor's sub-group index unchanged, and `BatchExitRoutine` recomputes `firstExiting = n - e` on every poll. A **new** school takes index `n` — *above* the block — which breaks contiguity, and the routine would then poll and cull the wrong schools, including the newcomer.

**Fix — `TryRecallExitingSchool`.** While anything is exiting, an Add is served by turning the newest departure around instead of spawning a school. Removal parks *downward* from the top (`StartRemoveExitImmediate` parks `settled - 1`), so the newest exit is the block's **lowest** index, `n - e`. Un-parking exactly that one shrinks the block from the bottom to `[n-e+1, n-1]` — **still a contiguous top block**. The invariant is worked *with*, not fought. Nothing is created or destroyed, so there is **no `ReinitializeBuffers()`** — a recall is far cheaper than an add.

Spam-remove-then-spam-add ×12 against 10 exiting resolves as 10 recalls (instant) then 2 ordinary adds, which proceed immediately because `_exitingCount` is now 0.

- **`EcosystemTargetGPU.Unpark()`** — mirror of `ParkAt`. Re-enables the animator, calls **`TransformAnimator.ResetAnimation()`**, then resyncs `AffecterPosition` from the transform.
  - ⚠ **The reset is not optional.** `TransformAnimator.Update` steps the target from wherever it currently *is* toward its next waypoint — it never snaps to the path. Just re-enabling the animator left the target at the off-screen exit point, crawling home at `MovementSpeed` (2 m/s) with its whole school in tow, milling around the gate. `ResetAnimation()` (extracted from `Start`, so both share one code path) drops it straight back onto the path start **and** re-seeds the waypoint state — position and next-waypoint are a pair, and setting only the position sends the target off toward a stale corner.
  - The `AffecterPosition` resync matters for timing: the affecter caches its own copy, so without it the GPU sees the exit-point position for one more dispatch — and the shader decides "exiting" purely from `IsStrictlyOutsideBounds(exitTarget.position)`, so the school would read as exiting for an extra frame.
- **`BoidSimulationGPU.TryRearmEntrySprint(startIndex, count)`** — recalled fish are parked off-screen, and would otherwise amble home at cruising speed. Writes `-1` into `EntryBoostTimeRemaining` (the same value `BoidSpawnerGPU.SpawnBoids` arms a fresh school with) so they rush back at MaxSpeed, and the kernel flips it to `_EntryBoostDuration` when they cross in, exactly as for a real arrival. **Skips fish already inside the bounds** — writing `-1` to one of those makes the kernel read it as an arrival that never crossed in, so it would sprint until it happened to wander out and back. Read-modify-write of a buffer slice = **two GPU stalls**, fine at one-visitor-tap frequency, never call it per frame.
- **`_exitRoutineActive` guard.** A recall can now zero `_exitingCount` **from outside `BatchExitRoutine`, while it is asleep between polls.** A Remove landing in that window used to see `alreadyExiting == 0` and start a **second** routine on the same species — both would then cull the block. Routine start now keys off this set, not the count. Released just before the tail `PumpQueue`, and cleared in `OnDisable` for the same reason that method already clears `_exitingCount`: a killed coroutine never runs its own cleanup, and a stale entry would permanently stop any future routine from starting. **Deliberately NOT cleared in `ResetToEmpty`** — a live routine there self-releases on its next poll, and clearing it early would re-open the very race the guard exists to close.
- **The cap check deliberately stays on the RAW count.** It is tempting to switch it to `CountCommittedGroups` so departing fish stop blocking an Add — **don't.** Each species reserves exactly `MaxSchools` flock IDs (`SetupAllSpecies`) and a school's ID is `base + index`, so a raw overshoot aliases a flock onto the **next species' reservation** and silently merges two species into one shoal. The population tick calls `AddSchool` directly, bypassing the pump, so a committed-based cap really could overshoot. It costs nothing to keep raw: the pump recalls before ever reaching `AddSchool`, and by the time it gets there nothing is exiting and the two counts are equal.

### Faster turning for entry / exit only (`23c2751c`, 2026-08-29)

**`FishMovementProperties.MaxTurnAngularVelocity`** (deg/s) — the turn-rate cap used **only** while a boid sprints in from an entry gate or out to an exit point. `0` falls back to `MaxAngularVelocity`, i.e. pre-existing behaviour. Cruising, flocking, hunting and obstacle avoidance are untouched.

Turning circle is `speed / turn rate`, and entry/exit are exactly the two moves that *sprint* — so at the species' ordinary turn rate the arc becomes enormous. The blacktip shark at 22°/s and 22 m/s had a **57 m turning radius, a 114 m circle — wider than the whole 74 m box**, so it physically could not come about inside the tank: it sailed in, overshot, and swung back through frame.

- Resolved by `ResolveTurnCap(boosted, schoolInfo)` in the compute shader, used in two places: `UpdateMovementDirection` (via a new `turnCap` parameter) and the **exit capture radius**, which is sized from the turn rate — leaving that on the old rate would over-estimate the radius and reproduce the documented "shark stopped 42 m short, in full view, and froze" bug with new numbers.
- Angular **acceleration and jerk scale by the same ratio** (`turnCap / maxAngularVelocity`). Raising the ceiling alone would leave the fish still ramping into its turn for the whole entry and arriving before it ever got there — the same trap as raising a speed cap without the acceleration to reach it.
- Current values give every species a turning radius comfortably inside the ~33 m box half-width: shark `70` (57.3 m → 18.0 m), grouper `70` (37.2 → 10.6), ray `70` (28.6 → 9.8), trevally `110` (31.0 → 13.5), mid-tier `110` (~20 → ~9.5), the three 180°/s species `260` (4–8 → 4–5).

> ### ⚠ `BoidSchoolInfo` grew from 20 to 21 floats
> `MaxTurnAngularVelocity` was appended at the **tail** of both `BoidSchoolInfoGPU.cs` and the HLSL `BoidSchoolInfo` in `BoidSimulationData.hlsl`, so every existing field keeps its offset. **The two must always be changed together, field-for-field, and `BoidSchoolInfoGPU.Size` updated to match** (it is the `ComputeBuffer` stride). A mismatch produces *garbage behaviour, not a compile error*.
>
> The struct is no longer a clean multiple of 16 bytes. That is fine for a `StructuredBuffer` stride — `BoidInfo` has been 18 floats for a while — the 16-byte note at the top of the file is a footprint nicety, not a correctness requirement.
>
> `BoidsGPU_Brute.compute` shares the same include, so its struct grows identically and its stride still matches; its own `UpdateMovementDirection` keeps the old signature and simply ignores the new field.

### Swim-out anti-freeze timeout (fixes §10.4)
- **`_exitTimeoutSeconds`** (default **25s**, serialised, in "Removal animation (swim-out)" group).
- Was: `BatchExitRoutine` had a `while(true)` with no timeout — one fish snagged on a reef obstacle never reached its exit radius, so `_exitingCount` never cleared, `IsExiting` stayed true, and that species ignored **Add and Remove for the rest of the session**.
- Now: past the deadline, the routine force-commits the exiting block and clears the count, logging a warning that names the species. The deadline **resets when the exiting block grows** (another Remove joins), so a long removal spree gets the full window.
- Added `OnDisable` cleanup (`_exitingCount.Clear()`) — disabling the component mid-exit killed the coroutine without clearing state, freezing the species on re-enable.

## 7.20 Fish Asset Pipeline (Blender UV1 baking)

**Full spec for prepping fish meshes so they animate correctly under the `Fish_Lit` / `Fish_Swimming_Motion` shader.**

Source assets live **outside the repo** at `C:\Users\Admin\OneDrive\Documents\TP\year 3 sem 1\MP\assest\<species>\` — one folder per species (`.obj` + body/eye PNG textures, some with a `.mtl`). Done in **Blender 5.1** (driven over the Blender MCP).

### Why UV1 is a MATH channel, not a texture unwrap

`Fish_Swimming_Motion.hlsl` reads its tail mask from **TEXCOORD1** (`float2 tailMaskUV : TEXCOORD1`), as `tailMask = saturate(pow(1.0 - tailMaskUV.x, _TailMaskFalloff))`. Each mesh needs a **second UV channel (UV1)** whose **`.x` is a head→tail gradient (tail = 0.0 → head = 1.0)** — mask ≈1 at the tail (full wave), ≈0 at the head (rigid). UV0 (`UVMap`) stays the texture unwrap.

**⚠ Do NOT seam-cut / Unwrap / Reset / Project-From-View UV1.** An island unwrap restarts U at ~0 on every island, so the front of every piece reads as "tail" and the whole fish wobbles.

**Bake it numerically:** for every vertex `UV1.x = (vert.z − z_min)/(z_max − z_min)` (models are oriented length-on-**Z**, head at **+Z**; head end confirmed via the eye-mesh centroid). Eyes = a separate mesh, so normalize their verts **in the body's local-Z range** (then join eyes into body) so they read ~1 and stay rigid at the head.

Keep `UVMap` (UV0) as the texture unwrap + active-render channel. UV0 = texturing, UV1 = swim math. Never swap them.

### Per-model Blender pipeline

1. Import `.obj`.
2. Wire body/eye textures (MTL auto-wires to Base Color; no-MTL wired manually to the Principled BSDF).
3. Bake UV1 on body + eyes (eye verts normalized in **body-local Z space** so eyes stay rigid at the head).
4. **Join eyes into body** (one mesh, two material slots: body + eye).
5. Clear all seams.
6. Save `.blend` next to the `.obj`.
7. Export **mesh-only FBX**: `use_selection`, `object_types={'MESH'}`, `mesh_smooth_type='FACE'`, `path_mode='COPY'`, `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'`, `global_scale=1.0`.

**⚠ Export must be FBX, not OBJ** — OBJ only stores one UV set and silently drops UV1. Blender writes `UVMap`→TEXCOORD0 and `UV1`→TEXCOORD1.

### FBX unit-scale bug — the "big in scene, tiny when instanced" trap ⚠

**The one that cost the most session time.**

- **Symptom:** dragging the FBX into a scene shows it at a normal size, but its Transform reads **scale 100**; when the GPU sim renders it (instanced), it's **tiny**. Other fish are 1-to-1.
- **Cause:** GPU instancing (`RenderMeshIndirect`) draws the **raw mesh** with only per-boid position+rotation — it **ignores the Transform scale**. If the mesh imported tiny with a 100× root, instancing draws the tiny mesh. The tiny+100root happens when the FBX is exported in **metres** (FBX `UnitScaleFactor = 1`) → Unity applies fileScale **0.01** and compensates with root **100**. Working fish are exported in **centimetres** (`UnitScaleFactor = 100`) → fileScale 1, root 1.
- **Check it:** in the `.fbx.meta`, a **working** fish has `bakeAxisConversion: 1` and file-scale (`humanDescription.globalScale`) ≈ **1**; a **broken** one has `bakeAxisConversion: 0` and file-scale **0.01**.
- **Fix (Blender export):** `apply_unit_scale=True` + `apply_scale_options='FBX_SCALE_ALL'`, `global_scale=1.0`. Verify the exported FBX's `UnitScaleFactor == 100` (must match the working fish). Do **not** hand-scale the mesh to "fix" it; that doesn't remove the root-100 and it breaks the swim.

### Mesh scale ↔ swim tuning are coupled

- The swim uses `position.z / _TailWaveLength` (native mesh units) and `sideToSide * 0.01`.
- If mesh's native size changes by ×N, tail wave gets N× tighter and side-to-side gets N× weaker.
- **After any mesh-scale change, scale `_TailWaveLength` (and side-to-side amplitude) by the same factor.**
- Rotation amplitudes (roll/yaw/panning) and `_TailMaskFalloff` are angle/UV-based → scale-invariant, leave them.

### Where the swim values live

- Material floats `_TailWaveLength` / `_TailMaskFalloff` stay on the **instanced material** (the sim's `BoidMaterial`).
- The five *animated* amplitudes are runtime-injected from each species' **`FishMotionRenderProperties`** asset (referenced by `SpeciesDataGPU`) — material values there are overwritten at runtime.
- Map material→SO: `_AutomaticSwimSpeed`→SwimPlaybackSpeed, `_SideToSideAmplitude`→SideToSide, `_YawRotationAmplitude`→Yaw, `_TailRollAmplitude`→Roll, `_TailYawAmplitude`→PanningYaw (each Min=cruise, Max=full-accel).

### Species mesh status (12/12 imported)

| Species | Mesh file | Source |
|---------|-----------|--------|
| Blacktip reef shark | `sharkv2_lowpoly.fbx` | JunHeng (Blender prep) |
| Bluespotted ribbontail ray | `stingray.fbx` | JunHeng — ⚠ tail-sways but pectoral wings don't flap (a tail-swimmer shader can't undulate ray wings); Akil later added `Ray_Wing_Lit_Instanced` shader with the signed turn-rate |
| Reticulated damselfish | `damselfish.fbx` | JunHeng — no MTL, textures wired manually |
| Yellowstripe scad | `YellowstripeScad.fbx` | JunHeng — eye texture wired manually |
| Bullethead parrotfish | `parrotfish.fbx` | akeel-h |
| Eyestripe surgeonfish | `surgeonfish.fbx` | JunHeng + akeel-h |
| Streaked spinefoot | `rabbitfish.fbx` | JunHeng + akeel-h |
| Russell's snapper | (imported) | JunHeng |
| Fringelip mullet | `mullet.fbx` | akeel-h (textures re-exported 2026-07-24, 46KB → 115KB) |
| Brown-marbled grouper | (imported) | — |
| Bluefin trevally | (imported) | — |
| Giant moray | `giant-moray.fbx` | akeel-h (2026-07-24, `d36ee95`) — plus moray-specific `Moray_Lit_Instanced` shader hooks (see §7.6) |

### New-fish checklist

1. Import OBJ → wire body/eye textures.
2. **Bake** UV1 (numeric gradient, not unwrap).
3. Join eyes into body → clear seams.
4. Export FBX with `FBX_SCALE_ALL`, verify `UnitScaleFactor==100`.
5. In Unity, **duplicate a working `*_Instanced.mat`** (e.g. `Clownfish_Instanced.mat`) + swap textures (don't hand-build).
6. Point the spawner's `BoidMesh`/`BoidMaterial` at it.
7. Set swim `_TailWaveLength` for the mesh's true size.
8. Convert tuned amplitudes into the species' `FishMotionRenderProperties`.

## 7.21 Editor Debug Harness

**`EcosystemDebugHarnessGPU.cs`** — in-editor OnGUI add/remove panel with per-species +/− buttons calling the sim's Add/Remove directly. **Test the ecosystem with no tablet, no netcode.** Dev-only test harness.

## 7.22 Splash Screen + Win Condition

- **`SplashSequence.cs`** — auto-advances by `GetActiveScene().buildIndex + 1` (splash = index 0, game scene = index 1). Works for both host and tablet builds; each ships its own scene 1. Removed the `gameScene`/`tabletScene` name fields. `waitForTap = false` for pure auto-advance. (Aloysius also added a black screen-fade overlay to this same script.)
- **`WinCondition.cs`** — the win detector. Latches `Won = true` when networked eco-health ≥ `winThreshold01` (default **0.99**) is held continuously for `holdSeconds` (default **2 s**) — one-frame spikes ignored. Reads `EcosystemNetworkManagerGPU.Instance.GetEcoHealth()`. It's a **local, per-scene** detector (singleton `Instance`, plain C# `Won` — NOT a NetworkVariable); host and tablet each run their own copy but read the same synced health, so they trip within a frame of each other. `WinCondition.Reset()` is called by the reset flow on every device.
- **Two separate win screens** (deliberately different, not copies):
  - **Large screen** — `WinScreen.cs`: full-screen celebration for bystanders. Watches `WinCondition.Instance.Won`, fades in, plays `UISound.Win`, shows a title ("ECOSYSTEM RESTORED"), an Alucia thank-you line (`AluciaLines.Get`) + her win sprite. `f39f7240` restyled it (`Wintext.png`). Its `resetButton` was **removed in-scene** (`3ce58787`) — the field/listener still exist in code but are unwired (the big screen is spectacle; restart belongs on the tablet + host F9).
  - **Tablet** — `TabletWinScreen.cs`: the **debrief**. Title "REEF RESTORED" + the lesson (balance over quantity: predators at ~1 school, prey under caps) + a real-world note. `Compose()` aggregates live populations into "N predators / M prey species" and "X of Y species, all healthy". Its **continue button restarts the exhibit** (`8812a384`): rewired from local `Dismiss` to `RestartExhibit()` → `ExhibitReset.TriggerReset()` (full networked reset), double-fire-guarded, falls back to local `Dismiss()` if no `ExhibitReset` found.
- **Reset integration:** the win is cleared by the host-authoritative reset (F9 hold or tablet Continue → `ExhibitReset` → `WinCondition.Reset()` on all devices), so both win screens hide in lockstep when `Won` clears.
- **`FishSwim.cs`** — title-screen "fishy" animation (title art assets: `bluefo`, `fish`, `seaweed`, `taptosat`, `coral` PNGs).

## 7.23 DualMonitor Support

**`DualMonitor.cs`** — activates Display 2 (Spacedesk / iPad) on startup. Used for the trifold display setup.

## 7.24 Moray Cave Navigation (Akil, `2053e8b9` + `2024bc52`, 2026-07-30/31)

The Giant moray no longer roams — each one claims a cave, swims an authored path into it, freezes head-out with its body coiled into the rock, gapes its jaw, and after a dwell timer relocates to another free cave.

- **`MorayCave.cs`** — marker component on an empty GameObject at a cave mouth. Self-registers into a static `MorayCave.All` (no wiring). Position = neck point, +Z = head-out direction; ordered child GameObjects define the swim-in path (last child = rest spot). Editor gizmos included.
- **`MorayCaveDirector.cs`** — the AI, sits beside the sim (like a camera director; one per scene, auto-finds sim/species/boidSim on Awake, matches the moray species by name containing "moray"). It does NOT touch the compute shader directly: it disables the claimed school's `EcosystemTargetGPU` and drives the target itself (same mechanism as `ParkAt`), running a `Travelling`/`InCave` state machine that steers to the path, locks the head to a kinematic cursor at `_pathSpeed`, and ramps `restWeight` 0→1.
- **GPU/compute hook:** new `EcosystemSimulationGPU.GetSchoolTarget()`; the director calls `BoidSimulationGPU.SetMorayRestAnchors(pos,dir,count)` each frame → compute kernel (`_MorayRestAnchorPos/Count`, `_MorayRestMatchRadius`) + render material via `BoidSpawnerGPU.SetSpineRestData(...)`. In the compute shader a moray whose **target** matches an anchor is `lerp`ed onto it by weight (so it can actually stop — boids can't otherwise slow below cruise), and the reef-penetration backstop is skipped for a pinned moray so it can clip into the rock. `2024bc52` switched matching from position-based to **target-based** (only the one assigned eel pins, never a passing one) and made `BoidSimulationGPU` preserve the moray head-path ring buffer across buffer recreates (else a resting eel snaps to a T-pose).
- **Scene setup:** one `MorayCave` per cave (at the mouth, +Z pointing out, ordered child path points ending deep in the hole) + one `MorayCaveDirector` in the scene. First N morays claim N caves; extras keep roaming until one frees.
- **⚠ Caveats / cleanup:**
  - `_enableRestPose` master switch has an OFF fallback ("if the pose misbehaves" → loiter-only) — the pose was fragile. `EnterCave()` has a `// FUTURE hook` for the curl/mouth animation (with Akil's spine shader).
  - `BoidSimulationGPU.SetMorayAvoidanceOverride()` / `_morayAvoidanceOverride` are now **dead code** (their only caller was removed in `2024bc52`).
  - Mouth-gape + body defaults are hand-tuned to the specific giant-moray mesh (body ~12 units) — **they will break on a different mesh**.
  - Tunables: `_caveDwellSeconds=100`, `_leaveRollInterval=30`, `_leaveChance=0.375`, `_pathSpeed=4`, `_arriveRadius=4.5`, `_restRadius=10`; arrival uses synchronous GPU read-backs (`_centroidPollInterval=0.5s`), with `_travelTimeout`/`_waypointTimeout` so a reef snag can't stall the eel forever.
  - `2024bc52` also committed a stray ~1M-line recovery artifact `Assets/_Recovery/0 (6).unity` — likely unintended and unrelated to the feature (candidate for deletion).

## 7.25 Blacktip Shark Waypoint Patrol (2026-08-18)

The blacktip reef shark no longer roams a random Circle/Rectangle/Line route — it patrols the authored `Waypoints` loop in `SCENE_MainScene`. **Its movement is still pure boiding.** Nothing writes the shark's position or speed; the director only changes *where its swim-target goes*, so the shark still corner-cuts, swings wide, dodges reef via the SDF and obeys its turn-rate limits — along your route instead of a random one.

- **`SharkPatrolDirector.cs`** — sits beside the sim like `MorayCaveDirector`, auto-finds sim + species (name contains "blacktip") on Awake. For each committed school it disables the `EcosystemTargetGPU`'s paired `TransformAnimator` and drives the target itself (same mechanism as `ParkAt`). No compute-shader changes; the only sim hooks used are the existing `GetSchoolTarget` / `CountCommittedGroups` / `TryGetSchoolCentroid`.
- **Path** = the ordered children of `_waypointsRoot` (defaults to the component's own transform), sampled into a closed Catmull-Rom spline with an arclength table, so the target advances at constant m/s regardless of waypoint spacing. Spline rather than polyline because the shark's `MaxAngularVelocity 22°/s` at cruise 2.7 m/s gives a ~7 m minimum turning radius, and the corner at waypoint 4 is ~80° — a raw polyline just gets overshot every lap.
- **Scene setup:** one `SharkPatrolDirector` on the `Waypoints` object. Nothing to wire. Current loop: 8 waypoints, 128.3 m, ~41 s/lap, entirely inside the sim bounds (min clearance 5.1 m — important, a target outside the bounds reads as "exiting" and would drag the school off-screen).
- **On spawn** the shark enters at its normal off-screen `FishEntryPointGPU` marker and beelines to waypoint 0, then rides the loop. **On removal** the director stops driving any school with index ≥ `CountCommittedGroups` (i.e. already parked for exit), so the swim-out is untouched — verified in play mode.
- **⚠ The leash — two non-obvious traps, both hit while building this:**
  - A boid only accelerates above cruising when fleeing a predator or sprinting in/out (`UpdateMovementSpeed`), so a patrolling shark swims at *exactly* 2.7 m/s and **can never catch up**. Its target must be slightly faster than cruise or the shark overtakes and mills around it. So the target permanently outruns the shark; left alone it eventually laps it, and the shark then turns round and cuts across the reef to chase — patrol gone. The leash caps the target at `_maxLeadDistance` **ahead along the path**.
  - Lead **must** be measured as arclength (project the centroid onto the spline), not straight-line distance to the target. A shark faithfully tracing the loop is far from a target half a lap ahead, and a straight-line measure can't distinguish that from a shark that has wandered off sideways. The first version used straight-line and mis-fired constantly.
  - The leash **slows, never stops**, the target (`_leashMinSpeedFactor` is a floor). A stopped target is a fixed point, and a fish that can't swim below cruise can't hover — it circles it at ≥ its turning radius. A stop-and-resume leash keyed on any threshold under ~7 m therefore **deadlocks**: observed live, target held for 40 s while the shark looped around it.
- **Measured equilibrium** (1 shark, defaults): target held ~18 m ahead along the path, shark tracking 4–7 m off the spline, leash oscillating 0.25↔1.0. Stable across laps.
- Tunables: `_speedFactor=1.15` (target speed = cruise × this), `_maxLeadDistance=18`, `_leashMinSpeedFactor=0.25`, `_centroidPollInterval=0.4s` (synchronous GPU read-back — do not poll per-frame), `_samplesPerSegment=20`, `_spawnStaggerMetres=0`.
- **Known:** with `_spawnStaggerMetres=0` every shark starts at waypoint 0, so concurrent sharks travel in near-lockstep (measured 9.2 m apart with 2 sharks). Fine for the 1-shark demo; set it to ~21 (loop length ÷ `MaxSchools` 6) to fan them around the loop.

---

# 8. Prototype Specification

_Reference — full interactive prototype at `prototype/oceanx-prototype.html`. Everything below derived from reading its source code._

## Layout — two panels

| Panel | Description |
|-------|-------------|
| **Left — Coral Reef** | Live reef visual: SVG terrain, layered coral/flora that grows with health, animated fish swimming across, god-rays, depth darkening, murk overlay that fades as health rises |
| **Right — Food Web tablet** | Bright light-blue rounded panel; two views toggled by chevron (⌄): **FOODWEB** (default) and **CURRENT ORGANISMS** |

**Top bar** (spans full width): OceanX logo + "BALANCE THE OCEAN" tagline → Eco-Health label → horizontal health bar (color: low=red, mid=orange, high=cyan) → numeric percentage.

Left reef panel also has a **vertical Eco-Health bar** on its left edge (same colour logic).

## Food Web view

- SVG canvas, nodes arranged by trophic tier (top = Keystone, bottom = Primary).
- Each node is a **glass bubble**: radial-gradient fill, white ring, highlight ellipse, emoji inside, name label below.
- Ring colour = trophic tier (Keystone=cyan, Tertiary=orange, Secondary/Primary=teal/green).
- **Locked nodes**: emoji shown as dark silhouette, name shows "???".
- **Count badge** top-right corner of bubble: cyan normal, red = overpopulated, orange = underpopulated.
- **Over/under glow**: ring turns red/orange + drop-shadow glow when imbalanced.
- **Predator arrows** (edges): hidden by default. **Long-press** a node to reveal arrows TO its predators; all other nodes dim; predators get a cyan highlight ring. Releasing clears the overlay.
- **Tap** (short press) → opens modal.

## Modal — tap a node

**Unlocked species:**
- Left column: large emoji (animates bobbing on first-reveal), common name, **[+ ADD]** button (rounded, cyan glow).
- Right column: trophic tier label (tier colour), Scientific Name, Role description (what it does in the reef), "What's next" contextual hint, count currently in ecosystem + balance status (✓ balanced / ⚠ overpopulated / ⚠ underpopulated).

**Locked species:**
- Left: silhouette emoji + no name shown.
- Right: "Species Missing" label, **progressive hint** (gets more specific each time the player taps — 3 levels: vague → clearer → almost there), requirements checklist:
  - Eco-health ≥ X% (shown if minHealth > 0).
  - Specific prey species count ≥ N (one row per requirement, green ✓ met / red ○ missing).

## Current Organisms view

- Trapezoid "tank" shape with subtle grid overlay.
- Grid of bubbles: species emoji + count badge, over/under colour coding, tap → remove popup (−1 or All).
- Empty state: "No organisms yet — add species from the food web."

## Game flow

1. **Intro screen** (inside the food-web panel): "⚠ REEF STATUS: CRITICAL" badge + Alucia's opening message + "Begin →" button. Disappears when Begin is pressed.
2. **Player taps nodes to Add** species one school at a time.
3. **Eco-health updates** every render frame (based on diversity + ratio scores).
4. **Unlock gate**: locked nodes auto-unlock when prerequisites are met → Alucia announces it.
5. **First-time add**: "New Species Discovered" reveal card floats over the reef panel for 5.5s (emoji, name, sci name, tier badge, description, hint, countdown bar).
6. **Win**: eco-health reaches 100% → Alucia celebrates, sticky win message.
7. **Reset** (↺ button): SpongeBob-style bubble-flood animation wipes the screen, then intro re-appears.

## Alucia — NPC guide

Translucent speech bubble, bottom-left of reef panel, mermaid avatar (🧜‍♀️). Three visual states:
- **Default** (light blue): tips on first add, unlock announcements.
- **Warn** (orange): overpopulation / underpopulation alerts (fires whenever player taps an imbalanced node, and after adding a species that tips the balance).
- **Win** (green): ecosystem fully recovered.

Auto-hides after 5.2s. Sticky for win message.

## Eco-Health formula (from prototype JS)

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

## Unlock prerequisites (prototype — placeholder species names, remap to canonical 12)

| Species (placeholder) | Requires | Min health |
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

> ⚠ **Prototype uses placeholder species names** that do **not** match the now-canonical list (see §4). The prototype's unlock prerequisites and node layout are still valid as *design reference* — but remap every species to the canonical 12 (and drop Manta Ray / Convict Surgeonfish, add Giant moray / the ray / snapper / spinefoot) when porting.

## Reef visual — habitat growth

Ocean background colour interpolates across 8 keyframes from dark murky teal (health 0%) to vivid cyan (health 100%). Flora appears in layers (back/mid/front/tall) with each item having a `minHealth` threshold — items fade in gradually as health crosses their threshold. Murk overlay opacity = `max(0, (70 - health) / 70 × 0.5)`, fully gone above 70%.

## ⚠ Key design difference vs current build

The prototype uses a **locked progression** model: only 2 species available at start; the rest unlock gate-by-gate as the player adds prey first. **✅ Implemented in Unity (2026-06-18)** via `EcosystemUnlockManagerGPU`. Progressive 3-level hints run through `SpeciesBubble.ShowLockedHint`. **Remaining:** the locked-modal requirement checklist UI (the data is already exposed via `EcosystemUnlockManagerGPU.GetLockInfo`).

---

# 9. Things Tried That Didn't Work — Avoid

## 9.1 Water shader + shark = GPU crash (hard native crash)

- **Symptom:** built player crashed at first render frame when the shark GameObject was present and using the URP water shader in view.
- **Diagnosed cause:** the Stylized Water 3 asset's Underwater Renderer Feature (installed as a URP renderer feature) fails on the specific Unity/URP version combo when the scene has too few opaque renderers in view — likely tied to `_CameraOpaqueTexture` / `_CameraDepthTexture` sampling.
- **Confirmed via bisection:**
  - Shark **alone with water shader** → crash.
  - Shark **without** water shader → runs fine.
  - Shark **+ water shader + any other opaque object** in scene → runs fine.
  - Swapping the shark's material to URP Lit didn't help (not the shader).
  - Import setting changes on the shark FBX didn't help (not the mesh).
  - Deleting Library and rebuilding didn't help.
- **Workaround (in place):** commit `b85296d` "fixing Crashing error" in `Boids_Demo.unity`. Also: keeping the Stylized Water 3 Underwater Renderer Feature disabled in some builds, OR ensuring other opaque geometry (coral, seabed) is always in the scene.
- **Not fully root-caused.** Stylized Water 3 v3.2.6 targets Unity 6.0; project runs Unity 6000.3.14f1. Version drift is likely.
- **Proper fix:** update Stylized Water 3 to latest (3.2.7+ has explicit Unity 6.4 compatibility notes), then re-enable the underwater renderer feature and re-test.

## 9.2 CPU ecosystem layer — deleted in Week 7

Full CPU implementation existed (Boid.cs, BoidSimulation.cs, EcosystemSimulation.cs, SpatialPartition3D.cs, EcosystemDefinition.cs, SpeciesDefinition.cs) but was superseded by the GPU pipeline. **Deleted, do not resurrect.** ~40 scripts removed in the cleanup (Week 7 codebase cleanup):
- All CPU Ecosystem scripts.
- Simple Flocking prototype folder.
- CPU networking scripts (`EcosystemNetworkManager`, `TabletEcosystemUI`, `TabletSpeciesCardUI`).
- Old GPU variants (`01 Brute Force Normal`, `02 Brute Force Instanced`).
- Editor-only shader GUI scripts.
- Fish_Swimming_CPU, unused CPU boid variants.
- Duplicate `ComputeShaderExtensions` (GameDevBuddies namespace).

## 9.3 `Fish_Lit_Instanced` on regular MeshRenderers — GPU crash

- **Error:** *"Fish_Lit_Instanced requires a buffer (SRV) _Boids ... none provided"* at draw time.
- **Cause:** `Fish_Lit_Instanced` reads per-boid data from a `StructuredBuffer<Boid> _Boids` SRV that's only bound inside `BoidSpawnerGPU.RenderBoids()` via a `MaterialPropertyBlock` during `Graphics.RenderMeshIndirect`. A plain MeshRenderer never binds it → shader samples an unbound resource → D3D12 crash (or "skipping draw calls to avoid crashing" warning + downstream corruption).
- **Rules:**
  1. `Fish_Lit_Instanced` materials belong **only** on a spawner's `BoidMaterial`, never on a scene MeshRenderer.
  2. Use the non-instanced `Fish_Lit` for scene/hero objects.
  3. The instanced material **must have "Enable GPU Instancing" ON** (`m_EnableInstancingVariants: 1`).
  4. **Do NOT hand-build the instanced material** (swapping the shader on a URP-Lit base leaves it missing passes/props); **duplicate a known-good one** (`Clownfish_Instanced.mat`) and just change its textures.

## 9.4 Instant rewrite of `boidInfo.direction` in reef backstop → "snap" bug

The hard penetration backstop originally rewrote `boidInfo.direction` in a **single frame**, bypassing the normal angular turn ramp → visible up/down snap when fish avoided rocks. Fix: capped-turn helpers (see §7.5). Direct-rewrite approach is off-limits — always cap the turn.

## 9.5 Per-species starvation model — replaced by global ratio model

- Original design had `StarvationDeathRate` + `StarvationThreshold` per species on `SpeciesDataGPU`.
- Original Week 8 also had `ReproductionRate` + `NaturalDeathRate` per species.
- **All four fields deleted.** Population is now driven by:
  - Global ratio dynamics (§7.2) — one rule for all species.
  - Manual add/remove via UI/RPCs.
- If you're tempted to re-add per-species rates, know that they were tried, felt fiddly to tune, and drifted out of sync with the eco-health formula. The global model is unified with eco-health so both agree.

## 9.6 `TMP_Text` fields with legacy `UI.Text` scene bindings — silently fails

Unity **cannot** assign a legacy `UnityEngine.UI.Text` GameObject into a `TMP_Text` component slot. The Inspector slot is stuck at `fileID: 0` — impossible to drag in. Every `if (nameText != null)` fails → placeholder text stays visible. See §7.9 for the specific instance that bit us. Rule: match the field type to what your scene actually has.

## 9.7 Onboarding scripts subscribing only in "not started" branch — misses late-joining tablets

A tablet that joins a session already-started receives `_hasStarted` inside the network spawn payload, which lands **before** `OnNetworkSpawn` wires up `OnValueChanged`. Old code only checked `HasStarted` inside the "just subscribed" branch → `OnStarted` never fires for the late joiner → nudge/tutorial stuck. Fix: always subscribe **and** poll `HasStarted` (see §7.18).

## 9.8 Hardcoded shader `CustomEditor` pointing at GameDevBuddies namespace

Both `Fish_Lit.shader` and `Fish_Lit_Instanced.shader` originally referenced `CustomEditor "GameDevBuddies.FishLitShaderGUI"` — a class from the third-party asset the shader was forked from. Class wasn't in the project → *"Could not create a custom UI for shader"* warning + no inspector foldouts.

**Fix (2026-07-26):** copied the editor scripts from the source project (`Underwater_Fish_Simulation_Unity_6`), renamed namespace `GameDevBuddies` → `OceanX` in all 7 files, updated both shaders' `CustomEditor` lines to `OceanX.FishLitShaderGUI`. Also removed a stray `using Codice.Client.BaseCommands;` (Plastic SCM leftover) that would've broken the build.

## 9.9 `_FORWARD_PLUS` → `_CLUSTER_LIGHT_LOOP` shader keyword swap — crashed player

Attempted to fix the URP 6.1 deprecation warning by renaming the multi_compile keyword. Player crashed on first render. The URP runtime still sets `_FORWARD_PLUS` for draw calls, so the swapped shaders had no matching variant → broken shader submitted → GPU crash. **Reverted.** The deprecation warning is harmless — leave it.

## 9.10 `UnityGBuffer.hlsl` → `GBufferOutput.hlsl` include swap — compile error

URP 6.1 deprecates `UnityGBuffer.hlsl` and suggests `GBufferOutput.hlsl`. Swap breaks the shader — the API changed too (`FragmentOutput` → `GBufferFragOutput`), not just the filename. Fix would require rewriting the entire GBuffer pass. **Reverted.** Deprecation warning is harmless.

## 9.11 Wrong shader on a fish material → fish render stacked at bounds centre

The GPU sim needs each species' `BoidMaterial` on `Fish_Lit_Instanced` (reads per-boid position from `_Boids`). A material on the plain `Fish_Lit` shader draws every instance at one point (the world origin / bounds centre) because the shader doesn't consume the per-instance buffer. Newly-imported fish assets often come in on `Fish_Lit` — **always verify the shader**. (Hit ray/scad/damsel on 2026-07-07; fixed by swapping shader on 3 materials.)

## 9.12 Assigning the instanced material to the wrong shader base

Swapping the shader on a URP-Lit base to create an instanced material leaves it missing passes/props. **Always duplicate a working `*_Instanced.mat` and swap only the textures.**

## 9.13 `BubbleTransition` initially had `bubbleRiseSeconds` too short

Early tuning: bubbles rose so fast the veil looked empty at the peak. Fixed by decoupling `bubbleRiseSeconds` from the veil timing (bubbles fade only when they reach the top by height, not by a global timer).

## 9.14 Modal dim-overlay coroutine on inactive GameObject

**Error:** *"Coroutine couldn't be started because the game object 'DimOverlay' is inactive"*. Moved reset from `Start()` to `Awake()` so it runs before the first `Show()` re-activates the overlay. See §7.10.

## 9.15 Google Sheet CSV: RFC-4180 parse bug — one stray quote wipes a sheet

`CsvUtil.Parse` originally treated `"` as a field-opener **anywhere**, not just at field start. A checker writing `Grows to 3" long` put the parser in quoted mode for the rest of the file → every following row collapsed into one field, those species silently vanished, **no error**. Fixed 2026-07-16: `"` only opens when the field is empty; loud warning if the file ends mid-quote; the previously-silent zero-row parse now logs. Regression-tested: all three sheets parse byte-identically to before.

## 9.16 Per-frame `GetData()` on the spatial-partition buffer — CRITICAL GPU stall

`SpatialPartitionGPU.UpdateGridOccupancy` originally called `_cellOccupancyBuffer.GetData()` **every frame** — a synchronous readback that flushes the command buffer and blocks the main thread. **~1–8 ms/frame in shipping builds** for data consumed only by `OnDrawGizmosSelected` (never fires in a player). Fixed 2026-07-16: readback wrapped in `#if UNITY_EDITOR`, compiled out of builds. Field default flipped to `false`. ⚠ **Still ON in 4 scenes** — untick **Visualize Occupancy** on the `Spatial_Partition_GPU` object.

## 9.17 `SpeciesContentDB.Reload` / `RevealContentDB.Reload` — texture leak

Both clear `_spriteCache` **without destroying** the Sprites or their Texture2Ds. Native objects; GC doesn't free them. Measured on decompressed RGBA32: `SpeciesImages` **39.6 MB** (3 IUCN badges are 6.28 MB each), `RevealImages` 12.4 MB — **~52 MB orphaned per reload cycle**. Not firing today only because `refreshIntervalSeconds` defaults to `0`. **Fix pending:** destroy texture + sprite before `Clear()`; only fire `NotifyReload` when the file actually changed.

A smaller adjacent leak was fixed 2026-07-16: `ContentService.LoadSprite` now destroys the throwaway Texture2D when a PNG fails to decode (was re-leaking on every card open, since misses aren't cached).

## 9.18 Ping-ponging the boid buffer against the sort scratch — sim ran at half rate (`dd609a87`, 2026-08-24)

- **Symptom:** a visible half-rate stutter, worst while fish were entering — which is why it read as "entry jitter" and got chased in the camera code first. It was neither the camera nor the entry logic.
- **Cause:** the two buffers are **not interchangeable**, and the code was alternating between them as if they were:
  - `_boidsComputeBuffer` — the **original-order state buffer**, the actual simulation result.
  - `_sortedBoidsComputeBuffer` — **cell-order scratch**, rewritten from scratch every frame by the grid's `ReArrangeBoids` kernel.
- Swapping them alternately broke both halves at once. On the frames that read the sorted buffer, the sort had **already overwritten the previous frame's simulation result** with a re-sort of stale state — so **every other frame's work was discarded and the fish only advanced once per two frames**.
- **Rule:** simulation state lives in `_boidsComputeBuffer`, full stop. The sorted buffer is scratch — never read it expecting last frame's result, and never treat the pair as a ping-pong. Anything doing a CPU readback of school state (`TryCountBoidsWithinRadius`, `TryGetSchoolCentroid`, `TryRearmEntrySprint`) must address the original-order buffer, which is what `BoidSpawnerGPU.RenderingOffset` indexes into.
- ⚠ Worth remembering as a debugging lesson too: the symptom appeared in the *camera*, the bug was in *buffer management*. Two independent causes were producing one visual, and fixing the camera alone never would have resolved it.

---

# 10. Known Issues / Watchpoints

## Live blockers / trap for the shipping build

- **[HIGH] Tablet can lock on "Connecting…" forever.** `ConnectionScreenUI.Connect` discards `StartClient()`'s bool and waits for a connect/disconnect callback. If the transport fails to start, *neither* fires — and by then `_connecting = true`, discovery is stopped and the button is disabled. No timeout. **Only an app force-quit recovers.** Trivially reachable: `:60` pre-fills the IP field with the **malformed** `"192.168.1."`, and `OnConnect` only rejects empty, not malformed. Attendant waits out discovery → taps Connect on the default → bricked. **Fix:** return `StartClient()`'s bool; on false reset `_connecting`, re-enable the button, restart discovery; validate with `IPAddress.TryParse`; add a ~10s watchdog; stop pre-filling a broken IP.
- **[HIGH] Two schools added in one frame: only the last swims in.** `ReinitializeBuffers` coalesces to one rebuild/frame, but `SetPendingSpawnOrigin` is a **single slot** and `RelocateGroupToEntryPoint` only relocates the **last** sub-group. Double-tap Add (or batched RPCs, or a queue drained after an exit) and the earlier schools **pop into existence mid-tank, on camera**. **Fix:** make the pending origin a list keyed by sub-group; relocate *every* newly added group. ⚠ Touches spawn positioning + position-preservation — the most delicate code in the project.
- **[HIGH] Reef stutters every 5s.** `RunShoalingTick` calls `TryGetBoidsCentroid` → `GetData()` (a full pipeline stall + array alloc) **once per school in the outer loop and again per candidate in the inner loop**. Worst case **~900 blocking readbacks + 900 allocations in one frame, every 5s**. **Fix:** read the whole boid buffer back **once** per tick and compute all centroids from that snapshot. Worth asking: do we actually want school-merging? If not, turning it off is a one-line fix that deletes the whole problem.
- **[HIGH] One unwired species caps eco-health below 100% forever.** `SetupAllSpecies` warns and `continue`s when a species has no spawner — never enters `_schoolCount`. But `ComputeEcoHealth01` still counts it in `totalSpecies`, so **both** `diversity` and `balance` are divided by an unreachable total. One orphan out of 12 caps health at **~92%**. Any `SpeciesData.minHealth` gate above that becomes **unreachable → the exhibit softlocks**. **Fix:** build a list of species that actually have a working spawner in `SetupAllSpecies` and use *that* as the eco-health denominator; raise the missing-spawner warning to `LogError`.
- **[HIGH] `VisionRange` above 8 is silently meaningless.** The neighbour search scans a **3×3×3 cell block** — one cell each way. With `_cellSize: 8`, the guaranteed sensing radius is **8m**, asymmetric (a boid at a cell edge sees 8m one way, 16m the other). **13 of 18 authored vision values exceed 8** (12/14/15/18/22). The shark's 22 gets ~a third of its authored radius. `visionRangeSquared` is checked and dutifully passes fish the grid never handed it. **Blocks Week 11–12 balancing.** Decide before balancing:
  - **(a) Widen search to `ceil(vision / cellSize)` cells** — keeps authored numbers honest, costs GPU. With ~889 boids in 183,000 m³ there is headroom. **Recommended.**
  - (b) Raise `_cellSize` to 22 — collapses grid to ~432 cells, partition becomes near-pointless.
  - (c) Accept 8m as the real cap and re-author assets — cheapest, but shark can no longer see further than a damselfish.
- **[HIGH — latent] `SpeciesContentDB.Reload` / `RevealContentDB.Reload` texture leak.** See §9.17. Set `refreshIntervalSeconds` to 5 min for live sheet updates and you leak ~52 MB every 5 min → Android OOM-kills the tablet mid-exhibit. **Not firing today** only because the interval defaults to 0.

## Scene / build hygiene

- **Scene divergence — three "large screen" scenes historically drifted.** JunHeng had sim, Aloysius forked with health bar, Akil owned environment/lighting. Convergence into JunHeng's `SCENE_MainScene.unity` is partial but not complete. See §11.
- **Build Settings = one-project-two-players toggle** (as of 2026-07-28): index 0 `Aloysius/Scenes/Start scene.unity`, index 1 is EITHER `Junheng/SCENE_MainScene.unity` (host/Windows) OR `Aloysius/Scenes/new netcode 2.unity` (tablet/Android) — enable one, match the Player platform. The stale `SCENE_MainScene 1.unity` reference is gone. ⚠ Easy to ship the wrong scene/platform pair — double-check before each build.
- **`Assets/_Recovery/` — Unity crash-recovery dumps tracked in git.** `0 (3).unity` alone is 8.4 MB; `2024bc52` (2026-07-31) committed another, `0 (6).unity` (~1M lines), alongside the moray work. Not in Build Settings so nothing ships, but Unity imports/parses them every project open and their live script GUIDs pollute every reference grep. → `git rm -r --cached "Assets/_Recovery"`, delete, add to `.gitignore`.
- **Duplicate AudioListener** — multiple cameras in scene, keep exactly one active.
- **`Boids_Simulation_CPU` GameObject in Boids_Demo** — disabled, holds missing script refs to deleted CPU scripts. Safe to delete from scene.

## Dead code / assets confirmed by both C# and script-GUID grep

- **`EcosystemUIAdapterGPU.cs`** — safe to delete.
- **`Networking/HostSpawner.cs`** — zero refs anywhere. Safe to delete.
- **`BoidSimulationGPU.SetMorayAvoidanceOverride()` / `_morayAvoidanceOverride`** — dead since `2024bc52` (its only caller was removed when moray pinning switched to target-based matching). Safe to delete the method + field.
- **`Aloysius/Scripts/UnlockTester.cs`** — safe to delete (Aloysius's keyboard-driven placeholder; `EcosystemUnlockManagerGPU` is the production replacement).
- **`Aloysius/Scripts/Health.cs`** — superseded by `HealthBarBinder`; safe to delete (but check tablet still uses `Health.cs` — actually keep for tablet client).

## Do NOT delete without checking

- **`Automatic_Fish_Swimming_CPU/*.cs`** — no C# refs, but GUIDs are **live in 4 scenes including the enabled build scene**. Deleting = missing-script errors. LOOK at the scene objects first, don't blind-delete.
- **`SpeciesBehaviorPropertiesGPU`** — confirmed never read at runtime, but **not deletable as-is** — 11 `_Behavior.asset` + 10 `_Data.asset` files reference it. Delete the assets and the field together, or leave documented-inert.

## Feature gaps

- **Food web lines broken** — `FoodWebLines.cs` `LineRenderer` edges are present but hidden (`LINE FOOD WEB HIDE`). Marked "wonky, TO BE CHANGED." Predator arrows need a rework before they can be shown.
- **Ecosystem state machine (Healthy/Unstable/Critical/Collapsing/Recovering) not built** — only the 0–1 score. Health-band Alucia reactions cover it for now.
- **Prey do NOT visibly flee predators** — confirmed 2026-07-07. `Behavior` asset (`SpeciesBehaviorPropertiesGPU`) is **dead data** — no runtime code reads `FleeRange`/`HuntWeight`/etc. `PreySpecies`/`PredatorSpecies` only drive **population counts** (the ratio tick), not movement. The compute shader's flee path fires only inside a `Predator`-type affecter's range, and there are **zero** in the scene. **Net:** "remove the shark → prey panic" is only a slow **number** change today, never visible fleeing. See §11 for the fix.
- **⚠ `OceanX/Moray_Lit_Instanced` shader not in the repo yet.** `BoidSpawnerGPU.SetSpineRenderData` and `SpeciesDataGPU.UseSpineDeformation` are wired for it (§7.6), but the shader itself is presumably still on Akil's side.
- **⚠ `minHealth` thresholds need tuning** against the real eco-health curve.
- **NetworkConfig mismatch** — client and host must have identical Network Prefabs Lists. Register `EcosystemNetworkManagerGPU` prefab on the client's NetworkManager.
- **`EcosystemDefinitionGPU.asset` species order** — all 12 species must be added in a fixed, shared order so index-based RPCs match between host and tablet.
- **Species UI fields split across two assets** — unlock config (`startUnlocked`, `minHealth`, `requires`, hints) lives on `SpeciesData` (linked to sim via `gpuSpecies`); `SpeciesDataGPU` stays pure sim data. Remaining UI-only fields (Icon, TrophicTier, FoodWebPosition) still to add to `SpeciesData` for the food-web graph.
- **Add/Remove wiring is a re-link trap when duplicating client scenes** — the decoupled input layer (`BubbleSelectHook` on every bubble + a `TabletAddRemoveUIGPU` with its buttons assigned) lives in the scene, not on a prefab, so a copied/new client scene (e.g. `ALOYLOU VEFR @`) loses it and taps do nothing until it's re-added. If population shows but Add/Remove are dead, check: hooks present on bubbles, controller present + buttons assigned, `TabletEcosystemUIGPU.Ecosystem` points at the **same** `EcosystemDefinitionGPU` (same order) as the host.

## Corrections to older claims

- **"Re-point Build Settings"** — superseded 2026-07-28 (`fdcfc4c3` "android to window"): index 0 `Aloysius/Scenes/Start scene.unity`, index 1 toggles `Junheng/SCENE_MainScene.unity` (host) ↔ `Aloysius/Scenes/new netcode 2.unity` (tablet). `SCENE_MainScene 1.unity` no longer in the list.
- **"Strip debug logging"** — 70 log calls total across Junheng+Aloysius, and **no unconditional per-frame or per-tick logging exists**. `AluciaEcologyEvents.debugLog` and `EnvironmentHealthReveal.logDebug` are already default-off Inspector toggles. The rest are one-shot init/error paths. Recommendation: don't strip, confirm toggles off and ship. ⚠ EXCEPTION: temporary `[Alucia] ResetForNewSession / muted / [ExhibitReset] / [NetMgr] _resetGeneration` diagnostics kept in for reset-flow debugging — STRIP those before final build.
- **Target/animator leak worry is unfounded** — every `CreateTarget` is matched by a `DestroyLastTarget` that destroys both the Target and its paired Animator. Rapid remove is safe.
- **Eco-health does NOT allocate** and its per-frame cost is fine — `_committedScratch` is reused and every enumerator is a struct. (Real latent defect: `BuildCommittedCounts` hands callers a *shared mutable* dictionary — aliasing hazard.)
- **Unlock events are instance events, not static** — no cross-scene-reload subscriber leak. Singleton guard is correct.
- **Two compute-shader bugs previously flagged are already fixed** — cell Z-stride now correctly reads `_CellCountX * _CellCountY`, and cohesion averaging divides by `neighborsCount + 1`.
- **§7.8 "`OnSpeciesFirstIntroduced` … not fired for subsequent adds or automatic population-tick growth"** — was wrong on **both** halves (corrected 2026-08-29, `23c2751c`). 0→1 alone is not a debut: a species driven extinct and added back hit 0→1 again and replayed the reveal card + camera fly-in. And auto-growth calls the same `AddSchool`, so it fired the intro too — that just never surfaced because the population tick is off in every production scene. Now latched per session by `_everIntroduced`.
- **The population tick is DISABLED as shipped** — `_enablePopulationDynamics: 0` in all three production scenes, so §7.2's ratio dynamics are dormant and populations move only on manual Add/Remove (verified 2026-08-29). Eco-health and shoaling both still work; see the banner at the top of §7.2 for why.

---

# 11. What Needs Building Next / To Do

Priority order.

## 1. Preset Scenarios (Week 10) — not started
- **Balanced Ocean** — everything at healthy equilibrium.
- **Shark Removed** — the keystone demo starting state.
- **Overpopulation** — one species runaway.
- **Collapse** — everything crashing.
- **Recovery** — mid-recovery state to demonstrate the reef bouncing back.

## 2. Converge the host scenes into ONE canonical scene
Sim (from JunHeng's `SCENE_MainScene`) + health bar (`HealthBarBinder` already merged) + baked environment (from Akil's `SCENE_MainScene 2`) + Alucia + reveal cards + `ExhibitReset` + `BubbleTransition` should all live in one scene. Alucia + reveal cards now exist in **7 scenes** after all the merges. **Team decision required about whose environment work survives.**

## 3. Build Settings — now a host/tablet toggle (mostly resolved)
Index 0 = `Aloysius/Scenes/Start scene.unity`; index 1 toggles between `Junheng/SCENE_MainScene.unity` (host/Windows) and `Aloysius/Scenes/new netcode 2.unity` (tablet/Android) — the `fdcfc4c3` "android to window" flip. Remaining care: enable the right scene AND set the matching Player platform for each of the two builds; don't ship both index-1 scenes enabled at once.

## 4. Strip temporary debug logging before final build
Specifically the temp diagnostics kept in for the reset-flow work: `[Alucia] ResetForNewSession …`, `[Alucia] Say(…) muted=…` (`AluciaController`), `[ExhibitReset] …`, `[NetMgr] _resetGeneration …`. Keep the general operational logs (see §10 correction).

## 5. Playtest reveal-card hold time (currently 4s)
Bumped from 2.5s → 4s on 2026-07-25 after testers said "too fast". If still too fast at playtest, push to 5–6s (both `SpeciesAddedReveal.holdSeconds` and `SpeciesUnlockReveal.revealHoldSeconds` in `SCENE_MainScene.unity`).

## 6. Bring the `OceanX/Moray_Lit_Instanced` shader into the repo
Referenced by Akil's moray render hooks (§7.6) but not yet committed. Without it, `UseSpineDeformation = true` would fail (unknown shader). Pull from Akil or ship the moray with the standard `Fish_Lit_Instanced` shader if the spine deformation is dropped.

## 7. Per-species collision radius (proper fix for big-body clipping)
Rather than raising `ObstacleAvoidanceRange` per species, add a **per-species collision radius** field used for both the backstop clearance and the avoidance margin. Essential for the long-bodied moray (§7.5).

## 8. Predator-prey **visual** flee (make the keystone demo real)
Currently only population *numbers* react to predators; fish don't visibly flee (§10). Fix: have each predator school emit a **`Predator`-type affecter** (radius from its `Behavior.DetectionRange`), so nearby prey hit the compute shader's existing flee path. This finally consumes the per-species `Behavior` values (FleeRange/FleeWeight/DetectionRange) that were tuned but are currently inert.

## 9. Ecosystem State Machine (Healthy → Unstable → Critical → Collapsing → Recovering)
Derive from `EcoHealth01` value and/or its rate of change; expose for the UI / Alucia warnings. Not urgent — health-band reactions cover it for now.

## 10. Address the CRITICAL/HIGH backlog from §10
- Tablet "Connecting…" watchdog.
- Multi-add spawn origin queue.
- Shoaling tick GPU stall (fix or turn off school-merging).
- Unwired-species eco-health denominator fix.
- `VisionRange` search-cell widening (option a).
- Content-DB texture leak (destroy before `Clear()`).

## 11. Multi-language content (FUTURE — deferred)
Overseas use → translations. **Do NOT build one player per language.** Add a runtime language pick to `ContentService`: one Google-Sheet workbook with a **tab per language** (each a published CSV URL), one build that selects the active language at launch (system-language auto-detect + in-app override). No rebuild to switch or update a language; schema is already localization-ready (stable ids).
- **Implementation:** extend `ContentService` from one URL per file to a `{language → URL}` map + a "current language" setting (~20 lines). Nothing already built needs redoing; today's single URL becomes the "English" entry.
- **Translating** is then just retyping the display text in each tab — no structural changes.

## 12. Repo/scene cleanup (~20 min for most of it)
- Delete `Assets/_Recovery/` (9.9 MB in git).
- Delete confirmed-dead scripts (`EcosystemUIAdapterGPU`, `HostSpawner`, `UnlockTester`).
- Prune dead Build Settings entries.
- Delete `Aloysius/Scripts/Health.cs` **only if** tablet uses `HealthBarBinder` instead (check first — tablet may still need `Health.cs`).

## 13. Final optimisation, balancing, and build (Weeks 11–12)

## 14. Update this HANDOFF regularly
Update the "Last updated" date at the top of this file whenever you edit it, so future readers know how recent the info is.

---

# 12. Reference — Population Dynamics Values

> ⚠ **The per-species rate fields are gone.** `ReproRate` / `NaturalDeath` were deleted in Week 8; `StarvationDeathRate` / `StarvationThreshold` were deleted in Week 9 when the model became a **global, ratio-driven** system.

## Global tuning (on the `EcosystemSimulationGPU` component)

| Constant | Default | Meaning |
|----------|---------|---------|
| `RatioBandLow` | 1 | Below this prey:predator ratio (schools) → prey shrinks / predators starve |
| `RatioBandHigh` | 3 | Above this → prey overpopulates / well-fed predators grow |
| `GrowRate` | 0.3 | Per-tick chance an out-of-band species gains a school |
| `ShrinkRate` | 0.3 | Per-tick chance an out-of-band species loses a school |
| `_tickInterval` | 5s | Seconds between population ticks |
| Eco-health weights | 0.4 / 0.4 / 0.2 | Diversity / balance / apex |
| `_overpopulatedRatio` | 7 | Predators-present: overpopulated if outnumbers combined predators by > this |
| `_overpopulatedFreeCount` | 3 | Predators-gone: overpopulated if > this many schools |
| `_reefBackstopTurnMultiplier` | 3 | Reef-backstop turn rate multiplier (Play-mode tunable) |
| `_tailSwayResponsiveness` | 4 | Ray tail-sway ease (frame-rate-independent) |
| `_entrySpawnJitterRadius` | 4 | Sideways nudge radius for new schools at entry markers |
| `_entrySpawnMinSeparation` | 3 | Preferred gap between new origin and recent ones |
| `_exitTimeoutSeconds` | 25 | Force-commit swim-out after this many seconds |
| `_exitArrivalRadius` | (tunable) | Distance to exit marker that counts as "arrived" |
| `_exitPollInterval` | (tunable) | Seconds between arrival polls |

## Per-species (on each `SpeciesDataGPU` asset)

- **`FishPerSchool`** — fish per school (constant density).
- **`MaxSchools`** — hard cap; carrying capacity = `MaxSchools × FishPerSchool`.
- **`PreySpecies` / `PredatorSpecies`** — **required**: drive both the dynamics and eco-health (empty lists = species doesn't participate).
- **`UseSpineDeformation`** — bool, moray-only. See §7.6.

Tune the global band/rates live in Play mode by watching whether the reef settles or collapses.

## Movement tuning — add/remove feel (each `*_MovementProperties.asset`)

Retuned 2026-08-29 (`5f17204e`, `23c2751c`) because schools took too long to arrive on camera after an Add. The three knobs interact, so change them as a set:

| Field | Value | What it governs |
|---|---|---|
| `MaxSpeed` | 12–26 by species | **Arrival time.** The gate→bounds leg runs pinned at `MaxSpeed` the whole way (jerk `1000` and terminal `MaxAcceleration/WaterFriction` ≈ 44 m/s mean it saturates the cap within a frame or two), so time-to-arrive is simply `distance / MaxSpeed`. |
| `Deceleration` | `100` (was 15) | How fast the boid stops **pushing** — bleeds `acceleration` to 0 in `MaxAcceleration / Deceleration` = 0.2s (was 1.33s). Does not touch cruising or max speed. |
| `MaxTurnAngularVelocity` | 70–260 by species | Entry/exit turn cap only. See §7.19. `0` = fall back to `MaxAngularVelocity`. |

⚠ **`Deceleration` is nearly spent as a knob and `MaxSpeed` has a hard ceiling.**
- Deceleration only bleeds *acceleration*. Once that hits 0 the remaining slowdown is pure water friction (`0.45`, a **2.2s time constant**) and no deceleration value can touch it — a fish dropping 18 → 2 m/s still takes `ln(18/2)/0.45` ≈ 4.9s. Above roughly `1200` it is instant at 60 fps and further increases do literally nothing. **`WaterFriction` is the knob that actually owns that tail** (0.45 → 0.9 halves it), at the cost of making *all* speed changes snappier.
- `MaxSpeed` is bounded by turning circle (`speed / turn rate`) against the ~33 m box half-width — which is exactly what `MaxTurnAngularVelocity` exists to relieve. Push speed up without it and fish overshoot the bounds on entry and swing back through frame.

> **`_simulationSpeedMultiplier` is `0.5` in `Host.unity` and `Trifold.unity`, but `1` in `SCENE_MainScene.unity`.** `BoidSimulationBase` does `timeDelta = Time.deltaTime * _simulationSpeedMultiplier`, so **the two scenes you test in run the whole sim at half speed** — every fish moves at half its authored m/s in real time. The field is declared `[Range(1.0f, 10.0f)]`, so **0.5 is below its own minimum** and cannot be reached with the Inspector slider; it predates the attribute or was set from script. Anyone who nudges that slider snaps it to 1 and silently doubles the entire simulation. Decide whether the 0.5 is deliberate slow-motion for the exhibit and make all three scenes agree.

## 100% eco-health recipe

Grazers at their predator-count: **Parrotfish 4, Surgeonfish 5, Mullet 5, Damselfish 6, Spinefoot 4**; **1 of every hunter** (Shark/Trevally/Ray/Snapper/Scad/Grouper/Moray). Grazers can range up to their caps and stay 100%; the 7 hunters are locked at 1 (each extra predator raises the grazers' floor).

## AluciaEcologyEvents timing

- `startupGrace = 8s`, `settleSeconds = 5s`, `checkInterval = 2s`.
- Lower to ~**3 / 2 / 1** for snappier reactions (though a still-growing species stays silent until it stabilises at its cap, since every count change resets the 5s settle).

---

# 13. Reference — Scene Setup

## Canonical host scene wiring checklist (`SCENE_MainScene.unity`)

Every host scene should have these components + wiring:

- **`EcosystemSimulationGPU`** — the sim itself. On a top-level GameObject.
- **`BoidSimulationGPU`** — reads from the sim.
- **12 spawner GameObjects**, each with a **`BoidSpawnerGPUMultiTargets`** — assigned into `BoidSimulationGPU._gpuBoidSpawners` array **in fixed order** matching `EcosystemDefinitionGPU.Species`.
- **`EcosystemNetworkManagerGPU`** — auto-finds the sim; syncs counts/health/status.
- **`NetworkManager`** (NGO) + **`UnityTransport`** — network prefabs list must match on host + client.
- **`NetworkBootstrap`** — role setup, spawns net-manager.
- **`LanDiscovery`** — UDP broadcast on port 47777.
- **`HealthBarBinder`** — large-screen health bar (host reads `EcoHealth01` direct).
- **`AluciaController`** — wire `simulation` → the `EcosystemSimulationGPU`. `waitForExperienceStart = 1` (waits for networked "tap to begin"); set 0 for standalone testing.
- **`AluciaEcologyEvents`** — wire `alucia` → `AluciaController`, `simulation` → the sim.
- **`ContentService`** — wire all 3 Sources (`alucia_lines`, `SpeciesContent`, `RevealContent`) with their published CSV URLs.
- **`RevealQueue`** + **`SpeciesAddedReveal`** + **`SpeciesUnlockReveal`** + **`NotificationManager`** — big-screen cards + hint queue.
- **`EnvironmentHealthReveal`** — with a Proportional group on the `Corals` parent. ⚠ Corals must NOT be "Batching Static."
- **`FishEntryPointGPU`** markers — one or more Entry / Exit / Both markers placed OUTSIDE the sim bounds.
- **`ReefSDFVolume`** — baked reef SDF for obstacle avoidance.
- **`IntroductionCameraDirectorGPU`** + Cinemachine setup.
- **`ExhibitReset`** + **`BubbleTransition`** — F9 operator reset.
- **`AdaptiveMusicSystem`** + audio mixer setup.
- **`WinCondition`** + **`WinScreen`**.
- **`SplashSequence`** on the splash scene.
- **`DualMonitor`** — activates Display 2 on startup.

## Canonical tablet client scene wiring checklist

- **`NetworkBootstrap`** — client role.
- **`ConnectionScreenUI`** — IP entry + auto-discovery.
- **`TabletEcosystemUIGPU`** on always-active object (e.g. Ecosystem Panel) — species→index lookup. `.Ecosystem` must point at the **same** `EcosystemDefinitionGPU` (same order) as the host.
- **`TabletAddRemoveUIGPU`** on same always-active object — Add/Remove +/− buttons + population label. Assign `addButton` / `removeButton` / `populationLabel`.
- **`ContentService`** — wire `SpeciesContent` source (tablet doesn't need Alucia lines).
- **12 species bubbles** (`SpeciesBubble`), each with:
  - A **`BubbleSelectHook`** component (auto-reads `SpeciesBubble.data.gpuSpecies`).
  - `data.gpuSpecies` linking to the correct `SpeciesDataGPU`.
- **`ModalController`** singleton — species info modal.
- **`SpeciesInfoPanel`** — the "View Details" panel.
- **`CurrentOrganismsGrid`** — Ecosystem tab.
- **`HintsPanel`** — Hints tab.
- **`Health.cs`** — reads networked value; wire `fillImage`.
- **`TabController`** — food web / ecosystem / hints tab switcher.
- **`FoodWebLines`** — predator arrow lines (currently hidden).
- **`TutorialPanel`** — onboarding HOW TO PLAY.
- **`ContextNudge`** GameObjects — the various onboarding hints.
- **`StartCrossfade` / `HideUntilStarted` / `ExperienceStartGate`** — reveal-on-start.
- **`SplashSequence`** + `FishSwim` (title screen).
- **`UISoundManager`** with `Tap.mp3` assigned.

---

# 14. Team Structure

| Role | Person |
|------|--------|
| Simulation / backend + integration | **JunHeng** |
| UI / UX (tablet food-web, modals, Alucia, notifications, adaptive music) | **Aloysius** |
| Scene environment / 3D art (coral, rockwork, fish/ray/moray meshes, lighting bake) | **Akil (akeel-h)** |

Each person has their own Claude session. Share context via this file and `CLAUDE.md` (project root), both committed to git.

Aloysius maintains a parallel **`ALOYSIUS_UI_HANDOFF.md`** at repo root for UI-side detail (his ~28-script suite, food-web/Alucia/notifications work). Read it for anything UI-team-specific.
