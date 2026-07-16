using UnityEngine;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Holds the baked distance field of the reef's real geometry, and the region it covers, so the boid
    /// simulation can sample the actual rock and coral surface instead of 381 box approximations.
    ///
    /// Bake it with OceanX > Bake Reef SDF after any change to the Rockwork or Corals hierarchies — the
    /// field is a snapshot, it does not follow moved geometry.
    ///
    /// The field stores UNSIGNED distance to the nearest reef surface, in metres. Its gradient points away
    /// from that surface, which is the escape direction the avoidance code wants, and unlike the old
    /// closest-affecter search it is continuous everywhere — there is no "nearest obstacle" that can flip
    /// between frames and teleport the escape direction.
    /// </summary>
    [DisallowMultipleComponent]
    public class ReefSDFVolume : MonoBehaviour
    {
        [Header("What Counts As Solid")]
        [Tooltip("Every hierarchy whose meshes fish must not swim through — rock, coral, the seabed. The " +
                 "bake walks all active MeshRenderers under each, so it is 1:1 with what is in the scene: " +
                 "add geometry under one of these roots and the next bake picks it up automatically.\n\n" +
                 "Do NOT list vegetation (Seagrass, Seaweed). Fish should swim through those, and they are " +
                 "~1,700 renderers that would bloat the bake for nothing.")]
        [SerializeField] private Transform[] _reefRoots = new Transform[0];

        [Header("Bake Region")]
        [Tooltip("Centre of the region to bake, in world space. Should cover wherever fish can swim — " +
                 "normally the simulation bounds. Geometry outside this is invisible to the fish.")]
        [SerializeField] private Vector3 _center = new Vector3(16.3f, 13.98f, 18.5f);

        [Tooltip("Size of the region to bake, in world space. Normally the simulation bounds — Padding is " +
                 "added on top.")]
        [SerializeField] private Vector3 _size = new Vector3(77f, 27.16f, 87.6f);

        [Tooltip("Extra metres baked beyond the region on every side. This is NOT optional slack.\n\n" +
                 "The escape direction comes from the field's gradient, which is a central difference " +
                 "sampling one voxel either side. Outside the baked volume there is no data, so a fish " +
                 "within a voxel of the edge would difference against a cliff and get a garbage direction. " +
                 "Padding pushes that edge beyond anywhere fish can actually swim. It also catches reef " +
                 "that straddles the boundary — rock just outside still shapes the field just inside.\n\n" +
                 "A few metres is plenty; it only needs to exceed a voxel or two.")]
        [Min(0f)]
        [SerializeField] private float _padding = 4f;

        [Tooltip("Metres per voxel. 0.5 resolves the gaps a ray threads between corals and costs ~2.8 MB " +
                 "over these bounds; 0.25 gives branch-level detail at ~22 MB. Memory scales with the CUBE " +
                 "of this, so halving it costs 8x.")]
        [Min(0.05f)]
        [SerializeField] private float _voxelSize = 0.5f;

        [Header("Baked Result (set by OceanX > Bake Reef SDF)")]
        [SerializeField] private Texture3D _field;
        [SerializeField] private Vector3 _bakedMin;
        [SerializeField] private Vector3 _bakedSize;
        [SerializeField] private float _bakedVoxelSize;

        /// <summary>Hierarchies whose meshes are solid to fish.</summary>
        public Transform[] ReefRoots => _reefRoots;

        /// <summary>Region the bake should cover, including the padding needed for a valid gradient.</summary>
        public Bounds Bounds => new Bounds(_center, _size + Vector3.one * (_padding * 2f));

        /// <summary>Metres per voxel requested for the next bake.</summary>
        public float VoxelSize => _voxelSize;

        /// <summary>The baked distance field, or null if it has never been baked.</summary>
        public Texture3D Field => _field;

        /// <summary>True when there is a usable field to sample.</summary>
        public bool IsBaked => _field != null && _bakedSize.x > 0f && _bakedSize.y > 0f && _bakedSize.z > 0f;

        /// <summary>Minimum corner of the region the field actually covers (from the bake, not the inspector).</summary>
        public Vector3 BakedMin => _bakedMin;

        /// <summary>Size of the region the field actually covers.</summary>
        public Vector3 BakedSize => _bakedSize;

        /// <summary>Metres per voxel of the baked field.</summary>
        public float BakedVoxelSize => _bakedVoxelSize;

        /// <summary>Called by the baker to attach its result. Records the region actually baked, which may
        /// differ from the inspector fields if they were edited afterwards.</summary>
        public void SetBakedField(Texture3D field, Bounds bounds, float voxelSize)
        {
            _field = field;
            _bakedMin = bounds.min;
            _bakedSize = bounds.size;
            _bakedVoxelSize = voxelSize;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsBaked ? new Color(0.2f, 0.9f, 0.4f, 0.6f) : new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(_center, _size);
        }
    }
}
