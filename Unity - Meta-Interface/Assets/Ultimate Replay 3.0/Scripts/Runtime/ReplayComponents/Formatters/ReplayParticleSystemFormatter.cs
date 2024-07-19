using UnityEngine;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayParticleSystemFormatter : ReplayFormatter
    {
        // Types
        public enum ReplayParticleSystemSerializeFlags : byte
        {
            None = 0,
            LowPrecision = 1 << 1,
        }

        // Private
        private ReplayParticleSystemSerializeFlags serializeFlags = 0;
        private uint randomSeed = 0;
        private float simulationTime = 0f;
        private bool isPlaying = false;

        // Properties
        internal ReplayParticleSystemSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        public uint RandomSeed
        {
            get { return randomSeed; }
        }

        public float SimulationTime
        {
            get { return simulationTime; }
        }

        public bool IsPlaying
        {
            get { return isPlaying; }
        }

        // Methods
        public override void OnReplaySerialize(ReplayState state)
        {
            state.Write((byte)serializeFlags);

            // Write seed value in full precision
            state.Write(randomSeed);

            // Check for low precision
            if ((serializeFlags & ReplayParticleSystemSerializeFlags.LowPrecision) != 0)
            {
                // Write half precision
                state.WriteHalf(simulationTime);
            }
            else
            {
                // Write full precision
                state.Write(simulationTime);
            }

            // Write play state
            state.Write(isPlaying);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Read serialize flags
            serializeFlags = (ReplayParticleSystemSerializeFlags)state.ReadByte();

            // Read seed
            randomSeed = state.ReadUInt32();

            // Check for low precision
            if ((serializeFlags & ReplayParticleSystemSerializeFlags.LowPrecision) != 0)
            {
                // Read half precision
                simulationTime = state.ReadHalf();
            }
            else
            {
                // Read full precision
                simulationTime = state.ReadSingle();
            }

            // Read play state
            isPlaying = state.ReadBool();
        }

        public void SyncParticleSystem(ParticleSystem sync)
        {
            SyncParticleSystem(sync, serializeFlags);
        }

        internal void SyncParticleSystem(ParticleSystem sync, ReplayParticleSystemSerializeFlags flags)
        {
            if(isPlaying == true)
            {
                // Clear old simulation
                sync.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                sync.randomSeed = randomSeed;

                // Set simulation time
                if (simulationTime < sync.main.duration)
                    sync.Simulate(simulationTime, true, true);
            }
            else
            {
                // Stop simulating
                if (sync.isPlaying == true)
                    sync.Stop(true);
            }
        }

        public void UpdateFromParticleSystem(ParticleSystem from, bool lowPrecision = false)
        {
            // Calculate serialize flags
            ReplayParticleSystemSerializeFlags flags = GetSerializeFlags(lowPrecision);

            // Update from particle system
            UpdateFromParticleSystem(from, flags);
        }

        internal void UpdateFromParticleSystem(ParticleSystem from, ReplayParticleSystemSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;

            // Get simulation values
            this.randomSeed = from.randomSeed;
            this.simulationTime = from.time;
            this.isPlaying = from.isPlaying;
        }

        internal static ReplayParticleSystemSerializeFlags GetSerializeFlags(bool lowPrecision)
        {
            ReplayParticleSystemSerializeFlags flags = 0;

            // Low precision
            if (lowPrecision == true) flags |= ReplayParticleSystemSerializeFlags.LowPrecision;

            return flags;
        }
    }
}
