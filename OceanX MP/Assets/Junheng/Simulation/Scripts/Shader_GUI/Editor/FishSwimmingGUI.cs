using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using Codice.Client.BaseCommands;

namespace OceanX
{
    internal class FishSwimmingGUI
    {
        internal static class Styles
        {
            /// <summary>
            /// The text and tool tip for the mask visualization GUI.
            /// </summary>
            public static readonly GUIContent visualizeMask = EditorGUIUtility.TrTextContent("Visualize Mask",
                "Should the fish swimming motion mask be visualized or not.");

            /// <summary>
            /// The text and tool tip for the automatic swim visualization GUI.
            /// </summary>
            public static readonly GUIContent automaticSwimVisualization = EditorGUIUtility.TrTextContent("Automatic Swim Visualization",
                "Should the fish swimming motion be guided automatically or via script.");

            /// <summary>
            /// The text and tool tip for the automatic swim speed GUI.
            /// </summary>
            public static readonly GUIContent automaticSwimSpeed = EditorGUIUtility.TrTextContent("Automatic Swim Speed",
                "The speed of the automatic fish swimming motion.");

            /// <summary>
            /// The text and tool tip for the side to side amplitude GUI.
            /// </summary>
            public static readonly GUIContent sideToSideAmplitude = EditorGUIUtility.TrTextContent("Side To Side Amplitude",
                "Amount of horizontal movement that the fish does while swimming.");

            /// <summary>
            /// The text and tool tip for the side to side offset GUI.
            /// </summary>
            public static readonly GUIContent sideToSideOffset = EditorGUIUtility.TrTextContent("Side To Side Offset",
                "Offset in the period for the side to side movement of the fish swim motion.");

            /// <summary>
            /// The text and tool tip for the yaw rotation amplitude GUI.
            /// </summary>
            public static readonly GUIContent yawRotationAmplitude = EditorGUIUtility.TrTextContent("Yaw Rotation Amplitude",
                "Maximum amount of the yaw rotation around the pivot of the fish during the swimming motion.");

            /// <summary>
            /// The text and tool tip for the yaw rotation offset GUI.
            /// </summary>
            public static readonly GUIContent yawRotationOffset = EditorGUIUtility.TrTextContent("Yaw Rotation Offset",
                "Offset for the yaw rotation motion. Used to match the motion with the rest of the fish movement.");

            /// <summary>
            /// The text and tool tip for the tail mask falloff GUI.
            /// </summary>
            public static readonly GUIContent tailMaskFalloff = EditorGUIUtility.TrTextContent("Tail Mask Falloff",
                "Falloff of the tail mask for the fish motion movement.");

            /// <summary>
            /// The text and tool tip for the tail roll amplitude GUI.
            /// </summary>
            public static readonly GUIContent tailRollAmplitude = EditorGUIUtility.TrTextContent("Tail Roll Amplitude",
                "Strength of the tail roll motion.");

            /// <summary>
            /// The text and tool tip for the tail roll offset GUI.
            /// </summary>
            public static readonly GUIContent tailRollOffset = EditorGUIUtility.TrTextContent("Tail Roll Offset",
                "Offset of the tail roll motion. Used to synchronize the movement motion with the rest of the fish swimming cycle.");

            /// <summary>
            /// The text and tool tip for the tail wave length GUI.
            /// </summary>
            public static readonly GUIContent tailWaveLength = EditorGUIUtility.TrTextContent("Tail Wave Length",
                "Strength of the tail wave motion.");

            /// <summary>
            /// The text and tool tip for the tail yaw amplitude GUI.
            /// </summary>
            public static readonly GUIContent tailYawAmplitude = EditorGUIUtility.TrTextContent("Tail Yaw Amplitude",
                "Yaw motion amplitude of the tail of the fish.");

            /// <summary>
            /// The text and tool tip for the fish swimming options GUI.
            /// </summary>
            public static readonly GUIContent FishSwimmingLabel = EditorGUIUtility.TrTextContent("Fish Swimming Properties",
                "These settings affect fish swimming motion vertex animation.");
        }

        public struct LitProperties
        {
            /// <summary>
            /// The MaterialProperty for fish swimming mask visualization.
            /// </summary>
            public MaterialProperty visualizeMaskProp { get; set; }

            /// <summary>
            /// The MaterialProperty for fish automatic swimming visualization.
            /// </summary>
            public MaterialProperty automaticSwimVisualizationProp { get; set; }

            /// <summary>
            /// The MaterialProperty for automatic fish swimming speed.
            /// </summary>
            public MaterialProperty automaticSwimSpeedProp { get; set; }

            /// <summary>
            /// The MaterialProperty for side to side movement amplitude.
            /// </summary>
            public MaterialProperty sideToSideAmplitudeProp { get; set; }

            /// <summary>
            /// The MaterialProperty for offset of the side to side movement.
            /// </summary>
            public MaterialProperty sideToSideOffsetProp { get; set; }

            /// <summary>
            /// The MaterialProperty for yaw rotation amplitude.
            /// </summary>
            public MaterialProperty yawRotationAmplitudeProp { get; set; }

            /// <summary>
            /// The MaterialProperty for offset of the yaw rotation.
            /// </summary>
            public MaterialProperty yawRotationOffsetProp { get; set; }

            /// <summary>
            /// The MaterialProperty for falloff of the tail mask.
            /// </summary>
            public MaterialProperty tailMaskFalloffProp { get; set; }

            /// <summary>
            /// The MaterialProperty for amplitude of the tail roll.
            /// </summary>
            public MaterialProperty tailRollAmplitudeProp { get; set; }

            /// <summary>
            /// The MaterialProperty for offset of the tail roll.
            /// </summary>
            public MaterialProperty tailRollOffsetProp { get; set; }

            /// <summary>
            /// The MaterialProperty for length of the tail wave affecting tail animations.
            /// </summary>
            public MaterialProperty tailWaveLengthProp { get; set; }

            /// <summary>
            /// The MaterialProperty for panning yaw amplitude along the fish spine.
            /// </summary>
            public MaterialProperty tailYawAmplitudeProp { get; set; }

            public LitProperties(MaterialProperty[] properties)
            {
                visualizeMaskProp = BaseShaderGUI.FindProperty("_VisualizeMask", properties, false);
                automaticSwimVisualizationProp = BaseShaderGUI.FindProperty("_AutomaticSwimVisualization", properties, false);
                automaticSwimSpeedProp = BaseShaderGUI.FindProperty("_AutomaticSwimSpeed", properties, false);
                sideToSideAmplitudeProp = BaseShaderGUI.FindProperty("_SideToSideAmplitude", properties, false);
                sideToSideOffsetProp = BaseShaderGUI.FindProperty("_SideToSideOffset", properties, false);
                yawRotationAmplitudeProp = BaseShaderGUI.FindProperty("_YawRotationAmplitude", properties, false);
                yawRotationOffsetProp = BaseShaderGUI.FindProperty("_YawRotationOffset", properties, false);
                tailMaskFalloffProp = BaseShaderGUI.FindProperty("_TailMaskFalloff", properties, false);
                tailRollAmplitudeProp = BaseShaderGUI.FindProperty("_TailRollAmplitude", properties, false);
                tailRollOffsetProp = BaseShaderGUI.FindProperty("_TailRollOffset", properties, false);
                tailWaveLengthProp = BaseShaderGUI.FindProperty("_TailWaveLength", properties, false);
                tailYawAmplitudeProp = BaseShaderGUI.FindProperty("_TailYawAmplitude", properties, false);
            }
        }

        public static void DoFishSwimmingArea(LitProperties properties, MaterialEditor materialEditor)
        {
            DrawFloatToggleProperty(Styles.visualizeMask, properties.visualizeMaskProp);
            DrawFloatToggleProperty(Styles.automaticSwimVisualization, properties.automaticSwimVisualizationProp);

            EditorGUI.BeginDisabledGroup(properties.automaticSwimVisualizationProp.floatValue <= 0.0f);
            DrawFloatSliderProperty(materialEditor, properties.automaticSwimSpeedProp, 0f, 5.0f, Styles.automaticSwimSpeed);
            EditorGUI.EndDisabledGroup();

            DrawFloatSliderProperty(materialEditor, properties.sideToSideAmplitudeProp, 0f, 10.0f, Styles.sideToSideAmplitude);
            DrawFloatSliderProperty(materialEditor, properties.sideToSideOffsetProp, -1f, 1f, Styles.sideToSideOffset);

            DrawFloatSliderProperty(materialEditor, properties.yawRotationAmplitudeProp, 0f, 1f, Styles.yawRotationAmplitude);
            DrawFloatSliderProperty(materialEditor, properties.yawRotationOffsetProp, -1f, 1f, Styles.yawRotationOffset);

            DrawFloatSliderProperty(materialEditor, properties.tailMaskFalloffProp, 0f, 4f, Styles.tailMaskFalloff);
            DrawFloatSliderProperty(materialEditor, properties.tailRollAmplitudeProp, 0f, 1f, Styles.tailRollAmplitude);
            DrawFloatSliderProperty(materialEditor, properties.tailRollOffsetProp, -1f, 1f, Styles.tailRollOffset);
            DrawFloatSliderProperty(materialEditor, properties.tailWaveLengthProp, 0f, 1f, Styles.tailWaveLength);
            DrawFloatSliderProperty(materialEditor, properties.tailYawAmplitudeProp, 0f, 1f, Styles.tailYawAmplitude);
        }

        internal static Rect GetRect(MaterialProperty prop)
        {
            return EditorGUILayout.GetControlRect(true, MaterialEditor.GetDefaultPropertyHeight(prop), EditorStyles.layerMaskField);
        }

        internal static void DrawFloatSliderProperty(MaterialEditor editor, MaterialProperty prop, float min, float max, GUIContent label)
        {
            MaterialEditor.BeginProperty(prop);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            float newValue = EditorGUI.Slider(GetRect(prop), label, prop.floatValue, min, max);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo(label.text);
                prop.floatValue = newValue;
            }

            MaterialEditor.EndProperty();
        }

        internal static void DrawFloatToggleProperty(GUIContent styles, MaterialProperty prop, int indentLevel = 0, bool isDisabled = false)
        {
            if (prop == null)
                return;

            EditorGUI.BeginDisabledGroup(isDisabled);
            EditorGUI.indentLevel += indentLevel;
            EditorGUI.BeginChangeCheck();
            MaterialEditor.BeginProperty(prop);
            bool newValue = EditorGUILayout.Toggle(styles, prop.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue ? 1.0f : 0.0f;
            MaterialEditor.EndProperty();
            EditorGUI.indentLevel -= indentLevel;
            EditorGUI.EndDisabledGroup();
        }
    }
}
