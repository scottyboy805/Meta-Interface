using System;
using System.Runtime.CompilerServices;
using System.Text;
using UltimateReplay.Util;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace UltimateReplay
{
    public partial class ReplayState
    {
        public void Write(IReplaySerialize replaySerializable)
        {
            CheckDisposed();

            // Serialize the object
            replaySerializable.OnReplaySerialize(this);
        }

        internal void Write(in ReplayIdentity id)
        {
            // Write to state
            id.WriteToState(this);
        }

        /// <summary>
        /// Write a byte to the state.
        /// </summary>
        /// <param name="value">Byte value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            // Check for disposed - inline to save overhead of CheckDisposed call
            if (readPointer == -1)
            {
                throw new ObjectDisposedException("The replay state has been disposed");
            }

            EnsureCapacity(dataSize + 1);

            bytes[dataSize] = value;
            dataSize++;

            // Reset cached hash because the data has changed
            dataHash = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            Write((byte)value);
        }

        /// <summary>
        /// Write a byte array to the state.
        /// </summary>
        /// <param name="bytes">Byte array value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte[] bytes)
        {
            // Check for disposed - inline to save overhead of CheckDisposed call
            if (readPointer == -1)
            {
                throw new ObjectDisposedException("The replay state has been disposed");
            }

            // Ensure size
            EnsureCapacity(dataSize + bytes.Length);

            // Quicker than adding using loop due to single allocation and Array.Copy optimization
            //Array.Copy(bytes, 0, this.bytes, dataSize, bytes.Length);
            Buffer.BlockCopy(bytes, 0, this.bytes, dataSize, bytes.Length);
            dataSize += bytes.Length;
        }

        /// <summary>
        /// Write a byte array to the state using an offset position and length.
        /// </summary>
        /// <param name="bytes">Byte array value</param>
        /// <param name="offset">The start index to read data from the array</param>
        /// <param name="length">The amount of data to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte[] bytes, int offset, int length)
        {
            // Check for disposed - inline to save overhead of CheckDisposed call
            if (readPointer == -1)
            {
                throw new ObjectDisposedException("The replay state has been disposed");
            }

            // Ensure size
            EnsureCapacity(dataSize + length);

            
            // Copy data
            //Array.Copy(bytes, offset, this.bytes, dataSize, length);
            Buffer.BlockCopy(bytes, offset, this.bytes, dataSize, length);
            dataSize += length;
        }

        /// <summary>
        /// Write a short to the state.
        /// </summary>
        /// <param name="value">Short value</param>
        public void Write(short value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short));
        }

        public void Write(ushort value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, (short)value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short));
        }

        /// <summary>
        /// Write an int to the state.
        /// </summary>
        /// <param name="value">Int value</param>
        public void Write(int value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(int));
        }

        public void Write(uint value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, (int)value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(int));
        }

        public void Write(long value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(long));
        }

        public void Write(ulong value)
        {
            Write((long)value);
        }

        /// <summary>
        /// Write a float to the state.
        /// </summary>
        /// <param name="value">Float value</param>
        public void Write(float value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(float));
        }

        public void Write(double value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(double));
        }

        /// <summary>
        /// Write a bool to the state.
        /// </summary>
        /// <param name="value">bool value</param>
        public void Write(bool value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(bool));
        }

        /// <summary>
        /// Write a string to the state.
        /// </summary>
        /// <param name="value">string value</param>
        public void Write(string value)
        {
            // Get string bytes
#if UNITY_WINRT && !UNITY_EDITOR
            byte[] bytes = Encoding.UTF8.GetBytes(value);
#else
            byte[] bytes = Encoding.Default.GetBytes(value);
#endif

            // Write all bytes
            Write((short)bytes.Length);
            Write(bytes);
        }

        /// <summary>
        /// Write a vector2 to the state.
        /// </summary>
        /// <param name="value">Vector2 value</param>
        public void Write(in Vector2 value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 0, value.x);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 1, value.y);

            // Write all bytes as a block
            Write(sharedBuffer, 0, sizeof(float) * 2);
        }

        /// <summary>
        /// Write a vector3 to the state.
        /// </summary>
        /// <param name="value">Vector3 value</param>
        public void Write(in Vector3 value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 0, value.x);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 1, value.y);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 2, value.z);

            // Write all bytes as a block
            Write(sharedBuffer, 0, sizeof(float) * 3);
        }

        /// <summary>
        /// Write a vector4 to the state.
        /// </summary>
        /// <param name="value">Vector4 value</param>
        public void Write(in Vector4 value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 0, value.x);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 1, value.y);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 2, value.z);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 3, value.w);

            // Write all bytes as a block
            Write(sharedBuffer, 0, sizeof(float) * 4);
        }

        /// <summary>
        /// Write a quaternion to the state.
        /// </summary>
        /// <param name="value">Quaternion value</param>
        public void Write(in Quaternion value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 0, value.x);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 1, value.y);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 2, value.z);
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(float) * 3, value.w);

            // Write all bytes as a block
            Write(sharedBuffer, 0, sizeof(float) * 4);
        }

        /// <summary>
        /// Write a color to the state.
        /// </summary>
        /// <param name="value">Color value</param>
        public void Write(in Color value)
        {
            CheckDisposed();

            // Convert to color 32 to save space
            Write((Color32)value);
        }

        /// <summary>
        /// Write a color32 value to the state.
        /// </summary>
        /// <param name="value">Color32 value</param>
        public void Write(in Color32 value)
        {
            CheckDisposed();

            // Fill buffer
            sharedBuffer[0] = value.r;
            sharedBuffer[1] = value.g;
            sharedBuffer[2] = value.b;
            sharedBuffer[3] = value.a;

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(byte) * 4);
        }

        /// <summary>
        /// Attempts to write a 32 bit float value as a low precision 16 bit representation.
        /// You should only use this method when the value is relatively small (less than 65000).
        /// Accuracy may be lost by storing low precision values.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <param name="value">float value</param>
        public void WriteHalf(float value)
        {
            // Convert to 16 bit
            ushort encoded = Mathf.FloatToHalf(value);

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, 0, (short)encoded);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short));
        }

        /// <summary>
        /// Write a vector2 to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <param name="value">vector2 value</param>
        public void WriteHalf(in Vector2 value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 0, (short)Mathf.FloatToHalf(value.x));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 1, (short)Mathf.FloatToHalf(value.y));
            
            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short) * 2);
        }

        /// <summary>
        /// Write a vector3 to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <param name="value">vector3 value</param>
        public void WriteHalf(in Vector3 value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 0, (short)Mathf.FloatToHalf(value.x));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 1, (short)Mathf.FloatToHalf(value.y));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 2, (short)Mathf.FloatToHalf(value.z));

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short) * 3);
        }

        /// <summary>
        /// Write a vector4 to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <param name="value">vector4 value</param>
        public void WriteHalf(in Vector4 value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 0, (short)Mathf.FloatToHalf(value.x));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 1, (short)Mathf.FloatToHalf(value.y));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 2, (short)Mathf.FloatToHalf(value.z));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 3, (short)Mathf.FloatToHalf(value.w));

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short) * 4);
        }

        /// <summary>
        /// Write a quaternion to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// When read, a half value will almost certainly be within +-0.015f tolerance of the original value.
        /// </summary>
        /// <param name="value">quaternion value</param>
        public void WriteHalf(in Quaternion value)
        {
            CheckDisposed();

            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 0, (short)Mathf.FloatToHalf(value.x));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 1, (short)Mathf.FloatToHalf(value.y));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 2, (short)Mathf.FloatToHalf(value.z));
            BitConverterNonAlloc.GetBytes(sharedBuffer, sizeof(short) * 3, (short)Mathf.FloatToHalf(value.w));

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short) * 4);
        }
    }
}
