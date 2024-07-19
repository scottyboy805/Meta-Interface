using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A replay identity is an essential component in the Ultimate Replay system and is used to identify replay objects between sessions.
    /// Replay identities are assigned at edit time where possible and will never change values.
    /// Replay identities are also use to identify prefab instances that are spawned during a replay.
    /// </summary>
    [Serializable]
    public struct ReplayIdentity : IEquatable<ReplayIdentity>, IReplaySerialize, IReplayStreamSerialize
    {
        // Internal
        internal const int maxGenerateAttempts = 512;
        internal const int unassignedID = 0;

        internal static int deserializeSize = sizeof(ushort);

        // Private
        private static System.Random rand = new System.Random();
        private static List<uint> usedIds = new List<uint>();

        // Store the value in memory as 32-bit but serialize as specified byteSize
        [SerializeField]
        private uint id;

        // Public
        public static readonly ReplayIdentity invalid = new ReplayIdentity(unassignedID);

        /// <summary>
        /// Get the number of bytes that this object uses to represent its id data.
        /// </summary>
#if ULTIMATEREPLAY_REPLAYIDENTITY_32BIT
        public static readonly int byteSize = sizeof(uint);
        public static readonly int maxValue = uint.MaxValue;
#else
        public static readonly int byteSize = sizeof(ushort);
        public static readonly int maxValue = ushort.MaxValue;
#endif

        // Properties
        /// <summary>
        /// Returns true if this id is not equal to <see cref="unassignedID"/>. 
        /// </summary>
        public bool IsValid
        {
            get { return id != unassignedID; }
        }

        public int ID
        {
            get { return (int)id; }
        }

        // Constructor
        /// <summary>
        /// Clear any old data on domain reload.
        /// </summary>
        static ReplayIdentity()
        {
            // Clear the set - it will be repopulated when each identity is initialized
            usedIds.Clear();
        }

        /// <summary>
        /// Create a new instance with the specified id value.
        /// </summary>
        /// <param name="id">The id value to give this identity</param>
        public ReplayIdentity(uint id)
        {
            this.id  = id;

            // Check for out of bounds
            if (id > maxValue)
                throw new InvalidOperationException("Id value `" + id + "` exceeds the maximum allowed value: " + maxValue);
        }

        public ReplayIdentity(ReplayIdentity other)
        {
            this.id = other.id;
        }

        // Methods
        #region InheritedAndOperator
        /// <summary>
        /// Override implementation.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        /// <summary>
        /// Override implementation.
        /// </summary>
        /// <param name="obj">The object to compare against</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is ReplayIdentity)
            {
                return id == ((ReplayIdentity)obj).id;
            }
            return false;
        }

        /// <summary>
        /// IEquateable implementation.
        /// </summary>
        /// <param name="obj">The <see cref="ReplayIdentity"/> to compare against</param>
        /// <returns></returns>
        public bool Equals(ReplayIdentity obj)
        {
            // Compare values
            return id == obj.id;
        }

        /// <summary>
        /// Override implementation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("ReplayIdentity({0})", id);
        }

        /// <summary>
        /// Override equals operator.
        /// </summary>
        /// <param name="a">First <see cref="ReplayIdentity"/></param>
        /// <param name="b">Second <see cref="ReplayIdentity"/></param>
        /// <returns></returns>
        public static bool operator ==(ReplayIdentity a, ReplayIdentity b)
        {
            return a.Equals( b) == true;
        }

        /// <summary>
        /// Override not-equals operator.
        /// </summary>
        /// <param name="a">First <see cref="ReplayIdentity"/></param>
        /// <param name="b">Second <see cref="ReplayIdentity"/></param>
        /// <returns></returns>
        public static bool operator !=(ReplayIdentity a, ReplayIdentity b)
        {
            // Check for not equal
            return a.Equals(b) == false;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteToState(ReplayState state)
        {
            if (byteSize == sizeof(ushort))
            {
                state.Write((ushort)id);
            }
            else if (byteSize == sizeof(uint))
            {
                state.Write(id);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReadFromState(ReplayState state)
        {
            if (deserializeSize == sizeof(ushort))
            {
                id = state.ReadUInt16();
            }
            else if (deserializeSize == sizeof(uint))
            {
                id = state.ReadUInt32();
            }
        }

        void IReplaySerialize.OnReplaySerialize(ReplayState state)
        {
            WriteToState(state);
        }

        void IReplaySerialize.OnReplayDeserialize(ReplayState state)
        {
            ReadFromState(state);
        }

        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            if (byteSize == sizeof(ushort))
            {
                writer.Write((ushort)id);
            }
            else
            {
                writer.Write((uint)id);
            }
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            if(deserializeSize == sizeof(ushort))
            {
                id = reader.ReadUInt16();
            }
            else if(deserializeSize == sizeof(uint))
            {
                id = reader.ReadUInt32();
            }
        }

        public static void RegisterIdentity(ReplayIdentity identity)
        {
            // Register the id
            if(identity.IsValid == true)
                usedIds.Add(identity.id);
        }

        public static void UnregisterIdentity(ReplayIdentity identity)
        {
            // Remove the id
            if (usedIds.Contains(identity.id) == true)
            {
                usedIds.Remove(identity.id);
            }
        }


        internal static void Generate(ref ReplayIdentity identity)
        {
            // Unregister current id
            //UnregisterIdentity(identity);

#if !ULTIMATEREPLAY_REPLAYIDENTITY_32BIT
            ushort next = unassignedID;
            ushort count = 0;

            // Use 2 byte array to create 16 bit int
            byte[] buffer = new byte[2];

#else
            uint next = unassignedID;
            uint count = 0;
#endif

            do
            {
                // Check for long loop
                if (count > maxGenerateAttempts)
                    throw new OperationCanceledException("Attempting to find a unique replay id took too long. The operation was canceled to prevent a long or infinite loop");

#if !ULTIMATEREPLAY_REPLAYIDENTITY_32BIT
                // Randomize the buffer
                rand.NextBytes(buffer);

                // Use random instead of linear
                next = (ushort)(buffer[0] << 8 | buffer[1]);
#else
                // Get random int
                next = (uint)rand.Next();
#endif

                // Keep track of how many times we have tried
                count++;
            }
            // Make sure our set does not contain the id
            while (next == unassignedID || usedIds.Contains(next) == true);

            // Mark the id as used
            usedIds.Add(next);

            // Update identity with unique value
            identity.id = next;     
        }

        internal void LogAllUsed()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < usedIds.Count; i++)
            {
                builder.Append(usedIds[i]);

                if (i < usedIds.Count - 1)
                    builder.Append(", ");
            }
            Debug.Log(builder.ToString());
        }

        public static bool IsIdentityUnique(in ReplayIdentity identity)
        {
            return usedIds.Contains(identity.id) == false;
        }
    }
}
