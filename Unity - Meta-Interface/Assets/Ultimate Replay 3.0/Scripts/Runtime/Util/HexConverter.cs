using System;
using UltimateReplay.Util;

namespace UltimateReplay
{
    public static class HexConverter
    {
        // Methods
        public static string GetHexString(byte[] bytes, int offset, int length)
        {
            // Check for bad offset
            if (offset < 0)
                throw new IndexOutOfRangeException(nameof(offset));

            // Check for bad length
            if (length <= 0)
                throw new ArgumentException("Length must be greater than zero");

            // Check for out of bounds
            if (offset + length >= bytes.Length)
                throw new IndexOutOfRangeException(nameof(length));

            int size = length * 2;

            // Create array
            char[] hex = new char[size];

            // Get start index
            int index = offset;

            for(int i = 0; i < size; i += 2)
            {
                // Get byte
                byte b = bytes[index++];

                // Get hex
                GetHexValue(b, out hex[i], out hex[i + 1]);
            }

            // Create string
            return new string(hex, 0, size);
        }

        public static void GetHexValue(byte val, out char a, out char b)
        {
            // Get high and low values
            int _0 = val / 16;
            int _1 = val % 16;

            // Convert to hex characters
            a = _0 < 10 ? (char)(_0 + '0') : (char)(_0 - 10 + 'A');
            b = _1 < 10 ? (char)(_1 + '0') : (char)(_1 - 10 + 'A');
        }

        public static void GetHexBytes(string hex, byte[] bytes, int offset)
        {
            // Check for no input
            if (hex == null || hex.Length == 0)
                return;

            // Check for invalid hex
            if (hex.Length % 2 == 1)
                throw new ArgumentException("Input hex cannot have an odd number of characters");

            // Get the size
            int size = hex.Length >> 1;

            // Process all characters
            for(int i = 0; i < size; i++)
            {
                bytes[i + offset] = (byte)((GetHexValue(hex[i << 1]) << 4) + (GetHexValue(hex[(i << 1) + 1])));
            }
        }

        public static int GetHexValue(char hex)
        {
            int val = (int)hex;

            // Calculate value
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }


        public static string ToHexString(int value)
        {
            return "0x" + value.ToString("X8");
        }

        public static string ToHexString(float value)
        {
            return ToHexString(new Common32 { single = value }.integer);
        }

        public static int FromHexStringInt32(string hex)
        {
            return Convert.ToInt32(hex, 16);
        }

        public static float FromHexStringSingle(string hex)
        {
            return new Common32 { integer = FromHexStringInt32(hex) }.single;
        }
    }
}
