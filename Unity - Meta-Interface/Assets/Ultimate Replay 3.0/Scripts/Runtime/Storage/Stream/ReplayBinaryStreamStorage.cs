using System;
using System.IO;
using System.IO.Compression;

namespace UltimateReplay.Storage
{
    internal sealed class ReplayBinaryStreamStorage : ReplayStreamStorage
    {
        // Private
        private ReplayStreamSource source = null;
        private BinaryWriter writer = null;
        private BinaryReader reader = null;

        private ReplayCompressStream compressStream = null;
        private CompressionLevel compressLevel = 0;

        // Properties
        protected override ReplayStreamSource StreamSource
        {
            get { return source; }
        }

        // Constructor
        public ReplayBinaryStreamStorage(ReplayStreamSource source, string replayName = null, bool useSegmentCompression = true, CompressionLevel blockCompressionLevel = CompressionLevel.NoCompression)
            : base(replayName, useSegmentCompression)
        {
            this.source = source;
            this.compressLevel = blockCompressionLevel;
        }

        // Methods
        protected override void OnStreamOpenWrite(Stream writeStream)
        {
            this.compressStream = new ReplayCompressStream(compressLevel);
            this.writer = new BinaryWriter(writeStream);
        }

        protected override void OnStreamOpenRead(Stream readStream)
        {
            this.compressStream = new ReplayCompressStream(compressLevel);
            this.reader = new BinaryReader(readStream);
        }

        #region ThreadRead
        protected override void ThreadReadReplayHeader(ref ReplayStreamHeader header)
        {
            // Read from stream
            ((IReplayStreamSerialize)header).OnReplayStreamDeserialize(reader);
        }

        protected override void ThreadReadReplayMetadata(Type metadataType, ref ReplayMetadata metadata)
        {
            // Check for version 120 - Feature metadata type name
            if (deserializeVersionContext >= 120)
            {
                // Read type
                string metaTypeName = reader.ReadString();

                // Check for matching type
                if(metadata == null || metadata.TypeName != metaTypeName)
                {
                    try
                    {
                        // Create custom metadata instance
                        metadata = ReplayMetadata.CreateFromType(metaTypeName);
                    }
                    catch { }
                }
            }

            // Deserialize
            compressStream.ReadCompressed(reader, metadata);
        }

        protected override void ThreadReadReplayPersistentData(ref ReplayPersistentData data)
        {
            // Read data
            compressStream.ReadCompressed(reader, data);
        }

        protected override void ThreadReadReplaySegment(ref ReplaySegment segment, int segmentID)
        {
            // Read segment
            compressStream.ReadCompressed(reader, segment);
        }

        protected override void ThreadReadReplaySegmentTable(ref ReplaySegmentTable table)
        {
            // Read segment table
            compressStream.ReadCompressed(reader, table);
        }
        #endregion

        #region ThreadWrite
        protected override void ThreadWriteReplayHeader(ReplayStreamHeader header)
        {
            // Write header
            ((IReplayStreamSerialize)header).OnReplayStreamSerialize(writer);
        }

        protected override void ThreadWriteReplayMetadata(ReplayMetadata metadata)
        {
            // Write type info
            writer.Write(metadata.TypeName);

            // Write metadata
            compressStream.WriteCompressed(writer, metadata);
        }

        protected override void ThreadWriteReplayPersistentData(ReplayPersistentData data)
        {
            // Write persistent data
            compressStream.WriteCompressed(writer, data);
        }

        protected override void ThreadWriteReplaySegment(ReplaySegment segment)
        {
            // Write segment
            compressStream.WriteCompressed(writer, segment);
        }

        protected override void ThreadWriteReplaySegmentTable(ReplaySegmentTable table)
        {
            // Write segment table
            compressStream.WriteCompressed(writer, table);
        }
        #endregion
    }
}
