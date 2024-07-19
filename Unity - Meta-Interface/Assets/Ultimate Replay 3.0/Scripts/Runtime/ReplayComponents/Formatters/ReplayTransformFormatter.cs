using System;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayTransformFormatter : ReplayFormatter
    {
        // Type
        [Flags]
        internal enum ReplayTransformSerializeFlags : ushort
        {
            None = 0,
            PosX = 1 << 0,
            PosY = 1 << 1,
            PosZ = 1 << 2,
            LocalPos = 1 << 3,
            LowPrecisionPos = 1 << 4,

            PosXYZ = PosX | PosY | PosZ,

            RotX = 1 << 5,
            RotY = 1 << 6,
            RotZ = 1 << 7,
            LocalRot = 1 << 8,
            LowPrecisionRot = 1 << 9,

            RotXYZ = RotX | RotY | RotZ,

            ScaX = 1 << 10,
            ScaY = 1 << 11,
            ScaZ = 1 << 12,
            LocalSca = 1 << 13,             // used for padding only
            LowPrecisionSca = 1 << 14,

            ScaXYZ = ScaX | ScaY | ScaZ,
        }

        // Private
        [ReplayTokenSerialize("Flags")]
        private ReplayTransformSerializeFlags serializeFlags = 0;
        [ReplayTokenSerialize("Pos")]
        private Vector3 position = Vector3.zero;
        [ReplayTokenSerialize("Rot")]
        private Quaternion rotation = Quaternion.identity;
        [ReplayTokenSerialize("Sca")]
        private Vector3 scale = Vector3.one;

        // Properties
        internal ReplayTransformSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        public Vector3 Position => position;

        public RecordAxisFlags PositionAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayTransformSerializeFlags.PosY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayTransformSerializeFlags.PosX | ReplayTransformSerializeFlags.PosY | ReplayTransformSerializeFlags.PosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayTransformSerializeFlags.PosX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayTransformSerializeFlags.PosY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayTransformSerializeFlags.PosZ;
            }
        }

        public RecordSpace PositionSpace
        {
            get
            {
                RecordSpace space = RecordSpace.World;

                // Check for local flag set
                if ((serializeFlags & ReplayTransformSerializeFlags.LocalPos) != 0) space = RecordSpace.Local;

                return space;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayTransformSerializeFlags.LocalPos;

                // Set
                if (value == RecordSpace.Local) serializeFlags |= ReplayTransformSerializeFlags.LocalPos;
            }
        }

        public RecordPrecision PositionPrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low precision set
                if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionPos) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayTransformSerializeFlags.LowPrecisionPos;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayTransformSerializeFlags.LowPrecisionPos;
            }
        }

        public Quaternion Rotation => rotation;

        public RecordAxisFlags RotationAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayTransformSerializeFlags.RotY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayTransformSerializeFlags.PosX | ReplayTransformSerializeFlags.PosY | ReplayTransformSerializeFlags.PosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayTransformSerializeFlags.RotX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayTransformSerializeFlags.RotY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayTransformSerializeFlags.RotZ;
            }
        }

        public RecordSpace RotationSpace
        {
            get
            {
                RecordSpace space = RecordSpace.World;

                // Check for local flag set
                if ((serializeFlags & ReplayTransformSerializeFlags.LocalRot) != 0) space = RecordSpace.Local;

                return space;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayTransformSerializeFlags.LocalRot;

                // Set
                if (value == RecordSpace.Local) serializeFlags |= ReplayTransformSerializeFlags.LocalRot;
            }
        }

        public RecordPrecision RotationPrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low precision set
                if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayTransformSerializeFlags.LowPrecisionRot;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayTransformSerializeFlags.LowPrecisionRot;
            }
        }

        public Vector3 Scale => scale;
        public RecordAxisFlags ScaleAxis
        {
            get
            {
                RecordAxisFlags flags = RecordAxisFlags.None;

                if ((serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0) flags |= RecordAxisFlags.X;
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0) flags |= RecordAxisFlags.Y;
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0) flags |= RecordAxisFlags.Z;

                return flags;
            }
            set
            {
                // Clear bits
                serializeFlags &= ~(ReplayTransformSerializeFlags.PosX | ReplayTransformSerializeFlags.PosY | ReplayTransformSerializeFlags.PosZ);

                // Set
                if ((value & RecordAxisFlags.X) != 0) serializeFlags |= ReplayTransformSerializeFlags.ScaX;
                if ((value & RecordAxisFlags.Y) != 0) serializeFlags |= ReplayTransformSerializeFlags.ScaY;
                if ((value & RecordAxisFlags.Z) != 0) serializeFlags |= ReplayTransformSerializeFlags.ScaZ;
            }
        }

        public RecordPrecision ScalePrecision
        {
            get
            {
                RecordPrecision precision = RecordPrecision.FullPrecision32Bit;

                // Check for low precision set
                if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionSca) != 0) precision = RecordPrecision.HalfPrecision16Bit;

                return precision;
            }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayTransformSerializeFlags.LowPrecisionSca;

                // Set
                if (value == RecordPrecision.HalfPrecision16Bit) serializeFlags |= ReplayTransformSerializeFlags.LowPrecisionSca;
            }
        }

        // Methods
        public override void OnReplaySerialize(ReplayState state)
        {
            // Get flags
            ReplayTransformSerializeFlags flags = serializeFlags;

            // Write flags
            state.Write((ushort)flags);

            // Record position
            if((flags & ReplayTransformSerializeFlags.LowPrecisionPos) != 0)
            {
                // If all axis are recorded then write data as Vector3 for improved performance (less overhead of write calls)
                if ((flags & ReplayTransformSerializeFlags.PosXYZ) == ReplayTransformSerializeFlags.PosXYZ)
                {
                    // Write position as batch
                    state.WriteHalf(position);
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.PosX) != 0) state.WriteHalf(position.x);
                    if ((flags & ReplayTransformSerializeFlags.PosY) != 0) state.WriteHalf(position.y);
                    if ((flags & ReplayTransformSerializeFlags.PosZ) != 0) state.WriteHalf(position.z);
                }
            }
            else
            {
                // If all axis are recorded then write data as Vector3 for improved performance (less overhead of write calls)
                if ((flags & ReplayTransformSerializeFlags.PosXYZ) == ReplayTransformSerializeFlags.PosXYZ)
                {
                    // Write position as a batch
                    state.Write(position);
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.PosX) != 0) state.Write(position.x);
                    if ((flags & ReplayTransformSerializeFlags.PosY) != 0) state.Write(position.y);
                    if ((flags & ReplayTransformSerializeFlags.PosZ) != 0) state.Write(position.z);
                }
            }

            // Record rotation
            if ((flags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0)
            {
                // Serialize as quaternion if all axis recorded
                if ((flags & ReplayTransformSerializeFlags.RotXYZ) == ReplayTransformSerializeFlags.RotXYZ)
                {                    
                    // Write rotation as a batch
                    state.WriteHalf(rotation);
                }
                // Serialize as euler
                else
                {                    
                    if ((flags & ReplayTransformSerializeFlags.RotX) != 0) state.WriteHalf(rotation.eulerAngles.x);
                    if ((flags & ReplayTransformSerializeFlags.RotY) != 0) state.WriteHalf(rotation.eulerAngles.y);
                    if ((flags & ReplayTransformSerializeFlags.RotZ) != 0) state.WriteHalf(rotation.eulerAngles.z);                    
                }
            }
            else
            {
                // Serialize as quaternion if all axis recorded
                if ((flags & ReplayTransformSerializeFlags.RotXYZ) == ReplayTransformSerializeFlags.RotXYZ)
                {                    
                    // Write rotation as a batch
                    state.Write(rotation);
                }
                // Serialize as euler
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.RotX) != 0) state.Write(rotation.eulerAngles.x);
                    if ((flags & ReplayTransformSerializeFlags.RotY) != 0) state.Write(rotation.eulerAngles.y);
                    if ((flags & ReplayTransformSerializeFlags.RotZ) != 0) state.Write(rotation.eulerAngles.z);
                }
            }

            // Record scale
            if ((flags & ReplayTransformSerializeFlags.LowPrecisionSca) != 0)
            {
                // If all axis are recorded then write data as Vector3 for improved performance (less overhead of write calls)
                if ((flags & ReplayTransformSerializeFlags.ScaXYZ) == ReplayTransformSerializeFlags.ScaXYZ)
                {
                    // Write rotation as a batch
                    state.WriteHalf(scale);
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.ScaX) != 0) state.WriteHalf(scale.x);
                    if ((flags & ReplayTransformSerializeFlags.ScaY) != 0) state.WriteHalf(scale.y);
                    if ((flags & ReplayTransformSerializeFlags.ScaZ) != 0) state.WriteHalf(scale.z);
                }
            }
            else
            {
                // If all axis are recorded then write data as Vector3 for improved performance (less overhead of write calls)
                if ((flags & ReplayTransformSerializeFlags.ScaXYZ) == ReplayTransformSerializeFlags.ScaXYZ)
                {
                    // Write rotation as a batch
                    state.Write(scale);
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.ScaX) != 0) state.Write(scale.x);
                    if ((flags & ReplayTransformSerializeFlags.ScaY) != 0) state.Write(scale.y);
                    if ((flags & ReplayTransformSerializeFlags.ScaZ) != 0) state.Write(scale.z);
                }
            }
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            ReplayTransformSerializeFlags flags = serializeFlags = (ReplayTransformSerializeFlags)state.ReadUInt16();

            // Read position
            if ((flags & ReplayTransformSerializeFlags.LowPrecisionPos) != 0)
            {
                // If all axis are recorded then read data as Vector3 for improved performance (less overhead of read calls)
                if ((flags & ReplayTransformSerializeFlags.PosXYZ) == ReplayTransformSerializeFlags.PosXYZ)
                {
                    // Read position as batch
                    position = state.ReadVector3Half();
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.PosX) != 0) position.x = state.ReadHalf();
                    if ((flags & ReplayTransformSerializeFlags.PosY) != 0) position.y = state.ReadHalf();
                    if ((flags & ReplayTransformSerializeFlags.PosZ) != 0) position.z = state.ReadHalf();
                }
            }
            else
            {
                // If all axis are recorded then read data as Vector3 for improved performance (less overhead of read calls)
                if ((flags & ReplayTransformSerializeFlags.PosXYZ) == ReplayTransformSerializeFlags.PosXYZ)
                {
                    // Read position as batch
                    position = state.ReadVector3();
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.PosX) != 0) position.x = state.ReadSingle();
                    if ((flags & ReplayTransformSerializeFlags.PosY) != 0) position.y = state.ReadSingle();
                    if ((flags & ReplayTransformSerializeFlags.PosZ) != 0) position.z = state.ReadSingle();
                }
            }

            // Read rotation
            if ((flags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0)
            {
                // Deserialize as quaternion if all axis recorded
                if ((flags & ReplayTransformSerializeFlags.RotXYZ) == ReplayTransformSerializeFlags.RotXYZ)
                {
                    rotation = state.ReadQuaternionHalf();
                }
                // Deserialize as euler
                else
                {
                    Vector3 euler = default;

                    if ((flags & ReplayTransformSerializeFlags.RotX) != 0) euler.x = state.ReadHalf();
                    if ((flags & ReplayTransformSerializeFlags.RotY) != 0) euler.y = state.ReadHalf();
                    if ((flags & ReplayTransformSerializeFlags.RotZ) != 0) euler.z = state.ReadHalf();

                    rotation = Quaternion.Euler(euler);
                }
            }
            else
            {
                // Deserialize as quaternion if all axis recorded
                if ((flags & ReplayTransformSerializeFlags.RotXYZ) == ReplayTransformSerializeFlags.RotXYZ)
                {
                    rotation = state.ReadQuaternion();
                }
                else
                {
                    Vector3 euler = default;

                    if ((flags & ReplayTransformSerializeFlags.RotX) != 0) euler.x = state.ReadSingle();
                    if ((flags & ReplayTransformSerializeFlags.RotY) != 0) euler.y = state.ReadSingle();
                    if ((flags & ReplayTransformSerializeFlags.RotZ) != 0) euler.z = state.ReadSingle();

                    rotation = Quaternion.Euler(euler);
                }
            }

            // Read scale
            if ((flags & ReplayTransformSerializeFlags.LowPrecisionSca) != 0)
            {
                // If all axis are recorded then read data as Vector3 for improved performance (less overhead of read calls)
                if ((flags & ReplayTransformSerializeFlags.ScaXYZ) == ReplayTransformSerializeFlags.ScaXYZ)
                {
                    // Read position as batch
                    scale = state.ReadVector3Half();
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.ScaX) != 0) scale.x = state.ReadHalf();
                    if ((flags & ReplayTransformSerializeFlags.ScaY) != 0) scale.y = state.ReadHalf();
                    if ((flags & ReplayTransformSerializeFlags.ScaZ) != 0) scale.z = state.ReadHalf();
                }
            }
            else
            {
                // If all axis are recorded then read data as Vector3 for improved performance (less overhead of read calls)
                if ((flags & ReplayTransformSerializeFlags.ScaXYZ) == ReplayTransformSerializeFlags.ScaXYZ)
                {
                    // Read position as batch
                    scale = state.ReadVector3();
                }
                else
                {
                    if ((flags & ReplayTransformSerializeFlags.ScaX) != 0) scale.x = state.ReadSingle();
                    if ((flags & ReplayTransformSerializeFlags.ScaY) != 0) scale.y = state.ReadSingle();
                    if ((flags & ReplayTransformSerializeFlags.ScaZ) != 0) scale.z = state.ReadSingle();
                }
            }
        }

        public void SyncTransform(Transform sync)
        {
            SyncTransform(sync, serializeFlags);
        }

        internal void SyncTransform(Transform sync, ReplayTransformSerializeFlags flags)
        {
            // Sync full transform
            SyncTransformPosition(sync, flags);
            SyncTransformRotation(sync, flags);
            SyncTransformScale(sync, flags);
        }

        public void SyncTransformPosition(Transform sync)
        {
            SyncTransformPosition(sync, serializeFlags);
        }

        internal void SyncTransformPosition(Transform sync, ReplayTransformSerializeFlags flags)
        {
            // Position
            if ((flags & (ReplayTransformSerializeFlags.PosX | ReplayTransformSerializeFlags.PosY | ReplayTransformSerializeFlags.PosZ)) != 0)
            {
                // Get current position
                Vector3 position = ((flags & ReplayTransformSerializeFlags.LocalPos) != 0) ? sync.localPosition : sync.position;

                // Update axis
                if ((flags & ReplayTransformSerializeFlags.PosX) != 0) position.x = this.position.x;
                if ((flags & ReplayTransformSerializeFlags.PosY) != 0) position.y = this.position.y;
                if ((flags & ReplayTransformSerializeFlags.PosZ) != 0) position.z = this.position.z;

                // Update transform
                if ((flags & ReplayTransformSerializeFlags.LocalPos) != 0)
                {
                    sync.localPosition = position;
                }
                else
                {
                    sync.position = position;
                }
            }
        }

        public void SyncTransformRotation(Transform sync)
        {
            SyncTransformRotation(sync, serializeFlags);
        }

        internal void SyncTransformRotation(Transform sync, ReplayTransformSerializeFlags flags)
        {
            // Rotation
            if ((flags & (ReplayTransformSerializeFlags.RotX | ReplayTransformSerializeFlags.RotY | ReplayTransformSerializeFlags.RotZ)) != 0)
            {
                // Check for full rotation - Use quaternion
                if((flags & ReplayTransformSerializeFlags.RotX) != 0 && (flags & ReplayTransformSerializeFlags.RotY) != 0 && (flags & ReplayTransformSerializeFlags.RotZ) != 0)
                {
                    // Check for local
                    if((flags & ReplayTransformSerializeFlags.LocalRot) != 0)
                    {
                        sync.localRotation = this.rotation;
                    }
                    else
                    {
                        sync.rotation = this.rotation;
                    }
                }
                // Use euler
                else
                {
                    // Get current euler rotation
                    Vector3 euler = ((flags & ReplayTransformSerializeFlags.LocalRot) != 0) ? sync.localEulerAngles : sync.eulerAngles;

                    // Update axis
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0) euler.x = this.rotation.eulerAngles.x;
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotY) != 0) euler.y = this.rotation.eulerAngles.y;
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0) euler.z = this.rotation.eulerAngles.z;

                    // Update transform
                    if((flags & ReplayTransformSerializeFlags.LocalRot) != 0)
                    {
                        sync.localEulerAngles = euler;
                    }
                    else
                    {
                        sync.eulerAngles = euler;
                    }
                }
            }
        }

        public void SyncTransformScale(Transform sync)
        {
            SyncTransformScale(sync, serializeFlags);
        }

        internal void SyncTransformScale(Transform sync, ReplayTransformSerializeFlags flags)
        {
            // Get current scale
            Vector3 scale = sync.localScale;

            // Update axis
            if ((flags & ReplayTransformSerializeFlags.ScaX) != 0) scale.x = this.scale.x;
            if ((flags & ReplayTransformSerializeFlags.ScaY) != 0) scale.y = this.scale.y;
            if ((flags & ReplayTransformSerializeFlags.ScaZ) != 0) scale.z = this.scale.z;

            // Update transform
            sync.localScale = scale;
        }

        public void UpdateFromTransform(Transform from, bool includeScale = false)
        {
            // Check for record scale
            RecordAxisFlags replayScale = (includeScale == true) ? RecordAxisFlags.XYZ : RecordAxisFlags.None;

            // Calculate serialize flags
            ReplayTransformSerializeFlags flags = GetSerializeFlags(RecordAxisFlags.XYZ, RecordAxisFlags.XYZ, replayScale);

            // Update from transform
            UpdateFromTransform(from, flags);
        }

        public void UpdateFromTransform(Transform from, RecordAxisFlags position, RecordAxisFlags rotation, RecordAxisFlags scale = RecordAxisFlags.None, RecordSpace positionSpace = RecordSpace.World, RecordSpace rotationSpace = RecordSpace.World, RecordPrecision positionPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision rotationPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision scalePrecision = RecordPrecision.FullPrecision32Bit)
        {
            // Calculate serialize flags
            ReplayTransformSerializeFlags flags = GetSerializeFlags(position, rotation, scale, positionSpace, rotationSpace, positionPrecision, rotationPrecision, scalePrecision);

            // Update from transform
            UpdateFromTransform(from, flags);
        }

        internal void UpdateFromTransform(Transform from, ReplayTransformSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;

            // Position
            if ((flags & (ReplayTransformSerializeFlags.PosX | ReplayTransformSerializeFlags.PosY | ReplayTransformSerializeFlags.PosZ)) != 0)
            {
                // Check for local
                if ((flags & ReplayTransformSerializeFlags.LocalPos) != 0)
                {
                    this.position = from.localPosition;
                }
                else
                {
                    this.position = from.position;
                }
            }

            // Rotation
            if((flags & (ReplayTransformSerializeFlags.RotX | ReplayTransformSerializeFlags.RotY | ReplayTransformSerializeFlags.RotZ)) != 0)
            {
                // Check for local
                if((flags & ReplayTransformSerializeFlags.LocalRot) != 0)
                {
                    this.rotation = from.localRotation;
                }
                else
                {
                    this.rotation = from.rotation;
                }
            }

            // Scale
            if((flags & (ReplayTransformSerializeFlags.ScaX | ReplayTransformSerializeFlags.ScaY | ReplayTransformSerializeFlags.ScaZ)) != 0)
            {
                this.scale = from.localScale;
            }
        }

        internal static ReplayTransformSerializeFlags GetSerializeFlags(RecordAxisFlags position, RecordAxisFlags rotation, RecordAxisFlags scale = RecordAxisFlags.XYZ, RecordSpace positionSpace = RecordSpace.World, RecordSpace rotationSpace = RecordSpace.World, RecordPrecision positionPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision rotationPrecision = RecordPrecision.FullPrecision32Bit, RecordPrecision scalePrecision = RecordPrecision.FullPrecision32Bit)
        {
            ReplayTransformSerializeFlags flags = ReplayTransformSerializeFlags.None;

            // Position
            if ((position & RecordAxisFlags.X) != 0) flags |= ReplayTransformSerializeFlags.PosX;
            if ((position & RecordAxisFlags.Y) != 0) flags |= ReplayTransformSerializeFlags.PosY;
            if ((position & RecordAxisFlags.Z) != 0) flags |= ReplayTransformSerializeFlags.PosZ;
            if ((positionSpace & RecordSpace.Local) != 0) flags |= ReplayTransformSerializeFlags.LocalPos;
            if ((positionPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayTransformSerializeFlags.LowPrecisionPos;

            // Rotation
            if ((rotation & RecordAxisFlags.X) != 0) flags |= ReplayTransformSerializeFlags.RotX;
            if ((rotation & RecordAxisFlags.Y) != 0) flags |= ReplayTransformSerializeFlags.RotY;
            if ((rotation & RecordAxisFlags.Z) != 0) flags |= ReplayTransformSerializeFlags.RotZ;
            if ((rotationSpace & RecordSpace.Local) != 0) flags |= ReplayTransformSerializeFlags.LocalRot;
            if ((rotationPrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayTransformSerializeFlags.LowPrecisionRot;

            // Scale
            if ((scale & RecordAxisFlags.X) != 0) flags |= ReplayTransformSerializeFlags.ScaX;
            if ((scale & RecordAxisFlags.Y) != 0) flags |= ReplayTransformSerializeFlags.ScaY;
            if ((scale & RecordAxisFlags.Z) != 0) flags |= ReplayTransformSerializeFlags.ScaZ;
            if ((scalePrecision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayTransformSerializeFlags.LowPrecisionSca;

            return flags;
        }
    }
}
