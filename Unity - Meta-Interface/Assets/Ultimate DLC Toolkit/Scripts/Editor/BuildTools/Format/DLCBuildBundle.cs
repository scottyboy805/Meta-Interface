using DLCToolkit.Format;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DLCToolkit.BuildTools.Format
{
    internal class DLCBuildBundle : DLCBundle
    {
        // Type
        private struct ContentEntry
        {
            // Public
            public ContentType type;
            public IDLCBuildBundleEntry entry;
            public ContentHeader header;
        }

        // Private
        private RuntimePlatform platform = 0;
        private ContentFlags contentFlags = 0;

        private List<ContentEntry> entries = new List<ContentEntry>();

        // Constructor
        internal DLCBuildBundle(RuntimePlatform platform, ContentFlags contentFlags)
            : base(null)
        {
            this.platform = platform;
            this.contentFlags = contentFlags;
        }

        // Methods
        public void AddContentEntry(ContentType contentType, IDLCBuildBundleEntry entry)
        {
            entries.Add(new ContentEntry
            {
                type = contentType,
                entry = entry,
                header = new ContentHeader
                {
                    type = contentType,
                    streamStart = 0,
                    streamSize = 0,
                },
            });
        }

        public void WiteToSteam(Stream stream)
        {
            // Write header data
            WriteBundleContents(stream);

            // Write all entries
            for(int i = 0; i < entries.Count; i++)
            {
                ContentEntry entry = entries[i];

                // Get starting position
                long offset = stream.Position;

                // Write the data
                entries[i].entry.WriteToStream(stream);

                // Calculate size
                long size = stream.Position - offset;

                // Update header
                entry.header.streamStart = offset;
                entry.header.streamSize = size;

                // Update entry
                entries[i] = entry;
            }

            // Overwrite header data with correct calculated values
            stream.Seek(0, SeekOrigin.Begin);

            // Write contents
            WriteBundleContents(stream);
        }

        private void WriteBundleContents(Stream stream)
        {
            // Create writer
            BinaryWriter writer = new BinaryWriter(stream);

            // Write identifier
            writer.Write(DLCFileIdentifier);

            // Create header
            header.version = DLCFileVersion;
            header.platform = platform;
            header.flags = contentFlags;
            header.contentSize = entries.Count;

            // Write file header values
            writer.Write(header.version);
            writer.Write((int)header.platform);
            writer.Write((ushort)header.flags);
            writer.Write(header.contentSize);
            writer.Write(header.reserved1);
            writer.Write(header.reserved2);

            // Check for signing
            if ((Flags & ContentFlags.Signed) != 0)
            {
                string productHash;
                string versionHash;

                // Get the has string
                GetSignedHashString(Flags, out productHash, out versionHash);

                // Write build guid hash
                using (HashAlgorithm hash = SHA256.Create())
                {
                    // Write product hash
                    writer.Write(hash.ComputeHash(Encoding.UTF8.GetBytes(productHash)));

                    // Check for version hash
                    if ((Flags & ContentFlags.SignedWithVersion) != 0)
                        writer.Write(hash.ComputeHash(Encoding.UTF8.GetBytes(versionHash)));
                }
            }

            // Write content entries
            for (int i = 0; i < entries.Count; i++)
            {
                // Write all entries
                writer.Write((ushort)entries[i].type);
                writer.Write(entries[i].header.streamStart);
                writer.Write(entries[i].header.streamSize);
            }            
        }

        private void GetSignedHashString(ContentFlags flags, out string productHash, out string versionHash)
        {
            // Get signed string
            productHash = GuidHashString + "-" + Application.productName;

            // Add signed version
            versionHash = ((flags & ContentFlags.SignedWithVersion) != 0)
                ? Application.version
                : null;
        }
    }
}
