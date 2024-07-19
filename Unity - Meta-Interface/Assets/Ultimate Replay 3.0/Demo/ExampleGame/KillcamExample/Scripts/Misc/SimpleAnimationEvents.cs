using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateReplay.Demo
{
    public class SimpleAnimationEvents : MonoBehaviour
    {
        // Public
        public GameObject playerRoot;
        public AudioSource landSound;
        public AudioSource footstepSound;

        public AudioClip[] landSfxClips;
        public AudioClip[] footstepSfxClips;

        public SimpleProjectile fireProjectilePrefab;
        public Transform fireProjectileLocation;

        // Methods
        private void OnLand()
        {
            if (landSound != null)
            {
                // Select random clip
                if (landSfxClips.Length > 0)
                    landSound.clip = landSfxClips[Random.Range(0, landSfxClips.Length)];

                // Play sound
                landSound.Play();
            }
        }

        private void OnFootstep()
        {
            if (footstepSound != null)
            {
                // Select random clip
                if (footstepSfxClips.Length > 0)
                    footstepSound.clip = footstepSfxClips[Random.Range(0, footstepSfxClips.Length)];

                // PLay sound
                footstepSound.Play();
            }
        }

        private void OnThrow()
        {
            // Create instance
            SimpleProjectile projectile = Instantiate(fireProjectilePrefab, fireProjectileLocation.position, fireProjectileLocation.rotation);

            // Fire projectile
            projectile.Fire(playerRoot, fireProjectileLocation.forward);

            // Add to replay scene
            ReplayManager.AddReplayObjectToRecordScenes(projectile.gameObject);
        }
    }
}