using System;
using System.IO;

namespace UltimateReplay.Storage
{
    internal struct ReplayStatePointer : IReplaySnapshotStorable
    {
        // Internal
        internal byte snapshotOffset;

        // Properties
        public int SnapshotOffset
        {
            get { return snapshotOffset; }
        }

        public ReplaySnapshotStorableType StorageType
        {
            get { return ReplaySnapshotStorableType.StatePointer; }
        }

        // Constructor
        public ReplayStatePointer(int snapshotOffset)
        {
            if (snapshotOffset > byte.MaxValue)
                throw new ArgumentException("Snapshot offset cannot exceed '255'");

            this.snapshotOffset = (byte)snapshotOffset;
        }

        // Methods
        public override string ToString()
        {
            return string.Format("ReplayStatePointer({0})", snapshotOffset);
        }

        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            writer.Write(snapshotOffset);
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            snapshotOffset = reader.ReadByte();
        }
    }
}
