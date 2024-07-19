/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/Lifecycle/ReplayObjectCustomLifecycleProvider.cs
/// </summary>
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
            get
            {
                return customProvider != null ? customProvider.IsAssigned : false;
            }
        }

        public override string ItemName
        {
            get
            {
                return customProvider != null ? customProvider.ItemName : "None";
            }
        }

        public override ReplayIdentity ItemPrefabIdentity
        {
            get
            {
                return customProvider != null ? customProvider.ItemPrefabIdentity : ReplayIdentity.invalid;
            }
        }

        // Methods
        public override ReplayObject InstantiateReplayInstance(Vector3 position, Quaternion rotation) => throw new System.NotImplementedException();
        public override void DestroyReplayInstance(ReplayObject replayInstance) => throw new System.NotImplementedException();
    }
}