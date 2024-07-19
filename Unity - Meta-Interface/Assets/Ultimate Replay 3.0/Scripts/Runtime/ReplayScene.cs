using System;
using System.Collections.Generic;
using System.Linq;
using UltimateReplay.StatePreparation;
using UltimateReplay.Storage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateReplay
{
    /// <summary>
    /// The scene state value used to determine which mode a particular scene instance is in.
    /// </summary>
    public enum ReplaySceneMode
    {
        /// <summary>
        /// The scene and all child objects are in live mode meaning gameplay can continue as normal.
        /// </summary>
        Live,
        /// <summary>
        /// The scene and all child objects are in playback mode. Objects in the scene should not be interfered with and will be updated frequently.
        /// </summary>
        Playback,
        /// <summary>
        /// The scene and all child objects are in record mode. Gameplay can continue but objects will be sampled frequently.
        /// </summary>
        Record,
    }

    /// <summary>
    /// The behaviour to use when detecting replay objects.
    /// </summary>
    public enum ReplaySceneDiscovery
    {
        /// <summary>
        /// Only include replay objects that are located in the current active Unity scene <see cref="SceneManager.SetActiveScene(Scene)"/>.
        /// </summary>
        ActiveScene,
        /// <summary>
        /// Include replay objects from all loaded Unity scenes.
        /// </summary>
        AllScenes,
    }

    /// <summary>
    /// A <see cref="ReplayScene"/> contains information about all active replay objects. 
    /// </summary>
    public sealed class ReplayScene
    {
        // Events
        /// <summary>
        /// Called when a replay object was added to this <see cref="ReplayScene"/>.
        /// </summary>
        public event Action<ReplayObject> OnReplayObjectAdded;

        /// <summary>
        /// Called when a replay object was removed from this <see cref="ReplayScene"/>.
        /// </summary>
        public event Action<ReplayObject> OnReplayObjectRemoved;

        // Private
        private static IReplayPreparer defaultReplayPreparer = null;
        private static List<ReplayBehaviour> sharedChildBehaviours = new List<ReplayBehaviour>();
        private static HashSet<ReplayIdentity> sharedDeadObjectIds = new HashSet<ReplayIdentity>();

        private IReplayPreparer replayPreparer = null;
        private ReplayPlaybackOperation playbackOperation = null;
        private List<ReplayRecordOperation> recordOperations = null;
        private HashSet<ReplayObject> replayObjects = new HashSet<ReplayObject>();
        private Dictionary<ReplayIdentity, ReplayObject> replayObjectCache = new Dictionary<ReplayIdentity, ReplayObject>();
        private HashSet<ReplayBehaviour> replayBehaviours = new HashSet<ReplayBehaviour>();
        private Queue<ReplayObject> dynamicReplayObjects = new Queue<ReplayObject>();
        private ReplaySnapshot prePlaybackState = null;
        private bool isPlayback = false;

        // Public        
        /// <summary>
        /// A value indicating whether the replay objects stored in this scene instance should be reverted to their initial state when playback ends.
        /// </summary>
        public bool restorePreviousSceneState = true;

        // Properties
        /// <summary>
        /// Enable or disable the replay scene in preparation for playback or live mode.
        /// When true, all replay objects will be prepared for playback causing certain components or scripts to be disabled to prevent interference from game systems.
        /// A prime candidate would be the RigidBody component which could cause a replay object to be affected by gravity and as a result deviate from its intended position.
        /// When false, all replay objects will be returned to their 'Live' state when all game systems will be reactivated.
        /// </summary>
        public bool ReplayEnabled
        {
            get { return isPlayback; }
        }
        
        /// <summary>
        /// Returns a value indicating whether the <see cref="ReplayScene"/> contains any <see cref="ReplayObject"/>.
        /// </summary>
        public bool IsEmpty
        {
            get { return replayObjects.Count == 0; }
        }

        /// <summary>
        /// Get a collection of all game objects that are registered with the replay system.
        /// </summary>
        public HashSet<ReplayObject> ActiveReplayObjects
        {
            get { return replayObjects; }
        }

        /// <summary>
        /// Get a collection of all <see cref="ReplayBehaviour"/> components that are registered in this <see cref="ReplayScene"/>.
        /// </summary>
        public HashSet<ReplayBehaviour> ActiveReplayBehaviours
        {
            get { return replayBehaviours; }
        }

        /// <summary>
        /// Create a new replay scene with no <see cref="ReplayObject"/> added.
        /// </summary>
        /// <param name="replayPreparer">A <see cref="IReplayPreparer"/> implementation used to prepare scene objects when switching between playback and live scene modes</param>
        public ReplayScene(IReplayPreparer replayPreparer = null)
        {
            // Create shared default preparer instance
            if(defaultReplayPreparer == null)
            {
                // Create instance
                defaultReplayPreparer = ReplayManager.Settings.DefaultReplayPreparer;
            }

            if (replayPreparer == null)
                replayPreparer = defaultReplayPreparer;

            this.replayPreparer = replayPreparer;
        }

        /// <summary>
        /// Create a new replay scene and add the specified replay object.
        /// </summary>
        /// <param name="replayObject">The single <see cref="ReplayObject"/> to add to the scene</param>
        /// <param name="replayPreparer">A <see cref="IReplayPreparer"/> implementation used to prepare scene objects when switching between playback and live scene modes</param>
        public ReplayScene(ReplayObject replayObject, IReplayPreparer replayPreparer = null)
        {
            // Parameter was null
            if (replayObject == null)
                throw new ArgumentNullException("replayObject");

            // Create shared default preparer instance
            if (defaultReplayPreparer == null)
            {
                // Create instance
                defaultReplayPreparer = ReplayManager.Settings.DefaultReplayPreparer;
            }

            if (replayPreparer == null)
                replayPreparer = defaultReplayPreparer;

            this.replayPreparer = replayPreparer;

            // Add object to scene
            AddReplayObject(replayObject);
        }

        /// <summary>
        /// Create a new replay scene from the specified collection or replay objects.
        /// </summary>
        /// <param name="replayObjects">A collection of <see cref="ReplayObject"/> that will be added to the scene</param>
        /// <param name="replayPreparer">A <see cref="IReplayPreparer"/> implementation used to prepare scene objects when switching between playback and live scene modes</param>
        public ReplayScene(IEnumerable<ReplayObject> replayObjects, IReplayPreparer replayPreparer = null)
        {
            // Create shared default preparer instance
            if (defaultReplayPreparer == null)
            {
                // Create instance
                defaultReplayPreparer = ReplayManager.Settings.DefaultReplayPreparer;
            }

            if (replayPreparer == null)
                replayPreparer = defaultReplayPreparer;

            this.replayPreparer = replayPreparer;

            foreach(ReplayObject obj in replayObjects)
            {
                // Only add if not null
                if (obj != null)
                    AddReplayObject(obj);
            }
        }

        // Methods
        /// <summary>
        /// Add the specified game object to the replay scene. Only game objects with a <see cref="ReplayObject"/> attached will be accepted.
        /// Replay objects must be added to a replay scene in order to be recorded or replayed by the replay system.
        /// </summary>
        /// <param name="gameObject">The target game object to add to the replay scene</param>
        /// <exception cref="ArgumentNullException">The specified game object is null</exception>
        /// <exception cref="InvalidOperationException">The specified game object does not have a <see cref="ReplayObject"/> attached</exception>
        public void AddReplayObject(GameObject gameObject)
        {
            // Check for null
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                throw new InvalidOperationException("gameObject does not has a ReplayObject component attached!");

            // Add to scene
            AddReplayObject(obj);
        }

        /// <summary>
        /// Registers a replay object with the replay system so that it can be recorded for playback.
        /// Typically all <see cref="ReplayObject"/> will auto register when they 'Awake' meaning that you will not need to manually register objects. 
        /// </summary>
        /// <param name="replayObject">The <see cref="ReplayObject"/> to register</param>
        /// <exception cref="ArgumentNullException">The specified game object is null</exception>
        public void AddReplayObject(ReplayObject replayObject)
        {
            // Check for null
            if (replayObject == null)
                throw new ArgumentNullException(nameof(replayObject));

            // Check for already added
            if (replayObjects.Contains(replayObject) == true)
                return;

            // Add the replay object
            replayObjects.Add(replayObject);
            replayObjectCache.Add(replayObject.ReplayIdentity, replayObject);

            // Check for disabled object
            if (replayObject.gameObject.activeInHierarchy == false || Application.isPlaying == false)
                replayObject.UpdateRuntimeComponents();

            // Trigger event
            if (OnReplayObjectAdded != null)
                OnReplayObjectAdded(replayObject);

            // Check if we are adding objects during playback
            if(isPlayback == true)
            {
                // We need to prepare the object for playback
                replayPreparer.PrepareForPlayback(replayObject);
            }
            // Check if we are adding objects during recording
            else
            {
                // The object was added during recording
                //if(replayObject.IsPrefab == true)
                    dynamicReplayObjects.Enqueue(replayObject);
            }

            // Update operations
            if (this.playbackOperation != null)
            {
                replayObject.BeginPlaybackOperation(this.playbackOperation);
            }

            if(this.recordOperations != null)
            {
                foreach (ReplayRecordOperation operation in this.recordOperations)
                    replayObject.BeginRecordOperation(operation);
            }

            // Find all child behaviours
            replayObject.GetComponentsInChildren(sharedChildBehaviours);

            // Register all behaviours
            for(int i = 0; i < sharedChildBehaviours.Count; i++)
                replayBehaviours.Add(sharedChildBehaviours[i]);

            // Clear shared collection
            sharedChildBehaviours.Clear();
        }

        /// <summary>
        /// Unregisters a replay object from this replay scene.
        /// </summary>
        /// <param name="replayObject">The <see cref="ReplayObject"/> to unregister</param>
        public void RemoveReplayObject(GameObject gameObject)
        {
            // Check for null
            if (gameObject == null)
                return;

            // Get component
            ReplayObject obj = gameObject.GetComponent<ReplayObject>();

            if (obj == null)
                return;

            // Add to scene
            RemoveReplayObject(obj);
        }

        /// <summary>
        /// Unregisters a replay object from the replay system so that it will no longer be recorded for playback.
        /// Typically all <see cref="ReplayObject"/> will auto un-register when they are destroyed so you will normally not need to un-register a replay object. 
        /// </summary>
        /// <param name="replayObject"></param>
        public void RemoveReplayObject(ReplayObject replayObject)
        {
            // Cannot remove null
            if (replayObject == null)
                return;

            // Remove the replay object
            if (replayObjects.Contains(replayObject) == true)
            {
                replayObjects.Remove(replayObject);
                replayObjectCache.Remove(replayObject.ReplayIdentity);

                // Trigger event
                if (OnReplayObjectRemoved != null)
                    OnReplayObjectRemoved(replayObject);


                // End operations
                if (this.playbackOperation != null)
                    replayObject.EndPlaybackOperation(this.playbackOperation);

                if(this.recordOperations != null)
                {
                    foreach (ReplayRecordOperation operation in this.recordOperations)
                        replayObject.EndRecordOperation(operation);
                }
            }

            // Find all behaviour components
            replayObject.GetComponentsInChildren(sharedChildBehaviours);

            // Unregister behaviours
            foreach (ReplayBehaviour behaviour in sharedChildBehaviours)
            {
                // Make sure that the behaviour is managed by the specified replay object
                if (replayBehaviours.Contains(behaviour) == true && behaviour.ReplayObject == replayObject)
                    replayBehaviours.Remove(behaviour);
            }

            // Clear shared collection
            sharedChildBehaviours.Clear();
        }

        /// <summary>
        /// Remove all replay objects form this replay scene.
        /// </summary>
        public void Clear()
        {
            // Get a list of all registered objects
            List<ReplayObject> clone = new List<ReplayObject>(replayObjects);

            // Clear all - Must call remove so that all replay objects and behaviours are cleaned up properly and have the necessary events called.
            foreach (ReplayObject obj in clone)
                RemoveReplayObject(obj);

            // Clear collections
            replayObjects.Clear();
            replayBehaviours.Clear();
        }

        /// <summary>
        /// Set the current replay scene mode.
        /// Use this method to switch the scene between playback and live modes.
        /// Playback modes will run the <see cref="replayPreparer"/> on all scene objects to disable or re-enable elements that could affect playback.
        /// </summary>
        /// <param name="mode">The scene mode to switch to</param>
        /// <param name="initialStateBuffer">The initial state buffer</param>
        public void SetReplaySceneMode(ReplaySceneMode mode, ReplayStorage storage, RestoreSceneMode restoreMode = RestoreSceneMode.RestoreState)
        {
            if (mode == ReplaySceneMode.Playback)
            {
                // Get the scene ready for playback
                PrepareForPlayback(storage);
                isPlayback = true;
            }
            else
            {
                // Get the scene ready for gameplay                
                isPlayback = false;

                if (mode == ReplaySceneMode.Record)
                {
                }
                else
                {
                    // Return to live mode
                    PrepareForGameplay(storage, restoreMode);
                }
            }
        }

        private void PrepareForPlayback(ReplayStorage storage)
        {
            // Sample the current scene
            prePlaybackState = CaptureSnapshot(0, 0, storage.PersistentData);

            // Prepare objects for playback
            foreach(ReplayObject obj in replayObjects)
            {
                if(obj != null)
                {
                    // Prepare the object for playback
                    replayPreparer.PrepareForPlayback(obj);
                }
            }
        }

        private void PrepareForGameplay(ReplayStorage storage, RestoreSceneMode restoreMode)
        {
            // Check if we can restore the previous scene state
            if (prePlaybackState != null)
            {
                // Restore the original game state
                if (restoreMode == RestoreSceneMode.RestoreState)
                    RestoreSnapshot(prePlaybackState, storage);

                // Reset to null so that next states are saved
                prePlaybackState = null;
            }

            // Prepare objects for gameplay
            foreach (ReplayObject obj in replayObjects)
            {
                if (obj != null)
                {
                    // Prepare the object for playback
                    replayPreparer.PrepareForGameplay(obj);
                }
            }
        }

        /// <summary>
        /// Take a snapshot of the current replay scene using the specified timestamp.
        /// </summary>
        /// <param name="timeStamp">The timestamp for the frame indicating its position in the playback sequence</param>
        /// <param name="initialStateBuffer">The <see cref="ReplayInitialDataBuffer"/> to restore dynamic object information from</param>
        /// <returns>A new snapshot of the current replay scene</returns>
        public ReplaySnapshot CaptureSnapshot(float timeStamp, int sequenceID, ReplayPersistentData persistentData)
        {
            // Get snapshot to use for recording
            ReplaySnapshot snapshot = ReplaySnapshot.pool.GetReusable();

            snapshot.TimeStamp = timeStamp;
            snapshot.SequenceID = sequenceID;

            if (persistentData != null)
            {
                // Be sure to record any objects initial transform if they were spawned during the snapshot
                while (dynamicReplayObjects.Count > 0)
                {
                    // Get the next object
                    ReplayObject obj = dynamicReplayObjects.Dequeue();

                    // Make sure the object has not been destroyed
                    if (obj != null)
                    {
                        // Create object created info
                        ReplaySnapshot.ReplayObjectCreatedData createdData = ReplaySnapshot.ReplayObjectCreatedData.FromReplayObject(timeStamp, obj);

                        // Write to state
                        ReplayState state = ReplayState.pool.GetReusable();
                        createdData.OnReplaySerialize(state);

                        // Store in persistent buffer
                        persistentData.StorePersistentDataByTimestamp(obj.ReplayIdentity, timeStamp, state);
                    }
                }
            }


            // Record each object in the scene
            foreach (ReplayObject obj in replayObjects)
            {
                ReplayState state = ReplayState.pool.GetReusable();

                // Serialize the object
                obj.OnReplaySerialize(state);

                // Check if the state contains any information - If not then don't waste valuable memory
                if (state.Size == 0)
                    continue;

                // Record the snapshot
                snapshot.RecordSnapshot(obj.ReplayIdentity, state);
            }

            return snapshot;
        }

        /// <summary>
        /// Restore the scene to the state described by the specified snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore</param>
        /// <param name="storage">The <see cref="ReplayStorage"/> used to restore dynamic object information from</param>
        public void RestoreSnapshot(ReplaySnapshot snapshot, ReplayStorage storage, bool simulate = false, bool ignoreUpdate = false)
        {
            // Update identity size
            ReplayIdentity.deserializeSize = storage.IdentitySize;

            // Restore all events first
            snapshot.RestoreReplayObjects(this, storage.PersistentData);

            // Restore all replay objects
            foreach (ReplayObject obj in replayObjects)
            {
                // Get the state based on the identity
                ReplayState state = snapshot.RestoreSnapshot(obj.ReplayIdentity);

                // Check if no state information for this object was found
                if (state == null)
                    continue;

                // Deserialize the object
                obj.OnReplayDeserialize(state, simulate, ignoreUpdate);
            }
        }

        /// <summary>
        /// Check if the replay scene has a <see cref="ReplayObject"/> registered with the specified <see cref="ReplayIdentity"/>.
        /// </summary>
        /// <param name="replayIdentity">The id of the to search for</param>
        /// <returns>True if an object with the specified id is added to this <see cref="ReplayScene"/></returns>
        public bool HasReplayObject(ReplayIdentity replayIdentity)
        {
            return replayObjectCache.ContainsKey(replayIdentity);
        }

        /// <summary>
        /// Check if any registered <see cref="ReplayObject"/> have been invalidated or destroyed since they were added to the scene.
        /// </summary>
        /// <param name="throwOnError">True if an exception should be thrown if there are integrity issues</param>
        /// <returns>True if this scene is valid or false if one or more registered <see cref="ReplayObject"/> have been destroyed but not unregistered</returns>
        /// <exception cref="Exception">The replay scene contains one or more destroyed objects</exception>
        public bool CheckIntegrity(bool throwOnError)
        {
            // Remove null
            replayObjects.Remove(null);

            // Find all dead objects in cache
            int count = 0;
            foreach(KeyValuePair<ReplayIdentity, ReplayObject> obj in replayObjectCache)
            {
                // Check for dead
                if(obj.Value == null)
                {
                    count++;
                    sharedDeadObjectIds.Add(obj.Key);
                }
            }

            // Remove all
            foreach(ReplayIdentity id in sharedDeadObjectIds)
            {
                replayObjectCache.Remove(id);
            }

            // Clear temp collection
            sharedDeadObjectIds.Clear();


            replayBehaviours.Remove(null);

            int dynamicCount = dynamicReplayObjects.Count;

            for(int i = 0; i < dynamicCount; i++)
            {
                // Get the item
                ReplayObject dynamic = dynamicReplayObjects.Dequeue();

                if(dynamic != null)
                {
                    dynamicReplayObjects.Enqueue(dynamic);
                }
                else
                {
                    count++;
                }
            }

            // Check for error
            if (throwOnError == true && count > 0)
                throw new Exception("One or more replay objects have been destroyed but are still registered with a replay scene instance. You should remove any dead objects from a replay scene before starting playback or recording");

            return count == 0;
        }

        /// <summary>
        /// Attempt to find a <see cref="ReplayObject"/> with the specified <see cref="ReplayIdentity"/>
        /// </summary>
        /// <param name="replayIdentity">The identity of the object to find</param>
        /// <returns>A <see cref="ReplayObject"/> with the specified ID or null if the object was not found</returns>
        public ReplayObject GetReplayObject(ReplayIdentity replayIdentity)
        {
            ReplayObject obj;
            replayObjectCache.TryGetValue(replayIdentity, out obj);

            return obj;
        }

        internal void BeginPlaybackOperation(ReplayPlaybackOperation operation)
        {
            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Store operation
            if (playbackOperation != null)
                throw new InvalidOperationException("Replay scene is already being used in another playback operation");

            // Store operation
            this.playbackOperation = operation;

            // Require lock of all objects
            foreach (ReplayObject obj in replayObjects)
                obj.BeginPlaybackOperation(operation);
        }

        internal void EndPlaybackOperation(ReplayPlaybackOperation operation)
        {
            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Check for match
            if (this.playbackOperation == operation)
                this.playbackOperation = null;

            // Unlock all objects for other playback operations
            foreach (ReplayObject obj in replayObjects)
                obj.EndPlaybackOperation(operation);
        }

        internal void BeginRecordOperation(ReplayRecordOperation operation)
        {
            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Create collection if required
            if (this.recordOperations == null)
                this.recordOperations = new List<ReplayRecordOperation>(4);

            // Add operation
            if (this.recordOperations.Contains(operation) == false)
                this.recordOperations.Add(operation);

            // Update all objects
            foreach (ReplayObject obj in replayObjects)
                obj.BeginRecordOperation(operation);
        }

        internal void EndRecordOperation(ReplayRecordOperation operation)
        {
            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Remove operation
            if(this.recordOperations != null && this.recordOperations.Contains(operation) == true)
                this.recordOperations.Remove(operation);

            // Update all objects
            foreach (ReplayObject obj in replayObjects)
                obj.EndRecordOperation(operation);
        }

        /// <summary>
        /// Create a new replay scene from the active Unity scene.
        /// All <see cref="ReplayObject"/> in the active scene will be added to the <see cref="ReplayScene"/> result.
        /// The active scene is equivalent of <see cref="SceneManager.GetActiveScene"/>;
        /// </summary>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromCurrentScene(IReplayPreparer preparer = null)
        {
            return FromScene(SceneManager.GetActiveScene(), preparer);
        }

        /// <summary>
        /// Create a new replay scene from the active Unity scene excluding the specified replay objects.
        /// All <see cref="ReplayObject"/> in the active scene will be added to the <see cref="ReplayScene"/> result.
        /// The active scene is equivalent of <see cref="SceneManager.GetActiveScene"/>;
        /// </summary>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromCurrentSceneExcept(IReplayPreparer preparer = null, params ReplayObject[] exclude)
        {
            return FromSceneExcept(SceneManager.GetActiveScene(), preparer, exclude);
        }

        /// <summary>
        /// Create a new replay scene from the specified Unity scene.
        /// All <see cref="ReplayScene"/> in the specified scene will be added to the <see cref="ReplayScene"/> result. 
        /// </summary>
        /// <param name="scene">The Unity scene used to create the <see cref="ReplayScene"/></param>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromScene(Scene scene, IReplayPreparer preparer = null)
        {
            // Create an empty scene
            ReplayScene replayScene = new ReplayScene(preparer);

            // Check all registered objects
            foreach (ReplayObject obj in ReplayObject.AllReplayObjects)
            {
                // Check for same scene
                if(obj.gameObject.scene.name != null && 
                    obj.gameObject.scene.name == scene.name)
                {
                    // Add the replay object
                    replayScene.AddReplayObject(obj);
                }
            }

            return replayScene;
        }

        /// <summary>
        /// Create a new replay scene from the specified Unity scene excluding the specified replay objects.
        /// All <see cref="ReplayScene"/> in the specified scene will be added to the <see cref="ReplayScene"/> result. 
        /// </summary>
        /// <param name="scene">The Unity scene used to create the <see cref="ReplayScene"/></param>
        /// <param name="exclude">A collection of replay objects that should not be added to the resulting scene</param>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromSceneExcept(Scene scene, IReplayPreparer preparer = null, params ReplayObject[] exclude)
        {
            // Create an empty scene
            ReplayScene replayScene = new ReplayScene(preparer);

            // Check all registered objects
            foreach (ReplayObject obj in ReplayObject.AllReplayObjects)
            {
                // Check for same scene
                if (obj.gameObject.scene.name != null &&
                    obj.gameObject.scene.name == scene.name)
                {
                    // Check for exclude
                    if (exclude.Contains(obj) == true)
                        continue;

                    // Add the replay object
                    replayScene.AddReplayObject(obj);
                }
            }

            return replayScene;
        }

        /// <summary>
        /// Create a new replay scene containing all active replay objects from all loaded Unity scenes.
        /// </summary>
        /// <param name="preparer"></param>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromAllScenes(IReplayPreparer preparer = null)
        {
            // Create an empty scene
            ReplayScene replayScene = new ReplayScene(preparer);

            // Check all registered objects
            foreach (ReplayObject obj in ReplayObject.AllReplayObjects)
            {
                // Add the replay object
                replayScene.AddReplayObject(obj);
            }

            return replayScene;
        }

        /// <summary>
        /// Create a new replay scene containing all active replay objects from all loaded Unity scenes, not including the specified exclude replay objects.
        /// </summary>
        /// <param name="preparer"></param>
        /// <param name="exclude">A collection of replay objects that should not be added to the resulting scene</param>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromAllScenesExcept(IReplayPreparer preparer = null, params ReplayObject[] exclude)
        {
            // Create an empty scene
            ReplayScene replayScene = new ReplayScene(preparer);

            // Check all registered objects
            foreach (ReplayObject obj in ReplayObject.AllReplayObjects)
            {
                // Check for exclude
                if(exclude.Contains(obj) == true) 
                    continue;

                // Add the replay object
                replayScene.AddReplayObject(obj);
            }

            return replayScene;
        }
    }
}
