
using System.IO;
using System.IO.Compression;

namespace UltimateReplay.Storage
{
    internal sealed class ReplayBinaryFileStorage : ReplayFileStorage
    {
        // Constructor
        public ReplayBinaryFileStorage(string filePath, bool useSegmentCompression = true, CompressionLevel blockCompressionLevel = CompressionLevel.Optimal)
            : base(filePath, new ReplayBinaryStreamStorage(ReplayStreamSource.FromFile(filePath), Path.GetFileNameWithoutExtension(filePath), useSegmentCompression, blockCompressionLevel))
        {
        }
    }
}
