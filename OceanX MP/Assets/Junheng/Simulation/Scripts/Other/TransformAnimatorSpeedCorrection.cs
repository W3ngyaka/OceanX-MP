using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Class that corrects the animation speed of the transform to match the simulation
    /// speed of the boid simulation.
    /// </summary>
    public class TransformAnimatorSpeedCorrection : MonoBehaviour
    {
        [SerializeField] private BoidSimulationBase _boidSimulation = null;
        [SerializeField] private List<TransformAnimator> _transformAnimators = null;

        private void Update()
        {
            if(_transformAnimators == null || _transformAnimators.Count == 0)
            {
                _transformAnimators = GetComponentsInChildren<TransformAnimator>().ToList();
            }

            float currentSimulationSpeed = _boidSimulation.SimulationSpeed;
            for(int i = 0; i <  _transformAnimators.Count; i++)
            {
                TransformAnimator transformAnimator = _transformAnimators[i];
                if(transformAnimator == null)
                {
                    _transformAnimators.RemoveAt(i);
                    i--;
                    continue;
                }

                transformAnimator.AnimationSpeedMultiplier = currentSimulationSpeed;
            }   
        }
    }
}