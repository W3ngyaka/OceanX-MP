using UnityEngine;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Class that tests the spatial partitioning work flow on the GPU.
    /// </summary>
    public class SpatialPartitionGPUTester : MonoBehaviour
    {
        [Header("References: ")]
        [SerializeField] private SpatialPartitionGPU _spatialPartitionGPU = null;
        [SerializeField] private BoidSpawnerGPU _boidSpawnerGPU = null;

        [Header("Options: ")]
        [SerializeField] private Bounds _simulationAreaBounds = default;
        [SerializeField] private int _boidsCount = 100;

        [Header("Visualization Options: ")]
        [SerializeField] private bool _visualizeOriginalBoids = false;
        [SerializeField] private bool _visualizeSortedBoids = false;

        private BoidInfoGPU[] _boidsInfos = null;
        private BoidInfoGPU[] _sortedBoidsInfos = null;
        private ComputeBuffer _boidsOriginalBuffer = null;
        private ComputeBuffer _boidsSortedBuffer = null;
        private bool _swapBuffers = false;

        private void OnDrawGizmosSelected()
        {
            // Bounds visualization in the scene.
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(_simulationAreaBounds.center, _simulationAreaBounds.size);

#if UNITY_EDITOR
            // Only visualize this in play mode.
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif
            if (_visualizeOriginalBoids)
            {
                VisualizeBoids(ref _boidsInfos, Color.green, 0.15f, true);
            }

            if (_visualizeSortedBoids)
            {
                VisualizeBoids(ref _sortedBoidsInfos, Color.blue, 0.1f);
            }
        }

        private void VisualizeBoids(ref BoidInfoGPU[] boids, Color color, float visualizationSize, bool renderWireSphere = false)
        {
            if (boids == null || boids.Length == 0)
            {
                return;
            }

            // Visualize boids as small green spheres in the scene.
            for (int i = 0; i < _boidsCount; i++)
            {
                //color.a = (i + 1) / (float)_boidsCount;
                Gizmos.color = color;
                if(renderWireSphere)
                {
                    Gizmos.DrawWireSphere(boids[i].Position, visualizationSize);
                }
                else
                {
                    Gizmos.DrawSphere(boids[i].Position, visualizationSize);
                }
            }
        }

        private void Start()
        {
            // First, initialize the grid.
            _spatialPartitionGPU.InitializeGrid(_simulationAreaBounds, _boidsCount);

            // Then, spawn the boids inside the simulation area.
            _boidSpawnerGPU.SpawnBoids(_simulationAreaBounds);
            _boidsInfos = _boidSpawnerGPU.Boids;

            // Next, initialize the compute buffers that will hold the boids.
            _boidsOriginalBuffer = new ComputeBuffer(_boidsCount, BoidInfoGPU.Size);
            _boidsSortedBuffer = new ComputeBuffer(_boidsCount, BoidInfoGPU.Size);
            _boidsOriginalBuffer.SetData(_boidsInfos);
            _boidsSortedBuffer.SetData(_boidsInfos);

            // Then, dispatch one iteration of the boid distribution algorithm.
            _spatialPartitionGPU.UpdateGridOccupancy(_boidsOriginalBuffer, _boidsSortedBuffer);
            _swapBuffers = true;
        }

        private void Update()
        {
            // Then, dispatch one iteration of the boid distribution algorithm.
            ComputeBuffer boidsSourceBuffer = _swapBuffers ? _boidsSortedBuffer : _boidsOriginalBuffer;
            ComputeBuffer boidsDestinationBuffer = _swapBuffers ? _boidsOriginalBuffer : _boidsSortedBuffer;
            _spatialPartitionGPU.UpdateGridOccupancy(boidsSourceBuffer, boidsDestinationBuffer);
            _swapBuffers = !_swapBuffers;

            if (_visualizeSortedBoids)
            {
                // Finally, fetch the data to check the sort of the boids.
                _sortedBoidsInfos = new BoidInfoGPU[_boidsCount];
                _boidsSortedBuffer.GetData(_sortedBoidsInfos);
            }
        }

        private void OnDestroy()
        {
            CleanUpComputeBuffer(ref _boidsOriginalBuffer);
            CleanUpComputeBuffer(ref _boidsSortedBuffer);
        }

        private void CleanUpComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            computeBuffer?.Release();
            computeBuffer?.Dispose();
            computeBuffer = null;
        }
    }
}