using UltimateReplay.Formatters;
using UnityEngine;

namespace UltimateReplay
{
    public class ReplayParentChange : ReplayRecordableBehaviour
    {
        // Private
        private static readonly ReplayParentChangeFormatter formatter = new ReplayParentChangeFormatter();

        private Transform parentTransform = null;
        private ReplayObject parentReplayObject = null;

        // Properties
        public override ReplayFormatter Formatter
        {
            get { return formatter; }
        }

        // Methods
        protected override void Awake()
        {
            base.Awake();

            // Get current parent
            parentTransform = transform.parent;
            parentReplayObject = (parentTransform != null) ? parentTransform.GetComponent<ReplayObject>() : null;
        }

        public override void OnReplaySerialize(ReplayState state)
		{
            // Check for parent changed
            if(transform.parent != parentTransform)
            {
                parentTransform = transform.parent;
                parentReplayObject = (parentTransform != null) ? parentTransform.GetComponent<ReplayObject>() : null;
            }

            // Update formatter
            formatter.ParentIdentity = parentReplayObject != null
                ? parentReplayObject.ReplayIdentity
                : new ReplayIdentity(0);

            // Serialize
            formatter.OnReplaySerialize(state);
		}

		public override void OnReplayDeserialize(ReplayState state)
        {
            // Deserialize
            formatter.OnReplayDeserialize(state);

            // Sync parent info
            if (transform.parent != parentTransform)
            {
                parentTransform = transform.parent;
                parentReplayObject = (parentTransform != null) ? parentTransform.GetComponent<ReplayObject>() : null;
            }

            // Get replay id
            ReplayIdentity targetParentID = formatter.ParentIdentity;

            // Check for parent changed
            if((parentReplayObject == null && targetParentID.ID != 0) || (parentReplayObject != null && targetParentID != parentReplayObject.ReplayIdentity))
            {
                // Check for no parent
                if (targetParentID.ID == 0)
                {
                    transform.parent = null;
                    parentTransform = null;
                    parentReplayObject = null;
                }
                else
                {
                    // Try to find the new parent target
                    ReplayObject targetParentReplayObject = ReplayObject.PlaybackOperation.Scene.GetReplayObject(targetParentID);

                    // Check for found
                    if(targetParentReplayObject == null)
                    {
                        Debug.LogWarning("Could not find replay object parent with ID: " + targetParentID);
                        return;
                    }

                    // Update parent
                    transform.parent = targetParentReplayObject.transform;
                    parentTransform = targetParentReplayObject.transform;
                    parentReplayObject = targetParentReplayObject;
                }
            }
        }        
    }
}