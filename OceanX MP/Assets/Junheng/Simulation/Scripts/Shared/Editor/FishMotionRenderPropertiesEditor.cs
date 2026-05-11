using UnityEditor;

namespace OceanX
{
    /// <summary>
    /// Custom editor for the <see cref="FishMotionRenderProperties"/> scriptable object.
    /// </summary>
    [CustomEditor(typeof(FishMotionRenderProperties))]
    public class FishMotionRenderPropertiesEditor : Editor
    {
        private FishMotionRenderProperties _fishMotionRenderProperties = null;

        private FishMotionRenderProperties FishMotionRenderProperties
        {
            get
            {
                if (_fishMotionRenderProperties == null)
                {
                    _fishMotionRenderProperties = (FishMotionRenderProperties)target;
                }
                return _fishMotionRenderProperties;
            }
        }

        private void Awake()
        {
            _fishMotionRenderProperties = (FishMotionRenderProperties)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawMinMaxFloatSlider("Swim Playback Speed",
                FishMotionRenderProperties.MIN_PLAYBACK_SPEED, 
                FishMotionRenderProperties.MAX_PLAYBACK_SPEED,
                ref FishMotionRenderProperties.MinSwimPlaybackSpeed,
                ref FishMotionRenderProperties.MaxSwimPlaybackSpeed);

            DrawMinMaxFloatSlider("Side To Side Amplitude",
                FishMotionRenderProperties.MIN_SIDE_TO_SIDE_AMPLITUDE,
                FishMotionRenderProperties.MAX_SIDE_TO_SIDE_AMPLITUDE,
                ref FishMotionRenderProperties.MinSideToSideAmplitude,
                ref FishMotionRenderProperties.MaxSideToSideAmplitude);

            DrawMinMaxFloatSlider("Yaw Rotation Amplitude",
                FishMotionRenderProperties.MIN_YAW_ROTATION_AMPLITUDE,
                FishMotionRenderProperties.MAX_YAW_ROTATION_AMPLITUDE,
                ref FishMotionRenderProperties.MinYawRotationAmplitude,
                ref FishMotionRenderProperties.MaxYawRotationAmplitude);

            DrawMinMaxFloatSlider("Roll Rotation Amplitude",
                FishMotionRenderProperties.MIN_ROLL_ROTATION_AMPLITUDE,
                FishMotionRenderProperties.MAX_ROLL_ROTATION_AMPLITUDE,
                ref FishMotionRenderProperties.MinRollRotationAmplitude,
                ref FishMotionRenderProperties.MaxRollRotationAmplitude);

            DrawMinMaxFloatSlider("Panning Yaw Amplitude",
                FishMotionRenderProperties.MIN_PANNING_YAW_AMPLITUDE,
                FishMotionRenderProperties.MAX_PANNING_YAW_AMPLITUDE,
                ref FishMotionRenderProperties.MinPanningYawAmplitude,
                ref FishMotionRenderProperties.MaxPanningYawAmplitude);

            EditorGUILayout.EndVertical();

            EditorUtility.SetDirty(FishMotionRenderProperties);
        }

        private void DrawMinMaxFloatSlider(string label, float minLimit, float maxLimit, ref float propertyMinValue, ref float propertyMaxValue)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            float minValue = propertyMinValue;
            float maxValue = propertyMaxValue;
            EditorGUILayout.MinMaxSlider(label, ref minValue, ref maxValue, minLimit, maxLimit);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(FishMotionRenderProperties, "Fish Motion Render Properties Update");
                propertyMinValue = minValue;
                propertyMaxValue = maxValue;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}