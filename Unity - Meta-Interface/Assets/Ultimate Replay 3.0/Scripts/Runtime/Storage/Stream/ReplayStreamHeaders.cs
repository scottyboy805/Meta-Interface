using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace UltimateReplay.Storage
{
    public abstract partial class ReplayStreamStorage
    {
        protected internal class ReplayStreamHeader : IReplayStreamSerialize, IReplayTokenSerialize
        {
            // Private
            private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplayStreamHeader>();

            // Public
            public const int replayIdentifier = ((byte)'U' |
                                                ((byte)'R' << 8) |
                                                ((byte)'3' << 16) |
                                                ((byte)'0' << 24));

            public const int replayVersion = 120;

            /// <summary>
            /// Unique id so that we know we are working with UR3.0 files.
            /// </summary>
            [ReplayTokenSerialize("File Identifier")]
            public int fileIdentifier;
            /// <summary>
            /// The current version of this replay file format
            /// </summary>
            [ReplayTokenSerialize("Version")]
            public int version;
            /// <summary>
            /// The size in bytes required to store a replay identity value. Can be 2 or 4 bytes.
            /// </summary>
            [ReplayTokenSerialize("Identity Byte Size")]
            public ushort identityByteSize;
            /// <summary>
            /// The amount of size in uncompressed bytes that the replay takes up.
            /// </summary>
            public int memorySize;
            /// <summary>
            /// The duration of the replay in seconds.
            /// </summary>
            public float duration;
            /// <summary>
            /// The number of snapshots in the replay.
            /// </summary>
            public int snapshotCount;
            /// <summary>
            /// The stream offset to the segment table. Segment table can map timestamps and sequence Ids to file offsets for replay segments.
            /// </summary>
            public int segmentTableOffset;
            /// <summary>
            /// The stream offset to the persistent data.
            /// </summary>
            public int persistentDataOffset;
            /// <summary>
            /// The stream offset to the metadata.
            /// </summary>
            public int metadataOffset;

            // Properties
            [ReplayTokenSerialize("Memory Size")]
            public string MemorySizeFixedLengthString
            {
                get { return HexConverter.ToHexString(memorySize); }
                set { memorySize = HexConverter.FromHexStringInt32(value); }
            }

            [ReplayTokenSerialize("Duration")]
            public string DurationFixedLengthString
            {
                get { return HexConverter.ToHexString(duration); }
                set { duration = HexConverter.FromHexStringSingle(value); }
            }

            [ReplayTokenSerialize("Snapshot Count")]
            public string SnapshotCountFixedLengthString
            {
                get { return HexConverter.ToHexString(snapshotCount); }
                set { snapshotCount = HexConverter.FromHexStringInt32(value); }
            }

            [ReplayTokenSerialize("Segment Table Offset")]
            public string SegmentTableOffsetFixedLengthString
            {
                get { return HexConverter.ToHexString(segmentTableOffset); }
                set { segmentTableOffset = HexConverter.FromHexStringInt32(value); }
            }

            [ReplayTokenSerialize("Persistent Data Offset")]
            public string PersistentDataOffsetString
            {
                get { return HexConverter.ToHexString(persistentDataOffset); }
                set { persistentDataOffset = HexConverter.FromHexStringInt32(value); }
            }

            [ReplayTokenSerialize("Metadata Offset")]
            public string MetadataOffsetFixedLengthString
            {
                get { return HexConverter.ToHexString(metadataOffset); }
                set { metadataOffset = HexConverter.FromHexStringInt32(value); }
            }

            // Methods
            #region TokenSerialize
            IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
            {
                foreach(ReplayToken token in tokens)
                {
                    if (token.IsOptional == false || includeOptional == true)
                        yield return token;
                }
            }
            #endregion

            #region StreamSerialize
            void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
            {
                writer.Write(replayIdentifier);
                writer.Write(replayVersion);

                writer.Write(identityByteSize);
                writer.Write(memorySize);
                writer.Write(duration);
                writer.Write(snapshotCount);
                writer.Write(segmentTableOffset);
                writer.Write(persistentDataOffset);
                writer.Write(metadataOffset);
            }

            void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
            {
                fileIdentifier = reader.ReadInt32();

                // Check for not a replay file
                if (fileIdentifier != replayIdentifier)
                    throw new FormatException("The specified stream does not contain a valid replay format");

                version = reader.ReadInt32();
                identityByteSize = reader.ReadUInt16();
                memorySize = reader.ReadInt32();
                duration = reader.ReadSingle();
                snapshotCount = reader.ReadInt32();
                segmentTableOffset = reader.ReadInt32();
                persistentDataOffset = reader.ReadInt32();
                metadataOffset = reader.ReadInt32();
            }
            #endregion
        }

        protected struct ReplaySegmentEntry : IReplayTokenSerialize
        {
            // Private
            private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplaySegmentEntry>();

            // Public
            /// <summary>
            /// The unique id of this replay segment.
            /// </summary>
            [ReplayTokenSerialize("Segment ID")]
            public int segmentId;
            /// <summary>
            /// The sequence id of the replay snapshot that is the first entry of this segment.
            /// </summary>
            [ReplayTokenSerialize("Start Sequence ID")]
            public int startSequenceId;
            /// <summary>
            /// The sequence id of the replay snapshot that is the last entry of this segment.
            /// </summary>
            [ReplayTokenSerialize("End Sequence ID")]
            public int endSequenceId;
            /// <summary>
            /// The timestamp of the replay snapshot that is the first entry of this segment.
            /// </summary>
            [ReplayTokenSerialize("Start Time")]
            public float startTimeStamp;
            /// <summary>
            /// The timestamp of the replay snapshot that is the last entry of this segment.
            /// </summary>
            [ReplayTokenSerialize("End Time")]
            public float endTimeStamp;
            /// <summary>
            /// The offset from the start of the stream data where the replay segment is located.
            /// </summary>
            [ReplayTokenSerialize("Stream Offset")]
            public int streamOffset;

            // Methods
            #region TokenSerialize
            IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
            {
                foreach (ReplayToken token in tokens)
                {
                    if (token.IsOptional == false || includeOptional == true)
                        yield return token;
                }
            }
            #endregion
        }

        protected class ReplaySegmentTable : IReplayStreamSerialize, IReplayTokenSerialize
        {
            // Private
            private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplaySegmentTable>();

            [ReplayTokenSerialize("Segment Entries")]
            private Dictionary<int, ReplaySegmentEntry> segmentEntries = new Dictionary<int, ReplaySegmentEntry>();

            // Methods
            #region TokenSerialize
            IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
            {
                foreach (ReplayToken token in tokens)
                {
                    if (token.IsOptional == false || includeOptional == true)
                        yield return token;
                }
            }
            #endregion

            public void AddSegment(ReplaySegmentEntry segment)
            {
                // Check if segment id is already used
                if (segmentEntries.ContainsKey(segment.segmentId) == true)
                    throw new InvalidOperationException("Segment id must be unique");

                // Add the segment info
                segmentEntries.Add(segment.segmentId, segment);
            }

            public int GetSegmentDataOffset(int segmentId)
            {
                ReplaySegmentEntry entry;

                // Check if segment is found
                if (segmentEntries.TryGetValue(segmentId, out entry) == true)
                    return entry.streamOffset;

                // Segment not found
                return -1;
            }

            public int GetSegmentId(int sequenceId)
            {
                // Check for invalid sequence id - quick return
                if (sequenceId < 1)
                    return -1;

                // Check all segments
                foreach(ReplaySegmentEntry entry in segmentEntries.Values)
                {
                    // Check for sequence in bounds
                    if(sequenceId >= entry.startSequenceId && sequenceId <= entry.endSequenceId)
                    {
                        return entry.segmentId;
                    }
                }

                // Sequence id not found
                return -1;
            }

            public int GetSegmentId(float timestamp, float duration)
            {
                // Check for last clip
                if(timestamp >= duration)
                {
                    // Get segment id of the last entry
                    return segmentEntries.Values.Last().segmentId;
                }


                ReplaySegmentEntry best = new ReplaySegmentEntry { segmentId = -1 };

                // Check for better match
                foreach(ReplaySegmentEntry entry in segmentEntries.Values)
                {
                    if(timestamp >= entry.startTimeStamp)
                    {
                        best = entry;

                        // We can stop searching at this point
                        if (timestamp <= entry.endTimeStamp)
                            break;
                    }

                    //// Check for no best match set yet
                    //if (best.segmentId == -1)
                    //    best = entry;

                    //// Check for better time match
                    //if(timestamp >= entry.startTimeStamp)
                    //{
                    //    best = entry;
                    //    break;
                    //}
                }

                // Get the segment id
                return best.segmentId;
            }

            #region Serialize
            void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
            {
                // Segment count
                writer.Write(segmentEntries.Count);

                // Write all segments
                foreach (ReplaySegmentEntry segment in segmentEntries.Values)
                {
                    writer.Write(segment.segmentId);
                    writer.Write(segment.startSequenceId);
                    writer.Write(segment.endSequenceId);
                    writer.Write(segment.startTimeStamp);
                    writer.Write(segment.endTimeStamp);
                    writer.Write(segment.streamOffset);
                }
            }

            void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
            {
                // Read count
                int size = reader.ReadInt32();

                // Clear old data
                segmentEntries.Clear();

                // Read all segments
                for (int i = 0; i < size; i++)
                {
                    int segmentId = reader.ReadInt32();

                    // Add entry
                    segmentEntries[segmentId] = new ReplaySegmentEntry
                    {
                        segmentId = segmentId,
                        startSequenceId = reader.ReadInt32(),
                        endSequenceId = reader.ReadInt32(),
                        startTimeStamp = reader.ReadSingle(),
                        endTimeStamp = reader.ReadSingle(),
                        streamOffset = reader.ReadInt32(),
                    };
                }
            }
            #endregion
        }
    }
}
