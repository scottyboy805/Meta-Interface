using DLCToolkit.Format;
using System;
using System.IO;

namespace DLCToolkit.BuildTools.Format
{
    internal sealed class DLCBuildMetadata : DLCMetadata, IDLCBuildBundleEntry
    {
        // Constructor
        internal DLCBuildMetadata(DLCNameInfo nameInfo, string guid, string description, string developer, string publisher, Version toolkitVersion, string unityVersion, DLCContentFlags contentFlags, bool shippedWithGame, DLCCustomMetadata customMetadata)
            : base(nameInfo, guid, description, developer, publisher, toolkitVersion, unityVersion, contentFlags, shippedWithGame)
        {
            this.customMetadata = customMetadata;
        }

        // Methods
        public void WriteToStream(Stream stream)
        {
            WriteBinaryStream(stream);
        }

        private void WriteBinaryStream(Stream stream)
        {
            // Create binary writer
            BinaryWriter writer = new BinaryWriter(stream);

            // Write name info
            nameInfo.WriteBinary(writer);

            // Write all values
            writer.Write(guid);
            writer.Write(description);
            writer.Write(developer);
            writer.Write(publisher);
            DLCFormatUtils.WriteVersion(writer, toolkitVersion);
            writer.Write(unityVersion);
            writer.Write((uint)contentFlags);
            writer.Write(buildTime.ToFileTime());
            writer.Write(shippedWithGame);

            // Write custom metadata
            writer.Write((bool)(customMetadata != null));

            if(customMetadata != null)
            {
                // Write custom metadata string
                DLCFormatUtils.WriteString(writer, customMetadata.GetType().AssemblyQualifiedName);

                // Write custom metadata serialized data
                DLCFormatUtils.WriteString(writer, customMetadata.ToSerializeString());
            }
        }
    }
}
