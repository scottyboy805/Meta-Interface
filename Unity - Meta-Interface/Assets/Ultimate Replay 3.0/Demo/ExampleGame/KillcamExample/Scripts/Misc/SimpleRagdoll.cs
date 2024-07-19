using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UltimateReplay.Demo
{
    public class SimpleRagdoll : MonoBehaviour
    {
        // Private
        private Collider[] ragdollColliders = null;
        private Rigidbody[] ragdollRigidbodies = null;

        // Public
        public Transform ragdollRoot;

        // Methods
        public void Awake()
        {
            ragdollColliders = ragdollRoot.GetComponentsInChildren<Collider>();
            ragdollRigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>();

            SetActiveColliders(ragdollColliders, false);
            SetActiveRigidBody(ragdollRigidbodies, false);
        }

        public void ActivateRagDoll()
        {
            SetActiveColliders(ragdollColliders, true);
            SetActiveRigidBody(ragdollRigidbodies, true);
        }

        public void DeactivateRagdoll()
        {
            SetActiveColliders(ragdollColliders, false);
            SetActiveRigidBody(ragdollRigidbodies, false);
        }

        private void SetActiveColliders(Collider[] colliders, bool active)
        {
            foreach (Collider c in colliders)
                c.enabled = active;
        }

        private void SetActiveRigidBody(Rigidbody[] bodies, bool active)
        {
            foreach (Rigidbody r in bodies)
            {
                r.isKinematic = active == false;

                //if (active == true)
                //{
                //    r.velocity = Vector3.zero;
                //    r.angularVelocity = Vector3.zero;
                //}
            }
        }
    }
}
