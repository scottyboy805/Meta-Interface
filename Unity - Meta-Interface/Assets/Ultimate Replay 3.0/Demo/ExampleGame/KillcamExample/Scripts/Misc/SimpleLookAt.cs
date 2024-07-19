using UnityEngine;

namespace UltimateReplay.Demo
{
    public class SimpleLookAt : MonoBehaviour
    {
        // Public
        public Transform target;

        // Methods
        private void Update()
        {
            if (target != null)
                transform.LookAt(target);
        }
    }
}
