/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/ReplayManager.cs
/// </summary>
using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateReplay.Storage;
using System.Runtime.CompilerServices;
using UltimateReplay.StatePreparation;
using UltimateReplay.Lifecycle;

[assembly: InternalsVisibleTo("UltimateReplayStudio.Editor")]
[assembly: InternalsVisibleTo("UltimateReplay.Editor")]
namespace UltimateReplay
{
    /// <summary>
    /// The main interface for Ultimate Replay and allows full control over object recording and playback.
    /// </summary>
    public sealed class ReplayManager : MonoBehaviour
    {
        // Public
        /// <summary>
        /// Should manual state update be enabled?
        /// If you set this value to true, you will then be responsible for update all replay and record operations by manually calling <see cref = "UpdateState(float)"/>.
        /// </summary>
        public static bool manualStateUpdate = false;
        // Properties
        /// <summary>
        /// Get or load the replay settings from the current project.
        /// This is the replay settings editable via `Tools -> Ultimate Replay 3.0 -> Settings` and can be edited from code if required.
        /// </summary>
        public static ReplaySettings Settings
        {
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns a value indicating if one or more recording operations are running.
        /// </summary>
        public static bool IsRecordingAny
        {
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns a value indicating if one or more replay operations are running.
        /// </summary>
        public static bool IsReplayingAny
        {
            get => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Update all running replay services using the specified delta time.
        /// </summary>
        /// <param name = "updateMethod">The update method used to update all services. Some services will require a specific update method to run</param>
        /// <param name = "deltaTime">The amount of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public static void ReplayTick(float deltaTime, ReplayUpdateMode updateMode = ReplayUpdateMode.Manual) => throw new System.NotImplementedException();
        /// <summary>
        /// Start a new recording operation capturing only the specified replay object with the specified parameters.
        /// </summary>
        /// <param name = "storage">The <see cref = "ReplayStorage"/> that replay data should be saved to</param>
        /// <param name = "recordObject">The <see cref = "ReplayObject"/> that should be sampled during recording</param>
        /// <param name = "cleanRecording">Should the recording start from scratch</param>
        /// <param name = "recordOptions">The <see cref = "ReplayRecordOptions"/> used to control the record behaviour. Pass null if the global record options should be used</param>
        /// <returns>A <see cref = "ReplayRecordOperation"/> object that allows control over the new recording operation</returns>
        /// <exception cref = "ArgumentNullException">The specified replay storage is null</exception>
        /// <exception cref = "ArgumentNullException">The specified replay object is null</exception>
        /// <exception cref = "AccessViolationException">The specified storage target is in use by another replay operation</exception>
        /// <exception cref = "NotSupportedException">The specified storage is not writable</exception>
        public static ReplayRecordOperation BeginRecording(ReplayStorage storage, ReplayObject recordObject, bool cleanRecording = true, ReplayRecordOptions recordOptions = null) => throw new System.NotImplementedException();
        /// <summary>
        /// Start a new recording operation with the specified parameters.
        /// </summary>
        /// <param name = "recordTarget">The <see cref = "ReplayStorage"/> that replay data should be saved to</param>
        /// <param name = "recordScene">The <see cref = "ReplayScene"/> that should be sampled during recording. Pass null to use all <see cref = "ReplayObject"/> in the active unity scene</param>
        /// <param name = "cleanRecording">Should the recording start from scratch</param>
        /// <param name = "recordOptions">The <see cref = "ReplayRecordOptions"/> used to control the record behaviour. Pass null if the global record options should be used</param>
        /// <returns>A <see cref = "ReplayRecordOperation"/> object that allows control over the new recording operation</returns>
        /// <exception cref = "ArgumentNullException">The specified replay storage is null</exception>
        /// <exception cref = "AccessViolationException">The specified storage target is in use by another replay operation</exception>
        /// <exception cref = "NotSupportedException">The specified storage is not writable</exception>
        public static ReplayRecordOperation BeginRecording(ReplayStorage storage, ReplayScene recordScene = null, bool cleanRecording = true, ReplayRecordOptions recordOptions = null) => throw new System.NotImplementedException();
        public static ReplayPlaybackOperation BeginPlayback(ReplayStorage storage, ReplayObject playbackObject, IReplayPreparer preparer = null, ReplayPlaybackOptions playbackOptions = null, RestoreSceneMode restoreSceneMode = RestoreSceneMode.RestoreState) => throw new System.NotImplementedException();
        /// <summary>
        /// Start a new playback operation with the specified parameters.
        /// The recorded data from the specified storage will be replayed onto the specified `playbackObject` and the `recordedObject` must be provided
        /// </summary>
        /// <param name = "storage">The storage where the replay is stored</param>
        /// <param name = "recordedObject">The source object that was originally recorded, used as the source object to clone the replay identities from</param>
        /// <param name = "playbackObject">The target object that should be replayed which is not the original recorded object. This object must have an identical replay component structure as the recorded object so that replay id's can be cloned</param>
        /// <param name = "preparer">The preparer that should be used to prepare the object for playback. Use null to use the default provider</param>
        /// <param name = "playbackOptions">The <see cref = "ReplayPlaybackOptions"/> used to control the record behaviour. Pass null if the global record options should be used</param>
        /// <param name = "restoreSceneMode">Should the replay objects be restored to their original state before the replay started, or should the replay objects continue from their current replay positions</param>
        /// <returns>A playback operation that can manage the new replay</returns>
        /// <exception cref = "ArgumentNullException">Storage, recorded object or playback object is null</exception>
        /// <exception cref = "InvalidOperationException">The replay identity could not be cloned from the source onto the playback object due to incompatible replay components used</exception>
        public static ReplayPlaybackOperation BeginPlayback(ReplayStorage storage, ReplayObject recordedObject, ReplayObject playbackObject, IReplayPreparer preparer = null, ReplayPlaybackOptions playbackOptions = null, RestoreSceneMode restoreSceneMode = RestoreSceneMode.RestoreState) => throw new System.NotImplementedException();
        /// <summary>
        /// Start a new playback operation with the specified parameters.
        /// </summary>
        /// <param name = "storage">The storage where the replay is stored</param>
        /// <param name = "playbackScene"></param>
        /// <param name = "playbackOptions"></param>
        /// <param name = "restoreReplayScene">Should the replay objects be restored to their original state before the replay started, or should the replay objects continue from their current replay positions</param>
        /// <returns>A playback operation that can manage the new replay</returns>
        /// <exception cref = "ArgumentNullException">Storage or playback scene is null</exception>
        /// <exception cref = "NotSupportedException">The specified storage does not support read operations</exception>
        public static ReplayPlaybackOperation BeginPlayback(ReplayStorage storage, ReplayScene playbackScene = null, ReplayPlaybackOptions playbackOptions = null, RestoreSceneMode restoreReplayScene = RestoreSceneMode.RestoreState) => throw new System.NotImplementedException();
        public static void StopPlaybackOperation(ReplayPlaybackOperation playback) => throw new System.NotImplementedException();
        public static void AddReplayObjectToRecordScenes(GameObject gameObject) => throw new System.NotImplementedException();
        public static void AddReplayObjectToRecordScenes(ReplayObject replayObject) => throw new System.NotImplementedException();
        public static void AddReplayObjectToRecordOperation(ReplayRecordOperation recordOperation, GameObject gameObject) => throw new System.NotImplementedException();
        public static void AddReplayObjectToRecordOperation(ReplayRecordOperation recordOperation, ReplayObject replayObject) => throw new System.NotImplementedException();
        public static void AddReplayObjectToPlaybackScenes(GameObject gameObject) => throw new System.NotImplementedException();
        public static void AddReplayObjectToPlaybackScenes(ReplayObject playbackObject) => throw new System.NotImplementedException();
        public static void AddReplayObjectToPlaybackOperation(ReplayPlaybackOperation playbackOperation, GameObject gameObject) => throw new System.NotImplementedException();
        public static void AddReplayObjectToPlaybackOperation(ReplayPlaybackOperation playbackOperation, ReplayObject replayObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromRecordScenes(GameObject gameObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromRecordScenes(ReplayObject replayObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromRecordOperation(ReplayRecordOperation recordOperation, GameObject gameObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromRecordOperation(ReplayRecordOperation recordOperation, ReplayObject replayObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromPlaybackScenes(GameObject gameObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromPlaybackScenes(ReplayObject playbackObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromPlaybackOperation(ReplayPlaybackOperation playbackOperation, GameObject gameObject) => throw new System.NotImplementedException();
        public static void RemoveReplayObjectFromPlaybackOperation(ReplayPlaybackOperation playbackOperation, ReplayObject replayObject) => throw new System.NotImplementedException();
        public static void ForceAwake() => throw new System.NotImplementedException();
        public static ReplayObjectLifecycleProvider AddReplayPrefabAssetProvider(ReplayObject replayPrefab) => throw new System.NotImplementedException();
        public static ReplayObjectLifecycleProvider AddReplayPrefabResourcesProvider(ReplayIdentity prefabID, string resourcesPath) => throw new System.NotImplementedException();
        public static ReplayObjectLifecycleProvider AddReplayPrefabCustomProvider(ReplayObjectLifecycleProvider provider) => throw new System.NotImplementedException();
        public static void RemoveReplayPrefabProvider(ReplayObjectLifecycleProvider provider) => throw new System.NotImplementedException();
    }
}