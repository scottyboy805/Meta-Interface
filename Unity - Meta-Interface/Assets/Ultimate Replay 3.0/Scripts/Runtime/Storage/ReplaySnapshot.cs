using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UltimateReplay.Formatters;
using UltimateReplay.Lifecycle;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// A frame state is a snapshot of a replay frame that is indexed based on its time stamp.
    /// By sequencing multiple frame states you can create the replay effect.
    /// </summary>
    [Serializable]
    public sealed class ReplaySnapshot : IDisposable, IReplayReusable, IReplayStreamSerialize, IReplayTokenSerialize
    {
        // Type
        public struct ReplayObjectCreatedData : IReplaySerialize
        {
            // Types
            /// <summary>
            /// Represents initial data that may be stored by an object.
            /// </summary>
            [Flags]
            public enum ReplaySerializeFlags : byte
            {
                /// <summary>
                /// No initial data is stored.
                /// </summary>
                None = 0,
                /// <summary>
                /// Initial position is recorded.
                /// </summary>
                Position = 1,
                /// <summary>
                /// Initial rotation is recorded.
                /// </summary>
                Rotation = 2,
                /// <summary>
                /// Initial scale is recorded.
                /// </summary>
                Scale = 4,
                /// <summary>
                /// Initial parent is recorded.
                /// </summary>
                Parent = 8,
            }

            // Public            
            [ReplayTokenSerialize("Serialize Flags")]
            public ReplaySerializeFlags flags;

            /// <summary>
            /// Initial replay object identity.
            /// </summary>
            [ReplayTokenSerialize("Object Identity")]
            public ReplayIdentity objectIdentity;
            /// <summary>
            /// The timestamp when this object was instantiated.
            /// </summary>
            [ReplayTokenSerialize("Time Stamp")]
            public float timestamp;
            /// <summary>
            /// Initial position data.
            /// </summary>
            [ReplayTokenSerialize("Position")]
            public Vector3 position;
            /// <summary>
            /// Initial rotation data.
            /// </summary>
            [ReplayTokenSerialize("Rotation")]
            public Quaternion rotation;
            /// <summary>
            /// Initial scale data.
            /// </summary>
            [ReplayTokenSerialize("Scale")]
            public Vector3 scale;
            /// <summary>
            /// Initial parent data.
            /// </summary>
            [ReplayTokenSerialize("Parent Identity")]
            public ReplayIdentity parentIdentity;
            /// <summary>
            /// The replay ids for all observed components ordered by array index.
            /// </summary>
            [ReplayTokenSerialize("Observed Component Identities")]
            public ReplayIdentity[] observedComponentIdentities;

            // Properties
            public ReplaySerializeFlags InitialFlags
            {
                get { return flags; }
            }

            // Methods
            /// <summary>
            /// Force the storage flags for this snapshot to be updated.
            /// </summary>
            public void GenerateDataFlags()
            {
                // Reset flags
                flags = ReplaySerializeFlags.None;

                // Create the initial object flag data
                if (position != Vector3.zero) flags |= ReplaySerializeFlags.Position;
                if (rotation != Quaternion.identity) flags |= ReplaySerializeFlags.Rotation;
                if (scale != Vector3.one) flags |= ReplaySerializeFlags.Scale;
                if (parentIdentity != ReplayIdentity.invalid) flags |= ReplaySerializeFlags.Parent;
            }

            /// <summary>
            /// Serialize the snapshot into the specified state.
            /// </summary>
            /// <param name="state"></param>
            public void OnReplaySerialize(ReplayState state)
            {
                // Write the object identity
                state.Write(objectIdentity);
                state.Write(timestamp);

                // Calculate the flags
                flags = ReplaySerializeFlags.None;

                // Make sure initial state flags are updated
                GenerateDataFlags();

                // Write the data flags
                state.Write((byte)flags);

                // Write Position
                if ((flags & ReplaySerializeFlags.Position) != 0)
                    state.Write(position);

                // Write rotation
                if ((flags & ReplaySerializeFlags.Rotation) != 0)
                    state.Write(rotation);

                // Write scale
                if ((flags & ReplaySerializeFlags.Scale) != 0)
                    state.Write(scale);

                // Write parent
                if ((flags & ReplaySerializeFlags.Parent) != 0)
                    state.Write(parentIdentity);

                // Write the component identities
                int size = (observedComponentIdentities == null) ? 0 : observedComponentIdentities.Length;

                // Write the number of ids
                state.Write((ushort)size);

                // Write all ids
                for (int i = 0; i < size; i++)
                {
                    // Write the identity
                    state.Write(observedComponentIdentities[i]);
                }
            }

            public void OnReplayDeserialize(ReplayState state)
            {
                // Read the object identity
                state.ReadSerializable(ref objectIdentity);
                timestamp = state.ReadSingle();

                // Read the flags
                flags = (ReplaySerializeFlags)state.ReadByte();

                // Read position
                if ((flags & ReplaySerializeFlags.Position) != 0)
                    position = state.ReadVector3();

                // Read rotation
                if ((flags & ReplaySerializeFlags.Rotation) != 0)
                    rotation = state.ReadQuaternion();

                // Read scale
                if ((flags & ReplaySerializeFlags.Scale) != 0)
                    scale = state.ReadVector3();

                // Read parent identity
                if ((flags & ReplaySerializeFlags.Parent) != 0)
                    parentIdentity = state.ReadIdentity();

                // Read the number of observed components
                int size = state.ReadUInt16();

                // Allocate the array
                observedComponentIdentities = new ReplayIdentity[size];

                // Read all ids
                for (int i = 0; i < size; i++)
                {
                    // Read the identity
                    observedComponentIdentities[i] = state.ReadIdentity();
                }
            }

            public static ReplayObjectCreatedData FromReplayObject(float timeStamp, ReplayObject obj)
            {
                ReplayObjectCreatedData data = new ReplayObjectCreatedData();

                data.objectIdentity = obj.ReplayIdentity;
                data.timestamp = timeStamp;

                data.position = obj.transform.position;
                data.rotation = obj.transform.rotation;
                data.scale = obj.transform.localScale;

                if(obj.transform.parent != null)
                {
                    ReplayObject replayParent = obj.transform.parent.GetComponent<ReplayObject>();

                    // Store parent identity
                    if (replayParent != null)
                        data.parentIdentity = replayParent.ReplayIdentity;
                }

                // Store observed component identity array
                int size = obj.ObservedComponents.Count;
                int index = 0;

                // Allocate array
                data.observedComponentIdentities = new ReplayIdentity[size];

                foreach (ReplayBehaviour behaviour in obj.ObservedComponents)
                {
                    // Store component identity in array
                    data.observedComponentIdentities[index] = behaviour.ReplayIdentity;
                    index++;
                }


                // Update stored data flags
                data.GenerateDataFlags();

                return data;
            }
        }

        // Abstraction to allow support for text serializers
        internal struct ReplayStateEntry : IReplayTokenSerializeProvider
        {
            // Public
            public IReplaySnapshotStorable state;

            // Properties
            /// <summary>
            /// Convert stored state data to token serialized formatter on demand for token serialize.
            /// Allows support for using a single serialize instance throughout the serialization process (Much less allocations).
            /// </summary>
            public IReplayTokenSerialize SerializeTarget
            {
                get
                {
                    // Get formatter
                    ReplayFormatter formatter = ReplayFormatter.GetFormatterOfType<ReplayObjectFormatter>();

                    // Check for deserialize
                    if(state != null && formatter != null)
                    {
                        // Deserialize
                        ((ReplayState)state).PrepareForRead();
                        formatter.OnReplayDeserialize((ReplayState)state);
                    }

                    return formatter;
                }
                set
                {
                    if ((value is ReplayObjectFormatter) == false)
                        throw new InvalidOperationException("Token serialize provider must be of type ReplayObjectFormatter");

                    // Create state
                    if (state == null)
                        state = ReplayState.pool.GetReusable();

                    // Serialize
                    ((ReplayObjectFormatter)value).OnReplaySerialize((ReplayState)state);
                }
            }
        }

        // Private
        private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplaySnapshot>();

        private static ReplayObjectFormatter sharedFormatter = new ReplayObjectFormatter();
        private static Queue<ReplayObject> sharedDestroyQueue = new Queue<ReplayObject>();

        [ReplayTokenSerialize("Time")]
        private float timeStamp = 0;
        [ReplayTokenSerialize("Sequence ID")]
        private int sequenceID = -1;
        [ReplayTokenSerialize("Size", true)]
        private int storageSize = 0;
        private Dictionary<ReplayIdentity, ReplayObjectCreatedData> newReplayObjectsThisFrame = new Dictionary<ReplayIdentity, ReplayObjectCreatedData>();
        [ReplayTokenSerialize("Replay Objects")]
        private Dictionary<ReplayIdentity, ReplayStateEntry> states = new Dictionary<ReplayIdentity, ReplayStateEntry>();

        // Public
        public const int startSequenceID = 1;

        public static readonly ReplayInstancePool<ReplaySnapshot> pool = new ReplayInstancePool<ReplaySnapshot>(() => new ReplaySnapshot());


        // Properties
        /// <summary>
        /// The time stamp for this snapshot.
        /// The time stamp is used to identify the snapshot location in the sequence.
        /// </summary>        
        public float TimeStamp
        {            
            get { return timeStamp; }
            internal set { timeStamp = value; }
        }

        
        /// <summary>
        /// The unique sequence id value for this snapshot.
        /// A sequence id is an ordered value starting from <see cref="startSequenceID"/> and counting upwards.
        /// You can get the previous snapshot in the replay using <see cref="SequenceID"/> -1, or the next snapshot using <see cref="SequenceID"/> +1.
        /// </summary>
        public int SequenceID
        {
            get { return sequenceID; }
            internal set { sequenceID = value; }
        }

        /// <summary>
        /// Get the size in bytes of the snapshot data.
        /// </summary>        
        public int Size
        {
            get
            {
                if(storageSize == -1)
                {
                    storageSize = 0;

                    // Calcualte the size of each object
                    foreach (ReplayStateEntry storable in states.Values)
                    {
                        // Snapshot storable type
                        storageSize += sizeof(byte);

                        if (storable.state.StorageType == ReplaySnapshotStorableType.StatePointer)
                        {
                            // Snapshot pointer value
                            storageSize += sizeof(ushort);
                        }
                        else
                        {
                            ReplayState state = storable.state as ReplayState;
                            storageSize += state.Size;
                        }
                    }
                }

                return storageSize;
            }
        }

        public IEnumerable<ReplayIdentity> Identities
        {
            get { return states.Keys; }
        }

        // Constructor
        internal ReplaySnapshot() { }

        /// <summary>
        /// Create a new snapshot with the specified time stamp.
        /// </summary>
        /// <param name="timeStamp">The time stamp to give to this snapshot</param>
        public ReplaySnapshot(float timeStamp, int sequenceID)
        {
            this.timeStamp = timeStamp;
            this.sequenceID = sequenceID;
        }

        // Methods
        #region TokenSerialize
        IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
        {
            foreach (ReplayToken token in tokens)
            {
                if (token.IsOptional == false || includeOptional == true)
                    yield return token;
            }
        }
        #endregion

        void IReplayReusable.Initialize()
        {
            this.timeStamp = 0;
            this.sequenceID = 0;
            this.storageSize = 0;
            this.states.Clear();
        }

        public void Dispose()
        {
            // Recycle states
            foreach(ReplayStateEntry entry in states.Values)
            {
                if(entry.state is ReplayState)
                    ((ReplayState)entry.state).Dispose();
            }

            this.timeStamp = 0;
            this.sequenceID = 0;
            this.storageSize = 0;
            this.states.Clear();

            // Add to waiting snapshots
            pool.PushReusable(this);
        }

        public override string ToString()
        {
            return string.Format("ReplaySnapshot(timestamp={0}, id={1}, size={2})", timeStamp, sequenceID, Size);
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplaySnapshot"/> should be serialized to binary. 
        /// </summary>
        /// <param name="writer">The binary stream to write te data to</param>
        public void OnReplayStreamSerialize(BinaryWriter writer)
        {
            writer.Write(timeStamp);
            writer.Write(sequenceID);
            writer.Write(storageSize);

            writer.Write((ushort)states.Count);

            foreach(KeyValuePair<ReplayIdentity, ReplayStateEntry> objectState in states)
            {
                // Write the identity
                ((IReplayStreamSerialize)objectState.Key).OnReplayStreamSerialize(writer);

                // Write the storable type
                writer.Write((byte)objectState.Value.state.StorageType);

                // Write the storable
                ((IReplayStreamSerialize)objectState.Value.state).OnReplayStreamSerialize(writer);
            }
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplaySnapshot"/> should be deserialized from binary. 
        /// </summary>
        /// <param name="reader">The binary stream to read the data from</param>
        public void OnReplayStreamDeserialize(BinaryReader reader)
        {
            timeStamp = reader.ReadSingle();
            sequenceID = reader.ReadInt32();
            storageSize = reader.ReadInt32();

            ushort count = reader.ReadUInt16();

            for(int i = 0; i < count; i++)
            {
                //////////// IMPORANT - The replay identity is not being deserialized because it is being boxed to the interface type and passed by value
                ReplayIdentity identity = new ReplayIdentity();

                // Read the identity
                ReplayStreamUtility.StreamDeserialize(ref identity, reader);

                // Read the storable type
                ReplaySnapshotStorableType storableType = (ReplaySnapshotStorableType)reader.ReadByte();
                IReplaySnapshotStorable storable = null;

                if(storableType == ReplaySnapshotStorableType.StatePointer)
                {
                    // Create the pointer
                    storable = new ReplayStatePointer();
                }
                else
                {
                    // Create the state
                    storable = ReplayState.pool.GetReusable();
                }

                // Deserialize the data
                ReplayStreamUtility.StreamDeserialize(ref storable, reader);

                // Register with snapshot
                states.Add(identity, new ReplayStateEntry { state = storable });
            }
        }

        /// <summary>
        /// Registers the specified replay state with this snapshot.
        /// The specified identity is used during playback to ensure that the replay objects receives the correct state to deserialize.
        /// </summary>
        /// <param name="identity">The identity of the object that was serialized</param>
        /// <param name="state">The state data for the object</param>
        public void RecordSnapshot(ReplayIdentity identity, ReplayState state)
        {
            // Register the state
            if (states.ContainsKey(identity) == false)
            {
                states.Add(identity, new ReplayStateEntry { state = state });

                // Reset cached size
                storageSize = -1;
            }
        }

        /// <summary>
        /// Attempts to recall the state information for the specified replay object identity.
        /// If the identity does not exist in the scene then the return value will be null.
        /// </summary>
        /// <param name="identity">The identity of the object to deserialize</param>
        /// <returns>The state information for the specified identity or null if the identity does not exist</returns>
        public ReplayState RestoreSnapshot(ReplayIdentity identity)
        {
            ReplayStateEntry storable;

            // Try to get the state
            if (states.TryGetValue(identity, out storable) == true)
            {
                // Get the state
                ReplayState state = storable.state as ReplayState;

                // Check for error
                if (state == null)
                    return null;

                // Reset the object state for reading
                state.PrepareForRead();

                return state;
            }

            // No state found
            return null;
        }

        /// <summary>
        /// Attempts to restore any replay objects that were spawned or despawned during this snapshot.
        /// </summary>
        public void RestoreReplayObjects(ReplayScene scene, ReplayPersistentData persistentData)
        {
            // Get all active scene objects
            HashSet<ReplayObject> activeReplayObjects = scene.ActiveReplayObjects;

            // Find all active replay objects
            foreach(ReplayObject obj in activeReplayObjects)
            {
                // Check if the object is no longer in the scene
                if (states.ContainsKey(obj.ReplayIdentity) == false)
                {
                    // Check for a prefab
                    if (ReplayManager.Settings.HasPrefabProvider(obj.PrefabIdentity) == false)
                    {
                        ReplayPlaybackAccuracyReporter.RecordPlaybackAccuracyError(obj.ReplayIdentity, ReplayPlaybackAccuracyReporter.PlaybackAccuracyError.DestroyNotARegisteredPrefab);
                        continue;
                    }

                    // We need to destroy the replay object
                    sharedDestroyQueue.Enqueue(obj);
                }
            }

            // Destroy all waiting objects
            while (sharedDestroyQueue.Count > 0)
            {
                // Get the target object
                ReplayObject destroyObject = sharedDestroyQueue.Dequeue();

                // Remove from the scene
                scene.RemoveReplayObject(destroyObject);

                // Destroy the game object
                ReplayObjectLifecycleProvider.DestroyReplayObject(destroyObject);             
            }


            // Process all snapshot state data to check if we need to add any scene objects
            foreach(KeyValuePair<ReplayIdentity, ReplayStateEntry> replayObject in states)
            {
                bool found = false;

                // Try to find replay object with identity
                found = scene.HasReplayObject(replayObject.Key);

                // We need to spawn the object
                if(found == false)
                {
                    // Get the replay state for the object because it contains the prefab information we need
                    ReplayState state = replayObject.Value.state as ReplayState;

                    if (state == null)
                        continue;

                    // Reset the state for reading
                    state.PrepareForRead();

                    // Deserialize the object
                    sharedFormatter.OnReplayDeserialize(state);

                    // Get the prefab identity
                    ReplayIdentity prefabIdentity = sharedFormatter.PrefabIdentity;

                    // Read the name of the prefab that we need to spawn
                    string name = "";

                    // Try to find the matching prefab in our replay manager
                    bool hasReplayPrefab = ReplayManager.Settings.HasPrefabProvider(prefabIdentity);// ReplayManager.FindReplayPrefab(name);

                    // Check if the prefab was found
                    if(hasReplayPrefab == false)
                    {
                        // Check for no prefab identity
                        if (prefabIdentity == ReplayIdentity.invalid)
                        {
                            // Display scene object warning
                            ReplayPlaybackAccuracyReporter.RecordPlaybackAccuracyError(replayObject.Key, ReplayPlaybackAccuracyReporter.PlaybackAccuracyError.InstantiateMissingObjectAndNotPrefab);
                        }
                        else
                        {
                            // Display prefab object warning
                            ReplayPlaybackAccuracyReporter.RecordPlaybackAccuracyError(prefabIdentity, ReplayPlaybackAccuracyReporter.PlaybackAccuracyError.InstantiatePrefabNotFound);
                        }
                        continue;
                    }

                    // Restore initial data
                    ReplayObjectCreatedData initialData = new ReplayObjectCreatedData();

                    // Get persistent data for object created
                    if(persistentData != null && persistentData.HasPersistentDataByTimestamp(replayObject.Key) == true)
                    {
                        // Get the replay state
                        ReplayState initialState = persistentData.FetchPersistentDataByTimestamp(replayObject.Key, timeStamp);
                        initialState.PrepareForRead();

                        // Deserialize initial data
                        initialData.OnReplayDeserialize(initialState);
                    }


                    Vector3 position = Vector3.zero;
                    Quaternion rotation = Quaternion.identity;
                    Vector3 scale = Vector3.one;

                    // Update transform values
                    if ((initialData.InitialFlags & ReplayObjectCreatedData.ReplaySerializeFlags.Position) != 0) position = initialData.position;
                    if ((initialData.InitialFlags & ReplayObjectCreatedData.ReplaySerializeFlags.Rotation) != 0) rotation = initialData.rotation;
                    if ((initialData.InitialFlags & ReplayObjectCreatedData.ReplaySerializeFlags.Scale) != 0) scale = initialData.scale;

                    // Call the instantiate method
                    ReplayObject obj = ReplayManager.Settings.InstantiatePrefabProvider(prefabIdentity, position, rotation); //UltimateReplay.ReplayInstantiate(prefab.gameObject, position, rotation);

                    if(obj == null)
                    {
                        Debug.LogWarning(string.Format("Replay instantiate failed for prefab '{0}'. Some replay objects may be missing", name));
                        continue;
                    }

                    // Be sure to apply initial scale also
                    obj.transform.localScale = scale;

                    // Check if we have the component
                    if (obj != null)
                    {
                        // Give the replay object its serialized identity so that we can send replay data to it
                        obj.ReplayIdentity = replayObject.Key;

                        // Map observed component identities
                        if(initialData.observedComponentIdentities != null)
                        {
                            int index = 0;

                            foreach(ReplayBehaviour behaviour in obj.ObservedComponents)
                            {
                                if(initialData.observedComponentIdentities.Length > index)
                                {
                                    behaviour.ReplayIdentity = initialData.observedComponentIdentities[index];
                                }
                                index++;
                            }
                        }

                        // Register the created object
                        newReplayObjectsThisFrame.Add(obj.ReplayIdentity, initialData);

                        // Add to replay scene
                        scene.AddReplayObject(obj);

                        // Trigger spawned event
                        ReplayBehaviour.InvokeReplaySpawnedEvent(obj.Behaviours, position, rotation);
                    }
                }
            }


            // Re-parent replay objects
            foreach(KeyValuePair<ReplayIdentity, ReplayObjectCreatedData> created in newReplayObjectsThisFrame)
            {
                // Try to get the target object
                ReplayObject createdObject = scene.GetReplayObject(created.Key);

                // Check if the object could not be found for some reason
                if (createdObject == null)
                    continue;

                // Check for a parent identity
                if(created.Value.parentIdentity != ReplayIdentity.invalid)
                {
                    bool foundTargetParent = false;

                    // We need to find the references parent
                    foreach(ReplayObject obj in scene.ActiveReplayObjects)
                    {
                        if(obj.ReplayIdentity == created.Value.parentIdentity)
                        {
                            // Parent the objects
                            createdObject.transform.SetParent(obj.transform, false);

                            // Set the flag
                            foundTargetParent = true;
                            break;
                        }
                    }

                    // Check if we failed to find the parent object
                    if (foundTargetParent == false)
                    {
                        // The target parent object is missing
                        Debug.LogWarning(string.Format("Newly created replay object '{0}' references identity '{1}' as a transform parent but the object could not be found in the current scene. Has the target parent been deleted this frame?", createdObject.name, created.Value.parentIdentity));
                    }
                }
            }

            // Clear ll tracked replay objects this frame
            newReplayObjectsThisFrame.Clear();


            // Report all playback errors
            ReplayPlaybackAccuracyReporter.ReportAllPlaybackAccuracyErrors();
        }

        internal void OverrideStateDataForReplayObject(ReplayIdentity identity, IReplaySnapshotStorable storable)
        {
            if (states.ContainsKey(identity) == true)
            {
                states[identity] = new ReplayStateEntry { state = storable };

                // Reset cached size
                storageSize = -1;
            }
        }

        /// <summary>
        /// Clears all state information from the snapshot but keeps the time stamp.
        /// </summary>
        public void Reset()
        {
            states.Clear();

            // Reset cached size
            storageSize = -1;
        }

        public bool CopyTo(ReplaySnapshot destination)
        {
            // Check for null
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            // Check for invalid sequence
            if (sequenceID <= 0)
                return false;

            // Check for destination not empty
            if (destination.sequenceID > 0 || destination.storageSize > 0 || destination.states.Count > 0)
                throw new InvalidOperationException("Destination snapshot contains replay data. It must be empty before copy");

            // Copy properties
            destination.timeStamp = timeStamp;
            destination.sequenceID = sequenceID;
            destination.storageSize = storageSize;

            // Copy all states
            foreach(KeyValuePair<ReplayIdentity, ReplayStateEntry> state in states)
            {
                // Create copy of state data
                ReplayState destState = ReplayState.pool.GetReusable();

                // Check for unresolved state
                if ((state.Value.state is ReplayState) == false)
                    throw new InvalidOperationException("One or more replay states contain unresolved data. Replay snapshot must be decompressed before copy");

                // Perform copy of state data
                ((ReplayState)state.Value.state).CopyTo(destState);

                // Insert to destination
                destination.states[state.Key] = new ReplayStateEntry
                {
                    state = destState,
                };
            }

            // Copy was successful
            return true;
        }

        internal IReplaySnapshotStorable GetReplayObjectState(ReplayIdentity identity)
        {
            ReplayStateEntry result;

            // Try to find matching data
            states.TryGetValue(identity, out result);

            return result.state;
        }        

        /// <summary>
        /// Attempts to modify the current snapshot time stamp by offsetting by the specified value.
        /// Negative values will reduce the timestamp.
        /// </summary>
        /// <param name="offset">The value to modify the timestamp with</param>
        internal void CorrectTimestamp(float offset)
        {
            // Modify the timestamp
            timeStamp += offset;
        }

        internal void CorrectSequenceID(int offset)
        {
            // Modify the sequence id
            sequenceID += offset;

            for(int i = 0; i < states.Count; i++)
            {
                if(states[states.ElementAt(i).Key].state is ReplayStatePointer)
                {
                    ReplayStatePointer pointer = (ReplayStatePointer)states[states.ElementAt(i).Key].state;

                    pointer.snapshotOffset += (byte)offset;

                    states[states.ElementAt(i).Key] = new ReplayStateEntry { state = pointer };
                }
            }
        }
    }
}