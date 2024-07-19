using System;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayAudioFormatter : ReplayFormatter
    {
        // Type
        [Flags]
        internal enum ReplayAudioSerializeFlags : byte
        { 
            None = 0,
            Pitch = 1 << 0,
            Volume = 1 << 1,
            StereoPan = 1 << 2,
            SpatialBlend = 1 << 3,
            ReverbZoneMix = 1 << 4,
            LowPrecision = 1 << 5,
            IsPlaying = 1 << 6,
        }

        // Private
        [ReplayTokenSerialize("Serialize Flags")]
        private ReplayAudioSerializeFlags serializeFlags = 0;
        [ReplayTokenSerialize("Time Samples")]
        private int timeSample = 0;
        [ReplayTokenSerialize("Pitch")]
        private float pitch = 0f;
        [ReplayTokenSerialize("Volume")]
        private float volume = 0f;
        [ReplayTokenSerialize("Stereo Pan")]
        private float stereoPan = 0f;
        [ReplayTokenSerialize("Spatial Blend")]
        private float spatialBlend = 0f;
        [ReplayTokenSerialize("Reverb Zone Mix")]
        private float reverbZoneMix = 0f;

        // Properties
        internal ReplayAudioSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        public bool IsPlaying
        {
            get { return (serializeFlags & ReplayAudioSerializeFlags.IsPlaying) != 0; }
            set
            {
                // Clear bit
                serializeFlags &= ~ReplayAudioSerializeFlags.IsPlaying;

                // Set bit
                if (value == true) serializeFlags |= ReplayAudioSerializeFlags.IsPlaying;
            }
        }

        public int TimeSample => timeSample;
        public float Pitch => pitch;
        public float Volume => volume;
        public float StereoPan => stereoPan;
        public float SpatialBlend => spatialBlend;
        public float ReverbZoneMix => reverbZoneMix;

        // Methods
        public override void OnReplaySerialize(ReplayState state)
        {
            // Get flags
            ReplayAudioSerializeFlags flags = serializeFlags;

            // Write flags
            state.Write((byte)flags);

            // Write required value
            state.Write(timeSample);

            // Check for low precision
            if((serializeFlags & ReplayAudioSerializeFlags.LowPrecision) != 0)
            {
                if ((flags & ReplayAudioSerializeFlags.Pitch) != 0) state.WriteHalf(pitch);
                if ((flags & ReplayAudioSerializeFlags.Volume) != 0) state.WriteHalf(volume);
                if ((flags & ReplayAudioSerializeFlags.StereoPan) != 0) state.WriteHalf(stereoPan);
                if ((flags & ReplayAudioSerializeFlags.SpatialBlend) != 0) state.WriteHalf(spatialBlend);
                if ((flags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) state.WriteHalf(reverbZoneMix);
            }
            else
            {
                if ((flags & ReplayAudioSerializeFlags.Pitch) != 0) state.Write(pitch);
                if ((flags & ReplayAudioSerializeFlags.Volume) != 0) state.Write(volume);
                if ((flags & ReplayAudioSerializeFlags.StereoPan) != 0) state.Write(stereoPan);
                if ((flags & ReplayAudioSerializeFlags.SpatialBlend) != 0) state.Write(spatialBlend);
                if ((flags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) state.Write(reverbZoneMix);
            }
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            ReplayAudioSerializeFlags flags = serializeFlags = (ReplayAudioSerializeFlags)state.ReadByte();

            // Time sample
            timeSample = state.ReadInt32();

            // Check for low precision
            if((flags & ReplayAudioSerializeFlags.LowPrecision) != 0)
            {
                if ((flags & ReplayAudioSerializeFlags.Pitch) != 0) pitch = state.ReadHalf();
                if ((flags & ReplayAudioSerializeFlags.Volume) != 0) volume = state.ReadHalf();
                if ((flags & ReplayAudioSerializeFlags.StereoPan) != 0) stereoPan = state.ReadHalf();
                if ((flags & ReplayAudioSerializeFlags.SpatialBlend) != 0) spatialBlend = state.ReadHalf();
                if ((flags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) reverbZoneMix = state.ReadHalf();
            }
            else
            {
                if ((flags & ReplayAudioSerializeFlags.Pitch) != 0) pitch = state.ReadSingle();
                if ((flags & ReplayAudioSerializeFlags.Volume) != 0) volume = state.ReadSingle();
                if ((flags & ReplayAudioSerializeFlags.StereoPan) != 0) stereoPan = state.ReadSingle();
                if ((flags & ReplayAudioSerializeFlags.SpatialBlend) != 0) spatialBlend = state.ReadSingle();
                if ((flags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) reverbZoneMix = state.ReadSingle();
            }
        }

        internal void SyncAudioSource(AudioSource audio, ReplayAudioSerializeFlags flags)
        {
            // Update optional only
            if ((flags & ReplayAudioSerializeFlags.Pitch) != 0) audio.pitch = this.pitch;
            if ((flags & ReplayAudioSerializeFlags.Volume) != 0) audio.volume = this.volume;
            if ((flags & ReplayAudioSerializeFlags.StereoPan) != 0) audio.panStereo = this.stereoPan;
            if ((flags & ReplayAudioSerializeFlags.SpatialBlend) != 0) audio.spatialBlend = this.spatialBlend;
            if ((flags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) audio.reverbZoneMix = this.reverbZoneMix;
        }

        internal void UpdateFromAudioSource(AudioSource audio, ReplayAudioSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;

            // Update main values
            this.IsPlaying = audio.isPlaying;
            this.timeSample = audio.timeSamples;

            // Update optional
            if ((flags & ReplayAudioSerializeFlags.Pitch) != 0) this.pitch = audio.pitch;
            if ((flags & ReplayAudioSerializeFlags.Volume) != 0) this.volume = audio.volume;
            if ((flags & ReplayAudioSerializeFlags.StereoPan) != 0) this.stereoPan = audio.panStereo;
            if ((flags & ReplayAudioSerializeFlags.SpatialBlend) != 0) this.spatialBlend = audio.spatialBlend;
            if ((flags & ReplayAudioSerializeFlags.ReverbZoneMix) != 0) this.reverbZoneMix = audio.reverbZoneMix;
        }

        internal static ReplayAudioSerializeFlags GetSerializeFlags(bool pitch, bool volume, bool stereoPan, bool spatialBlend, bool reverbZoneMix, RecordPrecision precision)
        {
            ReplayAudioSerializeFlags flags = 0;

            if (pitch == true) flags |= ReplayAudioSerializeFlags.Pitch;
            if (volume == true) flags |= ReplayAudioSerializeFlags.Volume;
            if (stereoPan == true) flags |= ReplayAudioSerializeFlags.StereoPan;
            if (spatialBlend == true) flags |= ReplayAudioSerializeFlags.SpatialBlend;
            if (reverbZoneMix == true) flags |= ReplayAudioSerializeFlags.ReverbZoneMix;

            if ((precision & RecordPrecision.HalfPrecision16Bit) != 0) flags |= ReplayAudioSerializeFlags.LowPrecision;

            return flags;
        }
    }
}
