#ifndef SPATIAL_PARTITION_UTILS
#define SPATIAL_PARTITION_UTILS

// Structure defining cell occupancy information for one boid.
// It contains the ID of the cell that the boid is located in as well as 
// its order inside the cell (0-based). 
struct BoidCellInfo 
{
    uint CellID;
    uint OrderInsideCell;
};

int _TotalCellCount;
int _CellCountX;
int _CellCountY;
int _CellCountZ;
float _CellSize;
float _InverseCellSize;
// Minimmum point of the whole grid (center - extents). Used for calculating
// the local position of each boid inside the grid.
float3 _GridMinPoint;

// Function calculates the ID of the cell that the provided world position would belong to.
// The specified position should be located inside the bounds of the grid, but the function
// will clamp its value to the closest cell on the grid never the less. 
uint GetCellID(float3 worldPosition) 
{
    float3 localPosition = worldPosition - _GridMinPoint;

    // Calculate the raw cell index based on the local position in the grid.
    int cellIndexX = floor(localPosition.x * _InverseCellSize);
    int cellIndexY = floor(localPosition.y * _InverseCellSize);
    int cellIndexZ = floor(localPosition.z * _InverseCellSize);

    // Clamp indices to keep them within grid bounds.
    cellIndexX = clamp(cellIndexX, 0, _CellCountX - 1);
    cellIndexY = clamp(cellIndexY, 0, _CellCountY - 1);
    cellIndexZ = clamp(cellIndexZ, 0, _CellCountZ - 1);

    // Precompute offsets for the row and the depth layer of the grid, to roll out the grid in 1D.
    int cellCountPerRow = _CellCountX;
    int cellCountPerSlice = _CellCountX * _CellCountY;

    // Calculate and return the final cell ID.
    return cellCountPerSlice * cellIndexZ + cellCountPerRow * cellIndexY + cellIndexX;
}

#endif // SPATIAL_PARTITION_UTILS