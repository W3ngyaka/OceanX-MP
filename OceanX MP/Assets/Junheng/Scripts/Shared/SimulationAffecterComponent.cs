using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Component that provides visual authoring for the <see cref="SimulationAffecter"/> structures.
    /// </summary>
    [ExecuteInEditMode]
    public class SimulationAffecterComponent : MonoBehaviour
    {
        [Header("Authoring Settings: ")]
        [SerializeField] private bool _updatePositionAtRuntime = false;

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
            Gizmos.DrawSphere(transform.position, transform.localScale.x);
        }

        private void OnValidate()
        {
            if(_simulationAffecter.Type == SimulationAffecterType.Obstacle)
            {
                _visualizationColor = Color.red;
            }
            else
            {
                _visualizationColor = Color.blue;
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
        }

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