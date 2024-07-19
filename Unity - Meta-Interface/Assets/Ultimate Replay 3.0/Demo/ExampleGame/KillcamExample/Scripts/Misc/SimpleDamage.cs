using System;
using UnityEngine;
using UnityEngine.Events;

namespace UltimateReplay.Demo
{
    /// <summary>
    /// A simple health script used in the demo scenes.
    /// </summary>
    public class SimpleDamage : MonoBehaviour
    {
        // Events
        public UnityEvent<GameObject> OnKilled;

        // Public
        /// <summary>
        /// The health value for the damageable item.
        /// </summary>
        public float health = 1f;

        // Properties
        public bool IsDead
        {
            get { return health <= 0; }
        }

        // Methods
        /// <summary>
        /// Apply the specified amount fo damage.
        /// </summary>
        /// <param name="amount">The amount of damage to apply</param>
        public void TakeDamage(float amount, GameObject damagingObject = null)
        {
            if (health > 0)
            {
                health -= amount;

                if (health < 0)
                {
                    health = 0;
                    if (OnKilled != null)
                    {
                        Debug.LogError("Killed");
                        OnKilled.Invoke(damagingObject);
                    }
                    //Destroy(gameObject);
                }
            }
        }

        public void RestoreHealth()
        {
            health = 1f;
        }
    }
}
