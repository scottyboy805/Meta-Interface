using System;
using System.Collections;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Represents a dedicated record operation in progress.
    /// Provides access to all recording related API's for a specific record operation.
    /// </summary>
    public sealed class ReplayRecordOperation : ReplayOperation
    {
        // Private
        private bool isDisposed = false;
        private ReplayRecordOptions options = null;
        private float time = 0f;
        private bool paused = false;
        private int recordSequenceId = 1;

        private float serviceTimer = 0f;

        // Public
        /// <summary>
        /// The default record fps rate.
        /// </summary>
        public const float defaultRecordRate = 8;

        // Properties
        /// <summary>
        /// Check if this record operation has been disposed.
        /// A record operation becomes disposed when recording has been stopped, at which point the API becomes unusable.
        /// </summary>
        public override bool IsDisposed
        {
            get { return isDisposed; }
        }

        /// <summary>
        /// Get the <see cref="ReplayUpdateMode"/> for this replay operation.
        /// This value determines at what stage in the Unity game loop the record operation is updated. 
        /// </summary>
        public override ReplayUpdateMode UpdateMode
        {
            get
            {
                CheckDisposed();
                return options.recordUpdateMode;
            }
        }

        /// <summary>
        /// Get the <see cref="ReplayRecordOptions"/> for this replay operation.
        /// </summary>
        public ReplayRecordOptions Options
        {
            get 
            {
                CheckDisposed();
                return options; 
            }
        }

        /// <summary>
        /// Returns a value indicating whether recording is in progress and the recording is not currently paused.
        /// </summary>
        public bool IsRecording
        {
            get 
            {
                CheckDisposed();
                return isDisposed == false && paused == false; 
            }
        }

        /// <summary>
        /// Returns a value indicating whether the recording is currently paused.
        /// </summary>
        public bool IsRecordingPaused
        {
            get 
            {
                CheckDisposed();
                return paused; 
            }
        }

        /// <summary>
        /// Returns a value indicating whether recording is in progress or if the recording is currently paused.
        /// </summary>
        public bool IsRecordingOrPaused
        {
            get
            {
                CheckDisposed();
                return isDisposed == false;
            }
        }

        /// <summary>
        /// The target number of snapshot frames that will be recorded per second.
        /// Higher rates will allow for smooth and more accurate replay, but may have an additional performance hit.
        /// The replay system will not be able to record faster than your current frame rate so there is no benefit in setting a value of '90' for example if you game will only run at 60 fps.
        /// Set this value to negative and the record operation will run as fast as possible.
        /// When interpolation is used, record rates of '16' or much lower can be possible depending upon the particular game, which can save on storage space and performance.
        /// </summary>
        public float RecordRate
        {
            get
            {
                CheckDisposed();
                return options.recordFPS;
            }
        }


        // Constructor
        internal ReplayRecordOperation(ReplayManager manager, ReplayScene scene, ReplayStorage storage, ReplayRecordOptions options)
            : base(manager, scene, storage)
        {
            this.options = options;

            // Update record rate

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

            // Update service time
            serviceTimer += delta;
            

            // Check for paused
            if(paused == true)
                return;

            // Update time
            time += delta;

            // Check for elapsed
            if (serviceTimer >= options.recordInterval)// recordInterval)
            {
                // Reset timer
                serviceTimer = 0f;

                // Update replay recording
                ReplayRecordUpdate(time);
            }
        }

        private void ReplayRecordUpdate(float time)
        {
            // Submit capture event
            ReplayBehaviour.InvokeReplayCaptureEvent(scene.ActiveReplayBehaviours);

            // Get the scene state
            ReplaySnapshot recordSnapshot = scene.CaptureSnapshot(time, recordSequenceId, storage.PersistentData);

            // Record the snapshot in storage
            storage.StoreSnapshot(recordSnapshot);

            // Update sequence id
            recordSequenceId++;
        }

        /// <summary>
        /// Dispose this replay operation.
        /// This will cause the recording to be stopped and this record operation should no longer be used.
        /// </summary>
        public override void Dispose()
        {
            // Stop recording
            if (isDisposed == false)
                StopRecording();
        }

        #region ReplayAPI
        internal void BeginRecording(bool cleanRecording)
        {
            // Register record operation
            scene.BeginRecordOperation(this);

            // Clear storage if required
            if (cleanRecording == true)
                storage.Prepare(ReplayStorageAction.Discard);

            // Prepare the target for writing operations
            storage.Prepare(ReplayStorageAction.Write);

            // Update metadata
            if (storage.Metadata != null)
                storage.Metadata.UpdateMetadata();

            // Switch scene mode
            scene.SetReplaySceneMode(ReplaySceneMode.Record, storage);


            // Everything is now setup and we can record the very first frame
            RecordInitialFrame();
        }

        internal void RecordInitialFrame()
        {
            if(recordSequenceId == 1)
            {
                // Send capture event
                ReplayBehaviour.InvokeReplayCaptureEvent(scene.ActiveReplayBehaviours);

                // Create the initial snapshot
                ReplaySnapshot initialSnapshot = scene.CaptureSnapshot(0f, recordSequenceId, storage.PersistentData);

                // Record to storage
                storage.StoreSnapshot(initialSnapshot);

                // Ensure validity
                recordSequenceId++;
            }
        }

        /// <summary>
        /// Pause the current record operation.
        /// No replay snapshots will be captured while recording is paused.
        /// </summary>
        public void PauseRecording()
        {
            // Check for disposed
            CheckDisposed();

            // Set paused
            paused = true;
        }

        /// <summary>
        /// Resume the current record operation.
        /// The recording will carry on from the point at which it was paused. 
        /// </summary>
        public void ResumeRecording()
        {
            // Check for disposed
            CheckDisposed();

            // Resume
            paused = false;
        }

        /// <summary>
        /// Stop this record operation.
        /// Recording will stop and this operation will be disposed so should no longed be used after this call.
        /// </summary>
        public void StopRecording()
        {
            // Check for disposed
            if (isDisposed == true)
                return;

            // Switch to live scene mode
            scene.SetReplaySceneMode(ReplaySceneMode.Live, storage);

            // Unregister record operation
            scene.EndRecordOperation(this);

            // Finalize storage target
            storage.Prepare(ReplayStorageAction.Commit);
            storage.Unlock(this);            

            // Unregister this operation
            ReplayManager.StopRecordingOperation(this);

            // Mark as disposed
            isDisposed = true;
        }

        /// <summary>
        /// Stop this record operation after the specified amount of second has passed.
        /// Recording will stop after the specified time and this operation will be disposed so should no longed be used after this call.
        /// </summary>
        /// <param name="delay">The amount of time in seconds to wait until the record operation is stopped</param>
        public void StopRecordingDelayed(float delay)
        {
            // Check for disposed
            if (isDisposed == true)
                return;

            // Check for not supported
            if (manager == null)
                throw new NotSupportedException("Call is not supported in edit mode");

            // Start coroutine
            manager.StartCoroutine(StopRecordingDelayedRoutine(delay));
        }

        private IEnumerator StopRecordingDelayedRoutine(float delay)
        {
            // Wait for delay
            yield return new WaitForSeconds(delay);

            // Stop playback
            StopRecording();
        }
        #endregion

        /// <summary>
        /// Throw an exception if this record operation has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The replay operation was disposed</exception>
        protected override void CheckDisposed()
        {
            if(isDisposed == true)
                throw new ObjectDisposedException("Operation is not valid because the record operation has been stopped or disposed");
        }
    }
}
