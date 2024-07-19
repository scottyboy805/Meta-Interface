using System.IO;
using System.IO.Compression;

namespace UltimateReplay.Storage
{
    internal sealed class ReplayCompressStream
    {
        // Private
        private MemoryStream compressBuffer = new MemoryStream(4096);
        private GZipStream compressStream = null;
        private CompressionLevel compressLevel = CompressionLevel.NoCompression;

        private BinaryWriter compressWriter = null;
        private BinaryReader compressReader = null;

        // Constructor
        public ReplayCompressStream(CompressionLevel level)
        {
            this.compressLevel = level;
        }
        
        // Methods
        public void WriteCompressed(BinaryWriter writer, IReplayStreamSerialize serialize)
        {
            // Writer the serializable
            if(compressLevel == CompressionLevel.NoCompression)
            {
                // Write with no compression
                serialize.OnReplayStreamSerialize(writer);
            }
            else
            {
                // Create compress writer
                compressStream = new GZipStream(compressBuffer, compressLevel, true);
                compressWriter = new BinaryWriter(compressStream);

                // Write with gzip compression
                serialize.OnReplayStreamSerialize(compressWriter);
                
                // Force flush data - Note calling flush does nothing so unfortunately we have to accept the allocations
                compressWriter.Close();


                // Write bytes
                writer.Write((uint)compressBuffer.Length);
                writer.Write(compressBuffer.GetBuffer(), 0, (int)compressBuffer.Length);

                // Recycle buffer
                compressBuffer.SetLength(0);
            }
        }

        public void ReadCompressed(BinaryReader reader, IReplayStreamSerialize serialize)
        {
            // Read the serializable
            if(compressLevel == CompressionLevel.NoCompression)
            {
                // Read with no compression
                serialize.OnReplayStreamDeserialize(reader);
            }
            else
            {
                // Get size
                uint size = reader.ReadUInt32();

                // Resize buffer if required
                if (size > compressBuffer.Capacity)
                    compressBuffer.Capacity = (int)size;

                // Read into buffer
                reader.BaseStream.CopyTo(compressBuffer, (int)size);
                compressBuffer.Position = 0;

                // Need to create zip stream from only a portion of the current input stream, so unfortunately there must be some allocations per segment load
                // Cannot used cached instances since we need to reset the state for every read and there is no API for that. Segments are only read every few seconds by default so should not be a problem.
                compressStream = new GZipStream(compressBuffer, CompressionMode.Decompress, true);
                compressReader = new BinaryReader(compressStream);

                // Read with gzip compress
                serialize.OnReplayStreamDeserialize(compressReader);

                // Recycle buffer
                compressBuffer.SetLength(0);
            }
        }
    }
}
