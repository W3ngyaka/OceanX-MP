\# OceanX MP — UI/UX Handoff (Aloysius)

\_Last updated: 2026-06-24 (rev 3)\_



> Companion to JunHeng's main handoff. Covers UI/UX work across recent sessions.

> Scope: food-web layout, species info card, atmospheric FX, bug fixes, the

> \*\*Alucia\*\* host guide, the \*\*unlock reveal + hint\*\*, a \*\*disabled-unlock-manager

> bug fix\*\*, and the latest tablet work — \*\*3-tab nav with swipe, eco-health

> dashboard gauge + status word, and a right-side species info panel\*\*.



\---



\## READ FIRST — Things JunHeng needs to know



1\. \*\*The host's `EcosystemUnlockManagerGPU` component was DISABLED.\*\* On the host

&#x20;  `DebugHarness`, the unlock manager had `enabled = false`, so it never ran

&#x20;  `Update()` -> never initialised -> `CheckUnlocks()` always early-returned.

&#x20;  Result: nothing ever unlocked on the host even when requirements were met

&#x20;  (Yellowstripe Scad sat at 2/2 + 2/2 and stayed locked). Fixed in the host

&#x20;  \*\*copy\*\* by enabling it. \*\*Check whether it's also disabled in canonical

&#x20;  `Boids\_Demo` — if so, host-side unlocking is broken there too.\*\*



2\. \*\*`Blacktip Reef Shark` has `startUnlocked = True`\*\* in its `SpeciesData` —

&#x20;  likely a data error (it's the apex with the heaviest requirements). Recommend

&#x20;  setting it to `False`.



3\. \*\*Shared scripts edited\*\* (affect canonical scenes): `SpeciesBubble.cs`

&#x20;  (TapPunch bug fix + OnTap now routes to the side info panel). Commit + mention.



\---



\## Where This Work Lives



| Thing | Scene / Path | Notes |

|-------|--------------|-------|

| Tablet UI (food web, tabs, eco-health, info panel) | \*\*Copy\*\* of `Netcode Simulation Test` (`Assets/Aloysius/`) | Not the canonical scene |

| Alucia + unlock reveal | \*\*Copy\*\* of `Boids\_Demo` (`Assets/Aloysius/Boids\_Demo.unity`) | Host/large-screen copy |

| New scripts | `Assets/Aloysius/Scripts/` | see file list below |

| Edited shared script | `Assets/Aloysius/Scripts/SpeciesBubble.cs` | affects canonical scene |



> Scene-level work is in \*\*copies\*\*; only the \*\*scripts\*\* (shared) touch canonical

> scenes. Integration into JunHeng's scenes is a separate step.



\---



\## Tablet UI — latest session



\### 3-tab navigation with swipe transition (`TabController.cs`)

\- Tab bar (FOOD WEB / ORGANISMS / HINTS) on the Ecosystem Panel, switching between

&#x20; `FoodWebLayer`, `CurrentOrganismsView`, and a new placeholder `HintsView`.

\- \*\*Swipe transition:\*\* outgoing panel slides off one side, incoming slides in from

&#x20; the other (direction depends on tab order), 0.3s smooth. `slideDistance` = panel

&#x20; width (1920).

\- \*\*Old drag-reveal mechanic disabled\*\* (`FoodWebDragReveal` on `arrow` + `DragHandle`)

&#x20; — tabs are now the single switch method.

\- \*\*Tab button colours:\*\* inactive = white (so your own tab art shows true colours,

&#x20; untinted), active = blue tint multiplied over the art. NOTE: tint \*multiplies\*, so

&#x20; it can't make a tab brighter than the source image — for a true glowing-selected

&#x20; look, use a two-sprite swap (separate active art) instead of a tint.

\- Tab art is being done in Photoshop (resting image baked with colour; selected state

&#x20; via tint or swap).



\### Rings now travel with the food web

\- The concentric ring guides were children of `BG` (a sibling that never hid), so they

&#x20; stayed on screen during tab swipes. \*\*Moved them into `FoodWebLayer`\*\* so they

&#x20; hide/show and slide \*with\* the food web.



\### Eco-health dashboard gauge (`EcoHealthDashboard.cs`)

\- Replaced the old `DataChassis` radial widget (which didn't read as a gauge — single

&#x20; flat sprite, partial fill looked identical) with a \*\*"Dashboard001" prefab\*\* that has

&#x20; a real arc fill.

\- Drives the prefab's arc fill Image (`Dashboard01\_Progress`) + percent text directly

&#x20; from eco-health; \*\*disabled the prefab's own `TMLineSliderP`\*\* so it doesn't fight.

\- Reads `EcosystemNetworkManagerGPU.Instance.GetEcoHealth()` (0-1, with 0-100 guard);

&#x20; falls back to a `manualHealth01` Inspector slider when no host (for edit-mode preview).

\- `\[ExecuteAlways]` so it previews in edit mode.

\- \*\*Status word\*\* under the %: THRIVING (>=85) / HEALTHY (>=60) / UNSTABLE (>=35) /

&#x20; CRITICAL (>0) / COLLAPSED (0), recoloured green/amber/red. The status text reference

&#x20; \*\*self-heals\*\* via `AutoWire()` (finds the "StatusText" child if the Inspector slot is

&#x20; cleared) — this was needed because component refs kept getting wiped on recompiles.

\- \*\*Health panel moved OUT of `FoodWebLayer` up to `Ecosystem Panel`\*\* so it stays

&#x20; FIXED during tab swipes and shows on all tabs.



\### Right-side species info panel (`SpeciesInfoPanel.cs`)

\- The `Info` panel on the right: shows \*\*"No Organism Selected"\*\* empty state; on a

&#x20; bubble tap, fills with the species \*\*name\*\*, \*\*tier/role badge\*\*, and \*\*description\*\*

&#x20; (`addedMessage`), plus a \*\*VIEW DETAILS\*\* button.

\- \*\*Flow:\*\* bubble tap -> side panel summary -> "View Details" opens the existing

&#x20; `ModalController` popup (full info + Add/Remove). `SpeciesBubble.OnTap` was updated to

&#x20; route to `SpeciesInfoPanel.Instance.Show(...)` (falls back to opening the modal

&#x20; directly if the panel isn't present).

\- Text-first (no fish image in the panel yet; `speciesImage` slot left null).

\- \*\*Open question (undecided):\*\* whether to keep the two-step (panel -> View Details ->

&#x20; modal) or put Add/Remove directly in the side panel (one step, better for a walk-up

&#x20; exhibit). Not yet decided.

\- \*\*Data note:\*\* 11/12 bubbles have a `cardImage`; \*\*Bullethead Parrotfish has none\*\*,

&#x20; so its View Details button shows but won't open the modal until a card sprite is

&#x20; assigned.



\---



\## Earlier sessions (still current)



\### Food web radial layout

12 bubbles in a radial layout centered on the shark (even angles/radii). Departure from

the prototype's tier-based design — \*\*team to confirm direction.\*\* Halos decorative.



\### Species info card hierarchy fix

Section labels small/muted, answers prominent, body softer. IUCN badge = the one colour

accent.



\### Atmospheric FX (food web)

`SonarPulse.cs` (expanding rings), `MarineSnow.cs` (rising reef bubbles — flipped from

falling snow), `GodRays.cs` (light shafts). Self-contained, behind the bubbles,

raycast-disabled, runtime-generated sprites. Order: GodRays -> SonarPulse -> MarineSnow

\-> Rings -> Linesmanager -> Organisms.



\### SpeciesBubble TapPunch bug fix

Spam-tap unbounded growth fixed: capture `baseScale` once in `Start()`, punch from/to it,

`OnTap` stops/resets running punch. Shared script — affects canonical scene.



\### Alucia — host guide character (`AluciaController.cs`)

2D overlay on the large/host screen (`Boids\_Demo`), octopus-girl per storyboard.

Appears only when speaking (character + bubble fade together), intro sequence on Start,

health-band reactions off `EcosystemSimulationGPU.EcoHealth01` (debounced + hysteresis,

improving vs worsening lines). Placeholder art; three moods tint the bubble.



\### Species unlock reveal + hint (`SpeciesUnlockReveal.cs`)

On first unlock: big "NEW SPECIES DISCOVERED" card (name/sci/tier/message, \~5.5s), then

Alucia hints the closest-to-unlockable locked species (fewest unmet reqs via

`GetLockInfo`). Subscribes to `EcosystemUnlockManagerGPU.OnSpeciesUnlocked`. Confirmed

working via forced-event test.



\---



\## How Unlocking Works (reference)



\- `CheckUnlocks()` unlocks a species when eco-health% >= its `minHealth` AND every

&#x20; `requires` entry met (prey school counts). Latching. Runs only while the manager

&#x20; component is \*\*enabled\*\* (see the bug above).

\- Config per `SpeciesData`: `startUnlocked`, `minHealth`, `requires`, `hint1/2/3`,

&#x20; `addedMessage`, `tier`, `gpuSpecies`.

\- Current data: all `minHealth = 0`, so unlocking depends \*\*only on prey counts\*\* now.

&#x20; Start-unlocked: the 5 grazers + (erroneously) the Shark.



\### Current unlock requirements (from the assets)

| Species | Requires |

|---------|----------|

| Bullethead Parrotfish / Eyestripe Surgeonfish / Fringelip Mullet / Reticulated Damselfish / Streaked Spinefoot | start unlocked |

| Yellowstripe Scad | Damselfish x2, Mullet x2 |

| Russell's Snapper | Damselfish x2, Surgeonfish x2 |

| Bluefin Trevally | Mullet x3, Parrotfish x2 |

| Bluespotted Ray | Parrotfish x2, Spinefoot x2 |

| Brown-Marbled Grouper | Scad x2, Trevally x1, Snapper x1 |

| Giant Moray | Snapper x2, Ray x1 |

| Blacktip Reef Shark | Grouper x1, Moray x1, Trevally x2, Scad x3 — \*\*startUnlocked=True (bug)\*\* |



\---



\## Open / To-Do



\*\*Tablet UI:\*\*

\- Decide: side-panel summary + View Details vs. Add/Remove directly in the side panel.

\- Tab art (Photoshop) — resting image + selected state (tint, or two-sprite swap for glow).

\- Assign a `cardImage` to \*\*Bullethead Parrotfish\*\* (only fish missing one).

\- Badge is plain text in caps — could become a coloured pill by tier.

\- Info-panel description uses `addedMessage`; confirm that's the right field per fish.



\*\*Alucia / reveal:\*\*

\- Real art (placeholder block); reveal card image slot empty.

\- \*\*Lower the "Thriving" win trigger from 100% to \~90%\*\* (health likely tops out <100). Not yet applied.

\- Wire unlock/extinction Alucia reactions.

\- Remove diagnostic Debug.Logs in `SpeciesUnlockReveal` before final (optional).



\*\*For JunHeng:\*\*

\- Re-check the unlock manager is ENABLED in canonical `Boids\_Demo`.

\- Fix Shark `startUnlocked -> False`.

\- Balance pass on requirements.



\*\*Asset note (shark model):\*\*

\- Explored AI 3D generation (Tripo / Meshy) for a stylized blacktip shark. AI topology

&#x20; is poor by default; Meshy's \*\*Remesh\*\* produced clean quad topology. Tripo gates export

&#x20; behind payment; \*\*Meshy free tier allows export but is CC-BY (must credit "Meshy AI")\*\*

&#x20; and free tier uses Meshy 5 (Low Poly Mode is a paid Meshy-6 feature). Free pre-made

&#x20; low-poly sharks on Sketchfab (mostly CC-Attribution) are a viable alternative.



\---



\## Watchpoints / Gotchas



\- \*\*SAVE after runtime-created objects\*\* — objects made via MCP `execute\_code` don't

&#x20; persist unless the scene is saved. Lost the Alucia objects once before a save.

\- \*\*Edit mode for scene edits\*\* — `execute\_code` scene changes fail in Play mode

&#x20; (MarkSceneDirty) and don't persist.

\- \*\*Component refs get wiped on recompile\*\* — bit us on TabController + the eco-health

&#x20; status text. The eco-health panel now self-wires; watch for it elsewhere.

\- \*\*MCP bridge dropped connection several times\*\* mid-edit (recovered). Re-verify edits

&#x20; if something looks half-applied.

\- \*\*Singletons (`...Instance`) are null in edit mode\*\* — diagnostics that read them must

&#x20; run in Play.

\- \*\*Isolated host = empty sim\*\* — population only arrives with the tablet connected.

\- `Boids\_Demo` shark+water shader crash warning still applies (JunHeng's note).



\---



\## Files Touched / Added (`Assets/Aloysius/Scripts/`)



\*\*New:\*\*

\- `SonarPulse.cs`, `MarineSnow.cs`, `GodRays.cs` — atmospheric FX

\- `AluciaController.cs` — host guide (intro + health reactions)

\- `SpeciesUnlockReveal.cs` — unlock reveal card + hint

\- `TabController.cs` — 3-tab nav with swipe

\- `EcoHealthDashboard.cs` — eco-health arc gauge + status word

\- `SpeciesInfoPanel.cs` — right-side species detail panel



\*\*Edited (shared):\*\*

\- `SpeciesBubble.cs` — TapPunch fix + OnTap routes to the info panel



\*\*Scene changes (copies only):\*\*

\- Tablet copy: radial bubbles; atmospheric FX; rings moved into FoodWebLayer; 3-tab bar

&#x20; + HintsView; eco-health Dashboard001 + status word; Health panel moved to Ecosystem

&#x20; Panel level; Info side panel with DetailRoot + View Details

\- `Boids\_Demo` copy: AluciaCanvas (+ character, bubble, reveal card); enabled the

&#x20; `EcosystemUnlockManagerGPU` component on DebugHarness

