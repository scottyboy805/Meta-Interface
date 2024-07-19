using System.Collections;
using UnityEngine;

namespace UltimateReplay.Demo
{
    public class SimpleProjectile : MonoBehaviour
    {
        // Private
        private Rigidbody body = null;
        private GameObject owner = null;
        private bool isLethal = true;

        // Public
        public float speed = 10;
        public float rotateSpeed = 8;

        public AudioSource throwSound;
        public AudioSource impactSoundSurface;
        public AudioSource impactSoundBounce;

        public AudioClip[] throwSfxClips;
        public AudioClip[] impactSurfaceSfxClips;
        public AudioClip[] impactBounceSfxClips;

        // Methods
        public void Fire(GameObject owner, Vector3 direction)
        {
            this.owner = owner;

            // Get rigid body
            CharacterController ownerBody = owner.GetComponentInParent<CharacterController>();

            // Calcualte additional force
            float additionalSpeed = 1f;

            if (ownerBody != null)
            {
                additionalSpeed += ownerBody.velocity.magnitude;
            }

            // Remove constraint
            if(rotateSpeed > body.maxAngularVelocity)
                body.maxAngularVelocity = rotateSpeed;

            body.velocity = direction * (speed + additionalSpeed);
            body.angularVelocity = transform.right * rotateSpeed;

            // Trigger sound effect
            if(throwSound != null)
            {
                // Select random clip
                if(throwSfxClips.Length > 0)
                {
                    throwSound.clip = throwSfxClips[Random.Range(0, throwSfxClips.Length)];
                }

                // Play sound effect
                throwSound.Play();
            }
        }

        public void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void OnCollisionEnter(Collision collision)
        {
            bool stuckinWall = false;


            SimpleDamage damage = collision.collider.GetComponentInParent<SimpleDamage>();

            if(damage != null && isLethal == true && owner != damage.gameObject)
            {
                damage.TakeDamage(2.6f, owner);

                // Reset velocity
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;

                // Disable body
                body.isKinematic = true;

                // Attach to collider
                body.transform.parent = collision.transform;
            }

            // Check for reduced velocity
            if (collision.relativeVelocity.magnitude < 2f)
                isLethal = false;
            

            // Check for stick in wall
            foreach(ContactPoint contact in collision.contacts)
            {
                if(contact.thisCollider.gameObject.name == "TipCollider")
                {
                    float dot = Vector3.Dot(contact.normal, Vector3.up);

                    // Check for floor
                    if (dot >= -0.8f && dot <= 0.2f)
                    {
                        // Reset velocity
                        body.velocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;

                        // Disable body
                        body.isKinematic = true;

                        // Set flag
                        stuckinWall = true;

                        // Trigger sound effect
                        if (impactSoundSurface != null)
                        {
                            // Select random clip
                            if (impactSurfaceSfxClips.Length > 0)
                            {
                                impactSoundSurface.clip = impactSurfaceSfxClips[Random.Range(0, impactSurfaceSfxClips.Length)];
                            }

                            // Play sound effect
                            impactSoundSurface.volume = Mathf.InverseLerp(0f, 25f, collision.relativeVelocity.magnitude);
                            impactSoundSurface.Play();
                        }
                    }
                }
            }

            // Play bound sound
            if(stuckinWall == false)
            {
                // Trigger sound effect
                if (impactSoundBounce != null)
                {
                    // Select random clip
                    if (impactBounceSfxClips.Length > 0)
                    {
                        impactSoundBounce.clip = impactBounceSfxClips[Random.Range(0, impactBounceSfxClips.Length)];
                    }

                    // Play sound effect
                    impactSoundBounce.volume = Mathf.InverseLerp(0f, 50f, collision.relativeVelocity.magnitude);
                    impactSoundBounce.Play();
                }
            }

            // Destroy after time
            StartCoroutine(DestroyAfterTime());
        }

        private IEnumerator DestroyAfterTime()
        {
            // Wait for destroy time
            yield return new WaitForSeconds(5);

            // Remove from replay scene
            ReplayManager.RemoveReplayObjectFromRecordScenes(gameObject);

            // Destroy projectile
            Destroy(gameObject);
        }
    }
}
