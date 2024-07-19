using UnityEngine;

namespace UltimateReplay.Demo
{
    public class SimpleRespawn : MonoBehaviour
    {
        // Methods
        private void OnTriggerEnter(Collider other)
        {
            // Check for controller
            SimpleCharacter controller = other.GetComponentInParent<SimpleCharacter>();

            // Respawn player
            if (controller != null)
                controller.Respawn();
        }
    }
}
