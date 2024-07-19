using System;
using UnityEngine;

namespace UltimateReplay.Lifecycle
{
    [Serializable]
    public abstract class ReplayObjectLifecycleProvider : ScriptableObject
    {
        // Properties
        public abstract bool IsAssigned { get; }

        public abstract string ItemName { get; }

        public abstract ReplayIdentity ItemPrefabIdentity { get; }

        // Methods
        public abstract ReplayObject InstantiateReplayInstance(Vector3 position, Quaternion rotation);

        public abstract void DestroyReplayInstance(ReplayObject replayInstance);

        public static void DestroyReplayObject(ReplayObject obj)
        {
            if (obj == null)
                return;

            // Check for provider
            if(obj.LifecycleProvider != null)
            {
                // Destroy using same provider that created the instance to support pooling
                obj.LifecycleProvider.DestroyReplayInstance(obj);
            }
            else
            {
                // Fallback to slow non-pooled destruction
                GameObject.Destroy(obj.gameObject);
            }
        }
    }
}
