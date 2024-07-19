using System;
using System.Threading;
using UltimateReplay.Statistics;
using UnityEngine;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Represents a task that can be issued to a <see cref="ReplayStorage"/>.
    /// </summary>
    public enum ReplayStorageAction
    {
        /// <summary>
        /// The replay target should commit all data currently in memory to its end destination.
        /// Similar to a flush method.
        /// </summary>
        Commit,
        /// <summary>
        /// The replay target should discard any recorded data.
        /// </summary>
        Discard,
        /// <summary>
        /// The replay target should prepare for subsequent write requests.
        /// </summary>
        Write,
        /// <summary>
        /// The replay target should prepare for subsequent read requests.
        /// </summary>
        Read,
    }

    /// <summary>
    /// Represents and abstract storage device capable of holding recorded state data for playback at a later date.
    /// Depending upon implementation, a <see cref="ReplayStorage"/> may be volatile or non-volatile. 
    /// </summary>
    [Serializable]
    public abstract class ReplayStorage : IDisposable
    {
        // Internal
        internal static int deserializeVersionContext = ReplayStreamStorage.ReplayStreamHeader.replayVersion;

        // Private
        private bool isDisposed = false;
        private ReplayOperation lockOperation = null;

        // Protected
        protected ReplayMetadata metadata = new ReplayMetadata();
        protected ReplayPersistentData persistentData = new ReplayPersistentData();

        // Properties
        /// <summary>
        /// Get the version information for the current deserialization context.
        /// This value is context driven and may change when accessed from different deserialize methods.
        /// </summary>
        public static int DeserializeVersionContext
        {
            get { return deserializeVersionContext; }
        }

        /// <summary>
        /// Return a value indicating whether this storage is currently locked to a replay operation.
        /// A locked storage target cannot be used by another replay operation.
        /// </summary>
        public bool IsLocked
        {
            get { return lockOperation != null; }
        }

        /// <summary>
        /// The user metadata associated with this storage target.
        /// Derive from <see cref="ReplayMetadata"/> and declare additional serialized fields in order to store custom metadata in a replay.
        /// </summary>
        public virtual ReplayMetadata Metadata
        {
            get 
            {
                CheckDisposed();
                return metadata; 
            }
            set 
            {
                CheckDisposed();
                metadata = value;
            }
        }

        /// <summary>
        /// The persistent data associated with this storage target.
        /// Typically used to store single shot data or object instantate data such as initial position, parent, etc.
        /// </summary>
        public virtual ReplayPersistentData PersistentData
        {
            get 
            {
                CheckDisposed();
                return persistentData; 
            }
            set 
            {
                CheckDisposed();
                persistentData = value; 
            }
        }

        // Properties
        /// <summary>
        /// Return a value indicating whether this storage is currently disposed.
        /// A disposed storage target should no longer be used at all.
        /// </summary>
        public bool IsDisposed
        {
            get { return isDisposed; }
        }

        /// <summary>
        /// Get a value indicating whether this storage target is readable.
        /// </summary>
        public abstract bool CanRead { get; }

        /// <summary>
        /// Get a value indicating whether this storage target is writable.
        /// </summary>
        public abstract bool CanWrite { get; }

        /// <summary>
        /// The amount of time in seconds that this recording lasts.
        /// </summary>
        public abstract float Duration { get; }

        /// <summary>
        /// Get the total amount of bytes that this replay uses.
        /// </summary>
        public abstract int MemorySize { get; }

        /// <summary>
        /// Get the total number of snapshots included in this replay.
        /// </summary>
        public abstract int SnapshotSize { get; }

        /// <summary>
        /// Get the size in bytes required to serialize a <see cref="ReplayIdentity"/>.
        /// </summary>
        public abstract int IdentitySize { get; }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="replayName">Optional name for the replay</param>
        protected ReplayStorage(string replayName = null)
        {
            // Check for replay name
            if (string.IsNullOrEmpty(replayName) == false)
                this.metadata = new ReplayMetadata(replayName);

            // Register storage target
            ReplayStorageTargetStatistics.AddStorageTarget(this);
        }

        // Methods
        /// <summary>
        /// Store a replay snapshot in the replay target.
        /// </summary>
        /// <param name="state">The snapshot to store</param>
        public abstract void StoreSnapshot(ReplaySnapshot state);

        /// <summary>
        /// Recall a snapshot from the replay target based on the specified replay offset.
        /// </summary>
        /// <param name="timeStamp">The time offset from the start of the recording pointing to the individual snapshot to recall</param>
        /// <returns>The replay snapshot at the specified offset</returns>
        public abstract ReplaySnapshot FetchSnapshot(float timeStamp);

        /// <summary>
        /// Recall a snapshot by its unique sequence id value.
        /// The sequence ID value indicates the snapshots 1-based index value for the recording sequence.
        /// </summary>
        /// <param name="sequenceID">The sequence ID to fetch the snapshot for</param>
        /// <returns>The replay snapshot at the specified sequence id</returns>
        public abstract ReplaySnapshot FetchSnapshot(int sequenceID);
        
        /// <summary>
        /// Called by the recording system to notify the active <see cref="ReplayStorage"/> of an upcoming event. 
        /// </summary>
        /// <param name="mode">The <see cref="ReplayStorageAction"/> that the target should prepare for</param>
        public abstract void Prepare(ReplayStorageAction mode);

        /// <summary>
        /// Called when the storage target should be disposed to cleanup.
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// Throws an exception if the current storage is disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Storage is disposed</exception>
        protected void CheckDisposed()
        {
            if (isDisposed == true)
                throw new ObjectDisposedException("The replay storage has already been disposed");
        }

        /// <summary>
        /// Release the storage target.
        /// Should always be called when you have finished using a storage target so that memory can be recycled and file/stream handles can be released.
        /// NOTE: The replay system will not call Dispose in normal circumstances and it must be called manually.
        /// </summary>
        public void Dispose()
        {
            if(isDisposed == false)
            {
                isDisposed = true;
                OnDispose();
            }
        }

        /// <summary>
        /// Called by the replay system when a lock should be created on this storage target, typically when a record or playback operation is started.
        /// Used to prevent other replay operations from accessing the same storage target.
        /// </summary>
        /// <param name="operation">The <see cref="ReplayOperation"/> that claimed the storage target</param>
        protected internal virtual void Lock(ReplayOperation operation)
        {
            if (this.lockOperation != null)
                throw new InvalidOperationException("The specified storage target is already in use by another replay operation!");

            this.lockOperation = operation;
        }

        /// <summary>
        /// Called by the replay system when a lock should be released on this storage target, typically when a record or playback operation is ended.
        /// </summary>
        /// <param name="operation">The <see cref="ReplayOperation"/> that created the lock</param>
        protected internal virtual void Unlock(ReplayOperation operation)
        {
            if (this.lockOperation == operation)
                this.lockOperation = null;
        }


        #region CopyOperations
        /// <summary>
        /// Copy the saved replay to the specified storage target.
        /// <see cref="Duration"/> must be greater than zero (Must contain data) otherwise this method will return false.
        /// Destination <see cref="Duration"/> must be zero (Must NOT contain data) otherwise this method will return false.
        /// Note that this operation can take some time to complete depending upon the size of the replay, and will block the calling thread until completed.
        /// </summary>
        /// <param name="destination">The target <see cref="ReplayStorage"/> where data should be copied</param>
        /// <returns>True if the copy was successful or false if not</returns>
        /// <seealso cref="CopyToAsync(ReplayStorage)"/>.
        /// <exception cref="ArgumentNullException">Destination storage is null</exception>
        /// <exception cref="ObjectDisposedException">This <see cref="ReplayStorage"/> or destination <see cref="ReplayStorage"/> is disposed</exception>
        public bool CopyTo(ReplayStorage destination)
        {
            // Check for disposed
            CheckDisposed();

            // Check for null
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            // Check for locked
            if (IsLocked == true)
                throw new InvalidOperationException("Source storage is currently in use by another operation");

            // Check for dest locked
            if (destination.IsLocked == true)
                throw new InvalidOperationException("Destination storage is currently in use by another operation");
            
            // Prepare for write
            destination.Prepare(ReplayStorageAction.Write);

            // Check for destination contains data
            if (destination.Duration > 0f)
            {
                Debug.LogError("Destination contains replay data. It must be empty before copy");
                return false;
            }

            // Prepare for read
            Prepare(ReplayStorageAction.Read);

            // Check for no data - Must be called after we have prepared for reading
            if (Duration == 0f)
            {
                Debug.LogError("No data to copy");
                return false;
            }

            // Copy all snapshots
            int sequenceId = 1;
            ReplaySnapshot current = null;

            do
            {
                // Fetch the snapshot
                current = FetchSnapshot(sequenceId);

                // Check for null
                if(current != null)
                {
                    // We must copy the state to avoid multiple storage targets from using the same snapshot reference
                    ReplaySnapshot clone = ReplaySnapshot.pool.GetReusable();

                    // Perform copy
                    current.CopyTo(clone);

                    // Write to destination
                    destination.StoreSnapshot(clone);
                }

                // Increment sequence
                sequenceId++;
            }
            while (current != null);

            // Copy persistent data
            if (persistentData.CopyTo(destination.persistentData) == false)
            {
                Debug.LogError("Failed to copy persistent data");
                return false;
            }

            // Copy metadata
            if (metadata.CopyTo(destination.metadata) == false)
            {
                Debug.LogError("Failed to copy metadata");
                return false;
            }

            // Commit the target
            destination.Prepare(ReplayStorageAction.Commit);

            // Success
            return true;
        }

        /// <summary>
        /// Copy the saved replay to the specified storage target without blocking the main thread.
        /// <see cref="Duration"/> must be greater than zero (Must contain data) otherwise this method will return failed operation.
        /// Destination <see cref="Duration"/> must be zero (Must NOT contain data) otherwise this method will return failed operation.
        /// Note that this operation can take some time to complete depending upon the size of the replay, but the main thread will not be blocked.
        /// The resulting <see cref="ReplayAsyncOperation"/> can be awaited in a coroutine if you need to wait for completion without blocking (Loading screen for example).
        /// </summary>
        /// <param name="destination">The target <see cref="ReplayStorage"/> where data should be copied</param>
        /// <returns>A <see cref="ReplayAsyncOperation"/> that contains information about the current state of the copy operation and can be awaited in a coroutine using `yield return`</returns>
        /// <exception cref="ArgumentNullException">Destination storage is null</exception>
        /// <exception cref="ObjectDisposedException">This <see cref="ReplayStorage"/> or destination <see cref="ReplayStorage"/> is disposed</exception>
        public ReplayAsyncOperation CopyToAsync(ReplayStorage destination)
        {
            // Check for null
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            // Create async
            ReplayAsyncOperation async = new ReplayAsyncOperation();

            // Prepare for write
            destination.Prepare(ReplayStorageAction.Write);

            // Check for destination not empty
            if (destination.Duration > 0f)
            {
                async.Complete(false, "Destination contains replay data. It must be empty before copy");
                return async;
            }

            // Prepare for read
            Prepare(ReplayStorageAction.Read);

            // Check for no data - Must be called after we have prepared for reading
            if (Duration == 0f)
            {
                async.Complete(false, "No data to copy");
                return async;
            }

            // Queue thread item
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                // Copy all snapshots
                int sequenceId = 1;
                ReplaySnapshot current = null;

                do
                {
                    // Fetch the snapshot
                    current = FetchSnapshot(sequenceId);

                    // Check for null
                    if (current != null)
                    {
                        // We must copy the state to avoid multiple storage targets from using the same snapshot reference
                        ReplaySnapshot clone = ReplaySnapshot.pool.GetReusable();

                        // Perform copy
                        current.CopyTo(clone);

                        // Write to destination
                        destination.StoreSnapshot(clone);

                        // Update async progress
                        async.UpdateProgress(Mathf.InverseLerp(0f, Duration, current.TimeStamp) * 0.9f);
                    }

                    // Increment sequence
                    sequenceId++;
                }
                while (current != null);

                // Copy persistent data
                if (persistentData.CopyTo(destination.persistentData) == false)
                {
                    async.Complete(false, "Failed to copy persistent data");
                    return;
                }

                // Update progress
                async.UpdateProgress(0.95f);

                // Copy metadata
                if (metadata.CopyTo(destination.metadata) == false)
                {
                    async.Complete(false, "Failed to copy metadata");
                    return;
                }

                // Commit the target
                destination.Prepare(ReplayStorageAction.Commit);

                // Update progress
                async.UpdateProgress(1f);

                // Complete operation
                async.Complete(true);
            });

            return async;
        }
        #endregion
    }
}
