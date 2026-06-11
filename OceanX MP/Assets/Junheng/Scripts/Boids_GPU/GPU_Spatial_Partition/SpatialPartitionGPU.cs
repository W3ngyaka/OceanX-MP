using System;
using UnityEngine;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Component that implements spatial partition of a bounds area into a grid of cells 
    /// and disperses boids into corresponding cell based on their location. The whole
    /// algorithm is executed on the GPU.
    /// </summary>
    public class SpatialPartitionGPU : MonoBehaviour
    {
        /// <summary>
        /// Structure containing information about the cell that a certain boid belongs to.
        /// It contains the ID of the cell and the order inside the cell that will be used to
        /// sort the boids.
        /// </summary>
        [Serializable]
        public struct BoidCellInfo
        {
            public const int Size = sizeof(uint) * 2;

            public uint CellID;
            public uint OrderInsideCell;
        }

        private const int THREAD_GROUP_SIZE = 1024; 

        [Header("References: ")]
        [SerializeField] private ComputeShader _spatialPartitionComputeShader = null;

        [Header("Options: ")]
        [Tooltip("Number of cells that will be added to each dimension in the grid " +
            "to handle cases of fish exiting the simulation bounds a bit.")]
        [SerializeField] private int _safetyBufferCellCount = 4;
        [Tooltip("Size of each cell in the grid, in meters.")]
        [SerializeField] private float _cellSize = 2f;

        [Header("Visualization: ")]
        [SerializeField] private bool _visualizeOccupancy = true;
        [SerializeField] private Color _occupancyVisualizationColor = Color.red;

        // Data that shouldn't be set from the inspector. It's just serialized for visualization of values.
        [Header("Debug Visualization Only: ")]
        [Tooltip("Number of cells in the grid across the X-Axis.")]
        [SerializeField] private int _cellCountX = 0;
        [Tooltip("Number of cells in the grid across the Y-Axis.")]
        [SerializeField] private int _cellCountY = 0;
        [Tooltip("Number of cells in the grid across the Z-Axis.")]
        [SerializeField] private int _cellCountZ = 0;
        [Space]
        [Tooltip("World position of the center of the grid.")]
        [SerializeField] private Vector3 _gridCenter = Vector3.zero;
        [Tooltip("Minimal point of the grid, used to convert the world position of " +
            "each boid into the local position in the grid so that it could be converted to " +
            "the correct cell ID.")]
        [SerializeField] private Vector3 _gridMinPoint = Vector3.zero;
        [Tooltip("Total size of the grid in each dimension.")]
        [SerializeField] private Vector3 _gridSize = Vector3.zero;

        private int _totalBoidsCount;
        private int _totalCellsCount;
        // Total number of thread group blocks that will be dispatched in the compute shader.
        // Each block contains THREAD_GROUP_SIZE number of threads.
        private int _totalThreadGroupsCount;

        // Cached IDs of kernels in the compute shader.
        private int _clearGridKernelID;
        private int _updateOccupancyKernelID;
        private int _calculateOffsetsKernelID;
        private int _sumOffsetsKernelID;
        private int _finalizeOffsetsKernelID;
        private int _rearrangeBoidsKernelID;

        /// <summary>
        /// Compute buffer that, for each boid, holds the ID of the cell and the order of the boid inside the cell.
        /// This information is stored as an uint 2-component vector for each boid. 
        /// <br>Example: </br>
        /// <br>(25, 3) --> This boid is located inside the cell with ID 25 and it is the third boid inside this cell.</br>
        /// </summary>
        private ComputeBuffer _boidsCellInfoBuffer = null;
        /// <summary>
        /// Buffer containing the number of boids for each cell in the grid.
        /// </summary>
        private ComputeBuffer _cellOccupancyBuffer = null;
        /// <summary>
        /// Buffer containing the offset of the last boid in the cell, for each cell in the grid.
        /// The offset is global, meaning that it can be used to sort the boids array. Note, that in order
        /// to get the index of the first boid in a certain cell, you need to fetch the offset of the neighboring
        /// cell located before that one. 
        /// <br>Example of cell offsets:</br>
        /// <br>Let's say there is a grid of 3 cells, with corresponding number of boids inside each cell being (1, 4, 2).</br>
        /// <br>The global offsets buffer would contain these values: (1, 5, 7).</br>
        /// <br>This means that is you want the INDEX of the first boid inside the SECOND cell, you would need to fetch the global offset of the FIRST cell, 
        /// since the index is 0-based and each cell contains the total boid count.</br>
        /// </summary>
        private ComputeBuffer _cellsGlobalOffsetBuffer = null;
        /// <summary>
        /// Compute buffer holding total offsets sums for each thread group. 
        /// This is used only as a helper buffer to calculate the final global offsets for each cell.
        /// </summary>
        private ComputeBuffer _threadGroupsGlobalOffsetBuffer = null;
        /// <summary>
        /// Compute buffer holding total offsets sums for each thread group. 
        /// This is used only as a helper buffer to calculate the final global offsets for each cell.
        /// </summary>
        private ComputeBuffer _threadGroupsGlobalOffsetHelperBuffer = null;


        private uint[] _cellsOccupancyVisualization = null;

        private void OnDrawGizmosSelected()
        {
            // Visualization of the grid.
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_gridCenter, _gridSize);

            if(!_visualizeOccupancy)
            {
                return;
            }

            if(_cellsOccupancyVisualization == null || _cellsOccupancyVisualization.Length == 0)
            {
                return;
            }

            // Calculate the max number of boids in one cell (for coloring purpose).
            Color cellColor = _occupancyVisualizationColor;
            uint maxNumberOfBoidsInCell = 1;
            for(int i = 0; i < _totalCellsCount; i++)
            {
                uint numberOfBoidsInThisCell = _cellsOccupancyVisualization[i];
                if(numberOfBoidsInThisCell > maxNumberOfBoidsInCell)
                {
                    maxNumberOfBoidsInCell = numberOfBoidsInThisCell;
                }
            }

            // For each cell, visualize its occupancy via color intensity.
            for(int i = 0; i < _totalCellsCount; i++)
            {                
                uint numberOfBoidsInThisCell = _cellsOccupancyVisualization[i];
                if(numberOfBoidsInThisCell <= 0)
                {
                    continue;
                }

                int cellIndexX = i % _cellCountX;
                int cellIndexY = Mathf.FloorToInt(i / (float)_cellCountX) % _cellCountY;
                int cellIndexZ = Mathf.FloorToInt(i / ((float)_cellCountX * _cellCountY)) % _cellCountZ;
                Vector3 cellLocalPosition = new Vector3((cellIndexX + 0.5f) * _cellSize, (cellIndexY + 0.5f) * _cellSize, (cellIndexZ + 0.5f) * _cellSize);
                
                cellColor.a = Mathf.Lerp(0.5f, 1.0f, Mathf.Clamp01(numberOfBoidsInThisCell / (float)maxNumberOfBoidsInCell));
                Gizmos.color = cellColor;

                Gizmos.DrawCube(cellLocalPosition + _gridMinPoint, Vector3.one * _cellSize);
            }
        }

        private void OnDestroy()
        {
            CleanUpComputeBuffer(ref _boidsCellInfoBuffer);
            CleanUpComputeBuffer(ref _cellOccupancyBuffer);
            CleanUpComputeBuffer(ref _cellsGlobalOffsetBuffer);
            CleanUpComputeBuffer(ref _threadGroupsGlobalOffsetBuffer);
            CleanUpComputeBuffer(ref _threadGroupsGlobalOffsetHelperBuffer);
        }

        /// <summary>
        /// Function initializes the grid and calculates the number of cells that will be required for partitioning the boids
        /// into cells during simulation. Also, the function initializes the required compute buffers for execution on the GPU.
        /// </summary>
        /// <param name="simulationAreaBounds"><see cref="Bounds"/> representing the simulation area for the boids simulation algorithm.</param>
        /// <param name="cellSize">Float representing the size of each cell of the grid. The size of the cell is the same in every direction.</param>
        /// <param name="totalBoidsCount">Total number of boids in the simulation.</param>
        public void InitializeGrid(Bounds simulationAreaBounds, int totalBoidsCount)
        {
            if(_boidsCellInfoBuffer != null)
            {
                // The grid has already been initialized, don't do it again.
                Debug.LogError("The grid has already been initialized, no need to call this function more than once!");
                return;
            }

            _gridCenter = simulationAreaBounds.center;
            _totalBoidsCount = totalBoidsCount;

            // First, calculate the required number of cells that the grid will be split into, with additional
            // safety buffer placed around each dimension of the grid to handle edge cases when boids exit the simulation area a bit.
            // Additionally, their position will be clamped to the expanded simulation area just to be fully sure.
            Vector3 simulationAreaSize = simulationAreaBounds.size;
            simulationAreaSize += Vector3.one * _safetyBufferCellCount * _cellSize;
            _gridSize = simulationAreaSize;
            _cellCountX = Mathf.CeilToInt(simulationAreaSize.x / _cellSize);
            _cellCountY = Mathf.CeilToInt(simulationAreaSize.y / _cellSize);
            _cellCountZ = Mathf.CeilToInt(simulationAreaSize.z / _cellSize);

            // Calculate the minimum simulation area point that will be used to convert boid positions
            // from world space to local space of the simulation area.
            _gridMinPoint = simulationAreaBounds.center - simulationAreaSize * 0.5f;

            _totalCellsCount = _cellCountX * _cellCountY * _cellCountZ;
            _totalThreadGroupsCount = Mathf.CeilToInt(_totalCellsCount / (float)THREAD_GROUP_SIZE);

            // Initialization of the required compute buffers.
            // Mathf.Max(1, ...) so the per-boid buffer is never zero-sized when the ocean is empty.
            // _totalBoidsCount stays honest (may be 0); UpdateGridOccupancy early-returns in that case
            // so the placeholder element is never dispatched.
            _boidsCellInfoBuffer = new ComputeBuffer(Mathf.Max(1, totalBoidsCount), BoidCellInfo.Size);
            _cellOccupancyBuffer = new ComputeBuffer(_totalCellsCount, sizeof(uint));
            _cellsGlobalOffsetBuffer = new ComputeBuffer(_totalCellsCount, sizeof(uint));
            _threadGroupsGlobalOffsetBuffer = new ComputeBuffer(_totalThreadGroupsCount, sizeof(uint));
            _threadGroupsGlobalOffsetHelperBuffer = new ComputeBuffer(_totalThreadGroupsCount, sizeof(uint));

            // Initialize kernel IDs of each required kernel from the compute shader.
            _clearGridKernelID = _spatialPartitionComputeShader.FindKernel("ClearGrid");
            _updateOccupancyKernelID = _spatialPartitionComputeShader.FindKernel("UpdateCellOccupancy");
            _calculateOffsetsKernelID = _spatialPartitionComputeShader.FindKernel("CalculateOffsets");
            _sumOffsetsKernelID = _spatialPartitionComputeShader.FindKernel("SumOffsets");
            _finalizeOffsetsKernelID = _spatialPartitionComputeShader.FindKernel("FinalizeOffsets");
            _rearrangeBoidsKernelID = _spatialPartitionComputeShader.FindKernel("ReArrangeBoids");

            // Send the required values to the compute shader.
            _spatialPartitionComputeShader.SetInt("_BoidsCount", _totalBoidsCount);
            _spatialPartitionComputeShader.SetInt("_TotalCellCount", _totalCellsCount);
            _spatialPartitionComputeShader.SetInt("_CellCountX", _cellCountX);
            _spatialPartitionComputeShader.SetInt("_CellCountY", _cellCountY);
            _spatialPartitionComputeShader.SetInt("_CellCountZ", _cellCountZ);
            _spatialPartitionComputeShader.SetInt("_TotalThreadGroupsCount", _totalThreadGroupsCount);
            _spatialPartitionComputeShader.SetFloat("_CellSize", _cellSize);
            _spatialPartitionComputeShader.SetFloat("_InverseCellSize", 1.0f / _cellSize);
            _spatialPartitionComputeShader.SetVector("_GridMinPoint", _gridMinPoint);

            // Clear grid kernel required references.
            _spatialPartitionComputeShader.SetBuffer(_clearGridKernelID, "_CellOccupancyBuffer", _cellOccupancyBuffer);

            // Update occupancy kernel required references.
            _spatialPartitionComputeShader.SetBuffer(_updateOccupancyKernelID, "_BoidsCellInfoBuffer", _boidsCellInfoBuffer);
            _spatialPartitionComputeShader.SetBuffer(_updateOccupancyKernelID, "_CellOccupancyBuffer", _cellOccupancyBuffer);

            // Offsets calculation kernel required references.
            _spatialPartitionComputeShader.SetBuffer(_calculateOffsetsKernelID, "_CellOccupancyBuffer", _cellOccupancyBuffer);
            _spatialPartitionComputeShader.SetBuffer(_calculateOffsetsKernelID, "_CellsOffsetsBuffer", _cellsGlobalOffsetBuffer);
            _spatialPartitionComputeShader.SetBuffer(_calculateOffsetsKernelID, "_ThreadGroupsGlobalOffsetsBuffer", _threadGroupsGlobalOffsetBuffer);

            // Adding reference to the output buffer for the finalization of offsets kernel. The input buffer will be set dynamically
            // each update, based on the number of ping-pong operations.
            _spatialPartitionComputeShader.SetBuffer(_finalizeOffsetsKernelID, "_CellsOffsetsBuffer", _cellsGlobalOffsetBuffer);

            // Assigning required buffers to the re-arrange boids kernel. The boids output compute buffer will be linked dynamically upon execution.
            _spatialPartitionComputeShader.SetBuffer(_rearrangeBoidsKernelID, "_BoidsCellInfoBuffer", _boidsCellInfoBuffer);
            _spatialPartitionComputeShader.SetBuffer(_rearrangeBoidsKernelID, "_CellsOffsetsBuffer", _cellsGlobalOffsetBuffer);
        }

        /// <summary>
        /// Function updates the current cell occupancy for each boid. Based on that occupancy it
        /// sorts the boids inside the <paramref name="boidsOutputBuffer"/> so that they can be 
        /// quickly fetched by the GPU when iterating one by one.
        /// </summary>
        /// <param name="boidsInputBuffer">Reference to the <see cref="ComputeBuffer"/> containing information
        /// about each boid (position, rotation, speed, etc.) that will be used to sort them into cells.</param>
        /// <param name="boidsOutputBuffer">Reference to the <see cref="ComputeBuffer"/> that will hold the 
        /// sorted boids information, based on their cell location. This buffer will be suitable for usage when
        /// checking distance between neighboring boids since they will be nicely sequentially aligned in the memory.</param>
        /// <param name="boidCount">Total number of boids in the simulation.</param>
        public void UpdateGridOccupancy(ComputeBuffer boidsInputBuffer, ComputeBuffer boidsOutputBuffer)
        {
            if (_boidsCellInfoBuffer == null)
            {
                Debug.LogError("The grid hasn't been initialized, can't execute update!");
                return;
            }

            // Empty ocean: no boids to sort. Skip all dispatches (the per-boid buffer is a size-1
            // placeholder and must not be dispatched). Callers also early-return upstream, but guard here too.
            if (_totalBoidsCount <= 0)
            {
                return;
            }

            // First, for each cell, reset the number of boids in the cell to 0.
            _spatialPartitionComputeShader.DispatchThreads(_clearGridKernelID, _totalCellsCount);

            // Next, for each boid, calculate its cell ID based on its position. Based on that cell ID,
            // update the number of boids in each cell.
            _spatialPartitionComputeShader.SetBuffer(_updateOccupancyKernelID, "_Boids", boidsInputBuffer);
            _spatialPartitionComputeShader.DispatchThreads(_updateOccupancyKernelID, _totalBoidsCount);

            // DEBUG VISUALIZATION: For each cell, output the number of boids inside it.
            if(_visualizeOccupancy)
            {
                if (_cellsOccupancyVisualization == null)
                {
                    _cellsOccupancyVisualization = new uint[_totalCellsCount];
                }
                _cellOccupancyBuffer.GetData(_cellsOccupancyVisualization);
            }

            // Now, for each boid, we know the cell they belong to, and their order in that cell.
            // Also, we know the number of boids in each cell. With that, we begin the boids sorting 
            // process by implementing the prefix-sum algorithm on the GPU. For each cell, we calculate
            // the sum of total number of boids before that cell and boids inside that cell. Note that the 
            // result is local for each thread group.
            _spatialPartitionComputeShader.DispatchThreads(_calculateOffsetsKernelID, _totalCellsCount);

            // Now, we need to unify the local cell offsets on a global level for all cells. We do that by 
            // using a ping-pong technique of swapping buffers between dispatches.
            bool readDataFromHelperBuffer = false;
            for(int completedThreadGroupsCount = 1;  completedThreadGroupsCount < _totalThreadGroupsCount; completedThreadGroupsCount *= 2)
            {
                // Set the correct buffers for reading/writing the data.
                _spatialPartitionComputeShader.SetBuffer(_sumOffsetsKernelID, "_ThreadGroupsGlobalOffsetsHelperBuffer", readDataFromHelperBuffer ? _threadGroupsGlobalOffsetHelperBuffer : _threadGroupsGlobalOffsetBuffer);
                _spatialPartitionComputeShader.SetBuffer(_sumOffsetsKernelID, "_ThreadGroupsGlobalOffsetsBuffer", readDataFromHelperBuffer ? _threadGroupsGlobalOffsetBuffer : _threadGroupsGlobalOffsetHelperBuffer);
                _spatialPartitionComputeShader.SetInt("_CompletedTreadGroupsCount", completedThreadGroupsCount);

                // Dispatch the buffer that will sum the local offsets of each thread group.
                _spatialPartitionComputeShader.DispatchThreads(_sumOffsetsKernelID, Mathf.CeilToInt(_totalThreadGroupsCount / (float)THREAD_GROUP_SIZE));

                // Swap the input and output buffers.
                readDataFromHelperBuffer = !readDataFromHelperBuffer;
            }

            // Finally, dispatch the compute shader that will add the sums of each thread group to each thread inside that group.
            // Or in other words, apply group offsets to each cell so it has a correct global offset. Using the helper buffer to hold the input data since
            // we don't have it declared as read/write which will make it more optimized.
            _spatialPartitionComputeShader.SetBuffer(_finalizeOffsetsKernelID, "_ThreadGroupsGlobalOffsetsHelperBuffer", readDataFromHelperBuffer ? _threadGroupsGlobalOffsetHelperBuffer : _threadGroupsGlobalOffsetBuffer);
            _spatialPartitionComputeShader.DispatchThreads(_finalizeOffsetsKernelID, _totalCellsCount);

            // The last thing left to do is to re-order the boids in a more memory neatly way for faster access by the boid simulation compute.
            _spatialPartitionComputeShader.SetBuffer(_rearrangeBoidsKernelID, "_Boids", boidsInputBuffer);
            _spatialPartitionComputeShader.SetBuffer(_rearrangeBoidsKernelID, "_SortedBoids", boidsOutputBuffer);
            _spatialPartitionComputeShader.DispatchThreads(_rearrangeBoidsKernelID, _totalBoidsCount);
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Releases all spatial partition compute buffers and resets visualisation state so that
        /// InitializeGrid() can be safely called again after a buffer rebuild.
        /// </summary>
        public void CleanupGrid()
        {
            CleanUpComputeBuffer(ref _boidsCellInfoBuffer);
            CleanUpComputeBuffer(ref _cellOccupancyBuffer);
            CleanUpComputeBuffer(ref _cellsGlobalOffsetBuffer);
            CleanUpComputeBuffer(ref _threadGroupsGlobalOffsetBuffer);
            CleanUpComputeBuffer(ref _threadGroupsGlobalOffsetHelperBuffer);
            _cellsOccupancyVisualization = null;
        }

        public void SetSpatialPartitionProperties(ComputeShader computeShader, int kernelID)
        {
            computeShader.SetBuffer(kernelID, "_CellsOffsetsBuffer", _cellsGlobalOffsetBuffer);
            computeShader.SetInt("_TotalCellCount", _totalCellsCount);
            computeShader.SetInt("_CellCountX", _cellCountX);
            computeShader.SetInt("_CellCountY", _cellCountY);
            computeShader.SetInt("_CellCountZ", _cellCountZ);
            computeShader.SetFloat("_CellSize", _cellSize);
            computeShader.SetFloat("_InverseCellSize", 1.0f / _cellSize);
            computeShader.SetVector("_GridMinPoint", _gridMinPoint);
        }

        private void CleanUpComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            computeBuffer?.Release();
            computeBuffer?.Dispose();
            computeBuffer = null;
        }
    }
}