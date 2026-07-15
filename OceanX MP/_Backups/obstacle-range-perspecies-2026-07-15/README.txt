Backup taken 2026-07-15 before wiring per-species obstacle avoidance range.
Pre-edit version from git HEAD (59eb0f8).

File: Assets/Junheng/Shaders/Compute/Boids_GPU_Spatial_Partition/BoidsGPU_Spatial_Partition.compute

Why: FishSchoolProperties.ObstacleAvoidanceRange was squared, uploaded to the GPU
(BoidSimulationBaseGPU.cs:314) and exposed on the struct as
obstacleAvoidanceRangeSquared -- but the kernel never read it. It used a hardcoded
#define OBSTACLE_AVOID_MARGIN 2.0 for every species. Benthic species (ray, moray)
were being shoved upward off the seabed because 257 of the 381 obstacle affecters
are centred below y=4, and the escape heading is blended at strength 5.0.

Change: the kernel now reads the per-species range. To keep this behaviour-
preserving, every species' ObstacleAvoidanceRange was set to 2.0 (== the old
hardcoded value) EXCEPT the ray and moray, which drop to 0.75 so they can hug
the sand.

Also changed (separate files, tracked by git):
  - all *_SchoolProperties.asset : ObstacleAvoidanceRange
  - ray *_Data.asset            : PreferredDepthBand -> (0, 0.06)
  - EcosystemSimulationGPU.cs   : _pathVerticalClearance default 1.5 -> 0.5
  - SCENE_MainScene.unity       : _pathVerticalClearance on the component -> 0.5

NOT fixed here (deliberate, deserves its own pass): AffecterAwayDirection points
away from an obstacle's CENTRE while AffecterSurfaceDistance measures to its
SURFACE. A fish beside a wide flat coral is pushed up rather than sideways.

To revert this file:
  git checkout -- "OceanX MP/Assets/Junheng/Shaders/Compute/Boids_GPU_Spatial_Partition/BoidsGPU_Spatial_Partition.compute"

NOTE: this folder is outside Assets/ so Unity does not import it.
