/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/ReplayRecordOperation.cs
/// </summary>
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
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the <see cref = "ReplayUpdateMode"/> for this replay operation.
        /// This value determines at what stage in the Unity game loop the record operation is updated. 
        /// </summary>
        public override ReplayUpdateMode UpdateMode
        {
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get the <see cref = "ReplayRecordOptions"/> for this replay operation.
        /// </summary>
        public ReplayRecordOptions Options
        {
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns a value indicating whether recording is in progress and the recording is not currently paused.
        /// </summary>
        public bool IsRecording
        {
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns a value indicating whether the recording is currently paused.
        /// </summary>
        public bool IsRecordingPaused
        {
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns a value indicating whether recording is in progress or if the recording is currently paused.
        /// </summary>
        public bool IsRecordingOrPaused
        {
            get => throw new System.NotImplementedException();
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
            get => throw new System.NotImplementedException();
        }

        // Constructor
        internal ReplayRecordOperation(ReplayManager manager, ReplayScene scene, ReplayStorage storage, ReplayRecordOptions options): base(default, default, default) => throw new System.NotImplementedException();
        // Methods
        /// <summary>
        /// The main update call for this replay operation.
        /// Can be called manually if required, but if manually update is required then it is recommended to use <see cref = "ReplayManager.ReplayTick(float, ReplayUpdateMode)"/>.
        /// </summary>
        /// <param name = "delta">The amount of time in seconds that has passed since the last update</param>
        public override void ReplayTick(float delta) => throw new System.NotImplementedException();
        /// <summary>
        /// Dispose this replay operation.
        /// This will cause the recording to be stopped and this record operation should no longer be used.
        /// </summary>
        public override void Dispose() => throw new System.NotImplementedException();
        /// <summary>
        /// Pause the current record operation.
        /// No replay snapshots will be captured while recording is paused.
        /// </summary>
        public void PauseRecording() => throw new System.NotImplementedException();
        /// <summary>
        /// Resume the current record operation.
        /// The recording will carry on from the point at which it was paused. 
        /// </summary>
        public void ResumeRecording() => throw new System.NotImplementedException();
        /// <summary>
        /// Stop this record operation.
        /// Recording will stop and this operation will be disposed so should no longed be used after this call.
        /// </summary>
        public void StopRecording() => throw new System.NotImplementedException();
        /// <summary>
        /// Stop this record operation after the specified amount of second has passed.
        /// Recording will stop after the specified time and this operation will be disposed so should no longed be used after this call.
        /// </summary>
        /// <param name = "delay">The amount of time in seconds to wait until the record operation is stopped</param>
        public void StopRecordingDelayed(float delay) => throw new System.NotImplementedException();
        /// <summary>
        /// Throw an exception if this record operation has been disposed.
        /// </summary>
        /// <exception cref = "ObjectDisposedException">The replay operation was disposed</exception>
        protected override void CheckDisposed() => throw new System.NotImplementedException();
    }
}