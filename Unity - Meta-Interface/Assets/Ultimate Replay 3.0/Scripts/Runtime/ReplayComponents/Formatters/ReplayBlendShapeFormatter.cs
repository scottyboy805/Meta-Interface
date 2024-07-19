using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayBlendShapeFormatter
    {
        // Types
        [Flags]
        internal enum ReplayBlendShapeSerializeFlags : byte
        {
            None = 0,
            LowPrecision = 1 << 1,
            LowPrecisionCount = 1 << 2,
        }

        // Private
        private ReplayBlendShapeSerializeFlags serializeFlags = 0;
        private List<float> blendWeights = new List<float>();

        // Properties
        internal ReplayBlendShapeSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        public IList<float> BlendWeights
        {
            get { return blendWeights; }
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            // Write flags
            state.Write((byte)serializeFlags);

            // Write count
            if ((serializeFlags & ReplayBlendShapeSerializeFlags.LowPrecisionCount) != 0)
            {
                state.Write((byte)blendWeights.Count);
            }
            else
            {
                state.Write((ushort)blendWeights.Count);
            }

            // Write all weights
            for (int i = 0; i < blendWeights.Count; i++)
            {
                if ((serializeFlags & ReplayBlendShapeSerializeFlags.LowPrecision) != 0)
                {
                    // Write half precision
                    state.WriteHalf(blendWeights[i]);
                }
                else
                {
                    // Write full precision
                    state.Write(blendWeights[i]);
                }
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            serializeFlags = (ReplayBlendShapeSerializeFlags)state.ReadByte();

            int count = 0;

            // Read count
            if ((serializeFlags & ReplayBlendShapeSerializeFlags.LowPrecisionCount) != 0)
            {
                count = state.ReadByte();
            }
            else
            {
                count = (int)state.ReadUInt16();
            }

            // Read all weights
            for (int i = 0; i < count; i++)
            {
                if ((serializeFlags & ReplayBlendShapeSerializeFlags.LowPrecision) != 0)
                {
                    // Read half precision
                    blendWeights.Add(state.ReadHalf());
                }
                else
                {
                    // Read full precision
                    blendWeights.Add(state.ReadSingle());
                }
            }
        }

        public void SyncSkinnedRenderer(SkinnedMeshRenderer sync)
        {
            // Apply all blend shapes
            for(int i = 0; i < blendWeights.Count; i++)
            {
                sync.SetBlendShapeWeight(i, blendWeights[i]);
            }
        }

        public void UpdateFromSkinnedRenderer(SkinnedMeshRenderer from)
        {
            // Calculate serialize flags
            ReplayBlendShapeSerializeFlags flags = GetSerializeFlags(from.sharedMesh.blendShapeCount, false);

            // Update from renderer
            UpdateFromSkinnedRenderer(from, flags);
        }

        internal void UpdateFromSkinnedRenderer(SkinnedMeshRenderer from, ReplayBlendShapeSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;

            // Clear old weights
            blendWeights.Clear();

            // Get weights
            int blendShapeCount = from.sharedMesh.blendShapeCount;

            for (int i = 0; i < blendShapeCount; i++)
            {
                blendWeights.Add(from.GetBlendShapeWeight(i));
            }
        }

        internal static ReplayBlendShapeSerializeFlags GetSerializeFlags(int blendShapeCount, bool lowPrecision)
        {
            ReplayBlendShapeSerializeFlags flags = 0;

            // Low count
            if (blendShapeCount < byte.MaxValue) flags |= ReplayBlendShapeSerializeFlags.LowPrecisionCount;

            // Low precision
            if (lowPrecision == true) flags |= ReplayBlendShapeSerializeFlags.LowPrecision;

            return flags;
        }
    }
}
