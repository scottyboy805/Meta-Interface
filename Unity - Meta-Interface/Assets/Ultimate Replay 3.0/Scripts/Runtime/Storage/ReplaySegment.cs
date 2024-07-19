using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UltimateReplay.Lifecycle;

namespace UltimateReplay.Storage
{
    public sealed class ReplaySegment : IDisposable, IReplayReusable, IReplayStreamSerialize, IReplayTokenSerialize
    {
        // Internal
        internal int deserializeVersionContext = ReplayStreamStorage.ReplayStreamHeader.replayVersion; 

        // Private
        private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplaySegment>();
        private static readonly List<ReplayIdentity> sharedIdentities = new List<ReplayIdentity>(64);
        private static readonly List<ReplaySnapshot> sharedSnapshots = new List<ReplaySnapshot>(64);                

        [ReplayTokenSerialize("Segment ID")]
        private int segmentID = 1;
        [ReplayTokenSerialize("Capacity", true)]
        private int snapshotCapacity = 30;
        [ReplayTokenSerialize("Compressed", true)]
        private bool isCompressed = false;
        [ReplayTokenSerialize("Snapshots")]
        private Dictionary<int, ReplaySnapshot> snapshots = null; // SequenceID, Snapshot   
        private ReplaySnapshot start = null;
        private ReplaySnapshot end = null;

        // Public
        public static readonly ReplayInstancePool<ReplaySegment> pool = new ReplayInstancePool<ReplaySegment>(() => new ReplaySegment());

        // Properties
        public int SegmentID
        {
            get { return segmentID; }
            internal set { segmentID = value; }
        }

        public int SnapshotCapacity
        {
            get { return snapshotCapacity; }
            internal set { snapshotCapacity = value; }
        }

        public ReplaySnapshot StartSnapshot
        {
            get { return start; }
        }

        public ReplaySnapshot EndSnapshot
        {
            get { return end; }
        }

        public float StartTimeStamp
        {
            get
            {
                // Get the time stamp
                if (start != null)
                    return start.TimeStamp;

                // Error
                return -1f;
            }
        }

        public float EndTimeStamp
        {
            get
            {
                // Get the time stamp
                if (end != null)
                    return end.TimeStamp;

                // Error
                return -1f;
            }
        }

        public int StartSequenceID
        {
            get
            {
                // Get the sequence id
                if (start != null)
                    return start.SequenceID;

                // Error
                return -1;
            }
        }

        public int EndSequenceID
        {
            get
            {
                // Get the sequence id
                if (end != null)
                    return end.SequenceID;

                // Error
                return -1;
            }
        }

        public int SnapshotCount
        {
            get { return snapshots.Count; }
        }

        public IEnumerable<ReplaySnapshot> Snapshots
        {
            get { return snapshots.Values; }
        }

        public bool IsEmpty
        {
            get { return snapshots.Count == 0; }
        }

        public bool IsFull
        {
            get { return snapshots.Count == snapshotCapacity; }
        }

        public bool IsCompressed
        {
            get { return isCompressed; }
        }

        // Constructor
        public ReplaySegment()
        {
            snapshots = new Dictionary<int, ReplaySnapshot>();
        }

        public ReplaySegment(int segmentID, int snapshotCount)
        {
            // Check for count exceeded
            if (snapshotCount > byte.MaxValue)
                throw new ArgumentException("Snapshot count cannot exceed '255'");

            this.segmentID = segmentID;
            this.snapshotCapacity = snapshotCount;
            this.snapshots = new Dictionary<int, ReplaySnapshot>(snapshotCount);
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

        public void Dispose()
        {
            // Dispose all snapshots
            foreach (ReplaySnapshot snapshot in snapshots.Values)
            {
                snapshot.Dispose();
            }

            segmentID = 0;
            snapshots.Clear();
            start = null;
            end = null;

            // Reset deserialize version
            deserializeVersionContext = ReplayStreamStorage.ReplayStreamHeader.replayVersion;

            // Recycle to pool
            pool.PushReusable(this);
        }

        void IReplayReusable.Initialize()
        {
            segmentID = 0;
            snapshots.Clear();
            start = null;
            end = null;

            // Reset deserialize version
            deserializeVersionContext = ReplayStreamStorage.ReplayStreamHeader.replayVersion;
        }

        public void AddSnapshot(ReplaySnapshot snapshot)
        {
            // Check for null
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            // Check for full
            if (IsFull == true)
                throw new InvalidOperationException("Cannot add snapshot to segment because it is already full");

            // Push to back
            if (snapshots.ContainsKey(snapshot.SequenceID) == false)
            {
                snapshots.Add(snapshot.SequenceID, snapshot);
            }

            // Update start snapshot
            if (start == null) 
                start = snapshot;

            // Update end snapshot
            end = snapshot;
        }

        public ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            // Check if decompression is required
            if (isCompressed == true)
                throw new InvalidOperationException("Replay segment needs to be decompressed before snapshots can be accessed");

            // Check for no snapshots
            if (snapshots.Count == 0)
                return null;

            ReplaySnapshot current = start;

            // Process all snapshots
            foreach (ReplaySnapshot snapshot in snapshots.Values)
            {
                // Check for time stamp
                if (timeStamp >= snapshot.TimeStamp)
                {
                    current = snapshot;
                    //break;
                }
                else break;
            }

            return current;
        }

        public ReplaySnapshot FetchSnapshot(int sequenceId)
        {
            // Check if decompression is required
            if (isCompressed == true)
                throw new InvalidOperationException("Replay segment needs to be decompressed before snapshots can be accessed");

            // Try to get snapshot
            ReplaySnapshot snapshot;
            snapshots.TryGetValue(sequenceId, out snapshot);

            return snapshot;
        }

        /// <summary>
        /// Segment compression algorithm.
        /// Lossless algorithm which works by replacing replay state data that uses a hash seen in previous snapshots in this segment with a pointer object that links to the former snapshot data via index.
        /// This means that is snapshot data is unchanged for a few snapshots, it is possible to eliminate many replay state containers since they contain duplicate data.
        /// </summary>
        public void CompressSegment()
        {
            // Check for no snapshots
            if (snapshots.Count == 0)
                return;

            // Add to list for quicker indexing and sorting
            sharedSnapshots.AddRange(snapshots.Values);

            // Process all snapshots in reverse
            // Stop at 1 because the first snapshot cannot be compressed
            for(int i = sharedSnapshots.Count - 1; i >= 1; i--)
            {
                // Get all identities in a separate iterator so that we can modify replay state without modifying collection
                sharedIdentities.AddRange(sharedSnapshots[i].Identities);

                // Process all identities
                for(int j = 0; j < sharedIdentities.Count; j++)
                {
                    ReplayIdentity identity = sharedIdentities[j];

                    // Get current state
                    IReplaySnapshotStorable currentState = sharedSnapshots[i].GetReplayObjectState(identity);
                    IReplaySnapshotStorable overrideState = currentState;

                    // Check for storage
                    if(currentState.StorageType == ReplaySnapshotStorableType.StateStorage)
                    {
                        // Get state data hash
                        long dataHash = ((ReplayState)currentState).FastDataHash;

                        // Find best snapshot with matching hash
                        // Start at first snapshot and work up to our current snapshot since snapshot pointers can only link to previous snapshots
                        // If no matching data hash is found then the data is unique and cannot be compressed, so we do nothing.
                        for(int k = 0; k < i; k++)
                        {
                            // Try to get actual state data
                            ReplayState dataState = sharedSnapshots[k].GetReplayObjectState(identity) as ReplayState;

                            // Check for found with matching hash
                            if(dataState != null && dataState.FastDataHash == dataHash)
                            {
                                overrideState = new ReplayStatePointer((ushort)k);
                                break;
                            }
                        }
                    }

                    // Check for override provided, in which case we need to update the state with the new compressed state pointer instead of the replay state data
                    if (overrideState != currentState)
                        sharedSnapshots[i].OverrideStateDataForReplayObject(identity, overrideState);
                }

                // Reset shared collection
                sharedIdentities.Clear();
            }

            // Reset shared collection
            sharedSnapshots.Clear();            
        }

        public void DecompressSegment()
        {
            // Check for no snapshots
            if (snapshots.Count == 0)
                return;

            // Add to list for quicker indexing and sorting
            sharedSnapshots.AddRange(snapshots.Values);

            // Process all snapshots starting from 1 - First snapshot cannot be compressed
            for (int i = 1; i < sharedSnapshots.Count; i++)
            {
                // Get all identities in a separate iterator so that we can modify replay state without modifying collection
                sharedIdentities.AddRange(sharedSnapshots[i].Identities);

                // Process all identities
                foreach (ReplayIdentity identity in sharedIdentities)
                {
                    // Get current state
                    IReplaySnapshotStorable currentState = sharedSnapshots[i].GetReplayObjectState(identity);

                    // Check for storage pointer in which case we need to relink to the actual data
                    if (currentState.StorageType == ReplaySnapshotStorableType.StatePointer)
                    {
                        // Get the state data for the snapshot that we are pointing to
                        ReplayState dataState = sharedSnapshots[((ReplayStatePointer)currentState).snapshotOffset].GetReplayObjectState(identity) as ReplayState;

                        // TODO - Maybe we should duplicate the data into a separate pooled ReplayState? There is potential for multiple snapshots to reference the same state but maybe it is not a problem?

                        // Check for no state
                        if (dataState == null)
                            throw new FormatException("Could not decompress segment because it appears to be corrupted: Expected state data");

                        // Override the data
                        sharedSnapshots[i].OverrideStateDataForReplayObject(identity, dataState);
                    }
                }

                // Reset shared collection
                sharedIdentities.Clear();
            }

            // Reset shared collection
            sharedSnapshots.Clear();
        }

        #region StreamSerialize
        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Write segment id and compression
            writer.Write(segmentID);
            writer.Write(isCompressed);

            // Write size
            writer.Write((ushort)snapshots.Count);

            // Write all snapshots
            foreach(ReplaySnapshot snapshot in snapshots.Values)
            {
                // Write snapshot
                ((IReplayStreamSerialize)snapshot).OnReplayStreamSerialize(writer);
            }
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        { 
            // Update version that we are deserializing
            ReplayStorage.deserializeVersionContext = deserializeVersionContext;

            // Read segment id and compression
            segmentID = reader.ReadInt32();
            isCompressed = reader.ReadBoolean();

            // Read size
            int size = reader.ReadUInt16();

            // Clear old data
            snapshots.Clear();

            // Read all snapshots
            for(int i = 0; i < size; i++)
            {
                // Create snapshot
                ReplaySnapshot snapshot = ReplaySnapshot.pool.GetReusable();

                // Read snapshot
                ((IReplayStreamSerialize)snapshot).OnReplayStreamDeserialize(reader);

                // Add to collection
                snapshots.Add(snapshot.SequenceID, snapshot);
            }
        }
        #endregion

        internal void TrimLeadingSnapshot(out int memorySize)
        {
            memorySize = 0;
            if(snapshots.Count > 0)
            {
                // Get first snapshot
                ReplaySnapshot snapshot = start;

                // Remove first
                snapshots.Remove(snapshot.SequenceID);

                // Get size of snapshot
                memorySize = snapshot.Size;

                // Recycle snapshot
                snapshot.Dispose();

                // Update start
                if (snapshots.Count > 0)
                    start = snapshots.Values.First();
            }
        }

        internal void CorrectSequenceId(ref int currentSequenceId)
        {
            // Add all snapshots
            foreach (ReplaySnapshot snapshot in snapshots.Values)
                sharedSnapshots.Add(snapshot);

            // Clear all snapshots
            snapshots.Clear();

            // Update all snapshots
            foreach (ReplaySnapshot snapshot in sharedSnapshots)
            {
                int id = currentSequenceId;

                // Add entry
                snapshots[id] = snapshot;
                snapshot.SequenceID = id;

                // Increment id
                currentSequenceId++;
            }

            // Clear shared collection
            sharedSnapshots.Clear();
        }
    }
}
