#if ULTIMATEREPLAY_ENABLE_JSON
using System.IO;

namespace UltimateReplay.Storage
{
    internal sealed class ReplayJsonFileStorage : ReplayFileStorage
    {
        // Constructor
#if !ULTIMATERPLAY_DISABLE_BSON
        public ReplayJsonFileStorage(string filePath, bool includeOptionalProperties = false, bool forceBson = false)
            : base(filePath, new ReplayJsonStreamStorage(ReplayStreamSource.FromFile(filePath), Path.GetFileNameWithoutExtension(filePath), includeOptionalProperties, forceBson == true || string.Compare(Path.GetExtension(filePath), ".bson", true) == 0))
        {
        }
#else
        public ReplayJsonFileStorage(string filePath, bool includeOptionalProperties = false)
            : base(filePath, new ReplayJsonStreamStorage(ReplayStreamSource.FromFile(filePath), Path.GetFileNameWithoutExtension(filePath), includeOptionalProperties))
        {
        }
#endif
    }
}
#endif