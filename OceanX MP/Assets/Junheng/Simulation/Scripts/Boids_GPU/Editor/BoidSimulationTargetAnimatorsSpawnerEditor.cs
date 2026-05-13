using UnityEditor;
using UnityEngine;

namespace OceanX.BoidsGPU
{
    [CustomEditor(typeof(BoidSimulationTargetAnimatorsSpawner))]
    public class BoidSimulationTargetAnimatorsSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            BoidSimulationTargetAnimatorsSpawner boidSimulationTargetAnimatorsSpawner = (BoidSimulationTargetAnimatorsSpawner)target;

            if (GUILayout.Button(nameof(boidSimulationTargetAnimatorsSpawner.CreateTargets)))
            {
                boidSimulationTargetAnimatorsSpawner.CreateTargets();
            }

            if (GUILayout.Button(nameof(boidSimulationTargetAnimatorsSpawner.DeleteTargets)))
            {
                boidSimulationTargetAnimatorsSpawner.DeleteTargets();
            }
        }
    }
}