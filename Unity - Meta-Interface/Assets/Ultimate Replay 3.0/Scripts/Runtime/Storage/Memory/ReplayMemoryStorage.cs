using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateReplay.Storage
{
    public sealed class ReplayMemoryStorage : ReplayStorage
    {
        // Private
        private float duration = 0f;
        private float rollingBufferDuration = -1f;
        private int memorySize = 0;
        private int identitySize = sizeof(ushort);
        private List<ReplaySegment> segments = new List<ReplaySegment>();

        // Properties
        public override bool CanRead
        {
            get
            {
                CheckDisposed();
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                CheckDisposed();
                return true;
            }
        }

        public override float Duration
        {
            get
            {
                CheckDisposed();
                return duration;
            }
        }

        public override int MemorySize
        {
            get
            {
                CheckDisposed();
                return memorySize;
            }
        }

        public override int SnapshotSize
        {
            get
            {
                CheckDisposed();

                // Check for none
                if (segments.Count == 0)
                    return 0;

                // Get total
                return segments[segments.Count - 1].EndSequenceID;
            }
        }

        public override int IdentitySize
        {
            get
            {
                CheckDisposed();
                return identitySize;
            }
        }

        public float RollingBufferDuration
        {
            get
            {
                CheckDisposed();
                return rollingBufferDuration;
            }
        }

        // Constructor
        public ReplayMemoryStorage(string replayName = null, float rollingBufferDuration = -1f)
            : base(replayName)
        {
            // Get size of identity
            identitySize = ReplayIdentity.byteSize;

            // Store buffer size
            this.rollingBufferDuration = rollingBufferDuration;
        }

        // Methods
        public override ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            // Check for disposed
            CheckDisposed();

            // Check for no data
            if (segments.Count == 0)
                return null;

            // Check for last clip
            if(timeStamp >= duration)
            {
                // Get the very last snapshot
                return segments[segments.Count - 1].EndSnapshot;
            }

            // Find the best matching segment
            ReplaySegment best = segments[0];

            // Check for better match
            foreach(ReplaySegment segment in segments)
            {
                if(timeStamp >= segment.StartTimeStamp)
                {
                    best = segment;
                    //break;

                    // We can stop searching at this point
                    if (timeStamp <= segment.EndTimeStamp)
                        break;
                }

                
                //if (timeStamp >= segment.EndTimeStamp)
                //    break;
            }

            // Fetch the snapshot from the segment
            return best.FetchSnapshot(timeStamp);
        }

        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            // Check for disposed
            CheckDisposed();

            // Check for no segments
            if (segments.Count == 0)
                return null;

            // Find the snapshot with sequence id
            foreach(ReplaySegment segment in segments)
            {
                // Check if the sequence id is passed the end of this segment
                if (sequenceID > segment.EndSequenceID)
                    continue;

                // Get snapshot for id
                return segment.FetchSnapshot(sequenceID);
            }

            // No snapshot found
            return null;
        }

        public override void StoreSnapshot(ReplaySnapshot state)
        {
            // Check for disposed
            CheckDisposed();

            ReplaySegment target = null;

            // Check if we can add to an existing segment
            if(segments.Count > 0 && segments[segments.Count - 1].IsFull == false)
            {
                // Get the end segment
                target = segments[segments.Count - 1];
            }
            else
            {
                // Create new segment
                target = new ReplaySegment(segments.Count, 30);
                segments.Add(target);
            }

            // Add the snapshot
            target.AddSnapshot(state);

            // Update duration
            duration = target.EndTimeStamp;

            // Constrain buffer for rolling buffer support
            if (rollingBufferDuration > 0f && duration > rollingBufferDuration)
            {
                // Constrain buffer
                ConstrainBuffer();

                // Update duration
                duration = target.EndTimeStamp - segments[0].StartTimeStamp;
            }
        }

        public override void Prepare(ReplayStorageAction mode)
        {
            // Check for disposed
            CheckDisposed();

            switch (mode)
            {
                case ReplayStorageAction.Discard:
                    {
                        // Dispose of all segments so that they can be recycled
                        foreach(ReplaySegment segment in segments)
                        {
                            segment.Dispose();
                        }

                        // Clear all snapshot data
                        segments.Clear();

                        // Clear all persistent data
                        persistentData = new ReplayPersistentData();

                        // Reset values
                        duration = 0f;
                        memorySize = 0;
                        break;
                    }
                case ReplayStorageAction.Write:
                    {
                        // Check for already written to
                        if (segments.Count > 0)
                            throw new InvalidOperationException("The memory storage target already has data store. You must clear the data to begin new writing operations");

                        break;
                    }

                case ReplayStorageAction.Commit:
                    {
                        // Check for any segments
                        if (segments.Count == 0)
                            break;

                        // Get first segment
                        ReplaySnapshot first = segments[0].StartSnapshot;

                        // Calculate offset values
                        float offsetTime = first.TimeStamp;

                        //int offsetSequenceId = first.SequenceID - 1;

                        int sequenceId = 1;

                        // Update all stored snapshots so that offsets and sequence ids start from appropriate values
                        foreach(ReplaySegment segment in segments)
                        {
                            // Need to correct segment indexes too
                            segment.CorrectSequenceId(ref sequenceId);

                            foreach(ReplaySnapshot snapshot in segment.Snapshots)
                            {
                                snapshot.CorrectTimestamp(-offsetTime);
                                //snapshot.CorrectSequenceID(-offsetSequenceId);
                            }
                        }
                        break;
                    }
            }
        }

        private void ConstrainBuffer()
        {
            // Check for segments
            if(segments.Count > 0)
            {
                // Get the first segment
                ReplaySegment segment = segments[0];

                // Remove first snapshot
                int memorySize;
                segment.TrimLeadingSnapshot(out memorySize);

                // Update memory size
                this.memorySize -= memorySize;

                // Check for empty segment
                if(segment.IsEmpty == true)
                {
                    segments.Remove(segment);

                    // Recycle the segment
                    segment.Dispose();
                }
            }
        }

        protected override void OnDispose()
        {
            // Dispose of all segments so that they can be recycled
            foreach (ReplaySegment segment in segments)
            {
                segment.Dispose();
            }

            duration = 0;
            memorySize = 0;
            segments.Clear();
        }

        #region Serialization
        public bool SaveToFile(string replayFile)
        {
            // Create file storage
            using (ReplayFileStorage fileStorage = ReplayFileStorage.FromFile(replayFile))
            {
                // Copy to
                return CopyTo(fileStorage);
            }
        }

        public ReplayAsyncOperation SaveToFileAsync(string replayFile)
        {
            // Create file storage
            using(ReplayFileStorage fileStorage = ReplayFileStorage.FromFile(replayFile))
            {
                // Copy to async
                return CopyToAsync(fileStorage);
            }
        }

        public bool LoadFromFile(string replayFile)
        {
            // Create file storage
            using (ReplayFileStorage fileStorage = ReplayFileStorage.FromFile(replayFile))
            {
                // Copy from file
                return fileStorage.CopyTo(this);
            }
        }

        public ReplayAsyncOperation LoadFromFileAsync(string replayFile)
        {
            // Create file storage
            using (ReplayFileStorage fileStorage = ReplayFileStorage.FromFile(replayFile))
            {
                // Copy from file async
                return fileStorage.CopyToAsync(this);
            }
        }

        public byte[] ToBytes()
        {
            // Create memory stream target
            using(MemoryStream stream = new MemoryStream())
            {
                // Create replay stream storage
                using(ReplayStreamStorage streamStorage = ReplayStreamStorage.FromStream(stream))
                {
                    // Copy to
                    if (CopyTo(streamStorage) == false)
                        return Array.Empty<byte>();
                }

                // Get stream bytes
                return stream.ToArray();
            }
        }

        public bool FromBytes(byte[] bytes)
        {
            // Create memory stream input
            using(MemoryStream stream = new MemoryStream(bytes))
            {
                // Create replay stream storage
                using (ReplayStreamStorage streamStorage = ReplayStreamStorage.FromStream(stream))
                {
                    // Copy from bytes
                    return streamStorage.CopyTo(this);
                }
            }
        }
        #endregion
    }
}
