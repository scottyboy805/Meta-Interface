using System;
using System.Collections.Generic;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// A special storage target that can combine multiple other storage sources into a single replay to create a highlight reel/montage.
    /// Useful for showing action replays in sequence or similar.
    /// </summary>
    public sealed class ReplayHighlightReelStorage : ReplayStorage
    {
        // Private
        private List<ReplayStorage> highlights = new List<ReplayStorage>();
        private bool disposeHighlights = true;
        private float duration = 0f;
        private int memorySize = 0;
        private int snapshotCount = 0;
        private int identitySize = sizeof(ushort);

        // Properties
        /// <summary>
        /// Does the storage target support read operations for playback mode.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                CheckDisposed();
                return true;
            }
        }

        /// <summary>
        /// Does the storage target support write operations for record mode.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                CheckDisposed();
                return false;
            }
        }

        /// <summary>
        /// Get the duration in seconds that the stored recording lasts.
        /// </summary>
        public override float Duration
        {
            get
            {
                CheckDisposed();
                return duration;
            }
        }

        /// <summary>
        /// Get the amount of bytes that have been stored for the current recording.
        /// The number of bytes represents only the data recorded by the replay system and not actual memory usage.
        /// </summary>
        public override int MemorySize
        {
            get
            {
                CheckDisposed();
                return memorySize;
            }
        }

        /// <summary>
        /// Get the total number of <see cref="ReplaySnapshot"/> stored in the current recording.
        /// </summary>
        public override int SnapshotSize
        {
            get
            {
                CheckDisposed();
                return snapshotCount;
            }
        }

        /// <summary>
        /// Get the size in bytes of all <see cref="ReplayIdentity"/> stored in this recording.
        /// The byte size of <see cref="ReplayIdentity"/> may be changed for better storage size vs max number of possible replay objects.
        /// </summary>
        public override int IdentitySize
        {
            get
            {
                CheckDisposed();
                return identitySize;
            }
        }

        // Constructor
        /// <summary>
        /// Create a new instance with the specified storage inputs to combine into a highlights reel.
        /// </summary>
        /// <param name="highlights">A number of storage targets used to form a montage in the order specified</param>
        /// <param name="disposeHighlights">True if all provided storage targets should also be disposed when this <see cref="ReplayHighlightReelStorage"/> is disposed</param>
        /// <exception cref="ArgumentNullException">One or more storage targets in the specified <see cref="IEnumerable{T}"/> are null</exception>
        public ReplayHighlightReelStorage(IEnumerable<ReplayStorage> highlights, bool disposeHighlights = true)
        {
            identitySize = ReplayIdentity.byteSize;

            this.highlights.AddRange(highlights);
            this.disposeHighlights = disposeHighlights;

            // Update values
            foreach(ReplayStorage storage in highlights)
            {
                // Check for null
                if (storage == null)
                    throw new ArgumentNullException("highlights", "One or more elements in the collection are null");

                // Update info
                duration += storage.Duration;
                memorySize += storage.MemorySize;
                snapshotCount += storage.SnapshotSize;
            }
        }

        // Methods
        /// <summary>
        /// Called by the replay system when a lock should be created on this storage target, typically when a record or playback operation is started.
        /// Used to prevent other replay operations from accessing the same storage target.
        /// </summary>
        /// <param name="operation">The <see cref="ReplayOperation"/> that claimed the storage target</param>
        protected internal override void Lock(ReplayOperation operation)
        {
            base.Lock(operation);

            // Lock all targets
            foreach(ReplayStorage storage in highlights)
            {
                storage.Lock(operation);
            }
        }

        /// <summary>
        /// Called by the replay system when a lock should be released on this storage target, typically when a record or playback operation is ended.
        /// </summary>
        /// <param name="operation">The <see cref="ReplayOperation"/> that created the lock</param>
        protected internal override void Unlock(ReplayOperation operation)
        {
            base.Unlock(operation);

            // Unlock all targets
            foreach (ReplayStorage storage in highlights)
            {
                storage.Unlock(operation);
            }
        }

        public override ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            float correctedTimestamp = -1f;
            ReplayStorage storage = null;

            // Get the storage target
            if (GetActualStorageTarget(timeStamp, out correctedTimestamp, out storage) == true)
                return storage.FetchSnapshot(correctedTimestamp);

            return null;
        }

        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            int correctedSequenceId = -1;
            ReplayStorage storage = null;

            // Get the storage target
            if (GetActualStorageTarget(sequenceID, out correctedSequenceId, out storage) == true)
                return storage.FetchSnapshot(correctedSequenceId);

            return null;
        }

        public override void Prepare(ReplayStorageAction mode)
        {
            // Prepare all storage
            foreach (ReplayStorage storage in highlights)
                storage.Prepare(mode);
        }

        public override void StoreSnapshot(ReplaySnapshot state)
        {
            throw new NotSupportedException("Highlight reel is read only");
        }

        protected override void OnDispose()
        {
            if (disposeHighlights == true)
            {
                // Dispose of all targets
                foreach (ReplayStorage storage in highlights)
                    storage.Dispose();
            }

            // Clear items
            highlights.Clear();
        }

        private bool GetActualStorageTarget(float timestamp, out float correctedTimestamp, out ReplayStorage storage)
        {
            float tempTimestamp = timestamp;

            // Check all storage
            for(int i = 0; i < highlights.Count; i++)
            {
                // Check for too high
                if(tempTimestamp > highlights[i].Duration)
                {
                    // Skip to next storage
                    tempTimestamp -= highlights[i].Duration;
                }
                else
                {
                    correctedTimestamp = tempTimestamp;
                    storage = highlights[i];
                    return true;
                }
            }
            correctedTimestamp = -1f;
            storage = null;
            return false;
        }

        private bool GetActualStorageTarget(int sequenceId, out int correctedSequenceId, out ReplayStorage storage)
        {
            int tempSequenceId = sequenceId;

            // Check all storage
            for(int i = 0; i < highlights.Count; i++)
            {
                // Check for too high
                if(tempSequenceId > highlights[i].SnapshotSize)
                {
                    // Skip to next storage
                    tempSequenceId -= highlights[i].SnapshotSize;
                }
                else
                {
                    correctedSequenceId = tempSequenceId;
                    storage = highlights[i];
                    return true;
                }
            }
            correctedSequenceId = -1;
            storage = null;
            return false;
        }
    }
}
