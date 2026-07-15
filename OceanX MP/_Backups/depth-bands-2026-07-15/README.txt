Backup taken 2026-07-15 before adding per-species depth bands.
These are the pre-edit versions, taken from git HEAD (branch combine-2).

Files backed up here:
  Assets/Junheng/Scripts/Boids_GPU/Ecosystem/EcosystemSimulationGPU.cs
  Assets/Junheng/Scripts/Boids_GPU/Ecosystem/SpeciesDataGPU.cs

Changes made:
  1. SpeciesDataGPU: new field  public Vector2 PreferredDepthBand = (0,1)
     Fraction of the simulation bounds height (0 = floor/sand, 1 = ceiling).
  2. EcosystemSimulationGPU: new field _pathVerticalClearance (default 1.5).
     Vertical-only inset, separate from _pathBoundsSafeZone (3m). The 3m safe
     zone is needed for a path's HORIZONTAL extent but would have made the
     seabed unreachable for benthic species.
  3. EcosystemSimulationGPU.ConfigureAnimator: a school's swim height now comes
     from its species' PreferredDepthBand instead of a random height across the
     whole water column.

The 12 *_Data.asset files also changed (one new PreferredDepthBand field each).
They are tracked by git, so `git diff` / `git checkout` reverts them.

To revert everything in this change:
  git checkout -- "OceanX MP/Assets/Junheng/Scripts/Boids_GPU/Ecosystem/" \
                  "OceanX MP/Assets/Junheng/Data/Fish/"
CAUTION: that would also revert any other uncommitted edits under those paths.

NOTE: this folder is outside Assets/ so Unity does not import it.

UPDATE 2026-07-15: this change was since committed as 4c96ace "Adjusted depth
banding for each species", so git already holds the before/after. An earlier
note here claimed Bluespotted ribbontail ray_MovementProperties.asset had been
modified -- that was stale git stat info; the file is identical to HEAD and was
never touched.
