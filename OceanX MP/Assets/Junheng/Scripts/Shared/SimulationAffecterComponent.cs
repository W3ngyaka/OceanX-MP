using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Component that provides visual authoring for the <see cref="SimulationAffecter"/> structures.
    /// Shape can be a Sphere (radius = transform X scale) or an oriented Box (half-extents =
    /// transform scale, respecting rotation). Type can be Target, Obstacle (casual avoidance) or
    /// Predator (flee).
    /// </summary>
    [ExecuteInEditMode]
    public class SimulationAffecterComponent : MonoBehaviour
    {
        [Header("Authoring Settings: ")]
        [SerializeField] private bool _updatePositionAtRuntime = false;

        [Tooltip("Sphere uses the transform's X scale as radius. Box uses the transform's scale as " +
                 "per-axis half-extents and respects the transform's rotation (oriented box).")]
        [SerializeField] private SimulationAffecterShape _shape = SimulationAffecterShape.Sphere;

        [Header("Visualization: ")]
        [SerializeField] protected Color _visualizationColor = Color.red;

        [Header("Info Only: ")]
        [SerializeField] protected SimulationAffecter _simulationAffecter = default;

        public SimulationAffecter Affecter { get => _simulationAffecter; }
        public Vector3 AffecterPosition
        {
            get
            {
                return _simulationAffecter.Position;
            }
            set
            {
                transform.position = value;
                _simulationAffecter.Position = value;
            }
        }

        private void OnDrawGizmos()
        {
            Color visualizationColor = _visualizationColor;
            visualizationColor.a = 0.25f;
            Gizmos.color = visualizationColor;

            if (_shape == SimulationAffecterShape.Box)
            {
                // Draw the oriented influence box (full size = 2 x half-extents = 2 x localScale).
                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Vector3 fullSize = transform.localScale * 2f;
                Gizmos.DrawCube(Vector3.zero, fullSize);
                Gizmos.color = new Color(visualizationColor.r, visualizationColor.g, visualizationColor.b, 0.9f);
                Gizmos.DrawWireCube(Vector3.zero, fullSize);
                Gizmos.matrix = previous;
            }
            else
            {
                Gizmos.DrawSphere(transform.position, transform.localScale.x);
            }
        }

        private void OnValidate()
        {
            // Bake shape + transform-derived fields immediately so a static affecter is correct in the
            // struct the moment it is edited (without waiting for the next Update tick).
            _simulationAffecter.Shape = _shape;
            _simulationAffecter.Position = transform.position;
            _simulationAffecter.Radius = transform.localScale.x;
            _simulationAffecter.HalfExtents = transform.localScale;
            _simulationAffecter.Rotation = transform.rotation;

            switch (_simulationAffecter.Type)
            {
                case SimulationAffecterType.Predator:
                    _visualizationColor = new Color(1f, 0.35f, 0f); // orange — flee
                    break;
                case SimulationAffecterType.Obstacle:
                    _visualizationColor = Color.red;                // casual avoidance
                    break;
                default:
                    _visualizationColor = Color.blue;               // target
                    break;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying && !_updatePositionAtRuntime)
            {
                return;
            }
#else
            if(!_updatePositionAtRuntime)
            {
                return;
            }
#endif

            // Positioning the affecter at the position of the transform, for easier initialization in the editor.
            _simulationAffecter.Position = transform.position;
            _simulationAffecter.Radius = transform.localScale.x;
            // Box influence tracks the transform's scale (per-axis half-extents) and rotation.
            _simulationAffecter.Shape = _shape;
            _simulationAffecter.HalfExtents = transform.localScale;
            _simulationAffecter.Rotation = transform.rotation;
        }

        /// <summary>
        /// Whether <see cref="BoidSpawnerBase.SetId"/> may stamp its spawner's species ID onto this
        /// affecter. False for ordinary affecters, which belong to exactly one species. Overridden to true
        /// by affecters that deliberately set their own IDs and must survive a rebuild — see
        /// <c>EcosystemTargetGPU</c>, whose targets are keyed to a globally unique FLOCK and are species-
        /// agnostic so that a mixed-species shoal can follow one shared target.
        /// </summary>
        public virtual bool KeepsOwnAffecterID => false;

        /// <summary>
        /// Function updates the ID of the boid group that this affecter affects.
        /// </summary>
        /// <param name="boidGroupID">ID of the boid group that this affecter should affect.</param>
        public void SetAffecterID(int boidGroupID)
        {
            _simulationAffecter.BoidGroupId = boidGroupID;
        }

        /// <summary>
        /// Function updates the sub group ID of the boid group that this affecter affects. This ID
        /// specifies a specific subset of boids that will be affected by this affecter. This is used for
        /// providing additional variety to the simulation.
        /// </summary>
        /// <param name="boidSubGroupID">ID of the sub-group of boids inside this boid group ID that will
        /// be affected by this affecter.</param>
        public void SetSubGroupID(int boidSubGroupID)
        {
            _simulationAffecter.BoidSubGroupId = boidSubGroupID;
        }

        /// <summary>
        /// Function sets the type of the affecter to the provided <paramref name="affecterType"/>.
        /// </summary>
        /// <param name="affecterType">Type of the simulation affecter.</param>
        public void SetAffecterType(SimulationAffecterType affecterType)
        {
            _simulationAffecter.Type = affecterType;
        }

        /// <summary>
        /// Should the position of the affecter be updated every frame on the GPU or not.
        /// </summary>
        /// <param name="shouldUpdatePositionEveryFrame">Boolean specifying if the position update
        /// should be sent to the GPU or not.</param>
        public void SetUpdatePositionEveryFrame(bool shouldUpdatePositionEveryFrame)
        {
            _updatePositionAtRuntime = shouldUpdatePositionEveryFrame;
        }
    }
}