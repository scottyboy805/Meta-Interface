using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UltimateReplay.Util
{
    /// <summary>
    /// Used as a union for conversion between common 32 bit data types without the use of unsafe code.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct Common32
    {
        // Private
        private static Common32 conversion = new Common32(); // Use a cached instance

        // Public
        /// <summary>
        /// The value represented as a float.
        /// </summary>
        [FieldOffset(0)]
        public float single;

        /// <summary>
        /// The value represented as an int.
        /// </summary>
        [FieldOffset(0)]
        public int integer;

        // Methods
        /// <summary>
        /// Converts a value from an integer to float.
        /// This is the equivilent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The int value to convert</param>
        /// <returns>The float value result</returns>
        public static float ToSingle(int value)
        {
            conversion.integer = value;
            return conversion.single;
        }

        /// <summary>
        /// Converts a value from a float to an integer.
        /// This is the equivilent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The float value to convert</param>
        /// <returns>The int value result</returns>
        public static int ToInteger(float value)
        {
            conversion.single = value;
            return conversion.integer;
        }
    }

    /// <summary>
    /// Used as a union for conversion between common 64 bit data types without the use of unsafe code.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct Common64
    {
        // Private
        private static Common64 conversion = new Common64(); // Use a cached instance

        // Public
        /// <summary>
        /// The value represented as a double.
        /// </summary>
        [FieldOffset(0)]
        public double single;

        /// <summary>
        /// The value represented as an 64-bit int.
        /// </summary>
        [FieldOffset(0)]
        public long integer;

        // Methods
        /// <summary>
        /// Converts a value from a 64-bit integer to double.
        /// This is the equivalent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The 64-bit integer value to convert</param>
        /// <returns>The double value result</returns>
        public static double ToDouble(long value)
        {
            conversion.integer = value;
            return conversion.single;
        }

        /// <summary>
        /// Converts a value from a double to a 64-bit integer.
        /// This is the equivalent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The double value to convert</param>
        /// <returns>The 64-bit int value result</returns>
        public static long ToInteger(double value)
        {
            conversion.single = value;
            return conversion.integer;
        }
    }

    /// <summary>
    /// Custom implementation of the BitConverter class that does not make any allocations.
    /// This is important as the methods may be called thousands of times per second.
    /// </summary>
    public static class BitConverterNonAlloc
    {
        // Methods
        #region ToBytes     
        /// <summary>
        /// Store a 16 bit int into the specified byte array.
        /// <param name="buffer">The buffer to store the int which must have a size of 2 or greater</param>
        /// <param name="value">The short value to store</param>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(byte[] buffer, int offset, short value)
        {
#if ULTIMATEREPLAY_UNSAFE
            unsafe
            {
                fixed (byte* b = &buffer[offset])
                    *((short*)b) = value;
            }
#else
            unchecked
            {
                buffer[offset + 0] = (byte)((value >> 8) & 0xFF);
                buffer[offset + 1] = (byte)(value & 0xFF);
            }
#endif
        }

        /// <summary>
        /// Store a 32-bit int into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the int which must have a size of 4 or greater</param>
        /// <param name="value">The int value to store</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(byte[] buffer, int offset, int value)
        {
#if ULTIMATEREPLAY_UNSAFE
            unsafe
            {
                fixed (byte* b = &buffer[offset])
                    *((int*)b) = value;
            }
#else
            unchecked
            {
                buffer[offset + 0] = (byte)((value >> 24) & 0xFF);
                buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
                buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
                buffer[offset + 3] = (byte)(value & 0xFF);
            }
#endif
        }

        /// <summary>
        /// Store a 64-bit int into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the int which must have a size of 8 or greater</param>
        /// <param name="value">The int value to store</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(byte[] buffer, int offset, long value)
        {
#if ULTIMATEREPLAY_UNSAFE
            unsafe
            {
                fixed (byte* b = &buffer[offset])
                    *((long*)b) = value;
            }
#else
            unchecked
            {
                buffer[offset + 0] = (byte)((value >> 56) & 0xFF);
                buffer[offset + 1] = (byte)((value >> 48) & 0xFF);
                buffer[offset + 2] = (byte)((value >> 40) & 0xFF);
                buffer[offset + 3] = (byte)((value >> 32) & 0xFF);
                buffer[offset + 4] = (byte)((value >> 24) & 0xFF);
                buffer[offset + 5] = (byte)((value >> 16) & 0xFF);
                buffer[offset + 6] = (byte)((value >> 8) & 0xFF);
                buffer[offset + 7] = (byte)(value & 0xFF);
            }
#endif
        }

        /// <summary>
        /// Store a 32-bit float into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the float which must have a size of 4 or greated</param>
        /// <param name="value">The float value to store</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(byte[] buffer, int offset, float value)
        {
            unchecked
            {
                // Convert the float to a common 32 bit value
                int intValue = Common32.ToInteger(value);

#if ULTIMATEREPLAY_UNSAFE
                unsafe
                {
                    fixed (byte* b = &buffer[offset])
                        *((int*)b) = value;
                }
#else
                buffer[offset + 0] = (byte)((intValue >> 24) & 0xFF);
                buffer[offset + 1] = (byte)((intValue >> 16) & 0xFF);
                buffer[offset + 2] = (byte)((intValue >> 8) & 0xFF);
                buffer[offset + 3] = (byte)(intValue & 0xFF);
#endif
            }
        }

        /// <summary>
        /// Store a 64-bit decimal value into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the value which must have a size of 8 or greater</param>
        /// <param name="value">The value to store</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(byte[] buffer, int offset, double value)
        {
            unchecked
            {
                // Convert the double to a common 64 bit value
                long intValue = Common64.ToInteger(value);

                // Call through
                GetBytes(buffer, offset, intValue);
            }
        }

        /// <summary>
        /// Store an 8-bit bool into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the bool which must have a size of 1 or greater</param>
        /// <param name="value">The bool value to store</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(byte[] buffer, int offset, bool value)
        {
            unchecked
            {
                buffer[offset + 0] = (byte)((value == true) ? 1 : 0);
            }
        }

#endregion

        #region FromBytes
        /// <summary>
        /// Retrieve a 16-bit int from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retrieve the short from which must have a size of 2 or greater</param>
        /// <returns>The unpacked short value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetInt16(byte[] buffer, int offset)
        {
#if ULTIMATEREPLAY_UNSAFE
            unsafe
            {
                fixed (byte* b = &buffer[offset])
                    return *((short*)(b));
            }
#else
            unchecked
            {
                return (short)((buffer[offset + 0] << 8)
                    | buffer[offset + 1]);
            }
#endif
        }

        /// <summary>
        /// Retrieve a 32-bit int from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retrieve the int from which must have a size of 4 or greater</param>
        /// <returns>The unpacked int value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInt32(byte[] buffer, int offset)
        {
#if ULTIMATEREPLAY_UNSAFE
            unsafe
            {
                fixed (byte* b = &buffer[offset])
                    return *((int*)b);
            }
#else
            unchecked
            {
                return (int)((buffer[offset + 0] << 24)
                    | (buffer[offset + 1] << 16)
                    | (buffer[offset + 2] << 8)
                    | buffer[offset + 3]);
            }
#endif
        }

        /// <summary>
        /// Retrieve a 64-bit int from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retrieve the int from which must have a size of 8 or greater</param>
        /// <returns>The unpacked long int value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetInt64(byte[] buffer, int offset)
        {
#if ULTIMATEREPLAY_UNSAFE
            unsafe
            {
                fixed (byte* b = &buffer[offset])
                    return *((long*)b);
            }
#else
            unchecked
            {
                return (long)((buffer[offset + 0] << 56)
                    | (buffer[offset + 1] << 48)
                    | (buffer[offset + 2] << 40)
                    | (buffer[offset + 3] << 32)
                    | (buffer[offset + 4] << 24)
                    | (buffer[offset + 5] << 16)
                    | (buffer[offset + 6] << 8)
                    | buffer[offset + 7]);
            }
#endif
        }

        /// <summary>
        /// Retrieve a 32-bit float from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retrieve the float from which must have a size of 4 or greater</param>
        /// <returns>The unpacked float value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloat(byte[] buffer, int offset)
        {
            int value;

#if ULTIMATEREPLAY_UNSAFE
            unsafe
            {
                fixed (byte* b = &buffer[offset])
                    value = *((int*)b);
            }
#else
            unchecked
            {
                value = (int)((buffer[offset + 0] << 24)
                    | (buffer[offset + 1] << 16)
                    | (buffer[offset + 2] << 8)
                    | buffer[offset + 3]);
            }
#endif

            // Convert to common value
            return Common32.ToSingle(value);
        }

        /// <summary>
        /// Get a 64-bit decimal value from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retrieve the data from which must have a size of 8 or greater</param>
        /// <returns>The unpacked double value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDouble(byte[] buffer, int offset)
        {
            unchecked
            {
                // Call through to read 64 bits of data
                long value = GetInt64(buffer, offset);

                // Convert to common value
                return Common64.ToDouble(value);
            }
        }

        /// <summary>
        /// Retrieve a 8-bit bool from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retrieve the bool from which must have a size of 1 or greater</param>
        /// <returns>The unpacked bool value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBool(byte[] buffer, int offset)
        {
            unchecked
            {
                return buffer[offset] != 0;
            }
        }
#endregion
    }
}
