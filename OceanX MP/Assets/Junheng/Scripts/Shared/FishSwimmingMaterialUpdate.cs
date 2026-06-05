using Unity.VisualScripting;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Component that updates the fish swimming material with appropriate values based on the current 
    /// swimming speed of the fish. Actually, it's based on acceleration of the fish, but the concept is the same.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteAlways]
    public class FishSwimmingMaterialUpdate : MonoBehaviour
    {
        [Header("References: ")]
        [SerializeField] private FishMotionRenderProperties _fishRenderProperties = null;

        [Header("Properties: ")]
        [SerializeField, Range(0f, 1f)] public float swimIntensityPercentage = 0f;

        private MeshRenderer _meshRenderer = null;
        private float _currentSwimTime = 0f;

        public FishMotionRenderProperties FishRenderProperties { get => _fishRenderProperties; set => _fishRenderProperties = value; }

        private MeshRenderer MeshRenderer
        {
            get
            {
                if(_meshRenderer == null)
                {
                    _meshRenderer = GetComponent<MeshRenderer>();
                }
                return _meshRenderer;
            }
        }
        private float SwimPlaybackSpeed { get => Mathf.Lerp(_fishRenderProperties.MinSwimPlaybackSpeed, _fishRenderProperties.MaxSwimPlaybackSpeed, swimIntensityPercentage); }
        private float SideToSideAmplitude { get => Mathf.Lerp(_fishRenderProperties.MinSideToSideAmplitude, _fishRenderProperties.MaxSideToSideAmplitude, swimIntensityPercentage); }
        private float YawAmplitude { get => Mathf.Lerp(_fishRenderProperties.MinYawRotationAmplitude, _fishRenderProperties.MaxYawRotationAmplitude, swimIntensityPercentage); }
        private float RollAmplitude { get => Mathf.Lerp(_fishRenderProperties.MinRollRotationAmplitude, _fishRenderProperties.MaxRollRotationAmplitude, swimIntensityPercentage); }
        private float PanningYawAmplitude { get => Mathf.Lerp(_fishRenderProperties.MinPanningYawAmplitude, _fishRenderProperties.MaxPanningYawAmplitude, swimIntensityPercentage); }
        private Material Material
        {
            get
            {
                bool createMaterialInstance = true;
#if UNITY_EDITOR
                createMaterialInstance = UnityEditor.EditorApplication.isPlaying;
#endif

                Material material = createMaterialInstance ? MeshRenderer.material : MeshRenderer.sharedMaterial;
                return material;
            }
        }

        private void OnValidate()
        {
            if(_fishRenderProperties == null)
            {
                return;
            }

            UpdateSwimIntensityPercentage(swimIntensityPercentage);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if(!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif
            // Disable automatic fish swimming motion on the material. This is only used for
            // setting up the fish movement properties in the editor.
            Material.SetFloat("_AutomaticSwimVisualization", 0f);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            // Updating current swim time that guides the swimming motion on the material.
            _currentSwimTime += GetSwimmingPlaybackSpeed(swimIntensityPercentage) * Time.deltaTime;
            UpdateCurrentSwimCycleTime(_currentSwimTime);
        }

        /// <summary>
        /// Function updates the current swim intensity percentage of the fish and updates all material 
        /// properties to reflect the correct looking motion on the fish.
        /// </summary>
        /// <param name="percentage">Swim intensity percentage [0, 1].</param>
        public void UpdateSwimIntensityPercentage(float percentage)
        {
            swimIntensityPercentage = Mathf.Clamp01(percentage);
            UpdateMaterialProperties();
        }

        /// <summary>
        /// Get the playback speed of the fish swim cycle based on the current swim intensity.
        /// Higher swim intensities should have larger playback speed to provide a quicker-looking fish.
        /// </summary>
        /// <param name="swimIntensityPercentage">Intensity of the swim [0, 1].</param>
        /// <returns>Playback speed multiplier for the fish swim cycle.</returns>
        public float GetSwimmingPlaybackSpeed(float swimIntensityPercentage)
        {
            return Mathf.Lerp(_fishRenderProperties.MinSwimPlaybackSpeed, _fishRenderProperties.MaxSwimPlaybackSpeed, Mathf.Clamp01(swimIntensityPercentage));
        }

        /// <summary>
        /// Function sets the automatic swim property on the material of the fish. The automatic swimming is used
        /// to visualize the swim motion of the fish while adjusting other material properties in the editor. At runtime,
        /// this should be set to false and the swim time should be guided by the fish motion controller.
        /// </summary>
        /// <param name="automaticSwimEnabled">Boolean specifying should the automatic swim be enabled or not.</param>
        public void SetAutomaticSwim(bool automaticSwimEnabled)
        {
            Material material = Material;
            if (material == null)
            {
                return;
            }

            material.SetFloat("_AutomaticSwimVisualization", automaticSwimEnabled ? 1.0f : 0.0f);
        }

        /// <summary>
        /// Function updates the current swim cycle of the fish by setting the current time of the material.
        /// The swim motion is guided by time, so make sure not to have sudden value changes in the swim time.
        /// </summary>
        /// <param name="swimTime">Current time of the fish swim cycle.</param>
        private void UpdateCurrentSwimCycleTime(float swimTime)
        {
            Material material = Material;
            if (material == null)
            {
                return;
            }

            // By updating the current swim time, we actually update the current swim cycle of the fish.
            // So, make sure to not change the swim time drastically because it will produce sudden skips in motion.
            material.SetFloat("_CurrentSwimTime", swimTime);
        }

        private void UpdateMaterialProperties()
        {
            if (_fishRenderProperties == null)
            {
                return;
            }

            Material material = Material;
            if(material == null)
            {
                return;
            }

            // This property is only important for visualization of swimming motion in editor, while adjusting the swim cycle properties.
            material.SetFloat("_AutomaticSwimSpeed", SwimPlaybackSpeed);

            // These properties determine the look of the swim motion at runtime.
            material.SetFloat("_SideToSideAmplitude", SideToSideAmplitude);
            material.SetFloat("_YawRotationAmplitude", YawAmplitude);
            material.SetFloat("_TailRollAmplitude", RollAmplitude);
            material.SetFloat("_TailYawAmplitude", PanningYawAmplitude);
        }        
    }
}