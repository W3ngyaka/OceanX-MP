using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Class that injects affecters into every boid spawner of the boids simulation.
    /// These affecters are treated as global and should affect every boid in the simulation.
    /// </summary>
    public class GlobalAffectersInjector : MonoBehaviour
    {
        [SerializeField] private BoidSimulationBase _boidSimulation = null;
        [SerializeField] private SimulationAffecterComponent[] _affecters = null;
        
        public void InjectObstacles()
        {
            // First, we need to make sure that obstacles affect all boids in the simulation.
            foreach(SimulationAffecterComponent obstacle in  _affecters)
            {
                obstacle.SetAffecterID(SimulationAffecter.ALL_BOIDS_AFFECTER_ID);
            }

            // Inject obstacles as global obstacles to the simulation.
            _boidSimulation.AddGlobalAffecters(_affecters);
        }
    }
}