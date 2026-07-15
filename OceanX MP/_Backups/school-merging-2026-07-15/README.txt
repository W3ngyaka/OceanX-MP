Backup taken 2026-07-15 before adding school merging / mixed-species shoaling.

Feature being added:
  SpeciesDataGPU gains AllowSchoolMerging (same-species) and AllowMixedSpeciesSchooling
  (cross-species, rarer), each with their own chance values, plus MergeDistance and a
  ShoalsWith list.

Design: BoidID's bits 8-15 (previously a PER-SPECIES sub-group index) become a GLOBALLY
unique flock ID. Bits 0-7 (species) are untouched and keep doing their real jobs: indexing
_BoidSchools[] for physics, and encoding body-size order for separation. A mixed shoal is
then just two schools of different species sharing a flock ID - each fish keeps its own
speed/size, they simply flock together. No struct/stride change.

Each species reserves a contiguous block of MaxSchools flock IDs (69 total across all
species, 8 bits gives 255, so there is headroom).

Decisions taken (confirmed by the user):
  - Removing a merged school SPLITS it first; removal always acts on a natural school.
  - Merge state is NOT persisted across rebuilds. Adding/removing a school resets that
    species' shoals. Acceptable because _enablePopulationDynamics is off, so rebuilds
    only happen on an explicit click.

To revert: copy these back over their original paths (leave the .meta files alone), or
git checkout the paths listed. NOTE EcosystemSimulationGPU.cs already had uncommitted
entry-gate changes when this backup was taken - this copy INCLUDES those.

NOTE: this folder is outside Assets/ so Unity does not import it.
