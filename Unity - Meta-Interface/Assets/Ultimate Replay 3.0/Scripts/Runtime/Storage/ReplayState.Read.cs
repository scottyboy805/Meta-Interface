using System;
using System.Runtime.CompilerServices;
using System.Text;
using UltimateReplay.Util;
using UnityEngine;

namespace UltimateReplay
{
    public sealed partial class ReplayState
    {
        // Methods
        public T ReadSerializable<T>() where T : IReplaySerialize, new()
        {
            CheckDisposed();

            // Create instance
            T replaySerializable = new T();

            // Deserialize the object
            replaySerializable.OnReplayDeserialize(this);
            return replaySerializable;
        }

        public bool ReadSerializable(IReplaySerialize replaySerializable)
        {
            CheckDisposed();

            // Deserialize the object
            replaySerializable.OnReplayDeserialize(this);
            return true;
        }

        public bool ReadSerializable<T>(ref T replaySerializable) where T : IReplaySerialize
        {
            CheckDisposed();

            // Deserialize the object
            replaySerializable.OnReplayDeserialize(this);
            return true;
        }

        internal ReplayIdentity ReadIdentity()
        {
            ReplayIdentity id = default;
            id.ReadFromState(this);

            return id;
        }

        internal void ReadIdentity(ref ReplayIdentity id)
        {
            id.ReadFromState(this);
        }

        /// <summary>
        /// Read a byte from the state.
        /// </summary>
        /// <returns>Byte value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            // Check for disposed - inline to save overhead of CheckDisposed call
            if(readPointer == -1)
            {
                throw new ObjectDisposedException("The replay state has been disposed");
            }
            else if(dataSize == 0)
            {
                throw new InvalidOperationException("There is no data in the object state");
            }
            else if(readPointer >= dataSize)
            {
                throw new InvalidOperationException("There are not enough bytes in the data to read the specified type");
            }

            // Fetch byte value
            byte value = bytes[readPointer];

            // Advance pointer
            readPointer++;

            return value;
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        /// <summary>
        /// Read a byte array from the state.
        /// </summary>
        /// <param name="amount">The number of bytes to read</param>
        /// <returns>Byte array value</returns>
        public byte[] ReadBytes(int amount)
        {
            // Check for disposed - inline to save overhead of CheckDisposed call
            if (readPointer == -1)
            {
                throw new ObjectDisposedException("The replay state has been disposed");
            }
            else if (dataSize == 0)
            {
                throw new InvalidOperationException("There is no data in the object state");
            }
            else if (readPointer >= dataSize - amount + 1)
            {
                throw new InvalidOperationException("There are not enough bytes in the data to read the specified type");
            }

            // Create return array
            byte[] bytes = new byte[amount];

            // Store bytes
            for (int i = 0; i < amount; i++)
                bytes[i] = this.bytes[readPointer + i];

            // Advance pointer
            readPointer += amount;

            return bytes;
        }

        /// <summary>
        /// Fill a byte array with data from the state.
        /// </summary>
        /// <param name="buffer">The byte array to store data in</param>
        /// <param name="offset">The index offset to start filling the buffer at</param>
        /// <param name="amount">The number of bytes to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(byte[] buffer, int offset, int amount)
        {
            // Check for disposed - inline to save overhead of CheckDisposed call
            if (readPointer == -1)
            {
                throw new ObjectDisposedException("The replay state has been disposed");
            }
            else if (dataSize == 0)
            {
                throw new InvalidOperationException("There is no data in the object state");
            }
            else if (readPointer >= dataSize - amount + 1)
            {
                throw new InvalidOperationException("There are not enough bytes in the data to read the specified type");
            }

            // Fill buffer with bytes
            // Maybe can be replaced with Array.Copy?
            for (int i = offset, j = 0; i < amount; i++, j++)
                buffer[i] = this.bytes[readPointer + j];

            // Advance pointer
            readPointer += amount;
        }

        /// <summary>
        /// Read a short from the state.
        /// </summary>
        /// <returns>Short value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(short));

            // Convert to short
            return BitConverterNonAlloc.GetInt16(sharedBuffer, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(ushort));

            return (ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, 0);
        }

        /// <summary>
        /// Read an int from the state.
        /// </summary>
        /// <returns>Int value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(int));

            // Convert to int
            return BitConverterNonAlloc.GetInt32(sharedBuffer, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(uint));

            // Convert to int
            return (uint)BitConverterNonAlloc.GetInt32(sharedBuffer, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(long));

            // Convert to int
            return BitConverterNonAlloc.GetInt64(sharedBuffer, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        /// <summary>
        /// Read a float from the state.
        /// </summary>
        /// <returns>Float value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingle()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(float));

            // Convert to float
            return BitConverterNonAlloc.GetFloat(sharedBuffer, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(double));

            // Convert to float
            return BitConverterNonAlloc.GetDouble(sharedBuffer, 0);
        }

        /// <summary>
        /// Read a bool from the state.
        /// </summary>
        /// <returns>Bool value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(bool));

            // Convert to bool
            return BitConverterNonAlloc.GetBool(sharedBuffer, 0);
        }

        /// <summary>
        /// Read a string from the state
        /// </summary>
        /// <returns>string value</returns>
        public string ReadString()
        {
            // Read the string size
            short size = ReadInt16();

            // Read the required number of bytes
            byte[] bytes = ReadBytes(size);

            // Decode the string
#if UNITY_WINRT && !UNITY_EDITOR
            return Encoding.UTF8.GetString(bytes);
#else
            return Encoding.Default.GetString(bytes);
#endif
        }
        
        /// <summary>
        /// Read a vector2 from the state.
        /// </summary>
        /// <returns>Vector2 value</returns>
        public Vector2 ReadVector2()
        {
            Vector2 v = default;

            // Read all bytes as block
            ReadBytes(sharedBuffer, 0, sizeof(float) * 2);

            // Decode elements from shared array
            v.x = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 0);
            v.y = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 1);

            // Get vector
            return v;
        }

        /// <summary>
        /// Read a vector3 from the state.
        /// </summary>
        /// <returns>Vector3 value</returns>
        public Vector3 ReadVector3()
        {
            Vector3 v = default;

            // Read all bytes as block
            ReadBytes(sharedBuffer, 0, sizeof(float) * 3);

            // Decode elements from shared array
            v.x = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 0);
            v.y = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 1);
            v.z = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 2);

            // Get vector
            return v;
        }

        /// <summary>
        /// Read a vector4 from the state.
        /// </summary>
        /// <returns>Vector4 value</returns>
        public Vector4 ReadVector4()
        {
            // Quicker than calling ctor
            Vector4 v = default;

            // Read all bytes as block
            ReadBytes(sharedBuffer, 0, sizeof(float) * 4);

            // Decode elements from shared array
            v.x = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 0);
            v.y = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 1);
            v.z = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 2);
            v.w = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 3);

            // Get vector
            return v;
        }

        /// <summary>
        /// Read a quaternion from the state.
        /// </summary>
        /// <returns>Quaternion value</returns>
        public Quaternion ReadQuaternion()
        {
            // Quicker than calling ctor
            Quaternion q = default;

            // Read all bytes as block
            ReadBytes(sharedBuffer, 0, sizeof(float) * 4);

            // Decode elements from shared array
            q.x = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 0);
            q.y = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 1);
            q.z = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 2);
            q.w = BitConverterNonAlloc.GetFloat(sharedBuffer, sizeof(float) * 3);

            // Get quaternion
            return q;
        }

        /// <summary>
        /// Read a color from the state.
        /// </summary>
        /// <returns>Color value</returns>
        public Color ReadColor()
        {
            // Read as color 32 to save space
            return (Color)ReadColor32();
        }

        /// <summary>
        /// Read a color32 from the state.
        /// </summary>
        /// <returns>Color32 value</returns>
        public Color32 ReadColor32()
        {
            // Quicker than calling ctor
            Color32 c = default;

            // Read all bytes
            ReadBytes(sharedBuffer, 0, sizeof(byte) * 4);

            // Fill values
            c.r = sharedBuffer[0];
            c.g = sharedBuffer[1];
            c.b = sharedBuffer[2];
            c.a = sharedBuffer[3];

            return c;
        }

        /// <summary>
        /// Attempts to read a low precision float.
        /// You should only use this method when the value is relatively small (less than 65000) and accuracy is not essential.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <returns>float value</returns>
        public float ReadHalf()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(ushort));

            // Read 16 bits
            ushort decoded = (ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, 0);

            // Convert to full
            return Mathf.HalfToFloat(decoded);
        }

        /// <summary>
        /// Attempts to read a low precision vector2.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <returns>vector2 value</returns>
        public Vector2 ReadVector2Half()
        {
            // Quicker than calling ctor
            Vector2 v = default;

            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(ushort) * 2);

            v.x = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 0));
            v.y = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 1));

            // Create vector
            return v;
        }

        /// <summary>
        /// Attempts to read a low precision vector3.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <returns>vector3 value</returns>
        public Vector3 ReadVector3Half()
        {
            // Quicker than calling ctor
            Vector3 v = default;

            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(ushort) * 3);

            v.x = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 0));
            v.y = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 1));
            v.z = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 2));

            // Create vector
            return v;
        }

        /// <summary>
        /// Attempts to read a low precision vector4.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <returns>vector4 value</returns>
        public Vector4 ReadVector4Half()
        {
            // Quicker than calling ctor
            Vector4 v = default;

            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(ushort) * 4);

            v.x = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 0));
            v.y = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 1));
            v.z = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 2));
            v.w = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 3));

            // Create vector
            return v;
        }

        /// <summary>
        /// Attempts to read a low precision quaternion.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <returns>quaternion value</returns>
        public Quaternion ReadQuaternionHalf()
        {
            // Quicker than calling ctor
            Quaternion q = default;

            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(ushort) * 4);

            q.x = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 0));
            q.y = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 1));
            q.z = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 2));
            q.w = Mathf.HalfToFloat((ushort)BitConverterNonAlloc.GetInt16(sharedBuffer, sizeof(short) * 3));

            // Get quaternion
            return q;
        }
    }
}
