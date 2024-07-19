using System;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayParentChangeFormatter : ReplayFormatter
    {
        // Private
        [ReplayTokenSerialize("Parent ID")]
        private ReplayIdentity parentIdentity = new ReplayIdentity(0);

        // Properties
        public ReplayIdentity ParentIdentity
        {
            get { return parentIdentity; }
            set { parentIdentity = value; }
        }

        public bool HasParent
        {
            get { return parentIdentity.ID != 0; }
        }

        // Methods
        public override void OnReplaySerialize(ReplayState state)
        {
            // Write identity
            state.Write(parentIdentity);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Read identity
            parentIdentity = state.ReadIdentity();
        }

        public void UpdateFromTransform(Transform from)
        {
            // Check for null
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            // Get the parent
            Transform parent = from.parent;
            ReplayObject parentReplay;
            
            // Check for replay object
            if(parent != null && (parentReplay = parent.GetComponentInParent<ReplayObject>()) != null)
            {
                // Use the parent id
                parentIdentity = parentReplay.ReplayIdentity;
            }
            else
            {
                // Use the default id
                parentIdentity = ReplayIdentity.invalid;
            }
        }

        public void SyncTransform(Transform sync, ReplayScene scene)
        {
            // Check for null
            if (sync == null)
                throw new ArgumentNullException(nameof(sync));

            // Check for null scene
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            // Check for any identity
            if(parentIdentity == ReplayIdentity.invalid)
            {
                // Update parent
                if (sync.parent != null)
                    sync.parent = null;
            }
            else
            {
                // Get the parent
                Transform parent = sync.parent;

                if (parent == null || parent.GetComponentInParent<ReplayObject>().ReplayIdentity != parentIdentity)
                {
                    // Find the target parent object
                    ReplayObject newParent = scene.GetReplayObject(parentIdentity); 

                    // Check for any
                    if(newParent != null)
                        sync.parent = newParent.transform;
                }
            }
        }
    }
}