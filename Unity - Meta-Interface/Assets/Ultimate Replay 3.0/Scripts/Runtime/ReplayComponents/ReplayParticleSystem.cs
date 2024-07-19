using System;
using UltimateReplay.Formatters;
using UnityEngine;

namespace UltimateReplay
{    
    /// <summary>
    /// A replay component which can be used to record and replay the Unity ParticleSystem.
    /// </summary>
    public class ReplayParticleSystem : ReplayRecordableBehaviour
    {
        // Internal
        internal ReplayParticleSystemFormatter.ReplayParticleSystemSerializeFlags serializeFlags = 0;

        // Private
        private static ReplayParticleSystemFormatter formatter = new ReplayParticleSystemFormatter();
                
        private float lastTime = 0;
        private float targetTime = 0;
        private uint randomSeed = 0;
        private bool isPlaying = false;

        // Public
        /// <summary>
        /// The Unity particle system that will be recorded and also used for playback.
        /// </summary>
        public ParticleSystem observedParticleSystem = null;

        // Internal
        [SerializeField]
        internal bool lowPrecision = false;
        [SerializeField]
        internal bool interpolate = true;

        // Properties
        public override ReplayFormatter Formatter
        {
            get { return formatter; }
        }

        public bool LowPrecision
        {
            get { return lowPrecision; }
            set 
            { 
                lowPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public bool Interpolate
        {
            get { return interpolate; }
            set { interpolate = value; }
        }

        // Methods
        protected override void Awake()
        {
            base.Awake();
            UpdateSerializeFlags();
        }

        private void Start()
        {
            if (observedParticleSystem == null)
                Debug.LogWarningFormat("Replay particle system '{0}' will not record or replay because the observed particle system has not been assigned", this);
        }

        protected override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to auto-find particle component
            if (observedParticleSystem == null)
                observedParticleSystem = GetComponent<ParticleSystem>();
        }

        /// <summary>
        ///  Called by the replay system when persistent data should be reset.
        /// </summary>
        protected override void OnReplayReset()
        {
            lastTime = targetTime;
        }

        /// <summary>
        /// Called by the replay system during playback mode.
        /// </summary>
        /// <param name="replayTime">The <see cref="ReplayTime"/> for the associated playback operation</param>
        protected override void OnReplayUpdate(float t)
        {
            // Check for no component
            if (observedParticleSystem == null)
                return;

            float time = targetTime;

            // Check for interpolate
            if (interpolate == true)
            {
                // Interpolate the time value
                time = Mathf.Lerp(lastTime, targetTime, t);

                if (isPlaying == true)
                {
                    // Reset particle system and set seed for deterministic simulation
                    observedParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);                    
                    observedParticleSystem.randomSeed = randomSeed;                    

                    // Set simulation time
                    if (time < observedParticleSystem.main.duration)
                    {
                        observedParticleSystem.Simulate(time, true, true);
                        observedParticleSystem.Play(true);
                    }
                }
                else
                {
                    if (observedParticleSystem.isEmitting == true)
                    {
                        observedParticleSystem.Stop(true);
                    }
                }
            }
        }

        /// <summary>
        /// Called by the replay system when the component should serialize its recorded data.
        /// </summary>
        /// <param name="state">The state object to write to</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedParticleSystem == null)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying == false)
                UpdateSerializeFlags();
#endif

            // Sample particle system
            formatter.UpdateFromParticleSystem(observedParticleSystem, serializeFlags);

            // Serialize
            formatter.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when the component should deserialize previously recorded data.
        /// </summary>
        /// <param name="state">The state object to read from</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedParticleSystem == null)
                return;

            // Reset 
            OnReplayReset();

            // Deserialize
            formatter.OnReplayDeserialize(state);

            // Check for interpolation
            if(interpolate == true)
            {
                randomSeed = formatter.RandomSeed;
                targetTime = formatter.SimulationTime;
                isPlaying = formatter.IsPlaying;
            }
            else
            {
                formatter.SyncParticleSystem(observedParticleSystem);
            }
        }

        private void UpdateSerializeFlags()
        {
            serializeFlags = ReplayParticleSystemFormatter.GetSerializeFlags(lowPrecision);
        }
    }
}
