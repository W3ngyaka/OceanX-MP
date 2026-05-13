using UnityEditor;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Custom editor for the <see cref="GlobalAffectersInjector"/> component that adds a
    /// button to the component in the inspector for injecting the obstacles into the boid simulation.
    /// </summary>
    [CustomEditor(typeof(GlobalAffectersInjector))]
    public class GlobalAffectersInjectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GlobalAffectersInjector globalAffectersInjector = (GlobalAffectersInjector)target;
            if (GUILayout.Button(nameof(globalAffectersInjector.InjectObstacles)))
            {
                globalAffectersInjector.InjectObstacles();
            }
        }
    }
}