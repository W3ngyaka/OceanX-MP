using UnityEngine;

namespace OceanX
{
    public class TransformFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target = null;

        private void Update()
        {
            transform.position = _target.position;
        }
    }
}