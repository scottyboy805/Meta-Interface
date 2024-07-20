using DLCToolkit.Format;
using DLCToolkit.Profile;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.BuildTools.Format
{
    internal sealed class DLCBuildIconSet : DLCIconSet, IDLCBuildBundleEntry
    {
        // Type
        private struct BuildIconHeader
        {
            public string customKey;
            public IconHeader data;
            public Texture2D icon;

            // Constructor
            public BuildIconHeader(DLCIconType type, Texture2D icon)
            {
                this.customKey = null;
                this.data = new IconHeader { type = type };
                this.icon = icon;
            }

            public BuildIconHeader(string key, Texture2D icon)
            {
                this.customKey = key;
                this.data = new IconHeader { type = (DLCIconType)(-1) };
                this.icon = icon;
            }
        }

        // Private
        private DLCProfile profile = null;
        private List<BuildIconHeader> buildIconHeaders = new List<BuildIconHeader>();

        // Constructor
        internal DLCBuildIconSet(DLCProfile profile) 
            : base(null)
        {
            this.profile = profile;
        }

        // Methods
        public void WriteToStream(Stream stream)
        {
            // Get built in size
            int builtInSize = 0;
            int customSize = profile.CustomIcons.Count;

            // Calculate size
            if (profile.SmallIcon != null) builtInSize++;
            if (profile.MediumIcon != null) builtInSize++;
            if (profile.LargeIcon != null) builtInSize++;
            if (profile.ExtraLargeIcon != null) builtInSize++;

            // Store icon start position
            long iconsStart = stream.Position;

            // Create writer
            BinaryWriter writer = new BinaryWriter(stream);

            // Write size
            writer.Write((ushort)builtInSize);
            writer.Write((ushort)customSize);

            // Write small icon
            if (profile.SmallIcon != null)
                buildIconHeaders.Add(new BuildIconHeader(DLCIconType.Small, profile.SmallIcon));

            // Write medium icon
            if(profile.MediumIcon != null)
                buildIconHeaders.Add(new BuildIconHeader(DLCIconType.Medium, profile.MediumIcon));

            // Write large icon
            if(profile.LargeIcon != null) 
                buildIconHeaders.Add(new BuildIconHeader(DLCIconType.Large, profile.LargeIcon));

            // Write extra large icon
            if(profile.ExtraLargeIcon != null)
                buildIconHeaders.Add(new BuildIconHeader(DLCIconType.ExtraLarge, profile.ExtraLargeIcon));

            // Remember stream position
            long headerStart = stream.Position;

            // Write custom icons
            for (int i = 0;  i < customSize; i++)
            {
                // Check for icon available
                if (profile.CustomIcons[i] != null)
                {
                    buildIconHeaders.Add(new BuildIconHeader(profile.CustomIcons[i].CustomKey, profile.CustomIcons[i].CustomIcon));
                }
                else
                {
                    Debug.LogWarning("Custom icon is setup but has no texture assigned: " + profile.CustomIcons[i].CustomKey);
                }
            }

            // Write all headers
            for(int i = 0; i < buildIconHeaders.Count; i++)
            {
                WriteIconEntry(writer, buildIconHeaders[i]);
            }

            // Write all raw icon data
            for(int i = 0; i < buildIconHeaders.Count; i++)
            {
                // Write the raw icon data
                BuildIconHeader header = buildIconHeaders[i];
                WriteIconRawData(writer, iconsStart, buildIconHeaders[i].icon, ref header.data);

                // Update the header info
                buildIconHeaders[i] = header;
            }

            // Get current position
            long iconEnd = stream.Position;

            // Return to start
            writer.Flush();
            stream.Seek(headerStart, SeekOrigin.Begin);

            // Overwrite all header data now with correct values
            for (int i = 0; i < buildIconHeaders.Count; i++)
            {
                WriteIconEntry(writer, buildIconHeaders[i]);
            }

            // Return to final position
            stream.Seek(iconEnd, SeekOrigin.Begin);
        }

        private void WriteIconEntry(BinaryWriter writer, BuildIconHeader header)
        {
            // Write type
            writer.Write((ushort)header.data.type);

            // Check for custom
            if(header.data.type < 0)
            {
                // Write custom type key
                writer.Write(header.customKey);
            }

            // Write the header data
            writer.Write(header.data.streamStart);
            writer.Write(header.data.streamSize);
        }

        private void WriteIconRawData(BinaryWriter writer, long relativeOffset, Texture2D icon, ref IconHeader header)
        {
            // Get current position
            long offset = writer.BaseStream.Position;

            // Get asset path
            string iconPath = AssetDatabase.GetAssetPath(icon);

            // Check for error
            if (string.IsNullOrEmpty(iconPath) == true)
            {
                Debug.LogWarning("Failed to locate icon asset: " + icon);
                return;
            }

            // Read as texture with write support
            Texture2D readable = new Texture2D(0, 0);
            if (ImageConversion.LoadImage(readable, File.ReadAllBytes(iconPath)) == false)
            {
                Debug.LogWarning("Failed to load icon asset: " + iconPath);
                return;
            }

            // Encode texture
            byte[] iconBytes = ImageConversion.EncodeToPNG(readable);

            // Write to stream
            writer.Write(iconBytes);

            // Calculate size
            long size = writer.BaseStream.Position - offset;

            // Update header
            header.streamStart = (int)(offset - relativeOffset);
            header.streamSize = (int)size;
        }
    }
}
