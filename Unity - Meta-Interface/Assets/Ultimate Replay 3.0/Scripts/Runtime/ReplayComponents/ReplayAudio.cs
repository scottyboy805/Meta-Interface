using System;
using System.Collections;
using UltimateReplay.Formatters;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Used to record and replay an AudioSource component 
    /// </summary>
    public class ReplayAudio : ReplayRecordableBehaviour
    {
        private struct ReplayAudioData
        {
            // Public
            public bool isPlaying;
            public int timeSample;
            public float pitch;
            public float volume;
            public float stereoPan;
            public float spatialBlend;
            public float reverbZoneMix;
        }

        // Internal
        internal ReplayAudioFormatter.ReplayAudioSerializeFlags serializeFlags = 0;

        // Private
        private const byte audioEventIDForward = 14;
        private const byte audioEventIDBackward = 15;
        private static readonly ReplayAudioFormatter formatter = new ReplayAudioFormatter();

        private ReplayAudioFormatter.ReplayAudioSerializeFlags deserializeFlags = 0;
        private ReplayAudioData lastAudio;
        private ReplayAudioData targetAudio;
        private bool lastPlayState = false;
        private float lastPlayTime = 0;

        // Internal
        [SerializeField]
        [HideInInspector]
        internal bool replayPitch = true;
        [SerializeField]
        [HideInInspector]
        internal bool replayVolume = true;
        [SerializeField]
        [HideInInspector]
        internal bool replaySteroPan = false;
        [SerializeField]
        [HideInInspector]
        internal bool replaySpatialBlend = false;
        [SerializeField]
        [HideInInspector]
        internal bool replayReverbZoneMix = false;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision recordPrecision = RecordPrecision.HalfPrecision16Bit;
        [SerializeField]
        [HideInInspector]
        internal bool interpolate = true;

        // Public
        /// <summary>
        /// The AudioSource component that will be observed during recording and used for playback during replays.
        /// Only a single AudioClip is supported and should be assigned to the AudioSource.
        /// </summary>
        public AudioSource observedAudio = null;

        // Methods
        protected override void Awake()
        {
            base.Awake();
            UpdateSerializeFlags();
        }

        private void Start()
        {
            if (observedAudio == null)
                Debug.LogWarningFormat("Replay audio '{0}' will not record or replay because the observed audio has not been assigned", this);
        }

        private void Update()
        {
            // Check for a valid source
            if (observedAudio == null)
                return;

            // Check for recording
            if (IsRecording == true)
            {
                // Check if Play has been called. Note that consideration is required for calling Play again before another sound has finished playing
                if ((observedAudio.isPlaying == true && lastPlayState == false) ||
                    (observedAudio.isPlaying == true && observedAudio.time < lastPlayTime))
                {
                    // Record an audio start event
                    RecordEvent(audioEventIDForward);

                    // Record an end event when the clip finishes playing
                    if (observedAudio.clip != null)
                        StartCoroutine(ScheduleEndEvent(observedAudio.clip.length * observedAudio.pitch));
                }

                // Update last state
                lastPlayState = observedAudio.isPlaying;
                lastPlayTime = observedAudio.time;
            }
        }

        private IEnumerator ScheduleEndEvent(float delay)
        {
            // Wait for time to pas
            yield return new WaitForSeconds(delay);

            // Check for still recording
            if (IsRecording == true)
                RecordEvent(audioEventIDBackward);
        }

        protected override void Reset()
        { 
            // Call base method
            base.Reset();

            // Try to auto-find component
            if (observedAudio == null)
                observedAudio = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Called by the replay system when the component should reset any persistent data.
        /// </summary>
        protected override void OnReplayReset()
        {
            lastAudio = targetAudio;
            lastPlayState = false;
            lastPlayTime = 0;
        }

        /// <summary>
        /// Called by the replay system when an event occurs.
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="eventData"></param>
        protected override void OnReplayEvent(ushort eventID, ReplayState eventData)
        {
            // Check for no audio
            if (observedAudio == null || observedAudio.clip == null)
                return;

            // Check for audio event
            if(eventID == audioEventIDForward && PlaybackDirection == PlaybackDirection.Forward)
            {
                // Play the audio sound
                observedAudio.Play();
            }
            else if(eventID == audioEventIDBackward && PlaybackDirection == PlaybackDirection.Backward)
            {
                // Play the audio sound from the end
                observedAudio.timeSamples = observedAudio.clip.samples - 1;
                observedAudio.Play();
            }
        }

        /// <summary>
        /// Called by the replay system during playback mode.
        /// </summary>
        /// <param name="replayTime">The <see cref="ReplayTime"/> associated with the playback operation for this replay component</param>
        protected override void OnReplayUpdate(float t)
        {
            // Check for component
            if (observedAudio == null || observedAudio.enabled == false)
                return;

            // Update audio source values
            ReplayAudioData updateAudio = targetAudio;

            if(interpolate == true)
            {
                updateAudio.pitch = Mathf.Lerp(lastAudio.pitch, targetAudio.pitch, t);
                updateAudio.volume = Mathf.Lerp(lastAudio.volume, targetAudio.volume, t);
                updateAudio.stereoPan = Mathf.Lerp(lastAudio.stereoPan, targetAudio.stereoPan, t);
                updateAudio.spatialBlend = Mathf.Lerp(lastAudio.spatialBlend, targetAudio.spatialBlend, t);
                updateAudio.reverbZoneMix = Mathf.Lerp(lastAudio.reverbZoneMix, targetAudio.reverbZoneMix, t);
            }

            // Apply options
            float pitch = ((PlaybackDirection == PlaybackDirection.Forward) ? updateAudio.pitch : -updateAudio.pitch) * PlaybackTimeScale;

            // Restore options
            if(replayPitch == true) observedAudio.pitch = pitch;
            if(replayVolume == true) observedAudio.volume = updateAudio.volume;
            if(replaySteroPan == true) observedAudio.panStereo = updateAudio.stereoPan;
            if(replaySpatialBlend == true) observedAudio.spatialBlend = updateAudio.spatialBlend;
            if(replayReverbZoneMix == true) observedAudio.reverbZoneMix = updateAudio.reverbZoneMix;
        }

        /// <summary>
        /// Called by the replay system when playback is paused or resumed.
        /// </summary>
        /// <param name="paused">True if the replay system is paused or false if it is resuming</param>
        protected override void OnReplayPlayPause(bool paused)
        {
            // Check for no component
            if (observedAudio == null || observedAudio.enabled == false)
                return;

            if (paused == true)
            {
                // Pause the clip
                if (observedAudio.isPlaying == true)
                    observedAudio.Pause();
            }
            else
            {
                // Unpause the clip
                if (observedAudio.isPlaying == true)
                    observedAudio.UnPause();
            }
        }

        /// <summary>
        /// Called by the replay system when the replay component should serialize its recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to write to</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedAudio == null || observedAudio.enabled == false)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying == false)
                UpdateSerializeFlags();
#endif

            // Update formatter
            formatter.UpdateFromAudioSource(observedAudio, serializeFlags);

            // Serialize data
            formatter.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when the replay component should deserialize previously recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to read from</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component - no point wasting time deserializing because the component will not be updated
            if (observedAudio == null || observedAudio.enabled == false)
                return;

            // Update last
            lastAudio = targetAudio;

            // Deserialize data
            formatter.OnReplayDeserialize(state);

            // Get serialize flags
            this.deserializeFlags = formatter.SerializeFlags;

            // Update target
            targetAudio.isPlaying = formatter.IsPlaying;
            targetAudio.timeSample = formatter.TimeSample;
            targetAudio.pitch = formatter.Pitch;
            targetAudio.volume = formatter.Volume;
            targetAudio.stereoPan = formatter.StereoPan;
            targetAudio.spatialBlend = formatter.SpatialBlend;
            targetAudio.reverbZoneMix = formatter.ReverbZoneMix;

            // Update immediate if interpolation is disabled
            if(interpolate == false)
            {
                // Update audio source
                formatter.SyncAudioSource(observedAudio, deserializeFlags);
            }
        }

        private void UpdateSerializeFlags()
        {
            serializeFlags = ReplayAudioFormatter.GetSerializeFlags(replayPitch, replayVolume, replaySteroPan, replaySpatialBlend, replayReverbZoneMix, recordPrecision);
        }
    }
}
