using System;
using System.IO;

namespace UltimateReplay.Storage
{
    public static class ReplayStreamUtility
    {
        // Methods
        internal static void StreamSerialize<T>(T item, BinaryWriter writer) where T : IReplayStreamSerialize
        {
            // Serialize the type
            item.OnReplayStreamSerialize(writer);
        }

        internal static void StreamDeserialize<T>(ref T item, BinaryReader reader) where T : IReplayStreamSerialize
        {
            // Deserialize the type
            item.OnReplayStreamDeserialize(reader);
        }
    }
}
