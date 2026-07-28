# OceanX MP — UI/UX Handoff (Aloysius)
_Last updated: 2026-07-28 (rev 9)_

> Companion to JunHeng's main handoff. Covers UI/UX work across recent sessions.
> Scope: food-web layout, species info card, atmospheric FX, bug fixes, the
> **Alucia** host guide, the **unlock reveal + hint**, a **disabled-unlock-manager
> bug fix**, and the tablet work — **3-tab nav with swipe, eco-health
> dashboard gauge + status word, and a right-side species info panel**, plus
> **producer unlock-registration, a new Macroalgae species, and a host
> health-bar binding**.
> **Newest (2026-07-28):** tablet session on a NEW scene,
> `Assets/Aloysius/Scenes/new netcode 2.unity` (see READ FIRST #7 — it is
> **unchecked in Build Settings**). Fixed a **late-join `OnStarted` subscription bug**
> that stopped the tutorial auto-showing, added a **"look up at the big screen" toast**,
> a **visible FFXIV-style Add cooldown**, **milestone-gated hint chaining**, a
> **"HOW TO BALANCE" advisor panel**, and **per-species add/remove numbers on the
> Organisms rows** (host-computed, networked). Also confirmed **100% eco-health IS
> reachable** and captured a **verified winning configuration**. Built and then
> **removed** a per-action health-delta popup (too distracting).
>
> **Previous (2026-07-18b):** **win screen** (WinCondition + WinScreen, Alucia
> thank-you, health hits 100%), **back button replacing swipe-to-close** on the
> modal, and a **hide-until-started gate** (Ecosystem Panel UI hidden until the
> tablet tap-to-start). Confirmed the **action->health narration was already fully
> built** (AluciaEcologyEvents) — no new work needed there.
> **Earlier (2026-07-18a):** tablet overpopulation badge on organism cards,
> organism-card icon source rewritten to `SpeciesBubble.cardImage` (+ new
> "Currentorgnism fish" art set wired), **ambient food-web arrows now survive
> tab switches**, **modal first-tap bug fixed**, health gauge made responsive
> (faster sync + instant number + easing matched to the large screen), a reusable
> **TapPunch** button animation, a **bottom-right unlock toast** on the tablet
> (animated slide+fade, name-only), a **right-side scrollbar** on the organism list,
> **Add-spam handling** (button cooldown + RevealQueue cap/dedupe), and the
> **reveal-card/intro-camera desync fixed** (card now event-driven, not polled).
> Reveal card converted to TMP.
>
> **Previous (2026-07-14):** NEW ARRIVAL card images wired, card text -> TMP,
> host health-bar colour-by-state + an Inspector debug slider, and a
> **sprite-tinting trap** documented (see READ FIRST #6).
>
> **Previous (2026-07-12):** food-web line/arrow routing + energy-flow
> animation, hold-to-reveal progress ring, health-gauge red-when-low, a shared
> popup **RevealQueue** (fixes clashing add/unlock cards), tap SFX, Alucia intro
> gated on the tablet start-tap, **locked species can no longer be added**, the
> shark/predator `startUnlocked` data corrected, and a safe deprecation cleanup.

---

## READ FIRST — Things JunHeng needs to know

1. **The host's `EcosystemUnlockManagerGPU` component was DISABLED.** On the host
   `DebugHarness`, the unlock manager had `enabled = false`, so it never ran
   `Update()` -> never initialised -> `CheckUnlocks()` always early-returned.
   Result: nothing ever unlocked on the host even when requirements were met
   (Yellowstripe Scad sat at 2/2 + 2/2 and stayed locked). Fixed in the host
   **copy** by enabling it. **Check whether it's also disabled in canonical
   `Boids_Demo` — if so, host-side unlocking is broken there too.**

2. ~~**`Blacktip Reef Shark` has `startUnlocked = True`**~~ **RESOLVED (2026-07-12).**
   The whole `startUnlocked` set is now correct: the 7 zero-requirement base species
   (Seagrass, Macroalgae, Parrotfish, Surgeonfish, Mullet, Damselfish, Spinefoot)
   start unlocked; the 7 species that have requirements (Scad, Snapper, Ray, Trevally,
   Grouper, Moray, and the Shark) start **locked**. Verified across all 14 assets.
   NOTE: the earlier "mostly True" state re-appeared briefly mid-session and then
   self-corrected via a git pull — if it regresses again, re-check the assets.

2b. **Locked species could be ADDED (2026-07-12 — now fixed).** The food web drew
   padlocks from `EcosystemUnlockManagerGPU`, but the Add path
   (`TabletAddRemoveUIGPU.OnAdd`) never checked unlock state, so a locked species
   could be selected and added. **Fixed:** added `EcosystemUnlockManagerGPU.IsUnlocked(SpeciesDataGPU)`
   and gated Add on it (blocks the RPC + greys the Add button while a locked species
   is selected). This is a **client-side UI gate**; a host-side authority check on
   `RequestAddSpeciesRpc` would be stronger hardening if desired.

3. **Shared scripts edited** (affect canonical scenes): `SpeciesBubble.cs`
   (TapPunch bug fix + OnTap now routes to the side info panel). Commit + mention.

4. **Producers were locked-by-default because they weren't registered with the
   unlock manager.** `EcosystemUnlockManagerGPU.IsUnlocked()` returns `false` for
   any `SpeciesData` not in its serialized `_allSpecies` list (locked-by-default).
   That list (on the **`Ecosystem Panel`** GameObject) held only the 12 fish, so
   **Seagrass** and **Macroalgae** reported `locked = true` at runtime even though
   their `startUnlocked = true` — the manager overrides `startUnlocked`. A locked
   bubble routes taps to the locked-hint path (no punch, no info panel), which is
   why Seagrass "couldn't be tapped." **Fixed:** both producers added to
   `_allSpecies` (now 14 entries). Same trap applies to any future species.

6. **Tinting only works on a NEUTRAL sprite — a pre-coloured one can't be recoloured.**
   The host health bar's fill sprite (`hhealthtth.png`) had bright green baked into its
   pixels (`#3DF1AB`). Unity tints by **multiplying**, so green sprite x green tint came
   out dark/muddy, and at low health red tint x green sprite would have gone brown — the
   colour-by-state ramp could never look right. **Fixed:** generated a white version
   (`hhealthtth_white.png`, same shape/alpha) and pointed the fill at it with a white base
   colour, so the script's gradient fully drives the colour. `EcoHealthDashboard` already
   warns about this in its own tooltip. **Any future tintable UI art must be white/greyscale.**

5. **Producers have `simIndex = -1`.** `TabletEcosystemUIGPU` can't map the
   producers' `gpuSpecies` to a live sim slot, so the info panel's Add/Remove
   buttons + population count do nothing for Seagrass / Macroalgae (panel still
   opens). To make them sim-interactive, JunHeng must register their GPU species
   in `TabletEcosystemUIGPU`.

7. **(2026-07-28) The scene this session's work lives in is NOT in Build Settings.**
   All of the 2026-07-28 tablet work is in `Assets/Aloysius/Scenes/new netcode 2.unity`.
   In Build Settings only **`Assets/Aloysius/Scenes/Start scene.unity`** and
   **`Assets/Junheng/Scenes/SCENE_MainScene.unity`** are enabled — `new netcode 2` is
   **unchecked**. If the tablet build does not load it some other way, **none of this
   session's scene objects ship**. Confirm before the next showcase. This is on top of
   the existing scene-proliferation problem (`new netcode 1`, `new netcode 2`,
   `new netcode 3`, and four `SCENE_MainScene` copies) — worth a consolidation pass.

8. **(2026-07-28) Rajdhani has no check mark, minus sign, or em dash.**
   `Rajdhani-Medium SDF` and `Rajdhani-Bold SDF` both report `HasCharacter == false` for
   **U+2713 (checkmark)**, **U+2212 (minus)** and **U+2014 (em dash)**, and neither font
   asset has a local fallback (`fallbackFontAssetTable` is empty). A tick rendered as a
   tofu box in the Organisms list until it was swapped for the word `ok`. Em dashes DO
   currently render, which means something in **TMP's global fallback list** is supplying
   them — a thin dependency. **Use ASCII (`-`, `->`) in any new tablet UI string**, or
   generate a Rajdhani font asset with the extra glyphs.

---

## This session (2026-07-28) — hint sequencing, balance advisor, per-species numbers

Tablet scene: **`Assets/Aloysius/Scenes/new netcode 2.unity`** (NEW — previous sessions
used `new netcode 1`). **See READ FIRST #7: this scene is unchecked in Build Settings.**

> **Nothing in this section is runtime-verified.** Every change compiles clean and scene
> wiring was confirmed by inspection, but the host sim is not in this scene, so no play
> test was possible from the tooling side. Aloysius spot-checked several items in a build
> during the session (noted per item).

### Fixed: tutorial never auto-showed on a late-joining tablet (`TutorialPanel.cs`, `ContextNudge.cs`)
- **Symptom:** in a build, the HOW TO PLAY panel did not appear on start; the (?) button
  had to be tapped manually. Worked fine in editor playtests.
- **Cause:** both scripts used this shape —
  `if (net.HasStarted) DoIt(); else net.OnStarted += DoIt;` — subscribing **only** in the
  else branch. `EcosystemNetworkManagerGPU` assigns `Instance` in **`Awake`**, but wires
  `_hasStarted.OnValueChanged` in **`OnNetworkSpawn`**. A tablet joining a session already
  in progress therefore receives `_hasStarted = true` inside the spawn payload, *before*
  the handler exists, so **`OnStarted` never fires for it**. The polling latch had already
  set `_subscribed = true` while `HasStarted` still read its default `false`.
- **Fixed:** always subscribe on first sight of the manager, **and** separately poll
  `HasStarted` (which is what catches the silent late-join sync).
- **Also fixed:** `ResetForNewSession()` claimed it "stays subscribed to OnStarted" — false
  if the `if (HasStarted)` branch had been taken, in which case the hint never returned for
  the next visitor. Re-arm is now **deferred** via a `_rearmPending` flag until `HasStarted`
  is observed back at `false`, so a reset cannot re-trigger the panel off a stale `true`.
- **Reference:** `ExperienceStartGate.cs` had the correct pattern all along (subscribe
  **then** check) — that is why the rest of the tablet UI came up normally.

### Fixed: UI flashed for ~0.6s at session start (`TutorialPanel.IsOpenOrPending` NEW)
- Anything gating on `TutorialPanel.IsOpen` gets a window where `HasStarted` is true but
  the panel has not faded in yet — `autoShowDelay` is 0.6s. The advisor and the look-up
  toast both appeared in that gap.
- **Added `IsOpenOrPending`** (`_open || _autoShowPending`), set the moment `TryAutoShow`
  runs rather than when the panel appears, cleared on show and on reset.
- `BalanceAdvisor` and `LookUpPrompt` gate on this. **`ContextNudge` deliberately still
  uses `IsOpen`** — it is a `while` loop *waiting* for the tutorial to close, where "open"
  is the correct read.

### "Look up at the big screen" toast (`LookUpPrompt.cs` NEW)
- **Problem observed in testing:** visitors add a fish and keep staring at the tablet,
  missing it arriving in the reef.
- Deliberately **not** a `ContextNudge`. A nudge is a one-shot onboarding hint that
  dismisses permanently; this is a recurring event toast that re-fires on every add for
  the whole session. Same `CanvasGroup` fade and the same BG+Text child layout so it reads
  as the same family, but its own lifecycle.
- Repeat adds **restart the hold window** instead of re-fading from zero (re-fading reads
  as a flicker when tapping quickly). `holdDuration` 2.5s, `fadeDuration` 0.35s.
- Suppressed while the tutorial is open/pending; dropped instantly on reset.
- **Scene:** `LookUpPrompt` on `TabletCanvas (1)` at y=-940, cloned from `TapFishNudge` so
  the styling matches. Copy is a placeholder: *"Look up! Your fish is joining the reef"*.
- **Hook:** one line in `TabletAddRemoveUIGPU.OnAdd()` after `RequestAddSpeciesRpc`, so it
  only fires on adds that actually pass the cooldown / cap / unlock checks.

### Visible Add cooldown, FFXIV-style (`ButtonCooldownOverlay.cs` NEW)
- The existing `addCooldown` was a **silent** guard: during it the button stayed fully lit
  and simply ate the press, which read as "the button is broken" rather than "not ready".
- **`TabletAddRemoveUIGPU`:** exposed `CooldownRemaining` / `CooldownDuration`, and folded
  cooldown into `addButton.interactable` so the button greys out through its existing
  ColorTint transition.
- **`ButtonCooldownOverlay`:** a dark radial wipe over the icon that unwinds clockwise from
  the top as the cooldown recovers. Read-only against the source above, so the visual and
  the gate cannot drift apart. `raycastTarget` off.
- **Scene:** `CooldownSweep` child under `Ecosystem Panel/Info/Add`, using the Add sprite
  itself as the fill so the wipe follows the icon silhouette. Editor properties pre-set to
  match what `Awake` does, so the scene view previews correctly instead of showing a white
  blob.
- **OPEN:** the source default was raised 0.3 -> 1.0 but **the serialized instance is still
  0.3** (same serialized-field trap as `_populationSyncInterval` in rev 8). At 0.3s the
  sweep is a blink. Set it on the `Ecosystem Panel` instance.

### Hint chaining now waits for a real milestone (`ContextNudge.Advance` NEW, `ModalController`, `SpeciesInfoPanel`)
- **Goal:** the hold-for-food-web hint should appear only after the visitor has opened a
  species' View Details modal **and closed it** — read the info first, then learn the hold
  gesture.
- Previously `showAfterId` only unlocked when the named nudge was **dismissed**, and the
  tap nudge dismisses on tap, so the hold hint fired while the info panel was still open.
- **`ContextNudge.Advance(gateId)`** decouples unlocking from dismissal. `Dismiss()` calls
  it internally so existing nudge-to-nudge chaining is unchanged, but any milestone can now
  release a gate.
- **`ModalController.Close()`** calls `Advance("details")`. That is the single close funnel
  (`ModalCloseButton` + `SwipeToClose` are its only callers, and no reset path calls it), so
  it cannot fire spuriously.
- **Scene:** `HoldFishNudge.showAfterId` changed `tap` -> `details`.
- **Safety net (`SpeciesInfoPanel.detailsHintFallbackSeconds`, default 8):** a visitor who
  selects a fish but never taps View Details would otherwise never discover the food web at
  all. The clock starts on the **first** species selection and is **not** restarted by
  further bubble taps (otherwise browsing bubbles postpones the hint forever). Tapping View
  Details cancels it; the real close path takes over. `0` disables it.
- **`Advance` also guards on `_rearmPending`** so a fallback timer still pending when the
  exhibit resets cannot fade a hint in over the attract screen.

### "HOW TO BALANCE" advisor panel (`BalanceAdvisor.cs` NEW) — went through five revisions
Bottom-left panel on `TabletCanvas (1)` (620x240 at 48,48, reusing the nudge `button2`
background and Rajdhani). Non-interactive so it cannot eat taps meant for the reef.

**The one architectural decision worth preserving:** every line is derived from the
**simulation's own verdict** — `GetSpeciesStatus`, already synced per species — never from
a second opinion computed on the tablet. Eco-health is a weighted mix of diversity,
per-species balance and apex presence; a panel that recomputed any of that itself would
eventually disagree with the bar and send visitors chasing fixes that do not move it.

Behaviour after the five revisions (each driven by showcase/playtest feedback):
- **Grouped, not enumerated.** Problems collapse by kind, so nine missing species is one
  line, not nine. Ranked so an active collapse outranks an empty reef: starving ->
  over-hunted -> overcrowded -> missing apex -> merely sparse. `maxLines` default 2.
- **Escalating detail.** Opens vague and only sharpens when the visitor is genuinely stuck,
  measured as eco-health failing to reach a **new high** for `escalateAfter` (25s) seconds.
  Any improvement resets to vague, so someone making progress is never handed the answer.
  The stuck clock is **frozen while the tutorial is open** (reading instructions is not
  failing to balance the reef) and **reset on the attract screen** so each visitor starts
  vague.
- **`maxDetailLevel` = 1 (ceiling).** Level 0 describes the problem, 1 also names the
  species, 2 would also state the fix. **Capped below 2 on purpose so the HINTS tab remains
  the only place that tells the visitor what to press** — otherwise the panel makes that tab
  redundant.
- **`reportMissingSpecies` = false.** It no longer mentions mere absence at all: the health
  card already shows "n / 12 species present" and the Organisms rows now carry per-species
  numbers. The panel earns its space on the **dynamics** — starving / over-hunted /
  overcrowded — which nothing else on the tablet explains.
- **Prose, not bullets.** `Line()` returns fragments with no terminal punctuation so they
  stay composable; an `AsSentence()` helper closes each one when rendered.
  `eachSentenceOnNewLine` (default off) stacks them instead of running them together.

**Fixed mid-session: it claimed "balanced" when it was not.** It inferred balance from
having no suggestions, but absent-and-locked species are deliberately skipped (the visitor
cannot add them) while `ComputeEcoHealth01` still counts them against diversity. It now
checks the health value against `balancedThreshold` (0.99) and otherwise shows
`nothingToDoMessage`.

**Also fixed: the quiet message pointed at the wrong tab.** It said "check the Organisms
list for gaps" — but organism cards `SetActive(false)` at zero population, so that list
*structurally cannot* show what is missing. Now points at the Food Web tab.

### Per-species add/remove numbers on the Organisms rows (host-computed, networked)
**This is the direct answer to the showcase feedback** that visitors could not tell which
species needed what amount.

- **`EcosystemSimulationGPU.GetSpeciesDelta(species)` NEW** — signed count change that
  would clear **that species' own** unhealthy state. Checks run in the **same order as
  `GetSpeciesStatus`** so the number can never contradict the badge.
  - `Absent` -> `+1`
  - `OverPredated` -> `ceil(_ratioBandLow * predN) - n`, **clamped to `MaxSchools`**
  - `Overpopulated` -> trim under `_overpopulatedRatio * predN`, or under
    `_overpopulatedFreeCount` if its predators are gone
  - `Starving` -> **0**. The fix is more prey, not fewer of itself, and the row only has a
    `-1` button. Mathematically you *can* clear starvation by removing the starving fish
    (fewer mouths), but that teaches the wrong lesson.
  - Lives in the sim because `_ratioBandLow` / `_overpopulatedRatio` /
    `_overpopulatedFreeCount` are private to that class — the tablet cannot do the
    arithmetic itself.
- **`EcosystemNetworkManagerGPU`** — new `NetworkList<int> _speciesDelta`, following
  `_speciesStatus` exactly (constructed in `Awake`, seeded in the spawn loop, written in
  `SyncPopulations`), plus `GetSpeciesDelta(int)`.
- **`OrganismCardData.UpdateDelta()`** — renders `+2` (blue), `-3` (red), `ok` (green) on
  the existing 0.2s poll. `needsFoodLabel` ("needs food") for `Starving`.
- **Scene/prefab:** new `DeltaLabel` child on `Assets/Aloysius/Prefabs/OrganismCard.prefab`
  at x=830, Rajdhani-Medium 30 to match the name label.
- **All additions are marked `BALANCE DELTA`** in all three scripts. To revert: grep for
  that marker, delete the marked blocks, delete `DeltaLabel` from the prefab. The advisor
  panel has no dependency on any of it.

**Fixed mid-session: the delta ignored the school cap.** It asked for `+2` damselfish when
they were already at `MaxSchools = 10`. It now measures remaining room
(`GetMaxSchools(species) - n`) and only reports a positive number if the increase actually
fits; at the cap it returns 0 and the row shows **`huntedOutLabel`** ("too many hunters")
rather than falling through to `ok`, which would have been worse than the wrong number.
**This is the exhibit's most interesting ecological idea** — at the cap the only remaining
lever is *thinning the predators to save the prey* — and nothing else on the tablet hints
at it.

### Built and then REMOVED: per-action health-delta popup
- `HealthChangeFeedback.cs` showed the eco-health change each action caused (`+4%` / `-2%`)
  floating off the gauge, with bursts sharing one baseline so four quick adds gave one
  combined total rather than four numbers fighting.
- **Removed the same session — too distracting.** A number firing on *every* press is
  noise, not feedback. Script deleted, `HealthDelta` object deleted, all three hooks
  (`OnAdd`, `OnTapRemoveOne`, `ExhibitReset`) stripped, zero lingering references.
- **If revisited:** the version that plausibly works only speaks when something *notable*
  happens — crossing into THRIVING, or an action that made things measurably worse — rather
  than annotating every tap.

### Reference findings (2026-07-28)
- **100% IS reachable — confirmed.** The runtime roster is `Assets/Junheng/Data/EcosystemDefinitionGPU.asset`
  with **12 species**, and **every one has food-web links**. The three assets with neither
  predators nor prey — **Clownfish, Macroalgae, Seagrass** — are **not** in that definition.
  This matters because `ComputeEcoHealth01` only counts a species toward `healthy` if
  `HasAny(PredatorSpecies) || HasAny(PreySpecies)`, while dividing `balance` by
  **`totalSpecies`**. Any link-less species in the roster would cap `balance` below 1 and
  make 100% impossible. **Do not add Clownfish / Seagrass / Macroalgae to the 12-species
  definition without giving them food-web links first.**
- **A verified 100% configuration** (read off the F1 debug overlay during the session):

  | | Species | Schools |
  |---|---|---|
  | Apex | Blacktip reef shark | 1 |
  | Meso | Bluefin trevally, Bluespotted ribbontail ray, Brown-marbled grouper, Giant moray, Russell's snapper, Yellowstripe scad | 1 each |
  | Prey | Bullethead parrotfish 5, Eyestripe surgeonfish 5, Fringelip mullet 5, Reticulated damselfish 6, Streaked spinefoot 5 | |

- **The shape of that solution is the design story.** Every predator at 1; prey at 5-6; and
  **nothing anywhere near its cap** (prey sit at 5-6 of 8-10, predators at 1 of 3-6).
  **You win by restraint, not accumulation** — and the tablet currently teaches the
  opposite. There is an Add button, counts read as `n/10` like a progress bar, "11 / 12
  species present" implies more is better, and species unlock as rewards for adding.
  Nothing anywhere says *predators must stay scarce*. That is the most likely single
  explanation for the showcase difficulty.
- **`MaxSchools` on the asset does not match the live cap for predators.** The debug overlay
  showed shark `1/3` against an asset `MaxSchools` of 6; trevally `1/3` against 8; grouper
  and moray `1/3` against 6. Prey matched (damselfish `6/10`, asset 10). So the live cap
  looks **dynamic** for predators, probably prey-dependent carrying capacity.
  `GetSpeciesDelta` calls `GetMaxSchools()` — the same function the Add button greys off —
  so it follows the live value either way, but **JunHeng should confirm which number is
  authoritative**.
- **Unresolved:** with rev 8's documented `_ratioBandLow = 1`, a snapper at 1 school with 3
  predators present at 1 each gives `n/predN = 0.33 < 1`, which should classify it
  `OverPredated` and make the verified 100% configuration impossible. Either the tuning has
  changed since rev 8, `SumCounts` does not mean what is assumed here, or the overlay's
  100% was a rounded 99.5%+. **`GetSpeciesDelta`'s arithmetic assumes that rule**, so this
  is worth pinning down — open the host scene and read the live serialized values.

### Performance note
`GetSpeciesDelta` calls `BuildCommittedCounts()` on **every** invocation, so at 12 species
and a 0.15s sync interval the dictionary is rebuilt 12x per tick. `GetSpeciesStatus` has
the same pattern (pre-existing), so this **doubles** it. Not observed as a problem, but if
it shows up in the profiler, build the counts once in `SyncPopulations` and pass them in.

### Ideas considered and NOT built (2026-07-28)
- **Route balance advice through Alucia.** Rejected: `AluciaController` has **zero
  instances in `new netcode 2`** — she lives in the host scene, i.e. the screen visitors are
  *not* looking at. Guidance has to stay on the tablet. Worth a team discussion though: the
  tutorial promises "Alucia will guide you along the way", but if she only exists on the
  large screen she is atmosphere for bystanders rather than guidance for the operator.
- **Pressure on the food-web edges — the strongest unbuilt idea.** Bubble colour = status,
  ring/size = population, and **edge appearance = the ratio across that link** (a starving
  predator's link to its prey goes thin/red; an overcrowded species' link to its absent
  predators goes dashed). "Which species needs what amount" becomes a picture instead of a
  sentence, requires no reading, teaches the actual mental model, and finally gives the
  hold-to-reveal food web a consequence. The Food Web tab is currently a browser; this
  would make it the balance instrument.
- **Show absent species in the Organisms list** (greyed, count 0, delta `+1`) instead of
  hiding them. Small change — `CurrentOrganismsGrid.Refresh()` plus removing the
  `SetActive(false)` in `OrganismCardData.Update()`. Would fix the "everything says ok but
  I'm at 93%" confusion, and would finally display the `+1` deltas that are already
  computed and synced for absent species but shown to nobody. Changes what the tab *means*
  (from "my reef" to "the roster and its state") — deliberately left as Aloysius's call.
- **`ContentSizeFitter` on the advisor panel** so 620x240 shrinks to fit one short sentence
  instead of sitting mostly empty.
- **Move `TutorialPanel` to the last sibling index.** Everything added this session landed
  above it in `TabletCanvas (1)` (TutorialPanel is index 5; TapFishNudge 7, HoldFishNudge 8,
  LookUpPrompt 9, BalanceAdvisor 10), so each one needs its own suppression gate. Reordering
  once would make those gates a nicety rather than the only thing holding it together.

---

## Previous session (2026-07-18b) — win screen, back button, hide-until-started

Tablet scene: `Assets/Aloysius/new netcode 1.unity`. Win screen also placed in the
host scene copy being used (`Assets/Aloysius/Scenes/SCENE_MainScene 1.unity` — a
duplicate of JH's host scene). NOTE: there are now MULTIPLE `SCENE_MainScene` copies
(Akil / Aloysius / Junheng / a `SCENE_MainScene 1`) — confirm which is the real build.

### Win screen (`WinCondition.cs` NEW, `WinScreen.cs` NEW)
- **Win = eco-health holds >= 99% for 2s** (not a one-frame touch of 100%). Tunable.
- `WinCondition` (plain MonoBehaviour, one per scene) reads `GetEcoHealth()` and latches
  `Won`. Deliberately NOT a NetworkBehaviour — the runtime manager spawns from a prefab and
  isn't in the scene at edit time, so a scene-placed NetworkBehaviour couldn't attach to it.
  Both screens run their own copy off the same networked health value, so they trigger together.
- `WinScreen` (per scene) watches `WinCondition.Won`, fades in a full-screen overlay: Alucia
  portrait (thanking the player) + title ("ECOSYSTEM RESTORED") + thank-you message +
  **PLAY AGAIN** reset button. Hidden by default, unscaled-time fade.
- Built into the host scene on `Canvas (1)` (dim, Alucia, title, message, reset button, all
  wired). Uses Alucia's `calmSprite` as portrait — **no dedicated happy Alucia sprite yet**;
  assign `aluciaWinSprite` when available.
- **OPEN:** (1) only built in the host scene so far — **needs a copy in the tablet scene** if
  you want it on both. (2) **Reset is local + hide-only** — "Play Again" clears the local
  `Won` but does NOT reset the ecosystem; if health is still >=99% it re-triggers. Wire it to
  an actual sim reset later. (3) The exact per-species counts for 100% are a *balance* condition
  (each species alive, apex alive, and every predator/prey ratio inside 1x..7x), not fixed
  numbers — see the health-formula notes below.

### Back button replaces swipe-to-close (`ModalController` modal, `SwipeToClose.cs`)
- Users were confused they had to swipe up to close the species modal. **Removed the
  `SwipeToClose` component** from `ModalPanel`; added a **X back button** (top-right of the
  panel) wired to `ModalController.Close()`, with `TapPunch` for feel.
- `SwipeToClose.cs` script still exists in the project (unused) — left in case the gesture is
  wanted elsewhere; safe to delete.

### Hide Ecosystem Panel until tablet tap-to-start (`HideUntilStarted.cs` NEW, `TabController.cs` gated)
- Goal: the whole tablet Ecosystem Panel UI stays hidden on the title/start screen and appears
  only when the tablet's "tap to start" flips the shared networked `HasStarted` flag.
- **Key gotcha:** the panel ROOT carries logic (`TabController`, `EcosystemUnlockManagerGPU`,
  `TabletEcosystemUIGPU`, `TabletAddRemoveUIGPU`) that must keep running — so you CANNOT hide
  the root. Solution hides only the **visual children** and keeps the root active.
- `HideUntilStarted` (on a new always-active sibling `PanelStartGate`) hides a LIST of visual
  children on Awake (`prompt`, `panel`, `FoodWebLayer`, `TabBar`, `Health`, `Info`) and re-shows
  them on `OnStarted` (handles late-join via `HasStarted`). The gate object stays active so its
  Update can listen — a self-hiding object can't un-hide itself (that was the first failed
  attempt).
- **`TabController.Start()` was ALSO gated** — it used to force-show the default (Food Web) tab
  + prompt on Start, overriding the hide. Split its body into `InitializeTabs()` that runs only
  after `HasStarted` (waits on `OnStarted`, or a coroutine if the manager hasn't spawned yet).
  This is JunHeng's script — flagged below.
- Confirmed working end to end (hides pre-start, reveals on tap).

### Action -> health narration — ALREADY BUILT (no new work)
- The rubric item "narrate how actions affect ecosystem health" is already fully implemented:
  - `AluciaController.EvaluateHealth()` narrates overall health across bands
    (critical/unstable-up/unstable-down/healthy/thriving), direction-aware.
  - `AluciaEcologyEvents.cs` (JunHeng's) polls `GetSpeciesStatus()` every 2s and speaks
    per-species cause lines (Starving / OverPredated / Overpopulated / extinct / added), with
    per-species cooldown + a grace delay so fresh adds aren't instantly flagged. Rich CSV
    content (generic + per-species) names the exact corrective action.
- **Only thing to verify:** `AluciaEcologyEvents` is actually ON an active object in the build
  scene (it's a script that must be placed). If it's missing from the scene you build, the
  per-species narration is silent.

### Eco-health formula (reference — for tuning the win / balance)
- `health = (0.4*diversity + 0.4*balance + 0.2*apex)`; 100% needs ALL three = 1:
  diversity=1 (all species alive), apex=1 (Blacktip shark alive), balance=1 (every species
  alive, food-web-connected, not declining, not overpopulated).
- Per species with predators: `predatorSchools <= ownSchools <= 7 * predatorSchools`
  (`_ratioBandLow = 1`, `_overpopulatedRatio = 7`), and each predator needs prey >= its own.
  So 100% is a whole-chain balance, not fixed counts — and the sim actively grows/shrinks
  populations (`_growRate = 0.3`), so hand-set counts drift.

---

## Previous session (2026-07-18a) — organism cards, tablet fixes, health responsiveness

Tablet scene this session: `Assets/Aloysius/new netcode 1.unity`. Reveal-card TMP
work touched the host scene (`SCENE_MainScene`) via shared scripts.

### Overpopulation badge on organism cards (`OrganismCardData.cs`, `OrganismCard.prefab`)
- Overpopulation *detection* already existed on the food-web bubbles
  (`SpeciesBubble.UpdateOverpopulation`: `pop >= GetMaxSchools(index)`), but the
  Current Organisms **list cards** never surfaced it.
- **Added** an `overpopBadge` field + `UpdateOverpop()` to `OrganismCardData` using the
  **identical** check (`pop >= net.GetMaxSchools(index)`, both values host-synced), polled
  on the same cadence as the count and baselined off in `Setup()` so a recycled card can't
  show stale state. Flags **only AT/over cap** (no near-cap warning).
- **Prefab:** new `OverpopBadge` child (amber rounded pill, bold "! OVER",
  `raycastTarget` off), hidden by default, wired to the field. Anchored left of the name.
- New look (not the food-web bubble's `Overpopulated` sprite) — uses Unity's built-in
  `UISprite`. Swap for custom art later if desired.

### Organism-card icon source rewritten (`CurrentOrganismsGrid.cs`) + new art set
- **Root cause of wrong/misplaced card icons:** the grid **scavenged** each bubble's child
  hierarchy for "the first child Image with a sprite that isn't a lock/overpop/glow/ring
  overlay." Fragile and name-dependent (child objects had mismatched names like a "Grouper"
  object holding a mullet sprite).
- **Fixed:** grid now reads the icon **straight from `SpeciesBubble.cardImage`** — one
  field, per species, set in the inspector. Deleted the scavenging loop. This is the single
  source of truth going forward.
- **New art wired:** all 12 bubbles' `cardImage` pointed at the matching sprite in
  `Assets/Aloysius/Currentorgnism fish'/` (blacktip, Grouper, Moray, Trevally,
  RusselsSnapper, blueray, scad, Mullet, reticulated, Parrotfish, Streakedspinefoot,
  Eyestripe). `HEL AA.png` and `ORange.png` in that folder are unmatched placeholders,
  skipped. (Previously 6 species shared a `saybah` placeholder; that's replaced.)
- **FishIcon layout still not tuned** — 80x80, `preserveAspect` on, left pivot, so the fish
  renders small and left-hugging in the card slot. Open item if it needs centering/resizing.

### Ambient food-web arrows survive tab switches (`FoodWebLines.cs`)
- **Bug:** the ambient "web pulse" started in `Start()` (one-shot). Switching tabs
  deactivates `FoodWebLayer`, which **permanently kills the coroutine** — Unity does not
  resume coroutines on re-enable and `Start()` never re-runs. So the pulse died after the
  first tab switch.
- **Fixed:** moved pulse startup to **`OnEnable()`** (guarded by `_pulseRunning` so it never
  double-starts) + added `OnDisable()` to reset `_pulseRunning`/`_revealActive` and clear any
  dangling pulse. Also hoisted the loop's `idx` to a persistent field `_pulseIdx` so the web
  **resumes** from the next link instead of restarting from link #0 each time.

### Modal first-tap bug fixed (`ModalController.cs`)
- **Bug:** "View Details" did nothing on the FIRST tap after build start, worked on the
  second. Cause: the ModalPanel is authored **inactive**, so its `Start()` was deferred to
  the first activation — which was the first `Open()`. `Start()` then called
  `SetActive(false)` on itself, slamming the panel shut right after opening. Second tap
  worked because `Start()` had already run.
- **Fixed:** removed the self-`SetActive(false)` from `Start()` (panel already starts
  inactive in the scene; `Hide()`/`Close()` handle closing). `Start()` now only resets the
  dim overlay. Added an `_opened` guard. Same deferred-`Awake`/`Start` family as the
  `NotificationManager` trap below.

### Health gauge responsiveness (`EcoHealthDashboard.cs`, `EcosystemNetworkManagerGPU.cs`)
- **Perceived lag had two stacked causes:** (1) the host only pushes eco-health to clients
  every **`_populationSyncInterval = 1s`** (health + population share `SyncPopulations()`);
  (2) the tablet gauge used linear `MoveTowards` smoothing at `smoothSpeed = 2`.
- **Changes:**
  - `_populationSyncInterval` **source default 1 -> 0.15** (health reaches the tablet ~6-7x
    more often; data is a few ints + one float). **NOTE: this is a serialized field — the
    manager instance in the netcode scene still has 1 saved; must be set on the instance +
    rebuilt. See open items.** Also networked: **host and client builds must match.**
  - Dashboard smoothing swapped from `MoveTowards` to the **same exponential easing the
    large screen uses** (`k = 1 - Exp(-smoothSpeed*dt)`, then `Lerp`) so tablet + large
    screen feel identical. `smoothSpeed` default 2 -> 8 (note: with the shared exp easing,
    the large screen's `HealthBarBinder` uses ~4; set the tablet to 4 to match exactly).
  - **Number + status word now update INSTANTLY** (read raw `target`, not the smoothed
    `_displayed01`); only the **arc fill** eases. Keeps the readout responsive while the
    ring glides.
- **Why the large screen was always smoother:** it reads `sim.EcoHealth01` **directly**
  (zero network lag) with exponential easing; the tablet reads the *networked* value (stepped
  at the sync interval) with harsher linear easing. Both fixes above close the gap.

### Reusable button tap animation (`TapPunch.cs`, NEW)
- Small `IPointerDownHandler` component that replicates `SpeciesBubble`'s tap feel: scale to
  **1.2x over 0.1s**, settle back over **0.15s**. Drop on any UI element. Tunables:
  `punchScale`, `upTime`, `downTime`.
- Added to the tablet **Add** button (`Ecosystem Panel/Info/Add`) and **View Details**
  button (`Info/DetailRoot/ViewDetailsButton`).
- **Watch:** the Add button also has a `Bob` component. If `Bob` writes `localScale` it can
  fight TapPunch (which snapshots `baseScale` once in `Awake`). Not observed broken, but if
  the punch jitters, that's the cause — have `Bob` drive position, or make TapPunch multiply
  onto Bob's current scale.

### Bottom-right unlock toast on the tablet (`NotificationManager.cs` reworked + `UnlockToast` object)
- Goal: pop the unlocked organism's name bottom-right on the tablet when a species unlocks,
  using the **same CSV-sourced data** the large screen uses.
- **How it's linked:** `EcosystemUnlockManagerGPU` (present in the tablet scene) fires
  `OnSpeciesUnlocked(SpeciesData)`; the name is `SpeciesData.speciesName` (populated from the
  CSV via `CsvUtil`).
- **Reworked `NotificationManager`:** now **self-subscribes** to `OnSpeciesUnlocked`
  (`autoSubscribeUnlock` flag, on) so it fires without the large screen calling it, and split
  into a **host object (stays active, listens)** + a **child `panel` (shows/hides)** — the old
  `Awake(){ SetActive(false); }` was the same deferred-Awake trap that would stop it receiving
  the event. Large-screen usage still works (falls back to old behavior if `panel` is unset).
- **Animation (not a hard pop):** drives a `CanvasGroup` (fade) + `anchoredPosition` (slide).
  Slide-up + fade-in (ease-out, `inDuration` 0.35s), hold (`showSeconds` 4s), fade-out +
  slide-down (ease-in, `outDuration` 0.3s). Captures the panel's authored rest position so it
  always returns exactly where placed. Unscaled time. Added a `CanvasGroup` to `Panel`.
- **Text is name-only** — shows `s.speciesName` (e.g. "Yellowstripe Scad"), not the full
  "You've unlocked the ..." sentence. The "Species Unlocked" header is a separate child.
- Built `UnlockToast` on `TabletCanvas (1)`, bottom-right, dark-blue pill.

### Add-spam handling (`TabletAddRemoveUIGPU.cs`, `RevealQueue.cs`, reveal callers)
- **Problem:** mashing Add fired one RPC per tap (10 taps = 10 fish + 10 messages) and queued
  a card per add/unlock, so the center-stage reveal drained slowly one-at-a-time long after the
  user stopped — plus fish count jumping. Two independent causes, fixed at both layers:
- **Button cooldown (`TabletAddRemoveUIGPU.OnAdd`, JunHeng's script):** new `addCooldown`
  (0.3s default) — `Time.unscaledTime` gate blocks mashing/double-taps without hurting
  deliberate tapping. Client-side UI guard (a host-side rate limit on `RequestAddSpeciesRpc`
  would be stronger hardening).
- **RevealQueue hard cap + dedupe (`RevealQueue.cs`):**
  - `maxBacklog` (default 3) — beyond this many WAITING cards, the oldest waiting card is
    dropped (its `onComplete` still fires), so a burst can't create a long tail.
  - **Dedupe** via a new optional `key` arg to `Enqueue` — if the last waiting card has the
    same key, the duplicate is skipped. Both callers now pass `key: species.speciesName`
    (`SpeciesAddedReveal`, `SpeciesUnlockReveal`), so spamming the same species won't stack
    identical cards. The queue already shortened holds during backlog; this stops the backlog
    forming in the first place.

### Reveal-card / intro-camera desync fixed (`SpeciesAddedReveal.cs`)
- **Symptom (host/large screen):** spamming Add showed a reveal card for the WRONG species
  relative to what the intro camera zoomed to — e.g. card said "Blacktip Reef Shark" while
  only Surgeonfish were in the tank.
- **Cause:** two different triggers. The **intro camera** (`IntroductionCameraDirectorGPU`)
  fires on the host sim event `EcosystemSimulationGPU.OnSpeciesFirstIntroduced` (guarded to one
  shot). The **card** (`SpeciesAddedReveal`) **polled** net population every 0.25s and submitted
  a card whenever it *noticed* a 0->1 transition. Under spam the poll-detection order drifted
  from the camera's single event, so card and zoom showed different species.
- **Fixed:** `SpeciesAddedReveal` now subscribes to the **same `OnSpeciesFirstIntroduced`
  event** the camera uses, maps the event's `SpeciesDataGPU` back to the UI `SpeciesData` via a
  `gpuSpecies` reverse map (`_gpuToData`), and submits that card. One signal, one species, one
  instant — cannot desync. Removed the polling `Update()`, the startup seed loop, and the dead
  members (`_lastPop`, `_seen`, `SafePop`, `_pollTimer`, `pollInterval`). Also removed the now-
  unused `_opened` field left over from the modal fix.
- **Behaviour change:** the card is now genuinely **first-introduction only** (matching the
  camera). Re-adding a species that was removed no longer re-pops a card (the camera didn't
  re-zoom for those either).
- **Testing note:** this is host-side and fires off a real add reaching the host — test via
  **tablet -> host** (the tablet Add RPC is what triggers the sim's introduction event), with
  **both builds rebuilt** (host has the new code; tablet must match the network layout). Adding
  via a host debug path may not exercise the same trigger.
- Lives in `Junheng/SCENE_MainScene` (the host scene) — verified there: single sim instance,
  camera + card both resolve to it, all 12 `allSpecies` have `gpuSpecies` set so the map
  resolves. No scene edit needed (fix is in the shared script that scene already uses).

### Right-side scrollbar on the organism list (`CurrentOrganismsView`)
- The list's `ScrollRect` (vertical) had no scrollbar. Added a vertical `Scrollbar Vertical`
  on the right edge (faint track + light-blue handle) as a "you can scroll" affordance.
- **IMPORTANT:** set to **`Permanent` visibility (overlay)**, NOT
  `AutoHideAndExpandViewport`. The auto-hide/expand mode **resizes the viewport** and shifted
  the card layout — that was the "you broke something" regression; reverted and rebuilt as a
  pure overlay that doesn't touch the content.
- Handle length is driven by the ScrollRect at runtime (`size` = viewport/content); set to
  0.25 in edit mode for preview but it self-recalculates in play. No built-in "max handle
  size" — needs a clamp script only if the handle looks too long with a real (overflowing)
  list in play.

### Reveal card -> TMP (resolves prior open item)
- `SpeciesRevealCard` (Header, NameText, SciText, TierText, MsgText) converted from legacy
  `Text` to **`TMP_Text`**; `SpeciesUnlockReveal` fields changed `Text -> TMP_Text`, `using
  TMPro` added, components swapped and references re-wired. **Font note:** TMP can't use the
  legacy fonts directly — NameText/SciText were on **Rajdhani-Medium**, which reset to TMP's
  default (LiberationSans). Generate a Rajdhani TMP Font Asset (Font Asset Creator) and assign
  to restore. (This closes the "convert SpeciesRevealCard to TMP" item from 2026-07-14.)

---

## Previous session (2026-07-14) — reveal-card images, TMP, health-bar colour

Host scene this session: `Assets/Aloysius/Scenes/SCENE_MainScene.unity`.

### NEW ARRIVAL card images (`SpeciesAddedReveal.cs`)
- **Root cause of "no image on the card":** `SpeciesAddedReveal.cardImages` was **empty
  (0 entries)**. `SpeciesData` has **no image field**, so the card gets its picture from a
  separate `List<Sprite> cardImages` that is **index-aligned with `allSpecies`**. Empty
  list -> `_dataToSprite` resolves nothing -> `revealImage.enabled = false` -> text-only card.
- **Fixed:** populated all **12** slots. Chose `Assets/Aloysius/Orgnanisms/` (matches the
  food-web bubble art); the four species missing from that folder (Shark, Grouper, Moray,
  Trevally) fall back to `Assets/Aloysius/Fishes/`. Verified at runtime — all 12 resolve.
- **To change an image:** the component lives on **`Canvas (1)`**; edit the **Card Images**
  list (order matches **All Species**).
- The unlock card (`SpeciesUnlockReveal` -> `SpeciesRevealCard`) has its **own** image
  handling and is wired separately — populating one does not populate the other.

### Card text -> TextMeshPro
- `AddedRevealCard/MsgText` converted from legacy `Text` to **`TMP_Text`**: script field
  type changed, `using TMPro` added, component swapped, reference re-wired. Preserved
  size 24, colour `#003A79`, top-left align, word-wrap.
- `AddedRevealCard` is now **fully TMP** (Header, NameText, TierText, MsgText).
- **`SpeciesRevealCard` is still ALL LEGACY** (Header, NameText, SciText, TierText, MsgText),
  and `SpeciesUnlockReveal` still declares `public Text ...`. Converting it is the same
  three-step process (field type -> component swap -> re-wire). **Open item.**

### Host health bar — colour by state (`HealthBarBinder.cs`)
- Added the same green -> amber -> red gradient `EcoHealthDashboard` uses, so host and
  tablet read consistently. New toggles: **`colorFill`** (on) and **`colorPercentText`** (off).
- Ramp: `0% #F24C4C` (red) -> `50% #FFBF33` (amber) -> `100% #59E680` (green), smoothly
  interpolated (not banded).
- Required the sprite fix in READ FIRST #6 to actually show correctly.

### Inspector debug slider (`HealthBarBinder.cs`)
- Added **`debugOverride`** (bool) + **`debugHealth01`** (0-1 slider) so health can be
  faked from the Inspector to test fill/colour without a live sim — mirrors
  `EcoHealthDashboard.manualHealth01`.
- Marked the class **`[ExecuteAlways]`** and gated smoothing to `Application.isPlaying`,
  so the slider tracks **instantly in edit mode**.
- **Remember to untick `Debug Override`** or the bar stays frozen at the slider value
  instead of reading real eco-health.
- Note: a bar that looks "empty/white" usually just means **eco-health is genuinely 0**
  (empty reef) — the tint is correct (red), there's simply no fill to see.

---

## Previous session (2026-07-12) — food-web polish, popups, audio, lock enforcement

### Scene roles (important — they are SEPARATE, not duplicates)
- **`Assets/Aloysius/new netcode 1.unity` = the TABLET (client).** Bubbles, food web,
  hold-to-reveal, eco-health gauge, add/remove, tap SFX. **No reveal popups.**
- **`Assets/Aloysius/SCENE_MainScene 1.unity` = the LARGE SHARED SCREEN (host).** The
  center-stage "New Species!" reveal cards (`SpeciesAddedReveal` / `SpeciesUnlockReveal`)
  live here. **No `EcoHealthDashboard`.**
- Splash flow: `Start scene` (logos + "Tap to Start") -> `SceneTemp` (large screen).
  `SplashSequence` auto-routes: large-screen build -> `SceneTemp`, tablet build ->
  `new netcode 1`. The networked "tap the tablet to begin" is a SEPARATE handshake
  (`ExperienceStartGate` + `EcosystemNetworkManagerGPU.OnStarted`) — hard-requires the
  tablet to be **connected** to the host, or the tap silently no-ops.
- Apply tablet-side changes in `new netcode 1`; reveal/host-screen changes in
  `SCENE_MainScene 1`. Shared scripts affect both; per-scene sprite/ref assignments must
  be saved into the correct scene. **Re-check `GetActiveScene()` before saving.**

### Food web — lines, arrows, energy flow (`FoodWebLines.cs`)
- **Routing rewrite:** lines are straight when the direct path is clear, and only bow —
  by the minimum needed — when a bubble is actually in the way (`PathPenalty` picks the
  smallest clearing bow). Endpoints trim to each bubble's rim. Kills the old always-42%
  swoopy arcs that clashed with bubbles.
- **Directional arrows:** each link is drawn prey -> predator with an arrowhead at the
  tip. Rule for reading the web: **the arrow always points at the predator (the eater).**
- **Energy-flow animation:** soft cyan dots drift prey -> predator along each revealed
  curve (fade in at prey, out at predator). Runtime-generated dot sprite — no asset.
  Tunables on the component: `animateFlow`, `flowSpeed`, `dotsPerLine`, `dotSize`.
- Only shows on the hold-to-reveal gesture, so there's no always-on clutter.

### Hold-to-reveal progress ring (`SpeciesBubble.cs`)
- Pressing a bubble now grows a radial ring toward the 0.5s long-press threshold, then
  the food-web lines appear — teaches the hold gesture by touch. Auto-creates a ring
  child from `holdRingSprite` (assigned `Assets/Aloysius/New/lastring.png` to all 14
  bubbles in `new netcode 1`). Tunables: `holdRingColor`, `holdRingScale`.
- **Text-hint half of the prompt is still TODO** (the "Tap on any species!" banner is a
  baked image `aa.png`; rewording needs new art or converting it to live text).

### Health gauge turns red when low (`EcoHealthDashboard.cs`)
- The arc fill now tints **green -> amber -> red** smoothly with health (`HealthColor`),
  matching the status word. New `colorFill` toggle.
- The original `healt.png` arc is a baked green gradient (tint can only darken, so it
  can't go red). Generated a white copy **`Assets/Aloysius/New/healt_white.png`** (same
  shape/alpha, RGB=white) and assigned it to the fill Image in `new netcode 1` so the
  tint fully drives the colour.

### Shared popup queue — fixes clashing cards (`RevealQueue.cs`, NEW)
- The large screen ran **two** independent reveal systems at the **same center position**:
  `SpeciesAddedReveal` (queued) and `SpeciesUnlockReveal` (which did `StopAllCoroutines()`
  and popped instantly). Spam-adding fired adds + the unlocks they trigger together, so
  cards stacked on cards.
- **Fix:** a single `RevealQueue` singleton owns the center slot; both scripts submit
  their card to it instead of fading their own `CanvasGroup`. One card at a time, filled
  the instant before it shows. Backlog cap: >2 waiting -> hold drops to 1.5s so bursts
  clear fast. Auto-creates itself at runtime (no scene object required).

### Tap SFX (`UISoundManager.cs`, NEW)
- Tiny shared one-shot player. `SpeciesBubble.OnTap` calls `UISoundManager.Instance.PlayTap()`.
  A `UISoundManager` object exists in `new netcode 1` — **drop a clip in its `Tap Sound`
  slot** (2D AudioSource, playOnAwake off). Fires on an unlocked-bubble tap only.

### Alucia intro gated on the start-tap (`AluciaController.cs`)
- **Bug:** the intro fired on scene load, so on the large screen Alucia talked to the
  "Tap the tablet to begin" title and players missed it. **Fix:** the intro (and health
  reactions) now wait for the networked `OnStarted` event — the same tap-to-begin the
  reef reveal uses. New `waitForExperienceStart` toggle (default on; turn off to play on
  load for standalone testing). Handles late-joining tablets.

### Safe deprecation cleanup (no behaviour change, no wiring touched)
- `FindObjectOfType` -> `FindFirstObjectByType` in `SpeciesAddedReveal`, `RevealQueue`,
  `HealthBarBinder`. Removed unused `HealthBarBinder.percentFormat` field and the dead
  `FoodWebLines.HighlightBubble()` method. All Aloysius-script warnings now clear.
- Still flagged (out of Aloysius scope): `NetworkBootstrap._hostAddress` unused,
  `EcosystemUIAdapterGPU` dead (both JunHeng's).

---

## Where This Work Lives

| Thing | Scene / Path | Notes |
|-------|--------------|-------|
| Tablet UI (food web, tabs, eco-health, info panel) | **Copy** of `Netcode Simulation Test` (`Assets/Aloysius/`) | Not the canonical scene |
| Alucia + unlock reveal | **Copy** of `Boids_Demo` (`Assets/Aloysius/Boids_Demo.unity`) | Host/large-screen copy |
| New scripts | `Assets/Aloysius/Scripts/` | see file list below |
| Edited shared script | `Assets/Aloysius/Scripts/SpeciesBubble.cs` | affects canonical scene |

> Scene-level work is in **copies**; only the **scripts** (shared) touch canonical
> scenes. Integration into JunHeng's scenes is a separate step.
>
> **Current live scenes (2026-07-12):** the tablet copy is now
> `Assets/Aloysius/new netcode 1.unity` and the host/large-screen scene is
> `Assets/Aloysius/SCENE_MainScene 1.unity` (+ `SceneTemp` as the splash target).
> See **"This session -> Scene roles"** above for the authoritative split.

---

## Tablet UI — latest session

### 3-tab navigation with swipe transition (`TabController.cs`)
- Tab bar (FOOD WEB / ORGANISMS / HINTS) on the Ecosystem Panel, switching between
  `FoodWebLayer`, `CurrentOrganismsView`, and a new placeholder `HintsView`.
- **Swipe transition:** outgoing panel slides off one side, incoming slides in from
  the other (direction depends on tab order), 0.3s smooth. `slideDistance` = panel
  width (1920).
- **Old drag-reveal mechanic disabled** (`FoodWebDragReveal` on `arrow` + `DragHandle`)
  — tabs are now the single switch method.
- **Tab button colours:** inactive = white (so your own tab art shows true colours,
  untinted), active = blue tint multiplied over the art. NOTE: tint *multiplies*, so
  it can't make a tab brighter than the source image — for a true glowing-selected
  look, use a two-sprite swap (separate active art) instead of a tint.
- Tab art is being done in Photoshop (resting image baked with colour; selected state
  via tint or swap).

### Rings now travel with the food web
- The concentric ring guides were children of `BG` (a sibling that never hid), so they
  stayed on screen during tab swipes. **Moved them into `FoodWebLayer`** so they
  hide/show and slide *with* the food web.

### Eco-health dashboard gauge (`EcoHealthDashboard.cs`)
- Replaced the old `DataChassis` radial widget (which didn't read as a gauge — single
  flat sprite, partial fill looked identical) with a **"Dashboard001" prefab** that has
  a real arc fill.
- Drives the prefab's arc fill Image (`Dashboard01_Progress`) + percent text directly
  from eco-health; **disabled the prefab's own `TMLineSliderP`** so it doesn't fight.
- Reads `EcosystemNetworkManagerGPU.Instance.GetEcoHealth()` (0-1, with 0-100 guard);
  falls back to a `manualHealth01` Inspector slider when no host (for edit-mode preview).
- `[ExecuteAlways]` so it previews in edit mode.
- **Status word** under the %: THRIVING (>=85) / HEALTHY (>=60) / UNSTABLE (>=35) /
  CRITICAL (>0) / COLLAPSED (0), recoloured green/amber/red. The status text reference
  **self-heals** via `AutoWire()` (finds the "StatusText" child if the Inspector slot is
  cleared) — this was needed because component refs kept getting wiped on recompiles.
- **Health panel moved OUT of `FoodWebLayer` up to `Ecosystem Panel`** so it stays
  FIXED during tab swipes and shows on all tabs.

### Right-side species info panel (`SpeciesInfoPanel.cs`)
- The `Info` panel on the right: shows **"No Organism Selected"** empty state; on a
  bubble tap, fills with the species **name**, **tier/role badge**, and **description**
  (`addedMessage`), plus a **VIEW DETAILS** button.
- **Flow:** bubble tap -> side panel summary -> "View Details" opens the existing
  `ModalController` popup (full info + Add/Remove). `SpeciesBubble.OnTap` was updated to
  route to `SpeciesInfoPanel.Instance.Show(...)` (falls back to opening the modal
  directly if the panel isn't present).
- Text-first (no fish image in the panel yet; `speciesImage` slot left null).
- **Open question (undecided):** whether to keep the two-step (panel -> View Details ->
  modal) or put Add/Remove directly in the side panel (one step, better for a walk-up
  exhibit). Not yet decided.
- **Data note:** 11/12 bubbles have a `cardImage`; **Bullethead Parrotfish has none**,
  so its View Details button shows but won't open the modal until a card sprite is
  assigned.

---

### Producer species — Seagrass & Macroalgae (this session)
- **Seagrass:** existing `SpeciesData` registered into the manager's `_allSpecies`;
  now unlocked + tappable. Its `hint1/2/3` + `addedMessage` are still copied from
  Yellowstripe Scad (harmless for a start-unlocked producer, but worth cleaning up).
- **Macroalgae (new):** created `Assets/Aloysius/SpeciesData/Macroalgae.asset`
  (duplicated from Seagrass, cleaned): `tier = Primary Producer`,
  `startUnlocked = true`, `minHealth = 0`, empty `requires`, hints + `addedMessage`
  cleared, **`sciName` blank (TODO)**. `gpuSpecies` -> JunHeng's new
  `Assets/Junheng/Data/Fish/Primary Producer/Macroalgae.asset`. Assigned to the
  `macroalgae bubble` and added to `_allSpecies`. Verified in Play (unlocked, tap
  punch + info panel). **`simIndex = -1`** (see READ FIRST #5).

### Host (Boids_Demo) health bar bound to eco-health (this session)
- **`Screen UI/healthbar`** now driven live by `HealthBarBinder.cs`, which reads
  `EcosystemSimulationGPU.EcoHealth01` (0-1) each frame and updates the filled
  Image's `fillAmount` + the "%" TMP label (auto-wires sim/fill/text on `Awake`).
- **Compile fix:** binder referenced `EcosystemSimulationGPU` without its
  namespace; added `using OceanX.BoidsGPU.Ecosystem;`.
- **Bug fix:** the fill Image carried a leftover **`TMLineSliderP`** demo-animation
  (from "LightColored Graph And Chart UI Pack") that looped `fillAmount` 0->1
  forever and overwrote the binder every frame — that was the "bar counts 1->100
  continuously" symptom. **Removed `TMLineSliderP`** from the fill Image.
- **Easing:** `Update()` uses frame-rate-independent ease-out (`1 - Exp(-speed*dt)`).
  Tunables: `Smooth`, `Smooth Speed` (~4), `Percent Format`.
- **Caveat:** reads 0% at start in the demo scene (no species spawned yet) — true
  value, not a bug.

### Species info-panel description edits (this session)
- **Fringelip Mullet** `addedMessage` -> "Schooling herbivore that grazes algae and
  detritus off sandy reef floors."
- **Eyestripe Surgeonfish** -> "Solitary herbivore scraping algal films; wields
  scalpel-like spines for defence." _(proposed — confirm applied)_

## Earlier sessions (still current)

### Food web radial layout
12 bubbles in a radial layout centered on the shark (even angles/radii). Departure from
the prototype's tier-based design — **team to confirm direction.** Halos decorative.

### Species info card hierarchy fix
Section labels small/muted, answers prominent, body softer. IUCN badge = the one colour
accent.

### Atmospheric FX (food web)
`SonarPulse.cs` (expanding rings), `MarineSnow.cs` (rising reef bubbles — flipped from
falling snow), `GodRays.cs` (light shafts). Self-contained, behind the bubbles,
raycast-disabled, runtime-generated sprites. Order: GodRays -> SonarPulse -> MarineSnow
-> Rings -> Linesmanager -> Organisms.

### SpeciesBubble TapPunch bug fix
Spam-tap unbounded growth fixed: capture `baseScale` once in `Start()`, punch from/to it,
`OnTap` stops/resets running punch. Shared script — affects canonical scene.

### Alucia — host guide character (`AluciaController.cs`)
2D overlay on the large/host screen (`Boids_Demo`), octopus-girl per storyboard.
Appears only when speaking (character + bubble fade together), intro sequence on Start,
health-band reactions off `EcosystemSimulationGPU.EcoHealth01` (debounced + hysteresis,
improving vs worsening lines). Placeholder art; three moods tint the bubble.

### Species unlock reveal + hint (`SpeciesUnlockReveal.cs`)
On first unlock: big "NEW SPECIES DISCOVERED" card (name/sci/tier/message, \~5.5s), then
Alucia hints the closest-to-unlockable locked species (fewest unmet reqs via
`GetLockInfo`). Subscribes to `EcosystemUnlockManagerGPU.OnSpeciesUnlocked`. Confirmed
working via forced-event test.

---

## How Unlocking Works (reference)

- `CheckUnlocks()` unlocks a species when eco-health% >= its `minHealth` AND every
  `requires` entry met (prey school counts). Latching. Runs only while the manager
  component is **enabled** (see the bug above).
- Config per `SpeciesData`: `startUnlocked`, `minHealth`, `requires`, `hint1/2/3`,
  `addedMessage`, `tier`, `gpuSpecies`.
- Most species have `minHealth = 0` (Bluefin Trevally = 10, Giant Moray = 40), so
  unlocking depends mostly on prey counts. Start-unlocked (corrected 2026-07-12):
  the 5 grazers + 2 producers (Seagrass, Macroalgae) only — the Shark and the other
  6 gated predators now start **locked**. Manager `_allSpecies` has 14 entries.

### Current unlock requirements (from the assets)
| Species | Requires |
|---------|----------|
| Bullethead Parrotfish / Eyestripe Surgeonfish / Fringelip Mullet / Reticulated Damselfish / Streaked Spinefoot | start unlocked |
| Seagrass / Macroalgae (producers) | start unlocked (now registered in `_allSpecies`; `simIndex = -1`) |
| Yellowstripe Scad | Damselfish x2, Mullet x2 |
| Russell's Snapper | Damselfish x2, Surgeonfish x2 |
| Bluefin Trevally | Mullet x3, Parrotfish x2 |
| Bluespotted Ray | Parrotfish x2, Spinefoot x2 |
| Brown-Marbled Grouper | Scad x2, Trevally x1, Snapper x1 |
| Giant Moray | Snapper x2, Ray x1 |
| Blacktip Reef Shark | Grouper x1, Moray x1, Trevally x2, Scad x3 — **startUnlocked=False (fixed 2026-07-12)** |

---

## Open / To-Do

**Tablet UI:**
- Decide: side-panel summary + View Details vs. Add/Remove directly in the side panel.
- Tab art (Photoshop) — resting image + selected state (tint, or two-sprite swap for glow).
- ~~Assign a `cardImage` to **Bullethead Parrotfish**~~ — RESOLVED 2026-07-18 (all 12
  `cardImage` slots now wired to the "Currentorgnism fish" art set).
- Badge is plain text in caps — could become a coloured pill by tier.
- Info-panel description uses `addedMessage`; confirm that's the right field per fish.

**Tablet UI (2026-07-18 — open items):**
- **Apply `_populationSyncInterval = 0.15` on the manager INSTANCE** in the netcode scene
  (source default is changed, but the serialized instance still reads 1) — and **rebuild the
  tablet + host** (networked value; builds must match).
- **Set dashboard `smoothSpeed` to 4** if you want the tablet gauge to match the large
  screen exactly (currently 8, snappier).
- **FishIcon layout on `OrganismCard.prefab`** — 80x80 + preserveAspect + left pivot makes
  the fish small/left-hugging. Center + resize to fill the slot with the new art.
- **OverpopBadge art** — currently built-in `UISprite` pill; swap for custom art if desired.
- **Scrollbar handle length** — only add a min-content clamp if it looks too long with a
  real overflowing list in play (built-in Scrollbar has no max-handle-size).
- **Generate a Rajdhani TMP Font Asset** and assign to the reveal card's NameText/SciText
  (they reset to LiberationSans when converted to TMP).

**Tablet UI (2026-07-18b — open items):**
- **Win screen only in the host scene** — build a copy in the tablet scene (`new netcode 1`)
  if you want it on both. Detection is non-networked so each screen triggers off shared health.
- **Win "Play Again" is hide-only** — wire it to an actual ecosystem reset (lower/reset the
  sim host-side); currently it clears the local `Won` and re-triggers if health is still >=99%.
- **Assign a happy Alucia sprite** to the WinScreen's `aluciaWinSprite` (using calm sprite now).
- **Verify `AluciaEcologyEvents` is on an active object** in the build scene — the per-species
  narration is written but silent if the component isn't placed.
- **Multiple `SCENE_MainScene` copies** (Akil / Aloysius / Junheng / `SCENE_MainScene 1`) —
  consolidate / confirm which is the real host build before shipping. Easy to edit the wrong one.
- **`SwipeToClose.cs`** now unused (back button replaced it) — delete if the gesture isn't
  wanted elsewhere.

**Tablet UI (2026-07-28 — open items):**
- **`new netcode 2` is UNCHECKED in Build Settings** — confirm the tablet build actually
  loads it, or none of this session's scene objects ship. See READ FIRST #7.
- **Set `addCooldown` on the `Ecosystem Panel` INSTANCE** — source default is now 1.0 but
  the serialized instance still reads 0.3, so the cooldown sweep is only a blink.
- **Pin down `_ratioBandLow`** by reading the live serialized value in the host scene — the
  delta arithmetic assumes rev 8's documented value of 1, and that does not reconcile with
  the verified 100% configuration (see the unresolved note above).
- **Confirm asset `MaxSchools` vs the live `GetMaxSchools()`** with JunHeng — the debug
  overlay's predator denominators disagree with the assets.
- **Tune the advisor's `escalateAfter`** (25s is a guess; 15s if museum visitors bail
  faster) and its placeholder copy.
- **Verify the `LookUpPrompt` position** (y=-940 was eyeballed from the two hint banners)
  and rewrite its placeholder copy.
- **Verify the `DeltaLabel` position** on `OrganismCard.prefab` (x=830) against a real
  populated list.
- **Consider whether the HINTS tab covers "remove predators to save prey"** — with
  `maxDetailLevel = 1` the advisor stops at "are being hunted out" and never says the fix.
  A visitor's instinct is to add more of the dying fish, which at the cap they cannot do.
- **Check every species' `MaxSchools` against its summed predator requirement.** Damselfish
  sits right on the line (cap 10, six predators). Any species whose cap is below its
  requirement can *never* be healthy, which would make 100% impossible rather than hard.
- **Re-audit the em dashes** in `BalanceAdvisor`'s strings — they render only via TMP's
  global fallback (READ FIRST #8).
- Consider the **food-web pressure display** and **showing absent species in the Organisms
  list** (both written up under "Ideas considered and NOT built").

**Alucia / reveal:**
- Real art (placeholder block); reveal card image slot empty.
- **Lower the "Thriving" win trigger from 100% to \~90%** (health likely tops out <100). Not yet applied.
- Wire unlock/extinction Alucia reactions.
- Remove diagnostic Debug.Logs in `SpeciesUnlockReveal` before final (optional).

**For JunHeng:**
- Re-check the unlock manager is ENABLED in canonical `Boids_Demo`.
- ~~Fix Shark `startUnlocked -> False`~~ — done 2026-07-12 (all gated species now locked).
- Balance pass on requirements.
- Note: I edited two of your scripts this session — `TabletAddRemoveUIGPU.cs` (Add now
  gated on unlock) and `EcosystemUnlockManagerGPU.cs` (new `IsUnlocked(SpeciesDataGPU)`).
- **(2026-07-18) I edited `EcosystemNetworkManagerGPU.cs` again** — two networked changes
  needing your attention:
  1. `_populationSyncInterval` default 1 -> 0.15 (health/pop reach the tablet ~6-7x more
     often). The **serialized instance in the netcode scene still reads 1** — set it on the
     instance, and **rebuild host + tablet together** (must match).
  2. `_ecoHealth` NetworkVariable given explicit `Everyone` read / `Server` write permissions
     so late-joining tablets get the current value.
  - Reminder from this session's debugging: a client stuck at **0% health while population
    synced** + an `OverflowException: Reading past the end of the buffer` in the console = a
    **build/scene mismatch** between host and client (we traced ours to building with the
    wrong scene set). Always rebuild all clients after any NetworkVariable/List change.
- **(2026-07-18) Also edited `TabletAddRemoveUIGPU.cs`** — added an `addCooldown` (0.3s) spam
  gate in `OnAdd` (client-side). A host-side rate limit / authority check on
  `RequestAddSpeciesRpc` would be stronger if you want to harden it server-side.
- **(2026-07-18) `SpeciesAddedReveal` now subscribes to your
  `EcosystemSimulationGPU.OnSpeciesFirstIntroduced`** event (read-only, same event the intro
  camera uses) to fix the card/camera desync on spam. No change to your sim or camera scripts —
  just a new consumer of the existing event. FYI in case you refactor that event's signature.
  Consider a host-side authority check on `RequestAddSpeciesRpc` too (mine is client-side).
- **(2026-07-18b) I edited `TabController.cs`** — split `Start()` into a gated `InitializeTabs()`
  that runs only after `HasStarted` (for the hide-Ecosystem-Panel-until-start feature). Behaviour
  is identical once started; it just defers the initial tab/prompt show. Waits on `OnStarted`, or
  a coroutine until the manager spawns. FYI in case you touch that script.
- **(2026-07-28) I edited FOUR of your scripts** — all additions are marked with a
  `BALANCE DELTA` comment where they belong to the per-species number feature:
  1. **`EcosystemSimulationGPU.cs`** — new `public int GetSpeciesDelta(SpeciesDataGPU)`.
     Read-only; uses your private `_ratioBandLow` / `_overpopulatedRatio` /
     `_overpopulatedFreeCount` / `BuildCommittedCounts()` / `GetMaxSchools()`. Mirrors
     `GetSpeciesStatus`'s check order deliberately so the two can never disagree.
  2. **`EcosystemNetworkManagerGPU.cs`** — new `NetworkList<int> _speciesDelta` alongside
     `_speciesStatus` (constructed in `Awake`, seeded in the spawn loop, written in
     `SyncPopulations`) + `GetSpeciesDelta(int)`. **This is a networked layout change —
     host and tablet MUST be rebuilt together** (see the rev 8 buffer-overflow note).
  3. **`TabletAddRemoveUIGPU.cs`** — `CooldownRemaining` / `CooldownDuration` properties;
     cooldown folded into `addButton.interactable`; `addCooldown` source default 0.3 -> 1.0
     (instance still 0.3); one `LookUpPrompt.Trigger()` line in `OnAdd`.
  4. **`ExhibitReset.cs`** — one `LookUpPrompt.ResetForNewSession()` line in
     `DoLocalReset()`, alongside the existing `ContextNudge` line.
- **(2026-07-28) `removeButton` is NULL** on the `Ecosystem Panel` instance of
  `TabletAddRemoveUIGPU`, so `OnRemove` is dead code and the shared `_lastAddTime` guard
  only ever gates Add. Removal happens exclusively through the organism rows'
  `OnTapRemoveOne`. Intentional or an oversight?
- **(2026-07-28) `SyncPopulations` writes `_ecoHealth.Value` AFTER the per-species loop**, so
  anything that throws inside that loop leaves health frozen at its last value. We briefly
  saw 0% stuck during the session. Moving the health assignment **above** the loop would make
  it immune to that class of failure.

**This session (2026-07-12) — open items:**
- **Text-hint half of the hold prompt** — the "Tap on any species!" banner (`aa.png`,
  baked image) still doesn't mention holding. Needs a new pill image or conversion to
  live text (decision pending).
- **Drop a tap-sound clip** into the `UISoundManager` object's `Tap Sound` slot in
  `new netcode 1` (wiring is in place; clip not assigned).
- Optional: sound for locked-bubble tap and Add/Remove.
- Optional: host-side authority check on adds (see JunHeng note above).

**This session (producers / host bar):**
- Fill Macroalgae `sciName` (left blank).
- Clean Seagrass's Scad-derived hint/`addedMessage` text.
- Decide whether producers should be sim-interactive (JunHeng to register
  Seagrass/Macroalgae GPU species in `TabletEcosystemUIGPU`; currently `simIndex = -1`).
- Confirm Eyestripe Surgeonfish description applied.

**This session (2026-07-14) — open items:**
- **Convert `SpeciesRevealCard` to TMP** (still all legacy Text; `SpeciesUnlockReveal`
  still declares `public Text`). Same process as `AddedRevealCard`.
- **Populate the unlock card's image** — `SpeciesRevealCard/RevealImage` is separate from
  the added-card's `cardImages` and is still unset.
- **`RevealImage` sizing** — currently 180x120 with **`preserveAspect = OFF`**, so the fish
  is squashed. Turn preserveAspect on and resize/reposition to fill the card's empty area.
- **CSV-driven reveal cards (in progress)** — a full CSV content system already exists
  (`SpeciesContentDB.cs` + `StreamingAssets/SpeciesContent.csv` + `SpeciesImages/`), and
  `ModalController` / `SpeciesInfoPanel` already use it. Goal: have the reveal cards pull
  name/role/description/image from the CSV via `SpeciesContentDB.Get(idOrName)` and
  `SpeciesContentDB.GetImage(imageFile)` instead of the hand-wired `cardImages` list.
  **Not implemented yet.**
- **Behavioural copy pass** — the "NEW ARRIVAL" line should read as an *event*
  ("Yellowstripe scad are forming schooling groups in open water"), but the five consumers
  still use *factual* descriptions ("Medium schooling Indo-Pacific rabbitfish that..."),
  while Seagrass/Macroalgae/Scad already use the behavioural voice. Decide whether the CSV
  needs **two columns** (factual `description` for the modal, behavioural `addedMessage`
  for the arrival card) and rewrite the inconsistent ones.

**Asset note (shark model):**
- Explored AI 3D generation (Tripo / Meshy) for a stylized blacktip shark. AI topology
  is poor by default; Meshy's **Remesh** produced clean quad topology. Tripo gates export
  behind payment; **Meshy free tier allows export but is CC-BY (must credit "Meshy AI")**
  and free tier uses Meshy 5 (Low Poly Mode is a paid Meshy-6 feature). Free pre-made
  low-poly sharks on Sketchfab (mostly CC-Attribution) are a viable alternative.

---

## Watchpoints / Gotchas

- **SAVE after runtime-created objects** — objects made via MCP `execute_code` don't
  persist unless the scene is saved. Lost the Alucia objects once before a save.
- **Edit mode for scene edits** — `execute_code` scene changes fail in Play mode
  (MarkSceneDirty) and don't persist.
- **Component refs get wiped on recompile** — bit us on TabController + the eco-health
  status text. The eco-health panel now self-wires; watch for it elsewhere.
- **MCP bridge dropped connection several times** mid-edit (recovered). Re-verify edits
  if something looks half-applied.
- **Singletons (`...Instance`) are null in edit mode** — diagnostics that read them must
  run in Play.
- **Isolated host = empty sim** — population only arrives with the tablet connected.
- **Tint needs white art** — a pre-coloured sprite multiplied by a tint gives muddy/wrong
  colours (see READ FIRST #6). Author tintable UI art in white/greyscale.
- **Sprite edge artefacts** (white lines / fringing that don't appear in Photoshop) are an
  *import* issue, not the PNG: set **Mesh Type = Full Rect**, **Generate Mip Maps = OFF**,
  **Wrap Mode = Clamp**, **Alpha Is Transparency = ON** for UI sprites.
- **An empty-looking health bar usually means health is genuinely 0**, not a broken binder.
- **Deferred `Awake`/`Start` on inactive objects** — a component on a GameObject that starts
  **inactive** does not run `Awake`/`Start` until first activated. If that `Start()` then
  deactivates itself (or a singleton sets `Instance` there), you get "first tap does nothing,
  second works" or a null `Instance`. Bit us on `ModalController` and `NotificationManager`
  this session (both fixed). Prefer authoring "start hidden" in the scene over
  `SetActive(false)` in `Start()`, and set singleton `Instance` in `Awake` on an
  always-active host object.
- **Coroutines die permanently when their GameObject is deactivated** — Unity does NOT resume
  them on re-enable. Anything started in `Start()` on a layer that gets toggled by tab
  switches (e.g. `FoodWebLayer`) must (re)start in `OnEnable()`. Bit the ambient food-web
  pulse this session (fixed).
- **Networked value changed => rebuild ALL clients** — host/client must be compiled from the
  same NetworkBehaviour layout AND the same build scene set. Mismatch = buffer-overflow
  deserialize errors and networked values stuck at defaults (e.g. tablet health stuck at 0
  while population synced).
- **ScrollRect scrollbar visibility** — `AutoHideAndExpandViewport` **resizes the viewport**
  and can shift your content layout. Use `Permanent` (overlay) if you just want a visible
  scrollbar without touching the list.
- **(2026-07-28) NGO delivers NetworkVariable initial values BEFORE `OnNetworkSpawn`.** A
  late-joining client gets the current value in the spawn payload, so an `OnValueChanged`
  handler wired in `OnNetworkSpawn` **never fires for it**. Any "wait for the session to
  start" code must **subscribe AND poll the current value** — subscribing alone silently
  does nothing for a client that joined mid-session. This broke the tutorial auto-show.
- **(2026-07-28) A singleton assigned in `Awake` is visible before its NetworkObject
  spawns.** `EcosystemNetworkManagerGPU.Instance` exists while its NetworkVariables still
  hold defaults, so a poll that latches on `Instance != null` can read stale state.
- **(2026-07-28) Changing a field's initializer does NOT change already-serialized
  instances.** Unity only uses the default for components that have no value saved. Bit us
  twice this session (`addCooldown`, `nothingToDoMessage`) and once in rev 8
  (`_populationSyncInterval`). **Always set the value on the instance/prefab too, and
  verify by reading it back.**
- **(2026-07-28) Rajdhani is missing common typographic glyphs** — no checkmark, minus sign
  or em dash, and no local fallback. Use ASCII in tablet UI strings. See READ FIRST #8.
- **(2026-07-28) A "compiles clean" check can be a false negative after a script edit.**
  A stale assembly kept loading and reflection reported a newly added field as absent;
  `UnityEditor.EditorUtility.scriptCompilationFailed` was `true` while the console looked
  quiet. Check that flag, not just the absence of console errors.
- **(2026-07-28) Namespace trap for new tablet scripts:** `EcosystemSimulationGPU`,
  `SpeciesDataGPU` and `SpeciesRoleGPU` live in `OceanX.BoidsGPU.Ecosystem`, but
  `EcosystemNetworkManagerGPU` is in the **global** namespace. A new script that uses the
  manager compiles without a `using`, then fails the moment it references `SpeciesStatus`.
- `Boids_Demo` shark+water shader crash warning still applies (JunHeng's note).

---

## Files Touched / Added (`Assets/Aloysius/Scripts/`)

**New:**
- `SonarPulse.cs`, `MarineSnow.cs`, `GodRays.cs` — atmospheric FX
- `AluciaController.cs` — host guide (intro + health reactions)
- `SpeciesUnlockReveal.cs` — unlock reveal card + hint
- `TabController.cs` — 3-tab nav with swipe
- `EcoHealthDashboard.cs` — eco-health arc gauge + status word
- `SpeciesInfoPanel.cs` — right-side species detail panel
- `RevealQueue.cs` — shared center-stage popup queue (2026-07-12)
- `UISoundManager.cs` — shared UI SFX one-shot player / tap sound (2026-07-12)

**Edited (2026-07-12):**
- `FoodWebLines.cs` — line routing rewrite, directional arrows, energy-flow dots;
  removed dead `HighlightBubble()`
- `SpeciesBubble.cs` (shared) — hold-to-reveal progress ring; tap SFX call
- `EcoHealthDashboard.cs` — `HealthColor` green->amber->red arc tint (`colorFill`)
- `SpeciesAddedReveal.cs` / `SpeciesUnlockReveal.cs` — route through `RevealQueue`
- `AluciaController.cs` — intro gated on `OnStarted` (`waitForExperienceStart`)
- `TabletAddRemoveUIGPU.cs` (JunHeng's) + `EcosystemUnlockManagerGPU.cs` (JunHeng's) —
  Add gated on unlock state; new `IsUnlocked(SpeciesDataGPU)` overload
- `HealthBarBinder.cs` — deprecation fix; removed unused `percentFormat`

**Edited (2026-07-14):**
- `SpeciesAddedReveal.cs` — `msgText` field `Text` -> `TMP_Text`; `using TMPro`
- `HealthBarBinder.cs` — `HealthColor` green->amber->red gradient (`colorFill`,
  `colorPercentText`); `debugOverride` + `debugHealth01` Inspector slider;
  `[ExecuteAlways]`; smoothing gated to play mode

**New assets (2026-07-14):**
- `Assets/Aloysius/New/hhealthtth_white.png` — white/neutral copy of the health-bar fill
  sprite so the colour ramp can tint it (original had green baked in)

**Scene changes (2026-07-14, `SCENE_MainScene`):**
- `Canvas (1)` -> `SpeciesAddedReveal.cardImages` populated (12 sprites)
- `AddedRevealCard/MsgText` legacy `Text` -> `TextMeshProUGUI`, reference re-wired
- `Canvas (1)/Health/healthbar` fill Image -> `hhealthtth_white`, base colour white

**New (2026-07-18):**
- `TapPunch.cs` — reusable tap "punch" scale animation (1.2x/0.1s up, 0.15s back)

**Edited (2026-07-18):**
- `OrganismCardData.cs` — `overpopBadge` field + `UpdateOverpop()` (matches food-web
  `pop >= GetMaxSchools`); baselined off in `Setup()`
- `CurrentOrganismsGrid.cs` — icon source rewritten to `SpeciesBubble.cardImage`; removed the
  child-Image scavenging loop
- `FoodWebLines.cs` — ambient pulse moved `Start()` -> `OnEnable()` (+ `OnDisable()` reset);
  persistent `_pulseIdx` so the web resumes across tab switches instead of restarting
- `ModalController.cs` — removed self-`SetActive(false)` from `Start()` (first-tap bug);
  `_opened` guard
- `EcoHealthDashboard.cs` — smoothing swapped to exponential easing (matches large screen);
  `smoothSpeed` default 2 -> 8; number + status word now read raw `target` (instant), only
  the fill eases
- `EcosystemNetworkManagerGPU.cs` (JunHeng's) — `_populationSyncInterval` source default
  1 -> 0.15 (faster health/pop sync); `_ecoHealth` NetworkVariable given explicit
  `Everyone`/`Server` read/write permissions (late-join replication). **NOTE: told JunHeng —
  networked, needs matching rebuilds + instance value set.**
- `NotificationManager.cs` — self-subscribes to `OnSpeciesUnlocked` (`autoSubscribeUnlock`);
  host/panel split so the listener object stays active; animated slide+fade show/hide
  (`inDuration`/`outDuration`/`slideDistance`); body text is name-only (`s.speciesName`)
- `RevealQueue.cs` — `maxBacklog` hard cap (drop oldest waiting card) + `key`-based dedupe on
  `Enqueue` (skip identical consecutive cards)
- `SpeciesAddedReveal.cs` — pass `key: species.speciesName` to `RevealQueue.Enqueue`; **also
  rewired to subscribe to `EcosystemSimulationGPU.OnSpeciesFirstIntroduced`** (the intro
  camera's event) instead of polling population — fixes the card/camera desync. Added
  `_gpuToData` reverse map + `HandleFirstIntroduced`; removed polling `Update()`, seed loop, and
  dead members
- `SpeciesUnlockReveal.cs` — pass `key: species.speciesName` to `RevealQueue.Enqueue`
- `TabletAddRemoveUIGPU.cs` (JunHeng's) — `addCooldown` (0.3s) spam gate in `OnAdd`
- `ModalController.cs` — removed the now-unused `_opened` guard field (cleanup from the
  first-tap fix)
- `SpeciesUnlockReveal.cs` — reveal-card fields `Text -> TMP_Text`; `using TMPro`

**Scene changes (2026-07-18, `new netcode 1` tablet scene):**
- All 12 `SpeciesBubble.cardImage` -> matching sprite in `Assets/Aloysius/Currentorgnism fish'/`
- `TabletCanvas (1)/Ecosystem Panel/Info/Add` + `Info/DetailRoot/ViewDetailsButton` -> `TapPunch`
- `TabletCanvas (1)` -> new `UnlockToast` object (`NotificationManager`, bottom-right, wired)
- `CurrentOrganismsView` -> new `Scrollbar Vertical` (right edge, `Permanent`/overlay), wired
  to the ScrollRect's `verticalScrollbar`

**Prefab changes (2026-07-18):**
- `Assets/Aloysius/Prefabs/OrganismCard.prefab` -> `OverpopBadge` child added + wired;
  background Image alpha 0.4 -> white/opaque (was near-invisible), preview width widened
  (runtime width still driven by the list's `VerticalLayoutGroup`)

**New (2026-07-18b):**
- `WinCondition.cs` — detects win (eco-health >= 99% held 2s); plain MonoBehaviour
- `WinScreen.cs` — full-screen win overlay (Alucia thank-you + title + Play Again)
- `HideUntilStarted.cs` — hides a list of visual objects until `HasStarted` (tap-to-start)

**Edited (2026-07-18b):**
- `ModalController` modal — `SwipeToClose` component REMOVED; X back button added -> `Close()`
- `TabController.cs` (JunHeng's) — `Start()` split into gated `InitializeTabs()` that runs only
  after `HasStarted` (so the tab UI/prompt don't show pre-start)

**Scene changes (2026-07-18b):**
- Host scene (`SCENE_MainScene 1`) `Canvas (1)` -> new `WinScreen` overlay (WinCondition +
  WinScreen, dim/Alucia/title/message/PLAY AGAIN, wired, hidden by default)
- Tablet scene (`new netcode 1`): `Ecosystem Panel/ModalPanel` -> `SwipeToClose` removed,
  `BackButton` (X) added; new always-active `PanelStartGate` object with `HideUntilStarted`
  (hides prompt/panel/FoodWebLayer/TabBar/Health/Info until start)

**New (2026-07-28):**
- `LookUpPrompt.cs` — recurring "look up at the big screen" toast, fires on every add
- `ButtonCooldownOverlay.cs` — FFXIV-style radial cooldown sweep for the Add button
- `BalanceAdvisor.cs` — bottom-left "HOW TO BALANCE" panel (grouped, escalating, diagnostic-only)

**Deleted (2026-07-28):**
- `HealthChangeFeedback.cs` — per-action `+4%` health popup; built and removed the same
  session as too distracting. All hooks stripped, no lingering references.

**Edited (2026-07-28):**
- `TutorialPanel.cs` — always subscribe to `OnStarted` + poll `HasStarted` (late-join fix);
  `_rearmPending` deferred re-arm; new `IsOpenOrPending` property + `_autoShowPending`
- `ContextNudge.cs` — same subscribe/poll fix + `_rearmPending`; new static
  `Advance(gateId)` extracted from `Dismiss` so a milestone can release a gate; `Advance`
  guards on `_rearmPending`; `showAfterId` tooltip rewritten
- `ModalController.cs` — `Close()` now calls `ContextNudge.Advance("details")`
- `SpeciesInfoPanel.cs` — `detailsHintFallbackSeconds` (default 8) + `EnsureHintFallback` /
  `CancelHintFallback` / `DetailsHintFallback`; cancelled by `OpenModal`
- `OrganismCardData.cs` — `BALANCE DELTA` block: `deltaText` / `okLabel` / `needsFoodLabel` /
  `huntedOutLabel` / colours, `UpdateDelta()`, `cappedHunted` branch; `using OceanX.BoidsGPU.Ecosystem`
- `EcosystemSimulationGPU.cs` (JunHeng's) — new `GetSpeciesDelta(SpeciesDataGPU)`, cap-aware
- `EcosystemNetworkManagerGPU.cs` (JunHeng's) — `NetworkList<int> _speciesDelta` + getter
  (**networked layout change — rebuild host + tablet together**)
- `TabletAddRemoveUIGPU.cs` (JunHeng's) — `CooldownRemaining` / `CooldownDuration`; cooldown
  folded into `addButton.interactable`; `addCooldown` default 0.3 -> 1.0; `LookUpPrompt.Trigger()`
- `ExhibitReset.cs` (JunHeng's) — `LookUpPrompt.ResetForNewSession()` in `DoLocalReset()`

**Scene changes (2026-07-28, `Assets/Aloysius/Scenes/new netcode 2.unity`):**
- `TabletCanvas (1)` -> new `LookUpPrompt` (cloned from `TapFishNudge`, y=-940)
- `TabletCanvas (1)` -> new `BalanceAdvisor` (bottom-left 48,48, 620x240, BG + Header + Body)
- `Ecosystem Panel/Info/Add` -> new `CooldownSweep` child (`ButtonCooldownOverlay`)
- `HoldFishNudge.showAfterId` -> `tap` changed to `details`
- **Deleted:** `Ecosystem Panel/Health/HealthDelta` (the removed popup)

**Prefab changes (2026-07-28):**
- `Assets/Aloysius/Prefabs/OrganismCard.prefab` -> new `DeltaLabel` child (Rajdhani-Medium 30,
  x=830), wired to `OrganismCardData.deltaText`; `okLabel` set to `ok` (was a tofu checkmark)

**Edited (earlier):**
- `SpeciesBubble.cs` (shared) — TapPunch fix + OnTap routes to the info panel
- `HealthBarBinder.cs` — added `using OceanX.BoidsGPU.Ecosystem;`; ease-out `Update()`

**New assets (2026-07-12):**
- `Assets/Aloysius/New/healt_white.png` — white copy of the eco-health arc (so the
  gauge can tint red); assigned to the fill Image in `new netcode 1`
- `Assets/Aloysius/New/lastring.png` — assigned as the hold-progress ring on all 14
  bubbles in `new netcode 1` (existing sprite, newly wired)

**New assets (earlier session):**
- `Assets/Aloysius/SpeciesData/Macroalgae.asset` (UI species data)
- edited `Seagrass.asset` (registered), `Fringelip_Mullet.asset` /
  `Eyestripe_Surgeonfish.asset` (`addedMessage`)

**Scene changes (copies only):**
- Tablet copy: radial bubbles; atmospheric FX; rings moved into FoodWebLayer; 3-tab bar
  + HintsView; eco-health Dashboard001 + status word; Health panel moved to Ecosystem
  Panel level; Info side panel with DetailRoot + View Details
- `Boids_Demo` copy: AluciaCanvas (+ character, bubble, reveal card); enabled the
  `EcosystemUnlockManagerGPU` component on DebugHarness; **`HealthBarBinder` on
  `Screen UI/healthbar` + removed `TMLineSliderP` from its fill Image**
- Tablet copy (this session): `Ecosystem Panel` -> `EcosystemUnlockManagerGPU._allSpecies`
  += Seagrass, Macroalgae; `macroalgae bubble.data` assigned