using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayRiggedGenericFormatter : ReplayFormatter
    {
        // Type
        [Flags]
        internal enum ReplayRiggedGenericSerializeFlags : uint
        {
            None = 0,
            PosX = 1 << 0,
            PosY = 1 << 1,
            PosZ = 1 << 2,
            LowPrecisionPos = 1 << 3,

            PosXYZ = PosX | PosY | PosZ,

            RotX = 1 << 4,
            RotY = 1 << 5,
            RotZ = 1 << 6,
            LowPrecisionRot = 1 << 7,

            RotXYZ = RotX | RotY | RotZ,

            ScaX = 1 << 8,
            ScaY = 1 << 9,
            ScaZ = 1 << 10,
            LowPrecisionSca = 1 << 11,

            ScaXYZ = ScaX | ScaY | ScaZ,

            HasBones = 1 << 12,

            RootPosX = 1 << 13,
            RootPosY = 1 << 14,
            RootPosZ = 1 << 15,
            RootLowPrecisionPos = 1 << 16,

            RootRotX = 1 << 17,
            RootRotY = 1 << 18,
            RootRotZ = 1 << 19,
            RootLowPrecisionRot = 1 << 20,

            RootScaX = 1 << 21,
            RootScaY = 1 << 22,
            RootScaZ = 1 << 23,
            RootLowPrecisionSca = 1 << 24,
        }

        internal struct BoneTransform : IReplayTokenSerialize
        {
            // Private
            private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<BoneTransform>();

            // Public
            [ReplayTokenSerialize("Position")]
            public Vector3 position;
            [ReplayTokenSerialize("Rotation")]
            public Quaternion rotation;
            [ReplayTokenSerialize("Scale")]
            public Vector3 scale;

            // Methods
            #region TokenSerialize
            IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
            {
                foreach (ReplayToken token in tokens)
                {
                    if (token.IsOptional == false || includeOptional == true)
                        yield return token;
                }
            }
            #endregion
        }

        // Private
        [ReplayTokenSerialize("Serialize Flags")]
        private ReplayRiggedGenericSerializeFlags serializeFlags = 0;
        [ReplayTokenSerialize("Root Bone Transform")]
        private BoneTransform rootBoneTransform = new BoneTransform();
        [ReplayTokenSerialize("Bone Transforms")]
        private List<BoneTransform> boneTransforms = new List<BoneTransform>();

        // Properties
        internal ReplayRiggedGenericSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        public Vector3 RootPosition => rootBoneTransform.position;

        public RecordAxisFlags RootPositionAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootPosX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootPosY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootPosZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayRiggedGenericSerializeFlags.RootPosX | ReplayRiggedGenericSerializeFlags.RootPosY | ReplayRiggedGenericSerializeFlags.RootPosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootPosX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootPosY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootPosZ;
            }
        }

        public RecordPrecision RootPositionPrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low prescision set
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionPos) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayRiggedGenericSerializeFlags.RootLowPrecisionPos;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootLowPrecisionPos;
            }
        }

        public Quaternion RootRotation => rootBoneTransform.rotation;

        public RecordAxisFlags RootRotationAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayRiggedGenericSerializeFlags.RootPosX | ReplayRiggedGenericSerializeFlags.RootPosY | ReplayRiggedGenericSerializeFlags.RootPosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootRotX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootRotY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootRotZ;
            }
        }

        public RecordPrecision RootRotationPrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low prescision set
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionRot) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayRiggedGenericSerializeFlags.RootLowPrecisionRot;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootLowPrecisionRot;
            }
        }

        public Vector3 RootScale => rootBoneTransform.scale;
        public RecordAxisFlags RootScaleAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootScaX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootScaY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootScaZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayRiggedGenericSerializeFlags.RootPosX | ReplayRiggedGenericSerializeFlags.RootPosY | ReplayRiggedGenericSerializeFlags.RootPosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootScaX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootScaY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RootScaZ;
            }
        }

        public int BoneCount
        {
            get { return boneTransforms.Count; }
        }

        public RecordAxisFlags BonePositionAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.PosX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.PosY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.PosZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayRiggedGenericSerializeFlags.PosX | ReplayRiggedGenericSerializeFlags.PosY | ReplayRiggedGenericSerializeFlags.PosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.PosX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.PosY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.PosZ;
            }
        }

        public RecordPrecision BonePositionPrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low prescision set
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.LowPrecisionPos) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayRiggedGenericSerializeFlags.LowPrecisionPos;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayRiggedGenericSerializeFlags.LowPrecisionPos;
            }
        }

        public RecordAxisFlags BoneRotationAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RotX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RotY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RotZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayRiggedGenericSerializeFlags.PosX | ReplayRiggedGenericSerializeFlags.PosY | ReplayRiggedGenericSerializeFlags.PosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RotX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RotY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.RotZ;
            }
        }

        public RecordPrecision BoneRotationPrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low prescision set
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.LowPrecisionRot) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayRiggedGenericSerializeFlags.LowPrecisionRot;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayRiggedGenericSerializeFlags.LowPrecisionRot;
            }
        }

        public RecordAxisFlags BoneScaleAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.ScaX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.ScaY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.ScaZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayRiggedGenericSerializeFlags.PosX | ReplayRiggedGenericSerializeFlags.PosY | ReplayRiggedGenericSerializeFlags.PosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.ScaX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.ScaY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayRiggedGenericSerializeFlags.ScaZ;
            }
        }

        public RecordPrecision BoneScalePrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low prescision set
                if ((serializeFlags & ReplayRiggedGenericSerializeFlags.LowPrecisionSca) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayRiggedGenericSerializeFlags.LowPrecisionSca;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayRiggedGenericSerializeFlags.LowPrecisionSca;
            }
        }

        // Methods
        public override void OnReplaySerialize(ReplayState state)
        {
            // Get flags
            ReplayRiggedGenericSerializeFlags flags = serializeFlags;


            // Estimate byte size required so we can perform a single large allocation if needed
            int estimatedByteSize = 20 * boneTransforms.Count;      // 12 bytes per sample = poshalf(6), rothalf(6)
            state.EnsureCapacity(estimatedByteSize);

            // Write flags
            state.Write((uint)flags);

            // Write root bone
            #region RootBone
            // Record root position
            if ((flags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionPos) != 0)
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosX) != 0) state.WriteHalf(rootBoneTransform.position.x);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosY) != 0) state.WriteHalf(rootBoneTransform.position.y);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosZ) != 0) state.WriteHalf(rootBoneTransform.position.z);
            }
            else
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosX) != 0) state.Write(rootBoneTransform.position.x);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosY) != 0) state.Write(rootBoneTransform.position.y);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosZ) != 0) state.Write(rootBoneTransform.position.z);
            }

            // Record root rotation
            if ((flags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionRot) != 0)
            {
                // Serialize as quaternion if all axis recorded
                if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0)
                {
                    state.WriteHalf(rootBoneTransform.rotation.x);
                    state.WriteHalf(rootBoneTransform.rotation.y);
                    state.WriteHalf(rootBoneTransform.rotation.z);
                    state.WriteHalf(rootBoneTransform.rotation.w);
                }
                // Serialize as euler
                else
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0) state.WriteHalf(rootBoneTransform.rotation.eulerAngles.x);
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0) state.WriteHalf(rootBoneTransform.rotation.eulerAngles.y);
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0) state.WriteHalf(rootBoneTransform.rotation.eulerAngles.z);
                }
            }
            else
            {
                // Serialize as quaternion if all axis recorded
                if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0)
                {
                    state.Write(rootBoneTransform.rotation.x);
                    state.Write(rootBoneTransform.rotation.y);
                    state.Write(rootBoneTransform.rotation.z);
                    state.Write(rootBoneTransform.rotation.w);
                }
                // Serialize as euler
                else
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0) state.Write(rootBoneTransform.rotation.eulerAngles.x);
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0) state.Write(rootBoneTransform.rotation.eulerAngles.y);
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0) state.Write(rootBoneTransform.rotation.eulerAngles.z);
                }
            }

            // Record root scale
            if ((flags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionSca) != 0)
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaX) != 0) state.WriteHalf(rootBoneTransform.scale.x);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaY) != 0) state.WriteHalf(rootBoneTransform.scale.y);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaZ) != 0) state.WriteHalf(rootBoneTransform.scale.z);
            }
            else
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaX) != 0) state.Write(rootBoneTransform.scale.x);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaY) != 0) state.Write(rootBoneTransform.scale.y);
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaZ) != 0) state.Write(rootBoneTransform.scale.z);
            }
            #endregion

            // Check for no bones
            if ((flags & ReplayRiggedGenericSerializeFlags.HasBones) == 0)
                return;

            #region AllBones
            // Write bone count
            state.Write((ushort)boneTransforms.Count);

            // Write all transforms
            for (int i = 0; i < boneTransforms.Count; i++)
            {
                // Record position
                if ((flags & ReplayRiggedGenericSerializeFlags.LowPrecisionPos) != 0)
                {
                    // If all axis are recorded then write data as Vector3 for improved performance (less overhead of write calls and resize)
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosXYZ) == ReplayRiggedGenericSerializeFlags.PosXYZ)
                    {
                        state.WriteHalf(boneTransforms[i].position);
                    }
                    else
                    {
                        if ((flags & ReplayRiggedGenericSerializeFlags.PosX) != 0) state.WriteHalf(boneTransforms[i].position.x);
                        if ((flags & ReplayRiggedGenericSerializeFlags.PosY) != 0) state.WriteHalf(boneTransforms[i].position.y);
                        if ((flags & ReplayRiggedGenericSerializeFlags.PosZ) != 0) state.WriteHalf(boneTransforms[i].position.z);
                    }
                }
                else
                {
                    // If all axis are recorded then write data as Vector3 for improved performance (less overhead of write calls and resize)
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosXYZ) == ReplayRiggedGenericSerializeFlags.PosXYZ)
                    {
                        state.Write(boneTransforms[i].position);
                    }
                    else
                    {
                        if ((flags & ReplayRiggedGenericSerializeFlags.PosX) != 0) state.Write(boneTransforms[i].position.x);
                        if ((flags & ReplayRiggedGenericSerializeFlags.PosY) != 0) state.Write(boneTransforms[i].position.y);
                        if ((flags & ReplayRiggedGenericSerializeFlags.PosZ) != 0) state.Write(boneTransforms[i].position.z);
                    }
                }

                // Record rotation
                if ((flags & ReplayRiggedGenericSerializeFlags.LowPrecisionRot) != 0)
                {
                    // Serialize as quaternion if all axis recorded
                    if ((flags & ReplayRiggedGenericSerializeFlags.RotXYZ) == ReplayRiggedGenericSerializeFlags.RotXYZ)
                    {                        
                        state.WriteHalf(boneTransforms[i].rotation);
                    }
                    // Serialize as euler
                    else
                    {

                        if ((flags & ReplayRiggedGenericSerializeFlags.RotX) != 0) state.WriteHalf(boneTransforms[i].rotation.eulerAngles.x);
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotY) != 0) state.WriteHalf(boneTransforms[i].rotation.eulerAngles.y);
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotZ) != 0) state.WriteHalf(boneTransforms[i].rotation.eulerAngles.z);
                    }
                }
                else
                {
                    // Serialize as quaternion if all axis recorded
                    if ((flags & ReplayRiggedGenericSerializeFlags.RotXYZ) == ReplayRiggedGenericSerializeFlags.RotXYZ)
                    {
                        state.Write(boneTransforms[i].rotation);
                    }
                    // Serialize as euler
                    else
                    {
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotX) != 0) state.Write(boneTransforms[i].rotation.eulerAngles.x);
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotY) != 0) state.Write(boneTransforms[i].rotation.eulerAngles.y);
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotZ) != 0) state.Write(boneTransforms[i].rotation.eulerAngles.z);
                    }
                }

                // Record scale
                if ((flags & ReplayRiggedGenericSerializeFlags.LowPrecisionSca) != 0)
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaX) != 0) state.WriteHalf(boneTransforms[i].scale.x);
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaY) != 0) state.WriteHalf(boneTransforms[i].scale.y);
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaZ) != 0) state.WriteHalf(boneTransforms[i].scale.z);
                }
                else
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaX) != 0) state.Write(boneTransforms[i].scale.x);
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaY) != 0) state.Write(boneTransforms[i].scale.y);
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaZ) != 0) state.Write(boneTransforms[i].scale.z);
                }
            }
            #endregion
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            ReplayRiggedGenericSerializeFlags flags = serializeFlags = (ReplayRiggedGenericSerializeFlags)state.ReadUInt32();

            // Clear old bones
            boneTransforms.Clear();

            // Estimate required capacity
            

            #region RootBone
            // Read root position
            if ((flags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionPos) != 0)
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosX) != 0) rootBoneTransform.position.x = state.ReadHalf();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosY) != 0) rootBoneTransform.position.y = state.ReadHalf();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosZ) != 0) rootBoneTransform.position.z = state.ReadHalf();
            }
            else
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosX) != 0) rootBoneTransform.position.x = state.ReadSingle();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosY) != 0) rootBoneTransform.position.y = state.ReadSingle();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootPosZ) != 0) rootBoneTransform.position.z = state.ReadSingle();
            }

            // Read root rotation
            if ((flags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionRot) != 0)
            {
                // Deserialize as quaternion if all axis recorded
                if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0)
                {
                    rootBoneTransform.rotation.x = state.ReadHalf();
                    rootBoneTransform.rotation.y = state.ReadHalf();
                    rootBoneTransform.rotation.z = state.ReadHalf();
                    rootBoneTransform.rotation.w = state.ReadHalf();
                }
                // Deseirliaze as euler
                else
                {
                    Vector3 euler = new Vector3();

                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0) euler.x = state.ReadHalf();
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0) euler.y = state.ReadHalf();
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0) euler.z = state.ReadHalf();

                    rootBoneTransform.rotation = Quaternion.Euler(euler);
                }
            }
            else
            {
                // Deserialize as quaternion if all axis recorded
                if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0)
                {
                    rootBoneTransform.rotation.x = state.ReadSingle();
                    rootBoneTransform.rotation.y = state.ReadSingle();
                    rootBoneTransform.rotation.z = state.ReadSingle();
                    rootBoneTransform.rotation.w = state.ReadSingle();
                }
                else
                {
                    Vector3 euler = new Vector3();

                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0) euler.x = state.ReadSingle();
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0) euler.y = state.ReadSingle();
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0) euler.z = state.ReadSingle();

                    rootBoneTransform.rotation = Quaternion.Euler(euler);
                }
            }

            // Read root scale
            if ((flags & ReplayRiggedGenericSerializeFlags.RootLowPrecisionSca) != 0)
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaX) != 0) rootBoneTransform.scale.x = state.ReadHalf();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaY) != 0) rootBoneTransform.scale.y = state.ReadHalf();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaZ) != 0) rootBoneTransform.scale.z = state.ReadHalf();
            }
            else
            {
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaX) != 0) rootBoneTransform.scale.x = state.ReadSingle();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaY) != 0) rootBoneTransform.scale.y = state.ReadSingle();
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaZ) != 0) rootBoneTransform.scale.z = state.ReadSingle();
            }
            #endregion

            // Check for any bones
            if ((flags & ReplayRiggedGenericSerializeFlags.HasBones) == 0)
                return;

            #region AllBones
            // Read bone count
            int size = state.ReadUInt16();

            // Resize collection
            if (boneTransforms.Capacity < size)
                boneTransforms.Capacity = size;

            // Read all bones
            for(int i = 0; i < size; i++)
            {
                BoneTransform boneTransform = new BoneTransform();

                // Read position
                if ((flags & ReplayRiggedGenericSerializeFlags.LowPrecisionPos) != 0)
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosX) != 0) boneTransform.position.x = state.ReadHalf();
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosY) != 0) boneTransform.position.y = state.ReadHalf();
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosZ) != 0) boneTransform.position.z = state.ReadHalf();
                }
                else
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosX) != 0) boneTransform.position.x = state.ReadSingle();
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosY) != 0) boneTransform.position.y = state.ReadSingle();
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosZ) != 0) boneTransform.position.z = state.ReadSingle();
                }

                // Read rotation
                if ((flags & ReplayRiggedGenericSerializeFlags.LowPrecisionRot) != 0)
                {
                    // Deserialize as quaternion if all axis recorded
                    if ((flags & ReplayRiggedGenericSerializeFlags.RotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RotZ) != 0)
                    {
                        boneTransform.rotation.x = state.ReadHalf();
                        boneTransform.rotation.y = state.ReadHalf();
                        boneTransform.rotation.z = state.ReadHalf();
                        boneTransform.rotation.w = state.ReadHalf();
                    }
                    // Deseirliaze as euler
                    else
                    {
                        Vector3 euler = new Vector3();

                        if ((flags & ReplayRiggedGenericSerializeFlags.RotX) != 0) euler.x = state.ReadHalf();
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotY) != 0) euler.y = state.ReadHalf();
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotZ) != 0) euler.z = state.ReadHalf();

                        boneTransform.rotation = Quaternion.Euler(euler);
                    }
                }
                else
                {
                    // Deserialize as quaternion if all axis recorded
                    if ((flags & ReplayRiggedGenericSerializeFlags.RotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RotZ) != 0)
                    {
                        boneTransform.rotation.x = state.ReadSingle();
                        boneTransform.rotation.y = state.ReadSingle();
                        boneTransform.rotation.z = state.ReadSingle();
                        boneTransform.rotation.w = state.ReadSingle();
                    }
                    else
                    {
                        Vector3 euler = new Vector3();

                        if ((flags & ReplayRiggedGenericSerializeFlags.RotX) != 0) euler.x = state.ReadSingle();
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotY) != 0) euler.y = state.ReadSingle();
                        if ((flags & ReplayRiggedGenericSerializeFlags.RotZ) != 0) euler.z = state.ReadSingle();

                        boneTransform.rotation = Quaternion.Euler(euler);
                    }
                }

                // Read scale
                if ((flags & ReplayRiggedGenericSerializeFlags.LowPrecisionSca) != 0)
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaX) != 0) boneTransform.scale.x = state.ReadHalf();
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaY) != 0) boneTransform.scale.y = state.ReadHalf();
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaZ) != 0) boneTransform.scale.z = state.ReadHalf();
                }
                else
                {
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaX) != 0) boneTransform.scale.x = state.ReadSingle();
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaY) != 0) boneTransform.scale.y = state.ReadSingle();
                    if ((flags & ReplayRiggedGenericSerializeFlags.ScaZ) != 0) boneTransform.scale.z = state.ReadSingle();
                }

                // Push to collecton
                boneTransforms.Add(boneTransform);
            }
            #endregion
        }


        public void GetBoneTransform(int index, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            // Fetch transform
            BoneTransform transform = boneTransforms[index];

            // Fill values
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.scale;
        }

        public Vector3 GetBonePosition(int index)
        {
            return boneTransforms[index].position;
        }

        public Quaternion GetBoneRotation(int index)
        {
            return boneTransforms[index].rotation;
        }

        public Vector3 GetBoneScale(int index)
        {
            return boneTransforms[index].scale;
        }


        internal void SyncTransforms(Transform root, Transform[] sync, ReplayRiggedGenericSerializeFlags flags)
        {
            // Update root
            SyncTransform(root, rootBoneTransform, flags, true);

            // Update all bones
            if(sync.Length == boneTransforms.Count)
            {
                for(int i = 0; i < sync.Length; i++)
                {
                    // Update child bone
                    SyncTransform(sync[i], boneTransforms[i], flags, false);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncTransform(Transform sync, in BoneTransform transform, ReplayRiggedGenericSerializeFlags flags, bool root)
        {
            // Sync full transform
            SyncTransformPosition(sync, transform, flags, root);
            SyncTransformRotation(sync, transform, flags, root);
            SyncTransformScale(sync, transform, flags, root);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncRootBoneTransformPosition(Transform sync, ReplayRiggedGenericSerializeFlags flags)
        {
            SyncTransformPosition(sync, rootBoneTransform, flags, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncBoneTransformPosition(Transform sync, int index, ReplayRiggedGenericSerializeFlags flags)
        {
            // Call through
            SyncTransformPosition(sync, boneTransforms[index], flags, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncTransformPosition(Transform sync, in BoneTransform transform, ReplayRiggedGenericSerializeFlags flags, bool root)
        {
            // Position
            if (root == false)
            {
                // Update child bone
                if ((flags & (ReplayRiggedGenericSerializeFlags.PosX | ReplayRiggedGenericSerializeFlags.PosY | ReplayRiggedGenericSerializeFlags.PosZ)) != 0)
                {
                    // Get current position
                    Vector3 position = sync.localPosition;

                    // Update axis
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosX) != 0) position.x = transform.position.x;
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosY) != 0) position.y = transform.position.y;
                    if ((flags & ReplayRiggedGenericSerializeFlags.PosZ) != 0) position.z = transform.position.z;

                    // Update transform
                    sync.localPosition = position;
                }
            }
            else
            {
                // Update root bone
                if ((flags & (ReplayRiggedGenericSerializeFlags.RootPosX | ReplayRiggedGenericSerializeFlags.RootPosY | ReplayRiggedGenericSerializeFlags.RootPosZ)) != 0)
                {
                    // Get current position
                    Vector3 position = sync.localPosition;

                    // Update axis
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootPosX) != 0) position.x = transform.position.x;
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootPosY) != 0) position.y = transform.position.y;
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootPosZ) != 0) position.z = transform.position.z;

                    // Update transform
                    sync.localPosition = position;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncRootBoneTransformRotation(Transform sync, ReplayRiggedGenericSerializeFlags flags)
        {
            SyncTransformRotation(sync, rootBoneTransform, flags, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncBoneTransformRotation(Transform sync, int index, ReplayRiggedGenericSerializeFlags flags)
        {
            SyncTransformRotation(sync, boneTransforms[index], flags, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncTransformRotation(Transform sync, in BoneTransform transform, ReplayRiggedGenericSerializeFlags flags, bool root)
        {
            // Rotation
            if (root == false)
            {
                // Update child bone
                if ((flags & (ReplayRiggedGenericSerializeFlags.RotX | ReplayRiggedGenericSerializeFlags.RotY | ReplayRiggedGenericSerializeFlags.RotZ)) != 0)
                {
                    // Check for full rotation - Use quaternion
                    if ((flags & ReplayRiggedGenericSerializeFlags.RotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RotZ) != 0)
                    {
                        // Update transform
                        sync.localRotation = transform.rotation;
                    }
                    // Use euler
                    else
                    {
                        // Get current euler rotation
                        Vector3 euler = sync.localEulerAngles;

                        // Update axis
                        if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RotX) != 0) euler.x = transform.rotation.eulerAngles.x;
                        if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RotY) != 0) euler.y = transform.rotation.eulerAngles.y;
                        if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RotZ) != 0) euler.z = transform.rotation.eulerAngles.z;

                        // Update transform
                        sync.localEulerAngles = euler;
                    }
                }
            }
            else
            {
                // Update root bone
                if ((flags & (ReplayRiggedGenericSerializeFlags.RootRotX | ReplayRiggedGenericSerializeFlags.RootRotY | ReplayRiggedGenericSerializeFlags.RootRotZ)) != 0)
                {
                    // Check for full rotation - Use quaternion
                    if ((flags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0 && (flags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0)
                    {
                        // Update transform
                        sync.localRotation = transform.rotation;
                    }
                    // Use euler
                    else
                    {
                        // Get current euler rotation
                        Vector3 euler = sync.localEulerAngles;

                        // Update axis
                        if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootRotX) != 0) euler.x = transform.rotation.eulerAngles.x;
                        if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootRotY) != 0) euler.y = transform.rotation.eulerAngles.y;
                        if ((serializeFlags & ReplayRiggedGenericSerializeFlags.RootRotZ) != 0) euler.z = transform.rotation.eulerAngles.z;

                        // Update transform
                        sync.localEulerAngles = euler;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncRootBoneTransformScale(Transform sync, ReplayRiggedGenericSerializeFlags flags)
        {
            SyncTransformScale(sync, rootBoneTransform, flags, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncBoneTransformScale(Transform sync, int index, ReplayRiggedGenericSerializeFlags flags)
        {
            SyncTransformScale(sync, boneTransforms[index], flags, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncTransformScale(Transform sync, in BoneTransform transform, ReplayRiggedGenericSerializeFlags flags, bool root)
        {
            if (root == false)
            {
                // Get current scale
                Vector3 scale = sync.localScale;

                // Update axis
                if ((flags & ReplayRiggedGenericSerializeFlags.ScaX) != 0) scale.x = transform.scale.x;
                if ((flags & ReplayRiggedGenericSerializeFlags.ScaY) != 0) scale.y = transform.scale.y;
                if ((flags & ReplayRiggedGenericSerializeFlags.ScaZ) != 0) scale.z = transform.scale.z;

                // Update transform
                sync.localScale = scale;
            }
            else
            {
                // Get current scale
                Vector3 scale = sync.localScale;

                // Update axis
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaX) != 0) scale.x = transform.scale.x;
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaY) != 0) scale.y = transform.scale.y;
                if ((flags & ReplayRiggedGenericSerializeFlags.RootScaZ) != 0) scale.z = transform.scale.z;

                // Update transform
                sync.localScale = scale;
            }
        }

        internal void UpdateFromTransforms(Transform root, Transform[] from, ReplayRiggedGenericSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;


            // Update root transform
            rootBoneTransform.position = root.localPosition;
            rootBoneTransform.rotation = root.localRotation;
            rootBoneTransform.scale = root.localScale;


            // Resize and clear collection
            boneTransforms.Clear();

            if (boneTransforms.Capacity < from.Length)
                boneTransforms.Capacity = from.Length;

            // Get length of array
            int length = from.Length;

            // Update transforms
            for(int i = 0; i < length; i++)
            {
                BoneTransform boneTransform = new BoneTransform();

                // Check for null entry
                if (from[i] == null)
                {
                    // Slot still needs to be filled
                    boneTransforms.Add(boneTransform);
                    continue;
                }

                // Position
                if ((flags & (ReplayRiggedGenericSerializeFlags.PosX | ReplayRiggedGenericSerializeFlags.PosY | ReplayRiggedGenericSerializeFlags.PosZ)) != 0)
                {
                    boneTransform.position = from[i].localPosition;
                }

                // Rotation
                if ((flags & (ReplayRiggedGenericSerializeFlags.RotX | ReplayRiggedGenericSerializeFlags.RotY | ReplayRiggedGenericSerializeFlags.RotZ)) != 0)
                {
                    boneTransform.rotation = from[i].localRotation;
                }

                // Scale
                if ((flags & (ReplayRiggedGenericSerializeFlags.ScaX | ReplayRiggedGenericSerializeFlags.ScaY | ReplayRiggedGenericSerializeFlags.ScaZ)) != 0)
                {
                    boneTransform.scale = from[i].localScale;
                }

                // Add to collection
                boneTransforms.Add(boneTransform);
            }
        }

        internal static ReplayRiggedGenericSerializeFlags GetSerializeFlags(int boneCount, RecordAxisFlags rootPosition, RecordAxisFlags position, RecordAxisFlags rootRotation, RecordAxisFlags rotation, RecordAxisFlags rootScale = RecordAxisFlags.XYZ, RecordAxisFlags scale = RecordAxisFlags.XYZ, RecordPrecision rootPositionPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision positionPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision rootRotationPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision rotationPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision rootScalePrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision scalePrecision = RecordPrecision.FullPrecision32Bit)
        {
            ReplayRiggedGenericSerializeFlags flags = ReplayRiggedGenericSerializeFlags.None;

            // Root position
            if ((rootPosition & RecordAxisFlags.X) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootPosX;
            if ((rootPosition & RecordAxisFlags.Y) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootPosY;
            if ((rootPosition & RecordAxisFlags.Z) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootPosZ;
            if ((rootPositionPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootLowPrecisionPos;

            // Root rotation
            if ((rootRotation & RecordAxisFlags.X) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootRotX;
            if ((rootRotation & RecordAxisFlags.Y) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootRotY;
            if ((rootRotation & RecordAxisFlags.Z) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootRotZ;
            if ((rootRotationPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootLowPrecisionRot;

            // Root scale
            if ((rootScale & RecordAxisFlags.X) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootScaX;
            if ((rootScale & RecordAxisFlags.Y) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootScaY;
            if ((rootScale & RecordAxisFlags.Z) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootScaZ;
            if ((rootScalePrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedGenericSerializeFlags.RootLowPrecisionSca;

            // Has Bones
            if (boneCount > 0) flags |= ReplayRiggedGenericSerializeFlags.HasBones;

            // Position
            if ((position & RecordAxisFlags.X) != 0) flags |= ReplayRiggedGenericSerializeFlags.PosX;
            if ((position & RecordAxisFlags.Y) != 0) flags |= ReplayRiggedGenericSerializeFlags.PosY;
            if ((position & RecordAxisFlags.Z) != 0) flags |= ReplayRiggedGenericSerializeFlags.PosZ;
            if ((positionPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedGenericSerializeFlags.LowPrecisionPos;

            // Rotation
            if ((rotation & RecordAxisFlags.X) != 0) flags |= ReplayRiggedGenericSerializeFlags.RotX;
            if ((rotation & RecordAxisFlags.Y) != 0) flags |= ReplayRiggedGenericSerializeFlags.RotY;
            if ((rotation & RecordAxisFlags.Z) != 0) flags |= ReplayRiggedGenericSerializeFlags.RotZ;
            if ((rotationPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedGenericSerializeFlags.LowPrecisionRot;

            // Scale
            if ((scale & RecordAxisFlags.X) != 0) flags |= ReplayRiggedGenericSerializeFlags.ScaX;
            if ((scale & RecordAxisFlags.Y) != 0) flags |= ReplayRiggedGenericSerializeFlags.ScaY;
            if ((scale & RecordAxisFlags.Z) != 0) flags |= ReplayRiggedGenericSerializeFlags.ScaZ;
            if ((scalePrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayRiggedGenericSerializeFlags.LowPrecisionSca;

            return flags;
        }
    }
}
