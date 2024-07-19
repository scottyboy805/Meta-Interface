using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateReplay.Demo
{
    public abstract class SimpleCharacter : ReplayBehaviour
    {
        // Private
        private Animator anim = null;
        private Vector3 startPosition = Vector3.zero;
        private Quaternion startRotation = Quaternion.identity;

        // Public
        public Transform characterRoot;
        public SimpleDamage damage;
        public SimpleRagdoll ragdoll;

        public Renderer torsoRenderer;

        // Properties
        public Vector3 StartPosition
        {
            get { return startPosition; }
        }

        public Quaternion StartRotation
        {
            get { return startRotation; }
        }

        // Methods
        protected override void Awake()
        {
            // Call base
            base.Awake();

            // Get animator
            anim = GetComponentInChildren<Animator>();
        }

        protected virtual void Start()
        {
            // Store start location
            startPosition = transform.position;
            startRotation = transform.rotation;

            // Add listener
            if (damage != null)
                damage.OnKilled.AddListener(OnKilled);
        }

        public void Respawn()
        {
            // Deactivate ragdoll
            ragdoll.DeactivateRagdoll();

            // Reset position
            transform.position = startPosition;
            transform.rotation = startRotation;

            // Reset health
            damage.RestoreHealth();

            // Trigger event
            OnRespawn();
        }

        public void UpdateAnimation(Vector3 velocity, bool jump, bool grounded, bool throwing)
        {
            anim.SetFloat("Speed", velocity.magnitude);
            anim.SetFloat("MotionSpeed", 1f);
            anim.SetBool("Jump", jump);
            anim.SetBool("Grounded", grounded);
            anim.SetBool("FreeFall", grounded == false && jump == false);
            anim.SetBool("Throw", throwing);
        }

        protected virtual void OnRespawn()
        {
            // Disable ragdoll
            ragdoll.DeactivateRagdoll();

            // Enable animation
            anim.enabled = true;
            anim.SetLayerWeight(2, 0f);
            
            anim.Rebind();
            anim.SetLayerWeight(2, 1f);
        }

        protected virtual void OnKilled(GameObject killedBy)
        {
            // Disable animation
            anim.enabled = false;

            // Enable ragdoll
            ragdoll.ActivateRagDoll();
        }

        public void SetTorsoRenderer(bool active)
        {
            torsoRenderer.shadowCastingMode = (active == true)
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}