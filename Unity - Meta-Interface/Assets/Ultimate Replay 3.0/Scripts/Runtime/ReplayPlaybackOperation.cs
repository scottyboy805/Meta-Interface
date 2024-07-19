using System;
using System.Collections;
using UltimateReplay.Storage;
using UnityEngine;
using UnityEngine.Events;

namespace UltimateReplay
{
    /// <summary>
    /// Represents a playback node that can be used to calculate playback offsets.
    /// </summary>
    public enum PlaybackOrigin
    {
        /// <summary>
        /// The start of the playback sequence.
        /// </summary>
        Start,
        /// <summary>
        /// The current frame in the playback sequence.
        /// </summary>
        Current,
        /// <summary>
        /// The end of the playback sequence.
        /// </summary>
        End,
    }

    /// <summary>
    /// Used to indicate what should happen when the end of a replay is reached.
    /// </summary>
    public enum PlaybackEndBehaviour
    {
        /// <summary>
        /// The playback service should automatically end the replay and trigger and playback end events listeners.
        /// The active replay scene will also be reverted to live mode causing physics objects and scripts to be re-activated.
        /// </summary>
        EndPlayback,
        /// <summary>
        /// The playback service should stop the playback and return to the start of the replay.
        /// The active replay scene will remain in playback mode and you will need to call <see cref="ReplayManager.StopPlayback(ref ReplayHandle, bool)"/> manually to end playback.
        /// </summary>
        StopPlayback,
        /// <summary>
        /// The playback service should loop back around to the start of the replay and continue playing.
        /// The replay will play indefinitely until <see cref="ReplayManager.StopPlayback(ref ReplayHandle, bool)"/> is called.
        /// </summary>
        LoopPlayback
    }

    /// <summary>
    /// The playback direction used during replay playback.
    /// </summary>
    public enum PlaybackDirection
    {
        /// <summary>
        /// The replay should be played back in normal mode.
        /// </summary>
        Forward,
        /// <summary>
        /// The replay should be played back in reverse mode.
        /// </summary>
        Backward,
    }

    /// <summary>
    /// The playback seek behaviour that will be used when seeking to a certain time stamp.
    /// </summary>
    public enum PlaybackSeekSnap
    {
        /// <summary>
        /// The replay system will interpolate between frames if possible when seeking.
        /// Seeking will give a smooth seamless transition if replay components support interpolation.
        /// </summary>
        Smooth,
        /// <summary>
        /// The replay system will constrain seeking to snapshot frames.
        /// Can give a notchy effect when seeking as the time stamp snaps to the nearest snapshot frame.
        /// </summary>
        SnapToFrame,
    }

    public enum RestoreSceneMode
    {
        /// <summary>
        /// Restore the scene state to just before the replay started.
        /// </summary>
        RestoreState,
        /// <summary>
        /// Do not restore the scene state and keep replay objects in their current state at the time the replay ends.
        /// Use this option for rewind time effect for example to keep playing the game from a certain point in the replay.
        /// </summary>
        KeepState,
    }

    /// <summary>
    /// Represents a dedicated playback operation in progress.
    /// Provides access to all playback replated API's for a specific playback operation.
    /// </summary>
    public sealed class ReplayPlaybackOperation : ReplayOperation, IDisposable
    {
        // Events
        public UnityEvent OnPlaybackEnd = new UnityEvent();
        public UnityEvent OnPlaybackStop = new UnityEvent();
        public UnityEvent OnPlaybackLooped = new UnityEvent();

        // Private
        private bool isDisposed = false;
        private ReplayPlaybackOptions options = null;
        private PlaybackDirection playbackDirection = PlaybackDirection.Forward;
        private PlaybackSeekSnap playbackSeekSnap = PlaybackSeekSnap.Smooth;
        private RestoreSceneMode restoreMode = RestoreSceneMode.RestoreState;
        private float time = 0f;
        private float timeScale = 1f;
        private bool paused = false;
        private ReplaySnapshot current = null;
        private ReplaySnapshot last = null;

        private float serviceTimer = 0f;
        private bool playbackStopped = false;

        // Public
        /// <summary>
        /// The default playback fps rate.
        /// </summary>
        public const float defaultPlaybackRate = 60f;

        // Properties
        /// <summary>
        /// Check if this playback operation has been disposed.
        /// A playback operation becomes disposed when playback has been stopped, at which point the API becomes unusable.
        /// </summary>
        public override bool IsDisposed
        {
            get { return isDisposed; }
        }

        /// <summary>
        /// Get the <see cref="ReplayUpdateMode"/> for this replay operation.
        /// This value determines at what stage in the Unity game loop the playback operation is updated. 
        /// </summary>
        public override ReplayUpdateMode UpdateMode
        {
            get { return options.playbackUpdateMode; }
        }

        /// <summary>
        /// Get the <see cref="ReplayPlaybackOptions"/> for this replay operation.
        /// </summary>
        public ReplayPlaybackOptions Options
        {
            get
            {
                CheckDisposed();
                return options;
            }
        }

        /// <summary>
        /// Get the duration of the replay.
        /// </summary>
        public float Duration
        {
            get 
            {
                CheckDisposed();
                return Storage.Duration; 
            }
        }

        /// <summary>
        /// Get the current playback time of this operation in seconds.
        /// Playback time will always be between 0 and <see cref="Duration"/>.
        /// To change the current playback time use <see cref="SeekPlayback(float, PlaybackOrigin, bool)"/> or <see cref="SeekPlaybackNormalized(float, bool)"/>.
        /// </summary>
        public float PlaybackTime
        {
            get 
            {
                CheckDisposed();
                return time;
            }
        }

        /// <summary>
        /// Get the current normalized playback time of this operation.
        /// The normalized time will always be between 0 and 1, where 0 represents that start of the replay, 1 represents the end of the relay, and 0.5 represents the middle of the replay.
        /// Can be used to easily seek to common offsets such as (middle) without needing to calculate the time based on <see cref="Duration"/>.
        /// </summary>
        public float PlaybackTimeNormalized
        {
            get 
            {
                CheckDisposed();
                return Mathf.InverseLerp(0f, Duration, time);
            }
        }

        /// <summary>
        /// The current playback time scale which represents the speed at which playback will occur.
        /// The playback time scale is used as a multiplier so a value of 1 represents normal speed, 2 represents twice the speed, and 0.5 represents half the speed.
        /// </summary>
        public float PlaybackTimeScale
        {
            get 
            {
                CheckDisposed();
                return timeScale; 
            }
            set 
            {
                CheckDisposed();
                timeScale = Mathf.Abs(value); 
            }
        }
        
        /// <summary>
        /// The current playback direction.
        /// Use <see cref="PlaybackDirection.Backward"/> to replay in reverse.
        /// </summary>
        public PlaybackDirection PlaybackDirection
        {
            get 
            {
                CheckDisposed();
                return playbackDirection; 
            }
            set 
            {
                CheckDisposed();

                // Reset cached
                last = null;

                playbackDirection = value; 
            }
        }

        /// <summary>
        /// Get the current playback end behaviour which determines what will happen when the replay reaches the end.
        /// By default, playback will end and the associated replay scene will switch back to live mode so that gameplay can resume.
        /// </summary>
        public PlaybackEndBehaviour EndBehaviour
        {
            get 
            {
                CheckDisposed();
                return options.playbackEndBehaviour; 
            }
        }

        /// <summary>
        /// The current playback seek snap setting.
        /// Seek snap determines how seeking behaviours in relation to the snapshots that are available from the recording.
        /// <see cref="PlaybackSeekSnap.SnapToFrame"/> means that the replay system will clamp to the nearest snapshot. This gives a snappy effect while drag seeking as the replay jumps to the nearest recorded snapshot.
        /// <see cref="PlaybackSeekSnap.Smooth"/> means that the replay system may interpolate between multiple snapshots if the time values falls between 2 snapshots. This gives a smooth replay while drag seeking.
        /// </summary>
        public PlaybackSeekSnap SeekSnap
        {
            get
            {
                CheckDisposed();
                return playbackSeekSnap;
            }
            set
            {
                CheckDisposed();
                playbackSeekSnap = value;
            }
        }

        /// <summary>
        /// The current scene restore mode which determines what will happen to the associated replay objects when playback ends.
        /// <see cref="RestoreSceneMode.KeepState"/> means that replay objects will maintain their current state when the replay ends, meaning that gameplay can continue from the current playback positions.
        /// <see cref="RestoreSceneMode.RestoreState"/> means that the replay system will restore all replay objects to their original state immediately before playback began.
        /// </summary>
        public RestoreSceneMode RestoreSceneMode
        {
            get 
            {
                CheckDisposed();
                return restoreMode; 
            }
            set 
            {
                CheckDisposed();
                restoreMode = value; 
            }
        }

        /// <summary>
        /// Returns a value indicating whether playback is in progress and the playback is not currently paused.
        /// </summary>
        public bool IsReplaying
        {
            get
            {
                CheckDisposed();
                return isDisposed == false && paused == false;
            }
        }

        /// <summary>
        /// Returns a value indicating whether the playback is currently paused.
        /// </summary>
        public bool IsPlaybackPaused
        {
            get
            {
                CheckDisposed();
                return paused;
            }
        }

        /// <summary>
        /// Returns a value indicating whether playback is in progress or if the playback is currently paused.
        /// </summary>
        public bool IsReplayingOrPaused
        {
            get
            {
                CheckDisposed();
                return isDisposed == false;
            }
        }

        /// <summary>
        /// The target number of playback frames that will be simulated per second.
        /// Higher rates will allow for smooth and more accurate playback, but may have an additional performance hit.
        /// The replay system will not be able to playback faster than your current frame rate so there is no benefit in setting a value of '90' for example if you game will only run at 60 fps.
        /// Set this value to negative and the playback operation will run as fast as possible.
        /// Default value is 60fps.
        /// </summary>
        public float PlaybackRate
        {
            get 
            {
                CheckDisposed();
                return options.playbackFPS; 
            }
        }

        // Constructor
        internal ReplayPlaybackOperation(ReplayManager manager, ReplayScene scene, ReplayStorage storage, ReplayPlaybackOptions options)
            : base(manager, scene, storage)
        {
            this.options = options;
        }

        // Methods
        /// <summary>
        /// The main update call for this replay operation.
        /// Can be called manually if required, but if manually update is required then it is recommended to use <see cref="ReplayManager.ReplayTick(float, ReplayUpdateMode)"/>.
        /// </summary>
        /// <param name="delta">The amount of time in seconds that has passed since the last update</param>
        public override void ReplayTick(float delta)
        {
            // Check for disposed
            CheckDisposed();

            // Check if the playback service can perform full update
            serviceTimer += delta;

            // Check for paused
            if (paused == true)
                return;

            // Make sure delta is positive
            delta = Mathf.Abs(delta);

            // Calculate scaled delta
            float scaledDelta = (delta * timeScale);

            // Check for playback direction
            if (playbackDirection == PlaybackDirection.Backward)
                scaledDelta = -scaledDelta;

            // Update time
            time += scaledDelta;

            

            // Check for elapsed
            if(serviceTimer >= options.PlaybackInterval)
            {
                // Reset timer
                serviceTimer = 0f;

                // Update replay playback
                ReplayPlaybackUpdate(time);
            }            
        }

        private void ReplayPlaybackUpdate(float time)
        {
            // Check for end
            switch (EndBehaviour)
            {
                case PlaybackEndBehaviour.EndPlayback:
                    {
                        // Check for playback time out of bounds
                        if ((playbackDirection == PlaybackDirection.Forward && time > Duration) ||
                            (playbackDirection == PlaybackDirection.Backward && time < Duration))
                        {
                            // Stop playback - late update only
                            // NOTE - playback end event will be called once all replay operations have been fully updated, usually at the end of the frame
                            ReplayManager.ReplayLateCallEvent(StopPlaybackEndEvent);
                        }
                        break;
                    }

                case PlaybackEndBehaviour.StopPlayback:
                    {
                        // Check for playback time out of bounds
                        if ((playbackDirection == PlaybackDirection.Forward && time > Duration) ||
                            (playbackDirection == PlaybackDirection.Backward && time < Duration))
                        {
                            // Set flag
                            playbackStopped = true;

                            // Trigger event
                            OnPlaybackStop.Invoke();

                            // Don't update out of bounds
                            return;
                        }
                        else
                        {
                            // Reset flag when seek
                            playbackStopped = false;
                        }
                        break;
                    }

                case PlaybackEndBehaviour.LoopPlayback:
                    {
                        if (playbackDirection == PlaybackDirection.Forward)
                        {
                            // Check for time passed end of replay
                            if (time > Duration)
                            {
                                // Return to start
                                SeekPlayback(0f, PlaybackOrigin.Start);

                                // Trigger event
                                OnPlaybackLooped.Invoke();
                                return;
                            }
                        }
                        else
                        {
                            // Check for time passed start of replay
                            if (time < 0)
                            {
                                // Return to end
                                SeekPlayback(0f, PlaybackOrigin.End);

                                // Trigger event
                                OnPlaybackLooped.Invoke();
                                return;
                            }
                        }
                        break;
                    }
            }

            // Update last
            last = current;

            ReplaySnapshot next;
            float t;

            // Fetch the current state from storage
            if (ReplayPlaybackFetchCurrentState(out current, out next, out t) == false)
                return;

            // Simulate all missed snapshot frames since the last update so that events etc are not missed
            if (last != null)
                ReplaySimulateMissedFrames(last, current);

            // Update scene with playback
            ReplayPerformSceneUpdate(last, current, next, t);
        }

        private bool ReplayPlaybackFetchCurrentState(out ReplaySnapshot current, out ReplaySnapshot next, out float t)
        {
            // Fetch time stamp
            current = storage.FetchSnapshot(time);

            // Check for no data
            if (current == null)
            {
                next = null;
                t = 0f;
                return false;
            }

            // Fetch next in sequence
            next = storage.FetchSnapshot(current.SequenceID + 1);

            // Check for null
            if (next == null)
                next = current;

            // Calculate snapshot delta
            t = Mathf.InverseLerp(current.TimeStamp, next.TimeStamp, time);

            return true;
        }

        private void ReplaySimulateMissedFrames(ReplaySnapshot lastSnapshot, ReplaySnapshot currentSnapshot)
        {
            // Get the difference in frame sequences
            int delta = Mathf.Abs(lastSnapshot.SequenceID - currentSnapshot.SequenceID);

            // The same frame or the next frame in the order is playing so no frames have been missed
            if (delta <= 1)
                return;

            if (PlaybackDirection == PlaybackDirection.Forward)
            {
                for (int i = lastSnapshot.SequenceID + 1; i < currentSnapshot.SequenceID; i++)
                {
                    // Fetch the missed frame
                    ReplaySnapshot missedFrame = storage.FetchSnapshot(i);

                    // Previous frame before current should not be simulated since replay components must be updated
                    bool simulate = i >= currentSnapshot.SequenceID - 1;

                    // Update replay
                    ReplayPerformSceneUpdate(null, null, missedFrame, 1f, false, simulate);
                }
            }
            else
            {
                for (int i = lastSnapshot.SequenceID - 1; i > currentSnapshot.SequenceID; i--)
                {
                    // Fetch the missed frame
                    ReplaySnapshot missedFrame = storage.FetchSnapshot(i);

                    // Next frame before current should not be simulated since replay components must be updated
                    bool simulate = i <= currentSnapshot.SequenceID + 1;

                    // Update replay
                    ReplayPerformSceneUpdate(null, null, missedFrame, 1f, false, simulate);
                }
            }
        }

        private void ReplayPerformSceneUpdate(ReplaySnapshot last, ReplaySnapshot current, ReplaySnapshot next, float t, bool replayUpdate = true, bool replaySimulate = false)
        {
            // Check for null
            if (next == null)
                return;

            // Only trigger scene update when snapshot has changed otherwise just trigger replay update for interpolation
            if (last == null || last.SequenceID != next.SequenceID - 1)
            {
                if (current != last)
                {
                    //Debug.LogFormat("Update Replay: Current = {0}, Next = {1}, Update = {2}, Delta = {3}", current.SequenceID, next.SequenceID, replayUpdate, t);

                    // Check for current
                    if (replaySimulate == false && current != null && current != next)
                        scene.RestoreSnapshot(current, storage, false, true);

                    // Restore frame and send update events
                    scene.RestoreSnapshot(next, storage, replaySimulate);
                }
            }

            // Check if the update event should be triggered
            if (replayUpdate == true)
            {
                // Trigger update event
                ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, t);
            }
        }

        /// <summary>
        /// Dispose this replay operation.
        /// This will cause the playback to be stopped and this playback operation should no longer be used.
        /// </summary>
        public override void Dispose()
        {
            // Stop playback
            if (isDisposed == false)
                StopPlayback();
        }

        #region ReplayAPI
        internal void BeginPlayback(RestoreSceneMode restoreReplayScene)
        {
            this.restoreMode = restoreReplayScene;

            // Lock playback scene (Important- Only 1 playback operation can affect a replay object at any time)
            scene.BeginPlaybackOperation(this);

            // Prepare the target for writing operations
            storage.Prepare(ReplayStorageAction.Read);

            // Prepare for playback
            scene.SetReplaySceneMode(ReplaySceneMode.Playback, storage);

            // Trigger start events
            ReplayBehaviour.InvokeReplayStartEvent(scene.ActiveReplayBehaviours);

            // Restore first playback frame
            RestoreInitialFrame();
        }

        internal void RestoreInitialFrame()
        {
            // Get first snapshot
            ReplaySnapshot initial = storage.FetchSnapshot(1);

            // Check for valid
            if (initial == null)
            {
                Debug.LogWarning("Replay storage does not contain any data");
                return;
            }

            last = initial;

            // Restore the snapshot
            //scene.RestoreSnapshot(initial, storage);
            //current = initial;
            //// Trigger update
            //ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, default);
            ReplayPerformSceneUpdate(null, null, initial, 1f, true);
        }

        /// <summary>
        /// Pause the current playback operation.
        /// Playback will not be updated but all associated replay objects will remain in playback mode.
        /// </summary>
        public void PausePlayback()
        {
            // Check for disposed
            CheckDisposed();

            // Pause playback
            paused = true;
        }

        /// <summary>
        /// Resume the current playback operation.
        /// The replay will carry on from the point at which it was paused. 
        /// </summary>
        public void ResumePlayback()
        {
            // Check for disposed
            CheckDisposed();

            // Resume playback
            paused = false;
        }

        private void StopPlaybackEndEvent()
        {
            // Release resources
            StopPlayback();

            // Trigger end event
            OnPlaybackEnd.Invoke();
        }

        /// <summary>
        /// Stop this playback operation.
        /// Playback will stop and this operation will be disposed so should no longed be used after this call.
        /// </summary>
        public void StopPlayback()
        {
            // Check for disposed
            if (isDisposed == true)
                return;

            // Switch to live mode and possible restore the scene state to before playback
            scene.SetReplaySceneMode(ReplaySceneMode.Live, storage, restoreMode);

            // Finalize storage
            storage.Unlock(this);


            // Trigger events
            ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, 1f);
            ReplayBehaviour.InvokeReplayResetEvent(scene.ActiveReplayBehaviours);

            // Trigger end event
            ReplayBehaviour.InvokeReplayEndEvent(scene.ActiveReplayBehaviours);


            // Unregister playback operation
            scene.EndPlaybackOperation(this);

            // Unregister this operation
            ReplayManager.StopPlaybackOperation(this);

            // Mark as disposed
            isDisposed = true;
        }

        /// <summary>
        /// Stop this playback operation after the specified amount of second has passed.
        /// Playback will stop after the specified time and this operation will be disposed so should no longed be used after this call.
        /// </summary>
        /// <param name="delay">The amount of time in seconds to wait until the record operation is stopped</param>
        public void StopPlaybackDelayed(float delay)
        {
            // Check for disposed
            if (isDisposed == true)
                return;

            // Check for not supported
            if (manager == null)
                throw new NotSupportedException("Call is not supported in edit mode");

            // Unregister record operation
            scene.EndPlaybackOperation(this);

            // Start coroutine
            manager.StartCoroutine(StopPlaybackDelayedRoutine(delay));
        }

        private IEnumerator StopPlaybackDelayedRoutine(float delay)
        {
            // Wait for delay
            yield return new WaitForSeconds(delay);

            // Stop playback
            StopPlayback();
        }

        /// <summary>
        /// Jump to a new time stamp in the replay and update all replaying objects.
        /// The time stamp is specified in seconds and should usually be between 0 - <see cref="Duration"/>, although negative values are allowed when using relative seeking from the current time stamp.
        /// You can specify a relative time offset if you wanted to seek + or - 5 seconds for example using the <see cref="PlaybackOrigin"/> enum to specify the seek mode.
        /// Take a look at <see cref="SeekPlaybackNormalized(float, bool)"/> if you want to seek using a normalized value between 0-1.
        /// Seeking will be performed smoothly by default meaning that interpolation may occur between 2 snapshots since the input time stamp is unlikely to exactly match any given snapshot time stamp. This behaviour can be disabled if required so that seeking will snap to the nearest snapshot using <see cref="SeekSnap"/>.
        /// Seeking can mean that the replay will jump over many snapshots meaning that replay events and method may go uncalled during the seeking process which may or may not be desirable. You can force the replay system to trigger any such events or method calls that may have been missed using the <paramref name="simulateMissedFrames"/> parameter. Note though that enabling this option can be extremely performance intensive so is only recommended for smaller replays with few replay objects.
        /// </summary>
        /// <param name="time">The time in seconds to seek to, or to use as an offset depending upon the <paramref name="origin"/> value></param>
        /// <param name="origin">The origin where the seek should start from. Use <see cref="PlaybackOrigin.Current"/> if you want to seek + or - a few seconds for example</param>
        /// <param name="simulateMissedFrames">Should the missed frames between seek positions be simulated. NOT RECOMMENDED FOR LARGER REPLAYS</param>
        public void SeekPlayback(float time, PlaybackOrigin origin = PlaybackOrigin.Start, bool simulateMissedFrames = false)
        {
            // Check for disposed
            CheckDisposed();

            // Check for origin
            switch(origin)
            {
                case PlaybackOrigin.Start:
                    {
                        this.time = time;
                        break;
                    }

                case PlaybackOrigin.Current:
                    {
                        this.time += time;
                        break;
                    }

                case PlaybackOrigin.End:
                    {
                        this.time = Duration - time;
                        break;
                    }
            }

            // Constrain time
            this.time = Mathf.Clamp(this.time, 0f, Duration);

            // Update scene
            ReplaySeekUpdate(simulateMissedFrames);
        }

        public void SeekPlaybackNormalized(float timeNormalized, bool simulateMissedFrames = false)
        {
            // Check for disposed
            CheckDisposed();

            // Clamp 0-1
            timeNormalized = Mathf.Clamp01(timeNormalized);

            // Update time
            this.time = Mathf.Lerp(0f, Duration, timeNormalized);

            // Update scene
            ReplaySeekUpdate(simulateMissedFrames);
        }

        private void ReplaySeekUpdate(bool simulateMissedFrames)
        {
            // Reset last
            last = null;

            ReplaySnapshot current;
            ReplaySnapshot next;
            float t;

            // Fetch the current state from storage
            ReplayPlaybackFetchCurrentState(out current, out next, out t);

            if (simulateMissedFrames == true)
            {
                // Simulate the frames that may be missed
                // NOTE - This will be very performance intensive for larger replays so is not recommended
                ReplaySimulateMissedFrames(current, next);
            }

            // Check for seek behaviour
            if (playbackSeekSnap == PlaybackSeekSnap.SnapToFrame)
                t = 0f;

            // Update scene with playback - Note it is important to update twice so that interpolation can work well
            ReplayPerformSceneUpdate(null, current, next, t);
        }
        #endregion

        /// <summary>
        /// Throw an exception if this playback operation has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The replay operation was disposed</exception>
        protected override void CheckDisposed()
        {
            if (isDisposed == true)
                throw new ObjectDisposedException("Operation is not valid because the record operation has been stopped or disposed");
        }
    }
}
