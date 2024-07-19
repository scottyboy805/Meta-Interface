﻿//using System;

//namespace UltimateReplay.Serializers
//{
//    public sealed class ReplayAudioSerializer : IReplaySerialize
//    {
//        // Types
//        [Flags]
//        public enum ReplayAudioSerializeFlags : byte
//        {
//            None = 0,
//            Pitch = 1 << 1,
//            Volume = 1 << 2,
//            StereoPan = 1 << 3,
//            SpatialBlend = 1 << 4,
//            ReverbZoneMix = 1 << 5,
//            LowPrecision = 1 << 6,
//        }

//        // Private
//        [ReplayTokenSerialize("Serialize Flags")]
//        private ReplayAudioSerializeFlags serializeFlags = 0;
//        [ReplayTokenSerialize("Is Playing")]
//        private bool isPlaying = false;
//        [ReplayTokenSerialize("Time Sample")]
//        private int timeSample = 0;
//        [ReplayTokenSerialize("Pitch")]
//        private float pitch = 0;
//        [ReplayTokenSerialize("Volume")]
//        private float volume = 0;
//        [ReplayTokenSerialize("StereoPan")]
//        private float stereoPan = 0;
//        [ReplayTokenSerialize("SpatialBlend")]
//        private float spatialBlend = 0;
//        [ReplayTokenSerialize("ReverbZoneMix")]
//        private float reverbZoneMix = 0;

//        // Properties
//        public ReplayAudioSerializeFlags SerializeFlags
//        {
//            get { return serializeFlags; }
//            set { serializeFlags = value; }
//        }

//        public bool IsPlaying
//        {
//            get { return isPlaying; }
//            set { isPlaying = value; }
//        }

//        public int TimeSample
//        {
//            get { return timeSample; }
//            set { timeSample = value; }
//        }

//        public float Pitch
//        {
//            get { return pitch; }
//            set { pitch = value; }
//        }

//        public float Volume
//        {
//            get { return volume; }
//            set { volume = value; }
//        }

//        public float StereoPan
//        {
//            get { return stereoPan; }
//            set { stereoPan = value; }
//        }

//        public float SpatialBlend
//        {
//            get { return spatialBlend; }
//            set { spatialBlend = value; }
//        }

//        public float ReverbZoneMix
//        {
//            get { return reverbZoneMix; }
//            set { reverbZoneMix = value; }
//        }

//        // Methods
//        public void OnReplaySerialize(ReplayState state)
//        {
//            // Write flags
//            state.Write((byte)serializeFlags);

//            // Write mandatory values in full precision
//            state.Write(isPlaying);
//            state.Write(timeSample);

//            // Check for low precision
//            if((serializeFlags & ReplayAudioSerializeFlags.LowPrecision) != 0)
//            {
//                if ((serializeFlags & ReplayAudioSerializeFlags.Pitch) != 0) state.WriteHalf(pitch);
//                if ((serializeFlags & ReplayAudioSerializeFlags.Volume) != 0) state.WriteHalf(volume);
//                if ((serializeFlags & ReplayAudioSerializeFlags.StereoPan) != 0) state.WriteHalf(stereoPan);
//                if ((serializeFlags & ReplayAudioSerializeFlags.SpatialBlend) != 0) state.WriteHalf(spatialBlend);
//                if ((serializeFlags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) state.WriteHalf(reverbZoneMix);
//            }
//            else
//            {
//                if ((serializeFlags & ReplayAudioSerializeFlags.Pitch) != 0) state.Write(pitch);
//                if ((serializeFlags & ReplayAudioSerializeFlags.Volume) != 0) state.Write(volume);
//                if ((serializeFlags & ReplayAudioSerializeFlags.StereoPan) != 0) state.Write(stereoPan);
//                if ((serializeFlags & ReplayAudioSerializeFlags.SpatialBlend) != 0) state.Write(spatialBlend);
//                if ((serializeFlags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) state.Write(reverbZoneMix);
//            }
//        }

//        public void OnReplayDeserialize(ReplayState state)
//        {
//            // Read flags
//            serializeFlags = (ReplayAudioSerializeFlags)state.ReadByte();

//            // Read mandatory values
//            isPlaying = state.ReadBool();
//            timeSample = state.ReadInt32();

//            // Check for low precision
//            if((serializeFlags & ReplayAudioSerializeFlags.LowPrecision) != 0)
//            {
//                if ((serializeFlags & ReplayAudioSerializeFlags.Pitch) != 0) pitch = state.ReadHalf();
//                if ((serializeFlags & ReplayAudioSerializeFlags.Volume) != 0) volume = state.ReadHalf();
//                if ((serializeFlags & ReplayAudioSerializeFlags.StereoPan) != 0) stereoPan = state.ReadHalf();
//                if ((serializeFlags & ReplayAudioSerializeFlags.SpatialBlend) != 0) spatialBlend = state.ReadHalf();
//                if ((serializeFlags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) reverbZoneMix = state.ReadHalf();
//            }
//            else
//            {
//                if ((serializeFlags & ReplayAudioSerializeFlags.Pitch) != 0) pitch = state.ReadSingle();
//                if ((serializeFlags & ReplayAudioSerializeFlags.Volume) != 0) volume = state.ReadSingle();
//                if ((serializeFlags & ReplayAudioSerializeFlags.StereoPan) != 0) stereoPan = state.ReadSingle();
//                if ((serializeFlags & ReplayAudioSerializeFlags.SpatialBlend) != 0) spatialBlend = state.ReadSingle();
//                if ((serializeFlags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) reverbZoneMix = state.ReadSingle();
//            }
//        }
//    }
//}
