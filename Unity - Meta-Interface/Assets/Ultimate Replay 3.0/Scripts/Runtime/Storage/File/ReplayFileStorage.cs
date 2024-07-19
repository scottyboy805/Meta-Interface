using System;
using System.IO;
using System.IO.Compression;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// The type of replay file to create or read.
    /// <see cref="ReplayFileType.Binary"/> is the default.
    /// </summary>
    public enum ReplayFileType
    {
        /// <summary>
        /// The replay system will select a file format based on file extension.
        /// </summary>
        FromExtension = 0,
        /// <summary>
        /// The replay system will use a high performance binary file format for best performance and storage requirements.
        /// </summary>
        Binary = 1,
        /// <summary>
        /// The replay system will use a human readable json file format for the replay. Useful for working with replay files in other applications.
        /// </summary>
        Json = 2,

#if !ULTIMATERPLAY_DISABLE_BSON
        /// <summary>
        /// The replay system will use the bson file format.
        /// </summary>
        Bson,
#endif
    }

    /// <summary>
    /// A replay storage container intended for writing or reading replay data via file IO.
    /// </summary>
    public abstract class ReplayFileStorage : ReplayStorage
    {
        // Private
        private string filePath = "";
        private ReplayStreamStorage stream = null;

        // Properties
        /// <summary>
        /// The path of the file to be created or read from.
        /// </summary>
        public string FilePath
        {
            get 
            {
                CheckDisposed();
                return filePath; 
            }
        }

        /// <summary>
        /// The name of the file to be created or read from.
        /// </summary>
        public string FileName
        {
            get
            {
                CheckDisposed();
                return Path.GetFileName(filePath);
            }
        }

        /// <summary>
        /// Get the duration of the replay.
        /// </summary>
        public override float Duration
        {
            get { return stream.Duration; }
        }

        /// <summary>
        /// Get the total memory size that of the stored replay data.
        /// </summary>
        public override int MemorySize
        {
            get { return stream.MemorySize; }
        }

        /// <summary>
        /// Get the total number of snapshots stored in this replay.
        /// </summary>
        public override int SnapshotSize
        {
            get { return stream.SnapshotSize; }
        }

        /// <summary>
        /// Get the number of bytes that replay identities are stored as. 
        /// Default is 2 bytes to reduce storage space.
        /// </summary>
        public override int IdentitySize
        {
            get { return stream.IdentitySize; }
        }

        /// <summary>
        /// Check if the background streaming thread is currently fetching any data from the file.
        /// </summary>
        public bool IsBuffering
        {
            get { return stream.IsBuffering; }
        }

        /// <summary>
        /// Get a value indicating whether this storage target is readable.
        /// Value will be true if the specified file exists.
        /// </summary>
        public override bool CanRead
        {
            get { return stream.CanRead; }
        }

        /// <summary>
        /// Get a value indicating whether this storage target is writable.
        /// Value will be true if the file path is valid and accessible.
        /// </summary>
        public override bool CanWrite
        {
            get { return stream.CanWrite; }
        }

        /// <summary>
        /// The metadata stored with this replay file.
        /// </summary>
        public override ReplayMetadata Metadata
        {
            get { return stream.Metadata; }
            set { stream.Metadata = value; }
        }

        /// <summary>
        /// The persistent data stored with this replay file.
        /// </summary>
        public override ReplayPersistentData PersistentData
        {
            get { return stream.PersistentData; }
            set { stream.PersistentData = value; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="filePath">The path of the replay file to create or read</param>
        /// <param name="stream">The stream storage encapsulated by this file storage</param>
        protected ReplayFileStorage(string filePath, ReplayStreamStorage stream)
            : base(Path.GetFileNameWithoutExtension(filePath))
        {
            this.filePath = filePath;
            this.stream = stream;

            // Check for not supported
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException("File streaming is not supported on this platform");
#endif
            
            // Register stream resource for dispose if user does not dispose manually - avoid file handle leaks
            ReplayCleanupUtility.RegisterUnreleasedResource(this);
        }

        // Methods
        /// <summary>
        /// Force all replay data to be fetched from the file and loaded into memory ready for quick access.
        /// This will block the main thread until loading is completed, and may take some time relative to the size of the file.
        /// </summary>
        public void LoadFileCompletely()
        {
            // Make sure stream is fully loaded
            stream.LoadStreamCompletely();
        }

        /// <summary>
        /// Force all replay data to be fetched from the file and loaded into memory asynchronously.
        /// This will not block thread but the async operation returned can be awaited in a coroutine to determine when loading has completed.
        /// </summary>
        /// <returns>An async operation containing information about the current load state which can be awaited in a coroutine</returns>
        public ReplayAsyncOperation LoadFileCompletelyAsync()
        {
            // Make sure stream is completely loaded
            return stream.LoadStreamCompletelyAsync();
        }

        /// <summary>
        /// Fetch the snapshot with the specified sequence id.
        /// The sequence id represents an ordered numeric identifier starting from 1 and counting upwards.
        /// This may cause the main thread to be blocked for a short period if data has not already been cached in memory and needs to be fetched from file.
        /// The storage must be prepared for reading before fetch calls can be used.
        /// </summary>
        /// <param name="sequenceID">The id of the snapshot to fetch</param>
        /// <returns>The snapshot with the matching sequence id</returns>
        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            return stream.FetchSnapshot(sequenceID);
        }

        /// <summary>
        /// Fetch the best matching snapshots for the specified time stamp.
        /// The time stamp value should be between 0 and <see cref="Duration"/>, but can exceed those bounds.
        /// This may cause the main thread to be blocked for a short period if data has not already been cached in memory and needs to be fetched from file.
        /// The storage must be prepared for reading before fetch calls can be used.
        /// </summary>
        /// <param name="timeStamp">The time stamp value in seconds</param>
        /// <returns>The best matching snapshot for the given time stamp</returns>
        public override ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            return stream.FetchSnapshot(timeStamp);
        }

        /// <summary>
        /// Store ths specified snapshot as part of the replay file.
        /// The storage must be prepared for writing before fetch calls can be used.
        /// </summary>
        /// <param name="state">The snapshot to store in file</param>
        public override void StoreSnapshot(ReplaySnapshot state)
        {
            stream.StoreSnapshot(state);
        }

        /// <summary>
        /// Prepare the storage for the specified action.
        /// Should be used in preparation for reading or writing, or if the file should be finalized.
        /// </summary>
        /// <param name="mode">The action to prepare the storage for</param>
        public override void Prepare(ReplayStorageAction mode)
        {
#if UNITY_EDITOR
            if(mode == ReplayStorageAction.Write)
            {
                // Check for writing in assets folder
                string assetsPath = UnityEngine.Application.dataPath;
                string filePathNormalized = Path.GetFullPath(filePath).Replace('\\', '/');

                if (filePathNormalized.StartsWith(assetsPath) == true)
                    UnityEngine.Debug.LogWarning("It is not recommended to save replay files into the project `Assets` folder as it can cause an infinite import cycle if `Auto Refresh` is enabled. If the file must be placed inside the assets folder then you should first save the replay file to a temp location and then copy the replay file into the assets folder after `StopRecording` has been called");
            }
#endif

            stream.Prepare(mode);
        }

        /// <summary>
        /// Release the encapsulated stream storage along with any open file handles.
        /// </summary>
        protected override void OnDispose()
        {
            stream.Dispose();
        }

        /// <summary>
        /// Read the metadata only from the target replay file.
        /// </summary>
        /// <param name="filePath">The path to the replay file</param>
        /// <returns>The metadata loaded from the target replay file</returns>
        public static ReplayMetadata ReadMetadataOnly(string filePath)
        {
            using(Stream stream = File.OpenRead(filePath))
            {
                return ReplayStreamStorage.ReadMetadataOnly(stream);
            }
        }

        /// <summary>
        /// Load an existing replay file completely into memory in contrast to the default streaming on demand behaviour.
        /// Subsequent read requests such as <see cref="FetchSnapshot(float)"/> will be near instant since all data will be cached in memory.
        /// Note that this method will block the calling thread until the file has been completely loaded into memory. See <see cref="ReadFileCompletelyAsync(string, ReplayFileType)"/> for a non-blocking alternative.
        /// Note that this method is only recommended for relatively small replay files depending upon target device in order to avoid out of memory scenarios.
        /// </summary>
        /// <param name="filePath">The path to the replay file to load</param>
        /// <param name="fileType">The optional type of replay file to load which will be determined from the file extension by default</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The specified file path is null or empty</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        public static ReplayFileStorage ReadFileCompletely(string filePath, ReplayFileType fileType = ReplayFileType.FromExtension)
        {
            // Check for empty
            if (string.IsNullOrEmpty(filePath) == true)
                throw new ArgumentException(nameof(filePath) + " cannot be null or empty");

            // Check for file
            if (File.Exists(filePath) == false)
                throw new FileNotFoundException("Replay file does not exist: " + filePath);

            // Load the file
            ReplayFileStorage fileStorage = FromFile(filePath, fileType);

            // Ensure fully loaded
            fileStorage.LoadFileCompletely();

            return fileStorage;
        }

        public static ReplayAsyncOperation<ReplayFileStorage> ReadFileCompletelyAsync(string filePath, ReplayFileType fileType = ReplayFileType.FromExtension)
        {
            // Check for empty
            if (string.IsNullOrEmpty(filePath) == true)
                throw new ArgumentException(nameof(filePath) + " cannot be null or empty");

            // Check for file
            if (File.Exists(filePath) == false)
                throw new FileNotFoundException("Replay file does not exist: " + filePath);

            // Load the file
            ReplayFileStorage fileStorage = FromFile(filePath, fileType);

            // Create async
            ReplayAsyncOperation<ReplayFileStorage> async = new ReplayAsyncOperation<ReplayFileStorage>(fileStorage);

            // Request load completely non-blocking
            fileStorage.stream.LoadStreamCompletelyAsync(async);

            return async;
        }

        /// <summary>
        /// Create or open or create the specified replay file with the specified format.
        /// </summary>
        /// <param name="filePath">The file path for the target replay file</param>
        /// <param name="fileType">The <see cref="ReplayFileType"/> format of the replay file</param>
        /// <param name="useSegmentCompression">Should the replay file use segment compression to reduce the file size</param>
        /// <param name="blockCompressionLevel">The target block compression level that should be used to reduce the file size further</param>
        /// <returns>A replay file storage target</returns>
        /// <exception cref="ArgumentException">File path is null or empty</exception>
        public static ReplayFileStorage FromFile(string filePath, ReplayFileType fileType = ReplayFileType.FromExtension, bool useSegmentCompression = true, CompressionLevel blockCompressionLevel = CompressionLevel.Optimal, bool includeOptionalProperties = false)
        {
            // Check for empty
            if (string.IsNullOrEmpty(filePath) == true)
                throw new ArgumentException(nameof(filePath) + " cannot be null or empty");

            switch (fileType)
            {
                // Check for any
                case ReplayFileType.FromExtension:
                    {
                        // Check for json extension
                        if (Path.GetExtension(filePath) == ".json")
                            return FromFileJson(filePath);

                        break;
                    }

                // Check for json
                case ReplayFileType.Json:
                    {
                        // Use json file
                        return FromFileJson(filePath, includeOptionalProperties);
                    }

                // Check for binary
                case ReplayFileType.Binary:
                    {
                        // Use binary file
                        return FromFileBinary(filePath, useSegmentCompression, blockCompressionLevel);
                    }

#if !ULTIMATERPLAY_DISABLE_BSON
                case ReplayFileType.Bson:
                    {
                        // Use bson file
                        return FromFileBson(filePath, includeOptionalProperties);
                    }
#endif
            }

            // Default to binary file
            return FromFileBinary(filePath, useSegmentCompression, blockCompressionLevel);
        }

        /// <summary>
        /// Create or open or create the specified replay file which contains a valid Ultimate Replay 3.0 binary format.
        /// </summary>
        /// <param name="filePath">The file path for the target replay file</param>
        /// <param name="useSegmentCompression">Should the replay file use segment compression to reduce the file size</param>
        /// <param name="blockCompressionLevel">The target block compression level that should be used to reduce the file size further</param>
        /// <returns>A replay file storage target</returns>
        /// <exception cref="ArgumentException">File path is null or empty</exception>
        public static ReplayFileStorage FromFileBinary(string filePath, bool useSegmentCompression = true, CompressionLevel blockCompressionLevel = CompressionLevel.Optimal)
        {
            // Check for empty
            if (string.IsNullOrEmpty(filePath) == true)
                throw new ArgumentException(nameof(filePath) + " cannot be null or empty");

            // Create binary
            return new ReplayBinaryFileStorage(filePath, useSegmentCompression, blockCompressionLevel);
        }

        /// <summary>
        /// Create or open the specified replay file which contains a valid Ultimate Replay 3.0 json format.
        /// </summary>
        /// <param name="filePath">The file path for the target replay file</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">File path is null or empty</exception>
        public static ReplayFileStorage FromFileJson(string filePath, bool includeOptionalProperties = false)
        {
            // Check for empty
            if (string.IsNullOrEmpty(filePath) == true)
                throw new ArgumentException(nameof(filePath) + " cannot be null or empty");

#if ULTIMATEREPLAY_ENABLE_JSON
            // Create json
            return new ReplayJsonFileStorage(filePath, includeOptionalProperties);
#else
            throw new NotSupportedException("JSON support is not enabled. Please import Json.Net and add `ULTIMATEREPLAY_ENABLE_JSON` in the player settings");
#endif
        }

#if !ULTIMATERPLAY_DISABLE_BSON
        /// <summary>
        /// Create or open the specified replay file which contains a valid Ultimate Replay 3.0 bson format.
        /// </summary>
        /// <param name="filePath">The file path for the target replay file</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">File path is null or empty</exception>
        public static ReplayFileStorage FromFileBson(string filePath, bool includeOptionalProperties = false)
        {
            // Check for empty
            if (string.IsNullOrEmpty(filePath) == true)
                throw new ArgumentException(nameof(filePath) + " cannot be null or empty");

#if ULTIMATEREPLAY_ENABLE_JSON
            // Create json
            return new ReplayJsonFileStorage(filePath, includeOptionalProperties, true);
#else
            throw new NotSupportedException("JSON support is not enabled. Please import Json.Net and add `ULTIMATEREPLAY_ENABLE_JSON` in the player settings");
#endif
        }
#endif

        /// <summary>
        /// Check if the specified file is a valid Ultimate Replay format file.
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>True if the target file is a valid Ultimate Replay file format</returns>
        public static bool IsReplayFile(string filePath)
        {
            // Check for empty
            if (string.IsNullOrEmpty(filePath) == false)
                return false;

            // Check for exists
            if (File.Exists(filePath) == false)
                return false;

            // Try to open for reading
            using (Stream stream = File.OpenRead(filePath))
            {
                try
                {
                    ReplayStreamStorage.ReadMetadataOnly(stream);
                    return true;
                }
                catch { }
            }
            return false;
        }
    }
}
