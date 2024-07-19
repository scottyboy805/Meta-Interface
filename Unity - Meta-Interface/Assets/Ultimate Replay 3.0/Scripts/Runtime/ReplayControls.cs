using System;
using UltimateReplay.Storage;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateReplay
{
    public class ReplayControls : MonoBehaviour
    {
        // Type
        [Serializable]
        public class HighlightButton
        {
            // Public
            public Button button;
            public Image highlight;
        }

        /// <summary>
        /// Helper class used to detect drag start and end events on UI slider control (Seek slider bar).
        /// </summary>
        public class SliderCallback : MonoBehaviour, IBeginDragHandler, IEndDragHandler
        {
            // Public
            public bool isDragging = false;

            // Methods
            public void OnBeginDrag(PointerEventData eventData)
            {
                // Set dragging flag
                isDragging = true;
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                // Unset dragging flag
                isDragging = false;
            }
        }

        [Serializable]
        private class UIControls
        {
#pragma warning disable 0649 // Value is never assigned - They are assigned in inspector
            public HighlightButton liveMode;

            [Header("Playback Mode")]
            public GameObject playModeRoot;
            public HighlightButton playMode;

            [Header("Record Mode")]
            public GameObject recordModeRoot;
            public HighlightButton recordMode;            

            [Header("Record Mode - Info")]
            public Text recordDuration;

            [Header("Playback Mode - Info")]
            public Text playbackTime;

            [Header("Playback Mode - Play/Pause")]
            public HighlightButton playPause;

            [Header("Playback Mode - Seek")]
            public Slider seek;
            public Slider seekVisuals;

            [Header("Playback Mode - Speed")]
            public GameObject playbackSpeedRoot;
            public HighlightButton playbackSpeed;
            public HighlightButton speedX2;
            public HighlightButton speedNormal;
            public HighlightButton speedX075;
            public HighlightButton speedX050;
            public HighlightButton speedX025;
            public Slider speedCustom;
            public Text speedCustomText;

            [Header("Playback Mode - Direction")]
            public HighlightButton playbackDirection;

            [Header("Playback Mode - Looped")]
            public HighlightButton playbackLooped;
#pragma warning restore 0649
        }


        // Private
        private SliderCallback sliderCallback = null;

        // Protected
        protected ReplayStorage storage = new ReplayMemoryStorage();
        protected ReplayRecordOperation record = null;
        protected ReplayPlaybackOperation playback = null;

        // Public
        [SerializeField]
        private UIControls controls;

        [Header("Options")]
        [Tooltip("Should recording start as soon as the replay controls have loaded")]
        public bool recordOnStart = true;
        [Tooltip("Replays will be saved to file when enabled or will be stored in memory when disabled")]
        public bool recordToFile = false;
        [Tooltip("The name of the replay file to save when 'recordToFile' is enabled")]
        public string recordFileName = "MyReplay.replay";

        // Properties
        public bool IsRecording
        {
            get { return record != null && record.IsRecordingOrPaused; }
        }

        public bool IsReplaying
        {
            get { return playback != null && playback.IsReplayingOrPaused; }
        }

        public bool IsLive
        {
            get { return IsRecording == false && IsReplaying == false; }
        }

        // Methods
        protected virtual void Awake()
        {
            // Add listeners
            if(controls.liveMode.button != null) controls.liveMode.button.onClick.AddListener(ReplayGoLive);
            if(controls.playMode.button != null) controls.playMode.button.onClick.AddListener(ReplayBeginPlayback);
            if(controls.recordMode.button != null) controls.recordMode.button.onClick.AddListener(ReplayBeginRecording);

            if (controls.playPause.button != null) controls.playPause.button.onClick.AddListener(TogglePlaybackPaused);
            if (controls.playbackSpeed.button != null) controls.playbackSpeed.button.onClick.AddListener(TogglePlaybackSpeedMenu);
            if (controls.playbackDirection.button != null) controls.playbackDirection.button.onClick.AddListener(TogglePlaybackDirection);
            if (controls.playbackLooped.button != null) controls.playbackLooped.button.onClick.AddListener(TogglePlaybackLooped);
                        
            if (controls.speedX2.button != null) controls.speedX2.button.onClick.AddListener(() => SetPlaybackSpeed(2f));
            if (controls.speedNormal.button != null) controls.speedNormal.button.onClick.AddListener(() => SetPlaybackSpeed(1f));
            if (controls.speedX075.button != null) controls.speedX075.button.onClick.AddListener(() => SetPlaybackSpeed(0.75f));
            if (controls.speedX050.button != null) controls.speedX050.button.onClick.AddListener(() => SetPlaybackSpeed(0.5f));
            if (controls.speedX025.button != null) controls.speedX025.button.onClick.AddListener(() => SetPlaybackSpeed(0.25f));
            if (controls.speedCustom != null) controls.speedCustom.onValueChanged.AddListener(SetPlaybackSpeed);

            if (controls.seek != null) controls.seek.onValueChanged.AddListener(SeekPlayback);
            if (controls.seek != null) sliderCallback = controls.seek.gameObject.AddComponent<SliderCallback>();


            // Check for event system
            if (EventSystem.current == null)
                Debug.LogWarning("No event system in current scene. Replay controls may not function correctly");
        }

        protected virtual void Start()
        {            
            // Check for record file
            if (recordToFile == true)
                storage = ReplayFileStorage.FromFile(recordFileName, ReplayFileType.FromExtension, true);

            // Enter live mode by default
            ReplayGoLive();

            // Hide playback speed
            if(controls.playbackSpeedRoot != null) controls.playbackSpeedRoot.SetActive(false);

            // Check for record on start
            if(recordOnStart == true)
                ReplayBeginRecording();
        }

        protected virtual void Update()
        {
            // Update recording
            if(IsRecording == true)
            {
                // Update duration info
                if(controls.recordDuration != null) controls.recordDuration.text = "Recording: " + storage.Duration.ToString("0.00");
            }

            // Update playback
            if(IsReplaying == true)
            {
                // Update held slider bar (Pause update effect)
                if (controls.seek != null && sliderCallback != null && sliderCallback.isDragging == true)
                {
                    // Keep seeking to the same time value while the user has dragged the slider
                    SeekPlayback(controls.seek.value);
                }
                else
                {
                    // Update seek slider visuals
                    if (controls.seekVisuals != null) controls.seekVisuals.SetValueWithoutNotify(playback.PlaybackTimeNormalized);
                }
                
                // Update time info
                if(controls.playbackTime != null) controls.playbackTime.text = playback.PlaybackTime.ToString("0.00") + " / " + playback.Duration.ToString("0.00");
            }
        }

        protected virtual void OnDestroy()
        {
            storage.Dispose();
            storage = null;
        }

        public virtual void ReplayGoLive()
        {
            // Activate UI
            HideTabUI();
            if(controls.liveMode.highlight != null) controls.liveMode.highlight.enabled = true;

            // Stop replay operations
            ReplayStopRecording();
            ReplayStopPlayback();
        }

        public virtual void ReplayBeginRecording()
        {
            // Check for already recording
            if (IsRecording == true)
                return;

            // Stop playback operations
            ReplayStopPlayback();

            // Activate UI
            HideTabUI();
            if(controls.recordMode.highlight != null) controls.recordMode.highlight.enabled = true;

            // Show record overlay
            if(controls.recordModeRoot != null) controls.recordModeRoot.SetActive(true);

            // Start recording
            ReplayStartRecording();
        }

        public virtual void ReplayBeginPlayback()
        {
            // Check for already replaying
            if (IsReplaying == true)
                return;

            // Stop record operations
            ReplayStopRecording();

            // Activate UI
            HideTabUI();
            if(controls.playMode.highlight != null) controls.playMode.highlight.enabled = true;

            // Show playback overlay
            if(controls.playModeRoot != null) controls.playModeRoot.SetActive(true);

            // Disable play button
            if (controls.playPause.highlight != null) controls.playPause.highlight.enabled = false;

            // Disable speed, direction and looped toggle
            if(controls.playbackSpeed.highlight != null) controls.playbackSpeed.highlight.enabled = false;
            if (controls.playbackDirection.highlight != null) controls.playbackDirection.highlight.enabled = false;
            if (controls.playbackLooped.highlight != null) controls.playbackLooped.highlight.enabled = false;

            // Set playback speed
            SetPlaybackSpeed(1f);

            // Start playback
            ReplayStartPlayback();
        }

        protected virtual void ReplayStopRecording()
        {
            // Stop any record operations
            if (record != null && record.IsDisposed == false)
            {
                record.Dispose();
                record = null;
            }
        }

        protected virtual void ReplayStopPlayback()
        {
            // Stop any playback operations
            if (playback != null && playback.IsDisposed == false)
            {
                playback.Dispose();
                playback = null;

                // Stop play button
                if(controls.playPause.highlight != null) controls.playPause.highlight.enabled = false;
            }
        }

        protected virtual void ReplayStartRecording()
        {
            // Start new operation
            record = ReplayManager.BeginRecording(storage);
        }

        protected virtual void ReplayStartPlayback()
        {
            // Start new operation
            playback = ReplayManager.BeginPlayback(storage);

            // Add end listener
            playback.OnPlaybackEnd.AddListener(OnPlaybackEnd);
        }

        
        public void SeekPlayback(float value)
        {
            // Make sure we are replaying
            if(IsReplaying == true)
            {
                // Seek to location in playback
                playback.SeekPlaybackNormalized(value);
            }

            // Update visuals
            if (controls.seekVisuals != null) controls.seekVisuals.SetValueWithoutNotify(value);
        }

        public void TogglePlaybackPaused()
        {
            // Make sure we are replaying
            if(IsReplaying == true)
            {
                // Set play paused
                if(playback.IsPlaybackPaused == true)
                {
                    playback.ResumePlayback();
                    if(controls.playPause.highlight != null) controls.playPause.highlight.enabled = false;
                }
                else
                {
                    playback.PausePlayback();
                    if(controls.playPause.highlight != null) controls.playPause.highlight.enabled = true;
                }
            }
        }

        public void TogglePlaybackSpeedMenu()
        {
            // Toggle enabled
            if(controls.playbackSpeedRoot != null) controls.playbackSpeedRoot.SetActive(!controls.playbackSpeedRoot.activeSelf);

            // Toggle highlight
            if (controls.playbackSpeed.highlight != null) controls.playbackSpeed.highlight.enabled = !controls.playbackSpeed.highlight.enabled;
        }

        public void TogglePlaybackDirection()
        {
            // Make sure we are replaying
            if(IsReplaying == true)
            {
                if(playback.PlaybackDirection == PlaybackDirection.Forward)
                {
                    // Set backward
                    playback.PlaybackDirection = PlaybackDirection.Backward;
                    if (controls.playbackDirection.highlight != null) controls.playbackDirection.highlight.enabled = true;
                }
                else
                {
                    // Set forward
                    playback.PlaybackDirection = PlaybackDirection.Forward;
                    if (controls.playbackDirection.highlight != null) controls.playbackDirection.highlight.enabled = false;
                }
            }
        }

        public void TogglePlaybackLooped()
        {
            // Make sure we are replaying
            if(IsReplaying == true)
            {
                if(playback.EndBehaviour == PlaybackEndBehaviour.EndPlayback)
                {
                    // Set looped
                    playback.Options.PlaybackEndBehaviour = PlaybackEndBehaviour.LoopPlayback;
                    if (controls.playbackLooped.highlight != null) controls.playbackLooped.highlight.enabled = true;
                }
                else
                {
                    // Set end
                    playback.Options.PlaybackEndBehaviour = PlaybackEndBehaviour.EndPlayback;
                    if (controls.playbackLooped.highlight != null) controls.playbackLooped.highlight.enabled = false;
                }
            }
        }

        public void SetPlaybackSpeed(float value)
        {
            // Hide Ui
            HidePlaybackSpeedUI();

            // Update value
            if(controls.speedCustom != null) controls.speedCustom.SetValueWithoutNotify(value);

            // Update hint
            if(controls.speedCustomText != null) controls.speedCustomText.text = "Speed X" + value.ToString("0.00");


            // Update replay
            if (IsReplaying == true)
            {
                // Set time scale
                playback.PlaybackTimeScale = value;
            }

            // Check for preset selection
            switch (Mathf.RoundToInt(value * 100f))
            {
                case 200: if(controls.speedX2.highlight != null) controls.speedX2.highlight.enabled = true; break;
                case 100: if(controls.speedNormal.highlight != null) controls.speedNormal.highlight.enabled = true; break;
                case 75: if(controls.speedX075.highlight != null) controls.speedX075.highlight.enabled = true; break;
                case 50: if(controls.speedX050.highlight != null) controls.speedX050.highlight.enabled = true; break;
                case 25: if(controls.speedX025.highlight != null) controls.speedX025.highlight.enabled = true; break;
            }
        }

        private void OnPlaybackEnd()
        {
            // Reset playback
            playback = null;

            // Switch to live mode
            ReplayGoLive();
        }

        private void HideTabUI()
        {
            // Hide all Ui overlay
            if(controls.recordModeRoot != null) controls.recordModeRoot.SetActive(false);
            if(controls.playModeRoot != null) controls.playModeRoot.SetActive(false);

            // Hide all highlights
            if(controls.liveMode.highlight != null) controls.liveMode.highlight.enabled = false;
            if(controls.recordMode.highlight != null) controls.recordMode.highlight.enabled = false;
            if(controls.playMode.highlight != null) controls.playMode.highlight.enabled = false;
        }

        private void HidePlaybackSpeedUI()
        {
            // Hide all highlights
            if(controls.speedX2.highlight != null) controls.speedX2.highlight.enabled = false;
            if(controls.speedNormal.highlight != null) controls.speedNormal.highlight.enabled = false;
            if(controls.speedX075.highlight != null) controls.speedX075.highlight.enabled = false;
            if(controls.speedX050.highlight != null) controls.speedX050.highlight.enabled = false;
            if(controls.speedX025.highlight != null) controls.speedX025.highlight.enabled = false;
        }
    }
}
