using UnityEngine;

namespace UltimateReplay.Lifecycle
{
    public sealed class ReplayObjectCustomLifecycleProvider : ReplayObjectLifecycleProvider
    {
        // Public
        public ReplayObjectLifecycleProvider customProvider;

        // Properties
        public override bool IsAssigned
        {
            get { return customProvider != null ? customProvider.IsAssigned : false; }
        }

        public override string ItemName
        {
            get { return customProvider != null ? customProvider.ItemName : "None"; }
        }

        public override ReplayIdentity ItemPrefabIdentity
        {
            get { return customProvider != null ? customProvider.ItemPrefabIdentity : ReplayIdentity.invalid; }
        }

        // Methods
        public override ReplayObject InstantiateReplayInstance(Vector3 position, Quaternion rotation)
        {
            return customProvider != null ? customProvider.InstantiateReplayInstance(position, rotation) : null;
        }

        public override void DestroyReplayInstance(ReplayObject replayInstance)
        {
            if (customProvider != null)
                customProvider.DestroyReplayInstance(replayInstance);
        }   
    }
}
