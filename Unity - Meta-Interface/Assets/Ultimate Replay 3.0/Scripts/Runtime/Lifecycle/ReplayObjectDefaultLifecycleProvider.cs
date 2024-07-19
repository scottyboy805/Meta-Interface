using System;
using UnityEngine;

namespace UltimateReplay.Lifecycle
{
    [Serializable]
    public class ReplayObjectDefaultLifecycleProvider : ReplayObjectLifecycleProvider
    {
        // Private
        private ReplayObjectPool pool = null;

        // Public
        public ReplayObject replayPrefab;
        public bool allowPooling = true;

        // Properties
        public override bool IsAssigned
        {
            get { return replayPrefab != null; }
        }

        public override string ItemName
        {
            get { return replayPrefab != null ? replayPrefab.name : string.Empty; }
        }

        public override ReplayIdentity ItemPrefabIdentity
        {
            get { return replayPrefab != null ? replayPrefab.PrefabIdentity : ReplayIdentity.invalid; }
        }

        // Methods
        private void OnEnable()
        {
            pool = new ReplayObjectPool(InstantiateReplayInstance, DestroyReplayInstance);
        }

        public override ReplayObject InstantiateReplayInstance(Vector3 position, Quaternion rotation)
        {
            // Check for assigned
            if (replayPrefab != null)
            {
                // Check for pooled
                if (allowPooling == true)
                {
                    // Get instance
                    ReplayObject pooledInstance = pool.Get();

                    // Update transform
                    pooledInstance.transform.position = position;
                    pooledInstance.transform.rotation = rotation;

                    return pooledInstance;
                }
                else
                {
                    // Create instance
                    return Instantiate(replayPrefab, position, rotation);
                }
            }

            return null;
        }

        public ReplayObject InstantiateReplayInstance()
        {
            return Instantiate(replayPrefab);
        }

        public override void DestroyReplayInstance(ReplayObject replayInstance)
        {
            if (allowPooling == true)
            {
                pool.Release(replayInstance);
            }
            else
            {
                // Destroy
                Destroy(replayInstance.gameObject);
            }
        }
    }
}
