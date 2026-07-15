Backup taken 2026-07-15 before making entry-gate choice depth-aware.
Pre-edit version from git HEAD (59eb0f8), i.e. WITH depth bands but WITHOUT
species-aware entry gates.

File: Assets/Junheng/Scripts/Boids_GPU/Ecosystem/EcosystemSimulationGPU.cs

Changes made:
  1. New field _entryMarkerDepthTolerance (default 3).
  2. PickEntryMarker() now takes the SpeciesDataGPU and prefers entry markers
     sitting at that species' PreferredDepthBand, so rays enter via the seabed
     gates and scad via the high ones. No ray-specific special case.
  3. New helper GetPreferredDepthRange() - single source of truth resolving a
     species' band into world Y. Shared by ConfigureAnimator (roaming height)
     and PickEntryMarker (gate choice), so a school enters at the depth it
     will then roam at.

To revert: copy this file back over the path above (leave the .meta alone),
or: git checkout -- "OceanX MP/Assets/Junheng/Scripts/Boids_GPU/Ecosystem/EcosystemSimulationGPU.cs"

NOTE: this folder is outside Assets/ so Unity does not import it.
