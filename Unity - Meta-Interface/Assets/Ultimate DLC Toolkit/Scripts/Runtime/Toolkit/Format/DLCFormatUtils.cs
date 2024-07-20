using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DLCToolkit.Format
{
    internal static class DLCFormatUtils
    {
        // Type
        [Flags]
        private enum DLCVersionFlags : ushort
        {
            Build = 1,
            Revision = 2,
        }

        // Private
        private const int stringShiftKey = 3;
        private static readonly StringBuilder builder = new StringBuilder();

        // Methods
        public static void WriteVersion(BinaryWriter writer, Version version)
        {
            // Calculate flags
            DLCVersionFlags flags = 0;

            if(version.Build >= 0) flags |= DLCVersionFlags.Build;
            if (version.Revision >= 0) flags |= DLCVersionFlags.Revision;

            // Write version flags
            writer.Write((ushort)flags);

            // Write version values
            writer.Write((ushort)version.Major);
            writer.Write((ushort)version.Minor);

            if((flags & DLCVersionFlags.Build) != 0)
                writer.Write((ushort)version.Build);

            if((flags & DLCVersionFlags.Revision) != 0)
                writer.Write((ushort)version.Revision);
        }

        public static void WriteString(BinaryWriter writer, string val)
        {
            // Shift characters for string
            unchecked
            {
                for (int i = 0; i < val.Length; i++)
                {
                    // Write shift character
                    builder.Append(CharacterShift(val[i], stringShiftKey));
                }
            }

            // Write to stream
            writer.Write(builder.ToString());

            // Reset shared builder
            builder.Clear();
        }

        public static Version ReadVersion(BinaryReader reader)
        {
            // Read flags
            DLCVersionFlags flags = (DLCVersionFlags)reader.ReadUInt16();

            // Check for full version
            if((flags & DLCVersionFlags.Build) != 0 && (flags & DLCVersionFlags.Revision) != 0)
            {
                // Read full version
                return new Version(
                    reader.ReadUInt16(),
                    reader.ReadUInt16(),
                    reader.ReadUInt16(),
                    reader.ReadUInt16());
            }
            // Check for build version
            else if((flags & DLCVersionFlags.Build) != 0)
            {
                // Read partial version
                return new Version(
                    reader.ReadUInt16(),
                    reader.ReadUInt16(),
                    reader.ReadUInt16());
            }
            // Short version
            else
            {
                // Read short version
                return new Version(
                    reader.ReadUInt16(),
                    reader.ReadUInt16());
            }
        }

        public static string ReadString(BinaryReader reader)
        {
            // Get temp string
            string temp = reader.ReadString();

            // Process all characters
            unchecked
            {
                for(int i = 0; i < temp.Length; i++)
                {
                    // Read shifted character
                    builder.Append(CharacterShift(temp[i], -stringShiftKey));
                }
            }

            // Get result
            string result = builder.ToString();

            // Reset shared builder
            builder.Clear();

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char CharacterShift(char c, int offset)
        {
            char max = char.MaxValue;
            char min = char.MinValue;

            // Calculate shifted
            int shifted = Convert.ToInt32(c) + offset;

            // Clamp max
            if (shifted > max)
                shifted -= max;

            // Clamp min
            if (shifted < min)
                shifted += min;

            // Get char
            return Convert.ToChar(shifted);
        }
    }
}
