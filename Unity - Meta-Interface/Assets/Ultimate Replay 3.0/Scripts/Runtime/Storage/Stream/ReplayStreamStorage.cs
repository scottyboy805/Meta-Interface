#if UNITY_WEBGL
#define ULTIMATEREPLAY_DISABLE_THREADING
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace UltimateReplay.Storage
{
    public enum ReplayStreamType
    {
        /// <summary>
        /// The result system will use the default replay format when writing or reading the stream (Binary format by default).
        /// </summary>
        Default = 0,
        /// <summary>
        /// The replay system will use a high performance binary stream format for best performance and storage requirements.
        /// </summary>
        Binary = 1,
        /// <summary>
        /// The replay system will use a human readable json stream format for the replay. Useful for working with replay data in other applications when using a TextWriter for example.
        /// </summary>
        Json = 2,

#if !ULTIMATERPLAY_DISABLE_BSON
        /// <summary>
        /// The replay system with use bson file format.
        /// </summary>
        Bson,
#endif
    }

    public abstract partial class ReplayStreamStorage : ReplayStorage
    {
        // Private
        private ReplayStreamHeader header = default;
        private ReplaySegmentTable segmentTable = new ReplaySegmentTable();
        private Dictionary<int, ReplaySegment> loadedSegments = new Dictionary<int, ReplaySegment>();
                
        private ReplaySegment writeSegment = null;
        private int writeSegmentId = 1;

#if !ULTIMATEREPLAY_DISABLE_THREADING
        private Queue<Action> threadTasks = new Queue<Action>();
        private bool threadRunning = true;
#endif

        // Protected
        protected bool useSegmentCompression = true;
        protected int snapshotsPerSegment = 30;
        protected Stream writeStream = null;
        protected Stream readStream = null;

        // Properties
        protected abstract ReplayStreamSource StreamSource { get; }

        public override bool CanRead
        {
            get
            {
                CheckDisposed();
                return StreamSource.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                CheckDisposed();
                return StreamSource.CanWrite;
            }
        }

        public override float Duration
        {
            get
            {
                CheckDisposed();
                return header.duration;
            }
        }

        public override int MemorySize
        {
            get
            {
                CheckDisposed();
                return header.memorySize;
            }
        }

        public override int SnapshotSize
        {
            get
            {
                CheckDisposed();
                return header.snapshotCount;
            }
        }

        public override int IdentitySize
        {
            get
            {
                CheckDisposed();
                return header.identityByteSize;
            }
        }

        public bool IsBuffering
        {
            get
            {
                CheckDisposed();

#if !ULTIMATEREPLAY_DISABLE_THREADING
                return threadTasks.Count > 0;
#else
                return false;
#endif
            }
        }

        // Constructor
        protected ReplayStreamStorage(string replayName = null, bool useSegmentCompression = false)
            : base(replayName)
        {
            this.useSegmentCompression = useSegmentCompression;

#if !ULTIMATEREPLAY_DISABLE_THREADING
            // Start running user task
            ThreadPool.QueueUserWorkItem(StreamingThreadMain);
#endif
        }

        // Methods
        protected abstract void ThreadWriteReplayHeader(ReplayStreamHeader header);
        protected abstract void ThreadWriteReplaySegment(ReplaySegment segment);
        protected abstract void ThreadWriteReplaySegmentTable(ReplaySegmentTable table);
        protected abstract void ThreadWriteReplayPersistentData(ReplayPersistentData data);
        protected abstract void ThreadWriteReplayMetadata(ReplayMetadata metadata);        

        protected abstract void ThreadReadReplayHeader(ref ReplayStreamHeader header);
        protected abstract void ThreadReadReplaySegment(ref ReplaySegment segment, int segmentID);
        protected abstract void ThreadReadReplaySegmentTable(ref ReplaySegmentTable table);
        protected abstract void ThreadReadReplayPersistentData(ref ReplayPersistentData data);
        protected abstract void ThreadReadReplayMetadata(Type metadataType, ref ReplayMetadata metadata);

        protected virtual void OnStreamOpenWrite(Stream writeStream) { }
        protected virtual void OnStreamOpenRead(Stream readStream) { }
        protected virtual void OnStreamCommit(Stream writeStream) { }

        protected virtual void OnStreamSeek(Stream stream, long offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
        }

        public void LoadStreamCompletely()
        {
            // Check for locked
            if (IsLocked == true)
                throw new InvalidOperationException("This replay storage is currently being used by a replay operation");

            // Prepare for read
            Prepare(ReplayStorageAction.Read);

            // Request all segments to be loaded
            for (int i = 0; i < SnapshotSize; i++)
                FetchSnapshot(i);
        }

        public ReplayAsyncOperation LoadStreamCompletelyAsync()
        {
            // Create async
            ReplayAsyncOperation async = new ReplayAsyncOperation();

            // Call internal
            LoadStreamCompletelyAsync(async);

            return async;
        }

        internal void LoadStreamCompletelyAsync(ReplayAsyncOperation async)
        {
#if ULTIMATEREPLAY_DISABLE_THREADING
            throw new NotSupportedException("Async operations are not supported");
#else

            // Check for locked
            if (IsLocked == true)
                throw new InvalidOperationException("This replay storage is currently being used by a replay operation");

            // Prepare for read
            Prepare(ReplayStorageAction.Read);

            // Run async operation
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                try
                {
                    // Request all segments to be loaded
                    for (int i = 0; i < SnapshotSize; i++)
                    {
                        // Request snapshot
                        FetchSnapshot(i);

                        // Update progress
                        async.UpdateProgress(UnityEngine.Mathf.InverseLerp(0, SnapshotSize, i));
                    }
                }
                catch(Exception e)
                {
                    async.Complete(false, e.ToString());
                    return;
                }

                // Mark as completed
                async.Complete(true);
            });
#endif
        }

        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            // Check for disposed
            CheckDisposed();

            // Check for read ready
            if (readStream == null)
                throw new InvalidOperationException("Stream storage is not ready to accept read calls. You must prepare the storage target for reading first!");

            // Check for out of bounds - quick return
            if (sequenceID < 1)
                return null;

            // Get segment id from sequence Id
            int segmentId = segmentTable.GetSegmentId(sequenceID);

            // Check for invalid
            if (segmentId == -1)
                return null;

            // Check for already loaded
            ReplaySegment segment;

            // Check for cached
            if (loadedSegments.TryGetValue(segmentId, out segment) == true)
                return segment.FetchSnapshot(sequenceID);


            // We need to load the segment - Wait for this request by blocking main thread
            // Results will be placed into 'loadedSegments' once completed
            StreamingReadReplaySegment(segmentId, true);

            // Get the segment
            if (loadedSegments.TryGetValue(segmentId, out segment) == true)
                return segment.FetchSnapshot(sequenceID);

            // Snapshot could not be found
            return null;
        }

        public override ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            // Check for disposed
            CheckDisposed();

            // Check for read ready
            if (readStream == null)
                throw new InvalidOperationException("Stream storage is not ready to accept read calls. You must prepare the storage target for reading first!");

            // Get segment id from timestamp
            int segmentId = segmentTable.GetSegmentId(timeStamp, header.duration);

            // Check for invalid
            if (segmentId == -1)
                return null;

            // Check for already loaded
            ReplaySegment segment;

            // Check for cached
            if (loadedSegments.TryGetValue(segmentId, out segment) == true)
                return segment.FetchSnapshot(timeStamp);


            // We need to load the segment - Wait for this request by blocking main thread
            // Results will be placed into 'loadedSegments' once completed
            StreamingReadReplaySegment(segmentId, true);

            // Get the segment
            if (loadedSegments.TryGetValue(segmentId, out segment) == true)
                return segment.FetchSnapshot(timeStamp);

            // Snapshot could not be found
            return null;
        }

        public override void StoreSnapshot(ReplaySnapshot state)
        {
            // Check for disposed
            CheckDisposed();

            // Check for null
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            // Check for write ready
            if (writeStream == null)
                throw new InvalidOperationException("Stream storage is not ready to accept write calls. You must prepare the storage target for writing first!");

            // Update size
            header.memorySize += state.Size;
            header.duration = state.TimeStamp;
            header.snapshotCount++;

            // Add to segment
            if(writeSegment.IsFull == true)
            {
                // Flush - Send write call to worker thread
                StreamingWriteReplaySegment(writeSegment);

                // Create new segment
                writeSegmentId++;
                //writeSegment = new ReplaySegment(writeSegmentId, snapshotsPerSegment); // TODO - Fetch instance from pool??
                writeSegment = ReplaySegment.pool.GetReusable();
                writeSegment.SegmentID = writeSegmentId;
                writeSegment.SnapshotCapacity = snapshotsPerSegment;
            }

            // Add to segment
            writeSegment.AddSnapshot(state);
        }

        public override void Prepare(ReplayStorageAction mode)
        {
            // Check for disposed
            CheckDisposed();

            switch (mode)
            {
                case ReplayStorageAction.Write:
                    {
                        // Prepare for stream write
                        PrepareStreamWrite();

                        // Create header
                        header = new ReplayStreamHeader();
                        header.fileIdentifier = ReplayStreamHeader.replayIdentifier;
                        header.version = ReplayStreamHeader.replayVersion;
                        header.identityByteSize = (ushort)ReplayIdentity.byteSize;

                        // Setup segments
                        writeSegmentId = 1;
                        writeSegment = new ReplaySegment(writeSegmentId, snapshotsPerSegment);

                        // Write placeholder head to file
                        StreamingWriteReplayHeader(header);
                        break;
                    }

                case ReplayStorageAction.Read:
                    {
                        // Prepare for stream reading
                        PrepareStreamRead();

                        // Fetch header, segment table, persistent data and metadata
                        StreamingReadReplayHeader(false);
                        StreamingReadReplaySegmentTable(false);
                        StreamingReadReplayPersistentData(false);
                        StreamingReadReplayMetadata(false);

                        // Wait for all requests to complete
                        StreamingWaitForBuffering();
                        break;
                    }

                case ReplayStorageAction.Discard:
                    {
                        // Cancel any operations
                        StreamingCancelBuffering();

                        // Discard stored data
                        header = new ReplayStreamHeader();
                        segmentTable = new ReplaySegmentTable();
                        persistentData = new ReplayPersistentData();

                        // Clear loaded segments
                        lock(loadedSegments)
                        {
                            foreach(ReplaySegment segment in loadedSegments.Values)
                            {
                                segment.Dispose();
                            }

                            loadedSegments.Clear();
                        }

                        // Dispose streams
                        StreamSource.Dispose();
                        break;
                    }

                case ReplayStorageAction.Commit:
                    {
                        // Write final segment
                        if (writeSegment != null && writeSegment.SnapshotCount > 0)
                            StreamingWriteReplaySegment(writeSegment);

                        // Wait for all streaming to complete
                        StreamingWaitForBuffering();

                        // Get segment table offset
                        int segmentTableStart = (int)writeStream.Position;

                        // Write segment table
                        StreamingWriteReplaySegmentTable(segmentTable);
                        StreamingWaitForBuffering();

                        // Get persistent data offset
                        int persistentDataStart = (int)writeStream.Position;

                        // Write persistent data
                        StreamingWriteReplayPersistentData(persistentData);
                        StreamingWaitForBuffering();

                        // Get metadata offset
                        int metadataOffset = (int)writeStream.Position;

                        // Write metadata
                        StreamingWriteReplayMetadata(metadata);
                        StreamingWaitForBuffering();


                        // Update header
                        header.segmentTableOffset = segmentTableStart;
                        header.persistentDataOffset = persistentDataStart;
                        header.metadataOffset = metadataOffset;

                        // Write header
                        OnStreamSeek(writeStream, 0);
                        StreamingWriteReplayHeader(header);
                        StreamingWaitForBuffering();

                        // Trigger commit
                        if(writeStream != null)
                            OnStreamCommit(writeStream);

                        // Close streams
                        StreamSource.Dispose();
                        break;
                    }
            }
        }

        private void PrepareStreamWrite()
        {
            // Open for writing
            writeStream = StreamSource.OpenWrite();

            // Trigger event
            OnStreamOpenWrite(writeStream);
        }

        private void PrepareStreamRead()
        {
            // Open for reading
            readStream = StreamSource.OpenRead();

            // Trigger event
            OnStreamOpenRead(readStream);

            // Initialize header
            header = new ReplayStreamHeader();
        }

        protected override void OnDispose()
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            // Unset flag so thread can exit
            threadRunning = false;
#endif

            // Cancel for streaming
            StreamingCancelBuffering();

            // Clear loaded segments
            lock (loadedSegments)
            {
                foreach (ReplaySegment segment in loadedSegments.Values)
                {
                    segment.Dispose();
                }

                loadedSegments = null;
            }

            // Release file
            StreamSource.Dispose();
        }

        #region WorkerApi
        private void StreamingWaitForBuffering()
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            // Rest main thread until buffering completes
            while (IsBuffering == true)
                Thread.Sleep(1);
#endif
        }

        private void StreamingCancelBuffering()
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock(threadTasks)
            {
                threadTasks.Clear();
            }
#endif
        }

        private void StreamingWriteReplayHeader(ReplayStreamHeader header)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue write task
                threadTasks.Enqueue(() => ThreadWriteReplayHeader(header));
            }
#else
            ThreadWriteReplayHeader(header);
#endif
        }

        private void StreamingWriteReplaySegment(ReplaySegment segment)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock(threadTasks)
            {
                // Enqueue write segment - segment reference should not be used on main thread after this call
                threadTasks.Enqueue(() =>
                {
                    // Create entry in segment table
                    segmentTable.AddSegment(new ReplaySegmentEntry
                    {
                        segmentId = segment.SegmentID,
                        startSequenceId = segment.StartSequenceID,
                        endSequenceId = segment.EndSequenceID,
                        startTimeStamp = segment.StartTimeStamp,
                        endTimeStamp = segment.EndTimeStamp,
                        streamOffset = (int)writeStream.Position,
                    });

                    // Check for compress
                    if (useSegmentCompression == true)
                        segment.CompressSegment();

                    // Write segment to stream
                    ThreadWriteReplaySegment(segment);

                    // Release the segment for reuse
                    segment.Dispose();
                });
            }
#else
            // Create entry in segment table
            segmentTable.AddSegment(new ReplaySegmentEntry
            {
                segmentId = segment.SegmentID,
                startSequenceId = segment.StartSequenceID,
                endSequenceId = segment.EndSequenceID,
                startTimeStamp = segment.StartTimeStamp,
                endTimeStamp = segment.EndTimeStamp,
                streamOffset = (int)writeStream.Position,
            });

            // Check for compress
            if (useSegmentCompression == true)
                segment.CompressSegment();

            // Write segment to stream
            ThreadWriteReplaySegment(segment);

            // Release the segment for reuse
            segment.Dispose();
#endif
        }

        private void StreamingWriteReplaySegmentTable(ReplaySegmentTable table)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue write table - table reference should not be used on main thread after this call
                threadTasks.Enqueue(() => ThreadWriteReplaySegmentTable(table));
            }
#else
            ThreadWriteReplaySegmentTable(table);
#endif
        }

        private void StreamingWriteReplayPersistentData(ReplayPersistentData data)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue write data
                threadTasks.Enqueue(() => ThreadWriteReplayPersistentData(data));
            }
#else
            ThreadWriteReplayPersistentData(data);
#endif
        }

        private void StreamingWriteReplayMetadata(ReplayMetadata metadata)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue write metadata
                threadTasks.Enqueue(() => ThreadWriteReplayMetadata(metadata));
            }
#else
            ThreadWriteReplayMetadata(metadata);
#endif
        }

        private void StreamingReadReplayHeader(bool waitFor = true)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue read header
                threadTasks.Enqueue(() =>
                {
                    // Seek to correct offset
                    OnStreamSeek(readStream, 0);

                    // Fetch header
                    ThreadReadReplayHeader(ref this.header);
                });
            }
#else
            // Seek to correct offset
            OnStreamSeek(readStream, 0);

            // Fetch header
            ThreadReadReplayHeader(ref this.header);
#endif

            // Wait for request
            if (waitFor == true)
                StreamingWaitForBuffering();
        }

        private void StreamingReadReplaySegment(int segmentId, bool waitFor = true)
        {
            // Find segment offset
            int offset = segmentTable.GetSegmentDataOffset(segmentId);

            // Check for valid
            if (offset < 0)
            {
                UnityEngine.Debug.LogWarning("Replay segment was not found: " + segmentId);
                return;
            }

#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue read segment
                threadTasks.Enqueue(() =>
                {
                    // Seek to correct offset
                    OnStreamSeek(readStream, offset);

                    // Create segment
                    ReplaySegment segment = ReplaySegment.pool.GetReusable();

                    // Set deserialize version
                    segment.deserializeVersionContext = header.version;

                    // Fetch segment
                    ThreadReadReplaySegment(ref segment, segmentId);

                    // Decompress the segment
                    if (segment.IsCompressed == true)
                        segment.DecompressSegment();

                    // Cache the segment
                    lock(loadedSegments)
                    {
                        loadedSegments.Add(segment.SegmentID, segment);
                    }
                });
            }
#else
            // Seek to correct offset
            OnStreamSeek(readStream, offset);

            // Create segment
            ReplaySegment segment = ReplaySegment.pool.GetReusable();

            // Fetch segment
            ThreadReadReplaySegment(ref segment, segmentId);

            // Decompress the segment
            if (segment.IsCompressed == true)
                segment.DecompressSegment();

            // Cache the segment
            loadedSegments.Add(segment.SegmentID, segment);
#endif

            // Wait for request
            if (waitFor == true)
                StreamingWaitForBuffering();
        }

        private void StreamingReadReplaySegmentTable(bool waitFor = true)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue read segment table
                threadTasks.Enqueue(() =>
                {
                    // Seek to correct offset
                    OnStreamSeek(readStream, header.segmentTableOffset);

                    // Fetch segment table
                    ThreadReadReplaySegmentTable(ref this.segmentTable);
                });
            }
#else
            // Seek to correct offset
            OnStreamSeek(readStream, header.segmentTableOffset);

            // Fetch segment table
            ThreadReadReplaySegmentTable(ref this.segmentTable);
#endif

            // Wait for request
            if (waitFor == true)
                StreamingWaitForBuffering();
        }

        private void StreamingReadReplayPersistentData(bool waitFor = true)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue read persistent data
                threadTasks.Enqueue(() =>
                {
                    // Seek to correct offset
                    OnStreamSeek(readStream, header.persistentDataOffset);

                    // Fetch persistent data
                    ThreadReadReplayPersistentData(ref this.persistentData);
                });
            }
#else
            // Seek to correct offset
            OnStreamSeek(readStream, header.persistentDataOffset);

            // Fetch persistent data
            ThreadReadReplayPersistentData(ref this.persistentData);
#endif

            // Wait for request
            if (waitFor == true)
                StreamingWaitForBuffering();
        }

        private void StreamingReadReplayMetadata(bool waitFor = true)
        {
#if !ULTIMATEREPLAY_DISABLE_THREADING
            lock (threadTasks)
            {
                // Enqueue read metadata
                threadTasks.Enqueue(() =>
                {
                    // Seek to correct offset
                    OnStreamSeek(readStream, header.metadataOffset);

                    // Fetch the metadata
                    ThreadReadReplayMetadata(typeof(ReplayMetadata), ref this.metadata);
                });
            }
#else
            // Seek to correct offset
            OnStreamSeek(readStream, header.metadataOffset);

            // Fetch the metadata
            ThreadReadReplayMetadata(typeof(ReplayMetadata), ref this.metadata);
#endif

            // Wait for request
            if (waitFor == true)
                StreamingWaitForBuffering();
        }

#if !ULTIMATEREPLAY_DISABLE_THREADING
        private void StreamingThreadMain(object state)
        {
            // Keep running
            while(threadRunning == true)
            {
                // Execute next action
                while(threadTasks.Count > 0)
                {
                    Action threadCall = null;

                    // Important - Only peek the item until the operation is completed because wait operations check that the buffer is not empty
                    // The thread call can be dequeue after it has completed.
                    lock(threadTasks)
                    {
                        // Get next operation
                        threadCall = threadTasks.Peek();
                    }

                    // Execute action
                    try
                    {
                        threadCall();
                    }
                    catch(Exception e)
                    {
                        UnityEngine.Debug.LogError(e);
                    }

                    // Remove item from waiting queue because it has now completed
                    lock(threadTasks)
                    {
                        threadTasks.Dequeue();
                    }
                }

                // Allow time to sleep
                Thread.Sleep(1);
            }
        }
#endif
#endregion

        public string ToJsonString(Encoding encoding = null)
        {
            // Check for locked
            if (IsLocked == true)
                throw new InvalidOperationException("Storage is currently in use by another operation");

            // Setup encoding
            if (encoding == null)
                encoding = Encoding.UTF8;

            // Create memory stream buffer
            using(MemoryStream stream = new MemoryStream())
            {
                // Create temp storage
                using (ReplayStreamStorage tempStorage = FromStreamJson(stream))
                {
                    // Perform copy
                    CopyTo(tempStorage);                    
                }

                // Get string by reading stream from start
                stream.Position = 0;
                return new StreamReader(stream, encoding).ReadToEnd();
            }
        }

        public byte[] ToBytes()
        {
            // Check for locked
            if (IsLocked == true)
                throw new InvalidOperationException("Storage is currently in use by another operation");

            // Create memory stream buffer
            using (MemoryStream stream = new MemoryStream())
            {
                // Create temp storage
                using (ReplayStreamStorage tempStorage = FromStreamBinary(stream))
                {
                    // Perform copy
                    CopyTo(tempStorage);
                }

                // Get bytes
                return stream.ToArray();
            }
        }

        //public ReplayAsyncOperation<byte[]> ToBytesAsync()
        //{
        //    // Check for locked
        //    if (IsLocked == true)
        //        throw new InvalidOperationException("Storage is currently in use by another operation");

        //    // Create temp storage
        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        // Create temp storage
        //        ReplayStreamStorage tempStorage = FromStreamBinary(stream);

        //        // Perform copy
        //        ReplayAsyncOperation async = CopyToAsync(tempStorage);

        //        // Get bytes
        //        return stream.ToArray();
        //    }
        //}

        public static ReplayMetadata ReadMetadataOnly(Stream stream, ReplayStreamType streamType = ReplayStreamType.Default)
        {
            return ReadMetadataOnly<ReplayMetadata>(stream, streamType);
        }

        public static T ReadMetadataOnly<T>(Stream stream, ReplayStreamType streamType = ReplayStreamType.Default) where T : ReplayMetadata
        {
            // Open stream for reading
            using (ReplayStreamStorage storage = FromStream(stream, null, streamType))
            {
                // Quick prepare for read
                storage.PrepareStreamRead();

                // Read necessary data
                storage.StreamingReadReplayHeader(true);
                storage.StreamingReadReplayMetadata(true);

                // Get metadata 
                return storage.Metadata as T;
            }
        }

        public static ReplayAsyncOperation<ReplayMetadata> ReadMetadataOnlyAsync(Stream stream, ReplayStreamType type = ReplayStreamType.Default)
        {
            return ReadMetadataOnlyAsync<ReplayMetadata>(stream, type);
        }

        public static ReplayAsyncOperation<T> ReadMetadataOnlyAsync<T>(Stream stream, ReplayStreamType streamType = ReplayStreamType.Default) where T : ReplayMetadata
        {
#if ULTIMATEREPLAY_DISABLE_THREADING
            throw new NotSupportedException("Async operations are not supported");
#else
            // Open stream for reading
            ReplayStreamStorage storage = FromStream(stream, null, streamType);

            // Prepare for read
            storage.PrepareStreamRead();

            // Request necessary data without blocking
            storage.StreamingReadReplayHeader(false);
            storage.StreamingReadReplayMetadata(false);

            // Create async
            ReplayAsyncOperation<T> async = new ReplayAsyncOperation<T>();

            // Wait until completed
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                // Wait for buffering to complete
                storage.StreamingWaitForBuffering();

                // Mark operation as finished
                async.UpdateResult(storage.metadata as T);
                async.Complete(async.Result != null);
            });

            // Get async
            return async;
#endif
        }

        public static ReplayStreamStorage ReadStreamCompletely(Stream stream, ReplayStreamType streamType = ReplayStreamType.Default)
        {
            // Check for null
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Check for read
            if (stream.CanRead == false)
                throw new ArgumentException("Stream must be readable");

            // Load the stream
            ReplayStreamStorage streamStorage = FromStream(stream, null, streamType);

            // Ensure full loaded
            streamStorage.LoadStreamCompletely();

            return streamStorage;
        }

        public static ReplayStreamStorage ReadBytesCompletely(byte[] bytes)
        {
            return ReadBytesCompletely(bytes, 0, bytes.Length);
        }

        public static ReplayStreamStorage ReadBytesCompletely(byte[] bytes, int index, int count)
        {
            // Check for no bytes
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            // Load the stream
            ReplayStreamStorage streamStorage = FromStreamBinary(new MemoryStream(bytes, index, count));

            // Ensure fully loaded
            streamStorage.LoadStreamCompletely();

            return streamStorage;
        }

        public static ReplayAsyncOperation<ReplayStreamStorage> ReadStreamCompletelyAsync(Stream stream, ReplayStreamType streamType = ReplayStreamType.Default)
        {
            // Check for null
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Check for read
            if (stream.CanRead == false)
                throw new ArgumentException("Stream must be readable");

            // Load the stream
            ReplayStreamStorage streamStorage = FromStream(stream, null, streamType);

            // Create async
            ReplayAsyncOperation<ReplayStreamStorage> async = new ReplayAsyncOperation<ReplayStreamStorage>(streamStorage);

            // Request load completely non-blocking
            streamStorage.LoadStreamCompletelyAsync(async);

            return async;
        }

        public static ReplayAsyncOperation<ReplayStreamStorage> ReadBytesCompletelyAsync(byte[] bytes)
        {
            return ReadBytesCompletelyAsync(bytes, 0, bytes.Length);
        }

        public static ReplayAsyncOperation<ReplayStreamStorage> ReadBytesCompletelyAsync(byte[] bytes, int index, int count)
        {
            // Check for no bytes
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            // Load the stream
            ReplayStreamStorage streamStorage = FromStreamBinary(new MemoryStream(bytes, index, count));

            // Create async
            ReplayAsyncOperation<ReplayStreamStorage> async = new ReplayAsyncOperation<ReplayStreamStorage>(streamStorage);

            // Request load completely non-blocking
            streamStorage.LoadStreamCompletelyAsync(async);

            return async;
        }

        public static ReplayStreamStorage FromStream(Stream stream, string replayName = null, ReplayStreamType streamType = ReplayStreamType.Default, bool useSegmentCompression = true, CompressionLevel blockCompressionLevel = CompressionLevel.Optimal, bool includeOptionalProperties = false)
        {
            // Check for no stream
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Check for other stream type
            switch(streamType)
            {
                case ReplayStreamType.Json:
                    {
                        // Create json stream
                        return FromStreamJson(stream, replayName, includeOptionalProperties);
                    }

#if !ULTIMATERPLAY_DISABLE_BSON
                case ReplayStreamType.Bson:
                    {
                        // Create bson stream
                        return FromStreamBson(stream, replayName, includeOptionalProperties);
                    }
#endif
            }

            // Use binary by default
            return FromStreamBinary(stream, replayName, useSegmentCompression, blockCompressionLevel);
        }

        public static ReplayStreamStorage FromStreamBinary(Stream stream, string replayName = null, bool useSegmentCompression = true, CompressionLevel blockCompressionLevel = CompressionLevel.Optimal)
        {
            // Check for no stream
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Create binary storage
            return new ReplayBinaryStreamStorage(ReplayStreamSource.FromStream(stream), replayName, useSegmentCompression, blockCompressionLevel);
        }

        public static ReplayStreamStorage FromStreamJson(Stream stream, string replayName = null, bool includeOptionalProperties = false)
        {
            // Check for no stream
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

#if ULTIMATEREPLAY_ENABLE_JSON
            // Create json storage
            return new ReplayJsonStreamStorage(ReplayStreamSource.FromStream(stream), replayName, includeOptionalProperties);
#else
            throw new NotSupportedException("JSON support is not enabled. Please import Json.Net and add `ULTIMATEREPLAY_ENABLE_JSON` in the player settings");
#endif
        }

#if !ULTIMATERPLAY_DISABLE_BSON
        public static ReplayStreamStorage FromStreamBson(Stream stream, string replayName = null, bool includeOptionalProperties = false)
        {
            // Check for no stream
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

#if ULTIMATEREPLAY_ENABLE_JSON
            // Create json storage
            return new ReplayJsonStreamStorage(ReplayStreamSource.FromStream(stream), replayName, includeOptionalProperties, true);
#else
            throw new NotSupportedException("JSON support is not enabled. Please import Json.Net and add `ULTIMATEREPLAY_ENABLE_JSON` in the player settings");
#endif
        }
#endif

            public static ReplayStreamStorage FromJsonString(string json, Encoding encoding = null)
        {
            // Check for empty json
            if(string.IsNullOrEmpty(json) == true)
                throw new ArgumentException(nameof(json) + " cannot be null or empty");

            // Check for no encoding
            if(encoding == null)
                encoding = Encoding.UTF8;

            // Get bytes
            byte[] bytes = encoding.GetBytes(json);

            // Create json from memory stream
            return FromStreamJson(new MemoryStream(bytes));
        }

        public static ReplayStreamStorage FromBytes(byte[] bytes)
        {
            return FromBytes(bytes, 0, bytes.Length);
        }

        public static ReplayStreamStorage FromBytes(byte[] bytes, int index, int count)
        {
            // Check for no bytes
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            // Create from memory stream
            return FromStreamBinary(new MemoryStream(bytes, index, count));
        }
    }
}
