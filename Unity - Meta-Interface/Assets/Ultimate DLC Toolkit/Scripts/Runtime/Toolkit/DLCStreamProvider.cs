using DLCToolkit.Format.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace DLCToolkit
{
    /// <summary>
    /// Represents a data stream source containing DLC content.
    /// </summary>
    public abstract class DLCStreamProvider : IDisposable
    {
        // Type
        #region FileStream
        private sealed class DLCFileStreamProvider : DLCStreamProvider
        {
            // Private
            private string loadPath = null; 
            private List<Stream> uniqueStreams = new List<Stream>();

            // Properties
            public override bool SupportsMultipleStreams => true;
            public override string HintPath => loadPath;

            // Constructor
            internal DLCFileStreamProvider(string loadPath)
            {
                // Check for exists
                if (File.Exists(loadPath) == false)
                    throw new FileNotFoundException("DLC file does not exist: " + loadPath);

                this.loadPath = loadPath;
            }

            // Methods
            public override Stream OpenReadStream()
            {
                // Open directly
                Stream stream = File.OpenRead(loadPath);
                uniqueStreams.Add(stream);
                return stream;
            }

            public override Stream OpenReadStream(long offset, long size)
            {
                // Open directly
                Stream stream = File.OpenRead(loadPath);
                uniqueStreams.Add(stream);

                // Create a sub access stream - Note sub-stream does not dispose base stream
                return new SubStream(stream, offset, size);
            }

            public override void Dispose()
            {
                // Dispose all
                for (int i = 0; i < uniqueStreams.Count; i++)
                    uniqueStreams[i].Dispose();

                // Clear collection
                uniqueStreams.Clear();
            }
        }
        #endregion

        #region DataStream
        private sealed class DLCDataStreamProvider : DLCStreamProvider
        {
            // Private
            private string hintPath = null;
            private byte[] data = null;

            // Properties
            public override bool SupportsMultipleStreams => true;
            public override string HintPath => hintPath;

            // Constructor
            internal DLCDataStreamProvider(byte[] data, string hintPath = null)
            {
                // Check for null
                if (data == null)
                    throw new ArgumentNullException(nameof(data));

                this.data = data;
                this.hintPath = hintPath;
            }

            // Methods
            public override Stream OpenReadStream()
            {
                return new MemoryStream(data, false);
            }

            public override Stream OpenReadStream(long offset, long size)
            {
                return new SubStream(new MemoryStream(data, false), offset, size);
            }

            public override void Dispose()
            {
                // Do nothing - memory streams don't need to be disposed
            }
        }
        #endregion

        #region
        private sealed class DLCRawStreamProvider : DLCStreamProvider
        {
            // Private
            private string hintPath = null;
            private Stream baseStream = null;

            // Properties
            public override bool SupportsMultipleStreams => false;
            public override string HintPath => hintPath;

            // Constructor
            internal DLCRawStreamProvider(Stream baseStream, string hintPath = null)
            {
                // Check for null
                if(baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                // Check for readable
                if (baseStream.CanRead == false)
                    throw new ArgumentException("Base stream must be readable");

                // Check for seekable
                if (baseStream.CanSeek == false)
                    throw new ArgumentException("Base stream must be seekable");

                this.baseStream = baseStream;
                this.hintPath = hintPath;
            }

            // Methods
            public override Stream OpenReadStream()
            {
                return baseStream;
            }

            public override Stream OpenReadStream(long offset, long size)
            {
                return new SubStream(baseStream, offset, size);
            }

            public override void Dispose()
            {
                baseStream.Dispose();
            }
        }
        #endregion


        // Properties
        /// <summary>
        /// Does the stream provider support multiple simultaneous open read streams.
        /// Can improve performance if enabled but each opened stream must have a unique stream/file handle to avoid seek read issues.
        /// </summary>
        public abstract bool SupportsMultipleStreams { get; }

        /// <summary>
        /// Get the path where the DLC content was sourced from.
        /// </summary>
        public abstract string HintPath { get; }

        // Methods
        /// <summary>
        /// Try to open the DLC content stream for reading.
        /// </summary>
        /// <returns>An open readable stream that supports seeking</returns>
        public abstract Stream OpenReadStream();

        /// <summary>
        /// Try to open the DLC content stream for reading with the specified stream offset and size bounds.
        /// The returned streams position '0' must represent the given offset for example.
        /// </summary>
        /// <param name="offset">The offset into the base stream</param>
        /// <param name="size">The size of the data stream</param>
        /// <returns></returns>
        public abstract Stream OpenReadStream(long offset, long size);

        /// <summary>
        /// Dispose of any open streams used by this stream provider.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Create a stream provider from the specified stream.
        /// Use <see cref="FromData(byte[], string)"/> or <see cref="FromFile(string)"/> where possible as both options support multiple simultaneous read calls for quicker loading.
        /// </summary>
        /// <param name="stream">The stream to load the content from</param>
        /// <param name="hintPath">An optional hint path describing where the stream content originated from</param>
        /// <returns>A stream provider that allows access to the DLC content via a standard API</returns>
        public static DLCStreamProvider FromStream(Stream stream, string hintPath = null)
        {
            return new DLCRawStreamProvider(stream, hintPath);
        }

        /// <summary>
        /// Create a stream provider from the specified dlc file data.
        /// This is recommended over <see cref="FromStream(Stream, string)"/> as it can support multiple simultaneous reads for quicker loading.
        /// </summary>
        /// <param name="data">The data where the dlc content is stored</param>
        /// <param name="hintPath">An optional hint path describing where the stream content originated from</param>
        /// <returns>A stream provider that allows access to the DLC content via a standard API</returns>
        public static DLCStreamProvider FromData(byte[] data, string hintPath = null)
        {
            return new DLCDataStreamProvider(data, hintPath);
        }

        /// <summary>
        /// Create a stream provider from the specified file path.
        /// It is highly recommended to use this option where possible, as reading from file path can support multiple simultaneous read operations to offset quicker load times.
        /// </summary>
        /// <param name="localDLCPath">The file path for the DLC content</param>
        /// <returns>A stream provider that allows access to the DLC content via a standard API</returns>
        public static DLCStreamProvider FromFile(string localDLCPath)
        {
            return new DLCFileStreamProvider(localDLCPath);
        }
    }
}
