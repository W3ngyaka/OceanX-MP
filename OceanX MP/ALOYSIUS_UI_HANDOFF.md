\# OceanX MP — UI/UX Handoff (Aloysius)

\_Last updated: 2026-06-24\_



> Companion to JunHeng's main handoff. Covers the UI/UX work done this session.

> Scope: food-web tablet layout, species info card, atmospheric effects, a

> bubble-scaling bug fix, and the new \*\*Alucia\*\* host-screen guide character.



\---



\## Where This Work Lives



| Thing | Scene / Path | Notes |

|-------|--------------|-------|

| Food web layout + atmospheric FX | \*\*Copy\*\* of `Netcode Simulation Test` | Working in a copy, not JunHeng's canonical scene |

| Alucia guide character | \*\*Copy\*\* of `Boids\_Demo` → `Assets/Aloysius/Boids\_Demo.unity` | Host/large-screen scene copy |

| New scripts | `Assets/Aloysius/Scripts/` | `SonarPulse.cs`, `MarineSnow.cs`, `GodRays.cs`, `AluciaController.cs` |

| Edited shared script | `Assets/Aloysius/Scripts/SpeciesBubble.cs` | TapPunch bug fix — \*\*affects JunHeng's scene too\*\* (shared file) |



> ⚠ \*\*Scene copies, not the canonical scenes.\*\* The food-web and Alucia work was

> done in \*\*copies\*\* to avoid clobbering JunHeng's `.unity` files (which merge

> badly in Git). The scripts are shared and live in `Assets/Aloysius/Scripts/`.

> When this is ready, hand the scripts + the placement/FX recipe to JunHeng to

> integrate into the canonical scenes.



\---



\## What Was Done This Session



\### 1. Food Web layout — radial redesign



\*\*Problem:\*\* the original bubble layout was flagged as "all over the place" — the

12 species bubbles had no organizing rule, so the food web read as a scatter and

the long-press connection lines (revealed on hold) would cross chaotically.



\*\*What changed:\*\*

\- Reorganized the 12 bubbles into a \*\*radial layout\*\* centered on the Blacktip

&#x20; Reef Shark (the keystone species).

\- Bubbles placed at \*\*even angles and consistent radii\*\* via exact coordinates

&#x20; (set on each bubble's `RectTransform.anchoredPosition`), measured from the

&#x20; shark's center — so spacing is mathematically even, not eyeballed.

\- Inner ring = mid-tier; outer ring = the rest. Shark enlarged as the focal point.

\- Normalized bubble \*\*scales\*\* (they previously varied 0.83–1.27 for no reason).

\- Soft concentric \*\*ring guides\*\* kept faint (the hard "dartboard" version read

&#x20; as cold/technical; faint rings imply structure without fighting the bubbles).



\*\*Open / for discussion with the team:\*\*

\- The radial layout is a \*\*departure from the prototype's tier-based design\*\*

&#x20; (prototype + JunHeng's handoff describe top=keystone, bottom=primary tiers).

&#x20; A groupmate pushed back on ring layouts. \*\*Team should confirm the direction.\*\*

\- Bubble \*\*halo colours\*\* are currently decorative (harmonious but not encoding

&#x20; anything). To push quality: tie halo colour to trophic tier, or remove them.



\### 2. Species info card — visual hierarchy fix



\*\*Problem:\*\* the card was flagged as "poor" — everything (title, section labels,

answers, body text) was the same bold bright blue, so there was no hierarchy and

nowhere for the eye to land.



\*\*What changed (type treatment, not layout):\*\*

\- Section labels (Scientific Name / Diet / IUCN Status) made \*\*small, muted,

&#x20; lighter weight\*\* so they read as labels.

\- Answer values made more prominent than their labels (the reverse of before).

\- Body description set to \*\*regular weight, smaller, softer colour\*\* so it stops

&#x20; competing with the headers.

\- IUCN status badge kept as the one meaningful colour accent (amber VU / green LC).



\*\*Remaining polish:\*\* italicize scientific names; shrink/relabel the "+" add

button (currently a large ambiguous glowing centre element).



\### 3. Atmospheric depth (immersion pass)



Three self-contained background effects added to the food-web scene copy to make

it feel underwater rather than like a flat diagram. Each is a standalone script

on its own object, behind the bubbles, \*\*raycast-disabled\*\* (never blocks taps),

and \*\*generates its own sprite at runtime\*\* (no art assets needed).



| Script | Effect | Object |

|--------|--------|--------|

| `SonarPulse.cs` | Faint cyan rings expanding outward from centre on a loop (sonar pulse) | `SonarPulse` under FoodWebLayer |

| `MarineSnow.cs` | \*\*Rising reef bubbles\*\* (started as falling "marine snow", flipped to rising bubbles since snow read wrong for a reef) | `MarineSnow` under FoodWebLayer |

| `GodRays.cs` | Soft light shafts from the top, gentle sway + shimmer | `GodRays` under FoodWebLayer |



Render order (back→front): `GodRays → SonarPulse → MarineSnow → Linesmanager → Organisms`.



All effects expose Inspector tunables (count, speed, opacity, colour, etc.).

\*\*Tune to subtle\*\* — they should sit below conscious notice.



\### 4. Bug fix — runaway bubble scaling (`SpeciesBubble.cs`)



\*\*Bug:\*\* spam-tapping a species bubble made it grow without bound.



\*\*Cause:\*\* `TapPunch()` captured `transform.localScale` as its "original" at the

moment it started. Rapid taps launched overlapping coroutines, each reading an

already-enlarged scale as baseline → compounding growth that never reset.



\*\*Fix:\*\*

\- Capture the true resting scale once in `Start()` (`baseScale`).

\- `TapPunch` animates from/to `baseScale` (absolute), not the live scale.

\- `OnTap` stops any running punch and resets to `baseScale` before starting a

&#x20; new one, so coroutines can't stack.



> ⚠ This edits the \*\*shared\*\* `SpeciesBubble.cs`, so it affects JunHeng's canonical

> scene too. It's a clean bug fix (no behaviour change beyond stopping the runaway

> growth), and the `OnTap` edit preserves JunHeng's netcode-index resolution +

> `ModalController.Open` call. \*\*Commit + mention to JunHeng.\*\*



\### 5. Alucia — host-screen guide character (NEW)



A 2D character overlay on the \*\*large/host screen\*\* (`Boids\_Demo`), separate from

the tablet. Replaces the prototype's mermaid speech-bubble; designed as an

octopus-girl guide per the storyboard. Reacts to live ecosystem state.



\*\*Script:\*\* `Assets/Aloysius/Scripts/AluciaController.cs`



\*\*Scene objects (in the `Boids\_Demo` copy):\*\*

\- `AluciaCanvas` (Screen Space Overlay, sortingOrder 100) — holds the controller

&#x20; - `AluciaCharacter` (Image + CanvasGroup) — \*\*placeholder colour block; swap in real art PNG later\*\*

&#x20; - `AluciaBubble` (Image + CanvasGroup) → `BubbleText` (legacy `Text`)



\*\*Behaviour:\*\*

\- \*\*Appears only when speaking\*\* — character + bubble fade in together, fade out

&#x20; together after \~5.2s. Hidden otherwise (keeps the reef view clean).

\- \*\*Intro sequence\*\* on Start: 3 storyboard lines ("Hey, my name's Alucia!" →

&#x20; "...isn't doing too well..." → "Please help me save it!").

\- \*\*Health reactions:\*\* reads `EcosystemSimulationGPU.EcoHealth01` live and speaks

&#x20; when health crosses a band boundary (Critical <35 / Unstable / Healthy >70 /

&#x20; Thriving), with \*\*debounce + hysteresis\*\* so it doesn't spam or flip-flop.

&#x20; Different lines for improving vs. worsening.

\- \*\*Three moods\*\* (Calm / Warn / Win) currently tint the bubble; swap for

&#x20; sprite/animation states when art exists.



\*\*Wiring:\*\* auto-finds `EcosystemSimulationGPU` in the scene (`Boids\_Simulation\_GPU`).



\---



\## What Alucia Still Needs



\- \*\*Real art.\*\* Currently a placeholder block. Needs an illustrated Alucia PNG

&#x20; (ideally a few poses for the moods). Drop into the `AluciaCharacter` Image —

&#x20; no code change needed.

\- \*\*Win threshold is unreachable as written.\*\* The thriving/win line fires at

&#x20; `EcoHealth01 == 100%`, but per JunHeng's handoff the health formula likely

&#x20; \*\*tops out below 100%\*\* (diversity term sits low; needs tuning). \*\*Lower the

&#x20; thriving trigger to \~90%\*\* (and optionally add an "almost there" line \~85%) so

&#x20; the win moment can actually play. \_(Identified but not yet applied.)\_

\- \*\*Unlock / extinction reactions not yet wired.\*\* `EcosystemUnlockManagerGPU`

&#x20; exists on the `DebugHarness` object and exposes `OnSpeciesUnlocked` /

&#x20; `OnUnlockStateChanged`. Hook Alucia to these for unlock announcements and

&#x20; species-extinction alarm lines.

\- \*\*Position/size\*\* of the character is a rough placeholder (bottom-left,

&#x20; 300×520) — tune to the real art and trifold layout.

\- \*\*TMP option:\*\* bubble uses legacy `Text`. If the project standardizes on

&#x20; TextMeshPro, swap for crisper text.

\- \*\*Bubble-flood reset animation\*\* (storyboard) not built — same

&#x20; particle/overlay approach as the FX scripts when wanted.



\---



\## Watchpoints / Gotchas



\- \*\*`Boids\_Demo` crash warning still applies.\*\* Per JunHeng's handoff, shark +

&#x20; water shader (URP opaque-texture) crashes the host scene. The Alucia work is

&#x20; pure UI overlay (unrelated), but the scene itself is fragile — confirm the copy

&#x20; runs before relying on it.

\- \*\*Save the scene after runtime-created objects.\*\* Objects created via the MCP

&#x20; `execute\_code` path do NOT persist unless the scene is saved (Ctrl+S). The

&#x20; Alucia objects were lost once when the editor reloaded before a save — always

&#x20; save immediately after creating scene objects this way.

\- \*\*MCP bridge dropped connection once\*\* mid-edit (recovered). If anything looks

&#x20; half-applied, re-read the script/scene to confirm state.

\- \*\*Scene copies vs canonical.\*\* All scene-level work is in copies. Only the

&#x20; \*\*scripts\*\* (shared, in `Assets/Aloysius/Scripts/`) affect the canonical scenes.

&#x20; Integration into JunHeng's `Netcode Simulation Test` / `Boids\_Demo` is a

&#x20; separate handoff step.



\---



\## Files Touched / Added



\*\*New scripts (`Assets/Aloysius/Scripts/`):\*\*

\- `SonarPulse.cs` — expanding sonar-ring background effect

\- `MarineSnow.cs` — rising reef bubbles background effect

\- `GodRays.cs` — light-shaft background effect

\- `AluciaController.cs` — host-screen guide character (intro + health reactions)



\*\*Edited (shared):\*\*

\- `SpeciesBubble.cs` — TapPunch runaway-scale bug fix (affects canonical scene)



\*\*Scene objects (in copies only):\*\*

\- Food-web copy: repositioned 12 bubbles (radial), `SonarPulse` / `MarineSnow` /

&#x20; `GodRays` objects under FoodWebLayer

\- `Boids\_Demo` copy: `AluciaCanvas` (+ `AluciaCharacter`, `AluciaBubble`,

&#x20; `BubbleText`) with `AluciaController`

