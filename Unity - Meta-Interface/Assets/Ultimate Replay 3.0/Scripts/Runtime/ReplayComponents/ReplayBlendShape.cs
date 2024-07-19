using System;
using System.Collections.Generic;
using UltimateReplay.Formatters;
using UnityEngine;

namespace UltimateReplay
{
    public class ReplayBlendShape : ReplayRecordableBehaviour
    {
        // Private
        private static readonly ReplayBlendShapeFormatter formatter = new ReplayBlendShapeFormatter();

        private ReplayBlendShapeFormatter.ReplayBlendShapeSerializeFlags serializeFlags = 0;

        private List<float> lastWeights = new List<float>();
        private List<float> targetWeights = new List<float>();

        // Public
        public SkinnedMeshRenderer observedSkinnedMeshRenderer;
        public bool interpolate = true;

        // Methods
        public void Start()
        {
            if (observedSkinnedMeshRenderer == null || observedSkinnedMeshRenderer.sharedMesh == null)
                Debug.LogWarningFormat("Replay blend shape '{0}' will not record or replay because the observed skinned mesh renderer has not been assigned or does not have a mesh assigned", this);
        }

        protected override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to auto-find skinned mesh renderer
            if (observedSkinnedMeshRenderer == null)
                observedSkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        protected override void OnReplayReset()
        {
            lastWeights.Clear();
            lastWeights.AddRange(targetWeights);
        }

        protected override void OnReplayUpdate(float t)
        {
            // Check for no component
            if (observedSkinnedMeshRenderer == null || observedSkinnedMeshRenderer.sharedMesh == null || observedSkinnedMeshRenderer.enabled == false)
                return;

            if (interpolate == true)
            {
                // Get the array size
                int weightsCount = observedSkinnedMeshRenderer.sharedMesh.blendShapeCount;

                // Set all points
                if (targetWeights.Count >= weightsCount)
                {
                    for (int i = 0; i < weightsCount; i++)
                    {                        
                        // Interpolate the value
                        float updateWeight = Mathf.Lerp(lastWeights[i], targetWeights[i], t);                        

                        // Set weight
                        observedSkinnedMeshRenderer.SetBlendShapeWeight(i, updateWeight);
                    }
                }
            }
        }

        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedSkinnedMeshRenderer == null || observedSkinnedMeshRenderer.sharedMesh == null || observedSkinnedMeshRenderer.enabled == false)
                return;

            // Sample all weights
            formatter.UpdateFromSkinnedRenderer(observedSkinnedMeshRenderer, serializeFlags);

            // Serialize the data
            formatter.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedSkinnedMeshRenderer == null || observedSkinnedMeshRenderer.sharedMesh == null)
                return;

            // Reset data
            OnReplayReset();

            // Deserialize the data
            formatter.OnReplayDeserialize(state);

            // Check for interpolation
            if(interpolate == true)
            {
                // Set our targets to aim for
                targetWeights.Clear();
                targetWeights.AddRange(formatter.BlendWeights);
            }
            else
            {
                // Update renderer directly
                formatter.SyncSkinnedRenderer(observedSkinnedMeshRenderer);
            }
        }
    }
}