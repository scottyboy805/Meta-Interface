/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/Storage/ReplaySnapshot.cs
/// </summary>
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
                get
                {
                    return flags;
                }
            }

            // Methods
            /// <summary>
            /// Force the storage flags for this snapshot to be updated.
            /// </summary>
            public void GenerateDataFlags() => throw new System.NotImplementedException();
            /// <summary>
            /// Serialize the snapshot into the specified state.
            /// </summary>
            /// <param name = "state"></param>
            public void OnReplaySerialize(ReplayState state) => throw new System.NotImplementedException();
            public void OnReplayDeserialize(ReplayState state) => throw new System.NotImplementedException();
            public static ReplayObjectCreatedData FromReplayObject(float timeStamp, ReplayObject obj) => throw new System.NotImplementedException();
        }

        // Public
        public const int startSequenceID = 1;
        public static readonly ReplayInstancePool<ReplaySnapshot> pool;
        // Properties
        /// <summary>
        /// The time stamp for this snapshot.
        /// The time stamp is used to identify the snapshot location in the sequence.
        /// </summary>        
        public float TimeStamp
        {
            get
            {
                return timeStamp;
            }
        }

        /// <summary>
        /// The unique sequence id value for this snapshot.
        /// A sequence id is an ordered value starting from <see cref = "startSequenceID"/> and counting upwards.
        /// You can get the previous snapshot in the replay using <see cref = "SequenceID"/> -1, or the next snapshot using <see cref = "SequenceID"/> +1.
        /// </summary>
        public int SequenceID
        {
            get
            {
                return sequenceID;
            }
        }

        /// <summary>
        /// Get the size in bytes of the snapshot data.
        /// </summary>        
        public int Size
        {
            get
            {
                if (storageSize == -1)
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
            get
            {
                return states.Keys;
            }
        }

        // Constructor
        internal ReplaySnapshot()
        {
        }

        /// <summary>
        /// Create a new snapshot with the specified time stamp.
        /// </summary>
        /// <param name = "timeStamp">The time stamp to give to this snapshot</param>
        public ReplaySnapshot(float timeStamp, int sequenceID)
        {
            this.timeStamp = timeStamp;
            this.sequenceID = sequenceID;
        }

        public void Dispose() => throw new System.NotImplementedException();
        public override string ToString() => throw new System.NotImplementedException();
        /// <summary>
        /// Called by the replay system when this <see cref = "ReplaySnapshot"/> should be serialized to binary. 
        /// </summary>
        /// <param name = "writer">The binary stream to write te data to</param>
        public void OnReplayStreamSerialize(BinaryWriter writer) => throw new System.NotImplementedException();
        /// <summary>
        /// Called by the replay system when this <see cref = "ReplaySnapshot"/> should be deserialized from binary. 
        /// </summary>
        /// <param name = "reader">The binary stream to read the data from</param>
        public void OnReplayStreamDeserialize(BinaryReader reader) => throw new System.NotImplementedException();
        /// <summary>
        /// Registers the specified replay state with this snapshot.
        /// The specified identity is used during playback to ensure that the replay objects receives the correct state to deserialize.
        /// </summary>
        /// <param name = "identity">The identity of the object that was serialized</param>
        /// <param name = "state">The state data for the object</param>
        public void RecordSnapshot(ReplayIdentity identity, ReplayState state) => throw new System.NotImplementedException();
        /// <summary>
        /// Attempts to recall the state information for the specified replay object identity.
        /// If the identity does not exist in the scene then the return value will be null.
        /// </summary>
        /// <param name = "identity">The identity of the object to deserialize</param>
        /// <returns>The state information for the specified identity or null if the identity does not exist</returns>
        public ReplayState RestoreSnapshot(ReplayIdentity identity) => throw new System.NotImplementedException();
        /// <summary>
        /// Attempts to restore any replay objects that were spawned or despawned during this snapshot.
        /// </summary>
        public void RestoreReplayObjects(ReplayScene scene, ReplayPersistentData persistentData) => throw new System.NotImplementedException();
        /// <summary>
        /// Clears all state information from the snapshot but keeps the time stamp.
        /// </summary>
        public void Reset() => throw new System.NotImplementedException();
        public bool CopyTo(ReplaySnapshot destination) => throw new System.NotImplementedException();
    }
}