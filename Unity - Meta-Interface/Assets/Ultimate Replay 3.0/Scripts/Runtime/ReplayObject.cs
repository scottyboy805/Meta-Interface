using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UltimateReplay.Formatters;
using UltimateReplay.Lifecycle;
using UltimateReplay.ComponentData;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-firstpass")]
[assembly: InternalsVisibleTo("UltimateReplay-Editor")]

namespace UltimateReplay
{
    public enum IDAssignMode
    {
        KeepIdentity,
        NewIdentity,
    }

    /// <summary>
    /// Only one instance of <see cref="ReplayObject"/> can be added to any game object. 
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class ReplayObject : MonoBehaviour, IReplaySerialize, ISerializationCallbackReceiver
    {
        // Types
        private enum ReplayPrefabType
        {
            PrefabAsset, 
            PrefabInstance,
        }

        [Serializable]
        public struct ReplayObjectReference
        {
            // Public
            public ReplayObject reference;

            // Constructor
            public ReplayObjectReference(ReplayObject obj)
            {
                this.reference = obj;
            }
        }

        // Internal
#if UNITY_EDITOR || ULTIMATEREPLAY_TRIAL        // Must be active in trail version as it is built as a dll
        internal bool isObservedComponentsExpanded = false;
#endif

        // Private        
        private static readonly ReplayObjectFormatter formatter = new ReplayObjectFormatter();
        private static readonly HashSet<ReplayObject> allReplayObjects = new HashSet<ReplayObject>();

        private List<ReplayComponentData> waitingComponents = new List<ReplayComponentData>();
        private List<ReplayVariableData> waitingVariables = new List<ReplayVariableData>();
        private List<ReplayEventData> waitingEvents = new List<ReplayEventData>();
        private List<ReplayMethodData> waitingMethods = new List<ReplayMethodData>();

        [SerializeField, HideInInspector]
        private IDAssignMode instantiateIdentity = IDAssignMode.NewIdentity;
        [SerializeField, HideInInspector]
        private ReplayIdentity replayIdentity = new ReplayIdentity();
        [SerializeField, HideInInspector]
        private ReplayIdentity prefabIdentity = new ReplayIdentity();        
        private ReplayObjectLifecycleProvider lifecycleProvider = null;
        private ReplayPlaybackOperation playbackOperation = null;
        private List<ReplayRecordOperation> recordOperations = null;        

        /// <summary>
        /// An array of <see cref="ReplayBehaviour"/> components that this object will serialize during recording.
        /// Dynamically adding replay components during recording is not supported.
        /// </summary>
        [SerializeField, HideInInspector]
        private List<ReplayBehaviour> observedComponents = new List<ReplayBehaviour>();
        [SerializeField, HideInInspector]
        private List<ReplayBehaviour> runtimeComponents = new List<ReplayBehaviour>();

        [SerializeField]
#if !ULTIMATEREPLAY_DEBUG
        [HideInInspector]
#endif
        private bool isPrefab = false;
        [SerializeField]
#if !ULTIMATEREPLAY_DEBUG
        [HideInInspector]
#endif
        private ReplayPrefabType prefabType = 0;

        // Properties
        /// <summary>
        /// Get all registered replay objects that exist in all loaded scenes.
        /// </summary>
        public static HashSet<ReplayObject> AllReplayObjects
        {
            get { return allReplayObjects; }
        }

        /// <summary>
        /// Get the unique <see cref="ReplayIdentity"/> for this <see cref="ReplayObject"/>.  
        /// </summary>
        public ReplayIdentity ReplayIdentity
        {
            get { return replayIdentity; }
            set { replayIdentity = value; }
        }

        /// <summary>
        /// Get the unique prefab <see cref="ReplayIdentity"/> for this <see cref="ReplayObject"/> which links to the associated replay prefab.
        /// </summary>
        public ReplayIdentity PrefabIdentity
        {
            get { return prefabIdentity; }
            set { prefabIdentity = value; }
        }

        /// <summary>
        /// Get the <see cref="ReplayObjectLifecycleProvider"/> responsible for the creation and destruction of this replay object.
        /// </summary>
        public ReplayObjectLifecycleProvider LifecycleProvider
        {
            get { return lifecycleProvider; }
            internal set { lifecycleProvider = value; }
        }

        /// <summary>
        /// Returns true when this game object is a prefab asset.
        /// Returns false when this game object is a scene object or prefab instance.
        /// </summary>
        public bool IsPrefab
        {
            get { return isPrefab; }
        }

        public bool IsPrefabAsset
        {
            get { return prefabType == ReplayPrefabType.PrefabAsset; }
        }

        /// <summary>
        /// Returns a value indicating whether this replay object is included in an active replay operation.
        /// This value will be false if the replay is paused. <see cref="IsPlaybackPaused"/> to check if the replay has been paused, or <see cref="IsReplayingOrPaused"/> to get an inclusive value.
        /// </summary>
        public bool IsReplaying
        {
            get { return playbackOperation != null && playbackOperation.IsReplaying == true; }
        }

        /// <summary>
        /// Returns a value indicating whether this replay object is included in an active or paused replay operation.
        /// </summary>
        public bool IsReplayingOrPaused
        {
            get { return playbackOperation != null && (playbackOperation.IsReplaying == true || playbackOperation.IsPlaybackPaused == true); }
        }

        /// <summary>
        /// Returns a value indicating whether this replay object is included in a playback operation that is currently paused.
        /// </summary>
        public bool IsPlaybackPaused
        {
            get { return playbackOperation != null && playbackOperation.IsPlaybackPaused == true; }
        }

        /// <summary>
        /// Returns a value indicating whether this replay object is included in an active record operation.
        /// This value will be false if recording is paused. <see cref="IsRecordingPaused"/> to check if the recording has been paused, or <see cref="IsReplayingOrPaused"/> to get an inclusive value.
        /// </summary>
        public bool IsRecording
        {
            get
            {
                if(recordOperations != null && recordOperations.Count > 0)
                {
                    foreach(ReplayRecordOperation operation in recordOperations)
                    {
                        if (operation.IsRecording == true)
                            return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Returns a value indicating whether this replay object is included in any record operation that is currently paused.
        /// </summary>
        public bool IsRecordingPaused
        {
            get
            {
                if (recordOperations != null && recordOperations.Count > 0)
                {
                    foreach (ReplayRecordOperation operation in recordOperations)
                    {
                        if (operation.IsRecordingPaused == true)
                            return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Returns a value indicating whether this replay object is included in an active or paused record operation.
        /// </summary>
        public bool IsRecordingOrPaused
        {
            get
            {
                if (recordOperations != null && recordOperations.Count > 0)
                {
                    foreach (ReplayRecordOperation operation in recordOperations)
                    {
                        if (operation.IsRecording == true || operation.IsRecordingPaused == true)
                            return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Get the current playback operation for this replay object if it is currently part of a replay.
        /// It is only possible for any given replay object to be associated with a single playback operation at any time, although an object can be recorded multiple times.
        /// </summary>
        public ReplayPlaybackOperation PlaybackOperation
        {
            get { return playbackOperation; }
        }

        /// <summary>
        /// Get all record operations that this replay object is currently associated with.
        /// It is possible for any given replay object to be recorded by multiple difference record operations simultaneously.
        /// </summary>
        public IReadOnlyList<ReplayRecordOperation> RecordOperations
        {
            get 
            {
                // Create collection if required
                if(recordOperations == null)
                    recordOperations = new List<ReplayRecordOperation>(4);

                return recordOperations; 
            }
        }

        /// <summary>
        /// Get all replay components that are observed and managed by this replay object.
        /// </summary>
        public IReadOnlyList<ReplayBehaviour> ObservedComponents
        {
            get { return observedComponents; }
        }

        /// <summary>
        /// Get all replay behaviours managed by this replay object.
        /// </summary>
        public IReadOnlyList<ReplayBehaviour> Behaviours
        {
            get { return runtimeComponents; }
        }

        internal bool ShouldAssignNewID
        {
            get { return isPrefab == false || (isPrefab == true && instantiateIdentity == IDAssignMode.NewIdentity); }
        }

        // Methods
        private void Awake()
        {
            if (ShouldAssignNewID == true)
            {
                // Check if we have instantiated the object and need to generate new ids
                if (isPrefab == true && prefabType == ReplayPrefabType.PrefabInstance && ReplayIdentity.IsIdentityUnique(replayIdentity) == false)
                {
                    // New id's are required for prefab instances
                    ForceRegenerateIdentityWithObservedComponents();
                }
                // Check for prefab spawned at runtime
                else if (isPrefab == true && prefabType == ReplayPrefabType.PrefabAsset && gameObject.scene.name != null && Application.isPlaying == true)
                {
                    prefabType = ReplayPrefabType.PrefabInstance;

                    // New id's are required for prefab instances
                    ForceRegenerateIdentityWithObservedComponents();
                }
                // Check for already used
                else if (isPrefab == false && ReplayIdentity.IsIdentityUnique(replayIdentity) == false)
                {
                    // New id's are required for instantiated scene objects
                    ForceRegenerateIdentityWithObservedComponents();
                }
                else
                {
                    // Claim the replay identity
                    ReplayIdentity.RegisterIdentity(replayIdentity);
                }
            }
        }

        private void Start()
        {
            UpdateRuntimeComponents();
        }

        private void Update()
        {
            // Only run in editor non-play mode
            if (Application.isPlaying == false)
            {
                // Check if components have been removed
                if (CheckComponentListIntegrity() == false)
                    RebuildComponentList();

                // Update prefab
                UpdatePrefabLinks();
            }
            else
            {
                // Update all variables
                for (int i = 0; i < runtimeComponents.Count; i++)
                    runtimeComponents[i].UpdateReplayVariables();
            }
        }

        private void OnEnable()
        {            
            // Register replay object
            allReplayObjects.Add(this);
        }

        private void OnDisable()
        {
            // Unregister replay object
            allReplayObjects.Remove(this);
        }

        private void OnDestroy()
        {
            // Release id
            ReplayIdentity.UnregisterIdentity(replayIdentity);
        }

        /// <summary>
        /// Called by Unity editor.
        /// Can also be called by scripts to force update the component.
        /// </summary>
        public void Reset()
        {
            UpdatePrefabLinks();
            RebuildComponentList();
        }

        public void OnValidate()
        {
            bool updateRequired = false;

            // Check for runtime components
            foreach(ReplayBehaviour behaviour in runtimeComponents)
            {
                if(behaviour == null)
                {
                    updateRequired = true;
                    break;
                }
            }

            if (updateRequired == true)
                UpdateRuntimeComponents(true);
        }

        public void UpdateRuntimeComponents(bool forceUpdate = false)
        {
            if (forceUpdate == true)
                runtimeComponents.Clear();

            //if (Application.isPlaying == true)
            {
                if (runtimeComponents.Count == 0)
                {
                    // Get all behaviour components
                    foreach (ReplayBehaviour behaviour in GetComponentsInChildren<ReplayBehaviour>())
                    {
                        // Only get components which are managed by this component
                        if (behaviour.ReplayObject == this)
                        {
                            // Register the component for receiving replay variable updates, events and method calls
                            runtimeComponents.Add(behaviour);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Force the <see cref="ReplayIdentity"/> to be regenerated with a unique value.
        /// </summary>
        public void ForceRegenerateIdentity()
        {
            // Generate replay id
            ReplayIdentity.Generate(ref replayIdentity);
        }

        /// <summary>
        /// Force the <see cref="ReplayIdentity"/> and all observed component id's to be regenerated with unique values.
        /// </summary>
        public void ForceRegenerateIdentityWithObservedComponents()
        {
            // Generate a new identity
            ForceRegenerateIdentity();

            // Regenerate observed component id's
            foreach (ReplayBehaviour behaviour in observedComponents)
            {
                // Check for destroyed behaviours
                if (behaviour == null)
                    continue;

                behaviour.ForceRegenerateIdentity();
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            UpdatePrefabLinks(true);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Not used
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplayObject"/> should serialize its replay data. 
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to serialize the data to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            // Request runtime behaviours to submit variable data
            foreach (ReplayBehaviour behaviour in runtimeComponents)
                behaviour.SubmitReplayVariables();


            // Generate serialize flags
            ReplayObjectFormatter.ReplayObjectSerializeFlags flags = 0;

            if (isPrefab == true) flags |= ReplayObjectFormatter.ReplayObjectSerializeFlags.Prefab;

            if (observedComponents.Count > 0) flags |= ReplayObjectFormatter.ReplayObjectSerializeFlags.Components;
            if (waitingVariables.Count > 0) flags |= ReplayObjectFormatter.ReplayObjectSerializeFlags.Variables;
            if (waitingEvents.Count > 0) flags |= ReplayObjectFormatter.ReplayObjectSerializeFlags.Events;
            if (waitingMethods.Count > 0) flags |= ReplayObjectFormatter.ReplayObjectSerializeFlags.Methods;

            // Serialize all observed components            
            for(int i = 0; i <  observedComponents.Count; i++)
            {
                ReplayRecordableBehaviour behaviour = observedComponents[i] as ReplayRecordableBehaviour;

                // Check for invalid behaviour
                if (behaviour == null)
                    continue;

                // Get a reusable state
                ReplayState stateData = ReplayState.pool.GetReusable();

                // Serialize the component
                behaviour.OnReplaySerialize(stateData);

                // Get serializer id
                int serializerID = behaviour.Formatter != null ? behaviour.Formatter.FormatterId : -1;

                // Add to formatter
                waitingComponents.Add(new ReplayComponentData(behaviour.ReplayIdentity, serializerID, stateData));
            }

            // Update formatter
            formatter.UpdateFromObject(prefabIdentity, waitingComponents, waitingVariables, waitingEvents, waitingMethods, flags);

            // Reset waiting collections
            waitingComponents.Clear();
            waitingVariables.Clear();
            waitingEvents.Clear();
            waitingMethods.Clear();

            // Run the serializer
            formatter.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplayObject"/> should deserialize its replay data. 
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to deserialize the data to</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            OnReplayDeserialize(state, false, false);
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplayObject"/> should deserialize its replay data. 
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to deserialize the data from</param>
        /// <param name="simulate">True if replay components should be simulated</param>
        public void OnReplayDeserialize(ReplayState state, bool simulate, bool ignoreUpdate)
        {
            // Deserialize
            formatter.OnReplayDeserialize(state, simulate);

            // Get flags
            ReplayObjectFormatter.ReplayObjectSerializeFlags flags = formatter.SerializeFlags;

            // Check for components
            if ((flags & ReplayObjectFormatter.ReplayObjectSerializeFlags.Components) != 0) DeserializeReplayComponents(state, formatter.ComponentStates);

            if (ignoreUpdate == false)
            {
                if ((flags & ReplayObjectFormatter.ReplayObjectSerializeFlags.Variables) != 0) DeserializeReplayVariables(state, formatter.VariableStates);
                if ((flags & ReplayObjectFormatter.ReplayObjectSerializeFlags.Events) != 0) DeserializeReplayEvents(state, formatter.EventStates);
                if ((flags & ReplayObjectFormatter.ReplayObjectSerializeFlags.Methods) != 0) DeserializeReplayMethods(state, formatter.MethodStates);
            }
        }

        #region VariablesEventsMethods
        public void RecordReplayVariable(ReplayIdentity senderIdentity, ReplayVariable replayVariable)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Push variable
            waitingVariables.Add(new ReplayVariableData(senderIdentity, replayVariable)); 
        }

        public void RecordReplayEvent(ReplayIdentity senderIdentity, ushort eventID, ReplayState eventData = null)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Push variable
            waitingEvents.Add(new ReplayEventData(senderIdentity, eventID, eventData));
        }

        public void Call(ReplayIdentity senderIdentity, Action method)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method));

            // Call method
            method.Invoke();
        }

        public void Call<T>(ReplayIdentity senderIdentity, Action<T> method, T arg)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg));

            // Call method
            method.Invoke(arg);
        }

        public void Call<T0, T1>(ReplayIdentity senderIdentity, Action<T0, T1> method, T0 arg0, T1 arg1)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg0, arg1));

            // Call method
            method.Invoke(arg0, arg1);
        }

        public void Call<T0, T1, T2>(ReplayIdentity senderIdentity, Action<T0, T1, T2> method, T0 arg0, T1 arg1, T2 arg2)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg0, arg1, arg2));

            // Call method
            method.Invoke(arg0, arg1, arg2);
        }

        public void Call<T0, T1, T2, T3>(ReplayIdentity senderIdentity, Action<T0, T1, T2, T3> method, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg0, arg1, arg2, arg3));

            // Call method
            method.Invoke(arg0, arg1, arg2, arg3);
        }

        private bool CheckMethodRecordable(ReplayIdentity senderIdentity, MethodInfo targetMethod)
        {
            // Check attribute
            if(targetMethod.IsDefined(typeof(ReplayMethodAttribute)) == false)
            {
                Debug.LogErrorFormat("The method '{0}' cannot be recorded because it does not have the 'ReplayMethod' attribute", targetMethod);
                return false;
            }

            // Check base type
            if(typeof(ReplayBehaviour).IsAssignableFrom(targetMethod.DeclaringType) == false)
            {
                Debug.LogErrorFormat("The method '{0}' cannot be recorded because it is not declared in a type that inherits from 'ReplayBehaviour'", targetMethod);
                return false;
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Returns a value indicating whether the specified recorder component is observed by this <see cref="ReplayObject"/>.
        /// </summary>
        /// <param name="component">The recorder component to check</param>
        /// <returns>True if the component is observed or false if not</returns>
        public bool IsComponentObserved(ReplayBehaviour component)
        {
            return observedComponents.Contains(component);
        }

        /// <summary>
        /// Forces the object to refresh its list of observed components.
        /// Observed components are components which inherit from <see cref="ReplayBehaviour"/> and exist on either this game object or a child of this game object. 
        /// </summary>
        [ContextMenu("Ultimate Replay/Update Replay Components")]
        public void RebuildComponentList()
        {
            observedComponents.Clear();

            // Process all child behaviour scripts
            foreach (ReplayBehaviour behaviour in GetComponentsInChildren<ReplayBehaviour>(true))
            {
                // Check for deleted components
                if (behaviour == null)
                    continue;

                // Only add the script if it is not marked as ignored
                if (behaviour.GetType().IsDefined(typeof(ReplayIgnoreAttribute), true) == false)
                {
                    // Check for sub object handlers
                    if(behaviour.gameObject != gameObject)
                    {
                        GameObject current = behaviour.gameObject;
                        bool skipBehaviour = false;                        

                        while(true)
                        {
                            if (current.GetComponent<ReplayObject>() != null)
                            {
                                skipBehaviour = true;
                                break;
                            }

                            if (current.transform.parent == null || current.transform.parent == transform)
                                break;

                            // Move up hierarchy
                            current = current.transform.parent.gameObject;
                        }

                        if (skipBehaviour == true)
                            continue;
                    }


                    // Update object
                    if (behaviour.ReplayObject == null || behaviour.ReplayObject == this)
                    {
                        // Add script
                        observedComponents.Add(behaviour);

                        // Check for invalid id
                        if (behaviour.ReplayIdentity.IsValid == false)
                            behaviour.ForceRegenerateIdentity();

#if UNITY_EDITOR
                        // Update the behaviour
                        behaviour.UpdateManagingObject();
#endif
                    }
                }
            }

            // Rebuild all parents
            foreach (ReplayObject obj in GetComponentsInParent<ReplayObject>())
                if (obj != this && obj != null && obj.transform != transform)
                    obj.RebuildComponentList();
        }

        /// <summary>
        /// Returns a value indicating whether the observed component list is valid or needs o be rebuilt.
        /// </summary>
        /// <returns>True if the collection is valid or false if not</returns>
        public bool CheckComponentListIntegrity()
        {
            foreach (ReplayBehaviour observed in observedComponents)
                if (observed == null)
                    return false;

            return true;
        }

        private void UpdatePrefabLinks(bool isSerializeCall = false)
        {
            // Only available in edit mode
            if (Application.isPlaying == true)
                return;

            bool forcePrefabAsset = false;

#if UNITY_EDITOR
            //if (isSerializeCall == false)
            //if(EditorApplication.isCompiling == false)
            {
                // Try to get current prefab stage (Edited in isolation or in context)
                PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                // ### Bug workaround
                // Weird bug in some Unity versions where just comparing the `gameObject` to null can trigger a null reference exception when saving prefab changes in context
                // We can catch the exception and the game object will not-be null on the next cycle, so prefab wil be updated at that point
                try
                {
                    // Check if we are dealing with a prefab
                    isPrefab = PrefabUtility.IsPartOfAnyPrefab(gameObject) || (prefabStage != null && prefabStage.IsPartOfPrefabContents(gameObject));
                }
                catch (NullReferenceException) { }

                if (prefabStage != null)
                {
                    // We are dealing with a prefab asset
                    forcePrefabAsset = true;
                }
            }
#endif

            // Update prefab type
            ReplayPrefabType type = 0;

            // ### Bug workaround
            // Weird bug in some Unity versions where just comparing the `gameObject` to null can trigger a null reference exception when saving prefab changes in context
            // Fallback to prefab asset type since this only seems to occur when editing prefab assets in context
            try
            {
                if (gameObject != null)
                {
                    type = gameObject.scene.rootCount == 0 || forcePrefabAsset == true
                        ? ReplayPrefabType.PrefabAsset
                        : ReplayPrefabType.PrefabInstance;
                }
            }
            catch (NullReferenceException) { }

            // Check for changed
            if(prefabType != type || (type == ReplayPrefabType.PrefabAsset && prefabIdentity.IsValid == false))
            {
                // Looks like we have spawned a prefab instance in the scene at edit time
                prefabType = type;

                if(ShouldAssignNewID == true)
                    ForceRegenerateIdentityWithObservedComponents();

                // Check for prefab asset#
                if (type == ReplayPrefabType.PrefabAsset && prefabIdentity.IsValid == false)
                {
                    if (ShouldAssignNewID == true)
                    {
                        ReplayIdentity.Generate(ref prefabIdentity);
                    }
                }
                //else if(type == ReplayPrefabType.PrefabInstance && prefabIdentity.IsValid == false)
                //{
                //    UpdatePrefabLinkFromAsset();
                //}

#if UNITY_EDITOR
                // ### Bug workaround
                // Weird bug in some Unity versions where just comparing `this` to null can trigger a null reference exception when saving prefab changes in context
                // Fallback to prefab asset type since this only seems to occur when editing prefab assets in context
                try
                {
                    if(this != null)
                        EditorUtility.SetDirty(this);
                }
                catch (NullReferenceException) { }
#endif
            }
            else if(type == ReplayPrefabType.PrefabInstance && prefabIdentity.IsValid == false)
            {
                UpdatePrefabLinkFromAsset();
            }

        }

        private void UpdatePrefabLinkFromAsset()
        {
#if UNITY_EDITOR
            // Try to get prefab
            ReplayObject replayPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(this);

            // Check for null;
            if (replayPrefab != null)
            {
                this.prefabIdentity = replayPrefab.prefabIdentity;
                EditorUtility.SetDirty(this);
            }
#endif
        }

        internal void BeginPlaybackOperation(ReplayPlaybackOperation operation)
        {
            // Check for already locked
            if (playbackOperation != null)
                throw new InvalidOperationException(string.Format("Replay Object '{0}' is already being used in another playback operation. A Replay Object can only belong to one playback operation at a time!", gameObject));

            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Create lock
            this.playbackOperation = operation;

            // Restore persistent data
            foreach(ReplayBehaviour behaviour in runtimeComponents)
            {
                // Check if there is any persistent data stored
                if(behaviour != null && operation.Storage.PersistentData.HasPersistentData(behaviour.ReplayIdentity) == true)
                {
                    // Fetch the persistent data for this behaviour and prepare for read
                    behaviour.ReplayPersistentData = operation.Storage.PersistentData.FetchPersistentData(behaviour.ReplayIdentity);
                    behaviour.ReplayPersistentData.PrepareForRead();
                }    
            }
        }

        internal void EndPlaybackOperation(ReplayPlaybackOperation operation)
        {
            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Reset lock
            if (this.playbackOperation == operation)
                this.playbackOperation = null;
        }

        internal void BeginRecordOperation(ReplayRecordOperation operation)
        {
            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Create collection if required
            if(recordOperations == null)
                recordOperations = new List<ReplayRecordOperation>(4);

            // Add to operations collection
            if (recordOperations.Contains(operation) == false)
                recordOperations.Add(operation);
        }

        internal void EndRecordOperation(ReplayRecordOperation operation)
        {
            // Check for null
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Submit persistent datas
            foreach (ReplayBehaviour behaviour in runtimeComponents)
            {
                // Check for any persistent data
                if (behaviour != null && behaviour.HasPersistentData == true)
                    operation.Storage.PersistentData.StorePersistentData(behaviour.ReplayIdentity, behaviour.ReplayPersistentData);
            }

            // Remove record operation
            if (recordOperations != null && recordOperations.Contains(operation) == true)
                recordOperations.Remove(operation);
        }

        private void DeserializeReplayComponents(ReplayState state, IList<ReplayComponentData> components)
        {
            for(int i = 0; i < components.Count; i++)
            {
                ReplayComponentData componentData = components[i];

                // Try to get behaviour
                ReplayRecordableBehaviour behaviour = GetReplayBehaviour(componentData.BehaviourIdentity) as ReplayRecordableBehaviour;

                // Check for found
                if(behaviour != null)
                {
                    // Get the state data
                    ReplayState behaviourState = componentData.ComponentStateData;

                    try
                    {
                        // Check for no data
                        if (behaviourState.Size == 0)
                            continue;

                        // Prepare for read
                        behaviourState.PrepareForRead();

                        // Run deserialize
                        behaviour.OnReplayDeserialize(behaviourState);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private void DeserializeReplayVariables(ReplayState state, IList<ReplayVariableData> variables)
        {
            for(int i = 0; i < variables.Count; i++)
            {
                ReplayVariableData variableData = variables[i];

                // Try to get behaviour
                ReplayBehaviour behaviour = GetReplayBehaviour(variableData.BehaviourIdentity);

                // Check for found
                if(behaviour != null)
                {
                    // Try to resolve the variable
                    ReplayVariable targetVariable = behaviour.GetReplayVariable(variableData.VariableFieldOffset);

                    // Check for found
                    if(targetVariable != null)
                    {
                        // Get the state data
                        ReplayState variableState = variableData.VariableStateData;

                        // Prepare for read
                        variableState.PrepareForRead();

                        // Run deserializer
                        targetVariable.OnReplayDeserialize(variableState);
                    }
                }
            }
        }

        private void DeserializeReplayEvents(ReplayState state, IList<ReplayEventData> events)
        {
            for(int i = 0; i < events.Count; i++)
            {
                ReplayEventData eventData = events[i];

                // Get target behaviour
                ReplayBehaviour behaviour = GetReplayBehaviour(eventData.BehaviourIdentity);

                // Check for found
                if(behaviour != null)
                {
                    // Send the event
                    try
                    {
                        // Get the event data
                        ReplayState eventState = eventData.EventState;

                        // Dont pass null state to the user method
                        if (eventState == null)
                            eventState = ReplayState.pool.GetReusable();

                        // Prepare for read
                        eventState.PrepareForRead();

                        // Safe call event method
                        behaviour.SendReplayEvent(eventData.EventID, eventState);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private void DeserializeReplayMethods(ReplayState state, IList<ReplayMethodData> methods) 
        {
            //foreach(ReplayMethodData methodData in methods)
            for(int i = 0; i < methods.Count; i++)
            {
                ReplayMethodData methodData = methods[i];

                // Get target behaviour
                ReplayBehaviour behaviour = GetReplayBehaviour(methodData.BehaviourIdentity);

                // Check for found
                if(behaviour != null)
                {
                    // Invoke the method
                    try
                    {
                        // Call tje method on the behaviour
                        methodData.TargetMethod.Invoke(behaviour, methodData.MethodArguments);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        internal void RegisterRuntimeBehaviour(ReplayBehaviour behaviour)
        {
            if (behaviour != null && runtimeComponents.Contains(behaviour) == false)
                runtimeComponents.Add(behaviour);
        }

        internal void UnregisterRuntimeBehaviour(ReplayBehaviour behaviour)
        {
            if (behaviour != null && runtimeComponents.Contains(behaviour) == true)
                runtimeComponents.Remove(behaviour);
        }

        /// <summary>
        /// Get the <see cref="ReplayBehaviour"/> observed by this <see cref="ReplayObject"/> with the specified <see cref="ReplayIdentity"/>.
        /// </summary>
        /// <param name="replayIdentity"></param>
        /// <returns></returns>
        public ReplayBehaviour GetReplayBehaviour(ReplayIdentity replayIdentity)
        {
            for(int i = 0; i < runtimeComponents.Count; i++)
            {
                if (runtimeComponents[i].ReplayIdentity == replayIdentity)
                    return runtimeComponents[i];
            }
            return null;
        }

        public static bool CloneReplayObjectIdentity(GameObject cloneFromObject, GameObject cloneToObject)
        {
            // Get replay object components
            ReplayObject[] from = cloneFromObject.GetComponentsInChildren<ReplayObject>();
            ReplayObject[] to = cloneToObject.GetComponentsInChildren<ReplayObject>();

            // Make sure components are found
            if (from.Length != to.Length)
                return false;

            bool cloned = true;

            for(int i = 0; i < from.Length; i++)
            {
                // Clone each replay object component
                if (CloneReplayObjectIdentity(from[i], to[i]) == false)
                    cloned = false;
            }

            // Call through
            return cloned;
        }

        public static bool CloneReplayObjectIdentity(ReplayObject cloneFromObject, ReplayObject cloneToObject)
        {
            // Check for same object
            if (cloneFromObject == cloneToObject)
                return false;

            // Replay components must match
            if (cloneFromObject.observedComponents.Count != cloneToObject.observedComponents.Count)
                return false;

            // Clone object id
            cloneToObject.replayIdentity = new ReplayIdentity(cloneFromObject.replayIdentity);

            for(int i = 0; i < cloneFromObject.observedComponents.Count; i++)
            {
                // Check for destroyed components
                if (cloneToObject.observedComponents[i] == null || cloneFromObject.observedComponents[i] == null)
                    continue;

                // Clone component id
                cloneToObject.observedComponents[i].ReplayIdentity = new ReplayIdentity(cloneFromObject.observedComponents[i].ReplayIdentity);
            }

            return true;
        }
    }
}
