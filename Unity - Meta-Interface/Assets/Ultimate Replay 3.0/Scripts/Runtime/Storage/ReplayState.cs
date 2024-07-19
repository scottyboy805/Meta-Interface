using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UltimateReplay.Lifecycle;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A <see cref="ReplayState"/> allows replay objects to serialize and deserialize their data.
    /// See <see cref="IReplaySerialize"/>. 
    /// </summary>
    public sealed partial class ReplayState : IDisposable, IReplayReusable, IReplaySerialize, IReplaySnapshotStorable, IReplayTokenSerialize
    {
        // Internal
        internal static readonly byte[] sharedBuffer = new byte[maxByteAllocation * 4]; // Support 4 64-bit values in buffer
        internal static readonly byte[] sharedDataBuffer = new byte[4096];

        // Private
        private static readonly ReplayToken token = ReplayToken.Create(nameof(AsHexString), typeof(ReplayState));
        private const int maxByteAllocation = 8; // Support 64 bit types
        private const int defaultByteCapacity = 64;

        private static readonly Dictionary<Type, MethodInfo> serializeMethods = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> deserializeMethods = new Dictionary<Type, MethodInfo>();

        private byte[] bytes = new byte[defaultByteCapacity];
        private long dataHash = -1;
        private int dataSize = 0;
        private int readPointer = 0;

        // Public
        public static readonly ReplayInstancePool<ReplayState> pool = new ReplayInstancePool<ReplayState>(() => new ReplayState());

        // Properties
        /// <summary>
        /// Returns true if the state contains any more data.
        /// </summary>
        public bool CanRead
        {
            get
            {
                CheckDisposed();
                return dataSize > 0;
            }
        }

        /// <summary>
        /// Returns true if the read pointer is at the end of the buffered data or false if there is still data to be read.
        /// </summary>
        public bool EndRead
        {
            get
            {
                CheckDisposed();
                return readPointer >= Size;
            }
        }

        /// <summary>
        /// Returns the size of the object state in bytes.
        /// </summary>
        public int Size
        {
            get
            {
                CheckDisposed();
                return dataSize;
            }
        }

        ReplaySnapshotStorableType IReplaySnapshotStorable.StorageType
        {
            get
            {
                CheckDisposed();
                return ReplaySnapshotStorableType.StateStorage;
            }
        }

        /// <summary>
        /// Get the data hash for the current data stored in this state.
        /// </summary>
        public long DataHash
        {
            get
            {
                // Check for disposed
                CheckDisposed();
                return FastDataHash;
            }
        }
                
        internal long FastDataHash
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Skip disposed check because it causes hash to take 2x as long due to method call overhead
                if (dataHash == -1)
                    dataHash = GetDataHash();

                return dataHash;
            }
        }

        /// <summary>
        /// The current stored data as a hes string representation.
        /// </summary>
        [ReplayTokenSerialize("Raw Data")]
        public string AsHexString
        {
            get
            {
                // Check for easy case
                if (dataSize == 0)
                    return string.Empty;

                // Encode as hex string
                return HexConverter.GetHexString(bytes, 0, dataSize);
            }
            set
            {
                // Clear and reset
                dataSize = 0;
                readPointer = 0;
                dataHash = -1;

                // Check for null of empty
                if (string.IsNullOrEmpty(value) == true)
                    return;

                // Store data size
                dataSize = value.Length / 2;

                // Ensure capacity
                EnsureCapacity(dataSize);

                try
                {
                    // Convert string into data bytes
                    HexConverter.GetHexBytes(value, bytes, 0);
                }
                catch
                {
                    dataSize = 0;
                    readPointer = -1;
                    throw;
                }
            }
        }

        // Constructor
        static ReplayState()
        {
            foreach(MethodInfo declaredMethod in typeof(ReplayState).GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if(declaredMethod.Name == "Write")
                {
                    if (declaredMethod.GetParameters().Length == 1)
                    {
                        // Get param type
                        Type paramType = declaredMethod.GetParameters()[0].ParameterType;

                        // Check for by reference - get type without the by reference qualifier
                        if (paramType.IsByRef == true)
                            paramType = paramType.GetElementType();

                        // Register the parameter type
                        serializeMethods.Add(paramType, declaredMethod);
                    }
                }
                else if(declaredMethod.Name.StartsWith("Read") == true)
                {
                    if(declaredMethod.ReturnType != typeof(void) && declaredMethod.GetParameters().Length == 0 && deserializeMethods.ContainsKey(declaredMethod.ReturnType) == false)
                    {
                        deserializeMethods.Add(declaredMethod.ReturnType, declaredMethod);
                    }
                }
            }
        }

        /// <summary>
        /// Create an empty <see cref="ReplayState"/> that can be written to. 
        /// </summary>
        private ReplayState() { }

        // Methods
        #region TokenSerialize
        IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
        {
            if (token.IsOptional == false || includeOptional == true)
                yield return token;
        }
        #endregion

        void IReplayReusable.Initialize()
        {
            // Mark as not disposed
            readPointer = 0;
            dataHash = -1;
        }

        public void InitializeFromData(byte[] stateData)
        {
            // Check for disposed
            CheckDisposed();

            EnsureCapacity(stateData.Length);

            // Copy data
            Array.Copy(stateData, bytes, stateData.Length);

            dataHash = -1;
        }

        /// <summary>
        /// Release all data stored in this state and return this instance to the pool to be used by another operation.
        /// </summary>
        public void Dispose()
        {
            dataSize = 0;
            readPointer = -1;
            dataHash = -1;

            // Add to awaiting states
            pool.PushReusable(this);
        }

        /// <summary>
        /// Make sure that the state is capable of storing the specified amount of bytes.
        /// </summary>
        /// <param name="size">The total required size in bytes</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int size)
        {
            if(bytes.Length < size)
            {
                // Calculate best size to grow to
                // Not efficient to increase size by one or 2 on every write call
                int newSize = Mathf.Max(size, bytes.Length * 2);

                // Resize and keep data
                Array.Resize(ref bytes, newSize);
            }
        }

        /// <summary>
        /// Prepares the state for read operations by seeking the read pointer back to the start.
        /// </summary>
        public void PrepareForRead()
        {
            // Check for disposed
            CheckDisposed();

            // Reset the read pointer
            readPointer = 0;
        }

        /// <summary>
        /// Clears all buffered data from this <see cref="ReplayState"/> and resets its state.
        /// </summary>
        public void Clear()
        {
            // Check for disposed
            CheckDisposed();

            //bytes.Clear();
            readPointer = 0;
            dataSize = 0;
            dataHash = -1;
        }

        /// <summary>
        /// Get the string representation of this state.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (readPointer == -1 && dataHash == -1)
                return string.Format("ReplayState(<Disposed>)");

            return string.Format("ReplayState({0})", Size);
        }

        /// <summary>
        /// Get the <see cref="ReplayState"/> data as a byte array. 
        /// </summary>
        /// <returns>A byte array of data</returns>
        public byte[] ToArray()
        {
            // Check for disposed
            CheckDisposed();

            // Convert to byte array
            return bytes.ToArray();
        }        

        /// <summary>
        /// Check if the specified state contains the same data as this state.
        /// Not suitable for high performance applications.
        /// Better to compare <see cref="DataHash"/> for better performance if suitable.
        /// </summary>
        /// <param name="other">The state to compare against</param>
        /// <returns>True if the data is equal or false if not</returns>
        public bool IsDataEqual(ReplayState other)
        {
            // Check for disposed
            CheckDisposed();

            return bytes.SequenceEqual(other.bytes);
        }

        /// <summary>
        /// Copy all data to the target <see cref="ReplayState"/>.
        /// All state information such as <see cref="dataHash"/> and <see cref="readPointer"/> will be maintained.
        /// This <see cref="ReplayState"/> must not be empty (Must contain data) otherwise this method will return false.
        /// The destination <see cref="ReplayState"/> must be empty otherwise this method will return false.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns>True if the copy was successful or false if not</returns>
        /// <exception cref="ArgumentNullException">Destination state is null</exception>
        /// <exception cref="ObjectDisposedException">This <see cref="ReplayState"/> or destination <see cref="ReplayState"/> is disposed</exception>
        public bool CopyTo(ReplayState destination)
        {
            // Check for disposed
            CheckDisposed();

            // Check for null
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            // Check destination disposed
            destination.CheckDisposed();

            // Check for no data
            if (dataSize == 0)
                return false;

            // Make sure destination is empty
            if (destination.dataSize != 0)
                return false;

            // Make sure destination is resized
            destination.EnsureCapacity(dataSize);

            // Perform copy
            Array.Copy(bytes, 0, destination.bytes, 0, dataSize);

            // Copy state values
            destination.dataHash = dataHash;
            destination.dataSize = dataSize;
            destination.readPointer = readPointer;

            return true;
        }

        /// <summary>
        /// Append all data from the specified state.
        /// This state will retain all original data and will have the specified data stored in addition.
        /// </summary>
        /// <param name="data">The target state to append</param>
        public void Append(ReplayState data)
        {
            // Add to back
            EnsureCapacity(dataSize + data.dataSize);

            // Perform copy
            Array.Copy(data.bytes, 0, bytes, dataSize, data.dataSize);

            dataHash = -1;
            dataSize += data.dataSize;
        }

        #region Serialize
        void IReplaySerialize.OnReplaySerialize(ReplayState state)
        {
            // Check for disposed
            CheckDisposed();

            if (state == this)
                throw new InvalidOperationException("Source state and target state references are the same");

#if DEBUG
            if(dataSize > ushort.MaxValue)
                Debug.LogWarning("Size overflow: Attempting to store too many bytes (> " +  ushort.MaxValue + ")");
#endif

            state.Write((ushort)dataSize);
            state.Write(bytes, 0, dataSize);
        }

        void IReplaySerialize.OnReplayDeserialize(ReplayState state)
        {
            // Check for disposed
            CheckDisposed();

            if (state == this)
                throw new InvalidOperationException("Source state and target state references are the same");

            // Read size
            int size = state.ReadUInt16();

            // Make sure we have enough capacity
            EnsureCapacity(size);

            // Read bytes into buffer
            state.ReadBytes(bytes, 0, size);

            dataSize = size;
            dataHash = -1;

            // Reset read pointer - PrepareForRead inline
            readPointer = 0;
        }

        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Check for disposed
            CheckDisposed();

            writer.Write((ushort)dataSize);
            writer.Write(bytes, 0, dataSize);
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            // Check for disposed
            CheckDisposed();

            int size = reader.ReadUInt16();

            // Make sure we have enough capacity
            EnsureCapacity(size);

            if (size > 0)
            { 
                // Read into buffer
                reader.Read(bytes, 0, size);

                dataSize = size;
                dataHash = -1;
            }
            else
            {
                dataSize = 0;
                dataHash = -1;
            }

            // Reset read pointer - PrepareForRead inline
            readPointer = 0;
        }
        #endregion

        /// <summary>
        /// Read an additional replay state from the internal stored data.
        /// </summary>
        /// <returns></returns>
        public ReplayState ReadState()
        {
            ReplayState state = pool.GetReusable();

            ushort size = ReadUInt16();

            state.EnsureCapacity(size);

            Array.Copy(bytes, readPointer, state.bytes, 0, size);
            readPointer += size;

            state.dataSize = size;
            return state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetDataHash()
        {
            int p = 16777619;
            long hash = 2166136261L;
            int count = dataSize;

            for (int i = 0, j = count - 1; i < count; i++, j--)
            {
                hash = (hash ^ bytes[i] ^ (i * j)) * p;
            }

            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (readPointer == -1)
                throw new ObjectDisposedException("The replay state has been disposed");
        }

        /// <summary>
        /// Check if the specified type can be serialized into a replay state.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type can be serialized</returns>
        public static bool IsTypeSerializable(Type type)
        {
            // Enums are supported
            if (type.IsEnum == true)
                return true;

            return serializeMethods.ContainsKey(type);
        }

        /// <summary>
        /// Check if the specified generic type can be serialized into a replay state.
        /// </summary>
        /// <typeparam name="T">The generic type to check</typeparam>
        /// <returns>True if the generic type can be serialized</returns>
        public static bool IsTypeSerializable<T>()
        {
            // Enums are supported
            if (typeof(T).IsEnum == true)
                return true;

            return serializeMethods.ContainsKey(typeof(T));
        }

        public static MethodInfo GetSerializeMethod(Type type)
        {
            MethodInfo method = null;
            serializeMethods.TryGetValue(type, out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(type) == true)
                serializeMethods.TryGetValue(typeof(IReplaySerialize), out method);

            return method;
        }

        public static MethodInfo GetSerializeMethod<T>()
        {
            MethodInfo method = null;
            serializeMethods.TryGetValue(typeof(T), out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(typeof(T)) == true)
                serializeMethods.TryGetValue(typeof(IReplaySerialize), out method);

            return method;
        }

        public static MethodInfo GetDeserializeMethod(Type type)
        {
            MethodInfo method = null;
            deserializeMethods.TryGetValue(type, out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(type) == true)
                method = typeof(ReplayState).GetMethod(nameof(ReadSerializable), Type.EmptyTypes).MakeGenericMethod(type);

            return method;
        }

        public static MethodInfo GetDeserializeMethod<T>()
        {
            MethodInfo method = null;
            deserializeMethods.TryGetValue(typeof(T), out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(typeof(T)) == true)
                method = typeof(ReplayState).GetMethod(nameof(ReadSerializable), Type.EmptyTypes).MakeGenericMethod(typeof(T));

            return method;
        }

        public static ReplayState FromByteArray(byte[] rawStateData)
        {
            ReplayState state = pool.GetReusable();

            state.InitializeFromData(rawStateData);

            return state;
        }
    }
}
