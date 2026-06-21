OceanX MP — UI Handoff Update (Aloysius)
Continuation of HANDOFF.md — covers tablet UI work in Netcode Simulation Test scene
What's New Since Last Handoff
Food Web Lines — Fixed
FoodWebLines.cs rewritten. Long-press a bubble now:

Draws lines to its prey/predators lists (assigned per-bubble in Inspector)
Dims all unconnected bubbles via CanvasGroup alpha (0.15)
Restores everything on release
Lines render behind bubbles (Linesmanager sits before Organisms in hierarchy)

Known requirement: each SpeciesBubble's prey/predators lists must be manually assigned in the Inspector — not yet auto-derived from SpeciesDataGPU.PreySpecies/PredatorSpecies.
Lock/Unlock Visual System — Prototype Built, Needs GPU Wiring
Built a placeholder unlock system in SpeciesData.cs + GameState.cs:

12 SpeciesData ScriptableObjects in Assets/Aloysius/SpeciesData/ with hints + requirements (ecologically-grounded prey counts per tier)
GameState.cs tracks counts/unlocked state, auto-unlocks when requirements met

Decision made: removed tap-to-show-hint on locked bubbles — locked bubbles are now fully dimmed and non-interactable (Button.interactable = false). Hints/requirements were decided to live on the large screen instead, not the tablet.
JunHeng has since integrated this — SpeciesBubble.Refresh() now checks EcosystemUnlockManagerGPU.Instance first (real sim-driven unlocks), falling back to GameState only if that manager isn't present. Aloysius's system is now the offline/standalone fallback, not the live path.
Modal Card — Dim Overlay + Add/Remove (Netcode-aware)
ModalController.cs rewritten:

Open(Sprite, int speciesIndex) — speciesIndex = -1 means cosmetic-only card; >= 0 wires Add/Remove to EcosystemNetworkManagerGPU.RequestAddSpeciesRpc/RequestRemoveSpeciesRpc
Population label updates live in Update() from EcosystemNetworkManagerGPU.GetPopulation()
Add/Remove buttons grey out at cap/zero
New: DimOverlay — dark CanvasGroup fades in behind the card on open, fades out on close
Bug fixed: fade coroutine now runs on a separate DimFader.cs component attached to the overlay itself, not on ModalController's own GameObject — previously Close() called SetActive(false) on the modal mid-fade, killing the coroutine instantly instead of animating out

Swipe-to-Close (Info Card)
SwipeToClose.cs — drag the open card in any direction past a threshold to dismiss, slides off-screen in the swipe direction. Fixed a bug where rapid re-tapping during the animation caused the card to jump to the wrong position (now locks originalPos once in Start(), never mutated).
Current Organisms Popup — Built
New FoodWebDragReveal.cs + DimFader.cs:

Swipe down on the arrow handle (top of food web panel) opens a CurrentOrganismsView popup
Popup slides in + fades in, with a separate dim overlay behind it
Close via dedicated Close button (not swipe-back-up)
DragHandle — invisible 220×100 hit zone layered over the small visual arrow, since the arrow's actual sprite is too small to reliably grab

Still placeholder: CurrentOrganismsView's internal content (grid of added species + counts) — only the slide/dim/open/close mechanics are built. No actual organism list UI yet.
Tap Feedback
SpeciesBubble.cs — added a quick scale-punch animation (TapPunch() coroutine) on tap: 1.2x scale up in 0.1s, back down in 0.15s.

Known Issues / Watchpoints (UI-specific, additive to main HANDOFF.md)

GameObjects intermittently vanish from the scene on domain reload — happened repeatedly with CurrentOrganismsView, ModalPanel children, and Lineprefab. Suspect a Lineprefab mis-assignment issue (it briefly had FoodWebLines.cs attached to itself, causing duplicate Instance overwrites) and possibly unsaved scene state before a reload. Save the scene (Ctrl+S) immediately after any MCP-driven hierarchy change.
Script edits via MCP's script_apply_edits add_field/structured ops were unreliable this session — repeatedly reported success but didn't persist. Fallback pattern that worked: delete the script, recreate from scratch with create_script.
GPU boid editor crash — SendShadowCullingCallbacks → PrepareDrawShadowsCommandStep1 crash in Boids_Demo when zooming/interacting with Scene view. Same crash as logged in main HANDOFF's Known Issues (shark + water shader). Not resolved this session — recommend disabling shadows on Directional Light and the boid cameras as a workaround, or guarding BoidSimulationGPU's [ExecuteAlways] update with if (!Application.isPlaying) return;.


What Still Needs Building (UI side)

Current Organisms grid content — species icons + counts inside CurrentOrganismsView, tap to remove (per prototype spec)
First-time "New Species Discovered" reveal card — not started; per prototype, lives on the large screen, not tablet
Tablet → host RPC for unlock notifications — tablet should show a simple toast ("You've unlocked X!"), large screen shows the full reveal card. Requires a ClientRpc from host, which is JunHeng's networking layer — flagged, not built by Aloysius this session
Wire prey/predators lists from SpeciesDataGPU instead of manual per-bubble Inspector assignment, once PreySpecies/PredatorSpecies are finalized on the GPU asset
Eco-health bar visual polish — Health.cs already reads live from EcosystemNetworkManagerGPU (per main HANDOFF), not touched this session

Assets/Aloysius/Scripts/
├── FoodWebLines.cs          rewritten — dim/highlight on long-press
├── SpeciesBubble.cs          tap-punch animation added (JunHeng has since extended further)
├── ModalController.cs        rewritten — dim overlay, fade-safe close
├── DimFader.cs                NEW — fade coroutine isolated from parent SetActive
├── SwipeToClose.cs            bug fix — originalPos no longer drifts
├── FoodWebDragReveal.cs       NEW — Current Organisms popup open/close
├── GameState.cs                NEW — placeholder unlock tracker (fallback path)
└── SpeciesData.cs              NEW — 12 ScriptableObject assets with hints/requirements