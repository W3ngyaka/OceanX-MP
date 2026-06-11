using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Abstract base class for all boid simulation controller that execute their logic on the GPU.
    /// It expands the <see cref="BoidSimulationBase"/> with additional helper methods for transferring
    /// the data from the CPU format to the GPU one.
    /// </summary>
    public abstract class BoidSimulationBaseGPU : BoidSimulationBase
    {
        [Header("References: ")]
        [SerializeField] protected ComputeShader _boidsComputeShader = null;

        protected List<FishSchoolProperties> _boidsSchools = new List<FishSchoolProperties>();
        protected List<SimulationAffecter> _affecters = new List<SimulationAffecter>();
        // Active GPU spawners ordered by their (dense) BoidGroupId. Inactive (zero-school)
        // spawners are excluded. Rebuilt on every InitializeBoidsSimulation and used by the
        // derived class for render setup and the per-frame render loop.
        protected List<BoidSpawnerGPU> _activeSortedSpawners = new List<BoidSpawnerGPU>();
        protected int _boidsKernelId = 0;
        protected int _boidsCount = 0;

        protected ComputeBuffer _boidsComputeBuffer = null;
        protected ComputeBuffer _boidsSchoolsComputeBuffer = null;
        protected ComputeBuffer _affectersComputeBuffer = null;

        protected BoidInfoGPU[] _boidsInfos = null;
        protected BoidSchoolInfoGPU[] _boidSchoolsInfos = null;
        protected AffecterGPU[] _affectersInfos = null;

        /// <summary>
        /// Function initializes additional data structures required for instanced rendering of
        /// boids on the GPU. If the boid simulation uses normal rendering, this function doesn't
        /// need to do anything.
        /// </summary>
        protected abstract void InitializeRenderProperties();

        /// <inheritdoc/>
        protected override void InitializeBoidsSimulation()
        {
            base.InitializeBoidsSimulation();

            BoidSpawnerBase[] boidSpawners = GetBoidSpawners();

            // Gather only the active GPU spawners — inactive (zero-school) species are excluded
            // entirely so they contribute no boids, affecters or render data.
            List<BoidSpawnerGPU> activeGpuSpawners = new List<BoidSpawnerGPU>(boidSpawners.Length);
            foreach (BoidSpawnerBase spawner in boidSpawners)
            {
                if (spawner is BoidSpawnerGPU gpuSpawner && gpuSpawner.IsActive)
                {
                    activeGpuSpawners.Add(gpuSpawner);
                }
            }

            // Sort the active GPU spawners by their (dense) boid group ID, which was assigned by fish size.
            int activeCount = activeGpuSpawners.Count;
            _activeSortedSpawners.Clear();
            for (int currentDesiredIndex = 0; currentDesiredIndex < activeCount; currentDesiredIndex++)
            {
                for (int i = 0; i < activeCount; i++)
                {
                    if (activeGpuSpawners[i].BoidGroupId == currentDesiredIndex)
                    {
                        _activeSortedSpawners.Add(activeGpuSpawners[i]);
                        break;
                    }
                }
            }

            // Use the sorted GPU spawners to calculate the offsets of each fish species in the final fish array.
            int currentOffset = 0;
            List<BoidInfoGPU> spawnedBoids = new List<BoidInfoGPU>();
            for (int i = 0; i < _activeSortedSpawners.Count; i++)
            {
                BoidSpawnerGPU boidSpawner = _activeSortedSpawners[i];
                boidSpawner.SetRenderingOffset(currentOffset);

                // Add all spawned boids to the final collection of boids, but also update their
                // original index for correct rendering since their original index was in their local array
                // of boids and not in the global one.
                BoidInfoGPU[] spawnedBoidsByThisSpawner = boidSpawner.Boids;
                int boidsSpawnedByThisSpawner = spawnedBoidsByThisSpawner != null ? spawnedBoidsByThisSpawner.Length : 0;
                for (int boidIndex = 0; boidIndex < boidsSpawnedByThisSpawner; boidIndex++)
                {
                    BoidInfoGPU spawnedBoid = spawnedBoidsByThisSpawner[boidIndex];
                    spawnedBoid.OriginalIndex += currentOffset;
                    spawnedBoids.Add(spawnedBoid);
                }
                currentOffset += boidsSpawnedByThisSpawner;
            }

            // Cache all boids and total boids count. May legitimately be 0 (empty ocean).
            _boidsCount = currentOffset;
            _boidsInfos = spawnedBoids.ToArray();

            // Collect all simulation affecters into one collection. They differentiate on their boid group ID.
            foreach (BoidSpawnerGPU gpuBoidSpawner in _activeSortedSpawners)
            {
                SimulationAffecter[] targets = gpuBoidSpawner.Targets;
                if(targets != null && targets.Length > 0)
                {
                    _affecters.AddRange(targets);
                }

                SimulationAffecter[] obstacles = gpuBoidSpawner.Obstacles;
                if (obstacles != null && obstacles.Length > 0)
                {
                    _affecters.AddRange(obstacles);
                }
            }

            // Include global affecters in the simulation.
            foreach(SimulationAffecterComponent globalAffecter in _globalAffecters)
            {
                _affecters.Add(globalAffecter.Affecter);
            }

            // Initialize data structures for instanced rendering of boids on the GPU.
            InitializeRenderProperties();

            // Initialize the compute shader and assign the required data for simulation on the GPU.
            InitializeComputeShaderData();
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>Total number of boids currently tracked by the simulation.</summary>
        public int BoidsCount => _boidsCount;

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Releases all base-class GPU compute buffers and resets cached boid data so that
        /// InitializeBoidsSimulation() can be called again cleanly. Called by ReinitializeBuffers()
        /// on the derived BoidSimulationGPU.
        /// </summary>
        protected void CleanupBaseGPUBuffers()
        {
            CleanUpComputeBuffer(ref _boidsComputeBuffer);
            CleanUpComputeBuffer(ref _boidsSchoolsComputeBuffer);
            CleanUpComputeBuffer(ref _affectersComputeBuffer);
            _boidsCount      = 0;
            _boidsInfos      = null;
            _boidSchoolsInfos = null;
            _affectersInfos  = null;
            _boidsSchools.Clear();
            _affecters.Clear();
            _activeSortedSpawners.Clear();
        }

        /// <inheritdoc/>
        protected virtual void OnDestroy()
        {
            CleanUpComputeBuffer(ref _boidsComputeBuffer);
            CleanUpComputeBuffer(ref _boidsSchoolsComputeBuffer);
            CleanUpComputeBuffer(ref _affectersComputeBuffer);
        }

        /// <summary>
        /// Function releases the referenced compute buffer.
        /// </summary>
        /// <param name="computeBuffer">Reference to the compute buffer that should be released.</param>
        protected virtual void CleanUpComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            computeBuffer?.Release();
            computeBuffer?.Dispose();
            computeBuffer = null;
        }

        /// <summary>
        /// Function initializes the data required for execution of the boids compute shader.
        /// </summary>
        protected virtual void InitializeComputeShaderData()
        {
            // Initialize the kernel ID for the boids simulation.
            _boidsKernelId = _boidsComputeShader.FindKernel("BoidSimulationKernel");

            // Boids compute buffer initialization.
            // Every buffer is sized Mathf.Max(1, count): a zero-size ComputeBuffer throws in Unity,
            // and at empty-ocean start (or after the last extinction) all of these counts can be 0.
            // The placeholder element is never dispatched or drawn — UpdateSimulation early-returns
            // when _boidsCount == 0 — so it has no visible or behavioural effect.
            _boidsComputeBuffer = new ComputeBuffer(Mathf.Max(1, _boidsCount), BoidInfoGPU.Size);
            if (_boidsCount > 0)
            {
                _boidsComputeBuffer.SetData(_boidsInfos);
            }

            // Boids schools compute buffer initialization.
            int boidSchoolsCount = _boidsSchools.Count;
            _boidsSchoolsComputeBuffer = new ComputeBuffer(Mathf.Max(1, boidSchoolsCount), BoidSchoolInfoGPU.Size);
            _boidSchoolsInfos = new BoidSchoolInfoGPU[boidSchoolsCount];
            for (int i = 0; i < boidSchoolsCount; i++)
            {
                _boidSchoolsInfos[i] = ExtractBoidSchoolInfo(_boidsSchools[i]);
            }
            if (boidSchoolsCount > 0)
            {
                _boidsSchoolsComputeBuffer.SetData(_boidSchoolsInfos);
            }

            // Affecters compute buffer initialization.
            int affectersCount = _affecters.Count;
            _affectersComputeBuffer = new ComputeBuffer(Mathf.Max(1, affectersCount), AffecterGPU.Size);
            _affectersInfos = new AffecterGPU[affectersCount];
            for (int i = 0; i < affectersCount; i++)
            {
                _affectersInfos[i] = ExtractAffecterInfo(_affecters[i]);
            }
            if (affectersCount > 0)
            {
                _affectersComputeBuffer.SetData(_affectersInfos);
            }

            // Bind all compute buffers to the compute shader.
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Boids", _boidsComputeBuffer);
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_BoidSchools", _boidsSchoolsComputeBuffer);
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Affecters", _affectersComputeBuffer);

            // Initialize other simulation properties that need to be passed to the compute shader.
            _boidsComputeShader.SetInt("_TotalBoidCount", _boidsCount);
            _boidsComputeShader.SetInt("_TotalAffectersCount", affectersCount);
            _boidsComputeShader.SetFloat("_TimeDelta", Time.deltaTime);
            _boidsComputeShader.SetVector("_SimulationAreaCenter", _simulationAreaBounds.center);
            _boidsComputeShader.SetVector("_SimulationAreaSize", _simulationAreaBounds.size);
        }

        /// <summary>
        /// Function updates the position and size of each simulation affecter in the simulation.
        /// Iterates the SAME active spawner set used to size the affecter buffer in
        /// InitializeComputeShaderData — iterating all spawners here (including inactive ones that may
        /// still carry inspector-assigned obstacles) would make the per-frame affecter count exceed the
        /// buffer size and throw on SetData.
        /// </summary>
        protected virtual void UpdateSimulationAffecters()
        {
            _affecters.Clear();
            foreach (BoidSpawnerGPU gpuBoidSpawner in _activeSortedSpawners)
            {
                SimulationAffecter[] targets = gpuBoidSpawner.Targets;
                if (targets != null && targets.Length > 0)
                {
                    _affecters.AddRange(targets);
                }

                SimulationAffecter[] obstacles = gpuBoidSpawner.Obstacles;
                if (obstacles != null && obstacles.Length > 0)
                {
                    _affecters.AddRange(obstacles);
                }
            }

            // Include global affecters in the simulation.
            foreach (SimulationAffecterComponent globalAffecter in _globalAffecters)
            {
                _affecters.Add(globalAffecter.Affecter);
            }

            int affectersCount = _affecters.Count;
            if (affectersCount == 0)
            {
                // No targets/obstacles this frame — nothing to upload (and SetData rejects empty arrays).
                return;
            }

            // The active affecter count is fixed for a buffer's lifetime (a rebuild re-runs init),
            // but guard the array size defensively so we never write out of range.
            if (_affectersInfos == null || _affectersInfos.Length != affectersCount)
            {
                _affectersInfos = new AffecterGPU[affectersCount];
            }
            for (int i = 0; i < affectersCount; i++)
            {
                _affectersInfos[i] = ExtractAffecterInfo(_affecters[i]);
            }
            _affectersComputeBuffer.SetData(_affectersInfos);
        }

        /// <summary>
        /// Function extracts the properties for one fish school from the provided <paramref name="fishSchoolProperties"/>
        /// and stores it into a GPU-readable struct type.
        /// </summary>
        /// <param name="fishSchoolProperties">Reference to the <see cref="FishSchoolProperties"/> scriptable 
        /// object containing properties describing the behavior of one fish school.</param>
        /// <returns>Reference to the <see cref="BoidSchoolInfoGPU"/> structure containing those properties
        /// in a GPU-readable format.</returns>
        protected virtual BoidSchoolInfoGPU ExtractBoidSchoolInfo(FishSchoolProperties fishSchoolProperties)
        {
            return new BoidSchoolInfoGPU
            {
                VisionRangeSquared = Mathf.Pow(fishSchoolProperties.VisionRange, 2.0f),
                ObstacleAvoidanceRangeSquared = Mathf.Pow(fishSchoolProperties.ObstacleAvoidanceRange, 2.0f),
                SeparationRangeSquared = Mathf.Pow(fishSchoolProperties.SeparationRange, 2.0f),
                SeparationWeight = fishSchoolProperties.SeparationWeight,
                CohesionWeight = fishSchoolProperties.CohesionWeight,
                AlignmentWeight = fishSchoolProperties.AlignmentWeight,
                TargetFollowWeight = fishSchoolProperties.TargetWeight,
                CruisingSpeed = fishSchoolProperties.MovementProperties.CruisingSpeed,
                MaxSpeed = fishSchoolProperties.MovementProperties.MaxSpeed,
                WaterFriction = fishSchoolProperties.MovementProperties.WaterFriction,
                Deceleration = fishSchoolProperties.MovementProperties.Deceleration,
                MaxAcceleration = fishSchoolProperties.MovementProperties.MaxAcceleration,
                MovementJerk = fishSchoolProperties.MovementProperties.MovementJerk,
                MaxAngularVelocity = fishSchoolProperties.MovementProperties.MaxAngularVelocity,
                AngularVelocityReduction = fishSchoolProperties.MovementProperties.AngularVelocityReduction,
                MaxAngularAcceleration = fishSchoolProperties.MovementProperties.MaxAngularAcceleration,
                AngularDeceleration = fishSchoolProperties.MovementProperties.AngularDeceleration,
                AngularJerk = fishSchoolProperties.MovementProperties.AngularJerk,
                RotationEffectOnSpeed = fishSchoolProperties.MovementProperties.RotationEffectOnSpeed,
                EmptyFiller = 0f
            };
        }

        /// <summary>
        /// Function extracts the properties for one simulation affecter from the provided <paramref name="simulationAffecter"/>
        /// and stores it into a GPU-readable type struct.
        /// </summary>
        /// <param name="simulationAffecter">Instance of the <see cref="SimulationAffecter"/> struct containing
        /// properties in a CPU-readable format for one simulation affecter.</param>
        /// <returns>Instance of the <see cref="AffecterGPU"/> struct that contains the stored properties of 
        /// the provided <paramref name="simulationAffecter"/> in a GPU-readable type.</returns>
        protected virtual AffecterGPU ExtractAffecterInfo(SimulationAffecter simulationAffecter)
        {
            return new AffecterGPU
            {
                Position = simulationAffecter.Position,
                Radius = simulationAffecter.Radius,
                AffecterType = (float)((int)simulationAffecter.Type),
                BoidGroupId = simulationAffecter.BoidGroupId,
                BoidSubGroupId = simulationAffecter.BoidSubGroupId,
                EmptyFiller = 0f
            };
        }

        /// <summary>
        /// Function extracts the properties for one fish species from the provided <paramref name="fishMotionRenderProperties"/>
        /// and stores it into a GPU-readable type struct.
        /// </summary>
        /// <param name="fishMotionRenderProperties">Reference to the <see cref="FishMotionRenderProperties"/> scriptable object
        /// that contains render properties in a CPU-readable format for one fish species.</param>
        /// <returns>Instance of the <see cref="BoidRenderInfoGPU"/> struct that contains the stored properties of 
        /// the provided <paramref name="fishMotionRenderProperties"/> in a GPU-readable type.</returns>
        protected virtual BoidRenderInfoGPU ExtractBoidSchoolRenderInfo(FishMotionRenderProperties fishMotionRenderProperties)
        {
            return new BoidRenderInfoGPU
            {
                MinSideToSideAmplitude = fishMotionRenderProperties.MinSideToSideAmplitude,
                MaxSideToSideAmplitude = fishMotionRenderProperties.MaxSideToSideAmplitude,
                MinYawRotationAmplitude = fishMotionRenderProperties.MinYawRotationAmplitude,
                MaxYawRotationAmplitude = fishMotionRenderProperties.MaxYawRotationAmplitude,
                MinRollRotationAmplitude = fishMotionRenderProperties.MinRollRotationAmplitude,
                MaxRollRotationAmplitude = fishMotionRenderProperties.MaxRollRotationAmplitude,
                MinPanningYawAmplitude = fishMotionRenderProperties.MinPanningYawAmplitude,
                MaxPanningYawAmplitude = fishMotionRenderProperties.MaxPanningYawAmplitude
            };
        }
    }
}