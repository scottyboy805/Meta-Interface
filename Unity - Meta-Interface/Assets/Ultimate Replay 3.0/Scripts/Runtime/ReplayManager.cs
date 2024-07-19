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
        // Private
        private static readonly ArgumentException disposedHandleException = new ArgumentException("The specified replay handle is not valid");
        private static readonly InvalidOperationException invalidHandleException = new InvalidOperationException("Invalid replay handle. The handle is not valid for this operation handle");
        private static readonly InvalidOperationException emptySceneException = new InvalidOperationException("The specified replay scene does not contains any replay objects");

        private static ReplayManager managerInstance = null;
        private static ReplaySettings settings = null;
        private static List<ReplayPlaybackOperation> playbackOperations = new List<ReplayPlaybackOperation>();
        private static List<ReplayRecordOperation> recordOperations = new List<ReplayRecordOperation>();
        private static Queue<ReplayOperation> stopOperations = new Queue<ReplayOperation>();
        private static Queue<Action> replayLateCall = new Queue<Action>();

        // Public
        /// <summary>
        /// Should manual state update be enabled?
        /// If you set this value to true, you will then be responsible for update all replay and record operations by manually calling <see cref="UpdateState(float)"/>.
        /// </summary>
        public static bool manualStateUpdate = false;

        // Properties
        /// <summary>
        /// Get or load the replay settings from the current project.
        /// This is the replay settings editable via `Tools -> Ultimate Replay 3.0 -> Settings` and can be edited from code if required.
        /// </summary>
        public static ReplaySettings Settings
        {
            get 
            { 
                // Load settings
                if(settings == null)
                {
                    // Try to load
                    settings = Resources.Load<ReplaySettings>("UltimateReplaySettings");

                    // Check for error
                    if(settings == null)
                    {
                        Debug.LogWarning("Ultimate Replay settings could not be loaded, using defaults!");
                        settings = ScriptableObject.CreateInstance<ReplaySettings>();
                    }
                }

                return settings; 
            }
        }

        /// <summary>
        /// Returns a value indicating if one or more recording operations are running.
        /// </summary>
        public static bool IsRecordingAny
        {
            get { return recordOperations.Count > 0; }
        }

        /// <summary>
        /// Returns a value indicating if one or more replay operations are running.
        /// </summary>
        public static bool IsReplayingAny
        {
            get { return playbackOperations.Count > 0; }
        }

        // Methods
        #region UnityCallbacks
        private void Start()
        {
#if ULTIMATEREPLAY_TRIAL
            // Required to keep the editor reference
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        
        private void Update()
        {
            if (manualStateUpdate == false)
            {
                // Update all replay services
                ReplayTick(Time.deltaTime, ReplayUpdateMode.Update);
            }
        }

        private void LateUpdate()
        {
            if (manualStateUpdate == false)
            {
                // Update all replay services
                ReplayTick(Time.deltaTime, ReplayUpdateMode.LateUpdate);
            }
        }

        private void FixedUpdate()
        {
            if (manualStateUpdate == false)
            {
                // Update all replay services
                ReplayTick(Time.fixedDeltaTime, ReplayUpdateMode.FixedUpdate);
            }
        }

        private void OnDestroy()
        {
            // Remove static registered instance
            if (managerInstance == this)
            {
                recordOperations.Clear();
                playbackOperations.Clear();
                stopOperations.Clear();
                replayLateCall.Clear();

                managerInstance = null;
            }

            // Release any undisposed resources
            ReplayCleanupUtility.CleanupUnreleasedResources();
        }
        #endregion

        internal static void ReplayLateCallEvent(Action action)
        {
            replayLateCall.Enqueue(action);
        }

        /// <summary>
        /// Update all running replay services using the specified delta time.
        /// </summary>
        /// <param name="updateMethod">The update method used to update all services. Some services will require a specific update method to run</param>
        /// <param name="deltaTime">The amount of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public static void ReplayTick(float deltaTime, ReplayUpdateMode updateMode = ReplayUpdateMode.Manual)
        {
            // Only update if time has increased
            if (deltaTime <= 0)
                return;

            // Process all playback operations first
            foreach(ReplayPlaybackOperation playback in playbackOperations)
            {
                switch(updateMode)
                {
                    case ReplayUpdateMode.Manual:
                        {
                            playback.ReplayTick(deltaTime);
                            break;
                        }

                    case ReplayUpdateMode.Update:
                        {
                            playback.ReplayTickUpdate(deltaTime);
                            break;
                        }
                    case ReplayUpdateMode.LateUpdate:
                        {
                            playback.ReplayTickLateUpdate(deltaTime);
                            break;
                        }

                    case ReplayUpdateMode.FixedUpdate:
                        {
                            playback.ReplayTickFixedUpdate(deltaTime);
                            break;
                        }
                }
            }

            // Process all record operations
            foreach(ReplayRecordOperation record in recordOperations)
            {
                switch(updateMode)
                {
                    case ReplayUpdateMode.Manual:
                        {
                            record.ReplayTick(deltaTime);
                            break;
                        }

                    case ReplayUpdateMode.Update:
                        {
                            record.ReplayTickUpdate(deltaTime);
                            break;
                        }
                    case ReplayUpdateMode.LateUpdate:
                        {
                            record.ReplayTickLateUpdate(deltaTime);
                            break;
                        }

                    case ReplayUpdateMode.FixedUpdate:
                        {
                            record.ReplayTickFixedUpdate(deltaTime);
                            break;
                        }
                }
            }

            // Check for playback stopped services
            if (updateMode == ReplayUpdateMode.Update)
            {
                while(replayLateCall.Count > 0)
                {
                    try
                    {
                        // Invoke the delegate
                        replayLateCall.Dequeue()();
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        /// <summary>
        /// Start a new recording operation capturing only the specified replay object with the specified parameters.
        /// </summary>
        /// <param name="storage">The <see cref="ReplayStorage"/> that replay data should be saved to</param>
        /// <param name="recordObject">The <see cref="ReplayObject"/> that should be sampled during recording</param>
        /// <param name="cleanRecording">Should the recording start from scratch</param>
        /// <param name="recordOptions">The <see cref="ReplayRecordOptions"/> used to control the record behaviour. Pass null if the global record options should be used</param>
        /// <returns>A <see cref="ReplayRecordOperation"/> object that allows control over the new recording operation</returns>
        /// <exception cref="ArgumentNullException">The specified replay storage is null</exception>
        /// <exception cref="ArgumentNullException">The specified replay object is null</exception>
        /// <exception cref="AccessViolationException">The specified storage target is in use by another replay operation</exception>
        /// <exception cref="NotSupportedException">The specified storage is not writable</exception>
        public static ReplayRecordOperation BeginRecording(ReplayStorage storage, ReplayObject recordObject, bool cleanRecording = true, ReplayRecordOptions recordOptions = null)
        {
            // Check for no object
            if (recordObject == null)
                throw new ArgumentNullException(nameof(recordObject));

            // Create the replay scene - Note: preparer is not required for record mode, only for playback
            ReplayScene scene = new ReplayScene(recordObject);

            // Start recording
            return BeginRecording(storage, scene, cleanRecording, recordOptions);
        }

        /// <summary>
        /// Start a new recording operation with the specified parameters.
        /// </summary>
        /// <param name="recordTarget">The <see cref="ReplayStorage"/> that replay data should be saved to</param>
        /// <param name="recordScene">The <see cref="ReplayScene"/> that should be sampled during recording. Pass null to use all <see cref="ReplayObject"/> in the active unity scene</param>
        /// <param name="cleanRecording">Should the recording start from scratch</param>
        /// <param name="recordOptions">The <see cref="ReplayRecordOptions"/> used to control the record behaviour. Pass null if the global record options should be used</param>
        /// <returns>A <see cref="ReplayRecordOperation"/> object that allows control over the new recording operation</returns>
        /// <exception cref="ArgumentNullException">The specified replay storage is null</exception>
        /// <exception cref="AccessViolationException">The specified storage target is in use by another replay operation</exception>
        /// <exception cref="NotSupportedException">The specified storage is not writable</exception>
        public static ReplayRecordOperation BeginRecording(ReplayStorage storage, ReplayScene recordScene = null, bool cleanRecording = true, ReplayRecordOptions recordOptions = null)
        {
            // Check for null storage
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            // Check for default scene
            if (recordScene == null)
            {
                recordScene = Settings.SceneDiscovery == ReplaySceneDiscovery.ActiveScene
                    ? ReplayScene.FromCurrentScene()
                    : ReplayScene.FromAllScenes();
            }

            // Check scene integrity
            try
            {
                recordScene.CheckIntegrity(true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // Check for not supported or locked
            if (storage.CanWrite == false) 
                throw new NotSupportedException("The specified storage is not writable and cannot be used for recording");

            // Check for valid record options
            if (recordOptions == null)
                recordOptions = Settings.recordOptions;

            // Make sure a manager component is running
            ForceAwake();

            // Create the operation
            ReplayRecordOperation record = new ReplayRecordOperation(managerInstance, recordScene, storage, recordOptions);
            recordOperations.Add(record);

            // Check for locked
            try
            {
                storage.Lock(record);
            }
            catch(AccessViolationException)
            {
                throw new AccessViolationException("The specified storage target is in use by another replay operation");
            }

            // Every recording should start with frame: 0, timestamp: 0
            record.BeginRecording(cleanRecording);            

            // Return the record operation
            return record;
        }
                
        internal static void StopRecordingOperation(ReplayRecordOperation record)
        {
            // Remove from collection
            if (recordOperations.Contains(record) == true)
                recordOperations.Remove(record);
        }

        public static ReplayPlaybackOperation BeginPlayback(ReplayStorage storage, ReplayObject playbackObject, IReplayPreparer preparer = null, ReplayPlaybackOptions playbackOptions = null, RestoreSceneMode restoreSceneMode = RestoreSceneMode.RestoreState)
        {
            // Check for null
            if(playbackObject == null)
                throw new ArgumentNullException(nameof(playbackObject));

            // Create the replay scene
            ReplayScene scene = new ReplayScene(playbackObject, preparer);

            // Start playback
            return BeginPlayback(storage, scene, playbackOptions, restoreSceneMode);
        }

        /// <summary>
        /// Start a new playback operation with the specified parameters.
        /// The recorded data from the specified storage will be replayed onto the specified `playbackObject` and the `recordedObject` must be provided
        /// </summary>
        /// <param name="storage">The storage where the replay is stored</param>
        /// <param name="recordedObject">The source object that was originally recorded, used as the source object to clone the replay identities from</param>
        /// <param name="playbackObject">The target object that should be replayed which is not the original recorded object. This object must have an identical replay component structure as the recorded object so that replay id's can be cloned</param>
        /// <param name="preparer">The preparer that should be used to prepare the object for playback. Use null to use the default provider</param>
        /// <param name="playbackOptions">The <see cref="ReplayPlaybackOptions"/> used to control the record behaviour. Pass null if the global record options should be used</param>
        /// <param name="restoreSceneMode">Should the replay objects be restored to their original state before the replay started, or should the replay objects continue from their current replay positions</param>
        /// <returns>A playback operation that can manage the new replay</returns>
        /// <exception cref="ArgumentNullException">Storage, recorded object or playback object is null</exception>
        /// <exception cref="InvalidOperationException">The replay identity could not be cloned from the source onto the playback object due to incompatible replay components used</exception>
        public static ReplayPlaybackOperation BeginPlayback(ReplayStorage storage, ReplayObject recordedObject, ReplayObject playbackObject, IReplayPreparer preparer = null, ReplayPlaybackOptions playbackOptions = null, RestoreSceneMode restoreSceneMode = RestoreSceneMode.RestoreState)
        {
            // Check for null
            if(recordedObject == null)
                throw new ArgumentNullException(nameof(recordedObject));

            if (playbackObject == null)
                throw new ArgumentNullException(nameof(playbackObject));

            // Try to clone the replay identities
            if (ReplayObject.CloneReplayObjectIdentity(recordedObject, playbackObject) == false)
                throw new InvalidOperationException("Playback object is not compatible with the recorded object. Make sure both replay objects use the same replay components!");

            // Create the replay scene
            ReplayScene scene = new ReplayScene(playbackObject, preparer);

            // Start playback
            return BeginPlayback(storage, scene, playbackOptions, restoreSceneMode);
        }

        /// <summary>
        /// Start a new playback operation with the specified parameters.
        /// </summary>
        /// <param name="storage">The storage where the replay is stored</param>
        /// <param name="playbackScene"></param>
        /// <param name="playbackOptions"></param>
        /// <param name="restoreReplayScene">Should the replay objects be restored to their original state before the replay started, or should the replay objects continue from their current replay positions</param>
        /// <returns>A playback operation that can manage the new replay</returns>
        /// <exception cref="ArgumentNullException">Storage or playback scene is null</exception>
        /// <exception cref="NotSupportedException">The specified storage does not support read operations</exception>
        public static ReplayPlaybackOperation BeginPlayback(ReplayStorage storage, ReplayScene playbackScene = null, ReplayPlaybackOptions playbackOptions = null, RestoreSceneMode restoreReplayScene = RestoreSceneMode.RestoreState)
        {
            // Check for null storage
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            // Check for default scene required
            if (playbackScene == null)
            {
                playbackScene = Settings.SceneDiscovery == ReplaySceneDiscovery.ActiveScene
                    ? ReplayScene.FromCurrentScene()
                    : ReplayScene.FromAllScenes();
            }

            // Check scene integrity
            try
            {
                playbackScene.CheckIntegrity(true);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }

            // Check for not supported or locked
            if (storage.CanRead == false) 
                throw new NotSupportedException("The specified replay storage is not readable and cannot be used for replaying");

            // Make sure options are valid
            if (playbackOptions == null)
                playbackOptions = Settings.playbackOptions;

            // Make sure a manager component is running
            ForceAwake();

            // Create the service
            ReplayPlaybackOperation playback = new ReplayPlaybackOperation(managerInstance, playbackScene, storage, playbackOptions);
            playbackOperations.Add(playback);

            // Trigger start events
            ReplayBehaviour.InvokeReplayStartEvent(playbackScene.ActiveReplayBehaviours);

            // Begin the replay operation
            playback.BeginPlayback(restoreReplayScene);

            return playback;
        }
                
        public static void StopPlaybackOperation(ReplayPlaybackOperation playback)
        {
            if (playbackOperations.Contains(playback) == true)
                playbackOperations.Remove(playback);
        }

        public static void AddReplayObjectToRecordScenes(GameObject gameObject)
        {
            // Check for null
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Add to scene
            AddReplayObjectToRecordScenes(obj);
        }

        public static void AddReplayObjectToRecordScenes(ReplayObject replayObject)
        {
            // Check for null
            if (replayObject == null) throw new ArgumentNullException(nameof(replayObject));

            // Process all record operations
            foreach(ReplayRecordOperation record in recordOperations)
            {
                record.Scene.AddReplayObject(replayObject);
            }
        }

        public static void AddReplayObjectToRecordOperation(ReplayRecordOperation recordOperation, GameObject gameObject)
        {
            // Check for disposed and null
            if (recordOperation == null) throw new ArgumentNullException(nameof(recordOperation));
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Call through
            AddReplayObjectToRecordOperation(recordOperation, obj);
        }

        public static void AddReplayObjectToRecordOperation(ReplayRecordOperation recordOperation, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (recordOperation == null) throw new ArgumentNullException(nameof(recordOperation));
            if (replayObject == null) throw new ArgumentNullException(nameof(replayObject));

            // Add to operation
            recordOperation.Scene.AddReplayObject(replayObject);
        }

        public static void AddReplayObjectToPlaybackScenes(GameObject gameObject)
        {
            // Check for null
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Add to scene
            AddReplayObjectToPlaybackScenes(obj);
        }

        public static void AddReplayObjectToPlaybackScenes(ReplayObject playbackObject)
        {
            // Check for null
            if (playbackObject == null) throw new ArgumentNullException(nameof(playbackObject));

            // Process all playback operations
            foreach(ReplayPlaybackOperation playback in playbackOperations)
            {
                playback.Scene.AddReplayObject(playbackObject);
            }
        }

        public static void AddReplayObjectToPlaybackOperation(ReplayPlaybackOperation playbackOperation, GameObject gameObject)
        {
            // Check for disposed and null
            if (playbackOperation == null) throw new ArgumentNullException(nameof(playbackOperation));
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Call through
            AddReplayObjectToPlaybackOperation(playbackOperation, obj);
        }

        public static void AddReplayObjectToPlaybackOperation(ReplayPlaybackOperation playbackOperation, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (playbackOperation == null) throw new ArgumentNullException(nameof(playbackOperation));
            if (replayObject == null) throw new ArgumentNullException(nameof(replayObject));

            // Add to operation
            playbackOperation.Scene.AddReplayObject(replayObject);
        }

        public static void RemoveReplayObjectFromRecordScenes(GameObject gameObject)
        {
            // Check for null
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Add to scene
            RemoveReplayObjectFromRecordScenes(obj);
        }

        public static void RemoveReplayObjectFromRecordScenes(ReplayObject replayObject)
        {
            // Check for null
            if (replayObject == null) throw new ArgumentNullException(nameof(replayObject), "You should remove the specified object before it is destroyed");

            // Remove from record
            foreach(ReplayRecordOperation record in recordOperations)
            {
                record.Scene.RemoveReplayObject(replayObject);
            }
        }

        public static void RemoveReplayObjectFromRecordOperation(ReplayRecordOperation recordOperation, GameObject gameObject)
        {
            // Check for disposed and null
            if (recordOperation == null) throw new ArgumentNullException(nameof(recordOperation));
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Call through
            RemoveReplayObjectFromRecordOperation(recordOperation, obj);
        }

        public static void RemoveReplayObjectFromRecordOperation(ReplayRecordOperation recordOperation, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (recordOperation == null) throw new ArgumentNullException(nameof(recordOperation));
            if (replayObject == null) throw new ArgumentNullException(nameof(replayObject), "You should remove the specified object before it is destroyed");

            // Remove from operation
            recordOperation.Scene.RemoveReplayObject(replayObject);
        }

        public static void RemoveReplayObjectFromPlaybackScenes(GameObject gameObject)
        {
            // Check for null
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Add to scene
            RemoveReplayObjectFromPlaybackScenes(obj);
        }

        public static void RemoveReplayObjectFromPlaybackScenes(ReplayObject playbackObject)
        {
            // Check for null
            if (playbackObject == null) throw new ArgumentNullException(nameof(playbackObject), "You should remove the specified object before it is destroyed");

            // Remove from playback
            foreach (ReplayPlaybackOperation playback in playbackOperations)
            {
                playback.Scene.RemoveReplayObject(playbackObject);
            }
        }

        public static void RemoveReplayObjectFromPlaybackOperation(ReplayPlaybackOperation playbackOperation, GameObject gameObject)
        {
            // Check for disposed and null
            if (playbackOperation == null) throw new ArgumentNullException(nameof(playbackOperation));
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Call through
            RemoveReplayObjectFromPlaybackOperation(playbackOperation, obj);
        }

        public static void RemoveReplayObjectFromPlaybackOperation(ReplayPlaybackOperation playbackOperation, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (playbackOperation == null) throw new ArgumentNullException(nameof(playbackOperation));
            if (replayObject == null) throw new ArgumentNullException(nameof(replayObject), "You should remove the specified object before it is destroyed");

            // Remove from operation
            playbackOperation.Scene.RemoveReplayObject(replayObject);
        }

        public static void ForceAwake()
        {
            if(managerInstance == null && Application.isPlaying == true)
            {
                GameObject go = new GameObject(nameof(ReplayManager));
                managerInstance = go.AddComponent<ReplayManager>();

                DontDestroyOnLoad(go);
            }
        }
        
        public static ReplayObjectLifecycleProvider AddReplayPrefabAssetProvider(ReplayObject replayPrefab)
        {
            // Check for null
            if (replayPrefab == null)
                throw new ArgumentNullException(nameof(replayPrefab));

            // Create prefab provider
            ReplayObjectDefaultLifecycleProvider provider = ScriptableObject.CreateInstance<ReplayObjectDefaultLifecycleProvider>();

            // Setup prefab
            provider.replayPrefab = replayPrefab;

            // Add custom provider
            return AddReplayPrefabCustomProvider(provider);
        }

        public static ReplayObjectLifecycleProvider AddReplayPrefabResourcesProvider(ReplayIdentity prefabID, string resourcesPath)
        {
            // Check for invalid
            if (prefabID.IsValid == false)
                throw new ArgumentException("Prefab ID is not valid");

            // Check for path
            if (string.IsNullOrEmpty(resourcesPath) == true)
                throw new ArgumentException("Resources path is null or empty");

            // Create the resources provider
            ReplayObjectResourcesLifecycleProvider provider = ScriptableObject.CreateInstance<ReplayObjectResourcesLifecycleProvider>();

            // Setup resources
            provider.resourcesPath = resourcesPath;

            // Add custom provider
            return AddReplayPrefabCustomProvider(provider);
        }

        public static ReplayObjectLifecycleProvider AddReplayPrefabCustomProvider(ReplayObjectLifecycleProvider provider)
        {
            // Check for null
            if(provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Register provider - May throw exception
            Settings.AddPrefabProvider(provider);

            return provider;
        }

        public static void RemoveReplayPrefabProvider(ReplayObjectLifecycleProvider provider)
        {
            // Check for null
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Remove provider
            Settings.RemovePrefabProvider(provider);
        }
    }
}
