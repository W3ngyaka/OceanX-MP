using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OceanX.BoidSpawnUtility;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Component that initializes boids inside the simulation area at random positions. 
    /// The initialized boids aren't actually being spawned in the scene, they're just
    /// a bunch of structures filled with data about the position, rotation, speed of each boid
    /// that are then transferred to the GPU for simulation.
    /// </summary>
    public class BoidSpawnerGPU : BoidSpawnerBase
    {
        protected BoidInfoGPU[] _boids = null;
        // Offset in the final boids compute buffer for this fish species.
        protected int _renderingOffset = 0;
        protected GraphicsBuffer _drawArgumentsBuffer = null;
        protected GraphicsBuffer.IndirectDrawIndexedArgs[] _drawArguments = null;

        /// <summary>
        /// Reference to the collection of all <see cref="BoidInfoGPU"/> structures that were
        /// initialized by this boid spawner.
        /// </summary>
        public BoidInfoGPU[] Boids { get => _boids; }

        protected virtual void OnDestroy()
        {
            _drawArgumentsBuffer?.Dispose();
            _drawArgumentsBuffer?.Release();
            _drawArgumentsBuffer = null;
            _drawArguments = null;
        }

        /// <summary>
        /// Function initializes structures filled with initial information for each boid. The 
        /// position of each boid is clamped inside the provided <paramref name="simulationAreaBounds"/>.
        /// </summary>
        /// <param name="simulationAreaBounds">Bounds of the simulation inside which the boids will be instantiated.</param>
        public override void SpawnBoids(Bounds simulationAreaBounds)
        {
            InitializeBoidsSpawnData(simulationAreaBounds);

            // Initialize the draw arguments buffer for indirect issuing of draw calls for rendering fish.
            _drawArgumentsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _drawArguments = new GraphicsBuffer.IndirectDrawIndexedArgs[1]
            {
                new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    baseVertexIndex = 0,
                    startIndex = 0,
                    startInstance= 0,
                    indexCountPerInstance = _boidSpawnData.BoidMesh.GetIndexCount(0),
                    instanceCount = (uint)_boidSpawnData.BoidsCount
                }
            };
            _drawArgumentsBuffer.SetData(_drawArguments);
        }

        /// <summary>
        /// Function caches the rendering offset of this fish species. This offset represents
        /// the offset of sampling the final boids buffer in order to fetch the appropriate data
        /// for this fish species on the GPU.
        /// </summary>
        /// <param name="renderingOffset">Offset of this fish species on the final boids buffer on the GPU.</param>
        public void SetRenderingOffset(int renderingOffset)
        {
            _renderingOffset = renderingOffset;
        }

        /// <summary>
        /// Function should issue a draw call on the GPU using the static mesh instancing method to
        /// render the boids from this spawn group. The data for the position, speed, and rotation 
        /// of each boid instance is contained inside the <paramref name="boidsComputeBuffer"/>.
        /// </summary>
        /// <param name="boidsComputeBuffer">Reference to the compute buffer holding the data
        /// for all boids that should be used when rendering them.</param>
        /// <param name="simulationAreaBounds">Bounds representing the simulation area of the boids.</param>
        public void RenderBoids(ComputeBuffer boidsComputeBuffer, ComputeBuffer boidSchoolsRenderInfosBuffer, Bounds simulationAreaBounds)
        {
            RenderParams renderParams = new RenderParams(_boidSpawnData.BoidMaterial);
            renderParams.matProps = new MaterialPropertyBlock();
            renderParams.matProps.SetBuffer("_Boids", boidsComputeBuffer);
            renderParams.matProps.SetBuffer("_BoidsRenderInfos", boidSchoolsRenderInfosBuffer);
            renderParams.matProps.SetInt("_BoidsBufferOffset", _renderingOffset);
            renderParams.matProps.SetVector("_SimulationAreaCenter", simulationAreaBounds.center);
            renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderParams.receiveShadows = false;
            renderParams.layer = gameObject.layer;
            // Increase the culling area a bit so that some fish don't randomly 
            // disappear when they get close to the edges of the simulation area.
            simulationAreaBounds.extents *= 1.25f;
            renderParams.worldBounds = simulationAreaBounds;

            Graphics.RenderMeshIndirect(renderParams, _boidSpawnData.BoidMesh, _drawArgumentsBuffer);
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Releases the draw arguments buffer and clears the cached boid array so that
        /// SpawnBoids() can be called again cleanly during a buffer rebuild.
        /// </summary>
        public void CleanupSpawnData()
        {
            _drawArgumentsBuffer?.Dispose();
            _drawArgumentsBuffer?.Release();
            _drawArgumentsBuffer = null;
            _boids = null;
        }

        /// <inheritdoc/>
        protected override void UpdateBoidGroupId(int boidGroupId)
        {
            for (int i = 0; i < _boids.Length; i++)
            {
                BoidInfoGPU BoidInformation = _boids[i];
                int currentBoidID = BitConverter.SingleToInt32Bits(BoidInformation.BoidID);
                // Keeping the sub-group ID intact while updating the group ID.
                int newBoidID = (currentBoidID & 0xFF00) | (boidGroupId & 0xFF);
                BoidInformation.BoidID = BitConverter.Int32BitsToSingle(newBoidID);
                _boids[i] = BoidInformation;
            }
        }

        /// <summary>
        /// Function calculates the initial positions and rotations for each boid. The initial position and rotation
        /// of each boid is ensured to be inside the <paramref name="simulationAreaBounds"/>.
        /// </summary>
        /// <param name="simulationAreaBounds">Bounds specifying area where the boids simulation will take place.</param>
        protected virtual void InitializeBoidsSpawnData(Bounds simulationAreaBounds)
        {
            FishSchoolProperties schoolProperties = _boidSpawnData.FishSchoolProperties;
            int totalBoidsCount = _boidSpawnData.BoidsCount;
            _boids = new BoidInfoGPU[totalBoidsCount];

            GroupOfBoidsSpawnData[] boidGroupsSpawnData = CalculateBoidsSpawnData(totalBoidsCount, simulationAreaBounds, _boidSpawnData.MinSpawnDistanceBetweenBoids, _initialGroupsCount);
            int totalBoidsSpawned = 0;
            int currentBoidSubGroup = 0;
            foreach (GroupOfBoidsSpawnData groupOfBoidsSpawnData in boidGroupsSpawnData)
            {
                Quaternion spawnRotation = groupOfBoidsSpawnData.RotationOfTheGroup;
                Vector3 initialDirection = spawnRotation * Vector3.forward;
                int boidsCountInThisGroup = groupOfBoidsSpawnData.SpawnPositions.Length;
                for (int i = 0; i < boidsCountInThisGroup; i++)
                {
                    int boidIndex = totalBoidsSpawned;
                    _boids[boidIndex] = new BoidInfoGPU
                    {
                        Position = groupOfBoidsSpawnData.SpawnPositions[i],
                        Direction = initialDirection,
                        Acceleration = 0f,
                        Speed = schoolProperties.MovementProperties.CruisingSpeed,
                        AngularAcceleration = 0f,
                        AngularVelocity = 0f,
                        // Boid ID contains sub group ID and group ID packed into single float. By default the
                        // boid group ID is 0, so just store the sub-group ID.
                        BoidID = BitConverter.Int32BitsToSingle((currentBoidSubGroup & 0xFF) << 8),
                        CurrentSwimTime = 0f,
                        MaxPlaybackSpeed = schoolProperties.MotionRenderProperties.MaxSwimPlaybackSpeed,
                        MinPlaybackSpeed = schoolProperties.MotionRenderProperties.MinSwimPlaybackSpeed,
                        SwimMotionIntensity = 0f,
                        OriginalIndex = boidIndex
                    };
                    totalBoidsSpawned++;
                }
                currentBoidSubGroup++;
            }
        }
    }
}