using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayRiggedHumanoidFormatter : ReplayFormatter
    {
        // Type
        [Flags]
        internal enum ReplayRiggedHumanoidSerializeFlags : byte
        {
            None = 0,
            BodyPosition = 1 << 1,
            LowPrecisionBodyPosition = 1 << 2,
            BodyRotation = 1 << 3,
            LowPrecisionBodyRotation = 1 << 4,
            MuscleValues = 1 << 5,
            LowPrecisionMuscleValues = 1 << 6,
        }

        // Private
        [ReplayTokenSerialize("Serialize Flags")]
        private ReplayRiggedHumanoidSerializeFlags serializeFlags = 0;
        [ReplayTokenSerialize("Body Position")]
        private Vector3 bodyPosition = Vector3.zero;
        [ReplayTokenSerialize("Body Rotation")]
        private Quaternion bodyRotation = Quaternion.identity;
        [ReplayTokenSerialize("Muscle Values")]
        private List<float> muscleValues = new List<float>();

        // Properties
        internal ReplayRiggedHumanoidSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        public Vector3 BodyPosition => bodyPosition;
        public Quaternion BodyRotation => bodyRotation;
        public IReadOnlyList<float> MuscleValues => muscleValues;
        
        // Methods
        public override void OnReplaySerialize(ReplayState state)
        {
            // Get flags
            ReplayRiggedHumanoidSerializeFlags flags = serializeFlags;

            // Write flags
            state.Write((ushort)flags);

            // Record body position
            if((flags & ReplayRiggedHumanoidSerializeFlags.BodyPosition) != 0)
            {
                // Check for low precision
                if((flags & ReplayRiggedHumanoidSerializeFlags.LowPrecisionBodyPosition) != 0)
                {
                    state.WriteHalf(bodyPosition);
                }
                else
                {
                    state.Write(bodyPosition);
                }
            }

            // Record body rotation
            if((flags & ReplayRiggedHumanoidSerializeFlags.BodyRotation) != 0)
            {
                // Check for low precision
                if((flags & ReplayRiggedHumanoidSerializeFlags.LowPrecisionBodyRotation) != 0)
                {
                    state.WriteHalf(bodyRotation);
                }
                else
                {
                    state.Write(bodyRotation);
                }
            }

            // Record muscle values
            if ((flags & ReplayRiggedHumanoidSerializeFlags.MuscleValues) != 0)
            {
                // Record size
                state.Write((ushort)muscleValues.Count);

                // Write all
                for (int i = 0; i < muscleValues.Count; i++)
                {
                    // Check for low precision
                    if ((flags & ReplayRiggedHumanoidSerializeFlags.LowPrecisionMuscleValues) != 0)
                    {
                        state.WriteHalf(muscleValues[i]);
                    }
                    else
                    {
                        state.Write(muscleValues[i]);
                    }
                }
            }
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            ReplayRiggedHumanoidSerializeFlags flags = serializeFlags = (ReplayRiggedHumanoidSerializeFlags)state.ReadUInt16();

            // Clear old data
            muscleValues.Clear();

            // Read body position
            if((flags & ReplayRiggedHumanoidSerializeFlags.BodyPosition) != 0)
            {
                // Check for low precision
                if((flags & ReplayRiggedHumanoidSerializeFlags.LowPrecisionBodyPosition) != 0)
                {
                    this.bodyPosition.x = state.ReadHalf();
                    this.bodyPosition.y = state.ReadHalf();
                    this.bodyPosition.z = state.ReadHalf();
                }
                else
                {
                    this.bodyPosition.x = state.ReadSingle();
                    this.bodyPosition.y = state.ReadSingle();
                    this.bodyPosition.z = state.ReadSingle();
                }
            }

            // Read body rotation
            if((flags & ReplayRiggedHumanoidSerializeFlags.BodyRotation) != 0)
            {
                // Check for low precision
                if((flags & ReplayRiggedHumanoidSerializeFlags.LowPrecisionBodyRotation) != 0)
                {
                    this.bodyRotation.x = state.ReadHalf();
                    this.bodyRotation.y = state.ReadHalf();
                    this.bodyRotation.z = state.ReadHalf();
                    this.bodyRotation.w = state.ReadHalf();
                }
                else
                {
                    this.bodyRotation.x = state.ReadSingle();
                    this.bodyRotation.y = state.ReadSingle();
                    this.bodyRotation.z = state.ReadSingle();
                    this.bodyRotation.w = state.ReadSingle();
                }
            }

            // Read muscle values
            if((flags & ReplayRiggedHumanoidSerializeFlags.MuscleValues) != 0)
            {
                // Read size
                int size = state.ReadUInt16();

                // Ensure capacity - single allocation
                if (muscleValues.Capacity < size)
                    muscleValues.Capacity = size;

                // Read all
                for(int i = 0; i < size; i++)
                {
                    // Check for low precision
                    if ((flags & ReplayRiggedHumanoidSerializeFlags.LowPrecisionMuscleValues) != 0)
                    {
                        muscleValues.Add(state.ReadHalf());
                    }
                    else
                    {
                        muscleValues.Add(state.ReadSingle());
                    }
                }
            }
        }

        internal void UpdateFromPose(in HumanPose pose, ReplayRiggedHumanoidSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;

            // Clear old muscles
            muscleValues.Clear();

            // Position
            if((flags & ReplayRiggedHumanoidSerializeFlags.BodyPosition) != 0)
            {
                this.bodyPosition = pose.bodyPosition;
            }

            // Rotation
            if((flags & ReplayRiggedHumanoidSerializeFlags.BodyRotation) != 0)
            {
                this.bodyRotation = pose.bodyRotation;
            }

            // Muscles
            if((flags & ReplayRiggedHumanoidSerializeFlags.MuscleValues) != 0)
            {
                // Copy values - Use add range for array copy optimization and single allocation
                muscleValues.AddRange(pose.muscles);
            }
        }

        internal static ReplayRiggedHumanoidSerializeFlags GetSerializeFlags(RecordFullAxisFlags bodyPosition, RecordPrecision bodyPositionPrecision, RecordFullAxisFlags bodyRotation, RecordPrecision bodyRotationPrecision, RecordPrecision muscleValuePrecision = RecordPrecision.FullPrecision32Bit)
        {
            ReplayRiggedHumanoidSerializeFlags flags = 0;

            // Body position
            if ((bodyPosition & RecordFullAxisFlags.XYZ) != 0) flags |= ReplayRiggedHumanoidSerializeFlags.BodyPosition;
            if ((bodyPositionPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedHumanoidSerializeFlags.LowPrecisionBodyPosition;

            // Body rotation
            if ((bodyRotation & RecordFullAxisFlags.XYZ) != 0) flags |= ReplayRiggedHumanoidSerializeFlags.BodyRotation;
            if ((bodyRotationPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedHumanoidSerializeFlags.LowPrecisionBodyRotation;

            // Muscle values
            flags |= ReplayRiggedHumanoidSerializeFlags.MuscleValues;
            if ((muscleValuePrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedHumanoidSerializeFlags.LowPrecisionMuscleValues;

            return flags;
        }
    }
}
