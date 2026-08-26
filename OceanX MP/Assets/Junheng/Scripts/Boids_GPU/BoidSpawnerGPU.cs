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

        // Live GPU positions read back just before a buffer rebuild, so existing fish
        // keep their current positions instead of teleporting to spawn positions.
        private BoidInfoGPU[] _preservedBoids = null;

        // ---- Moray serpentine spine render data ----------------------------------------------------
        // Set by BoidSimulationGPU for the moray spawner ONLY (the species with UseSpineDeformation).
        // These are forwarded to the OceanX/Moray_Lit_Instanced material's property block in RenderBoids
        // so the vertex shader can lay the eel body along its recorded head-path. Untouched (and unbound)
        // for every other spawner, which keeps using the standard rigid fish shader.
        private bool          _hasSpineRenderData      = false;
        private ComputeBuffer _spineTrailBuffer        = null;
        private ComputeBuffer _spineTrailCursorBuffer  = null;
        private int           _spineTrailCount         = 0;
        private float         _spineTrailSpacing        = 0.12f;
        private float         _spineHeadLocalZ          = 0f;
        private float         _spineBodyLength          = 1f;
        private float         _spineUndulationAmplitude = 0f;
        private float         _spineUndulationWaves     = 0f;
        private float         _spineUndulationSpeed     = 0f;
        private bool          _spineDebugStraight       = false;
        private bool          _spineFlipNormals         = false;
        private float         _spineSmoothingWindow     = 1f;
        private float         _spineUndulationHeadHold   = 0.03f;

        // ---- Moray resting-in-cave pose + mouth gape (set by BoidSimulationGPU from MorayCaveDirector) ----
        // Cave anchors near which a moray lays its body back into the rock and breathes. Fixed-length so
        // the material array bind is always valid; only the first _spineRestAnchorCount entries are live.
        private const int     SpineMaxRestAnchors        = 8; // must match MORAY_MAX_REST_ANCHORS in the shader
        private readonly Vector4[] _spineRestAnchorPos    = new Vector4[SpineMaxRestAnchors];
        private readonly Vector4[] _spineRestAnchorDir    = new Vector4[SpineMaxRestAnchors];
        private int           _spineRestAnchorCount       = 0;
        private float         _spineRestRadius            = 10f;
        private float         _spineRestCurlRadius        = 3f;
        private float         _spineRestUndulationScale   = 0.15f;
        private float         _spineMouthMaxAngle         = 0f;
        private float         _spineMouthRate             = 0.5f;
        private float         _spineMouthLength           = 0.5f;
        private float         _spineMouthHingeY           = 0f;

        /// <summary>Global offset of this spawner's slice in the shared boids compute buffer.</summary>
        public int RenderingOffset => _renderingOffset;

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

            // Arm the entry sprint, once, for the fish that were placed OUTSIDE the simulation bounds —
            // i.e. a school relocated to an off-screen entry point (see BoidSpawnerGPUMultiTargets.
            // RelocateGroupToEntryPoint). -1 is the "spawned outside, hasn't crossed in yet" sentinel the
            // compute shader looks for; it sprints until it enters, then counts down _EntryBoostDuration
            // and settles. Fish spawned inside the box start at 0 and never sprint.
            //
            // Arming it here rather than in the shader is what lets the shader stop treating "currently
            // outside the bounds" as "newly arrived" — a settled fish that later strays over the boundary
            // is now left alone instead of being re-launched at MaxSpeed. Runs BEFORE the preservation
            // pass below on purpose, so existing fish keep their own live entry state.
            for (int i = 0; i < _boids.Length; i++)
            {
                BoidInfoGPU boid = _boids[i];
                boid.EntryBoostTimeRemaining = simulationAreaBounds.Contains(boid.Position) ? 0f : -1f;
                _boids[i] = boid;
            }

            // Restore the LIVE state of fish that existed before this rebuild so they carry on seamlessly
            // instead of snapping back to a spawn pose. As well as position + direction, this preserves the
            // motion (speed / acceleration / angular velocity) AND the swim-animation state
            // (CurrentSwimTime = animation phase, SwimMotionIntensity), so adding/removing a school no
            // longer resets existing fishes' tail animation — which was making them visibly "tweak".
            // New fish (indices beyond the preserved count) keep their fresh spawn values. BoidID and
            // OriginalIndex stay FRESH — they encode this rebuild's group layout — as do the per-species
            // Min/MaxPlaybackSpeed constants.
            if (_preservedBoids != null)
            {
                int copyCount = Mathf.Min(_preservedBoids.Length, _boids.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    BoidInfoGPU fresh = _boids[i];
                    BoidInfoGPU old   = _preservedBoids[i];

                    fresh.Position                = old.Position;
                    fresh.Direction               = old.Direction;
                    fresh.Speed                   = old.Speed;
                    fresh.Acceleration            = old.Acceleration;
                    fresh.AngularVelocity         = old.AngularVelocity;
                    fresh.AngularAcceleration     = old.AngularAcceleration;
                    fresh.CurrentSwimTime         = old.CurrentSwimTime;
                    fresh.SwimMotionIntensity     = old.SwimMotionIntensity;
                    fresh.EntryBoostTimeRemaining = old.EntryBoostTimeRemaining;

                    _boids[i] = fresh;
                }
                _preservedBoids = null;
            }

            // Initialize the draw arguments buffer for indirect issuing of draw calls for rendering fish.
            // One entry PER SUB-MESH so a multi-material mesh (e.g. body + eyes) can be drawn with a
            // separate material per sub-mesh. Each entry points at that sub-mesh's index range.
            Mesh mesh = _boidSpawnData.BoidMesh;
            int subMeshCount = Mathf.Max(1, mesh.subMeshCount);

            _drawArgumentsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, subMeshCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _drawArguments = new GraphicsBuffer.IndirectDrawIndexedArgs[subMeshCount];
            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
            {
                _drawArguments[subMesh] = new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    baseVertexIndex       = mesh.GetBaseVertex(subMesh),
                    startIndex            = mesh.GetIndexStart(subMesh),
                    startInstance         = 0,
                    indexCountPerInstance = mesh.GetIndexCount(subMesh),
                    instanceCount         = (uint)_boidSpawnData.BoidsCount
                };
            }
            _drawArgumentsBuffer.SetData(_drawArguments);
        }

        /// <summary>
        /// Returns the material to use for the given sub-mesh of <see cref="BoidSpawnData.BoidMesh"/>:
        /// sub-mesh 0 = body, sub-mesh 1 = eyes. Returns null when no material is assigned for that
        /// sub-mesh, in which case it isn't drawn.
        /// </summary>
        protected Material GetMaterialForSubMesh(int subMesh)
        {
            switch (subMesh)
            {
                case 0:  return _boidSpawnData.BoidMaterial;
                case 1:  return _boidSpawnData.BoidEyeMaterial;
                default: return null;
            }
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
        /// Supplies this spawner's material with the serpentine spine render data (moray only). Called by
        /// BoidSimulationGPU every frame for the spawner whose species has UseSpineDeformation, so the
        /// tuning values can be adjusted live. <paramref name="trail"/> / <paramref name="cursor"/> are the
        /// same compute buffers the simulation kernel writes; here they are read by the render shader.
        /// </summary>
        public void SetSpineRenderData(ComputeBuffer trail, ComputeBuffer cursor, int trailCount, float spacing,
            float headLocalZ, float bodyLength, float undulationAmplitude, float undulationWaves,
            float undulationSpeed, bool debugStraight, bool flipNormals, float smoothingWindow,
            float undulationHeadHold)
        {
            _hasSpineRenderData       = true;
            _spineTrailBuffer         = trail;
            _spineTrailCursorBuffer   = cursor;
            _spineTrailCount          = trailCount;
            _spineTrailSpacing        = spacing;
            _spineHeadLocalZ          = headLocalZ;
            _spineBodyLength          = bodyLength;
            _spineUndulationAmplitude = undulationAmplitude;
            _spineUndulationWaves     = undulationWaves;
            _spineUndulationSpeed     = undulationSpeed;
            _spineDebugStraight       = debugStraight;
            _spineFlipNormals         = flipNormals;
            _spineSmoothingWindow     = smoothingWindow;
            _spineUndulationHeadHold  = undulationHeadHold;
        }

        /// <summary>
        /// Moray only: the cave rest anchors + pose/mouth tuning, forwarded to the material each frame so the
        /// vertex shader lays a resting eel's body into the rock and gapes its jaw. <paramref name="count"/>
        /// entries of the two arrays are read (capped to <see cref="SpineMaxRestAnchors"/>). Called by
        /// BoidSimulationGPU right before RenderBoids; pass count 0 to disable resting entirely.
        /// </summary>
        public void SetSpineRestData(Vector4[] anchorPos, Vector4[] anchorDir, int count,
            float restRadius, float restCurlRadius, float restUndulationScale,
            float mouthMaxAngle, float mouthRate, float mouthLength, float mouthHingeY)
        {
            int n = Mathf.Clamp(count, 0, SpineMaxRestAnchors);
            for (int i = 0; i < SpineMaxRestAnchors; i++)
            {
                _spineRestAnchorPos[i] = (anchorPos != null && i < anchorPos.Length) ? anchorPos[i] : Vector4.zero;
                _spineRestAnchorDir[i] = (anchorDir != null && i < anchorDir.Length) ? anchorDir[i] : Vector4.zero;
            }
            _spineRestAnchorCount     = n;
            _spineRestRadius          = restRadius;
            _spineRestCurlRadius      = restCurlRadius;
            _spineRestUndulationScale = restUndulationScale;
            _spineMouthMaxAngle       = mouthMaxAngle;
            _spineMouthRate           = mouthRate;
            _spineMouthLength         = mouthLength;
            _spineMouthHingeY         = mouthHingeY;
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
            if (_drawArgumentsBuffer == null || _drawArguments == null) return;

            // All sub-meshes share the same per-instance boid data, so build the property block once.
            MaterialPropertyBlock matProps = new MaterialPropertyBlock();
            matProps.SetBuffer("_Boids", boidsComputeBuffer);
            matProps.SetBuffer("_BoidsRenderInfos", boidSchoolsRenderInfosBuffer);
            matProps.SetInt("_BoidsBufferOffset", _renderingOffset);
            matProps.SetVector("_SimulationAreaCenter", simulationAreaBounds.center);

            // Moray only: hand the head-path trail + spine tuning to the OceanX/Moray_Lit_Instanced
            // material. matProps is rebuilt every call, so these are (re)bound each frame — which also
            // lets the undulation/debug tunables be adjusted live in Play mode.
            if (_hasSpineRenderData && _spineTrailBuffer != null && _spineTrailCursorBuffer != null)
            {
                matProps.SetBuffer("_MorayTrail", _spineTrailBuffer);
                matProps.SetBuffer("_MorayTrailCursor", _spineTrailCursorBuffer);
                matProps.SetInt("_MorayTrailCount", _spineTrailCount);
                matProps.SetFloat("_MorayTrailSpacing", _spineTrailSpacing);
                matProps.SetFloat("_MorayHeadLocalZ", _spineHeadLocalZ);
                matProps.SetFloat("_MorayBodyLength", _spineBodyLength);
                matProps.SetFloat("_MorayUndulationAmplitude", _spineUndulationAmplitude);
                matProps.SetFloat("_MorayUndulationWaves", _spineUndulationWaves);
                matProps.SetFloat("_MorayUndulationSpeed", _spineUndulationSpeed);
                matProps.SetInt("_MorayDebugStraight", _spineDebugStraight ? 1 : 0);
                matProps.SetInt("_MorayFlipNormals", _spineFlipNormals ? 1 : 0);
                matProps.SetFloat("_MoraySmoothingWindow", _spineSmoothingWindow);
                matProps.SetFloat("_MorayUndulationHeadHold", _spineUndulationHeadHold);

                // Resting-in-cave pose + mouth gape. Arrays are always the fixed max length so the bind is
                // valid even with 0 active anchors (count gates it in the shader).
                matProps.SetVectorArray("_MorayRestAnchorPos", _spineRestAnchorPos);
                matProps.SetVectorArray("_MorayRestAnchorDir", _spineRestAnchorDir);
                matProps.SetInt("_MorayRestAnchorCount", _spineRestAnchorCount);
                matProps.SetFloat("_MorayRestRadius", _spineRestRadius);
                matProps.SetFloat("_MorayRestCurlRadius", _spineRestCurlRadius);
                matProps.SetFloat("_MorayRestUndulationScale", _spineRestUndulationScale);
                matProps.SetFloat("_MorayMouthMaxAngle", _spineMouthMaxAngle);
                matProps.SetFloat("_MorayMouthRate", _spineMouthRate);
                matProps.SetFloat("_MorayMouthLength", _spineMouthLength);
                matProps.SetFloat("_MorayMouthHingeY", _spineMouthHingeY);
            }

            // Increase the culling area a bit so that some fish don't randomly
            // disappear when they get close to the edges of the simulation area.
            simulationAreaBounds.extents *= 1.25f;

            // Draw each sub-mesh with its own material (e.g. sub-mesh 0 = body, sub-mesh 1 = eyes).
            // startCommand selects this sub-mesh's entry in the indirect args buffer.
            for (int subMesh = 0; subMesh < _drawArguments.Length; subMesh++)
            {
                Material material = GetMaterialForSubMesh(subMesh);
                if (material == null) continue;   // no material assigned for this sub-mesh — skip it

                RenderParams renderParams = new RenderParams(material);
                renderParams.matProps = matProps;
                renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderParams.receiveShadows = false;
                renderParams.layer = gameObject.layer;
                renderParams.worldBounds = simulationAreaBounds;

                Graphics.RenderMeshIndirect(renderParams, _boidSpawnData.BoidMesh, _drawArgumentsBuffer, 1, subMesh);
            }
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Stores a snapshot of live GPU boid data for this spawner's slice of the global
        /// buffer. Call this before CleanupSpawnData() so that SpawnBoids() can restore
        /// positions of existing fish instead of teleporting them to spawn positions.
        /// </summary>
        public void StorePreservedBoids(BoidInfoGPU[] preserved)
        {
            _preservedBoids = preserved;
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

            GroupOfBoidsSpawnData[] boidGroupsSpawnData = CalculateBoidsSpawnData(totalBoidsCount, simulationAreaBounds, ResolvedMinSpawnDistance, _initialGroupsCount);
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
                    // Slight per-individual swim-speed variation (±12%) so no two fish tail-beat at exactly
                    // the same rate — this keeps a school from re-synchronising into one identical wave over
                    // time. Combined with the randomized starting phase below.
                    float playbackSpeedVariation = UnityEngine.Random.Range(0.88f, 1.12f);
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
                        // Bits 8-15 are the GLOBAL flock id (see BoidSpawnerBase.FlockIdForSchool), bits 0-7
                        // the species id, which SetId/UpdateBoidGroupId ORs in afterwards.
                        BoidID = BitConverter.Int32BitsToSingle((FlockIdForSchool(currentBoidSubGroup) & 0xFF) << 8),
                        // Randomize each fish's starting animation phase so individuals are desynced the moment
                        // they spawn instead of all tail-beating in lockstep. One full tail cycle == a
                        // CurrentSwimTime of 1, so a [0,1) start covers the entire phase. NOTE: this per-boid
                        // swim time only drives the visual once the fish material's _AutomaticSwimVisualization
                        // is OFF — otherwise the shader animates every fish off a single shared global clock.
                        CurrentSwimTime = UnityEngine.Random.value,
                        MaxPlaybackSpeed = schoolProperties.MotionRenderProperties.MaxSwimPlaybackSpeed * playbackSpeedVariation,
                        MinPlaybackSpeed = schoolProperties.MotionRenderProperties.MinSwimPlaybackSpeed * playbackSpeedVariation,
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