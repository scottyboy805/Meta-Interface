using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace UltimateReplay.Demo
{
    public class SimpleEnemy : SimpleCharacter
    {
        // Private
        private NavMeshAgent agent = null;
        private float lastShootTime = 0;

        // Public
        public Collider mainCollider;
        public Transform playerTarget;        
        public float viewRange = 10;
        public float viewField = 0.6f;
        public float shootRange = 8;

        // Methods
        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
        }

        protected override void Start()
        {
            base.Start();

            // Try to auto-find target
            if (playerTarget == null)
            {
                // Try to find player
                GameObject result = GameObject.Find("PlayerController");

                // Update target tarnsform
                if (result != null)
                    playerTarget = result.transform;
            }
        }

        public void Update()
        {
            // Check for dead or replay occuring
            if (damage.IsDead == true || IsReplayingOrPaused == true)
                return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            Vector3 directionToPlayer = (playerTarget.position - transform.position);

            bool isInSight = false;

            // Check distance
            if (distanceToPlayer <= viewRange)
            {
                if (Vector3.Dot(transform.forward, directionToPlayer.normalized) > 0.6f)
                {
                    // Perform line of sight raycast
                    RaycastHit hit;
                    if (Physics.Raycast(new Ray(transform.position, directionToPlayer.normalized), out hit) == true)
                    {
                        // Check for matching hit object
                        if (hit.transform == playerTarget)
                            isInSight = true;
                    }
                }
            }



            // Check for not in sight and move to player
            if (isInSight == false)
            {
                agent.SetDestination(playerTarget.transform.position);
                agent.updateRotation = true;
            }

            bool throwKnife = false;

            // Create line of sight ray
            Vector3 direction = (transform.position - playerTarget.position).normalized;

            // Calcualte start offset
            Vector3 startOffset = transform.position + (direction * 1.5f);
            Vector3 endOffset = playerTarget.position - (direction * 1.5f);

            // Check for line of sight to player
            bool hasLineOfSight = Physics.Linecast(startOffset, endOffset);

            // Aim at player
            if(distanceToPlayer < shootRange && hasLineOfSight == true)
            {
                // Set the current destination to match the current position
                agent.SetDestination(transform.position);

                if(Vector3.Dot(transform.forward, directionToPlayer.normalized) > 0.98f)
                {
                    SimpleDamage playerDamage = playerTarget.GetComponent<SimpleDamage>();

                    if (playerDamage.IsDead == false && Time.time > lastShootTime + 0.7f)
                    {
                        //Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity).Fire(gameObject, bulletSpawn.forward);

                        // Shoot the player
                        //playerDamage.TakeDamage(0.6f, gameObject);

                        // Play muzzle flash
                        //muzzleFlash.Play();

                        // Throw a knife
                        throwKnife = true;

                        // Reset shoot timer
                        lastShootTime = Time.time;
                    }
                }
                else
                {
                    Vector3 lookDirection = directionToPlayer;
                    lookDirection.y = 0;

                    Quaternion look = Quaternion.LookRotation(lookDirection);

                    agent.updateRotation = false;

                    float lookRotationSpeed = agent.angularSpeed * Time.deltaTime * 0.2f;

                    if (lookRotationSpeed < 0.3f)
                        lookRotationSpeed = 0.3f;

                    if (distanceToPlayer < 4)
                        lookRotationSpeed = 1f;

                    transform.rotation = Quaternion.Slerp(transform.rotation, look, lookRotationSpeed);
                }
            }

            // Update animator
            UpdateAnimation(agent.velocity, false, true, throwKnife);
        }

        protected override void OnRespawn()
        {
            base.OnRespawn();

            // Enable agent
            agent.enabled = true;

            // Enable collider
            mainCollider.enabled = true;

            // Sync agent position
            agent.Warp(StartPosition);
        }

        protected override void OnKilled(GameObject killedBy)
        {
            base.OnKilled(killedBy);

            // Disable agent
            agent.enabled = false;

            // Disable collider
            mainCollider.enabled = false;
        }
    }
}
