using UnityEditor;
using UnityEngine;

namespace OceanX
{
    [CustomEditor(typeof(BoidSimulationTargetAnimatorsSpawner))]
    public class BoidSimulationTargetAnimatorsSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            BoidSimulationTargetAnimatorsSpawner spawner = (BoidSimulationTargetAnimatorsSpawner)target;

            if (GUILayout.Button(nameof(spawner.CreateTargets)))
            {
                spawner.CreateTargets();
            }

            if (GUILayout.Button(nameof(spawner.DeleteTargets)))
            {
                spawner.DeleteTargets();
            }
        }
    }
}
