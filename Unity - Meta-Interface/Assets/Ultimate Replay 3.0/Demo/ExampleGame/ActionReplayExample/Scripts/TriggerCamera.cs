using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UltimateReplay.Demo
{
    public class TriggerCamera : MonoBehaviour
    {
        // Events
        public UnityEvent OnTriggerStateChanged;

        // Private
        private List<Collider> triggeringObjects = new List<Collider>();

        // Public
        public Camera cam;
            
        // Properties
        public bool HasTriggerObjects
        {
            get { return triggeringObjects.Count > 0; }
        }

        // Methods
        private void LateUpdate()
        {
            if(triggeringObjects.Count > 0)
            {
                // Look at our target
                cam.transform.LookAt(triggeringObjects[0].transform);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Make sure a car is triggering
            if (other.GetComponentInParent<CarController>() == null)
                return;

            triggeringObjects.Add(other);

            // Trigger event
            OnTriggerStateChanged.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            // Make sure a car is triggering
            if (other.GetComponentInParent<CarController>() == null)
                return;

            triggeringObjects.Remove(other);

            // Trigger event
            OnTriggerStateChanged.Invoke();
        }
    }
}
