Backup taken 2026-07-15 before the yellowstripe scad schooling fix.
Branch at time of backup: combine-2 (these 4 files were clean/committed).

Original copies of:
  Assets/Junheng/Shaders/Compute/Boids_GPU_Spatial_Partition/BoidsGPU_Spatial_Partition.compute
  Assets/Junheng/Shaders/Compute/BoidSimulationData.hlsl
  Assets/Junheng/Scripts/Boids_GPU/BoidSpawnerGPU.cs
  Assets/Junheng/Scripts/Boids_GPU/BoidInfoGPU.cs

To restore, copy these back over the paths above (keep the existing .meta files
in Assets/ - they are not backed up here and must not be replaced).

Changes made:
  1. compute: cellOffsetZ used _CellCountX * _CellCountZ, should be * _CellCountY
     (wrong Z stride -> fish only saw neighbours in their own 8m Z-slab).
  2. compute: cohesion/alignment averaged by neighborsCount but summed neighborsCount+1
     samples (self included) -> centroid scaled about the world origin.
  3. compute + spawner: entry sprint re-armed on ANY out-of-bounds boid; now armed once
     at spawn via a -1 sentinel so only genuinely-entering fish sprint.

NOTE: this folder is outside Assets/ so Unity does not import it.
