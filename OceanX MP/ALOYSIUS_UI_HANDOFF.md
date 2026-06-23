\# OceanX MP — UI/UX Handoff (Aloysius)

\_Last updated: 2026-06-24 (rev 2)\_



> Companion to JunHeng's main handoff. Covers the UI/UX work done this session.

> Scope: food-web tablet layout, species info card, atmospheric effects, a

> bubble-scaling bug fix, the \*\*Alucia\*\* host-screen guide character, the

> \*\*species unlock reveal + hint\*\* feature, and a \*\*disabled-unlock-manager bug\*\*

> found and fixed on the host.



\---



\## READ FIRST — Two things JunHeng needs to know



1\. \*\*The host's `EcosystemUnlockManagerGPU` component was DISABLED.\*\* On the host

&#x20;  `DebugHarness` object, the unlock manager component had `enabled = false`, so it

&#x20;  never ran `Update()` -> never initialised -> `CheckUnlocks()` always early-returned.

&#x20;  Result: \*\*nothing ever unlocked on the host\*\*, even when every requirement was met

&#x20;  (e.g. Yellowstripe Scad sitting at 2/2 + 2/2 stayed locked). Fixed in the host

&#x20;  \*\*copy\*\* by enabling the component. \*\*Check whether it's also disabled in the

&#x20;  canonical `Boids\_Demo` — if so, host-side unlocking is broken there too.\*\*



2\. \*\*`Blacktip Reef Shark` has `startUnlocked = True` in its `SpeciesData`.\*\* This

&#x20;  looks wrong — the shark is the apex keystone with the heaviest requirements

&#x20;  (grouper x1, moray x1, trevally x2, scad x3) yet is set to start unlocked. Likely a

&#x20;  data error. Recommend setting it to `False`.



\---



\## Where This Work Lives



| Thing | Scene / Path | Notes |

|-------|--------------|-------|

| Food web layout + atmospheric FX | \*\*Copy\*\* of `Netcode Simulation Test` (`Assets/Aloysius/`) | Working in a copy, not the canonical scene |

| Alucia + unlock reveal | \*\*Copy\*\* of `Boids\_Demo` -> `Assets/Aloysius/Boids\_Demo.unity` | Host/large-screen scene copy |

| New scripts | `Assets/Aloysius/Scripts/` | `SonarPulse.cs`, `MarineSnow.cs`, `GodRays.cs`, `AluciaController.cs`, `SpeciesUnlockReveal.cs` |

| Edited shared script | `Assets/Aloysius/Scripts/SpeciesBubble.cs` | TapPunch bug fix — \*\*affects canonical scene too\*\* |



> \*\*Scene copies, not the canonical scenes.\*\* Scene-level work is in copies to

> avoid clobbering JunHeng's `.unity` files. The \*\*scripts\*\* are shared and live in

> `Assets/Aloysius/Scripts/`. Integration into the canonical scenes is a separate

> handoff step.



\---



\## What Was Done This Session



\### 1. Food Web layout — radial redesign

\- Reorganized the 12 species bubbles into a \*\*radial layout\*\* centered on the shark,

&#x20; with even angles / consistent radii (exact `anchoredPosition` per bubble), to fix

&#x20; the "all over the place" feedback. Normalized bubble scales. Faint ring guides.

\- This is a \*\*departure from the prototype's tier-based design\*\*; a groupmate

&#x20; pushed back on rings. \*\*Team to confirm direction.\*\*

\- Halo colours are currently decorative (not tier-encoded).



\### 2. Species info card — visual hierarchy fix

\- Section labels made small/muted; answers more prominent; body text regular weight

&#x20; and softer. IUCN badge kept as the one meaningful colour accent.

\- Remaining polish: italic sci-names, shrink/relabel the "+" add button.



\### 3. Atmospheric depth (immersion pass)

Three self-contained background scripts in the food-web scene copy. Each is on its

own object, behind the bubbles, raycast-disabled, generates its own sprite at runtime.



| Script | Effect |

|--------|--------|

| `SonarPulse.cs` | Expanding cyan rings from centre, looping |

| `MarineSnow.cs` | \*\*Rising reef bubbles\*\* (started as falling snow, flipped to rising — snow read wrong for a reef) |

| `GodRays.cs` | Soft light shafts from the top, sway + shimmer |



Render order back->front: `GodRays -> SonarPulse -> MarineSnow -> Linesmanager -> Organisms`.

All expose Inspector tunables; tune to subtle.



\### 4. Bug fix — runaway bubble scaling (`SpeciesBubble.cs`)

Spam-tapping a bubble grew it without bound. `TapPunch()` captured the live

(already-enlarged) scale as "original", so overlapping coroutines compounded.

Fixed: capture true `baseScale` once in `Start()`; punch from/to `baseScale`;

`OnTap` stops any running punch + resets before starting a new one.

Shared script — \*\*affects canonical scene; commit + tell JunHeng.\*\*



\### 5. Alucia — host-screen guide character

2D character overlay on the \*\*large/host screen\*\* (`Boids\_Demo`), separate from the

tablet. Octopus-girl guide per the storyboard; replaces the prototype's mermaid.



\*\*Script:\*\* `AluciaController.cs`

\*\*Scene objects (host copy):\*\* `AluciaCanvas` (Screen Space Overlay, sortingOrder 100)

\-> `AluciaCharacter` (Image + CanvasGroup, \*\*placeholder block — swap real art later\*\*),

`AluciaBubble` (Image + CanvasGroup) -> `BubbleText`.



\*\*Behaviour:\*\*

\- \*\*Appears only when speaking\*\* — character + bubble fade in/out together; hidden

&#x20; otherwise (keeps the reef clean).

\- \*\*Intro sequence\*\* on Start: 3 storyboard lines.

\- \*\*Health reactions:\*\* reads `EcosystemSimulationGPU.EcoHealth01`; speaks on band

&#x20; crossing (Critical <35 / Unstable / Healthy >70 / Thriving), debounced + hysteresis,

&#x20; different lines for improving vs worsening.

\- Three moods (Calm/Warn/Win) tint the bubble; swap for sprite states with real art.



\### 6. Species unlock reveal + next-fish hint (NEW)

On the host screen, when a species unlocks for the first time:

1\. A big \*\*"NEW SPECIES DISCOVERED" reveal card\*\* fades in (name, sci-name, tier,

&#x20;  `addedMessage`), holds \~5.5s, fades out. Image slot present but empty (text-first;

&#x20;  add fish PNG later).

2\. \*\*Alucia then delivers a hint\*\* for the \*\*closest-to-unlockable\*\* locked species

&#x20;  (fewest unmet requirements via `GetLockInfo`), using its `hint1` or an auto-built

&#x20;  requirement summary.



\*\*Script:\*\* `SpeciesUnlockReveal.cs` (on `AluciaCanvas`).

\*\*Wiring:\*\* subscribes to `EcosystemUnlockManagerGPU.Instance.OnSpeciesUnlocked`;

loads all 12 `SpeciesData`; linked to the reveal card UI + `AluciaController`.



\*\*Status: confirmed working\*\* via a forced-event test (reveal card + hint displayed

correctly). It fires on real unlock events once the unlock manager is running (see

the disabled-manager fix above).



\---



\## How Unlocking Actually Works (reference)



\- \*\*Logic:\*\* `EcosystemUnlockManagerGPU.CheckUnlocks()` (runs every `\_checkInterval`).

&#x20; A locked species unlocks when \*\*both\*\*: eco-health% >= its `minHealth`, AND every

&#x20; `requires` entry met (each prey species has >= that many schools live). Latching.

\- \*\*Per-species config:\*\* on each `SpeciesData` asset (`Assets/Aloysius/SpeciesData/`):

&#x20; `startUnlocked`, `minHealth`, `requires` (prey + count), `hint1/2/3`, `addedMessage`.

\- \*\*Current values (read this session):\*\* all `minHealth = 0`, so unlocking currently

&#x20; depends \*\*only on prey counts\*\*, not health. Start-unlocked: the 5 grazers

&#x20; (Parrotfish, Surgeonfish, Mullet, Damselfish, Spinefoot) + (erroneously) the Shark.

\- The tablet bubbles only \*\*read\*\* unlock state (`IsUnlocked`) and show hints on a

&#x20; locked tap; they do \*\*not\*\* unlock — the manager does it automatically.



\### Current unlock requirements (read from the assets)

| Species | minHealth | Requires |

|---------|-----------|----------|

| Bullethead Parrotfish | 0 | start unlocked |

| Eyestripe Surgeonfish | 0 | start unlocked |

| Fringelip Mullet | 0 | start unlocked |

| Reticulated Damselfish | 0 | start unlocked |

| Streaked Spinefoot | 0 | start unlocked |

| Yellowstripe Scad | 0 | Damselfish x2, Mullet x2 |

| Russell's Snapper | 0 | Damselfish x2, Surgeonfish x2 |

| Bluefin Trevally | 0 | Mullet x3, Parrotfish x2 |

| Bluespotted Ray | 0 | Parrotfish x2, Spinefoot x2 |

| Brown-Marbled Grouper | 0 | Scad x2, Trevally x1, Snapper x1 |

| Giant Moray | 0 | Snapper x2, Ray x1 |

| Blacktip Reef Shark | 0 | Grouper x1, Moray x1, Trevally x2, Scad x3 — \*\*but startUnlocked=True (bug)\*\* |



\---



\## What's Still Open



\*\*Alucia / reveal:\*\*

\- \*\*Real art\*\* — Alucia is a placeholder block; reveal card image slot is empty.

&#x20; Drop PNGs in later, no code change. (Fish PNGs exist in `Assets/Aloysius/Fishes/`

&#x20; and `iamge/` but are \*\*inconsistently named\*\* — consider an explicit `revealImage`

&#x20; Sprite field on `SpeciesData` rather than name-matching.)

\- \*\*Win threshold unreachable\*\* — Alucia's "Thriving" line fires at `EcoHealth01 == 100%`.

&#x20; Health likely tops out below 100. \*\*Lower the thriving trigger to \~90%\*\* (+ optional

&#x20; "almost there" \~85%). Identified, not yet applied.

\- \*\*Unlock/extinction Alucia reactions\*\* — not yet wired (only the reveal+hint use the

&#x20; unlock event so far; Alucia could also react to extinctions).

\- \*\*Position/size + TMP\*\* — placeholder layout; bubble uses legacy `Text` (swap to TMP

&#x20; if standardized).

\- \*\*Diagnostic Debug.Logs\*\* still in `SpeciesUnlockReveal` (`\[Reveal] Subscribed...`,

&#x20; `\[Reveal] HandleUnlock...`) — harmless, remove before final if desired.

\- \*\*Bubble-flood reset animation\*\* (storyboard) not built.



\*\*For JunHeng (simulation/integration):\*\*

\- \*\*Re-check the unlock manager is ENABLED\*\* in canonical `Boids\_Demo` (was disabled).

\- \*\*Fix `Blacktip Reef Shark` `startUnlocked` -> False\*\* (likely data error).

\- \*\*Balance pass\*\* on `requires` counts and (if re-enabled) `minHealth` thresholds.



\---



\## Watchpoints / Gotchas



\- \*\*Save the scene after runtime-created objects.\*\* Objects created via the MCP

&#x20; `execute\_code` path do NOT persist unless the scene is saved (Ctrl+S). The Alucia

&#x20; objects were lost once before a save. Always save immediately.

\- \*\*MCP bridge dropped connection a few times\*\* mid-edit (recovered each time). If

&#x20; anything looks half-applied, re-read the script/scene to confirm state.

\- \*\*`Boids\_Demo` crash warning\*\* (shark + water shader, URP) still applies per

&#x20; JunHeng's handoff — the Alucia work is pure UI overlay (unrelated) but the scene is

&#x20; fragile; confirm the copy runs.

\- \*\*Singletons (`...Instance`) are null in edit mode\*\* — only valid during Play.

&#x20; Diagnostics that read `Instance` must run while in Play.

\- \*\*Isolated host = empty sim.\*\* Testing the host alone shows 0 population; population

&#x20; only arrives when the tablet client is connected and adding. Test connected.



\---



\## Files Touched / Added



\*\*New scripts (`Assets/Aloysius/Scripts/`):\*\*

\- `SonarPulse.cs`, `MarineSnow.cs`, `GodRays.cs` — atmospheric background effects

\- `AluciaController.cs` — host guide character (intro + health reactions)

\- `SpeciesUnlockReveal.cs` — unlock reveal card + next-fish hint via Alucia



\*\*Edited (shared):\*\*

\- `SpeciesBubble.cs` — TapPunch runaway-scale bug fix (affects canonical scene)



\*\*Scene changes (in copies only):\*\*

\- Food-web copy: 12 bubbles repositioned (radial); `SonarPulse`/`MarineSnow`/`GodRays`

\- `Boids\_Demo` copy: `AluciaCanvas` (+ `AluciaCharacter`, `AluciaBubble`, `BubbleText`,

&#x20; `SpeciesRevealCard`) with `AluciaController` + `SpeciesUnlockReveal`; \*\*enabled the

&#x20; `EcosystemUnlockManagerGPU` component on `DebugHarness`\*\* (was disabled)



